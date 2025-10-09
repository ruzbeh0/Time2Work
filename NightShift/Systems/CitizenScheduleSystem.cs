using Game;
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
using Unity.Entities.UniversalDelegates;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.PlayerLoop;
using static Time2Work.Time2WorkWorkerSystem;
using Student = Game.Citizens.Student;

namespace Time2Work.Systems
{
    [UpdateAfter(typeof(WeekSystem))]
    public partial class CitizenScheduleSystem : GameSystemBase
    {

        private EntityQuery m_AllCitizenScheduleQuery;
        private EntityQuery m_NewCitizenScheduleQuery;
        private EntityQuery m_TimeDataQuery;
        private EntityQuery m_EconomyParameterQuery;
        private SimulationSystem m_SimulationSystem;
        private Time2WorkTimeSystem m_TimeSystem;
        private EndFrameBarrier m_EndFrameBarrier;
        private CitizenScheduleSystem.TypeHandle __TypeHandle;
        private Setting.DTSimulationEnum m_daytype;
        private int lastUpdatedDay = -1;


        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            // One day (or month) in-game is '262144' ticks
            return 262144 / 512;
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            this.m_SimulationSystem = this.World.GetOrCreateSystemManaged<SimulationSystem>();
            this.m_TimeSystem = this.World.GetOrCreateSystemManaged<Time2WorkTimeSystem>();
            this.m_EndFrameBarrier = this.World.GetOrCreateSystemManaged<EndFrameBarrier>();
            this.m_EconomyParameterQuery = this.GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
            this.m_AllCitizenScheduleQuery = this.GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[2]
             {
                   ComponentType.ReadOnly<Citizen>(),
                   ComponentType.ReadWrite<CitizenSchedule>(),
             },
                Any = new ComponentType[2]
               {
                    ComponentType.ReadOnly<Worker>(),
                    ComponentType.ReadOnly<Game.Citizens.Student>(),
               },
                None = new ComponentType[2]
             {
                ComponentType.Exclude<Deleted>(),
                ComponentType.Exclude<Temp>(),
             }
            });
            this.m_NewCitizenScheduleQuery = this.GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[1]
             {
                   ComponentType.ReadOnly<Citizen>(),
                   
             },
                Any = new ComponentType[2]
               {
                    ComponentType.ReadOnly<Worker>(),
                    ComponentType.ReadOnly<Game.Citizens.Student>(),
               },
                None = new ComponentType[3]
             {
                ComponentType.Exclude<Deleted>(),
                ComponentType.Exclude<Temp>(),
                ComponentType.Exclude<CitizenSchedule>(),
             }
            });
            this.m_TimeDataQuery = this.GetEntityQuery(ComponentType.ReadOnly<Game.Common.TimeData>());
            this.RequireAnyForUpdate(m_AllCitizenScheduleQuery, m_NewCitizenScheduleQuery);
            this.m_daytype = WeekSystem.currentDayOfTheWeek;

            Mod.log.Info("CitizenScheduleSystem Created");
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            if (!WeekSystem.initialized)
            {
                return; // Skip update until WeekSystem has run
            }


            DateTime currentDateTime = m_TimeSystem.GetCurrentDateTime();
            int day = currentDateTime.Day;

            //Mod.log.Info("CitizenScheduleSystem OnUpdate");
            uint frameWithInterval = SimulationUtils.GetUpdateFrameWithInterval(this.m_SimulationSystem.frameIndex, (uint)this.GetUpdateInterval(SystemUpdatePhase.GameSimulation), 16);
            this.__TypeHandle.__Game_City_Population_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.WorkerTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.StudentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_Citizen_RW_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_CitizenSchedule_RW_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.CitizenScheduleLookup.Update(ref this.CheckedStateRef);
            this.m_daytype = WeekSystem.currentDayOfTheWeek;

            JobHandle jobHandle;

            //Run after work shift update system
            if (currentDateTime.Hour == 3 && currentDateTime.Minute > 4 && currentDateTime.Minute < 9 && lastUpdatedDay != day)
            {
                //Refresh All Schedules
                Mod.log.Info($"Recalculating All Schedules - Normalized Time:{m_TimeSystem.normalizedTime}");
                CitizenScheduleSystem.AllCitizenScheduleJob jobDataAll = new CitizenScheduleSystem.AllCitizenScheduleJob()
                    {
                        m_CitizenType = this.__TypeHandle.__Game_Citizens_Citizen_RW_ComponentTypeHandle,
                        m_CitizenSchedule = this.__TypeHandle.__Game_Citizens_CitizenSchedule_RW_ComponentTypeHandle,
                        m_EntityType = this.__TypeHandle.__Unity_Entities_Entity_TypeHandle,
                        m_PropertyRenters = this.__TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup,
                    m_WorkerType = __TypeHandle.WorkerTypeHandle,
                    m_StudentType = __TypeHandle.StudentTypeHandle,
                    m_Prefabs = this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup,
                        m_PopulationData = this.__TypeHandle.__Game_City_Population_RO_ComponentLookup,
                        CommercialPropertyLookup = this.__TypeHandle.CommercialPropertyLookup,
                        IndustrialPropertyLookup = this.__TypeHandle.IndustrialPropertyLookup,
                        OfficePropertyLookup = this.__TypeHandle.OfficePropertyLookup,
                        PropertyRenterLookup = this.__TypeHandle.PropertyRenterLookup,
                        PrefabRefLookup = this.__TypeHandle.PrefabRefLookup,
                        m_CommandBuffer = this.m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
                        m_UpdateFrameIndex = frameWithInterval,
                        m_SimulationFrame = this.m_SimulationSystem.frameIndex,
                        m_NormalizedTime = this.m_TimeSystem.normalizedTime,
                        m_TimeData = this.m_TimeDataQuery.GetSingleton<Game.Common.TimeData>(),
                        m_EconomyParameters = this.m_EconomyParameterQuery.GetSingleton<EconomyParameterData>(),
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
                jobHandle = jobDataAll.ScheduleParallel<CitizenScheduleSystem.AllCitizenScheduleJob>(this.m_AllCitizenScheduleQuery, this.Dependency);
                this.Dependency = jobHandle;
                lastUpdatedDay = day;
            }
            else
            {
                //Mod.log.Info($"Recalculating New Schedules - Normalized Time:{m_TimeSystem.normalizedTime}, st: {(float)Mod.m_Setting.work_start_time}, et:{(float)Mod.m_Setting.work_end_time}");
                CitizenScheduleSystem.NewCitizenScheduleJob jobDataNew = new CitizenScheduleSystem.NewCitizenScheduleJob()
                {
                    m_CitizenType = this.__TypeHandle.__Game_Citizens_Citizen_RW_ComponentTypeHandle,
                    m_CitizenSchedule = this.__TypeHandle.__Game_Citizens_CitizenSchedule_RW_ComponentTypeHandle,
                    m_EntityType = this.__TypeHandle.__Unity_Entities_Entity_TypeHandle,
                    m_PropertyRenters = this.__TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup,
                    m_WorkerType = __TypeHandle.WorkerTypeHandle,
                    m_StudentType = __TypeHandle.StudentTypeHandle,
                    m_Prefabs = this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup,
                    m_PopulationData = this.__TypeHandle.__Game_City_Population_RO_ComponentLookup,
                    CommercialPropertyLookup = this.__TypeHandle.CommercialPropertyLookup,
                    IndustrialPropertyLookup = this.__TypeHandle.IndustrialPropertyLookup,
                    OfficePropertyLookup = this.__TypeHandle.OfficePropertyLookup,
                    PropertyRenterLookup = this.__TypeHandle.PropertyRenterLookup,
                    PrefabRefLookup = this.__TypeHandle.PrefabRefLookup,
                    m_CommandBuffer = this.m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
                    m_UpdateFrameIndex = frameWithInterval,
                    m_SimulationFrame = this.m_SimulationSystem.frameIndex,
                    m_NormalizedTime = this.m_TimeSystem.normalizedTime,
                    m_TimeData = this.m_TimeDataQuery.GetSingleton<Game.Common.TimeData>(),
                    m_EconomyParameters = this.m_EconomyParameterQuery.GetSingleton<EconomyParameterData>(),
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
                jobHandle = jobDataNew.ScheduleParallel<CitizenScheduleSystem.NewCitizenScheduleJob>(this.m_NewCitizenScheduleQuery, this.Dependency);
                this.Dependency = jobHandle;
            }
            this.m_EndFrameBarrier.AddJobHandleForProducer(this.Dependency);
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
        public CitizenScheduleSystem()
        {
        }


        [BurstCompile]
        private struct AllCitizenScheduleJob : IJobChunk
        {
            [ReadOnly]
            public EntityTypeHandle m_EntityType;
            public ComponentTypeHandle<Citizen> m_CitizenType;
            public ComponentTypeHandle<CitizenSchedule> m_CitizenSchedule;
            [ReadOnly] public ComponentTypeHandle<Worker> m_WorkerType;
            [ReadOnly] public ComponentTypeHandle<Game.Citizens.Student> m_StudentType;
            [ReadOnly]
            public ComponentLookup<PropertyRenter> m_PropertyRenters;
            [ReadOnly]
            public ComponentLookup<PrefabRef> m_Prefabs;
            [ReadOnly]
            public BufferLookup<Game.Buildings.Student> m_BuildingStudents;
            [ReadOnly]
            public ComponentLookup<Population> m_PopulationData;
            [ReadOnly]
            public ComponentLookup<CommuterHousehold> m_CommuterHouseholds;
            [ReadOnly]
            public EconomyParameterData m_EconomyParameters;
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
            public Game.Common.TimeData m_TimeData;
            public Entity m_PopulationEntity;
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
            public NativeArray<float> specialEventStartTime;
            public NativeArray<float> specialEventEndTime;
            public int remote_work_prob;


            public void Execute(
              in ArchetypeChunk chunk,
              int unfilteredChunkIndex,
              bool useEnabledMask,
              in v128 chunkEnabledMask)
            {
                NativeArray<Entity> nativeArray1 = chunk.GetNativeArray(this.m_EntityType);
                NativeArray<Citizen> nativeArray2 = chunk.GetNativeArray<Citizen>(ref this.m_CitizenType);
                NativeArray<CitizenSchedule> nativeArrayCitizenSchedule = chunk.GetNativeArray<CitizenSchedule>(ref this.m_CitizenSchedule);
                int population = this.m_PopulationData[this.m_PopulationEntity].m_Population;

                bool hasWorker = chunk.Has(ref m_WorkerType);
                bool hasStudent = chunk.Has(ref m_StudentType);

                NativeArray<Worker> workers = default;
                NativeArray<Student> students = default;

                if (hasWorker) workers = chunk.GetNativeArray(ref m_WorkerType);
                if (hasStudent) students = chunk.GetNativeArray(ref m_StudentType);

                for (int index = 0; index < nativeArray1.Length; ++index)
                {
                    Entity entity1 = nativeArray1[index];
                    Citizen citizen = nativeArray2[index];
                    CitizenSchedule citizenSchedule = nativeArrayCitizenSchedule[index];

                    bool isWorker = hasWorker;
                    Worker workerData = isWorker ? workers[index] : default;
                    Student studentData = !isWorker ? students[index] : default;

                    citizenSchedule = CitizenScheduleHelper.CalculateScheduleForCitizen(
                        entity1, citizen, isWorker, workerData, studentData, PrefabRefLookup, PropertyRenterLookup,
                        CommercialPropertyLookup, IndustrialPropertyLookup, OfficePropertyLookup,
                        m_PopulationData, m_EconomyParameters, population, m_NormalizedTime,
                        m_SimulationFrame, m_TimeData, ticksPerDay, lunch_break_pct,
                        office_offdayprob, commercial_offdayprob, industry_offdayprob, cityservices_offdayprob,
                        school_start_time, school_end_time, work_start_time, work_end_time, school_vanilla_timeoff,
                        delayFactor, disable_early_shop_leisure, school_offdayprob, part_time_prob,
                        commute_top10, dow, overtime, part_time_reduction, specialEventStartTime, specialEventEndTime,
                        remote_work_prob,  ref citizenSchedule);

                    nativeArrayCitizenSchedule[index] = citizenSchedule;
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

        [BurstCompile]
        private struct NewCitizenScheduleJob : IJobChunk
        {
            [ReadOnly]
            public EntityTypeHandle m_EntityType;
            public ComponentTypeHandle<Citizen> m_CitizenType;
            public ComponentTypeHandle<CitizenSchedule> m_CitizenSchedule;
            [ReadOnly] public ComponentTypeHandle<Worker> m_WorkerType;
            [ReadOnly] public ComponentTypeHandle<Student> m_StudentType;
            [ReadOnly]
            public ComponentLookup<PropertyRenter> m_PropertyRenters;
            [ReadOnly]
            public ComponentLookup<PrefabRef> m_Prefabs;
            [ReadOnly]
            public BufferLookup<Game.Buildings.Student> m_BuildingStudents;
            [ReadOnly]
            public ComponentLookup<Population> m_PopulationData;
            [ReadOnly]
            public ComponentLookup<CommuterHousehold> m_CommuterHouseholds;
            [ReadOnly]
            public EconomyParameterData m_EconomyParameters;
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
            public Game.Common.TimeData m_TimeData;
            public Entity m_PopulationEntity;
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
            public NativeArray<float> specialEventStartTime;
            public NativeArray<float> specialEventEndTime;
            public int remote_work_prob;


            public void Execute(
              in ArchetypeChunk chunk,
              int unfilteredChunkIndex,
              bool useEnabledMask,
              in v128 chunkEnabledMask)
            {
                NativeArray<Entity> nativeArray1 = chunk.GetNativeArray(this.m_EntityType);
                NativeArray<Citizen> nativeArray2 = chunk.GetNativeArray<Citizen>(ref this.m_CitizenType);
                int population = this.m_PopulationData[this.m_PopulationEntity].m_Population;

                bool hasWorker = chunk.Has(ref m_WorkerType);
                bool hasStudent = chunk.Has(ref m_StudentType);

                NativeArray<Worker> workers = default;
                NativeArray<Student> students = default;

                if (hasWorker) workers = chunk.GetNativeArray(ref m_WorkerType);
                if (hasStudent) students = chunk.GetNativeArray(ref m_StudentType);

                for (int index = 0; index < nativeArray1.Length; ++index)
                {
                    Entity entity1 = nativeArray1[index];
                    Citizen citizen = nativeArray2[index];
                    var schedule = CitizenSchedule.CreateDefault();

                    bool isWorker = hasWorker;
                    Worker workerData = isWorker ? workers[index] : default;
                    Student studentData = !isWorker ? students[index] : default;

                    CitizenSchedule citizenSchedule = CitizenScheduleHelper.CalculateScheduleForCitizen(
                        entity1, citizen, isWorker, workerData, studentData, PrefabRefLookup, PropertyRenterLookup,
                        CommercialPropertyLookup, IndustrialPropertyLookup, OfficePropertyLookup,
                        m_PopulationData, m_EconomyParameters, population, m_NormalizedTime,
                        m_SimulationFrame, m_TimeData, ticksPerDay, lunch_break_pct,
                        office_offdayprob, commercial_offdayprob, industry_offdayprob, cityservices_offdayprob,
                        school_start_time, school_end_time, work_start_time, work_end_time, school_vanilla_timeoff,
                        delayFactor, disable_early_shop_leisure, school_offdayprob, part_time_prob,
                        commute_top10, dow, overtime, part_time_reduction, specialEventStartTime, specialEventEndTime,
                        remote_work_prob, ref schedule);


                    m_CommandBuffer.AddComponent<CitizenSchedule>(unfilteredChunkIndex, entity1, citizenSchedule);
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
            public ComponentTypeHandle<CitizenSchedule> __Game_Citizens_CitizenSchedule_RW_ComponentTypeHandle;
            [ReadOnly]
            public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;
            [ReadOnly]
            public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;
            public ComponentTypeHandle<Worker> WorkerTypeHandle;
            public ComponentTypeHandle<Student> StudentTypeHandle;
            [ReadOnly]
            public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Population> __Game_City_Population_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Citizen> __Game_Citizens_Citizen_RO_ComponentLookup;
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
            public ComponentLookup<CitizenSchedule> CitizenScheduleLookup;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void __AssignHandles(ref SystemState state)
            {
                this.__Game_Citizens_Citizen_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Citizen>();
                this.__Game_Citizens_CitizenSchedule_RW_ComponentTypeHandle = state.GetComponentTypeHandle<CitizenSchedule>();
                this.__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
                this.__Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(true);
                this.WorkerTypeHandle = state.GetComponentTypeHandle<Worker>(true);
                this.StudentTypeHandle = state.GetComponentTypeHandle<Student>(true);
                this.__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(true);
                this.__Game_City_Population_RO_ComponentLookup = state.GetComponentLookup<Population>(true);
                this.__Game_Citizens_Citizen_RO_ComponentLookup = state.GetComponentLookup<Citizen>(true);
                this.CommercialPropertyLookup = state.GetComponentLookup<CommercialProperty>(true);
                this.IndustrialPropertyLookup = state.GetComponentLookup<IndustrialProperty>(true);
                this.OfficePropertyLookup = state.GetComponentLookup<OfficeProperty>(true);
                this.PropertyRenterLookup = state.GetComponentLookup<PropertyRenter>(true);
                this.PrefabRefLookup = state.GetComponentLookup<PrefabRef>(true);
                this.CitizenScheduleLookup = state.GetComponentLookup<CitizenSchedule>(false);
            }
        }
    }
}
