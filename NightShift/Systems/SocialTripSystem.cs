using Game;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Pathfind;
using Game.Simulation;
using Game.Tools;
using Time2Work.Bridge;
using Time2Work.Components;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Time2Work.Systems
{
    public partial class SocialTripSystem : GameSystemBase
    {
        private EntityQuery m_RequestQuery;
        private EntityQuery m_ActiveQuery;
        private Time2WorkTimeSystem m_TimeSystem;
        private SimulationSystem m_SimulationSystem;

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            return 16;
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            m_TimeSystem = World.GetOrCreateSystemManaged<Time2WorkTimeSystem>();
            m_SimulationSystem = World.GetOrCreateSystemManaged<SimulationSystem>();

            m_RequestQuery = GetEntityQuery(new EntityQueryDesc()
            {
                All = new[]
                {
                    ComponentType.ReadWrite<SocialTripRequest>(),
                    ComponentType.ReadWrite<Citizen>(),
                    ComponentType.ReadOnly<HouseholdMember>(),
                    ComponentType.ReadOnly<CurrentBuilding>(),
                    ComponentType.ReadWrite<TripNeeded>()
                },
                None = new[]
                {
                    ComponentType.Exclude<Deleted>(),
                    ComponentType.Exclude<Temp>(),
                    ComponentType.Exclude<HealthProblem>()
                }
            });

            m_ActiveQuery = GetEntityQuery(new EntityQueryDesc()
            {
                All = new[]
                {
                    ComponentType.ReadWrite<SocialTripData>(),
                    ComponentType.ReadWrite<Citizen>(),
                    ComponentType.ReadOnly<HouseholdMember>(),
                    ComponentType.ReadOnly<CurrentBuilding>(),
                    ComponentType.ReadWrite<TripNeeded>()
                },
                None = new[]
                {
                    ComponentType.Exclude<Deleted>(),
                    ComponentType.Exclude<Temp>(),
                    ComponentType.Exclude<HealthProblem>()
                }
            });

            RequireAnyForUpdate(m_RequestQuery, m_ActiveQuery);
        }

        protected override void OnUpdate()
        {
            ProcessRequests();
            ProcessActiveTrips();
        }

        private void ProcessRequests()
        {
            NativeArray<Entity> entities = m_RequestQuery.ToEntityArray(Allocator.Temp);
            try
            {
                uint frame = m_SimulationSystem.frameIndex;
                float timeOfDay = m_TimeSystem.normalizedTime;

                for (int i = 0; i < entities.Length; i++)
                {
                    Entity citizen = entities[i];
                    SocialTripRequest request = EntityManager.GetComponentData<SocialTripRequest>(citizen);

                    if (!IsValidTarget(request.targetBuilding) ||
                        IsRequestExpired(request, frame) ||
                        EntityManager.HasComponent<SocialTripData>(citizen) ||
                        (IsHomeVisitTrip(request.tripType) && !IsHostAvailableAtHome(request.hostCitizen, request.targetBuilding)))
                    {
                        EntityManager.RemoveComponent<SocialTripRequest>(citizen);
                        continue;
                    }

                    if (EntityManager.HasComponent<TravelPurpose>(citizen))
                    {
                        if (IsRequestExpired(request, frame))
                        {
                            EntityManager.RemoveComponent<SocialTripRequest>(citizen);
                        }
                        continue;
                    }

                    CurrentBuilding currentBuilding = EntityManager.GetComponentData<CurrentBuilding>(citizen);
                    bool alreadyThere = currentBuilding.m_CurrentBuilding == request.targetBuilding;

                    SocialTripData tripData = new SocialTripData()
                    {
                        version = 1,
                        targetBuilding = request.targetBuilding,
                        hostCitizen = request.hostCitizen,
                        tripType = request.tripType,
                        startTime = 0f,
                        duration = math.max(10f / 1440f, request.duration),
                        flags = 0
                    };

                    if (alreadyThere)
                    {
                        tripData.MarkArrived(timeOfDay);
                        EnsureTravelPurpose(citizen, Game.Citizens.Purpose.Leisure);
                    }
                    else
                    {
                        AddTrip(citizen, request.targetBuilding, Game.Citizens.Purpose.Leisure);
                        SetTarget(citizen, request.targetBuilding);
                    }

                    RemoveTripSetupComponents(citizen);
                    EntityManager.AddComponentData(citizen, tripData);
                    if (IsHomeVisitTrip(request.tripType))
                    {
                        LockHomeVisitHost(request.hostCitizen, tripData, citizen, timeOfDay);
                    }
                    EntityManager.RemoveComponent<SocialTripRequest>(citizen);
                }
            }
            finally
            {
                entities.Dispose();
            }
        }

        private void ProcessActiveTrips()
        {
            NativeArray<Entity> entities = m_ActiveQuery.ToEntityArray(Allocator.Temp);
            try
            {
                float timeOfDay = m_TimeSystem.normalizedTime;

                for (int i = 0; i < entities.Length; i++)
                {
                    Entity citizen = entities[i];
                    SocialTripData tripData = EntityManager.GetComponentData<SocialTripData>(citizen);

                    if (!IsValidTarget(tripData.targetBuilding))
                    {
                        FinishTrip(citizen, tripData, sendHome: false);
                        continue;
                    }

                    if (tripData.IsHostLocked)
                    {
                        ProcessHostLock(citizen, tripData, timeOfDay);
                        continue;
                    }

                    CurrentBuilding currentBuilding = EntityManager.GetComponentData<CurrentBuilding>(citizen);
                    bool atTarget = currentBuilding.m_CurrentBuilding == tripData.targetBuilding;

                    if (atTarget && !tripData.HasArrived)
                    {
                        tripData.MarkArrived(timeOfDay);
                        EntityManager.SetComponentData(citizen, tripData);
                        EnsureTravelPurpose(citizen, Game.Citizens.Purpose.Leisure);
                        SetTarget(citizen, tripData.targetBuilding);
                        continue;
                    }

                    if (atTarget && tripData.HasArrived)
                    {
                        EnsureTravelPurpose(citizen, Game.Citizens.Purpose.Leisure);
                        if (GetElapsed(tripData.startTime, timeOfDay) >= tripData.duration)
                        {
                            FinishTrip(citizen, tripData, sendHome: true);
                        }
                    }
                    else if (tripData.HasArrived)
                    {
                        FinishTrip(citizen, tripData, sendHome: false);
                    }
                }
            }
            finally
            {
                entities.Dispose();
            }
        }

        private bool IsValidTarget(Entity target)
        {
            return target != Entity.Null &&
                   EntityManager.Exists(target) &&
                   EntityManager.HasComponent<Building>(target) &&
                   !EntityManager.HasComponent<Deleted>(target) &&
                   !EntityManager.HasComponent<Temp>(target);
        }

        private static bool IsRequestExpired(SocialTripRequest request, uint frame)
        {
            uint timeoutFrames = (uint)math.max(1, Time2WorkTimeSystem.kTicksPerDay / 24);
            return frame > request.requestedFrame && frame - request.requestedFrame > timeoutFrames;
        }

        private static float GetElapsed(float start, float end)
        {
            start = math.frac(start);
            end = math.frac(end);
            return end >= start ? end - start : 1f - start + end;
        }

        private static bool IsHomeVisitTrip(int tripType)
        {
            return tripType == 1 || tripType == 2;
        }

        private bool IsHostAvailableAtHome(Entity hostCitizen, Entity targetBuilding)
        {
            if (hostCitizen == Entity.Null ||
                !EntityManager.Exists(hostCitizen) ||
                !EntityManager.HasComponent<Citizen>(hostCitizen) ||
                !EntityManager.HasComponent<HouseholdMember>(hostCitizen) ||
                !EntityManager.HasComponent<CurrentBuilding>(hostCitizen) ||
                !EntityManager.HasBuffer<TripNeeded>(hostCitizen) ||
                EntityManager.HasComponent<TravelPurpose>(hostCitizen) ||
                EntityManager.HasComponent<SocialTripData>(hostCitizen) ||
                EntityManager.HasComponent<Deleted>(hostCitizen) ||
                EntityManager.HasComponent<Temp>(hostCitizen) ||
                EntityManager.HasComponent<HealthProblem>(hostCitizen))
            {
                return false;
            }

            CurrentBuilding currentBuilding = EntityManager.GetComponentData<CurrentBuilding>(hostCitizen);
            if (currentBuilding.m_CurrentBuilding != targetBuilding)
                return false;

            Entity household = EntityManager.GetComponentData<HouseholdMember>(hostCitizen).m_Household;
            if (household == Entity.Null ||
                !EntityManager.Exists(household) ||
                !EntityManager.HasComponent<PropertyRenter>(household))
            {
                return false;
            }

            return EntityManager.GetComponentData<PropertyRenter>(household).m_Property == targetBuilding;
        }

        private void LockHomeVisitHost(Entity hostCitizen, SocialTripData visitorTripData, Entity visitor, float timeOfDay)
        {
            if (!IsHostAvailableAtHome(hostCitizen, visitorTripData.targetBuilding))
                return;

            SocialTripData hostTripData = new SocialTripData()
            {
                version = 1,
                targetBuilding = visitorTripData.targetBuilding,
                hostCitizen = visitor,
                tripType = visitorTripData.tripType,
                startTime = timeOfDay,
                duration = visitorTripData.duration,
                flags = SocialTripData.ArrivedFlag | SocialTripData.HostLockedFlag
            };

            RemoveTripSetupComponents(hostCitizen);
            EnsureTravelPurpose(hostCitizen, Game.Citizens.Purpose.Leisure);
            SetTarget(hostCitizen, visitorTripData.targetBuilding);
            EntityManager.AddComponentData(hostCitizen, hostTripData);
        }

        private void ProcessHostLock(Entity hostCitizen, SocialTripData tripData, float timeOfDay)
        {
            if (GetElapsed(tripData.startTime, timeOfDay) >= tripData.duration)
            {
                FinishTrip(hostCitizen, tripData, sendHome: false);
                return;
            }

            CurrentBuilding currentBuilding = EntityManager.GetComponentData<CurrentBuilding>(hostCitizen);
            if (currentBuilding.m_CurrentBuilding != tripData.targetBuilding)
            {
                AddTrip(hostCitizen, tripData.targetBuilding, Game.Citizens.Purpose.Leisure);
            }

            EnsureTravelPurpose(hostCitizen, Game.Citizens.Purpose.Leisure);
            SetTarget(hostCitizen, tripData.targetBuilding);
        }

        private void AddTrip(Entity citizen, Entity target, Game.Citizens.Purpose purpose)
        {
            DynamicBuffer<TripNeeded> trips = EntityManager.GetBuffer<TripNeeded>(citizen);
            for (int i = 0; i < trips.Length; i++)
            {
                TripNeeded trip = trips[i];
                if (trip.m_TargetAgent == target && trip.m_Purpose == purpose)
                    return;
            }

            trips.Add(new TripNeeded()
            {
                m_TargetAgent = target,
                m_Purpose = purpose,
                m_Priority = 128
            });
        }

        private void SetTarget(Entity citizen, Entity target)
        {
            Target component = new Target()
            {
                m_Target = target
            };

            if (EntityManager.HasComponent<Target>(citizen))
            {
                EntityManager.SetComponentData(citizen, component);
            }
            else
            {
                EntityManager.AddComponentData(citizen, component);
            }
        }

        private void EnsureTravelPurpose(Entity citizen, Game.Citizens.Purpose purpose)
        {
            if (EntityManager.HasComponent<TravelPurpose>(citizen))
            {
                TravelPurpose travelPurpose = EntityManager.GetComponentData<TravelPurpose>(citizen);
                if (travelPurpose.m_Purpose == purpose)
                    return;

                EntityManager.SetComponentData(citizen, new TravelPurpose()
                {
                    m_Purpose = purpose
                });
                return;
            }

            EntityManager.AddComponentData(citizen, new TravelPurpose()
            {
                m_Purpose = purpose
            });
        }

        private void RemoveTripSetupComponents(Entity citizen)
        {
            if (EntityManager.HasComponent<Leisure>(citizen))
            {
                EntityManager.RemoveComponent<Leisure>(citizen);
            }
            if (EntityManager.HasComponent<PathInformation>(citizen))
            {
                EntityManager.RemoveComponent<PathInformation>(citizen);
            }
            if (EntityManager.HasBuffer<PathElement>(citizen))
            {
                EntityManager.RemoveComponent<PathElement>(citizen);
            }
        }

        private void AwardCompletedSocialTripLeisure(Entity citizen, SocialTripData tripData)
        {
            if (!tripData.HasArrived ||
                !EntityManager.HasComponent<Citizen>(citizen))
            {
                return;
            }

            Citizen citizenData = EntityManager.GetComponentData<Citizen>(citizen);
            int leisureCounter = citizenData.m_LeisureCounter + GetSocialTripLeisureReward(tripData);
            citizenData.m_LeisureCounter = (byte)(leisureCounter > byte.MaxValue ? byte.MaxValue : leisureCounter);
            EntityManager.SetComponentData(citizen, citizenData);
        }

        private static int GetSocialTripLeisureReward(SocialTripData tripData)
        {
            switch ((SocialTripTypeBridge)tripData.tripType)
            {
                case SocialTripTypeBridge.Funeral:
                    return 0;
                case SocialTripTypeBridge.VisitFriend:
                case SocialTripTypeBridge.VisitFamily:
                    return 24;
                case SocialTripTypeBridge.HospitalVisit:
                case SocialTripTypeBridge.PrisonVisit:
                    return 16;
                case SocialTripTypeBridge.SchoolTroubleMeeting:
                    return 0;
                case SocialTripTypeBridge.BirthdayParty:
                case SocialTripTypeBridge.HouseParty:
                case SocialTripTypeBridge.GraduationParty:
                case SocialTripTypeBridge.WeddingParty:
                case SocialTripTypeBridge.NewBabyCelebration:
                case SocialTripTypeBridge.HousewarmingParty:
                case SocialTripTypeBridge.FirstJobCelebration:
                    return 40;
                default:
                    return 32;
            }
        }

        private void FinishTrip(Entity citizen, SocialTripData tripData, bool sendHome)
        {
            AwardCompletedSocialTripLeisure(citizen, tripData);

            if (EntityManager.HasComponent<TravelPurpose>(citizen))
            {
                TravelPurpose travelPurpose = EntityManager.GetComponentData<TravelPurpose>(citizen);
                if (travelPurpose.m_Purpose == Game.Citizens.Purpose.Leisure)
                {
                    EntityManager.RemoveComponent<TravelPurpose>(citizen);
                }
            }

            if (EntityManager.HasComponent<Target>(citizen))
            {
                Target target = EntityManager.GetComponentData<Target>(citizen);
                if (target.m_Target == tripData.targetBuilding)
                {
                    EntityManager.RemoveComponent<Target>(citizen);
                }
            }

            EntityManager.RemoveComponent<SocialTripData>(citizen);

            if (!sendHome)
                return;

            HouseholdMember householdMember = EntityManager.GetComponentData<HouseholdMember>(citizen);
            if (!EntityManager.HasComponent<PropertyRenter>(householdMember.m_Household))
                return;

            Entity home = EntityManager.GetComponentData<PropertyRenter>(householdMember.m_Household).m_Property;
            if (!IsValidTarget(home))
                return;

            CurrentBuilding currentBuilding = EntityManager.GetComponentData<CurrentBuilding>(citizen);
            if (currentBuilding.m_CurrentBuilding == home)
                return;

            AddTrip(citizen, home, Game.Citizens.Purpose.GoingHome);
        }

#if ENABLE_SOCIAL_TRIP_BURST_ECB
        [BurstCompile]
        private struct ProcessRequestsJob : IJobChunk
        {
            [ReadOnly] public EntityTypeHandle EntityType;
            [ReadOnly] public ComponentTypeHandle<SocialTripRequest> RequestType;
            [ReadOnly] public ComponentTypeHandle<CurrentBuilding> CurrentBuildingType;
            public BufferTypeHandle<TripNeeded> TripNeededType;
            [ReadOnly] public ComponentLookup<Building> BuildingLookup;
            [ReadOnly] public ComponentLookup<Deleted> DeletedLookup;
            [ReadOnly] public ComponentLookup<Temp> TempLookup;
            [ReadOnly] public ComponentLookup<Citizen> CitizenLookup;
            [ReadOnly] public ComponentLookup<HouseholdMember> HouseholdMemberLookup;
            [ReadOnly] public ComponentLookup<CurrentBuilding> CurrentBuildingLookup;
            [ReadOnly] public BufferLookup<TripNeeded> HostTripNeededLookup;
            [ReadOnly] public ComponentLookup<PropertyRenter> PropertyRenterLookup;
            [ReadOnly] public ComponentLookup<TravelPurpose> TravelPurposeLookup;
            [ReadOnly] public ComponentLookup<SocialTripData> SocialTripDataLookup;
            [ReadOnly] public ComponentLookup<HealthProblem> HealthProblemLookup;
            [ReadOnly] public ComponentLookup<Target> TargetLookup;
            [ReadOnly] public ComponentLookup<Leisure> LeisureLookup;
            [ReadOnly] public ComponentLookup<PathInformation> PathInformationLookup;
            [ReadOnly] public BufferLookup<PathElement> PathElementLookup;
            public NativeParallelHashSet<Entity>.ParallelWriter TripDataReservations;
            public EntityCommandBuffer.ParallelWriter CommandBuffer;
            public uint Frame;
            public uint RequestTimeoutFrames;
            public float TimeOfDay;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> entities = chunk.GetNativeArray(EntityType);
                NativeArray<SocialTripRequest> requests = chunk.GetNativeArray(ref RequestType);
                NativeArray<CurrentBuilding> currentBuildings = chunk.GetNativeArray(ref CurrentBuildingType);
                BufferAccessor<TripNeeded> tripBuffers = chunk.GetBufferAccessor(ref TripNeededType);

                for (int i = 0; i < entities.Length; i++)
                {
                    Entity citizen = entities[i];
                    SocialTripRequest request = requests[i];
                    bool homeVisit = IsHomeVisitTrip(request.tripType);

                    if (!IsValidTarget(request.targetBuilding) ||
                        IsRequestExpired(request, Frame) ||
                        SocialTripDataLookup.HasComponent(citizen) ||
                        (homeVisit && !IsHostAvailableAtHome(request.hostCitizen, request.targetBuilding)))
                    {
                        CommandBuffer.RemoveComponent<SocialTripRequest>(unfilteredChunkIndex, citizen);
                        continue;
                    }

                    if (TravelPurposeLookup.HasComponent(citizen))
                        continue;

                    if (homeVisit && !TripDataReservations.Add(request.hostCitizen))
                    {
                        CommandBuffer.RemoveComponent<SocialTripRequest>(unfilteredChunkIndex, citizen);
                        continue;
                    }

                    if (!TripDataReservations.Add(citizen))
                    {
                        CommandBuffer.RemoveComponent<SocialTripRequest>(unfilteredChunkIndex, citizen);
                        continue;
                    }

                    bool alreadyThere = currentBuildings[i].m_CurrentBuilding == request.targetBuilding;
                    SocialTripData tripData = new SocialTripData()
                    {
                        version = 1,
                        targetBuilding = request.targetBuilding,
                        hostCitizen = request.hostCitizen,
                        tripType = request.tripType,
                        startTime = 0f,
                        duration = math.max(10f / 1440f, request.duration),
                        flags = 0
                    };

                    if (alreadyThere)
                    {
                        tripData.MarkArrived(TimeOfDay);
                        EnsureTravelPurpose(unfilteredChunkIndex, citizen, Game.Citizens.Purpose.Leisure);
                    }
                    else
                    {
                        AddTripIfMissing(tripBuffers[i], request.targetBuilding, Game.Citizens.Purpose.Leisure);
                        SetTarget(unfilteredChunkIndex, citizen, request.targetBuilding);
                    }

                    RemoveTripSetupComponents(unfilteredChunkIndex, citizen);
                    CommandBuffer.AddComponent(unfilteredChunkIndex, citizen, tripData);

                    if (homeVisit)
                    {
                        LockHomeVisitHost(unfilteredChunkIndex, request.hostCitizen, tripData, citizen);
                    }

                    CommandBuffer.RemoveComponent<SocialTripRequest>(unfilteredChunkIndex, citizen);
                }
            }

            private bool IsValidTarget(Entity target)
            {
                return target != Entity.Null &&
                       BuildingLookup.HasComponent(target) &&
                       !DeletedLookup.HasComponent(target) &&
                       !TempLookup.HasComponent(target);
            }

            private bool IsRequestExpired(SocialTripRequest request, uint frame)
            {
                return frame > request.requestedFrame && frame - request.requestedFrame > RequestTimeoutFrames;
            }

            private static bool IsHomeVisitTrip(int tripType)
            {
                return tripType == 1 || tripType == 2;
            }

            private bool IsHostAvailableAtHome(Entity hostCitizen, Entity targetBuilding)
            {
                if (hostCitizen == Entity.Null ||
                    !CitizenLookup.HasComponent(hostCitizen) ||
                    !HouseholdMemberLookup.HasComponent(hostCitizen) ||
                    !CurrentBuildingLookup.HasComponent(hostCitizen) ||
                    !HostTripNeededLookup.HasBuffer(hostCitizen) ||
                    TravelPurposeLookup.HasComponent(hostCitizen) ||
                    SocialTripDataLookup.HasComponent(hostCitizen) ||
                    DeletedLookup.HasComponent(hostCitizen) ||
                    TempLookup.HasComponent(hostCitizen) ||
                    HealthProblemLookup.HasComponent(hostCitizen))
                {
                    return false;
                }

                if (CurrentBuildingLookup[hostCitizen].m_CurrentBuilding != targetBuilding)
                    return false;

                Entity household = HouseholdMemberLookup[hostCitizen].m_Household;
                return household != Entity.Null &&
                       PropertyRenterLookup.HasComponent(household) &&
                       PropertyRenterLookup[household].m_Property == targetBuilding;
            }

            private void LockHomeVisitHost(int sortKey, Entity hostCitizen, SocialTripData visitorTripData, Entity visitor)
            {
                SocialTripData hostTripData = new SocialTripData()
                {
                    version = 1,
                    targetBuilding = visitorTripData.targetBuilding,
                    hostCitizen = visitor,
                    tripType = visitorTripData.tripType,
                    startTime = TimeOfDay,
                    duration = visitorTripData.duration,
                    flags = SocialTripData.ArrivedFlag | SocialTripData.HostLockedFlag
                };

                RemoveTripSetupComponents(sortKey, hostCitizen);
                EnsureTravelPurpose(sortKey, hostCitizen, Game.Citizens.Purpose.Leisure);
                SetTarget(sortKey, hostCitizen, visitorTripData.targetBuilding);
                CommandBuffer.AddComponent(sortKey, hostCitizen, hostTripData);
            }

            private void AddTripIfMissing(DynamicBuffer<TripNeeded> trips, Entity target, Game.Citizens.Purpose purpose)
            {
                for (int i = 0; i < trips.Length; i++)
                {
                    TripNeeded trip = trips[i];
                    if (trip.m_TargetAgent == target && trip.m_Purpose == purpose)
                        return;
                }

                trips.Add(new TripNeeded()
                {
                    m_TargetAgent = target,
                    m_Purpose = purpose,
                    m_Priority = 128
                });
            }

            private void SetTarget(int sortKey, Entity citizen, Entity target)
            {
                Target component = new Target()
                {
                    m_Target = target
                };

                if (TargetLookup.HasComponent(citizen))
                    CommandBuffer.SetComponent(sortKey, citizen, component);
                else
                    CommandBuffer.AddComponent(sortKey, citizen, component);
            }

            private void EnsureTravelPurpose(int sortKey, Entity citizen, Game.Citizens.Purpose purpose)
            {
                TravelPurpose component = new TravelPurpose()
                {
                    m_Purpose = purpose
                };

                if (TravelPurposeLookup.HasComponent(citizen))
                    CommandBuffer.SetComponent(sortKey, citizen, component);
                else
                    CommandBuffer.AddComponent(sortKey, citizen, component);
            }

            private void RemoveTripSetupComponents(int sortKey, Entity citizen)
            {
                if (LeisureLookup.HasComponent(citizen))
                    CommandBuffer.RemoveComponent<Leisure>(sortKey, citizen);
                if (PathInformationLookup.HasComponent(citizen))
                    CommandBuffer.RemoveComponent<PathInformation>(sortKey, citizen);
                if (PathElementLookup.HasBuffer(citizen))
                    CommandBuffer.RemoveComponent<PathElement>(sortKey, citizen);
            }
        }

        [BurstCompile]
        private struct ProcessActiveTripsJob : IJobChunk
        {
            [ReadOnly] public EntityTypeHandle EntityType;
            public ComponentTypeHandle<SocialTripData> SocialTripDataType;
            public ComponentTypeHandle<Citizen> CitizenType;
            [ReadOnly] public ComponentTypeHandle<CurrentBuilding> CurrentBuildingType;
            [ReadOnly] public ComponentTypeHandle<HouseholdMember> HouseholdMemberType;
            public BufferTypeHandle<TripNeeded> TripNeededType;
            [ReadOnly] public ComponentLookup<Building> BuildingLookup;
            [ReadOnly] public ComponentLookup<Deleted> DeletedLookup;
            [ReadOnly] public ComponentLookup<Temp> TempLookup;
            [ReadOnly] public ComponentLookup<PropertyRenter> PropertyRenterLookup;
            [ReadOnly] public ComponentLookup<TravelPurpose> TravelPurposeLookup;
            [ReadOnly] public ComponentLookup<Target> TargetLookup;
            public EntityCommandBuffer.ParallelWriter CommandBuffer;
            public float TimeOfDay;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> entities = chunk.GetNativeArray(EntityType);
                NativeArray<SocialTripData> socialTripData = chunk.GetNativeArray(ref SocialTripDataType);
                NativeArray<Citizen> citizens = chunk.GetNativeArray(ref CitizenType);
                NativeArray<CurrentBuilding> currentBuildings = chunk.GetNativeArray(ref CurrentBuildingType);
                NativeArray<HouseholdMember> householdMembers = chunk.GetNativeArray(ref HouseholdMemberType);
                BufferAccessor<TripNeeded> tripBuffers = chunk.GetBufferAccessor(ref TripNeededType);

                for (int i = 0; i < entities.Length; i++)
                {
                    Entity citizen = entities[i];
                    SocialTripData tripData = socialTripData[i];

                    if (!IsValidTarget(tripData.targetBuilding))
                    {
                        FinishTrip(unfilteredChunkIndex, citizen, tripData, ref citizens, i, tripBuffers[i], householdMembers[i], currentBuildings[i], sendHome: false);
                        continue;
                    }

                    if (tripData.IsHostLocked)
                    {
                        ProcessHostLock(unfilteredChunkIndex, citizen, tripData, ref citizens, i, tripBuffers[i], householdMembers[i], currentBuildings[i]);
                        continue;
                    }

                    bool atTarget = currentBuildings[i].m_CurrentBuilding == tripData.targetBuilding;

                    if (atTarget && !tripData.HasArrived)
                    {
                        tripData.MarkArrived(TimeOfDay);
                        socialTripData[i] = tripData;
                        EnsureTravelPurpose(unfilteredChunkIndex, citizen, Game.Citizens.Purpose.Leisure);
                        SetTarget(unfilteredChunkIndex, citizen, tripData.targetBuilding);
                        continue;
                    }

                    if (atTarget && tripData.HasArrived)
                    {
                        EnsureTravelPurpose(unfilteredChunkIndex, citizen, Game.Citizens.Purpose.Leisure);
                        if (GetElapsed(tripData.startTime, TimeOfDay) >= tripData.duration)
                        {
                            FinishTrip(unfilteredChunkIndex, citizen, tripData, ref citizens, i, tripBuffers[i], householdMembers[i], currentBuildings[i], sendHome: true);
                        }
                    }
                    else if (tripData.HasArrived)
                    {
                        FinishTrip(unfilteredChunkIndex, citizen, tripData, ref citizens, i, tripBuffers[i], householdMembers[i], currentBuildings[i], sendHome: false);
                    }
                }
            }

            private bool IsValidTarget(Entity target)
            {
                return target != Entity.Null &&
                       BuildingLookup.HasComponent(target) &&
                       !DeletedLookup.HasComponent(target) &&
                       !TempLookup.HasComponent(target);
            }

            private static float GetElapsed(float start, float end)
            {
                start = math.frac(start);
                end = math.frac(end);
                return end >= start ? end - start : 1f - start + end;
            }

            private void ProcessHostLock(
                int sortKey,
                Entity hostCitizen,
                SocialTripData tripData,
                ref NativeArray<Citizen> citizens,
                int index,
                DynamicBuffer<TripNeeded> trips,
                HouseholdMember householdMember,
                CurrentBuilding currentBuilding)
            {
                if (GetElapsed(tripData.startTime, TimeOfDay) >= tripData.duration)
                {
                    FinishTrip(sortKey, hostCitizen, tripData, ref citizens, index, trips, householdMember, currentBuilding, sendHome: false);
                    return;
                }

                if (currentBuilding.m_CurrentBuilding != tripData.targetBuilding)
                {
                    AddTripIfMissing(trips, tripData.targetBuilding, Game.Citizens.Purpose.Leisure);
                }

                EnsureTravelPurpose(sortKey, hostCitizen, Game.Citizens.Purpose.Leisure);
                SetTarget(sortKey, hostCitizen, tripData.targetBuilding);
            }

            private void FinishTrip(
                int sortKey,
                Entity citizen,
                SocialTripData tripData,
                ref NativeArray<Citizen> citizens,
                int index,
                DynamicBuffer<TripNeeded> trips,
                HouseholdMember householdMember,
                CurrentBuilding currentBuilding,
                bool sendHome)
            {
                if (tripData.HasArrived)
                {
                    Citizen citizenData = citizens[index];
                    int leisureCounter = citizenData.m_LeisureCounter + GetSocialTripLeisureReward(tripData);
                    citizenData.m_LeisureCounter = (byte)(leisureCounter > byte.MaxValue ? byte.MaxValue : leisureCounter);
                    citizens[index] = citizenData;
                }

                FinishTripWithoutSendHome(sortKey, citizen, tripData);

                if (!sendHome)
                    return;

                Entity household = householdMember.m_Household;
                if (household == Entity.Null || !PropertyRenterLookup.HasComponent(household))
                    return;

                Entity home = PropertyRenterLookup[household].m_Property;
                if (!IsValidTarget(home) || currentBuilding.m_CurrentBuilding == home)
                    return;

                AddTripIfMissing(trips, home, Game.Citizens.Purpose.GoingHome);
            }

            private void FinishTripWithoutSendHome(int sortKey, Entity citizen, SocialTripData tripData)
            {
                if (TravelPurposeLookup.HasComponent(citizen))
                {
                    TravelPurpose travelPurpose = TravelPurposeLookup[citizen];
                    if (travelPurpose.m_Purpose == Game.Citizens.Purpose.Leisure)
                    {
                        CommandBuffer.RemoveComponent<TravelPurpose>(sortKey, citizen);
                    }
                }

                if (TargetLookup.HasComponent(citizen))
                {
                    Target target = TargetLookup[citizen];
                    if (target.m_Target == tripData.targetBuilding)
                    {
                        CommandBuffer.RemoveComponent<Target>(sortKey, citizen);
                    }
                }

                CommandBuffer.RemoveComponent<SocialTripData>(sortKey, citizen);
            }

            private void AddTripIfMissing(DynamicBuffer<TripNeeded> trips, Entity target, Game.Citizens.Purpose purpose)
            {
                for (int i = 0; i < trips.Length; i++)
                {
                    TripNeeded trip = trips[i];
                    if (trip.m_TargetAgent == target && trip.m_Purpose == purpose)
                        return;
                }

                trips.Add(new TripNeeded()
                {
                    m_TargetAgent = target,
                    m_Purpose = purpose,
                    m_Priority = 128
                });
            }

            private void SetTarget(int sortKey, Entity citizen, Entity target)
            {
                Target component = new Target()
                {
                    m_Target = target
                };

                if (TargetLookup.HasComponent(citizen))
                    CommandBuffer.SetComponent(sortKey, citizen, component);
                else
                    CommandBuffer.AddComponent(sortKey, citizen, component);
            }

            private void EnsureTravelPurpose(int sortKey, Entity citizen, Game.Citizens.Purpose purpose)
            {
                TravelPurpose component = new TravelPurpose()
                {
                    m_Purpose = purpose
                };

                if (TravelPurposeLookup.HasComponent(citizen))
                {
                    TravelPurpose existing = TravelPurposeLookup[citizen];
                    if (existing.m_Purpose == purpose)
                        return;

                    CommandBuffer.SetComponent(sortKey, citizen, component);
                    return;
                }

                CommandBuffer.AddComponent(sortKey, citizen, component);
            }

            private static int GetSocialTripLeisureReward(SocialTripData tripData)
            {
                switch ((SocialTripTypeBridge)tripData.tripType)
                {
                    case SocialTripTypeBridge.Funeral:
                        return 0;
                    case SocialTripTypeBridge.VisitFriend:
                    case SocialTripTypeBridge.VisitFamily:
                        return 24;
                    case SocialTripTypeBridge.HospitalVisit:
                    case SocialTripTypeBridge.PrisonVisit:
                        return 16;
                    case SocialTripTypeBridge.SchoolTroubleMeeting:
                        return 0;
                    case SocialTripTypeBridge.BirthdayParty:
                    case SocialTripTypeBridge.HouseParty:
                    case SocialTripTypeBridge.GraduationParty:
                    case SocialTripTypeBridge.WeddingParty:
                    case SocialTripTypeBridge.NewBabyCelebration:
                    case SocialTripTypeBridge.HousewarmingParty:
                    case SocialTripTypeBridge.FirstJobCelebration:
                        return 40;
                    default:
                        return 32;
                }
            }
        }
#endif
    }
}
