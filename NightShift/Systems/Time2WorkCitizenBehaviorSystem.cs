﻿using Game;
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
using Game.Simulation;
using Game.Tools;
using Game.Vehicles;
using System;
using System.Data;
using System.Runtime.CompilerServices;
using Time2Work.Components;
using Time2Work.Systems;
using Time2Work.Utils;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static Time2Work.Time2WorkWorkerSystem;

namespace Time2Work
{
    public partial class Time2WorkCitizenBehaviorSystem : GameSystemBase
    {
        public static readonly float kMaxPathfindCost = 17000f;
        public static readonly float kMaxPathfindCostLeisure = 17000f;
        public static readonly float kMaxMovingAwayCost = CitizenBehaviorSystem.kMaxPathfindCost * 10f;
        public static readonly int kMinLeisurePossibility = 80;
        private JobHandle m_CarReserveWriters;
        private EntityQuery m_CitizenQuery;
        private EntityQuery m_OutsideConnectionQuery;
        private EntityQuery m_EconomyParameterQuery;
        private EntityQuery m_LeisureParameterQuery;
        private EntityQuery m_TimeDataQuery;
        private EntityQuery m_PopulationQuery;
        private SimulationSystem m_SimulationSystem;
        private Time2WorkTimeSystem m_TimeSystem;
        private EndFrameBarrier m_EndFrameBarrier;
        private EntityArchetype m_HouseholdArchetype;
        private NativeQueue<Entity> m_CarReserveQueue;
        private NativeQueue<Entity>.ParallelWriter m_ParallelCarReserveQueue;
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
          float2 time2WorkOrStudy)
        {
            int age = (int)citizen.GetAge();
            float2 float2_1 = new float2(0.875f, 0.21f);
            float num = float2_1.y - float2_1.x;
            Unity.Mathematics.Random pseudoRandom = citizen.GetPseudoRandom(CitizenPseudoRandom.SleepOffset);
            float2 x1 = float2_1 + (float)(GaussianRandom.NextGaussianDouble(pseudoRandom) * 0.1f) + 0.1f;

            if (age == 3)
                x1 -= 0.05f;
            if (age == 0)
                x1 -= 0.1f;
            if (age == 1)
                x1 += 0.05f;
            float2 x2 = math.frac(x1);
            float2 float2_2 = time2WorkOrStudy;
            if (workers.HasComponent(entity))
            {
                //This is to avoid part time workers from going to sleep too early
                if (workers[entity].m_Shift == Workshift.Day && float2_2.y < 0.55f)
                {
                    float2_2.y += 0.2f;
                }
                else if (workers[entity].m_Shift == Workshift.Day && float2_2.x >= 0.55f)
                {
                    float2_2.x -= 0.2f;
                }
            }
            else
            {
                if (!students.HasComponent(entity))
                    return x2;
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
          int3 school_start_time,
          int3 school_end_time,
          float work_start_time,
          float work_end_time,
          float delayFactor,
          int ticksPerDay,
          int part_time_prob,
          float commute_top10,
          float overtime,
          float part_time_reduction)
        {
            float2 sleepTime = Time2WorkCitizenBehaviorSystem.GetSleepTime(entity, citizen, ref economyParameters, ref workers, ref students, lunch_break_pct, school_start_time, school_end_time, work_start_time, work_end_time, delayFactor, ticksPerDay, part_time_prob, commute_top10, overtime, part_time_reduction);
            return (double)sleepTime.y < (double)sleepTime.x ? (double)normalizedTime > (double)sleepTime.x || (double)normalizedTime < (double)sleepTime.y : (double)normalizedTime > (double)sleepTime.x && (double)normalizedTime < (double)sleepTime.y;
        }

        public static float2 GetSleepTime(
          Entity entity,
          Citizen citizen,
          ref EconomyParameterData economyParameters,
          ref ComponentLookup<Worker> workers,
          ref ComponentLookup<Game.Citizens.Student> students,
          int lunch_break_pct,
          int3 school_start_time,
          int3 school_end_time,
          float work_start_time,
          float work_end_time,
          float delayFactor,
          int ticksPerDay,
          int part_time_prob,
          float commute_top10,
          float overtime,
          float part_time_reduction)
        {
            float2 float2_2;
            float start_work;
            if (workers.HasComponent(entity))
            {
                float2_2 = Time2WorkWorkerSystem.GetTimeToWork(citizen, workers[entity], ref economyParameters, true, lunch_break_pct, work_start_time, work_end_time, delayFactor, ticksPerDay, part_time_prob, commute_top10, overtime, part_time_reduction, out start_work);
            }
            else
            {
                float2_2 = Time2WorkStudentSystem.GetTimeToStudy(citizen, students[entity], ref economyParameters, school_start_time, school_end_time, ticksPerDay, out start_work);
            }

            return GetSleepTime(entity, citizen, ref economyParameters, ref workers, ref students, float2_2); ;
        }

        public static bool IsSleepTime(
          Entity entity,
          Citizen citizen,
          ref EconomyParameterData economyParameters,
          float normalizedTime,
          ref ComponentLookup<Worker> workers,
          ref ComponentLookup<Game.Citizens.Student> students,
          float2 time2WorkOrStudy,
          out float2 sleepTime)
        {
            sleepTime = Time2WorkCitizenBehaviorSystem.GetSleepTime(entity, citizen, ref economyParameters, ref workers, ref students, time2WorkOrStudy);
            return (double)sleepTime.y < (double)sleepTime.x ? (double)normalizedTime > (double)sleepTime.x || (double)normalizedTime < (double)sleepTime.y : (double)normalizedTime > (double)sleepTime.x && (double)normalizedTime < (double)sleepTime.y;
        }

        public NativeQueue<Entity>.ParallelWriter GetCarReserveQueue(out JobHandle deps)
        {
            deps = this.m_CarReserveWriters;
            return this.m_ParallelCarReserveQueue;
        }

        public void AddCarReserveWriter(JobHandle writer)
        {
            this.m_CarReserveWriters = JobHandle.CombineDependencies(this.m_CarReserveWriters, writer);
        }

        [UnityEngine.Scripting.Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();
            this.m_SimulationSystem = this.World.GetOrCreateSystemManaged<SimulationSystem>();
            this.m_TimeSystem = this.World.GetOrCreateSystemManaged<Time2WorkTimeSystem>();
            this.m_EndFrameBarrier = this.World.GetOrCreateSystemManaged<EndFrameBarrier>();
            this.m_CarReserveQueue = new NativeQueue<Entity>((AllocatorManager.AllocatorHandle)Allocator.Persistent);
            this.m_ParallelCarReserveQueue = this.m_CarReserveQueue.AsParallelWriter();
            this.m_EconomyParameterQuery = this.GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
            this.m_LeisureParameterQuery = this.GetEntityQuery(ComponentType.ReadOnly<LeisureParametersData>());
            this.m_PopulationQuery = this.GetEntityQuery(ComponentType.ReadOnly<Population>());
            this.m_CitizenQuery = this.GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[4]
             {
                   ComponentType.ReadWrite<Citizen>(),
                   ComponentType.ReadOnly<CurrentBuilding>(),
                   ComponentType.ReadOnly<HouseholdMember>(),
                   ComponentType.ReadOnly<UpdateFrame>(),
             },
                Any = new ComponentType[0]
               {
                    
               },
                None = new ComponentType[4]
             {
                ComponentType.Exclude<TravelPurpose>(),
                ComponentType.Exclude<ResourceBuyer>(),
                ComponentType.Exclude<Deleted>(),
                ComponentType.Exclude<Temp>(),
             }
            });
            this.m_OutsideConnectionQuery = this.GetEntityQuery(ComponentType.ReadOnly<Game.Objects.OutsideConnection>(), ComponentType.Exclude<Game.Objects.ElectricityOutsideConnection>(), ComponentType.Exclude<Game.Objects.WaterPipeOutsideConnection>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
            this.m_TimeDataQuery = this.GetEntityQuery(ComponentType.ReadOnly<Game.Common.TimeData>());
            this.m_HouseholdArchetype = this.World.EntityManager.CreateArchetype(ComponentType.ReadWrite<Household>(), ComponentType.ReadWrite<HouseholdNeed>(), ComponentType.ReadWrite<HouseholdCitizen>(), ComponentType.ReadWrite<TaxPayer>(), ComponentType.ReadWrite<Game.Economy.Resources>(), ComponentType.ReadWrite<UpdateFrame>(), ComponentType.ReadWrite<Created>());
            this.RequireForUpdate(this.m_CitizenQuery);
            this.RequireForUpdate(this.m_EconomyParameterQuery);
            this.RequireForUpdate(this.m_LeisureParameterQuery);
            this.RequireForUpdate(this.m_TimeDataQuery);
            this.RequireForUpdate(this.m_PopulationQuery);
            this.m_daytype = WeekSystem.currentDayOfTheWeek;
        }

        [UnityEngine.Scripting.Preserve]
        protected override void OnDestroy()
        {
            this.m_CarReserveQueue.Dispose();
            base.OnDestroy();
        }

        [UnityEngine.Scripting.Preserve]
        protected override void OnUpdate()
        {
            uint frameWithInterval = SimulationUtils.GetUpdateFrameWithInterval(this.m_SimulationSystem.frameIndex, (uint)this.GetUpdateInterval(SystemUpdatePhase.GameSimulation), 16);
            NativeQueue<Entity> nativeQueue1 = new NativeQueue<Entity>((AllocatorManager.AllocatorHandle)Allocator.TempJob);
            NativeQueue<Entity> nativeQueue2 = new NativeQueue<Entity>((AllocatorManager.AllocatorHandle)Allocator.TempJob);

            this.m_daytype = WeekSystem.currentDayOfTheWeek;
            JobHandle outJobHandle;

            Time2WorkCitizenBehaviorSystem.CitizenAITickJob jobData = new Time2WorkCitizenBehaviorSystem.CitizenAITickJob()
            {
                CitizenScheduleLookup = InternalCompilerInterface.GetComponentLookup<CitizenSchedule>(ref this.__TypeHandle.CitizenScheduleLookup, ref this.CheckedStateRef),
                m_CitizenType = InternalCompilerInterface.GetComponentTypeHandle<Citizen>(ref this.__TypeHandle.__Game_Citizens_Citizen_RW_ComponentTypeHandle, ref this.CheckedStateRef),
                m_CurrentBuildingType = InternalCompilerInterface.GetComponentTypeHandle<CurrentBuilding>(ref this.__TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle, ref this.CheckedStateRef),
                m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref this.__TypeHandle.__Unity_Entities_Entity_TypeHandle, ref this.CheckedStateRef),
                m_HouseholdMemberType = InternalCompilerInterface.GetComponentTypeHandle<HouseholdMember>(ref this.__TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentTypeHandle, ref this.CheckedStateRef),
                m_UpdateFrameType = InternalCompilerInterface.GetSharedComponentTypeHandle<UpdateFrame>(ref this.__TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, ref this.CheckedStateRef),
                m_HealthProblemType = InternalCompilerInterface.GetComponentTypeHandle<HealthProblem>(ref this.__TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentTypeHandle, ref this.CheckedStateRef),
                m_TripType = InternalCompilerInterface.GetBufferTypeHandle<TripNeeded>(ref this.__TypeHandle.__Game_Citizens_TripNeeded_RW_BufferTypeHandle, ref this.CheckedStateRef),
                m_LeisureType = InternalCompilerInterface.GetComponentTypeHandle<Leisure>(ref this.__TypeHandle.__Game_Citizens_Leisure_RO_ComponentTypeHandle, ref this.CheckedStateRef),
                m_HouseholdNeeds = InternalCompilerInterface.GetComponentLookup<HouseholdNeed>(ref this.__TypeHandle.__Game_Citizens_HouseholdNeed_RW_ComponentLookup, ref this.CheckedStateRef),
                m_Households = InternalCompilerInterface.GetComponentLookup<Household>(ref this.__TypeHandle.__Game_Citizens_Household_RO_ComponentLookup, ref this.CheckedStateRef),
                m_PropertyRenters = InternalCompilerInterface.GetComponentLookup<PropertyRenter>(ref this.__TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup, ref this.CheckedStateRef),
                m_Transforms = InternalCompilerInterface.GetComponentLookup<Game.Objects.Transform>(ref this.__TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref this.CheckedStateRef),
                m_CarKeepers = InternalCompilerInterface.GetComponentLookup<CarKeeper>(ref this.__TypeHandle.__Game_Citizens_CarKeeper_RO_ComponentLookup, ref this.CheckedStateRef),
                m_PersonalCars = InternalCompilerInterface.GetComponentLookup<Game.Vehicles.PersonalCar>(ref this.__TypeHandle.__Game_Vehicles_PersonalCar_RW_ComponentLookup, ref this.CheckedStateRef),
                m_MovingAway = InternalCompilerInterface.GetComponentLookup<MovingAway>(ref this.__TypeHandle.__Game_Agents_MovingAway_RO_ComponentLookup, ref this.CheckedStateRef),
                m_Workers = InternalCompilerInterface.GetComponentLookup<Worker>(ref this.__TypeHandle.__Game_Citizens_Worker_RO_ComponentLookup, ref this.CheckedStateRef),
                m_Students = InternalCompilerInterface.GetComponentLookup<Game.Citizens.Student>(ref this.__TypeHandle.__Game_Citizens_Student_RO_ComponentLookup, ref this.CheckedStateRef),
                m_TouristHouseholds = InternalCompilerInterface.GetComponentLookup<TouristHousehold>(ref this.__TypeHandle.__Game_Citizens_TouristHousehold_RO_ComponentLookup, ref this.CheckedStateRef),
                m_HomelessHouseholds = InternalCompilerInterface.GetComponentLookup<HomelessHousehold>(ref this.__TypeHandle.__Game_Citizens_HomelessHousehold_RO_ComponentLookup, ref this.CheckedStateRef),
                m_OutsideConnections = InternalCompilerInterface.GetComponentLookup<Game.Objects.OutsideConnection>(ref this.__TypeHandle.__Game_Objects_OutsideConnection_RO_ComponentLookup, ref this.CheckedStateRef),
                m_InDangerData = InternalCompilerInterface.GetComponentLookup<InDanger>(ref this.__TypeHandle.__Game_Events_InDanger_RO_ComponentLookup, ref this.CheckedStateRef),
                m_Attendees = InternalCompilerInterface.GetBufferLookup<CoordinatedMeetingAttendee>(ref this.__TypeHandle.__Game_Citizens_CoordinatedMeetingAttendee_RO_BufferLookup, ref this.CheckedStateRef),
                m_Meetings = InternalCompilerInterface.GetComponentLookup<CoordinatedMeeting>(ref this.__TypeHandle.__Game_Citizens_CoordinatedMeeting_RW_ComponentLookup, ref this.CheckedStateRef),
                m_AttendingMeetings = InternalCompilerInterface.GetComponentLookup<AttendingMeeting>(ref this.__TypeHandle.__Game_Citizens_AttendingMeeting_RO_ComponentLookup, ref this.CheckedStateRef),
                m_MeetingDatas = InternalCompilerInterface.GetBufferLookup<HaveCoordinatedMeetingData>(ref this.__TypeHandle.__Game_Prefabs_HaveCoordinatedMeetingData_RO_BufferLookup, ref this.CheckedStateRef),
                m_Prefabs = InternalCompilerInterface.GetComponentLookup<PrefabRef>(ref this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref this.CheckedStateRef),
                m_BuildingStudents = InternalCompilerInterface.GetBufferLookup<Game.Buildings.Student>(ref this.__TypeHandle.__Game_Buildings_Student_RO_BufferLookup, ref this.CheckedStateRef),
                m_PopulationData = InternalCompilerInterface.GetComponentLookup<Population>(ref this.__TypeHandle.__Game_City_Population_RO_ComponentLookup, ref this.CheckedStateRef),
                m_OutsideConnectionDatas = InternalCompilerInterface.GetComponentLookup<OutsideConnectionData>(ref this.__TypeHandle.__Game_Prefabs_OutsideConnectionData_RO_ComponentLookup, ref this.CheckedStateRef),
                m_OwnedVehicles = InternalCompilerInterface.GetBufferLookup<OwnedVehicle>(ref this.__TypeHandle.__Game_Vehicles_OwnedVehicle_RO_BufferLookup, ref this.CheckedStateRef),
                m_CommuterHouseholds = InternalCompilerInterface.GetComponentLookup<CommuterHousehold>(ref this.__TypeHandle.__Game_Citizens_CommuterHousehold_RO_ComponentLookup, ref this.CheckedStateRef),
                m_EmployeeBufs = InternalCompilerInterface.GetBufferLookup<Employee>(ref this.__TypeHandle.__Game_Companies_Employee_RO_BufferLookup, ref this.CheckedStateRef),
                m_HouseholdArchetype = this.m_HouseholdArchetype,
                CommercialPropertyLookup = InternalCompilerInterface.GetComponentLookup<CommercialProperty>(ref this.__TypeHandle.CommercialPropertyLookup, ref this.CheckedStateRef),
                IndustrialPropertyLookup = InternalCompilerInterface.GetComponentLookup<IndustrialProperty>(ref this.__TypeHandle.IndustrialPropertyLookup, ref this.CheckedStateRef),
                OfficePropertyLookup = InternalCompilerInterface.GetComponentLookup<OfficeProperty>(ref this.__TypeHandle.OfficePropertyLookup, ref this.CheckedStateRef),
                PropertyRenterLookup = InternalCompilerInterface.GetComponentLookup<PropertyRenter>(ref this.__TypeHandle.PropertyRenterLookup, ref this.CheckedStateRef),
                PrefabRefLookup = InternalCompilerInterface.GetComponentLookup<PrefabRef>(ref this.__TypeHandle.PrefabRefLookup, ref this.CheckedStateRef),
                m_OutsideConnectionEntities = this.m_OutsideConnectionQuery.ToEntityListAsync((AllocatorManager.AllocatorHandle)Allocator.TempJob, out outJobHandle),
                m_EconomyParameters = this.m_EconomyParameterQuery.GetSingleton<EconomyParameterData>(),
                m_LeisureParameters = this.m_LeisureParameterQuery.GetSingleton<LeisureParametersData>(),
                m_CommandBuffer = this.m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
                m_UpdateFrameIndex = frameWithInterval,
                m_SimulationFrame = this.m_SimulationSystem.frameIndex,
                m_NormalizedTime = this.m_TimeSystem.normalizedTime,
                m_TimeData = this.m_TimeDataQuery.GetSingleton<Game.Common.TimeData>(),
                m_PopulationEntity = this.m_PopulationQuery.GetSingletonEntity(),
                m_CarReserverQueue = this.m_ParallelCarReserveQueue,
                m_MailSenderQueue = nativeQueue1.AsParallelWriter(),
                m_SleepQueue = nativeQueue2.AsParallelWriter(),
                m_RandomSeed = RandomSeed.Next(),
                lunch_break_pct = Mod.m_Setting.lunch_break_percentage,
                office_offdayprob = WeekSystem.getOfficeOffDayProb(),
                commercial_offdayprob = WeekSystem.getCommercialOffDayProb(),
                industry_offdayprob = WeekSystem.getIndustryOffDayProb(),
                cityservices_offdayprob = WeekSystem.getCityServicesOffDayProb(),
                school_start_time = new int3((int)Mod.m_Setting.school_start_time, (int)Mod.m_Setting.high_school_start_time, (int)Mod.m_Setting.univ_start_time),
                school_end_time = new int3((int)Mod.m_Setting.school_end_time, (int)Mod.m_Setting.high_school_end_time, (int)Mod.m_Setting.univ_end_time),
                work_start_time = (float)Mod.m_Setting.work_start_time,
                work_end_time = (float)Mod.m_Setting.work_end_time,
                school_vanilla_timeoff = Mod.m_Setting.use_school_vanilla_timeoff,
                delayFactor = (float)(Mod.m_Setting.delay_factor) / 100,
                disable_early_shop_leisure = Mod.m_Setting.disable_early_shop_leisure,
                school_offdayprob = WeekSystem.getSchoolOffDayProb(),
                ticksPerDay = Time2WorkTimeSystem.kTicksPerDay,
                part_time_prob = Mod.m_Setting.part_time_percentage,
                commute_top10 = Mod.m_Setting.commute_top10per,
                dow = this.m_daytype,
                part_time_reduction = Mod.m_Setting.avg_work_hours_pt_wd / Mod.m_Setting.avg_work_hours_ft_wd,
                overtime = ((Mod.m_Setting.avg_work_hours_ft_wd - (Mod.m_Setting.work_end_time - Mod.m_Setting.work_start_time) / 2) / 24),
                specialEventStartTime = SpecialEventSystem.startTime,
                specialEventEndTime = SpecialEventSystem.endTime,
                remote_work_prob = Mod.m_Setting.remote_percentage
            };
            JobHandle jobHandle1 = jobData.ScheduleParallel<Time2WorkCitizenBehaviorSystem.CitizenAITickJob>(this.m_CitizenQuery, JobHandle.CombineDependencies(this.m_CarReserveWriters, JobHandle.CombineDependencies(this.Dependency, outJobHandle)));
            jobData.m_OutsideConnectionEntities.Dispose(jobHandle1);
            this.m_EndFrameBarrier.AddJobHandleForProducer(jobHandle1);
            this.AddCarReserveWriter(jobHandle1);

            JobHandle jobHandle2 = new Time2WorkCitizenBehaviorSystem.CitizenReserveHouseholdCarJob()
            {
                m_CarKeepers = this.__TypeHandle.__Game_Citizens_CarKeeper_RW_ComponentLookup,
                m_HouseholdMembers = this.__TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentLookup,
                m_OwnedVehicles = this.__TypeHandle.__Game_Vehicles_OwnedVehicle_RO_BufferLookup,
                m_PersonalCars = this.__TypeHandle.__Game_Vehicles_PersonalCar_RW_ComponentLookup,
                m_Citizens = this.__TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup,
                m_ReserverQueue = this.m_CarReserveQueue
            }.Schedule<Time2WorkCitizenBehaviorSystem.CitizenReserveHouseholdCarJob>(JobHandle.CombineDependencies(jobHandle1, this.m_CarReserveWriters));

            this.m_EndFrameBarrier.AddJobHandleForProducer(jobHandle2);
            this.AddCarReserveWriter(jobHandle2);

            JobHandle jobHandle3 = new Time2WorkCitizenBehaviorSystem.CitizenTryCollectMailJob()
            {
                m_CurrentBuildingData = InternalCompilerInterface.GetComponentLookup<CurrentBuilding>(ref this.__TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentLookup, ref this.CheckedStateRef),
                m_PrefabRefData = InternalCompilerInterface.GetComponentLookup<PrefabRef>(ref this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref this.CheckedStateRef),
                m_SpawnableBuildingData = InternalCompilerInterface.GetComponentLookup<SpawnableBuildingData>(ref this.__TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup, ref this.CheckedStateRef),
                m_MailAccumulationData = InternalCompilerInterface.GetComponentLookup<MailAccumulationData>(ref this.__TypeHandle.__Game_Prefabs_MailAccumulationData_RO_ComponentLookup, ref this.CheckedStateRef),
                m_ServiceObjectData = InternalCompilerInterface.GetComponentLookup<ServiceObjectData>(ref this.__TypeHandle.__Game_Prefabs_ServiceObjectData_RO_ComponentLookup, ref this.CheckedStateRef),
                m_MailSenderData = InternalCompilerInterface.GetComponentLookup<MailSender>(ref this.__TypeHandle.__Game_Citizens_MailSender_RW_ComponentLookup, ref this.CheckedStateRef),
                m_MailProducerData = InternalCompilerInterface.GetComponentLookup<MailProducer>(ref this.__TypeHandle.__Game_Buildings_MailProducer_RW_ComponentLookup, ref this.CheckedStateRef),
                m_MailSenderQueue = nativeQueue1
            }.Schedule<Time2WorkCitizenBehaviorSystem.CitizenTryCollectMailJob>(jobHandle1);

            this.m_EndFrameBarrier.AddJobHandleForProducer(jobHandle3);
            nativeQueue1.Dispose(jobHandle3);

            JobHandle jobHandle4 = new Time2WorkCitizenBehaviorSystem.CitizeSleepJob()
            {
                m_CurrentBuildingData = InternalCompilerInterface.GetComponentLookup<CurrentBuilding>(ref this.__TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentLookup, ref this.CheckedStateRef),
                m_CitizenPresenceData = InternalCompilerInterface.GetComponentLookup<CitizenPresence>(ref this.__TypeHandle.__Game_Buildings_CitizenPresence_RW_ComponentLookup, ref this.CheckedStateRef),
                m_SleepQueue = nativeQueue2
            }.Schedule<Time2WorkCitizenBehaviorSystem.CitizeSleepJob>(jobHandle1);
            nativeQueue2.Dispose(jobHandle4);
            this.Dependency = JobHandle.CombineDependencies(jobHandle2, jobHandle3, jobHandle4);
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

        [UnityEngine.Scripting.Preserve]
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
            public ComponentLookup<CitizenSchedule> CitizenScheduleLookup;
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
            public ComponentLookup<PropertyRenter> m_PropertyRenters;
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
            public ComponentLookup<HomelessHousehold> m_HomelessHouseholds;
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
            public BufferLookup<Employee> m_EmployeeBufs;
            [ReadOnly]
            public EntityArchetype m_HouseholdArchetype;
            [ReadOnly]
            public NativeList<Entity> m_OutsideConnectionEntities;
            [ReadOnly]
            public EconomyParameterData m_EconomyParameters;
            [ReadOnly]
            public LeisureParametersData m_LeisureParameters;
            [ReadOnly]
            public ComponentLookup<CommercialProperty> CommercialPropertyLookup;
            [ReadOnly]
            public ComponentLookup<IndustrialProperty> IndustrialPropertyLookup;
            [ReadOnly]
            public ComponentLookup<OfficeProperty> OfficePropertyLookup;
            [ReadOnly]
            public ComponentLookup<PropertyRenter> PropertyRenterLookup;
            [ReadOnly]
            public ComponentLookup<PrefabRef> PrefabRefLookup;
            public uint m_UpdateFrameIndex;
            public float m_NormalizedTime;
            public uint m_SimulationFrame;
            public EntityCommandBuffer.ParallelWriter m_CommandBuffer;
            public NativeQueue<Entity>.ParallelWriter m_CarReserverQueue;
            public NativeQueue<Entity>.ParallelWriter m_MailSenderQueue;
            public NativeQueue<Entity>.ParallelWriter m_SleepQueue;
            public Game.Common.TimeData m_TimeData;
            public Entity m_PopulationEntity;
            public RandomSeed m_RandomSeed;
            public int lunch_break_pct;
            public float4 office_offdayprob;
            public float4 commercial_offdayprob;
            public float4 industry_offdayprob;
            public float4 cityservices_offdayprob;
            public int3 school_start_time;
            public int3 school_end_time;
            public float work_start_time;
            public float work_end_time;
            public bool school_vanilla_timeoff;
            public float delayFactor;
            public bool disable_early_shop_leisure;
            public float3 school_offdayprob;
            public int ticksPerDay;
            public int part_time_prob;
            public float commute_top10;
            public Setting.DTSimulationEnum dow;
            public float overtime;
            public float part_time_reduction;
            public float3 specialEventStartTime;
            public float3 specialEventEndTime;
            public int remote_work_prob;


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
              float2 time2WorkOrStudy)
            {
                float2 time2Sleep;
                bool sleepTime = Time2WorkCitizenBehaviorSystem.IsSleepTime(entity, citizen, ref economyParameters, this.m_NormalizedTime, ref this.m_Workers, ref this.m_Students, time2WorkOrStudy, out time2Sleep);
                if (!(home != Entity.Null) || !sleepTime)
                    return false;
                if (currentBuilding == home)
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
                    double threshold_go_home = Math.Min(Math.Abs(time2Sleep.x - this.m_NormalizedTime), Math.Abs(1 - (time2Sleep.x - this.m_NormalizedTime)));

                    //Mod.log.Info($"Going home. TIme:{this.m_NormalizedTime}, Threshold:{threshold_go_home}, sleeptime:{sleepTime}");


                    if (threshold_go_home <= 0.03)
                    {
                        this.GoHome(entity, home, trips, currentBuilding);
                    }
                }
                return true;
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
              Entity targetBuilding,
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
                    if (targetBuilding == Entity.Null)
                    {
                        OutsideConnectionTransferType ocTransferType = OutsideConnectionTransferType.Train | OutsideConnectionTransferType.Air | OutsideConnectionTransferType.Ship;
                        if (this.m_OwnedVehicles.HasBuffer(household) && this.m_OwnedVehicles[household].Length > 0)
                            ocTransferType |= OutsideConnectionTransferType.Road;
                        BuildingUtils.GetRandomOutsideConnectionByTransferType(ref this.m_OutsideConnectionEntities, ref this.m_OutsideConnectionDatas, ref this.m_Prefabs, random, ocTransferType, out targetBuilding);
                    }
                    if (targetBuilding == Entity.Null && this.m_OutsideConnectionEntities.Length != 0)
                    {
                        targetBuilding = this.m_OutsideConnectionEntities[random.NextInt(this.m_OutsideConnectionEntities.Length)];
                    }
                    trips.Add(new TripNeeded()
                    {
                        m_TargetAgent = targetBuilding,
                        m_Purpose = purpose
                    });
                }
                else
                {
                    if (purpose != Game.Citizens.Purpose.MovingAway)
                        return;
                    citizen.m_State |= CitizenFlags.MovingAwayReachOC;
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
              Entity citizenEntity,
              Entity householdEntity,
              Entity currentBuilding,
              Entity homeEntity,
              bool isTourist,
              ref Citizen citizenData,
              int population,
              int ticksPerDay,
              ref Unity.Mathematics.Random random,
              ref EconomyParameterData economyParameters,
              bool specialEvent,
              float2 timeToWork,
              float2 timeToLunch,
              bool lunchTime)
            {
                bool flag = CitizenUtils.HasMovedIn(householdEntity, this.m_Households) && homeEntity == Entity.Null;
                if (isTourist)
                {
                    if (this.m_OutsideConnections.HasComponent(currentBuilding) && this.m_TouristHouseholds[householdEntity].m_Hotel != Entity.Null)
                        return false;
                }
                else if (!flag)
                {
                    int num = 128 - (int)citizenData.m_LeisureCounter;
                    if (specialEvent)
                    {
                        num += 15;
                    }
                    if (this.m_OutsideConnections.HasComponent(currentBuilding) || random.NextInt(this.m_LeisureParameters.m_LeisureRandomFactor) > num)
                        return false;
                }
                int leisureProb = Time2WorkCitizenBehaviorSystem.kMinLeisurePossibility;
                if (specialEvent)
                {
                    leisureProb += 15;
                }
                int num1 = math.min(leisureProb, Mathf.RoundToInt(200f / math.max(1f, math.sqrt(economyParameters.m_TrafficReduction * (float)population))));
                if (!isTourist && !flag && random.NextInt(100) > num1)
                {
                    citizenData.m_LeisureCounter = byte.MaxValue;
                    return true;
                }
                float x = this.GetTimeLeftUntilInterval(Time2WorkCitizenBehaviorSystem.GetSleepTime(citizenEntity, citizenData, ref economyParameters, ref this.m_Workers, ref this.m_Students, timeToWork)); ;
                if (this.m_Workers.HasComponent(citizenEntity))
                {
                    Worker worker = this.m_Workers[citizenEntity];

                    if (lunchTime && timeToLunch.x > 0)
                    {
                        float x1 = math.min(this.GetTimeLeftUntilInterval(timeToLunch), this.GetTimeLeftUntilInterval(timeToWork));
                        x = math.min(x, x1);
                    }
                    else
                    {
                        x = math.min(x, this.GetTimeLeftUntilInterval(timeToWork));
                    }
                }
                else
                {
                    if (this.m_Students.HasComponent(citizenEntity))
                    {
                        Game.Citizens.Student student = this.m_Students[citizenEntity];
                        float2 timeToStudy = timeToWork;
                        x = this.GetTimeLeftUntilInterval(Time2WorkCitizenBehaviorSystem.GetSleepTime(citizenEntity, citizenData, ref economyParameters, ref this.m_Workers, ref this.m_Students, timeToStudy));
                        x = math.min(x, this.GetTimeLeftUntilInterval(timeToStudy));
                    }
                }
                if (isTourist)
                    citizenData.m_LeisureCounter = (byte)0;
                uint num2 = (uint)((double)x * ticksPerDay);
                Leisure component = new Leisure()
                {
                    m_LastPossibleFrame = this.m_SimulationFrame + num2
                };
                this.m_CommandBuffer.AddComponent<Leisure>(chunkIndex, citizenEntity, component);
                return true;
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
                if (!this.m_CarKeepers.IsComponentEnabled(entity))
                {
                    this.m_CarReserverQueue.Enqueue(entity);
                }
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
                                if (coordinatedMeetingData.m_TravelPurpose.m_Purpose == Game.Citizens.Purpose.Shopping)
                                {
                                    float3 position = this.m_Transforms[currentBuilding].m_Position;
                                    this.GoShopping(chunkIndex, entity, household, new HouseholdNeed()
                                    {
                                        m_Resource = coordinatedMeetingData.m_TravelPurpose.m_Resource,
                                        m_Amount = coordinatedMeetingData.m_TravelPurpose.m_Data
                                    }, position);
                                    return true;
                                }
                                if (coordinatedMeetingData.m_TravelPurpose.m_Purpose == Game.Citizens.Purpose.Traveling)
                                {
                                    Citizen citizen1 = new Citizen();
                                    this.GoToOutsideConnection(entity, household, currentBuilding, Entity.Null, ref citizen1, trips, coordinatedMeetingData.m_TravelPurpose.m_Purpose, ref random);
                                }
                                else if (coordinatedMeetingData.m_TravelPurpose.m_Purpose == Game.Citizens.Purpose.GoingHome)
                                {
                                    if (this.m_PropertyRenters.HasComponent(household))
                                    {
                                        meeting2.m_Target = this.m_PropertyRenters[household].m_Property;
                                        this.m_Meetings[meeting1] = meeting2;
                                        this.GoHome(entity, this.m_PropertyRenters[household].m_Property, trips, currentBuilding);
                                    }
                                }
                                else
                                {
                                    trips.Add(new TripNeeded()
                                    {
                                        m_Purpose = coordinatedMeetingData.m_TravelPurpose.m_Purpose,
                                        m_Resource = coordinatedMeetingData.m_TravelPurpose.m_Resource,
                                        m_Data = coordinatedMeetingData.m_TravelPurpose.m_Data,
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
                                    if (meeting2.m_Target != Entity.Null && currentBuilding != meeting2.m_Target && (!this.m_PropertyRenters.HasComponent(meeting2.m_Target) || this.m_PropertyRenters[meeting2.m_Target].m_Property != currentBuilding))
                                        trips.Add(new TripNeeded()
                                        {
                                            m_Purpose = coordinatedMeetingData.m_TravelPurpose.m_Purpose,
                                            m_Resource = coordinatedMeetingData.m_TravelPurpose.m_Resource,
                                            m_Data = coordinatedMeetingData.m_TravelPurpose.m_Data,
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
                NativeArray<HealthProblem> nativeArray5 = chunk.GetNativeArray<HealthProblem>(ref this.m_HealthProblemType);
                BufferAccessor<TripNeeded> bufferAccessor = chunk.GetBufferAccessor<TripNeeded>(ref this.m_TripType);
                bool flag1 = nativeArray5.Length > 0;
                int population = this.m_PopulationData[this.m_PopulationEntity].m_Population;

                //Check if there is a special event happening
                bool specialEvent = false;
                for (int i = 0; i < 3; i++)
                {
                    if (m_NormalizedTime >= specialEventStartTime[i] && m_NormalizedTime <= specialEventEndTime[i])
                    {
                        specialEvent = true;
                    }
                    //Mod.log.Info($"i:{i},m_NormalizedTime:{m_NormalizedTime},{specialEventStartTime[i]},{specialEventEndTime[i]},{specialEvent}");
                }

                for (int index = 0; index < nativeArray1.Length; ++index)
                {
                    Citizen citizen = nativeArray2[index];
                    if (!flag1 || !CitizenUtils.IsDead(nativeArray5[index]))
                    {
                        Entity household = nativeArray3[index].m_Household;
                        Entity entity1 = nativeArray1[index];
                        bool isTourist = this.m_TouristHouseholds.HasComponent(household);
                        bool flag2 = this.m_HomelessHouseholds.HasComponent(household);
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
                            UnityEngine.Debug.LogWarning((object)$"Citizen:{entity1.Index} don't have valid household");
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
                                if (currentBuilding == Entity.Null && this.m_MovingAway.HasComponent(household))
                                {
                                    this.m_CommandBuffer.AddComponent<Deleted>(unfilteredChunkIndex, household, new Deleted());
                                }
                                else
                                {
                                    if (this.m_Transforms.HasComponent(currentBuilding) && (!this.m_InDangerData.HasComponent(currentBuilding) || (this.m_InDangerData[currentBuilding].m_Flags & DangerFlags.StayIndoors) == (DangerFlags)0))
                                    {
                                        bool flag3 = (citizen.m_State & CitizenFlags.Commuter) != 0;
                                        CitizenAge age = citizen.GetAge();
                                        if (flag3 && (age == CitizenAge.Elderly || age == CitizenAge.Child))
                                        {
                                            this.m_CommandBuffer.AddComponent<Deleted>(unfilteredChunkIndex, entity1, new Deleted());
                                        }
                                        if ((citizen.m_State & CitizenFlags.MovingAwayReachOC) != CitizenFlags.None)
                                        {
                                            this.m_CommandBuffer.AddComponent<Deleted>(unfilteredChunkIndex, entity1, new Deleted());
                                        }
                                        else
                                        {
                                            MovingAway componentData1;
                                            if (this.m_MovingAway.TryGetComponent(household, out componentData1))
                                            {
                                                this.GoToOutsideConnection(entity1, household, currentBuilding, componentData1.m_Target, ref citizen, trips, Game.Citizens.Purpose.MovingAway, ref random);
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
                                                if (this.m_PropertyRenters.HasComponent(household))
                                                {
                                                    entity3 = this.m_PropertyRenters[household].m_Property;
                                                }
                                                else if (flag2)
                                                {
                                                    entity3 = this.m_HomelessHouseholds[household].m_TempHome;
                                                }
                                                else if (isTourist)
                                                {
                                                    Entity hotel = this.m_TouristHouseholds[household].m_Hotel;
                                                    if (this.m_PropertyRenters.HasComponent(hotel))
                                                    {
                                                        entity3 = this.m_PropertyRenters[hotel].m_Property;
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
                                                        CommuterHousehold componentData2;
                                                        if (this.m_CommuterHouseholds.TryGetComponent(household, out componentData2))
                                                            entity3 = componentData2.m_OriginalFrom;
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
                                                        int day = Time2WorkTimeSystem.GetDay(m_SimulationFrame, m_TimeData, ticksPerDay);

                                                        float2 time2Lunch = new float2(-1f, -1f);
                                                        float2 time2Work = new float2(-1f, -1f);
                                                        bool workFromHome = false;
                                                        bool lunchTime = false;
                                                        bool workTime = false;
                                                        float start_work = 0f;
                                                        bool dayOff = false;

                                                        bool hasSchedule = this.CitizenScheduleLookup.HasComponent(entity1);
                                                        if (hasSchedule)
                                                        {
                                                            CitizenSchedule citizenSchedule = this.CitizenScheduleLookup[entity1];
                                                            time2Lunch = new float2(citizenSchedule.start_lunch, citizenSchedule.end_lunch);
                                                            time2Work = new float2(citizenSchedule.go_to_work, citizenSchedule.end_work);
                                                            workFromHome = citizenSchedule.work_from_home;
                                                            lunchTime = Time2WorkWorkerSystem.IsLunchTime(this.m_NormalizedTime, time2Lunch);
                                                            workTime = Time2WorkWorkerSystem.IsTimeToWork(this.m_NormalizedTime, time2Work);
                                                            start_work = citizenSchedule.start_work;
                                                            dayOff = citizenSchedule.dayoff;
                                                        }


                                                        if (this.m_Workers.HasComponent(entity1) && !dayOff && workTime || this.m_Students.HasComponent(entity1) && workTime && !dayOff)
                                                        {
                                                           
                                                            //Filtering work times that correspond to part time shifts. Those do not take lunch breaks
                                                            if (this.m_Workers.HasComponent(entity1) && time2Work.x <= 0.55f && time2Work.y > 0.55f && lunchTime)
                                                            {
                                                                HouseholdNeed householdNeed = this.m_HouseholdNeeds[household];

                                                                int num = 50;
                                                                Unity.Mathematics.Random rand = Unity.Mathematics.Random.CreateFromIndex((uint)(citizen.m_PseudoRandom));
                                                                int prob = rand.NextInt(100);
                                                                if ((householdNeed.m_Resource != Resource.NoResource || num < prob) && this.m_Transforms.HasComponent(currentBuilding))
                                                                {
                                                                    if (householdNeed.m_Resource == Resource.NoResource)
                                                                    {
                                                                        if (prob < 25)
                                                                        {
                                                                            householdNeed.m_Resource = Resource.Meals;
                                                                        }
                                                                        else
                                                                        {
                                                                            if (prob < 35)
                                                                            {
                                                                                householdNeed.m_Resource = Resource.Food;
                                                                            }
                                                                            else
                                                                            {
                                                                                if (prob < 45)
                                                                                {
                                                                                    householdNeed.m_Resource = Resource.ConvenienceFood;
                                                                                }
                                                                                else
                                                                                {
                                                                                    householdNeed.m_Resource = Resource.Beverages;
                                                                                }
                                                                            }
                                                                        }
                                                                        householdNeed.m_Amount = rand.NextInt(1, 5);
                                                                    }
                                                                    this.GoShopping(unfilteredChunkIndex, entity1, household, householdNeed, this.m_Transforms[currentBuilding].m_Position);
                                                                    householdNeed.m_Resource = Resource.NoResource;
                                                                    this.m_HouseholdNeeds[household] = householdNeed;
                                                                    if (chunk.Has<Leisure>(ref this.m_LeisureType))
                                                                    {
                                                                        this.m_CommandBuffer.RemoveComponent<Leisure>(unfilteredChunkIndex, entity1);
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    if (!chunk.Has<Leisure>(ref this.m_LeisureType) && this.DoLeisure(unfilteredChunkIndex, entity1, household, currentBuilding, entity3, isTourist, ref citizen, population, ticksPerDay, ref random, ref this.m_EconomyParameters, specialEvent, time2Work, time2Lunch, lunchTime))
                                                                    {
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
                                                            if (this.CheckSleep(index, entity1, ref citizen, currentBuilding, household, entity3, trips, ref this.m_EconomyParameters, ref random, time2Work))
                                                            {
                                                                if (chunk.Has<Leisure>(ref this.m_LeisureType))
                                                                {
                                                                    this.m_CommandBuffer.RemoveComponent<Leisure>(unfilteredChunkIndex, entity1);
                                                                }
                                                            }
                                                            else
                                                            {
                                                                //If worker and in the afternoon, go home first and maybe shop or do leisure later
                                                                if (this.m_Workers.HasComponent(entity1) || this.m_Students.HasComponent(entity1))
                                                                {
                                                                    int num = 65;
                                                                    if ((int)dow != (int)Setting.DTSimulationEnum.Weekday)
                                                                    {
                                                                        num = 55;
                                                                    }
                                                                    int prob = random.NextInt(100);
                                                                    if (this.m_NormalizedTime > 0.65f && num < prob)
                                                                    {
                                                                        if ((this.m_Workers.HasComponent(entity1) && currentBuilding == this.m_Workers[entity1].m_Workplace)
                                                                            || (this.m_Students.HasComponent(entity1) && currentBuilding == this.m_Students[entity1].m_School))
                                                                        {
                                                                            this.GoHome(entity1, entity3, trips, currentBuilding);
                                                                        }
                                                                    }
                                                                }

                                                                //If today is off day, go shopping or for leisure around 10 AM
                                                                float x1 = (float)(GaussianRandom.NextGaussianDouble(random)) * 0.05f + 0.5f;
                                                                float x2 = 1 - Math.Abs((float)(GaussianRandom.NextGaussianDouble(random)) * 0.05f);

                                                                if ((this.m_NormalizedTime > x1 && this.m_NormalizedTime < x2) || !disable_early_shop_leisure)
                                                                {
                                                                    //If in the morning, cim might stay home and go out later
                                                                    if (!disable_early_shop_leisure || (this.m_NormalizedTime <= 0.5 && (random.NextInt(100) < 20)) || this.m_NormalizedTime > 0.5)
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
                                                                }
                                                            }

                                                            if (!chunk.Has<Leisure>(ref this.m_LeisureType) && this.DoLeisure(unfilteredChunkIndex, entity1, household, currentBuilding, entity3, isTourist, ref citizen, population, ticksPerDay, ref random, ref this.m_EconomyParameters, specialEvent, time2Work, time2Lunch, lunchTime))
                                                            {
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
            public ComponentLookup<HomelessHousehold> __Game_Citizens_HomelessHousehold_RO_ComponentLookup;
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
            [ReadOnly]
            public BufferLookup<Employee> __Game_Companies_Employee_RO_BufferLookup;
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
            [ReadOnly]
            public ComponentLookup<CommercialProperty> CommercialPropertyLookup;
            [ReadOnly]
            public ComponentLookup<IndustrialProperty> IndustrialPropertyLookup;
            [ReadOnly]
            public ComponentLookup<OfficeProperty> OfficePropertyLookup;
            [ReadOnly]
            public ComponentLookup<PropertyRenter> PropertyRenterLookup;
            [ReadOnly]
            public ComponentLookup<PrefabRef> PrefabRefLookup;
            [ReadOnly]
            public ComponentLookup<CitizenSchedule> CitizenScheduleLookup;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
                this.__Game_Citizens_HomelessHousehold_RO_ComponentLookup = state.GetComponentLookup<HomelessHousehold>(true);
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
                this.__Game_Companies_Employee_RO_BufferLookup = state.GetBufferLookup<Employee>(true);
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
                this.CommercialPropertyLookup = state.GetComponentLookup<CommercialProperty>(true);
                this.IndustrialPropertyLookup = state.GetComponentLookup<IndustrialProperty>(true);
                this.OfficePropertyLookup = state.GetComponentLookup<OfficeProperty>(true);
                this.PropertyRenterLookup = state.GetComponentLookup<PropertyRenter>(true);
                this.PrefabRefLookup = state.GetComponentLookup<PrefabRef>(true);
                this.CitizenScheduleLookup = state.GetComponentLookup<CitizenSchedule>(true);
            }
        }
    }
}
