using Game.Agents;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Events;
using Game.Pathfind;
using Game.Prefabs;
using Game.Tools;
using Game.Vehicles;
using Game.Simulation;
using Game;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using System;
using System.Data;

namespace Time2Work
{
    public partial class Time2WorkCitizenBehaviorSystem : GameSystemBase
    {
        private JobHandle m_CarReserveWriters;
        private EntityQuery m_CitizenQuery;
        private EntityQuery m_OutsideConnectionQuery;
        private EntityQuery m_EconomyParameterQuery;
        private EntityQuery m_LeisureParameterQuery;
        private EntityQuery m_TimeDataQuery;
        private EntityQuery m_PopulationQuery;
        private SimulationSystem m_SimulationSystem;
        private TimeSystem m_TimeSystem;
        private EndFrameBarrier m_EndFrameBarrier;
        private CityStatisticsSystem m_CityStatisticsSystem;
        private EntityArchetype m_HouseholdArchetype;
        private NativeQueue<Entity> m_CarReserverQueue;
        private NativeQueue<Entity>.ParallelWriter m_ParallelCarReserverQueue;
        private Time2WorkCitizenBehaviorSystem.TypeHandle __TypeHandle;
        private Setting.DTSimulationEnum m_daytype;

        public override int GetUpdateInterval(SystemUpdatePhase phase) => 16;

        public override int GetUpdateOffset(SystemUpdatePhase phase) => 11;

        public static float2 GetSleepTime(
          Entity entity,
          Citizen citizen,
          ref EconomyParameterData economyParameters,
          ref ComponentLookup<Worker> workers,
          ref ComponentLookup<Game.Citizens.Student> students,
          int lunch_break_pct,
          int school_start_time,
          int school_end_time,
          float work_start_time,
          float work_end_time,
          float delayFactor)
        {
            int age = (int)citizen.GetAge();
            float2 float2_1 = new float2(0.875f, 0.21f);
            float num = float2_1.y - float2_1.x;
            Unity.Mathematics.Random pseudoRandom = citizen.GetPseudoRandom(CitizenPseudoRandom.SleepOffset);
            float2 x1 = float2_1 + (float)(GaussianRandom.NextGaussianDouble(pseudoRandom)*0.1f) + 0.1f;

            if (age == 3)
                x1 -= 0.05f;
            if (age == 0)
                x1 -= 0.1f;
            if (age == 1)
                x1 += 0.05f;
            float2 x2 = math.frac(x1);
            float2 float2_2;
            if (workers.HasComponent(entity))
            {
                float2_2 = Time2WorkWorkerSystem.GetTimeToWork(citizen, workers[entity], ref economyParameters, true, lunch_break_pct, work_start_time, work_end_time, delayFactor);
            }
            else
            {
                if (!students.HasComponent(entity))
                    return x2;
                float2_2 = Time2WorkStudentSystem.GetTimeToStudy(citizen, students[entity], ref economyParameters, school_start_time, school_end_time);
            }
            if ((double)float2_2.x < (double)float2_2.y)
            {
                if ((double)x2.x > (double)x2.y && (double)float2_2.y > (double)x2.x)
                    x2 += float2_2.y - x2.x;
                else if ((double)x2.y > (double)float2_2.x)
                    x2 += (float)(1.0 - ((double)x2.y - (double)float2_2.x));
            }
            else
                x2 = new float2(float2_2.y, float2_2.y + num);
            x2 = math.frac(x2);
            return x2;
        }

        public static bool IsSleepTime(
          Entity entity,
          Citizen citizen,
          ref EconomyParameterData economyParameters,
          float normalizedTime,
          ref ComponentLookup<Worker> workers,
          ref ComponentLookup<Game.Citizens.Student> students,
          int lunch_break_pct,
          int school_start_time,
          int school_end_time,
          float work_start_time,
          float work_end_time,
          float delayFactor)
        {
            float2 sleepTime = Time2WorkCitizenBehaviorSystem.GetSleepTime(entity, citizen, ref economyParameters, ref workers, ref students, lunch_break_pct, school_start_time, school_end_time, work_start_time, work_end_time, delayFactor);
            return (double)sleepTime.y < (double)sleepTime.x ? (double)normalizedTime > (double)sleepTime.x || (double)normalizedTime < (double)sleepTime.y : (double)normalizedTime > (double)sleepTime.x && (double)normalizedTime < (double)sleepTime.y;
        }

        public NativeQueue<Entity>.ParallelWriter GetCarReserverQueue(out JobHandle deps)
        {
            deps = this.m_CarReserveWriters;
            return this.m_ParallelCarReserverQueue;
        }

        public void AddCarReserveWriter(JobHandle writer)
        {
            this.m_CarReserveWriters = JobHandle.CombineDependencies(this.m_CarReserveWriters, writer);
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            this.m_SimulationSystem = this.World.GetOrCreateSystemManaged<SimulationSystem>();
            this.m_TimeSystem = this.World.GetOrCreateSystemManaged<TimeSystem>();
            this.m_EndFrameBarrier = this.World.GetOrCreateSystemManaged<EndFrameBarrier>();
            this.m_CityStatisticsSystem = this.World.GetOrCreateSystemManaged<CityStatisticsSystem>();
            this.m_CarReserverQueue = new NativeQueue<Entity>((AllocatorManager.AllocatorHandle)Allocator.Persistent);
            this.m_ParallelCarReserverQueue = this.m_CarReserverQueue.AsParallelWriter();
            this.m_EconomyParameterQuery = this.GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
            this.m_LeisureParameterQuery = this.GetEntityQuery(ComponentType.ReadOnly<LeisureParametersData>());
            this.m_PopulationQuery = this.GetEntityQuery(ComponentType.ReadOnly<Population>());
            this.m_CitizenQuery = this.GetEntityQuery(ComponentType.ReadWrite<Citizen>(), ComponentType.Exclude<TravelPurpose>(), ComponentType.Exclude<ResourceBuyer>(), ComponentType.ReadOnly<CurrentBuilding>(), ComponentType.ReadOnly<HouseholdMember>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
            this.m_OutsideConnectionQuery = this.GetEntityQuery(ComponentType.ReadOnly<Game.Objects.OutsideConnection>(), ComponentType.Exclude<Game.Objects.ElectricityOutsideConnection>(), ComponentType.Exclude<Game.Objects.WaterPipeOutsideConnection>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
            this.m_TimeDataQuery = this.GetEntityQuery(ComponentType.ReadOnly<TimeData>());
            this.m_HouseholdArchetype = this.World.EntityManager.CreateArchetype(ComponentType.ReadWrite<Household>(), ComponentType.ReadWrite<HouseholdNeed>(), ComponentType.ReadWrite<HouseholdCitizen>(), ComponentType.ReadWrite<TaxPayer>(), ComponentType.ReadWrite<Game.Economy.Resources>(), ComponentType.ReadWrite<UpdateFrame>(), ComponentType.ReadWrite<Created>());
            this.RequireForUpdate(this.m_CitizenQuery);
            this.RequireForUpdate(this.m_EconomyParameterQuery);
            this.RequireForUpdate(this.m_LeisureParameterQuery);
            this.RequireForUpdate(this.m_TimeDataQuery);
            this.RequireForUpdate(this.m_PopulationQuery);
            this.m_daytype = WeekSystem.currentDayOfTheWeek;
        }

        protected override void OnDestroy()
        {
            this.m_CarReserverQueue.Dispose();
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            uint frameWithInterval = SimulationUtils.GetUpdateFrameWithInterval(this.m_SimulationSystem.frameIndex, (uint)this.GetUpdateInterval(SystemUpdatePhase.GameSimulation), 16);
            NativeQueue<Entity> nativeQueue1 = new NativeQueue<Entity>((AllocatorManager.AllocatorHandle)Allocator.TempJob);
            NativeQueue<Entity> nativeQueue2 = new NativeQueue<Entity>((AllocatorManager.AllocatorHandle)Allocator.TempJob);
            this.__TypeHandle.__Game_Citizens_CommuterHousehold_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Vehicles_OwnedVehicle_RO_BufferLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_OutsideConnectionData_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_City_Population_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_Student_RO_BufferLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_HaveCoordinatedMeetingData_RO_BufferLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_AttendingMeeting_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_CoordinatedMeeting_RW_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_CoordinatedMeetingAttendee_RO_BufferLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Events_InDanger_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Objects_OutsideConnection_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_TouristHousehold_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_Student_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_Worker_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Agents_MovingAway_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Vehicles_PersonalCar_RW_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_CarKeeper_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Objects_Transform_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_Household_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_HouseholdNeed_RW_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_Leisure_RO_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_TripNeeded_RW_BufferTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_Citizen_RW_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.m_daytype = WeekSystem.currentDayOfTheWeek;
            JobHandle outJobHandle;

            Time2WorkCitizenBehaviorSystem.CitizenAITickJob jobData = new Time2WorkCitizenBehaviorSystem.CitizenAITickJob()
            {
                m_CitizenType = this.__TypeHandle.__Game_Citizens_Citizen_RW_ComponentTypeHandle,
                m_CurrentBuildingType = this.__TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle,
                m_EntityType = this.__TypeHandle.__Unity_Entities_Entity_TypeHandle,
                m_HouseholdMemberType = this.__TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentTypeHandle,
                m_UpdateFrameType = this.__TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle,
                m_HealthProblemType = this.__TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentTypeHandle,
                m_TripType = this.__TypeHandle.__Game_Citizens_TripNeeded_RW_BufferTypeHandle,
                m_LeisureType = this.__TypeHandle.__Game_Citizens_Leisure_RO_ComponentTypeHandle,
                m_HouseholdNeeds = this.__TypeHandle.__Game_Citizens_HouseholdNeed_RW_ComponentLookup,
                m_Households = this.__TypeHandle.__Game_Citizens_Household_RO_ComponentLookup,
                m_Properties = this.__TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup,
                m_Transforms = this.__TypeHandle.__Game_Objects_Transform_RO_ComponentLookup,
                m_CarKeepers = this.__TypeHandle.__Game_Citizens_CarKeeper_RO_ComponentLookup,
                m_PersonalCars = this.__TypeHandle.__Game_Vehicles_PersonalCar_RW_ComponentLookup,
                m_MovingAway = this.__TypeHandle.__Game_Agents_MovingAway_RO_ComponentLookup,
                m_Workers = this.__TypeHandle.__Game_Citizens_Worker_RO_ComponentLookup,
                m_Students = this.__TypeHandle.__Game_Citizens_Student_RO_ComponentLookup,
                m_TouristHouseholds = this.__TypeHandle.__Game_Citizens_TouristHousehold_RO_ComponentLookup,
                m_OutsideConnections = this.__TypeHandle.__Game_Objects_OutsideConnection_RO_ComponentLookup,
                m_InDangerData = this.__TypeHandle.__Game_Events_InDanger_RO_ComponentLookup,
                m_Attendees = this.__TypeHandle.__Game_Citizens_CoordinatedMeetingAttendee_RO_BufferLookup,
                m_Meetings = this.__TypeHandle.__Game_Citizens_CoordinatedMeeting_RW_ComponentLookup,
                m_AttendingMeetings = this.__TypeHandle.__Game_Citizens_AttendingMeeting_RO_ComponentLookup,
                m_MeetingDatas = this.__TypeHandle.__Game_Prefabs_HaveCoordinatedMeetingData_RO_BufferLookup,
                m_Prefabs = this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup,
                m_BuildingStudents = this.__TypeHandle.__Game_Buildings_Student_RO_BufferLookup,
                m_PopulationData = this.__TypeHandle.__Game_City_Population_RO_ComponentLookup,
                m_OutsideConnectionDatas = this.__TypeHandle.__Game_Prefabs_OutsideConnectionData_RO_ComponentLookup,
                m_OwnedVehicles = this.__TypeHandle.__Game_Vehicles_OwnedVehicle_RO_BufferLookup,
                m_CommuterHouseholds = this.__TypeHandle.__Game_Citizens_CommuterHousehold_RO_ComponentLookup,
                m_HouseholdArchetype = this.m_HouseholdArchetype,
                m_OutsideConnectionEntities = this.m_OutsideConnectionQuery.ToEntityListAsync((AllocatorManager.AllocatorHandle)Allocator.TempJob, out outJobHandle),
                m_EconomyParameters = this.m_EconomyParameterQuery.GetSingleton<EconomyParameterData>(),
                m_LeisureParameters = this.m_LeisureParameterQuery.GetSingleton<LeisureParametersData>(),
                m_CommandBuffer = this.m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
                m_UpdateFrameIndex = frameWithInterval,
                m_SimulationFrame = this.m_SimulationSystem.frameIndex,
                m_NormalizedTime = this.m_TimeSystem.normalizedTime,
                m_TimeData = this.m_TimeDataQuery.GetSingleton<TimeData>(),
                m_PopulationEntity = this.m_PopulationQuery.GetSingletonEntity(),
                m_CarReserverQueue = this.m_ParallelCarReserverQueue,
                m_MailSenderQueue = nativeQueue1.AsParallelWriter(),
                m_SleepQueue = nativeQueue2.AsParallelWriter(),
                m_RandomSeed = RandomSeed.Next(),
                lunch_break_pct = Mod.m_Setting.lunch_break_percentage,
                offdayprob = WeekSystem.getOffDayProb(),
                school_start_time = (int)Mod.m_Setting.school_start_time,
                school_end_time = (int)Mod.m_Setting.school_end_time,
                work_start_time = (float)Mod.m_Setting.work_start_time,
                work_end_time = (float)Mod.m_Setting.work_end_time,
                school_vanilla_timeoff = Mod.m_Setting.use_school_vanilla_timeoff,
                delayFactor = (float)(Mod.m_Setting.delay_factor) / 100,
                disable_early_shop_leisure = Mod.m_Setting.disable_early_shop_leisure,
                school_offdayprob = WeekSystem.getSchoolOffDayProb()
            };
            JobHandle jobHandle1 = jobData.ScheduleParallel<Time2WorkCitizenBehaviorSystem.CitizenAITickJob>(this.m_CitizenQuery, JobHandle.CombineDependencies(this.m_CarReserveWriters, JobHandle.CombineDependencies(this.Dependency, outJobHandle)));
            jobData.m_OutsideConnectionEntities.Dispose(jobHandle1);
            this.m_EndFrameBarrier.AddJobHandleForProducer(jobHandle1);
            this.AddCarReserveWriter(jobHandle1);
            this.__TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Vehicles_PersonalCar_RW_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Vehicles_OwnedVehicle_RO_BufferLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_CarKeeper_RW_ComponentLookup.Update(ref this.CheckedStateRef);

            JobHandle jobHandle2 = new Time2WorkCitizenBehaviorSystem.CitizenReserveHouseholdCarJob()
            {
                m_CarKeepers = this.__TypeHandle.__Game_Citizens_CarKeeper_RW_ComponentLookup,
                m_HouseholdMembers = this.__TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentLookup,
                m_OwnedVehicles = this.__TypeHandle.__Game_Vehicles_OwnedVehicle_RO_BufferLookup,
                m_PersonalCars = this.__TypeHandle.__Game_Vehicles_PersonalCar_RW_ComponentLookup,
                m_Citizens = this.__TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup,
                m_CommandBuffer = this.m_EndFrameBarrier.CreateCommandBuffer(),
                m_ReserverQueue = this.m_CarReserverQueue
            }.Schedule<Time2WorkCitizenBehaviorSystem.CitizenReserveHouseholdCarJob>(JobHandle.CombineDependencies(jobHandle1, this.m_CarReserveWriters));

            this.m_EndFrameBarrier.AddJobHandleForProducer(jobHandle2);
            this.AddCarReserveWriter(jobHandle2);
            this.__TypeHandle.__Game_Buildings_MailProducer_RW_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_MailSender_RW_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_ServiceObjectData_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_MailAccumulationData_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentLookup.Update(ref this.CheckedStateRef);

            JobHandle jobHandle3 = new Time2WorkCitizenBehaviorSystem.CitizenTryCollectMailJob()
            {
                m_CurrentBuildingData = this.__TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentLookup,
                m_PrefabRefData = this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup,
                m_SpawnableBuildingData = this.__TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup,
                m_MailAccumulationData = this.__TypeHandle.__Game_Prefabs_MailAccumulationData_RO_ComponentLookup,
                m_ServiceObjectData = this.__TypeHandle.__Game_Prefabs_ServiceObjectData_RO_ComponentLookup,
                m_MailSenderData = this.__TypeHandle.__Game_Citizens_MailSender_RW_ComponentLookup,
                m_MailProducerData = this.__TypeHandle.__Game_Buildings_MailProducer_RW_ComponentLookup,
                m_MailSenderQueue = nativeQueue1,
                m_CommandBuffer = this.m_EndFrameBarrier.CreateCommandBuffer()
            }.Schedule<Time2WorkCitizenBehaviorSystem.CitizenTryCollectMailJob>(jobHandle1);

            this.m_EndFrameBarrier.AddJobHandleForProducer(jobHandle3);
            nativeQueue1.Dispose(jobHandle3);
            this.__TypeHandle.__Game_Buildings_CitizenPresence_RW_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            JobHandle jobHandle4 = new Time2WorkCitizenBehaviorSystem.CitizeSleepJob()
            {
                m_CurrentBuildingData = this.__TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentLookup,
                m_CitizenPresenceData = this.__TypeHandle.__Game_Buildings_CitizenPresence_RW_ComponentLookup,
                m_SleepQueue = nativeQueue2
            }.Schedule<Time2WorkCitizenBehaviorSystem.CitizeSleepJob>(jobHandle1);
            nativeQueue2.Dispose(jobHandle4);
            this.Dependency = JobHandle.CombineDependencies(jobHandle2, jobHandle3, jobHandle4);
        }

        private void __AssignQueries(ref SystemState state)
        {
        }

        protected override void OnCreateForCompiler()
        {
            base.OnCreateForCompiler();

            this.__AssignQueries(ref this.CheckedStateRef);
            this.__TypeHandle.__AssignHandles(ref this.CheckedStateRef);
        }

        public Time2WorkCitizenBehaviorSystem()
        {
        }

        [BurstCompile]
        private struct CitizenReserveHouseholdCarJob : IJob
        {
            public ComponentLookup<CarKeeper> m_CarKeepers;
            public ComponentLookup<Game.Vehicles.PersonalCar> m_PersonalCars;
            [ReadOnly]
            public ComponentLookup<HouseholdMember> m_HouseholdMembers;
            [ReadOnly]
            public BufferLookup<OwnedVehicle> m_OwnedVehicles;
            [ReadOnly]
            public ComponentLookup<Citizen> m_Citizens;
            public NativeQueue<Entity> m_ReserverQueue;
            public EntityCommandBuffer m_CommandBuffer;

            public void Execute()
            {
                Entity entity;
                while (this.m_ReserverQueue.TryDequeue(out entity))
                {
                    if (this.m_HouseholdMembers.HasComponent(entity))
                    {
                        Entity household = this.m_HouseholdMembers[entity].m_Household;
                        Entity car = Entity.Null;
                        if (this.m_Citizens[entity].GetAge() != CitizenAge.Child && HouseholdBehaviorSystem.GetFreeCar(household, this.m_OwnedVehicles, this.m_PersonalCars, ref car) && !this.m_CarKeepers.IsComponentEnabled(entity))
                        {
                            this.m_CarKeepers.SetComponentEnabled(entity, true);
                            this.m_CarKeepers[entity] = new CarKeeper()
                            {
                                m_Car = car
                            };
                            Game.Vehicles.PersonalCar personalCar = this.m_PersonalCars[car];
                            personalCar.m_Keeper = entity;

                            this.m_PersonalCars[car] = personalCar;
                        }
                    }
                }
            }
        }

        [BurstCompile]
        private struct CitizenTryCollectMailJob : IJob
        {
            [ReadOnly]
            public ComponentLookup<CurrentBuilding> m_CurrentBuildingData;
            [ReadOnly]
            public ComponentLookup<PrefabRef> m_PrefabRefData;
            [ReadOnly]
            public ComponentLookup<SpawnableBuildingData> m_SpawnableBuildingData;
            [ReadOnly]
            public ComponentLookup<MailAccumulationData> m_MailAccumulationData;
            [ReadOnly]
            public ComponentLookup<ServiceObjectData> m_ServiceObjectData;
            public ComponentLookup<MailSender> m_MailSenderData;
            public ComponentLookup<MailProducer> m_MailProducerData;
            public NativeQueue<Entity> m_MailSenderQueue;
            public EntityCommandBuffer m_CommandBuffer;

            public void Execute()
            {
                Entity entity;

                while (this.m_MailSenderQueue.TryDequeue(out entity))
                {
                    CurrentBuilding componentData1;
                    MailProducer componentData2;

                    if (this.m_CurrentBuildingData.TryGetComponent(entity, out componentData1) && this.m_MailProducerData.TryGetComponent(componentData1.m_CurrentBuilding, out componentData2) && componentData2.m_SendingMail >= (ushort)15 && !this.RequireCollect(this.m_PrefabRefData[componentData1.m_CurrentBuilding].m_Prefab))
                    {

                        bool flag = this.m_MailSenderData.IsComponentEnabled(entity);
                        MailSender mailSender = flag ? this.m_MailSenderData[entity] : new MailSender();
                        int num = math.min((int)componentData2.m_SendingMail, 100 - (int)mailSender.m_Amount);
                        if (num > 0)
                        {
                            mailSender.m_Amount += (ushort)num;
                            componentData2.m_SendingMail -= (ushort)num;
                            this.m_MailProducerData[componentData1.m_CurrentBuilding] = componentData2;
                            if (!flag)
                            {
                                this.m_MailSenderData.SetComponentEnabled(entity, true);
                            }
                            this.m_MailSenderData[entity] = mailSender;
                        }
                    }
                }
            }

            private bool RequireCollect(Entity prefab)
            {

                if (this.m_SpawnableBuildingData.HasComponent(prefab))
                {

                    SpawnableBuildingData spawnableBuildingData = this.m_SpawnableBuildingData[prefab];
                    if (this.m_MailAccumulationData.HasComponent(spawnableBuildingData.m_ZonePrefab))
                    {
                        return this.m_MailAccumulationData[spawnableBuildingData.m_ZonePrefab].m_RequireCollect;
                    }
                }
                else
                {

                    if (this.m_ServiceObjectData.HasComponent(prefab))
                    {

                        ServiceObjectData serviceObjectData = this.m_ServiceObjectData[prefab];

                        if (this.m_MailAccumulationData.HasComponent(serviceObjectData.m_Service))
                        {
                            return this.m_MailAccumulationData[serviceObjectData.m_Service].m_RequireCollect;
                        }
                    }
                }
                return false;
            }
        }

        [BurstCompile]
        private struct CitizeSleepJob : IJob
        {
            [ReadOnly]
            public ComponentLookup<CurrentBuilding> m_CurrentBuildingData;
            public ComponentLookup<CitizenPresence> m_CitizenPresenceData;
            public NativeQueue<Entity> m_SleepQueue;

            public void Execute()
            {
                Entity entity;
                while (this.m_SleepQueue.TryDequeue(out entity))
                {
                    if (this.m_CurrentBuildingData.HasComponent(entity))
                    {
                        CurrentBuilding currentBuilding = this.m_CurrentBuildingData[entity];

                        if (this.m_CitizenPresenceData.HasComponent(currentBuilding.m_CurrentBuilding))
                        {
                            CitizenPresence citizenPresence = this.m_CitizenPresenceData[currentBuilding.m_CurrentBuilding];
                            citizenPresence.m_Delta = (sbyte)math.max(-127, (int)citizenPresence.m_Delta - 1);
                            this.m_CitizenPresenceData[currentBuilding.m_CurrentBuilding] = citizenPresence;
                        }
                    }
                }
            }
        }

        [BurstCompile]
        private struct CitizenAITickJob : IJobChunk
        {
            [ReadOnly]
            public EntityTypeHandle m_EntityType;
            public ComponentTypeHandle<Citizen> m_CitizenType;
            [ReadOnly]
            public ComponentTypeHandle<HouseholdMember> m_HouseholdMemberType;
            [ReadOnly]
            public ComponentTypeHandle<CurrentBuilding> m_CurrentBuildingType;
            [ReadOnly]
            public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;
            [ReadOnly]
            public ComponentTypeHandle<HealthProblem> m_HealthProblemType;
            public BufferTypeHandle<TripNeeded> m_TripType;
            [ReadOnly]
            public ComponentTypeHandle<Leisure> m_LeisureType;
            [NativeDisableParallelForRestriction]
            public ComponentLookup<HouseholdNeed> m_HouseholdNeeds;
            [ReadOnly]
            public ComponentLookup<Household> m_Households;
            [ReadOnly]
            public ComponentLookup<PropertyRenter> m_Properties;
            [ReadOnly]
            public ComponentLookup<Game.Objects.Transform> m_Transforms;
            [ReadOnly]
            public ComponentLookup<CarKeeper> m_CarKeepers;
            [NativeDisableParallelForRestriction]
            public ComponentLookup<Game.Vehicles.PersonalCar> m_PersonalCars;
            [ReadOnly]
            public ComponentLookup<MovingAway> m_MovingAway;
            [ReadOnly]
            public ComponentLookup<Worker> m_Workers;
            [ReadOnly]
            public ComponentLookup<Game.Citizens.Student> m_Students;
            [ReadOnly]
            public ComponentLookup<TouristHousehold> m_TouristHouseholds;
            [ReadOnly]
            public ComponentLookup<Game.Objects.OutsideConnection> m_OutsideConnections;
            [ReadOnly]
            public ComponentLookup<OutsideConnectionData> m_OutsideConnectionDatas;
            [ReadOnly]
            public ComponentLookup<InDanger> m_InDangerData;
            [ReadOnly]
            public ComponentLookup<AttendingMeeting> m_AttendingMeetings;
            [NativeDisableParallelForRestriction]
            public ComponentLookup<CoordinatedMeeting> m_Meetings;
            [ReadOnly]
            public BufferLookup<CoordinatedMeetingAttendee> m_Attendees;
            [ReadOnly]
            public BufferLookup<HaveCoordinatedMeetingData> m_MeetingDatas;
            [ReadOnly]
            public ComponentLookup<PrefabRef> m_Prefabs;
            [ReadOnly]
            public BufferLookup<Game.Buildings.Student> m_BuildingStudents;
            [ReadOnly]
            public ComponentLookup<Population> m_PopulationData;
            [ReadOnly]
            public BufferLookup<OwnedVehicle> m_OwnedVehicles;
            [ReadOnly]
            public ComponentLookup<CommuterHousehold> m_CommuterHouseholds;
            [ReadOnly]
            public EntityArchetype m_HouseholdArchetype;
            [ReadOnly]
            public NativeList<Entity> m_OutsideConnectionEntities;
            [ReadOnly]
            public EconomyParameterData m_EconomyParameters;
            [ReadOnly]
            public LeisureParametersData m_LeisureParameters;
            public uint m_UpdateFrameIndex;
            public float m_NormalizedTime;
            public uint m_SimulationFrame;
            public EntityCommandBuffer.ParallelWriter m_CommandBuffer;
            public NativeQueue<Entity>.ParallelWriter m_CarReserverQueue;
            public NativeQueue<Entity>.ParallelWriter m_MailSenderQueue;
            public NativeQueue<Entity>.ParallelWriter m_SleepQueue;
            public TimeData m_TimeData;
            public Entity m_PopulationEntity;
            public RandomSeed m_RandomSeed;
            public int lunch_break_pct;
            public float offdayprob;
            public int school_start_time;
            public int school_end_time;
            public float work_start_time;
            public float work_end_time;
            public bool school_vanilla_timeoff;
            public float delayFactor;
            public bool disable_early_shop_leisure;
            public float school_offdayprob;

            private bool CheckSleep(
              int index,
              Entity entity,
              ref Citizen citizen,
              Entity currentBuilding,
              Entity household,
              Entity home,
              DynamicBuffer<TripNeeded> trips,
              ref EconomyParameterData economyParameters,
              ref Unity.Mathematics.Random random,
              int lunch_break_pct,
              int school_start_time,
              int school_end_time,
              float work_start_time,
              float work_end_time)
            {
                if (!Time2WorkCitizenBehaviorSystem.IsSleepTime(entity, citizen, ref economyParameters, this.m_NormalizedTime, ref this.m_Workers, ref this.m_Students, lunch_break_pct, school_start_time, school_end_time, work_start_time, work_end_time, delayFactor))
                    return false;
                if (home != Entity.Null && currentBuilding == home)
                {
                    this.m_CommandBuffer.AddComponent<TravelPurpose>(index, entity, new TravelPurpose()
                    {
                        m_Purpose = Game.Citizens.Purpose.Sleeping
                    });

                    this.m_SleepQueue.Enqueue(entity);
                    this.ReleaseCar(index, entity);
                }
                else
                {
                    //If too much time has passed since sleep start time, stay where you are
                    float2 sleepTime = Time2WorkCitizenBehaviorSystem.GetSleepTime(entity, citizen, ref economyParameters, ref this.m_Workers, ref this.m_Students, lunch_break_pct, school_start_time, school_end_time, work_start_time, work_end_time, delayFactor);
                    double threshold_go_home = Math.Min(Math.Abs(sleepTime.x - this.m_NormalizedTime), Math.Abs(1 - (sleepTime.x - this.m_NormalizedTime)));

                    if (threshold_go_home <= 0.03)
                    {
                        this.GoHome(entity, home, trips, currentBuilding);
                    }      
                }
                return true;
            }

            private bool CheckLeisure(ref Citizen citizenData, ref Unity.Mathematics.Random random)
            {
                int num = 128 - (int)citizenData.m_LeisureCounter;
                return random.NextInt(this.m_LeisureParameters.m_LeisureRandomFactor) < num;
            }

            private void GoHome(
              Entity entity,
              Entity target,
              DynamicBuffer<TripNeeded> trips,
              Entity currentBuilding)
            {
                if (target == Entity.Null || currentBuilding == target)
                    return;
                if (!this.m_CarKeepers.IsComponentEnabled(entity))
                {
                    this.m_CarReserverQueue.Enqueue(entity);
                }
                this.m_MailSenderQueue.Enqueue(entity);
                TripNeeded elem = new TripNeeded()
                {
                    m_TargetAgent = target,
                    m_Purpose = Game.Citizens.Purpose.GoingHome
                };
                trips.Add(elem);
            }

            private void GoToOutsideConnection(
              Entity entity,
              Entity household,
              Entity currentBuilding,
              ref Citizen citizen,
              DynamicBuffer<TripNeeded> trips,
              Game.Citizens.Purpose purpose,
              ref Unity.Mathematics.Random random)
            {
                if (purpose == Game.Citizens.Purpose.MovingAway)
                {
                    for (int index = 0; index < trips.Length; ++index)
                    {
                        if (trips[index].m_Purpose == Game.Citizens.Purpose.MovingAway)
                            return;
                    }
                }
                if (!this.m_OutsideConnections.HasComponent(currentBuilding))
                {
                    if (!this.m_CarKeepers.IsComponentEnabled(entity))
                    {
                        this.m_CarReserverQueue.Enqueue(entity);
                    }
                    this.m_MailSenderQueue.Enqueue(entity);
                    DynamicBuffer<OwnedVehicle> ownedVehicle;
                    Entity result;
                    if (this.m_OwnedVehicles.HasBuffer(household))
                    {
                        ownedVehicle = this.m_OwnedVehicles[household];
                        if (ownedVehicle.Length > 0 && purpose == Game.Citizens.Purpose.MovingAway)
                        {
                            BuildingUtils.GetRandomOutsideConnectionByTransferType(ref this.m_OutsideConnectionEntities, ref this.m_OutsideConnectionDatas, ref this.m_Prefabs, random, OutsideConnectionTransferType.Road, out result);
                            goto label_16;
                        }
                    }
                    OutsideConnectionTransferType ocTransferType = OutsideConnectionTransferType.Train | OutsideConnectionTransferType.Air | OutsideConnectionTransferType.Ship;
                    if (this.m_OwnedVehicles.HasBuffer(household))
                    {
                        ownedVehicle = this.m_OwnedVehicles[household];
                        if (ownedVehicle.Length > 0)
                            ocTransferType |= OutsideConnectionTransferType.Road;
                    }
                    BuildingUtils.GetRandomOutsideConnectionByTransferType(ref this.m_OutsideConnectionEntities, ref this.m_OutsideConnectionDatas, ref this.m_Prefabs, random, ocTransferType, out result);
                label_16:
                    if (result == Entity.Null)
                    {
                        result = this.m_OutsideConnectionEntities[random.NextInt(this.m_OutsideConnectionEntities.Length)];
                    }
                    trips.Add(new TripNeeded()
                    {
                        m_TargetAgent = result,
                        m_Purpose = purpose
                    });
                }
                else
                {
                    if (purpose != Game.Citizens.Purpose.MovingAway)
                        return;
                    citizen.m_State |= CitizenFlags.MovingAway;
                }
            }

            private void GoShopping(
              int chunkIndex,
              Entity citizen,
              Entity household,
              HouseholdNeed need,
              float3 position)
            {
                if (!this.m_CarKeepers.IsComponentEnabled(citizen))
                {
                    this.m_CarReserverQueue.Enqueue(citizen);
                }
                this.m_MailSenderQueue.Enqueue(citizen);
                this.m_CommandBuffer.AddComponent<ResourceBuyer>(chunkIndex, citizen, new ResourceBuyer()
                {
                    m_Payer = household,
                    m_Flags = SetupTargetFlags.Commercial,
                    m_Location = position,
                    m_ResourceNeeded = need.m_Resource,
                    m_AmountNeeded = need.m_Amount
                });
            }

            private float GetTimeLeftUntilInterval(float2 interval)
            {
                return (double)this.m_NormalizedTime >= (double)interval.x ? 1f - this.m_NormalizedTime + interval.x : interval.x - this.m_NormalizedTime;
            }

            private bool DoLeisure(
              int chunkIndex,
              Entity entity,
              ref Citizen citizen,
              Entity household,
              float3 position,
              int population,
              ref Unity.Mathematics.Random random,
              ref EconomyParameterData economyParameters)
            {
                int num1 = math.min(80, Mathf.RoundToInt(200f / math.max(1f, math.sqrt(economyParameters.m_TrafficReduction * (float)population))));
                if (random.NextInt(100) > num1)
                {
                    citizen.m_LeisureCounter = byte.MaxValue;
                    return true;
                }
                float x = this.GetTimeLeftUntilInterval(Time2WorkCitizenBehaviorSystem.GetSleepTime(entity, citizen, ref economyParameters, ref this.m_Workers, ref this.m_Students, lunch_break_pct, school_start_time, school_end_time, work_start_time, work_end_time, delayFactor));
                if (this.m_Workers.HasComponent(entity))
                {
                    Worker worker = this.m_Workers[entity];
                    float2 timeToWork = Time2WorkWorkerSystem.GetTimeToWork(citizen, worker, ref economyParameters, true, lunch_break_pct, work_start_time, work_end_time, delayFactor);
                    float2 timeToLunch = Time2WorkWorkerSystem.GetLunchTime(citizen, worker, ref economyParameters); 
                    if(timeToLunch.x > 0)
                    {
                        float x1 = math.min(this.GetTimeLeftUntilInterval(timeToLunch), this.GetTimeLeftUntilInterval(timeToWork));
                        x = math.min(x, x1);
                    } else
                    {
                        x = math.min(x, this.GetTimeLeftUntilInterval(timeToWork));
                    }
                }
                else
                {
                    if (this.m_Students.HasComponent(entity))
                    {
                        Game.Citizens.Student student = this.m_Students[entity];
                        float2 timeToStudy = Time2WorkStudentSystem.GetTimeToStudy(citizen, student, ref economyParameters, school_start_time, school_end_time);
                        x = math.min(x, this.GetTimeLeftUntilInterval(timeToStudy));
                    }
                }
                uint num2 = (uint)((double)x * 262144.0);
                Leisure component = new Leisure()
                {
                    m_LastPossibleFrame = this.m_SimulationFrame + num2
                };
                this.m_CommandBuffer.AddComponent<Leisure>(chunkIndex, entity, component);
                return false;
            }

            private void ReleaseCar(int chunkIndex, Entity citizen)
            {
                if (!this.m_CarKeepers.IsComponentEnabled(citizen))
                    return;
                Entity car = this.m_CarKeepers[citizen].m_Car;
                if (this.m_PersonalCars.HasComponent(car))
                {
                    Game.Vehicles.PersonalCar personalCar = this.m_PersonalCars[car];
                    personalCar.m_Keeper = Entity.Null;
                    this.m_PersonalCars[car] = personalCar;
                }

                this.m_CommandBuffer.SetComponentEnabled<CarKeeper>(chunkIndex, citizen, false);
            }

            private bool AttendMeeting(
              int chunkIndex,
              Entity entity,
              ref Citizen citizen,
              Entity household,
              Entity currentBuilding,
              DynamicBuffer<TripNeeded> trips,
              ref Unity.Mathematics.Random random)
            {
                Entity meeting1 = this.m_AttendingMeetings[entity].m_Meeting;
                if (this.m_Attendees.HasBuffer(meeting1) && this.m_Meetings.HasComponent(meeting1))
                {
                    CoordinatedMeeting meeting2 = this.m_Meetings[meeting1];
                    if (this.m_Prefabs.HasComponent(meeting1) && meeting2.m_Status != MeetingStatus.Done)
                    {
                        HaveCoordinatedMeetingData coordinatedMeetingData = this.m_MeetingDatas[this.m_Prefabs[meeting1].m_Prefab][meeting2.m_Phase];
                        DynamicBuffer<CoordinatedMeetingAttendee> attendee = this.m_Attendees[meeting1];
                        if (meeting2.m_Status == MeetingStatus.Waiting && meeting2.m_Target == Entity.Null)
                        {
                            if (attendee.Length > 0 && attendee[0].m_Attendee == entity)
                            {
                                if (coordinatedMeetingData.m_Purpose.m_Purpose == Game.Citizens.Purpose.Shopping)
                                {
                                    float3 position = this.m_Transforms[currentBuilding].m_Position;
                                    this.GoShopping(chunkIndex, entity, household, new HouseholdNeed()
                                    {
                                        m_Resource = coordinatedMeetingData.m_Purpose.m_Resource,
                                        m_Amount = coordinatedMeetingData.m_Purpose.m_Data
                                    }, position);
                                    return true;
                                }
                                if (coordinatedMeetingData.m_Purpose.m_Purpose == Game.Citizens.Purpose.Traveling)
                                {
                                    Citizen citizen1 = new Citizen();
                                    this.GoToOutsideConnection(entity, household, currentBuilding, ref citizen1, trips, coordinatedMeetingData.m_Purpose.m_Purpose, ref random);
                                }
                                else if (coordinatedMeetingData.m_Purpose.m_Purpose == Game.Citizens.Purpose.GoingHome)
                                {
                                    if (this.m_Properties.HasComponent(household))
                                    {
                                        meeting2.m_Target = this.m_Properties[household].m_Property;
                                        this.m_Meetings[meeting1] = meeting2;
                                        this.GoHome(entity, this.m_Properties[household].m_Property, trips, currentBuilding);
                                    }
                                }
                                else
                                {
                                    trips.Add(new TripNeeded()
                                    {
                                        m_Purpose = coordinatedMeetingData.m_Purpose.m_Purpose,
                                        m_Resource = coordinatedMeetingData.m_Purpose.m_Resource,
                                        m_Data = coordinatedMeetingData.m_Purpose.m_Data,
                                        m_TargetAgent = new Entity()
                                    });
                                    return true;
                                }
                            }
                        }
                        else if (meeting2.m_Status == MeetingStatus.Waiting || meeting2.m_Status == MeetingStatus.Traveling)
                        {
                            for (int index = 0; index < attendee.Length; ++index)
                            {
                                if (attendee[index].m_Attendee == entity)
                                {
                                    if (meeting2.m_Target != Entity.Null && currentBuilding != meeting2.m_Target && (!this.m_Properties.HasComponent(meeting2.m_Target) || this.m_Properties[meeting2.m_Target].m_Property != currentBuilding))
                                        trips.Add(new TripNeeded()
                                        {
                                            m_Purpose = coordinatedMeetingData.m_Purpose.m_Purpose,
                                            m_Resource = coordinatedMeetingData.m_Purpose.m_Resource,
                                            m_Data = coordinatedMeetingData.m_Purpose.m_Data,
                                            m_TargetAgent = meeting2.m_Target
                                        });
                                    return true;
                                }
                            }
                            this.m_CommandBuffer.RemoveComponent<AttendingMeeting>(chunkIndex, entity);
                            return false;
                        }
                    }
                    return meeting2.m_Status != MeetingStatus.Done;
                }
                this.m_CommandBuffer.RemoveComponent<AttendingMeeting>(chunkIndex, entity);
                return false;
            }

            public void Execute(
              in ArchetypeChunk chunk,
              int unfilteredChunkIndex,
              bool useEnabledMask,
              in v128 chunkEnabledMask)
            {
                if ((int)chunk.GetSharedComponent<UpdateFrame>(this.m_UpdateFrameType).m_Index != (int)this.m_UpdateFrameIndex)
                    return;
                Unity.Mathematics.Random random = this.m_RandomSeed.GetRandom(unfilteredChunkIndex);
                NativeArray<Entity> nativeArray1 = chunk.GetNativeArray(this.m_EntityType);
                NativeArray<Citizen> nativeArray2 = chunk.GetNativeArray<Citizen>(ref this.m_CitizenType);
                NativeArray<HouseholdMember> nativeArray3 = chunk.GetNativeArray<HouseholdMember>(ref this.m_HouseholdMemberType);
                NativeArray<CurrentBuilding> nativeArray4 = chunk.GetNativeArray<CurrentBuilding>(ref this.m_CurrentBuildingType);
                BufferAccessor<TripNeeded> bufferAccessor = chunk.GetBufferAccessor<TripNeeded>(ref this.m_TripType);
                bool flag1 = chunk.Has<HealthProblem>(ref this.m_HealthProblemType);
                int population = this.m_PopulationData[this.m_PopulationEntity].m_Population;
                for (int index = 0; index < nativeArray1.Length; ++index)
                {
                    Entity household = nativeArray3[index].m_Household;
                    Entity entity1 = nativeArray1[index];
                    bool flag2 = this.m_TouristHouseholds.HasComponent(household);
                    DynamicBuffer<TripNeeded> trips = bufferAccessor[index];
                    if (household == Entity.Null)
                    {
                        Entity entity2 = this.m_CommandBuffer.CreateEntity(unfilteredChunkIndex, this.m_HouseholdArchetype);
                        this.m_CommandBuffer.SetComponent<HouseholdMember>(unfilteredChunkIndex, entity1, new HouseholdMember()
                        {
                            m_Household = entity2
                        });
                        this.m_CommandBuffer.SetBuffer<HouseholdCitizen>(unfilteredChunkIndex, entity2).Add(new HouseholdCitizen()
                        {
                            m_Citizen = entity1
                        });
                    }
                    else
                    {
                        if (!this.m_Households.HasComponent(household))
                        {
                            this.m_CommandBuffer.AddComponent<Deleted>(unfilteredChunkIndex, entity1, new Deleted());
                        }
                        else
                        {
                            Entity currentBuilding = nativeArray4[index].m_CurrentBuilding;
                            if (this.m_Transforms.HasComponent(currentBuilding) && (!this.m_InDangerData.HasComponent(currentBuilding) || (this.m_InDangerData[currentBuilding].m_Flags & DangerFlags.StayIndoors) == (DangerFlags)0))
                            {
                                Citizen citizen = nativeArray2[index];
                                bool flag3 = (citizen.m_State & CitizenFlags.Commuter) != 0;
                                CitizenAge age = citizen.GetAge();
                                if (flag3 && (age == CitizenAge.Elderly || age == CitizenAge.Child))
                                {
                                    this.m_CommandBuffer.AddComponent<Deleted>(unfilteredChunkIndex, entity1, new Deleted());
                                }
                                if ((citizen.m_State & CitizenFlags.MovingAway) != CitizenFlags.None)
                                {
                                    this.m_CommandBuffer.AddComponent<Deleted>(unfilteredChunkIndex, entity1, new Deleted());
                                }
                                else
                                {
                                    if (this.m_MovingAway.HasComponent(household))
                                    {
                                        this.GoToOutsideConnection(entity1, household, currentBuilding, ref citizen, trips, Game.Citizens.Purpose.MovingAway, ref random);
                                        if (chunk.Has<Leisure>(ref this.m_LeisureType))
                                        {
                                            this.m_CommandBuffer.RemoveComponent<Leisure>(unfilteredChunkIndex, entity1);
                                        }
                                        if (this.m_Workers.HasComponent(entity1))
                                        {
                                            this.m_CommandBuffer.RemoveComponent<Worker>(unfilteredChunkIndex, entity1);
                                        }
                                        if (this.m_Students.HasComponent(entity1))
                                        {
                                            if (this.m_BuildingStudents.HasBuffer(this.m_Students[entity1].m_School))
                                            {
                                                this.m_CommandBuffer.AddComponent<StudentsRemoved>(unfilteredChunkIndex, this.m_Students[entity1].m_School);
                                            }
                                            this.m_CommandBuffer.RemoveComponent<Game.Citizens.Student>(unfilteredChunkIndex, entity1);
                                        }
                                        nativeArray2[index] = citizen;
                                    }
                                    else
                                    {
                                        Entity entity3 = Entity.Null;
                                        if (this.m_Properties.HasComponent(household))
                                        {
                                            entity3 = this.m_Properties[household].m_Property;
                                        }
                                        else if (flag2)
                                        {
                                            Entity hotel = this.m_TouristHouseholds[household].m_Hotel;
                                            if (this.m_Properties.HasComponent(hotel))
                                            {
                                                entity3 = this.m_Properties[hotel].m_Property;
                                            }
                                        }
                                        else if (flag3)
                                        {
                                            if (this.m_OutsideConnections.HasComponent(currentBuilding))
                                            {
                                                entity3 = currentBuilding;
                                            }
                                            else
                                            {
                                                CommuterHousehold componentData;
                                                if (this.m_CommuterHouseholds.TryGetComponent(household, out componentData))
                                                    entity3 = componentData.m_OriginalFrom;
                                                if (entity3 == Entity.Null)
                                                {
                                                    entity3 = this.m_OutsideConnectionEntities[random.NextInt(this.m_OutsideConnectionEntities.Length)];
                                                }
                                            }
                                        }
                                        if (flag1)
                                        {
                                            if (chunk.Has<Leisure>(ref this.m_LeisureType))
                                            {
                                                this.m_CommandBuffer.RemoveComponent<Leisure>(unfilteredChunkIndex, entity1);
                                            }
                                        }
                                        else
                                        {
                                            if (!this.m_AttendingMeetings.HasComponent(entity1) || !this.AttendMeeting(unfilteredChunkIndex, entity1, ref citizen, household, currentBuilding, trips, ref random))
                                            {
                                                if (this.m_Workers.HasComponent(entity1) && !Time2WorkWorkerSystem.IsTodayOffDay(citizen, ref this.m_EconomyParameters, this.m_SimulationFrame, this.m_TimeData, population, this.m_NormalizedTime, offdayprob) 
                                                    && Time2WorkWorkerSystem.IsTimeToWork(citizen, this.m_Workers[entity1], ref this.m_EconomyParameters, this.m_NormalizedTime, lunch_break_pct, work_start_time, work_end_time, delayFactor) || this.m_Students.HasComponent(entity1) && Time2WorkStudentSystem.IsTimeToStudy(citizen, this.m_Students[entity1], ref this.m_EconomyParameters, this.m_NormalizedTime, this.m_SimulationFrame, this.m_TimeData, population, school_offdayprob, school_start_time, school_end_time))
                                                {
                                                    if(this.m_Workers.HasComponent(entity1) && Time2WorkWorkerSystem.IsLunchTime(citizen, this.m_Workers[entity1], ref this.m_EconomyParameters, this.m_NormalizedTime, lunch_break_pct))
                                                    {
                                                        HouseholdNeed householdNeed = this.m_HouseholdNeeds[household];

                                                        int num = 60;
                                                        int prob = Unity.Mathematics.Random.CreateFromIndex((uint)(citizen.m_PseudoRandom)).NextInt(100);
                                                        if ((householdNeed.m_Resource != Resource.NoResource || num < prob) && this.m_Transforms.HasComponent(currentBuilding))
                                                        {
                                                            if (householdNeed.m_Resource == Resource.NoResource)
                                                            {
                                                                if (prob < 40)
                                                                {
                                                                    householdNeed.m_Resource = Resource.Meals;
                                                                } else
                                                                {
                                                                    if(prob < 50)
                                                                    {
                                                                        householdNeed.m_Resource = Resource.Food;
                                                                    } else
                                                                    {
                                                                        householdNeed.m_Resource = Resource.ConvenienceFood;
                                                                    }  
                                                                }
                                                                householdNeed.m_Amount = 5;
                                                            }
                                                            this.GoShopping(unfilteredChunkIndex, entity1, household, householdNeed, this.m_Transforms[currentBuilding].m_Position);
                                                            householdNeed.m_Resource = Resource.NoResource;
                                                            this.m_HouseholdNeeds[household] = householdNeed;
                                                            if (chunk.Has<Leisure>(ref this.m_LeisureType))
                                                            {
                                                                this.m_CommandBuffer.RemoveComponent<Leisure>(unfilteredChunkIndex, entity1);
                                                            }
                                                        } else
                                                        {
                                                            num = chunk.Has<Leisure>(ref this.m_LeisureType) || this.m_OutsideConnections.HasComponent(currentBuilding) ? 0 : (this.CheckLeisure(ref citizen, ref random) ? 1 : 0);
                                                            nativeArray2[index] = citizen;
                                                            if (num != 0)
                                                            {
                                                                if (this.DoLeisure(unfilteredChunkIndex, entity1, ref citizen, household, this.m_Transforms[currentBuilding].m_Position, population, ref random, ref this.m_EconomyParameters))
                                                                    nativeArray2[index] = citizen;
                                                            }
                                                        }
                                                    }
                                                    if (chunk.Has<Leisure>(ref this.m_LeisureType))
                                                    {
                                                        this.m_CommandBuffer.RemoveComponent<Leisure>(unfilteredChunkIndex, entity1);
                                                    }
                                                }
                                                else
                                                {
                                                    if (this.CheckSleep(index, entity1, ref citizen, currentBuilding, household, entity3, trips, ref this.m_EconomyParameters, ref random, lunch_break_pct, school_start_time, school_end_time, work_start_time, work_end_time))
                                                    {
                                                        if (chunk.Has<Leisure>(ref this.m_LeisureType))
                                                        {
                                                            this.m_CommandBuffer.RemoveComponent<Leisure>(unfilteredChunkIndex, entity1);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        //If today is off day, go shopping or for leisure around 10 AM
                                                        float x1 = (float)(GaussianRandom.NextGaussianDouble(random)) * 0.05f + 0.4f;
                                                        float x2 = 1 - Math.Abs((float)(GaussianRandom.NextGaussianDouble(random)) * 0.05f);

                                                        if ((this.m_NormalizedTime > x1 && this.m_NormalizedTime < x2)|| !disable_early_shop_leisure)
                                                        {
                                                            if (age == CitizenAge.Adult || age == CitizenAge.Elderly)
                                                            {
                                                                HouseholdNeed householdNeed = this.m_HouseholdNeeds[household];
                                                                if (householdNeed.m_Resource != Resource.NoResource && this.m_Transforms.HasComponent(currentBuilding))
                                                                {
                                                                    this.GoShopping(unfilteredChunkIndex, entity1, household, householdNeed, this.m_Transforms[currentBuilding].m_Position);
                                                                    householdNeed.m_Resource = Resource.NoResource;
                                                                    this.m_HouseholdNeeds[household] = householdNeed;
                                                                    if (chunk.Has<Leisure>(ref this.m_LeisureType))
                                                                    {
                                                                        this.m_CommandBuffer.RemoveComponent<Leisure>(unfilteredChunkIndex, entity1);
                                                                        continue;
                                                                    }
                                                                    continue;
                                                                }
                                                            }
                                                        }

                                                        int num = chunk.Has<Leisure>(ref this.m_LeisureType) || this.m_OutsideConnections.HasComponent(currentBuilding) ? 0 : (this.CheckLeisure(ref citizen, ref random) ? 1 : 0);
                                                        nativeArray2[index] = citizen;
                                                        if (num != 0 && (this.m_NormalizedTime > x1 || disable_early_shop_leisure))
                                                        {
                                                            if (this.DoLeisure(unfilteredChunkIndex, entity1, ref citizen, household, this.m_Transforms[currentBuilding].m_Position, population, ref random, ref this.m_EconomyParameters))
                                                                nativeArray2[index] = citizen;
                                                        }
                                                        else
                                                        {
                                                            if (!chunk.Has<Leisure>(ref this.m_LeisureType))
                                                            {
                                                                if (currentBuilding != entity3)
                                                                {
                                                                    this.GoHome(entity1, entity3, trips, currentBuilding);
                                                                }
                                                                else
                                                                {
                                                                    this.ReleaseCar(unfilteredChunkIndex, entity1);
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
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

        private struct TypeHandle
        {
            public ComponentTypeHandle<Citizen> __Game_Citizens_Citizen_RW_ComponentTypeHandle;
            [ReadOnly]
            public ComponentTypeHandle<CurrentBuilding> __Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle;
            [ReadOnly]
            public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;
            [ReadOnly]
            public ComponentTypeHandle<HouseholdMember> __Game_Citizens_HouseholdMember_RO_ComponentTypeHandle;
            public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;
            [ReadOnly]
            public ComponentTypeHandle<HealthProblem> __Game_Citizens_HealthProblem_RO_ComponentTypeHandle;
            public BufferTypeHandle<TripNeeded> __Game_Citizens_TripNeeded_RW_BufferTypeHandle;
            [ReadOnly]
            public ComponentTypeHandle<Leisure> __Game_Citizens_Leisure_RO_ComponentTypeHandle;
            public ComponentLookup<HouseholdNeed> __Game_Citizens_HouseholdNeed_RW_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Household> __Game_Citizens_Household_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<CarKeeper> __Game_Citizens_CarKeeper_RO_ComponentLookup;
            public ComponentLookup<Game.Vehicles.PersonalCar> __Game_Vehicles_PersonalCar_RW_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<MovingAway> __Game_Agents_MovingAway_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Worker> __Game_Citizens_Worker_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Game.Citizens.Student> __Game_Citizens_Student_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<TouristHousehold> __Game_Citizens_TouristHousehold_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Game.Objects.OutsideConnection> __Game_Objects_OutsideConnection_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<InDanger> __Game_Events_InDanger_RO_ComponentLookup;
            [ReadOnly]
            public BufferLookup<CoordinatedMeetingAttendee> __Game_Citizens_CoordinatedMeetingAttendee_RO_BufferLookup;
            public ComponentLookup<CoordinatedMeeting> __Game_Citizens_CoordinatedMeeting_RW_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<AttendingMeeting> __Game_Citizens_AttendingMeeting_RO_ComponentLookup;
            [ReadOnly]
            public BufferLookup<HaveCoordinatedMeetingData> __Game_Prefabs_HaveCoordinatedMeetingData_RO_BufferLookup;
            [ReadOnly]
            public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;
            [ReadOnly]
            public BufferLookup<Game.Buildings.Student> __Game_Buildings_Student_RO_BufferLookup;
            [ReadOnly]
            public ComponentLookup<Population> __Game_City_Population_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<OutsideConnectionData> __Game_Prefabs_OutsideConnectionData_RO_ComponentLookup;
            [ReadOnly]
            public BufferLookup<OwnedVehicle> __Game_Vehicles_OwnedVehicle_RO_BufferLookup;
            [ReadOnly]
            public ComponentLookup<CommuterHousehold> __Game_Citizens_CommuterHousehold_RO_ComponentLookup;
            public ComponentLookup<CarKeeper> __Game_Citizens_CarKeeper_RW_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<HouseholdMember> __Game_Citizens_HouseholdMember_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Citizen> __Game_Citizens_Citizen_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<CurrentBuilding> __Game_Citizens_CurrentBuilding_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<MailAccumulationData> __Game_Prefabs_MailAccumulationData_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<ServiceObjectData> __Game_Prefabs_ServiceObjectData_RO_ComponentLookup;
            public ComponentLookup<MailSender> __Game_Citizens_MailSender_RW_ComponentLookup;
            public ComponentLookup<MailProducer> __Game_Buildings_MailProducer_RW_ComponentLookup;
            public ComponentLookup<CitizenPresence> __Game_Buildings_CitizenPresence_RW_ComponentLookup;

            public void __AssignHandles(ref SystemState state)
            {
                this.__Game_Citizens_Citizen_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Citizen>();
                this.__Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CurrentBuilding>(true);
                this.__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
                this.__Game_Citizens_HouseholdMember_RO_ComponentTypeHandle = state.GetComponentTypeHandle<HouseholdMember>(true);
                this.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
                this.__Game_Citizens_HealthProblem_RO_ComponentTypeHandle = state.GetComponentTypeHandle<HealthProblem>(true);
                this.__Game_Citizens_TripNeeded_RW_BufferTypeHandle = state.GetBufferTypeHandle<TripNeeded>();
                this.__Game_Citizens_Leisure_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Leisure>(true);
                this.__Game_Citizens_HouseholdNeed_RW_ComponentLookup = state.GetComponentLookup<HouseholdNeed>();
                this.__Game_Citizens_Household_RO_ComponentLookup = state.GetComponentLookup<Household>(true);
                this.__Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(true);
                this.__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(true);
                this.__Game_Citizens_CarKeeper_RO_ComponentLookup = state.GetComponentLookup<CarKeeper>(true);
                this.__Game_Vehicles_PersonalCar_RW_ComponentLookup = state.GetComponentLookup<Game.Vehicles.PersonalCar>();
                this.__Game_Agents_MovingAway_RO_ComponentLookup = state.GetComponentLookup<MovingAway>(true);
                this.__Game_Citizens_Worker_RO_ComponentLookup = state.GetComponentLookup<Worker>(true);
                this.__Game_Citizens_Student_RO_ComponentLookup = state.GetComponentLookup<Game.Citizens.Student>(true);
                this.__Game_Citizens_TouristHousehold_RO_ComponentLookup = state.GetComponentLookup<TouristHousehold>(true);
                this.__Game_Objects_OutsideConnection_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.OutsideConnection>(true);
                this.__Game_Events_InDanger_RO_ComponentLookup = state.GetComponentLookup<InDanger>(true);
                this.__Game_Citizens_CoordinatedMeetingAttendee_RO_BufferLookup = state.GetBufferLookup<CoordinatedMeetingAttendee>(true);
                this.__Game_Citizens_CoordinatedMeeting_RW_ComponentLookup = state.GetComponentLookup<CoordinatedMeeting>();
                this.__Game_Citizens_AttendingMeeting_RO_ComponentLookup = state.GetComponentLookup<AttendingMeeting>(true);
                this.__Game_Prefabs_HaveCoordinatedMeetingData_RO_BufferLookup = state.GetBufferLookup<HaveCoordinatedMeetingData>(true);
                this.__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(true);
                this.__Game_Buildings_Student_RO_BufferLookup = state.GetBufferLookup<Game.Buildings.Student>(true);
                this.__Game_City_Population_RO_ComponentLookup = state.GetComponentLookup<Population>(true);
                this.__Game_Prefabs_OutsideConnectionData_RO_ComponentLookup = state.GetComponentLookup<OutsideConnectionData>(true);
                this.__Game_Vehicles_OwnedVehicle_RO_BufferLookup = state.GetBufferLookup<OwnedVehicle>(true);
                this.__Game_Citizens_CommuterHousehold_RO_ComponentLookup = state.GetComponentLookup<CommuterHousehold>(true);
                this.__Game_Citizens_CarKeeper_RW_ComponentLookup = state.GetComponentLookup<CarKeeper>();
                this.__Game_Citizens_HouseholdMember_RO_ComponentLookup = state.GetComponentLookup<HouseholdMember>(true);
                this.__Game_Citizens_Citizen_RO_ComponentLookup = state.GetComponentLookup<Citizen>(true);
                this.__Game_Citizens_CurrentBuilding_RO_ComponentLookup = state.GetComponentLookup<CurrentBuilding>(true);
                this.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup = state.GetComponentLookup<SpawnableBuildingData>(true);
                this.__Game_Prefabs_MailAccumulationData_RO_ComponentLookup = state.GetComponentLookup<MailAccumulationData>(true);
                this.__Game_Prefabs_ServiceObjectData_RO_ComponentLookup = state.GetComponentLookup<ServiceObjectData>(true);
                this.__Game_Citizens_MailSender_RW_ComponentLookup = state.GetComponentLookup<MailSender>();
                this.__Game_Buildings_MailProducer_RW_ComponentLookup = state.GetComponentLookup<MailProducer>();
                this.__Game_Buildings_CitizenPresence_RW_ComponentLookup = state.GetComponentLookup<CitizenPresence>();
            }
        }
    }
}
