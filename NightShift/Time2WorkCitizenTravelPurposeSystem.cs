// Decompiled with JetBrains decompiler
// Type: Game.Simulation.Time2WorkCitizenTravelPurposeSystem
// Assembly: Game, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 3C8C3C1D-D7EB-4536-8BE0-6F4028D2725F
// Assembly location: C:\Program Files (x86)\Steam\steamapps\common\Cities Skylines II\Cities2_Data\Managed\Game.dll

using Colossal.Collections;
using Colossal.Entities;
using Game;
using Game.Agents;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Prefabs;
using Game.Tools;
using Game.Simulation;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

#nullable disable
namespace Time2Work
{
    public partial class Time2WorkCitizenTravelPurposeSystem : GameSystemBase
    {
        private TimeSystem m_TimeSystem;
        private CityStatisticsSystem m_CityStatisticsSystem;
        private EndFrameBarrier m_EndFrameBarrier;
        private EntityQuery m_ArrivedGroup;
        private EntityQuery m_StuckGroup;
        private EntityQuery m_EconomyParameterGroup;
        private EntityQuery m_OutsideConnectionQuery;
        private EntityQuery m_ServiceBuildingQuery;
        private Time2WorkCitizenTravelPurposeSystem.TypeHandle __TypeHandle;

        public override int GetUpdateInterval(SystemUpdatePhase phase) => 16;

        protected override void OnCreate()
        {
            base.OnCreate();
            
            this.m_TimeSystem = this.World.GetOrCreateSystemManaged<TimeSystem>();
            
            this.m_CityStatisticsSystem = this.World.GetOrCreateSystemManaged<CityStatisticsSystem>();
            
            this.m_EndFrameBarrier = this.World.GetOrCreateSystemManaged<EndFrameBarrier>();
            
            this.m_ArrivedGroup = this.GetEntityQuery(ComponentType.ReadOnly<Citizen>(), ComponentType.ReadWrite<TravelPurpose>(), ComponentType.ReadWrite<TripNeeded>(), ComponentType.ReadOnly<CurrentBuilding>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
            
            this.m_StuckGroup = this.GetEntityQuery(ComponentType.ReadOnly<Citizen>(), ComponentType.ReadWrite<TravelPurpose>(), ComponentType.ReadWrite<TripNeeded>(), ComponentType.Exclude<CurrentTransport>(), ComponentType.Exclude<CurrentBuilding>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
            
            this.m_OutsideConnectionQuery = this.GetEntityQuery(ComponentType.ReadWrite<Game.Objects.OutsideConnection>(), ComponentType.Exclude<Game.Objects.ElectricityOutsideConnection>(), ComponentType.Exclude<Game.Objects.WaterPipeOutsideConnection>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
            
            this.m_ServiceBuildingQuery = this.GetEntityQuery(ComponentType.ReadWrite<CityServiceUpkeep>(), ComponentType.ReadWrite<Building>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Destroyed>(), ComponentType.Exclude<Temp>());
            
            this.m_EconomyParameterGroup = this.GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
            
            this.m_TimeSystem = this.World.GetOrCreateSystemManaged<TimeSystem>();
            
            
            this.RequireAnyForUpdate(this.m_ArrivedGroup, this.m_StuckGroup);
        }

        protected override void OnUpdate()
        {
            NativeQueue<Time2WorkCitizenTravelPurposeSystem.Arrive> nativeQueue = new NativeQueue<Time2WorkCitizenTravelPurposeSystem.Arrive>((AllocatorManager.AllocatorHandle)Allocator.TempJob);
            
            
            this.__TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            
            
            this.__TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            
            
            this.__TypeHandle.__Game_Buildings_EmergencyShelter_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            
            
            this.__TypeHandle.__Game_Buildings_DeathcareFacility_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            
            
            this.__TypeHandle.__Game_Buildings_Hospital_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            
            
            this.__TypeHandle.__Game_Buildings_Prison_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            
            
            this.__TypeHandle.__Game_Buildings_PoliceStation_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            
            
            this.__TypeHandle.__Game_Citizens_Worker_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            
            
            this.__TypeHandle.__Game_Citizens_Student_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            
            
            this.__TypeHandle.__Game_Companies_WorkProvider_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            
            
            this.__TypeHandle.__Game_Buildings_School_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            
            
            this.__TypeHandle.__Game_Buildings_Building_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            
            
            this.__TypeHandle.__Game_Citizens_Arrived_RO_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            
            
            this.__TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            
            
            this.__TypeHandle.__Game_Citizens_TravelPurpose_RW_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            
            
            this.__TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            
            
            this.__TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref this.CheckedStateRef);

            Time2WorkCitizenTravelPurposeSystem.CitizenArriveJob jobData = new Time2WorkCitizenTravelPurposeSystem.CitizenArriveJob()
            {
                m_EntityType = this.__TypeHandle.__Unity_Entities_Entity_TypeHandle,
                m_CurrentBuildingType = this.__TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle,
                m_TravelPurposeType = this.__TypeHandle.__Game_Citizens_TravelPurpose_RW_ComponentTypeHandle,
                m_HealthProblemType = this.__TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentTypeHandle,
                m_ArrivedType = this.__TypeHandle.__Game_Citizens_Arrived_RO_ComponentTypeHandle,
                m_BuildingData = this.__TypeHandle.__Game_Buildings_Building_RO_ComponentLookup,
                m_Schools = this.__TypeHandle.__Game_Buildings_School_RO_ComponentLookup,
                m_WorkProviders = this.__TypeHandle.__Game_Companies_WorkProvider_RO_ComponentLookup,
                m_Students = this.__TypeHandle.__Game_Citizens_Student_RO_ComponentLookup,
                m_Workers = this.__TypeHandle.__Game_Citizens_Worker_RO_ComponentLookup,
                m_PoliceStationData = this.__TypeHandle.__Game_Buildings_PoliceStation_RO_ComponentLookup,
                m_PrisonData = this.__TypeHandle.__Game_Buildings_Prison_RO_ComponentLookup,
                m_HospitalData = this.__TypeHandle.__Game_Buildings_Hospital_RO_ComponentLookup,
                m_DeathcareFacilityData = this.__TypeHandle.__Game_Buildings_DeathcareFacility_RO_ComponentLookup,
                m_EmergencyShelterData = this.__TypeHandle.__Game_Buildings_EmergencyShelter_RO_ComponentLookup,
                m_Citizens = this.__TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup,
                m_HealthProblems = this.__TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentLookup,
                m_EconomyParameters = this.m_EconomyParameterGroup.GetSingleton<EconomyParameterData>(),
                m_CommandBuffer = this.m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
                m_ArriveQueue = nativeQueue.AsParallelWriter(),
                m_NormalizedTime = this.m_TimeSystem.normalizedTime,
                lunch_break_pct = Mod.m_Setting.lunch_break_percentage,
                school_start_time = (int)Mod.m_Setting.school_start_time,
                school_end_time = (int)Mod.m_Setting.school_end_time,
                work_start_time = (float)Mod.m_Setting.work_start_time,
                work_end_time = (float)Mod.m_Setting.work_end_time,
                delayFactor = (float)(Mod.m_Setting.delay_factor) / 100
        };
            
            this.Dependency = jobData.ScheduleParallel<Time2WorkCitizenTravelPurposeSystem.CitizenArriveJob>(this.m_ArrivedGroup, this.Dependency);
            
            this.m_EndFrameBarrier.AddJobHandleForProducer(this.Dependency);
            
            
            this.__TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup.Update(ref this.CheckedStateRef);
            
            
            this.__TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            
            
            this.__TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            
            
            this.__TypeHandle.__Game_Citizens_Household_RW_ComponentLookup.Update(ref this.CheckedStateRef);
            
            
            this.__TypeHandle.__Game_Buildings_Occupant_RW_BufferLookup.Update(ref this.CheckedStateRef);
            
            
            this.__TypeHandle.__Game_Buildings_Patient_RW_BufferLookup.Update(ref this.CheckedStateRef);
            
            
            this.__TypeHandle.__Game_Buildings_CitizenPresence_RW_ComponentLookup.Update(ref this.CheckedStateRef);
            JobHandle deps;
            
            JobHandle jobHandle1 = new Time2WorkCitizenTravelPurposeSystem.ArriveJob()
            {
                m_CitizenPresenceData = this.__TypeHandle.__Game_Buildings_CitizenPresence_RW_ComponentLookup,
                m_Patients = this.__TypeHandle.__Game_Buildings_Patient_RW_BufferLookup,
                m_Occupants = this.__TypeHandle.__Game_Buildings_Occupant_RW_BufferLookup,
                m_Households = this.__TypeHandle.__Game_Citizens_Household_RW_ComponentLookup,
                m_HouseholdMembers = this.__TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentLookup,
                m_PropertyRenters = this.__TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup,
                m_HouseholdCitizens = this.__TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup,
                m_StatisticsQueue = this.m_CityStatisticsSystem.GetStatisticsEventQueue(out deps),
                m_ArriveQueue = nativeQueue
            }.Schedule<Time2WorkCitizenTravelPurposeSystem.ArriveJob>(JobHandle.CombineDependencies(this.Dependency, deps));
            nativeQueue.Dispose(jobHandle1);
            
            
            this.__TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            
            
            this.__TypeHandle.__Game_Buildings_Building_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            
            
            this.__TypeHandle.__Game_Agents_MovingAway_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            
            
            this.__TypeHandle.__Game_Citizens_Household_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            
            
            this.__TypeHandle.__Game_Citizens_Citizen_RW_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            
            
            this.__TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            
            
            this.__TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            
            
            this.__TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref this.CheckedStateRef);
            JobHandle outJobHandle1;
            JobHandle outJobHandle2;
            
            JobHandle jobHandle2 = new Time2WorkCitizenTravelPurposeSystem.CitizenStuckJob()
            {
                m_EntityType = this.__TypeHandle.__Unity_Entities_Entity_TypeHandle,
                m_HouseholdMemberType = this.__TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentTypeHandle,
                m_HealthProblemType = this.__TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentTypeHandle,
                m_CitizenType = this.__TypeHandle.__Game_Citizens_Citizen_RW_ComponentTypeHandle,
                m_Households = this.__TypeHandle.__Game_Citizens_Household_RO_ComponentLookup,
                m_MovingAways = this.__TypeHandle.__Game_Agents_MovingAway_RO_ComponentLookup,
                m_Buildings = this.__TypeHandle.__Game_Buildings_Building_RO_ComponentLookup,
                m_PropertyRenters = this.__TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup,
                m_RandomSeed = RandomSeed.Next(),
                m_ServiceBuildings = this.m_ServiceBuildingQuery.ToEntityListAsync((AllocatorManager.AllocatorHandle)this.World.UpdateAllocator.ToAllocator, out outJobHandle1),
                m_OutsideConnections = this.m_OutsideConnectionQuery.ToEntityListAsync((AllocatorManager.AllocatorHandle)this.World.UpdateAllocator.ToAllocator, out outJobHandle2),
                m_CommandBuffer = this.m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter()
            }.ScheduleParallel<Time2WorkCitizenTravelPurposeSystem.CitizenStuckJob>(this.m_StuckGroup, JobUtils.CombineDependencies(outJobHandle2, outJobHandle1, jobHandle1, this.Dependency));
            
            this.m_EndFrameBarrier.AddJobHandleForProducer(jobHandle2);
            this.Dependency = JobHandle.CombineDependencies(jobHandle1, jobHandle2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void __AssignQueries(ref SystemState state)
        {
        }

        protected override void OnCreateForCompiler()
        {
            base.OnCreateForCompiler();
            this.__AssignQueries(ref this.CheckedStateRef);
            this.__TypeHandle.__AssignHandles(ref this.CheckedStateRef);
        }

        public Time2WorkCitizenTravelPurposeSystem()
        {
        }

        [BurstCompile]
        private struct CitizenArriveJob : IJobChunk
        {
            [ReadOnly]
            public EntityTypeHandle m_EntityType;
            [ReadOnly]
            public ComponentTypeHandle<CurrentBuilding> m_CurrentBuildingType;
            public ComponentTypeHandle<TravelPurpose> m_TravelPurposeType;
            [ReadOnly]
            public ComponentTypeHandle<HealthProblem> m_HealthProblemType;
            [ReadOnly]
            public ComponentTypeHandle<Arrived> m_ArrivedType;
            [ReadOnly]
            public ComponentLookup<Worker> m_Workers;
            [ReadOnly]
            public ComponentLookup<Game.Citizens.Student> m_Students;
            [ReadOnly]
            public ComponentLookup<WorkProvider> m_WorkProviders;
            [ReadOnly]
            public ComponentLookup<Building> m_BuildingData;
            [ReadOnly]
            public ComponentLookup<Game.Buildings.School> m_Schools;
            [ReadOnly]
            public ComponentLookup<Game.Buildings.PoliceStation> m_PoliceStationData;
            [ReadOnly]
            public ComponentLookup<Game.Buildings.Prison> m_PrisonData;
            [ReadOnly]
            public ComponentLookup<Game.Buildings.Hospital> m_HospitalData;
            [ReadOnly]
            public ComponentLookup<Game.Buildings.DeathcareFacility> m_DeathcareFacilityData;
            [ReadOnly]
            public ComponentLookup<Game.Buildings.EmergencyShelter> m_EmergencyShelterData;
            [ReadOnly]
            public ComponentLookup<Citizen> m_Citizens;
            [ReadOnly]
            public ComponentLookup<HealthProblem> m_HealthProblems;
            public EntityCommandBuffer.ParallelWriter m_CommandBuffer;
            public NativeQueue<Time2WorkCitizenTravelPurposeSystem.Arrive>.ParallelWriter m_ArriveQueue;
            public EconomyParameterData m_EconomyParameters;
            public float m_NormalizedTime;
            public int lunch_break_pct;
            public int school_start_time;
            public int school_end_time;
            public float work_start_time;
            public float work_end_time;
            public float delayFactor;

            public void Execute(
              in ArchetypeChunk chunk,
              int unfilteredChunkIndex,
              bool useEnabledMask,
              in v128 chunkEnabledMask)
            {
                
                NativeArray<Entity> nativeArray1 = chunk.GetNativeArray(this.m_EntityType);
                
                NativeArray<TravelPurpose> nativeArray2 = chunk.GetNativeArray<TravelPurpose>(ref this.m_TravelPurposeType);
                
                NativeArray<CurrentBuilding> nativeArray3 = chunk.GetNativeArray<CurrentBuilding>(ref this.m_CurrentBuildingType);
                
                bool flag1 = chunk.Has<HealthProblem>(ref this.m_HealthProblemType);
                for (int index = 0; index < chunk.Count; ++index)
                {
                    
                    bool flag2 = chunk.IsComponentEnabled<Arrived>(ref this.m_ArrivedType, index);
                    Entity entity = nativeArray1[index];
                    TravelPurpose travelPurpose = nativeArray2[index];
                    
                    if (flag1 && CitizenUtils.IsDead(entity, ref this.m_HealthProblems) && travelPurpose.m_Purpose != Game.Citizens.Purpose.Deathcare && travelPurpose.m_Purpose != Game.Citizens.Purpose.InDeathcare && travelPurpose.m_Purpose != Game.Citizens.Purpose.Hospital && travelPurpose.m_Purpose != Game.Citizens.Purpose.InHospital)
                    {
                        
                        this.m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, entity);
                    }
                    else if (travelPurpose.m_Purpose == Game.Citizens.Purpose.Sleeping)
                    {
                        
                        Citizen citizen = this.m_Citizens[entity];
                        if (!Time2WorkCitizenBehaviorSystem.IsSleepTime(entity, citizen, ref this.m_EconomyParameters, this.m_NormalizedTime, ref this.m_Workers, ref this.m_Students, lunch_break_pct, school_start_time, school_end_time, work_start_time, work_end_time, delayFactor))
                        {
                            
                            this.m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, entity);
                            
                            if (nativeArray3.Length != 0 && this.m_BuildingData.HasComponent(nativeArray3[index].m_CurrentBuilding))
                            {
                                this.m_ArriveQueue.Enqueue(new Time2WorkCitizenTravelPurposeSystem.Arrive(entity, nativeArray3[index].m_CurrentBuilding, Time2WorkCitizenTravelPurposeSystem.ArriveType.WakeUp));
                            }
                        }
                    }
                    else if (flag2)
                    {
                        
                        this.m_CommandBuffer.SetComponentEnabled<Arrived>(unfilteredChunkIndex, entity, false);
                        switch (travelPurpose.m_Purpose)
                        {
                            case Game.Citizens.Purpose.None:
                            case Game.Citizens.Purpose.Shopping:
                            case Game.Citizens.Purpose.Leisure:
                            case Game.Citizens.Purpose.Exporting:
                            case Game.Citizens.Purpose.MovingAway:
                            case Game.Citizens.Purpose.Safety:
                            case Game.Citizens.Purpose.Escape:
                            case Game.Citizens.Purpose.Traveling:
                            case Game.Citizens.Purpose.SendMail:
                            case Game.Citizens.Purpose.Disappear:
                            case Game.Citizens.Purpose.WaitingHome:
                            case Game.Citizens.Purpose.PathFailed:
                                
                                this.m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, entity);
                                continue;
                            case Game.Citizens.Purpose.GoingHome:
                                
                                if (nativeArray3.Length != 0 && this.m_BuildingData.HasComponent(nativeArray3[index].m_CurrentBuilding))
                                {
                                    this.m_ArriveQueue.Enqueue(new Time2WorkCitizenTravelPurposeSystem.Arrive(entity, nativeArray3[index].m_CurrentBuilding, Time2WorkCitizenTravelPurposeSystem.ArriveType.Resident));
                                }
                                
                                this.m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, entity);
                                continue;
                            case Game.Citizens.Purpose.GoingToWork:
                                
                                if (nativeArray3.Length != 0 && this.m_BuildingData.HasComponent(nativeArray3[index].m_CurrentBuilding))
                                {
                                    this.m_ArriveQueue.Enqueue(new Time2WorkCitizenTravelPurposeSystem.Arrive(entity, nativeArray3[index].m_CurrentBuilding, Time2WorkCitizenTravelPurposeSystem.ArriveType.Worker));
                                }
                                
                                if (this.m_Workers.HasComponent(entity))
                                {
                                    
                                    
                                    if (this.m_WorkProviders.HasComponent(this.m_Workers[entity].m_Workplace))
                                    {
                                        travelPurpose.m_Purpose = Game.Citizens.Purpose.Working;
                                        nativeArray2[index] = travelPurpose;
                                        continue;
                                    }
                                    
                                    this.m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, entity);
                                    continue;
                                }
                                continue;
                            case Game.Citizens.Purpose.GoingToSchool:
                                
                                if (this.m_Students.HasComponent(entity))
                                {
                                    
                                    
                                    if (this.m_Schools.HasComponent(this.m_Students[entity].m_School))
                                    {
                                        travelPurpose.m_Purpose = Game.Citizens.Purpose.Studying;
                                        nativeArray2[index] = travelPurpose;
                                        continue;
                                    }
                                    
                                    this.m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, entity);
                                    continue;
                                }
                                
                                this.m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, entity);
                                continue;
                            case Game.Citizens.Purpose.Hospital:
                                
                                if (nativeArray3.Length != 0 && this.m_HospitalData.HasComponent(nativeArray3[index].m_CurrentBuilding))
                                {
                                    travelPurpose.m_Purpose = Game.Citizens.Purpose.InHospital;
                                    nativeArray2[index] = travelPurpose;
                                    this.m_ArriveQueue.Enqueue(new Time2WorkCitizenTravelPurposeSystem.Arrive(entity, nativeArray3[index].m_CurrentBuilding, Time2WorkCitizenTravelPurposeSystem.ArriveType.Patient));
                                    continue;
                                }
                                
                                this.m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, entity);
                                continue;
                            case Game.Citizens.Purpose.EmergencyShelter:
                                
                                if (nativeArray3.Length != 0 && this.m_EmergencyShelterData.HasComponent(nativeArray3[index].m_CurrentBuilding))
                                {
                                    travelPurpose.m_Purpose = Game.Citizens.Purpose.InEmergencyShelter;
                                    nativeArray2[index] = travelPurpose;
                                    this.m_ArriveQueue.Enqueue(new Time2WorkCitizenTravelPurposeSystem.Arrive(entity, nativeArray3[index].m_CurrentBuilding, Time2WorkCitizenTravelPurposeSystem.ArriveType.Occupant));
                                    continue;
                                }
                                
                                this.m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, entity);
                                continue;
                            case Game.Citizens.Purpose.GoingToJail:
                                
                                if (nativeArray3.Length != 0 && this.m_PoliceStationData.HasComponent(nativeArray3[index].m_CurrentBuilding))
                                {
                                    travelPurpose.m_Purpose = Game.Citizens.Purpose.InJail;
                                    nativeArray2[index] = travelPurpose;
                                    this.m_ArriveQueue.Enqueue(new Time2WorkCitizenTravelPurposeSystem.Arrive(entity, nativeArray3[index].m_CurrentBuilding, Time2WorkCitizenTravelPurposeSystem.ArriveType.Occupant));
                                    continue;
                                }
                                
                                this.m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, entity);
                                continue;
                            case Game.Citizens.Purpose.GoingToPrison:
                                
                                if (nativeArray3.Length != 0 && this.m_PrisonData.HasComponent(nativeArray3[index].m_CurrentBuilding))
                                {
                                    travelPurpose.m_Purpose = Game.Citizens.Purpose.InPrison;
                                    nativeArray2[index] = travelPurpose;
                                    this.m_ArriveQueue.Enqueue(new Time2WorkCitizenTravelPurposeSystem.Arrive(entity, nativeArray3[index].m_CurrentBuilding, Time2WorkCitizenTravelPurposeSystem.ArriveType.Occupant));
                                    continue;
                                }
                                
                                this.m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, entity);
                                continue;
                            case Game.Citizens.Purpose.Deathcare:
                                
                                if (nativeArray3.Length != 0 && this.m_DeathcareFacilityData.HasComponent(nativeArray3[index].m_CurrentBuilding))
                                {
                                    travelPurpose.m_Purpose = Game.Citizens.Purpose.InDeathcare;
                                    nativeArray2[index] = travelPurpose;
                                    this.m_ArriveQueue.Enqueue(new Time2WorkCitizenTravelPurposeSystem.Arrive(entity, nativeArray3[index].m_CurrentBuilding, Time2WorkCitizenTravelPurposeSystem.ArriveType.Patient));
                                    continue;
                                }
                                
                                this.m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, entity);
                                continue;
                            default:
                                continue;
                        }
                    }
                }
            }

            void IJobChunk.Execute(
              in ArchetypeChunk chunk,
              int unfilteredChunkIndex,
              bool useEnabledMask,
              in v128 chunkEnabledMask)
            {
                this.Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
            }
        }

        private struct Arrive
        {
            public Entity m_Citizen;
            public Entity m_Target;
            public Time2WorkCitizenTravelPurposeSystem.ArriveType m_Type;

            public Arrive(Entity citizen, Entity target, Time2WorkCitizenTravelPurposeSystem.ArriveType type)
            {
                
                this.m_Citizen = citizen;
                
                this.m_Target = target;
                
                this.m_Type = type;
            }
        }

        private enum ArriveType
        {
            Patient,
            Occupant,
            Resident,
            Worker,
            WakeUp,
        }

        [BurstCompile]
        private struct ArriveJob : IJob
        {
            public ComponentLookup<CitizenPresence> m_CitizenPresenceData;
            public BufferLookup<Patient> m_Patients;
            public BufferLookup<Occupant> m_Occupants;
            public ComponentLookup<Household> m_Households;
            public BufferLookup<HouseholdCitizen> m_HouseholdCitizens;
            [ReadOnly]
            public ComponentLookup<PropertyRenter> m_PropertyRenters;
            [ReadOnly]
            public ComponentLookup<HouseholdMember> m_HouseholdMembers;
            public NativeQueue<StatisticsEvent> m_StatisticsQueue;
            public NativeQueue<Time2WorkCitizenTravelPurposeSystem.Arrive> m_ArriveQueue;

            private void SetPresent(Time2WorkCitizenTravelPurposeSystem.Arrive arrive)
            {

                if (!this.m_CitizenPresenceData.HasComponent(arrive.m_Target))
                    return;

                CitizenPresence citizenPresence = this.m_CitizenPresenceData[arrive.m_Target];
                citizenPresence.m_Delta = (sbyte)math.min((int)sbyte.MaxValue, (int)citizenPresence.m_Delta + 1);
                
                
                this.m_CitizenPresenceData[arrive.m_Target] = citizenPresence;
            }

            public void Execute()
            {
                
                int count = this.m_ArriveQueue.Count;
                for (int index = 0; index < count; ++index)
                {
                    Time2WorkCitizenTravelPurposeSystem.Arrive arrive = this.m_ArriveQueue.Dequeue();
                    Time2WorkCitizenTravelPurposeSystem.ArriveType type = arrive.m_Type;
                    switch (type)
                    {
                        case Time2WorkCitizenTravelPurposeSystem.ArriveType.Patient:

                            if (this.m_Patients.HasBuffer(arrive.m_Target))
                            {
                                CollectionUtils.TryAddUniqueValue<Patient>(this.m_Patients[arrive.m_Target], new Patient(arrive.m_Citizen));
                                break;
                            }
                            break;
                        case Time2WorkCitizenTravelPurposeSystem.ArriveType.Occupant:
                            
                            
                            if (this.m_Occupants.HasBuffer(arrive.m_Target))
                            {
                                CollectionUtils.TryAddUniqueValue<Occupant>(this.m_Occupants[arrive.m_Target], new Occupant(arrive.m_Citizen));
                                break;
                            }
                            break;
                        case Time2WorkCitizenTravelPurposeSystem.ArriveType.Resident:
   
                            Entity household1 = this.m_HouseholdMembers[arrive.m_Citizen].m_Household;

                            if (this.m_PropertyRenters.HasComponent(household1) && this.m_PropertyRenters[household1].m_Property == arrive.m_Target)
                            {
                                
                                Household household2 = this.m_Households[household1];
                                
                                if (this.m_HouseholdCitizens.HasBuffer(household1) && (household2.m_Flags & HouseholdFlags.MovedIn) == HouseholdFlags.None)
                                {
                                    this.m_StatisticsQueue.Enqueue(new StatisticsEvent()
                                    {
                                        m_Statistic = StatisticType.CitizensMovedIn,
                                        m_Change = (float)this.m_HouseholdCitizens[household1].Length
                                    });
                                }
                                household2.m_Flags |= HouseholdFlags.MovedIn;
                                
                                this.m_Households[household1] = household2;
                            }
                            this.SetPresent(arrive);
                            break;
                        case Time2WorkCitizenTravelPurposeSystem.ArriveType.Worker:
                        case Time2WorkCitizenTravelPurposeSystem.ArriveType.WakeUp:
                            this.SetPresent(arrive);
                            break;
                    }
                }
            }
        }

        [BurstCompile]
        private struct CitizenStuckJob : IJobChunk
        {
            [ReadOnly]
            public EntityTypeHandle m_EntityType;
            [ReadOnly]
            public ComponentTypeHandle<HouseholdMember> m_HouseholdMemberType;
            [ReadOnly]
            public ComponentTypeHandle<HealthProblem> m_HealthProblemType;
            public ComponentTypeHandle<Citizen> m_CitizenType;
            [ReadOnly]
            public ComponentLookup<Household> m_Households;
            [ReadOnly]
            public ComponentLookup<MovingAway> m_MovingAways;
            [ReadOnly]
            public ComponentLookup<PropertyRenter> m_PropertyRenters;
            [ReadOnly]
            public ComponentLookup<Building> m_Buildings;
            [ReadOnly]
            public NativeList<Entity> m_OutsideConnections;
            [ReadOnly]
            public NativeList<Entity> m_ServiceBuildings;
            public EntityCommandBuffer.ParallelWriter m_CommandBuffer;
            public RandomSeed m_RandomSeed;

            public void Execute(
              in ArchetypeChunk chunk,
              int unfilteredChunkIndex,
              bool useEnabledMask,
              in v128 chunkEnabledMask)
            {
                
                NativeArray<Entity> nativeArray1 = chunk.GetNativeArray(this.m_EntityType);
                
                NativeArray<HouseholdMember> nativeArray2 = chunk.GetNativeArray<HouseholdMember>(ref this.m_HouseholdMemberType);
                
                NativeArray<HealthProblem> nativeArray3 = chunk.GetNativeArray<HealthProblem>(ref this.m_HealthProblemType);
                
                NativeArray<Citizen> nativeArray4 = chunk.GetNativeArray<Citizen>(ref this.m_CitizenType);
                
                if (nativeArray2.Length < chunk.Count || this.m_OutsideConnections.Length == 0)
                    return;
                for (int index = 0; index < chunk.Count; ++index)
                {
                    Entity entity1 = nativeArray1[index];
                    Entity household = nativeArray2[index].m_Household;
                    
                    
                    bool flag = (this.m_Households[household].m_Flags & HouseholdFlags.MovedIn) != HouseholdFlags.None && !this.m_MovingAways.HasComponent(household);
                    HealthProblem healthProblem;
                    if (CollectionUtils.TryGet<HealthProblem>(nativeArray3, index, out healthProblem) && (healthProblem.m_Flags & HealthProblemFlags.Dead) != HealthProblemFlags.None)
                    {
                        
                        this.m_CommandBuffer.AddComponent<Deleted>(unfilteredChunkIndex, nativeArray1[index]);
                    }
                    else
                    {
                        Entity entity2 = Entity.Null;
                        
                        Random random = this.m_RandomSeed.GetRandom((1 + index) * (entity1.Index + 1));
                        if (flag)
                        {
                            
                            if (this.m_PropertyRenters.HasComponent(household))
                            {
                                
                                entity2 = this.m_PropertyRenters[household].m_Property;
                            }
                            
                            if (entity2 == Entity.Null && this.m_ServiceBuildings.Length > 0)
                            {
                                int num = 0;
                                do
                                {
                                    ++num;
                                    entity2 = this.m_ServiceBuildings[random.NextInt(this.m_ServiceBuildings.Length)];
                                }
                                while ((!this.m_Buildings.HasComponent(entity2) || this.m_Buildings[entity2].m_RoadEdge == Entity.Null) && num < 10);
                            }
                            
                            
                            if (!this.m_Buildings.HasComponent(entity2) || this.m_Buildings[entity2].m_RoadEdge == Entity.Null)
                            {
                                
                                this.m_CommandBuffer.AddComponent<Deleted>(unfilteredChunkIndex, nativeArray1[index]);
                            }
                        }
                        else
                        {
                            entity2 = this.m_OutsideConnections[random.NextInt(this.m_OutsideConnections.Length)];
                        }
                        if (entity2 != Entity.Null)
                        {
                            
                            this.m_CommandBuffer.AddComponent<CurrentBuilding>(unfilteredChunkIndex, nativeArray1[index], new CurrentBuilding()
                            {
                                m_CurrentBuilding = entity2
                            });
                            
                            this.m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, nativeArray1[index]);
                            Citizen citizen = nativeArray4[index];
                            citizen.m_PenaltyCounter = byte.MaxValue;
                            nativeArray4[index] = citizen;
                        }
                    }
                }
            }

            void IJobChunk.Execute(
              in ArchetypeChunk chunk,
              int unfilteredChunkIndex,
              bool useEnabledMask,
              in v128 chunkEnabledMask)
            {
                // ISSUE: reference to a compiler-generated method
                this.Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
            }
        }

        private struct TypeHandle
        {
            [ReadOnly]
            public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;
            [ReadOnly]
            public ComponentTypeHandle<CurrentBuilding> __Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle;
            public ComponentTypeHandle<TravelPurpose> __Game_Citizens_TravelPurpose_RW_ComponentTypeHandle;
            [ReadOnly]
            public ComponentTypeHandle<HealthProblem> __Game_Citizens_HealthProblem_RO_ComponentTypeHandle;
            [ReadOnly]
            public ComponentTypeHandle<Arrived> __Game_Citizens_Arrived_RO_ComponentTypeHandle;
            [ReadOnly]
            public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Game.Buildings.School> __Game_Buildings_School_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<WorkProvider> __Game_Companies_WorkProvider_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Game.Citizens.Student> __Game_Citizens_Student_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Worker> __Game_Citizens_Worker_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Game.Buildings.PoliceStation> __Game_Buildings_PoliceStation_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Game.Buildings.Prison> __Game_Buildings_Prison_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Game.Buildings.Hospital> __Game_Buildings_Hospital_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Game.Buildings.DeathcareFacility> __Game_Buildings_DeathcareFacility_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Game.Buildings.EmergencyShelter> __Game_Buildings_EmergencyShelter_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Citizen> __Game_Citizens_Citizen_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<HealthProblem> __Game_Citizens_HealthProblem_RO_ComponentLookup;
            public ComponentLookup<CitizenPresence> __Game_Buildings_CitizenPresence_RW_ComponentLookup;
            public BufferLookup<Patient> __Game_Buildings_Patient_RW_BufferLookup;
            public BufferLookup<Occupant> __Game_Buildings_Occupant_RW_BufferLookup;
            public ComponentLookup<Household> __Game_Citizens_Household_RW_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<HouseholdMember> __Game_Citizens_HouseholdMember_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;
            [ReadOnly]
            public BufferLookup<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferLookup;
            [ReadOnly]
            public ComponentTypeHandle<HouseholdMember> __Game_Citizens_HouseholdMember_RO_ComponentTypeHandle;
            public ComponentTypeHandle<Citizen> __Game_Citizens_Citizen_RW_ComponentTypeHandle;
            [ReadOnly]
            public ComponentLookup<Household> __Game_Citizens_Household_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<MovingAway> __Game_Agents_MovingAway_RO_ComponentLookup;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void __AssignHandles(ref SystemState state)
            {
                
                this.__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
                
                this.__Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CurrentBuilding>(true);
                
                this.__Game_Citizens_TravelPurpose_RW_ComponentTypeHandle = state.GetComponentTypeHandle<TravelPurpose>();
                
                this.__Game_Citizens_HealthProblem_RO_ComponentTypeHandle = state.GetComponentTypeHandle<HealthProblem>(true);
                
                this.__Game_Citizens_Arrived_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Arrived>(true);
                
                this.__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(true);
                
                this.__Game_Buildings_School_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.School>(true);
                
                this.__Game_Companies_WorkProvider_RO_ComponentLookup = state.GetComponentLookup<WorkProvider>(true);
                
                this.__Game_Citizens_Student_RO_ComponentLookup = state.GetComponentLookup<Game.Citizens.Student>(true);
                
                this.__Game_Citizens_Worker_RO_ComponentLookup = state.GetComponentLookup<Worker>(true);
                
                this.__Game_Buildings_PoliceStation_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.PoliceStation>(true);
                
                this.__Game_Buildings_Prison_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.Prison>(true);
                
                this.__Game_Buildings_Hospital_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.Hospital>(true);
                
                this.__Game_Buildings_DeathcareFacility_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.DeathcareFacility>(true);
                
                this.__Game_Buildings_EmergencyShelter_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.EmergencyShelter>(true);
                
                this.__Game_Citizens_Citizen_RO_ComponentLookup = state.GetComponentLookup<Citizen>(true);
                
                this.__Game_Citizens_HealthProblem_RO_ComponentLookup = state.GetComponentLookup<HealthProblem>(true);
                
                this.__Game_Buildings_CitizenPresence_RW_ComponentLookup = state.GetComponentLookup<CitizenPresence>();
                
                this.__Game_Buildings_Patient_RW_BufferLookup = state.GetBufferLookup<Patient>();
                
                this.__Game_Buildings_Occupant_RW_BufferLookup = state.GetBufferLookup<Occupant>();
                
                this.__Game_Citizens_Household_RW_ComponentLookup = state.GetComponentLookup<Household>();
                
                this.__Game_Citizens_HouseholdMember_RO_ComponentLookup = state.GetComponentLookup<HouseholdMember>(true);
                
                this.__Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(true);
                
                this.__Game_Citizens_HouseholdCitizen_RO_BufferLookup = state.GetBufferLookup<HouseholdCitizen>(true);
                
                this.__Game_Citizens_HouseholdMember_RO_ComponentTypeHandle = state.GetComponentTypeHandle<HouseholdMember>(true);
                
                this.__Game_Citizens_Citizen_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Citizen>();
                
                this.__Game_Citizens_Household_RO_ComponentLookup = state.GetComponentLookup<Household>(true);
                
                this.__Game_Agents_MovingAway_RO_ComponentLookup = state.GetComponentLookup<MovingAway>(true);
            }
        }
    }
}
