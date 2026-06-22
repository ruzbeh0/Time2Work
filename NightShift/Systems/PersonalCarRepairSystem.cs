using Colossal.Mathematics;
using Game;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Net;
using Game.Simulation;
using Game.Tools;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Time2Work.Systems
{
    public partial class PersonalCarRepairSystem : GameSystemBase
    {
        private const uint kRepairIntervalFrames = 4096;
        private const int kMaxFreeRemoteRepairsPerUpdate = 64;
        private const float kRemotePersonalCarDistanceSq = 1000f * 1000f;

        private EntityQuery m_HouseholdVehicleQuery;
        private SimulationSystem m_SimulationSystem;
        private Time2WorkTimeSystem m_TimeSystem;
        private EndFrameBarrier m_EndFrameBarrier;
        private uint m_LastRepairFrame;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_SimulationSystem = World.GetOrCreateSystemManaged<SimulationSystem>();
            m_TimeSystem = World.GetOrCreateSystemManaged<Time2WorkTimeSystem>();
            m_EndFrameBarrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();
            m_HouseholdVehicleQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<Household>(),
                    ComponentType.ReadOnly<PropertyRenter>(),
                    ComponentType.ReadOnly<OwnedVehicle>()
                },
                None = new ComponentType[]
                {
                    ComponentType.ReadOnly<Deleted>(),
                    ComponentType.ReadOnly<Temp>()
                }
            });
            RequireForUpdate(m_HouseholdVehicleQuery);
        }

        protected override void OnUpdate()
        {
            uint frame = m_SimulationSystem.frameIndex;
            if (m_LastRepairFrame != 0 && frame - m_LastRepairFrame < kRepairIntervalFrames)
                return;

            m_LastRepairFrame = frame;

            NativeArray<Entity> households = m_HouseholdVehicleQuery.ToEntityArray(Allocator.Temp);
            try
            {
                BufferLookup<OwnedVehicle> ownedVehicles = SystemAPI.GetBufferLookup<OwnedVehicle>(true);
                BufferLookup<HouseholdCitizen> householdCitizens = SystemAPI.GetBufferLookup<HouseholdCitizen>(true);
                ComponentLookup<PropertyRenter> propertyRenters = SystemAPI.GetComponentLookup<PropertyRenter>(true);
                ComponentLookup<Game.Vehicles.PersonalCar> personalCars = SystemAPI.GetComponentLookup<Game.Vehicles.PersonalCar>(true);
                ComponentLookup<ParkedCar> parkedCars = SystemAPI.GetComponentLookup<ParkedCar>(true);
                ComponentLookup<CurrentBuilding> currentBuildings = SystemAPI.GetComponentLookup<CurrentBuilding>(true);
                ComponentLookup<Game.Objects.Transform> transforms = SystemAPI.GetComponentLookup<Game.Objects.Transform>(true);
                ComponentLookup<Curve> curves = SystemAPI.GetComponentLookup<Curve>(true);
                ComponentLookup<FixParkingLocation> fixParkingLocations = SystemAPI.GetComponentLookup<FixParkingLocation>(true);
                ComponentLookup<Updated> updateds = SystemAPI.GetComponentLookup<Updated>(true);
                ComponentLookup<Deleted> deleteds = SystemAPI.GetComponentLookup<Deleted>(true);
                ComponentLookup<Temp> temps = SystemAPI.GetComponentLookup<Temp>(true);
                EntityCommandBuffer commandBuffer = default;
                bool commandBufferCreated = false;

                int remoteFree = 0;
                int remoteFreeNearHouseholdMember = 0;
                int freeRepairQueued = 0;
                int freeRepairDeferred = 0;
                int remoteKeptHome = 0;
                int repairQueued = 0;
                int alreadyPending = 0;
                int updatedQueued = 0;
                int unknownLocation = 0;

                for (int i = 0; i < households.Length; i++)
                {
                    Entity household = households[i];
                    if (!ownedVehicles.HasBuffer(household) || !propertyRenters.TryGetComponent(household, out PropertyRenter propertyRenter))
                        continue;

                    Entity home = propertyRenter.m_Property;
                    if (home == Entity.Null)
                        continue;

                    DynamicBuffer<OwnedVehicle> vehicles = ownedVehicles[household];
                    for (int j = 0; j < vehicles.Length; j++)
                    {
                        Entity car = vehicles[j].m_Vehicle;
                        if (deleteds.HasComponent(car) || temps.HasComponent(car))
                            continue;

                        if (!personalCars.TryGetComponent(car, out Game.Vehicles.PersonalCar personalCar))
                            continue;

                        if ((personalCar.m_State & PersonalCarFlags.HomeTarget) != 0)
                            continue;

                        if (!parkedCars.TryGetComponent(car, out ParkedCar parkedCar))
                            continue;

                        bool locationKnown;
                        if (IsParkedNearEntity(parkedCar, home, transforms, curves, out locationKnown))
                            continue;

                        if (!locationKnown)
                        {
                            unknownLocation++;
                            continue;
                        }

                        if (personalCar.m_Keeper == Entity.Null)
                        {
                            remoteFree++;
                            if (IsParkedNearHouseholdMember(parkedCar, household, householdCitizens, currentBuildings, transforms, curves))
                            {
                                remoteFreeNearHouseholdMember++;
                                continue;
                            }

                            if (freeRepairQueued >= kMaxFreeRemoteRepairsPerUpdate)
                            {
                                freeRepairDeferred++;
                                continue;
                            }

                            if (QueueParkingRepair(car, home, m_EndFrameBarrier, fixParkingLocations, updateds, ref commandBuffer, ref commandBufferCreated, ref alreadyPending, ref updatedQueued))
                            {
                                repairQueued++;
                                freeRepairQueued++;
                            }
                            continue;
                        }

                        if (deleteds.HasComponent(personalCar.m_Keeper) || temps.HasComponent(personalCar.m_Keeper))
                            continue;

                        if (!currentBuildings.TryGetComponent(personalCar.m_Keeper, out CurrentBuilding currentBuilding) || currentBuilding.m_CurrentBuilding != home)
                            continue;

                        remoteKeptHome++;
                        if (QueueParkingRepair(car, home, m_EndFrameBarrier, fixParkingLocations, updateds, ref commandBuffer, ref commandBufferCreated, ref alreadyPending, ref updatedQueued))
                            repairQueued++;
                    }
                }

                if (remoteFree > 0 || remoteKeptHome > 0 || repairQueued > 0 || alreadyPending > 0 || updatedQueued > 0 || unknownLocation > 0)
                {
                    Mod.log.Info($"[RT][PersonalCarRepair] frame={frame} time={m_TimeSystem.normalizedTime:0.000} remote_free={remoteFree} remote_free_near_household_member={remoteFreeNearHouseholdMember} free_repair_queued={freeRepairQueued} free_repair_deferred={freeRepairDeferred} remote_kept_home={remoteKeptHome} repair_queued={repairQueued} already_pending={alreadyPending} updated_queued={updatedQueued} unknown_location={unknownLocation}");
                }
            }
            finally
            {
                households.Dispose();
            }
        }

        private static bool IsParkedNearEntity(
            ParkedCar parkedCar,
            Entity target,
            ComponentLookup<Game.Objects.Transform> transforms,
            ComponentLookup<Curve> curves,
            out bool locationKnown)
        {
            locationKnown = true;

            if (target == Entity.Null || !transforms.TryGetComponent(target, out Game.Objects.Transform targetTransform) || !curves.TryGetComponent(parkedCar.m_Lane, out Curve curve))
            {
                locationKnown = false;
                return false;
            }

            float3 parkedPosition = MathUtils.Position(curve.m_Bezier, parkedCar.m_CurvePosition);
            return math.distancesq(parkedPosition, targetTransform.m_Position) <= kRemotePersonalCarDistanceSq;
        }

        private static bool IsParkedNearHouseholdMember(
            ParkedCar parkedCar,
            Entity household,
            BufferLookup<HouseholdCitizen> householdCitizens,
            ComponentLookup<CurrentBuilding> currentBuildings,
            ComponentLookup<Game.Objects.Transform> transforms,
            ComponentLookup<Curve> curves)
        {
            if (!householdCitizens.HasBuffer(household))
                return false;

            DynamicBuffer<HouseholdCitizen> citizens = householdCitizens[household];
            for (int i = 0; i < citizens.Length; i++)
            {
                if (!currentBuildings.TryGetComponent(citizens[i].m_Citizen, out CurrentBuilding currentBuilding))
                    continue;

                bool locationKnown;
                if (IsParkedNearEntity(parkedCar, currentBuilding.m_CurrentBuilding, transforms, curves, out locationKnown) && locationKnown)
                    return true;
            }

            return false;
        }

        private static bool QueueParkingRepair(
            Entity car,
            Entity home,
            EndFrameBarrier endFrameBarrier,
            ComponentLookup<FixParkingLocation> fixParkingLocations,
            ComponentLookup<Updated> updateds,
            ref EntityCommandBuffer commandBuffer,
            ref bool commandBufferCreated,
            ref int alreadyPending,
            ref int updatedQueued)
        {
            if (fixParkingLocations.HasComponent(car))
            {
                alreadyPending++;
                if (!updateds.HasComponent(car))
                {
                    EnsureCommandBuffer(endFrameBarrier, ref commandBuffer, ref commandBufferCreated);
                    commandBuffer.AddComponent(car, new Updated());
                    updatedQueued++;
                }
                return false;
            }

            EnsureCommandBuffer(endFrameBarrier, ref commandBuffer, ref commandBufferCreated);
            commandBuffer.AddComponent(car, new FixParkingLocation(Entity.Null, home));
            if (!updateds.HasComponent(car))
            {
                commandBuffer.AddComponent(car, new Updated());
                updatedQueued++;
            }
            return true;
        }

        private static void EnsureCommandBuffer(
            EndFrameBarrier endFrameBarrier,
            ref EntityCommandBuffer commandBuffer,
            ref bool commandBufferCreated)
        {
            if (commandBufferCreated)
                return;

            commandBuffer = endFrameBarrier.CreateCommandBuffer();
            commandBufferCreated = true;
        }
    }
}
