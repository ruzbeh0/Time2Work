using Game;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Game.Triggers;
using System;
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
using UnityEngine.Rendering;
namespace Time2Work
{
    public partial class Time2WorkWorkerSystem : GameSystemBase
    {
        private EndFrameBarrier m_EndFrameBarrier;
        private Time2WorkTimeSystem m_TimeSystem;
        private Time2WorkCitizenBehaviorSystem m_Time2WorkCitizenBehaviorSystem;
        private EntityQuery m_EconomyParameterQuery;
        private EntityQuery m_GotoWorkQuery;
        private EntityQuery m_WorkerQuery;
        private EntityQuery m_TimeDataQuery;
        private EntityQuery m_PopulationQuery;
        private SimulationSystem m_SimulationSystem;
        private TriggerSystem m_TriggerSystem;
        private Time2WorkWorkerSystem.TypeHandle __TypeHandle;
        private Setting.DTSimulationEnum m_daytype;

        public enum WorkType { Commercial, Office, Industrial, CityService }

        public override int GetUpdateInterval(SystemUpdatePhase phase) => 16;

        public static bool IsTodayOffDay(
          Citizen citizen,
          ref EconomyParameterData economyParameters,
          uint frame,
          Game.Common.TimeData timeData,
          int population, float timeOfDay,
          float offdayprob,
          int ticksPerDay
          )
        {
            int num = (int)Math.Round(offdayprob);
            //int num = math.min((int)Math.Round(offdayprob), Mathf.RoundToInt(100f / math.max(1f, math.sqrt(economyParameters.m_TrafficReduction * (float)population))));
            int day = Time2WorkTimeSystem.GetDay(frame, timeData, ticksPerDay);
            bool todayOff = Unity.Mathematics.Random.CreateFromIndex((uint)(citizen.m_PseudoRandom + day)).NextInt(100) <= num;
            bool yesterdayOff = Unity.Mathematics.Random.CreateFromIndex((uint)(citizen.m_PseudoRandom + day - 1)).NextInt(100) <= num;
            if (yesterdayOff && timeOfDay < 0.18)
            {
                return yesterdayOff;
            }

            return todayOff;
        }

        public static bool IsTodayOffDay(
          Citizen citizen,
          ref EconomyParameterData economyParameters,
          uint frame,
          Game.Common.TimeData timeData,
          int population, float timeOfDay,
          float offdayprob,
          int ticksPerDay,
          int day
          )
        {
            int num = (int)Math.Round(offdayprob);

            //int num = math.min((int)Math.Round(offdayprob), Mathf.RoundToInt(100f / math.max(1f, math.sqrt(economyParameters.m_TrafficReduction * (float)population))));

            bool todayOff = Unity.Mathematics.Random.CreateFromIndex((uint)(citizen.m_PseudoRandom + day)).NextInt(100) <= num;
            bool yesterdayOff = Unity.Mathematics.Random.CreateFromIndex((uint)(citizen.m_PseudoRandom + day - 1)).NextInt(100) <= num;
            if (yesterdayOff && timeOfDay < 0.18)
            {
                todayOff = yesterdayOff;
            }

            return todayOff;
        }

        public static bool IsTimeToWork(
          Citizen citizen,
          Worker worker,
          ref EconomyParameterData economyParameters,
          float timeOfDay,
          int lunch_break_pct,
          float work_start_time,
          float work_end_time,
          float delayFactor,
          int ticksPerDay,
          int part_time_prob,
          float commute_top10,
          float overtime,
          float part_time_reduction,
          out float2 timeToWork,
          out float start_work)
        {
            timeToWork = Time2WorkWorkerSystem.GetTimeToWork(citizen, worker, ref economyParameters, true, lunch_break_pct, work_start_time, work_end_time, delayFactor, ticksPerDay, part_time_prob, commute_top10, overtime, part_time_reduction, out start_work);
            return (double)timeToWork.x >= (double)timeToWork.y ? (double)timeOfDay >= (double)timeToWork.x || (double)timeOfDay <= (double)timeToWork.y : (double)timeOfDay >= (double)timeToWork.x && (double)timeOfDay <= (double)timeToWork.y;
        }

        public static bool IsTimeToWork(
          float timeOfDay, float2 timeToWork)
        {
            return (double)timeToWork.x >= (double)timeToWork.y ? (double)timeOfDay >= (double)timeToWork.x || (double)timeOfDay <= (double)timeToWork.y : (double)timeOfDay >= (double)timeToWork.x && (double)timeOfDay <= (double)timeToWork.y;
        }

        public static bool IsTodayLunchBreak(Citizen citizen, int lunch_break_pct)
        {
            int num = 100 - lunch_break_pct;
            if (Unity.Mathematics.Random.CreateFromIndex((uint)(citizen.m_PseudoRandom)).NextInt(100) > num)
            {
                return true;
            }

            return false;
        }
        public static bool IsTodayWorkFromHome(Citizen citizen, uint frame,
          Game.Common.TimeData timeData,
          int ticksPerDay,
          int remote_work_prob)
        {
            int day = Time2WorkTimeSystem.GetDay(frame, timeData, ticksPerDay);
            bool remoteWork = Unity.Mathematics.Random.CreateFromIndex((uint)(citizen.m_PseudoRandom + day)).NextInt(100) <= remote_work_prob;
            return remoteWork;
        }
        public static bool IsLunchTime(
          Citizen citizen,
          Worker worker,
          ref EconomyParameterData economyParameters,
          float timeOfDay,
          int lunch_break_pct,
          uint frame,
          Game.Common.TimeData timeData, int ticksPerDay, out float2 timeToLunch)
        {
            if (!Time2WorkWorkerSystem.IsTodayLunchBreak(citizen, lunch_break_pct))
            {
                timeToLunch = new float2(-1, -1);
                return false;
            }
            timeToLunch = Time2WorkWorkerSystem.GetLunchTime(citizen, worker, ref economyParameters);
            if (timeToLunch.x < 0)
            {
                return false;
            }
            else
            {
                return (double)timeToLunch.x >= (double)timeToLunch.y ? (double)timeOfDay >= (double)timeToLunch.x || (double)timeOfDay <= (double)timeToLunch.y : (double)timeOfDay >= (double)timeToLunch.x && (double)timeOfDay <= (double)timeToLunch.y;
            }
        }

        public static bool IsLunchTime(
          float timeOfDay, float2 timeToLunch)
        {
            if (timeToLunch.x < 0)
            {
                return false;
            }
            else
            {
                return (double)timeToLunch.x >= (double)timeToLunch.y ? (double)timeOfDay >= (double)timeToLunch.x || (double)timeOfDay <= (double)timeToLunch.y : (double)timeOfDay >= (double)timeToLunch.x && (double)timeOfDay <= (double)timeToLunch.y;
            }
        }

        public static float2 GetLunchTime(
        Citizen citizen,
          Worker worker,
          ref EconomyParameterData economyParameters)
        {
            Unity.Mathematics.Random random = Unity.Mathematics.Random.CreateFromIndex((uint)(citizen.m_PseudoRandom));
            float lunch_median = 0.5f;
            float lunch_duration = 0.05f;

            double startOnTime = random.NextDouble(-0.04, 0.04) + GaussianRandom.NextGaussianDouble(random) * 0.02;
            double endOnTime = GaussianRandom.NextGaussianDouble(random) * 0.03;

            if (worker.m_Shift == Workshift.Day)
            {
                float start_lunch = (float)(lunch_median + startOnTime);
                float end_lunch = (float)(lunch_median + lunch_duration + endOnTime);
                float diff = end_lunch - start_lunch;
                //Don't allow lunch break that is less than 30 minutes
                if(diff < 0.2f)
                {
                    end_lunch = start_lunch + 0.2f;
                }
                return new float2(start_lunch, end_lunch);
            }
            else
            {
                return new float2(-1, -1);
            }
        }

        public static float2 GetTimeToWork(
        Citizen citizen,
          Worker worker,
          ref EconomyParameterData economyParameters,
          bool includeCommute,
          int lunch_break_pct,
          float work_start_time,
          float work_end_time,
          float delayFactor,
          int ticksPerDay,
          int part_time_prob,
          float commute_top10,
          float overtime,
          float part_time_reduction,
          out float start_work)
        {
            //Unity.Mathematics.Random random = citizen.GetPseudoRandom(CitizenPseudoRandom.WorkOffset);
            Unity.Mathematics.Random random = Unity.Mathematics.Random.CreateFromIndex((uint)(citizen.m_PseudoRandom));
            double startOnTime = GaussianRandom.NextGaussianDouble(random) * delayFactor;
            double endOnTime = (GaussianRandom.NextGaussianDouble(random)) * delayFactor;
            endOnTime *= 1.4f;

            if (startOnTime > 0)
            {
                startOnTime *= 1.2;
            }
            if (endOnTime > 0)
            {
                endOnTime *= 1.1;
            }

            float startTimeOffset = (work_start_time - 4f) * (1 / 48f);
            float endTimeOffset = (work_end_time - 19f) * (1 / 48f);

            float workOffset = WorkerSystem.GetWorkOffset(citizen);
            double lateShiftOffset = GaussianRandom.NextGaussianDouble(random);
            if (worker.m_Shift == Workshift.Day)
            {
                workOffset *= 0.9f;
                bool lunch = Time2WorkWorkerSystem.IsTodayLunchBreak(citizen, lunch_break_pct);
                if (lunch)
                {
                    endOnTime += random.NextFloat(0.0f, 0.03f);
                }
                startOnTime += startTimeOffset;
                endOnTime += endTimeOffset;

                //Part Time
                int part_time_rand = random.NextInt(100);

                if (part_time_rand < part_time_prob && !lunch)
                {
                    double shift_duration = Math.Abs(economyParameters.m_WorkDayEnd + workOffset + endOnTime - (economyParameters.m_WorkDayStart + workOffset + startOnTime));
                    //Shift duration varies by education level
                    if (worker.m_Level <= 1)
                    {
                        shift_duration *= 1.05f;
                    }
                    else if (worker.m_Level > 2)
                    {
                        shift_duration /= 1.1f;
                    }
                    if (part_time_rand < part_time_prob / 2)
                    {
                        //startOnTime += shift_duration * (Math.Abs(GaussianRandom.NextGaussianDouble(random) * 0.2f) + 0.35f);
                        startOnTime += shift_duration * part_time_reduction;
                    }
                    else
                    {
                        //endOnTime -= shift_duration * (0.55f - Math.Abs(GaussianRandom.NextGaussianDouble(random) * 0.2f));
                        endOnTime -= shift_duration * part_time_reduction;
                    }
                }
                else
                {
                    endOnTime += overtime;
                }
            }
            else if (worker.m_Shift == Workshift.Evening)
            {
                //workOffset *= 2f;
                startOnTime *= 1.2f;
                endOnTime *= 1.2f;
                //workOffset += random.NextFloat(0.2f, 0.6f) + (float)(lateShiftOffset * delayFactor * 2);
                workOffset += 0.42f + (float)(lateShiftOffset * delayFactor * 2);
            }
            else if (worker.m_Shift == Workshift.Night)
            {
                //workOffset *= 4f;
                //workOffset += random.NextFloat(0.4f,0.8f) + (float)(lateShiftOffset * delayFactor * 4);
                //Night shifts can start either around 11pm or 4am
                if (random.NextInt(100) >= 60)
                {
                    workOffset -= 0.18f;
                    workOffset += (float)(lateShiftOffset * delayFactor * 3);
                    startOnTime *= 1.2f;
                    endOnTime *= 1.2f;
                }
                else
                {
                    workOffset += 0.63f + (float)(lateShiftOffset * delayFactor * 3);
                    startOnTime *= 1.3f;
                    endOnTime *= 1.3f;
                }
            }

            double num1 = (double)(float)(((double)economyParameters.m_WorkDayStart + (double)workOffset + startOnTime));
            float y = math.frac((float)(((double)economyParameters.m_WorkDayEnd + (double)workOffset + endOnTime)));
            //float y = math.frac((float)(((double)economyParameters.m_WorkDayEnd + (double)workOffset)));

            //Evening and Night Shifts are 6 to 4 hours long
            if (worker.m_Shift != Workshift.Day)
            {
                y -= random.NextFloat(0.10f, 0.16f);
            }

            float num2 = 0.0f;
            float peak_spread = 0f;
            if (includeCommute)
            {
                float num3 = 60f * worker.m_LastCommuteTime;
                if ((double)num3 < 60.0)
                    num3 = 40000f;
                num2 = num3 / ticksPerDay;

                if (commute_top10 > 0 && (24f * num2) > commute_top10)
                {
                    peak_spread = 0.2f * num2;
                    num2 += peak_spread;
                }
            }
            double num4 = (double)num2;
            start_work = math.frac((float)(num1));

            return new float2(math.frac((float)(num1 - num4)), y - peak_spread);
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            this.m_Time2WorkCitizenBehaviorSystem = this.World.GetOrCreateSystemManaged<Time2WorkCitizenBehaviorSystem>();
            this.m_EndFrameBarrier = this.World.GetOrCreateSystemManaged<EndFrameBarrier>();
            this.m_TimeSystem = this.World.GetOrCreateSystemManaged<Time2WorkTimeSystem>();
            this.m_SimulationSystem = this.World.GetOrCreateSystemManaged<SimulationSystem>();
            this.m_TriggerSystem = this.World.GetOrCreateSystemManaged<TriggerSystem>();
            this.m_WorkerQuery = this.GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[5]
             {
                   ComponentType.ReadOnly<Worker>(),
                   ComponentType.ReadOnly<Citizen>(),
                   ComponentType.ReadOnly<TravelPurpose>(),
                   ComponentType.ReadOnly<CurrentBuilding>(),
                   ComponentType.ReadOnly<CitizenSchedule>()
             },
                Any = new ComponentType[0]
               {

               },
                None = new ComponentType[2]
             {
                ComponentType.Exclude<Deleted>(),
                ComponentType.Exclude<Temp>()
             }
            });
            this.m_GotoWorkQuery = this.GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[5]
             {
                   ComponentType.ReadOnly<Worker>(),
                   ComponentType.ReadOnly<Citizen>(),
                   ComponentType.ReadOnly<CurrentBuilding>(),
                   ComponentType.ReadOnly<CitizenSchedule>(),
                   ComponentType.ReadWrite<TripNeeded>()
             },
                Any = new ComponentType[0]
               {

               },
                None = new ComponentType[5]
             {
                ComponentType.Exclude<TravelPurpose>(),
                ComponentType.Exclude<HealthProblem>(),
                ComponentType.Exclude<ResourceBuyer>(),
                ComponentType.Exclude<Deleted>(),
                ComponentType.Exclude<Temp>()
             }
            });
            this.m_EconomyParameterQuery = this.GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
            this.m_TimeDataQuery = this.GetEntityQuery(ComponentType.ReadOnly<Game.Common.TimeData>());
            this.m_PopulationQuery = this.GetEntityQuery(ComponentType.ReadOnly<Population>());
            this.RequireAnyForUpdate(this.m_GotoWorkQuery, this.m_WorkerQuery);
            this.RequireForUpdate(this.m_EconomyParameterQuery);
            this.m_daytype = WeekSystem.currentDayOfTheWeek;
        }

        protected override void OnUpdate()
        {
            double delayFactor = (float)(Mod.m_Setting.delay_factor) / 100;

            float4 office_offdayprob = WeekSystem.getOfficeOffDayProb();
            float4 commercial_offdayprob = WeekSystem.getCommercialOffDayProb();
            float4 industry_offdayprob = WeekSystem.getIndustryOffDayProb();
            float4 cityservices_offdayprob = WeekSystem.getCityServicesOffDayProb();


            uint frameWithInterval = SimulationUtils.GetUpdateFrameWithInterval(this.m_SimulationSystem.frameIndex, (uint)this.GetUpdateInterval(SystemUpdatePhase.GameSimulation), 16);
            this.__TypeHandle.__Game_City_Population_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_AttendingMeeting_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_TravelPurpose_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Objects_OutsideConnection_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_CarKeeper_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_Building_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_TripNeeded_RW_BufferTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_Worker_RO_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_Citizen_RW_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_CitizenSchedule_RO_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref this.CheckedStateRef);
            this.m_daytype = WeekSystem.currentDayOfTheWeek;
            JobHandle deps;
            //Mod.log.Info($"day type: {this.m_daytype}");
            JobHandle jobHandle1 = new Time2WorkWorkerSystem.GoToWorkJob()
            {
                m_EntityType = this.__TypeHandle.__Unity_Entities_Entity_TypeHandle,
                m_CitizenType = this.__TypeHandle.__Game_Citizens_Citizen_RW_ComponentTypeHandle,
                m_CitizenSchedule = this.__TypeHandle.__Game_Citizens_CitizenSchedule_RO_ComponentTypeHandle,
                m_CurrentBuildingType = this.__TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle,
                m_WorkerType = this.__TypeHandle.__Game_Citizens_Worker_RO_ComponentTypeHandle,
                m_HouseholdMemberType = this.__TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentTypeHandle,
                m_TripType = this.__TypeHandle.__Game_Citizens_TripNeeded_RW_BufferTypeHandle,
                m_UpdateFrameType = this.__TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle,
                m_Buildings = this.__TypeHandle.__Game_Buildings_Building_RO_ComponentLookup,
                m_CarKeepers = this.__TypeHandle.__Game_Citizens_CarKeeper_RO_ComponentLookup,
                m_Properties = this.__TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup,
                m_OutsideConnections = this.__TypeHandle.__Game_Objects_OutsideConnection_RO_ComponentLookup,
                m_Purposes = this.__TypeHandle.__Game_Citizens_TravelPurpose_RO_ComponentLookup,
                m_Attendings = this.__TypeHandle.__Game_Citizens_AttendingMeeting_RO_ComponentLookup,
                m_PopulationData = this.__TypeHandle.__Game_City_Population_RO_ComponentLookup,
                WorkplaceDataLookup = this.__TypeHandle.WorkplaceDataLookup,
                CommercialPropertyLookup = this.__TypeHandle.CommercialPropertyLookup,
                IndustrialPropertyLookup = this.__TypeHandle.IndustrialPropertyLookup,
                OfficePropertyLookup = this.__TypeHandle.OfficePropertyLookup,
                PropertyRenterLookup = this.__TypeHandle.PropertyRenterLookup,
                PrefabRefLookup = this.__TypeHandle.PrefabRefLookup,
                m_TriggerBuffer = this.m_TriggerSystem.CreateActionBuffer().AsParallelWriter(),
                m_EconomyParameters = this.m_EconomyParameterQuery.GetSingleton<EconomyParameterData>(),
                m_TimeOfDay = this.m_TimeSystem.normalizedTime,
                m_UpdateFrameIndex = frameWithInterval,
                m_Frame = this.m_SimulationSystem.frameIndex,
                m_TimeData = this.m_TimeDataQuery.GetSingleton<Game.Common.TimeData>(),
                m_PopulationEntity = this.m_PopulationQuery.GetSingletonEntity(),
                m_CarReserverQueue = this.m_Time2WorkCitizenBehaviorSystem.GetCarReserveQueue(out deps),
                m_CommandBuffer = this.m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
                lunch_break_pct = Mod.m_Setting.lunch_break_percentage,
                vacation = Mod.m_Setting.vacation_per_year,
                holidays = Mod.m_Setting.holidays_per_year,
                vanilla_timeoff = Mod.m_Setting.use_school_vanilla_timeoff,
                office_offdayprob = WeekSystem.getOfficeOffDayProb(),
                commercial_offdayprob = WeekSystem.getCommercialOffDayProb(),
                industry_offdayprob = WeekSystem.getIndustryOffDayProb(),
                cityservices_offdayprob = WeekSystem.getCityServicesOffDayProb(),
                dow = this.m_daytype,
                work_start_time = (float)Mod.m_Setting.work_start_time,
                work_end_time = (float)Mod.m_Setting.work_end_time,
                delayFactor = (float)(Mod.m_Setting.delay_factor) / 100,
                ticksPerDay = Time2WorkTimeSystem.kTicksPerDay,
                part_time_prob = Mod.m_Setting.part_time_percentage,
                remote_work_prob = Mod.m_Setting.remote_percentage,
                commute_top10 = Mod.m_Setting.commute_top10per,
                part_time_reduction = Mod.m_Setting.avg_work_hours_pt_wd / Mod.m_Setting.avg_work_hours_ft_wd,
                overtime = (Mod.m_Setting.avg_work_hours_ft_wd - (Mod.m_Setting.work_end_time - Mod.m_Setting.work_start_time) / 2) / 24
            }.ScheduleParallel<Time2WorkWorkerSystem.GoToWorkJob>(this.m_GotoWorkQuery, JobHandle.CombineDependencies(this.Dependency, deps));
            this.m_EndFrameBarrier.AddJobHandleForProducer(jobHandle1);
            this.m_Time2WorkCitizenBehaviorSystem.AddCarReserveWriter(jobHandle1);
            this.m_TriggerSystem.AddActionBufferWriter(jobHandle1);
            this.__TypeHandle.__Game_Companies_WorkProvider_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_AttendingMeeting_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_Citizen_RW_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_CitizenSchedule_RO_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_TravelPurpose_RO_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_Worker_RO_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref this.CheckedStateRef);

            JobHandle jobHandle2 = new Time2WorkWorkerSystem.WorkJob()
            {
                m_EntityType = this.__TypeHandle.__Unity_Entities_Entity_TypeHandle,
                m_CitizenSchedule = this.__TypeHandle.__Game_Citizens_CitizenSchedule_RO_ComponentTypeHandle,
                m_WorkerType = this.__TypeHandle.__Game_Citizens_Worker_RO_ComponentTypeHandle,
                m_PurposeType = this.__TypeHandle.__Game_Citizens_TravelPurpose_RO_ComponentTypeHandle,
                m_UpdateFrameType = this.__TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle,
                m_CitizenType = this.__TypeHandle.__Game_Citizens_Citizen_RW_ComponentTypeHandle,
                m_Attendings = this.__TypeHandle.__Game_Citizens_AttendingMeeting_RO_ComponentLookup,
                m_Workplaces = this.__TypeHandle.__Game_Companies_WorkProvider_RO_ComponentLookup,
                m_TriggerBuffer = this.m_TriggerSystem.CreateActionBuffer().AsParallelWriter(),
                m_EconomyParameters = this.m_EconomyParameterQuery.GetSingleton<EconomyParameterData>(),
                m_UpdateFrameIndex = frameWithInterval,
                m_TimeOfDay = this.m_TimeSystem.normalizedTime,
                m_Frame = this.m_SimulationSystem.frameIndex,
                m_TimeData = this.m_TimeDataQuery.GetSingleton<Game.Common.TimeData>(),
                m_CommandBuffer = this.m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
                lunch_break_pct = Mod.m_Setting.lunch_break_percentage,
                work_start_time = (float)Mod.m_Setting.work_start_time,
                work_end_time = (float)Mod.m_Setting.work_end_time,
                delayFactor = (float)(Mod.m_Setting.delay_factor) / 100,
                ticksPerDay = Time2WorkTimeSystem.kTicksPerDay,
                part_time_prob = Mod.m_Setting.part_time_percentage,
                commute_top10 = Mod.m_Setting.commute_top10per,
                part_time_reduction = Mod.m_Setting.avg_work_hours_pt_wd / Mod.m_Setting.avg_work_hours_ft_wd,
                overtime = (Mod.m_Setting.avg_work_hours_ft_wd - (Mod.m_Setting.work_end_time - Mod.m_Setting.work_start_time) / 2) / 24
            }.ScheduleParallel<Time2WorkWorkerSystem.WorkJob>(this.m_WorkerQuery, JobHandle.CombineDependencies(this.Dependency, jobHandle1));
            this.m_EndFrameBarrier.AddJobHandleForProducer(jobHandle2);
            this.m_TriggerSystem.AddActionBufferWriter(jobHandle2);
            this.Dependency = jobHandle2;
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

        public static WorkType GetWorkerOffDayAndPartTimeProb(ComponentLookup<PrefabRef> PrefabRefLookup, ComponentLookup<PropertyRenter> PropertyRenterLookup,
            ComponentLookup<CommercialProperty> CommercialPropertyLookup, ComponentLookup<IndustrialProperty> IndustrialPropertyLookup, 
            ComponentLookup<OfficeProperty> OfficePropertyLookup, Worker worker,out float offdayprob, int part_time_prob, out int parttime_prob, float4 commercial_offdayprob, 
            float4 industry_offdayprob, float4 office_offdayprob, float4 cityservices_offdayprob, Setting.DTSimulationEnum dow,  out int remote_work_probability)
        {
            offdayprob = 60f;
            parttime_prob = part_time_prob;
            remote_work_probability = 0;
            WorkType work = WorkType.CityService;
            if (PrefabRefLookup.TryGetComponent(worker.m_Workplace, out var prefab1))
            {
                if (PropertyRenterLookup.TryGetComponent(worker.m_Workplace, out var propertyRenter))
                {
                    //x = weekday, y = friday, z = saturday, w = sunday
                    work = WorkType.Commercial;
                    if (CommercialPropertyLookup.HasComponent(propertyRenter.m_Property))
                    {
                        if ((int)dow == (int)Setting.DTSimulationEnum.Weekday)
                        {
                            offdayprob = commercial_offdayprob.x;
                        }
                        else if ((int)dow == (int)Setting.DTSimulationEnum.AverageDay)
                        {
                            offdayprob = commercial_offdayprob.y;
                        }
                        else if ((int)dow == (int)Setting.DTSimulationEnum.Saturday)
                        {
                            offdayprob = commercial_offdayprob.z;
                            parttime_prob = 100;
                        }
                        else
                        {
                            offdayprob = commercial_offdayprob.w;
                            parttime_prob = 100;
                        }
                        //No remote work for commercial
                        remote_work_probability = 0;
                    }
                    if (IndustrialPropertyLookup.HasComponent(propertyRenter.m_Property))
                    {
                        work = WorkType.Industrial;
                        if ((int)dow == (int)Setting.DTSimulationEnum.Weekday)
                        {
                            offdayprob = industry_offdayprob.x;
                        }
                        else if ((int)dow == (int)Setting.DTSimulationEnum.AverageDay)
                        {
                            offdayprob = industry_offdayprob.y;
                        }
                        else if ((int)dow == (int)Setting.DTSimulationEnum.Saturday)
                        {
                            offdayprob = industry_offdayprob.z;
                            parttime_prob = 100;
                        }
                        else
                        {
                            offdayprob = industry_offdayprob.w;
                            parttime_prob = 100;
                        }
                        //No remote work for industry
                        remote_work_probability = 0;
                    }
                    if (OfficePropertyLookup.HasComponent(propertyRenter.m_Property))
                    {
                        work = WorkType.Office;
                        if ((int)dow == (int)Setting.DTSimulationEnum.Weekday)
                        {
                            offdayprob = office_offdayprob.x;
                        }
                        else if ((int)dow == (int)Setting.DTSimulationEnum.AverageDay)
                        {
                            offdayprob = office_offdayprob.y;
                        }
                        else if ((int)dow == (int)Setting.DTSimulationEnum.Saturday)
                        {
                            offdayprob = office_offdayprob.z;
                            parttime_prob = 100;
                        }
                        else
                        {
                            offdayprob = office_offdayprob.w;
                            parttime_prob = 100;
                        }
                    }
                    else
                    {
                        if ((int)dow == (int)Setting.DTSimulationEnum.Weekday)
                        {
                            offdayprob = cityservices_offdayprob.x;
                        }
                        else if ((int)dow == (int)Setting.DTSimulationEnum.AverageDay)
                        {
                            offdayprob = cityservices_offdayprob.y;
                        }
                        else if ((int)dow == (int)Setting.DTSimulationEnum.Saturday)
                        {
                            offdayprob = cityservices_offdayprob.z;
                            parttime_prob = 100;
                        }
                        else
                        {
                            offdayprob = cityservices_offdayprob.w;
                            parttime_prob = 100;
                        }
                    }
                }
            }
            return work;
        }

        public static void GetWorkTypeProbabilities(WorkType workType, Setting.DTSimulationEnum dow, out float offdayprob, out int parttime_prob, float4 commercial_offdayprob,
            float4 industry_offdayprob, float4 office_offdayprob, float4 cityservices_offdayprob, out int remote_work_probability)
        {
            offdayprob = 0;
            parttime_prob = 0;
            remote_work_probability = 0;
            if ((int)workType == (int)WorkType.Commercial)
            {
                if ((int)dow == (int)Setting.DTSimulationEnum.Weekday)
                {
                    offdayprob = commercial_offdayprob.x;
                }
                else if ((int)dow == (int)Setting.DTSimulationEnum.AverageDay)
                {
                    offdayprob = commercial_offdayprob.y;
                }
                else if ((int)dow == (int)Setting.DTSimulationEnum.Saturday)
                {
                    offdayprob = commercial_offdayprob.z;
                    parttime_prob = 100;
                }
                else
                {
                    offdayprob = commercial_offdayprob.w;
                    parttime_prob = 100;
                }
                //No remote work for commercial
                remote_work_probability = 0;
            } else if ((int)workType == (int)WorkType.Industrial)
            {
                if ((int)dow == (int)Setting.DTSimulationEnum.Weekday)
                {
                    offdayprob = industry_offdayprob.x;
                }
                else if ((int)dow == (int)Setting.DTSimulationEnum.AverageDay)
                {
                    offdayprob = industry_offdayprob.y;
                }
                else if ((int)dow == (int)Setting.DTSimulationEnum.Saturday)
                {
                    offdayprob = industry_offdayprob.z;
                    parttime_prob = 100;
                }
                else
                {
                    offdayprob = industry_offdayprob.w;
                    parttime_prob = 100;
                }
                //No remote work for industry
                remote_work_probability = 0;
            }
            else if ((int)workType == (int)WorkType.Office)
            {
                if ((int)dow == (int)Setting.DTSimulationEnum.Weekday)
                {
                    offdayprob = office_offdayprob.x;
                }
                else if ((int)dow == (int)Setting.DTSimulationEnum.AverageDay)
                {
                    offdayprob = office_offdayprob.y;
                }
                else if ((int)dow == (int)Setting.DTSimulationEnum.Saturday)
                {
                    offdayprob = office_offdayprob.z;
                    parttime_prob = 100;
                }
                else
                {
                    offdayprob = office_offdayprob.w;
                    parttime_prob = 100;
                }
            }
            else if ((int)workType == (int)WorkType.CityService)
            {
                if ((int)dow == (int)Setting.DTSimulationEnum.Weekday)
                {
                    offdayprob = cityservices_offdayprob.x;
                }
                else if ((int)dow == (int)Setting.DTSimulationEnum.AverageDay)
                {
                    offdayprob = cityservices_offdayprob.y;
                }
                else if ((int)dow == (int)Setting.DTSimulationEnum.Saturday)
                {
                    offdayprob = cityservices_offdayprob.z;
                    parttime_prob = 100;
                }
                else
                {
                    offdayprob = cityservices_offdayprob.w;
                    parttime_prob = 100;
                }
            }
        }
        public Time2WorkWorkerSystem()
        {
        }

        [BurstCompile]
        private struct GoToWorkJob : IJobChunk
        {
            [ReadOnly]
            public EntityTypeHandle m_EntityType;
            public ComponentTypeHandle<Citizen> m_CitizenType;
            [ReadOnly]
            public ComponentTypeHandle<CitizenSchedule> m_CitizenSchedule;
            [ReadOnly]
            public ComponentTypeHandle<Worker> m_WorkerType;
            [ReadOnly]
            public ComponentTypeHandle<HouseholdMember> m_HouseholdMemberType;
            [ReadOnly]
            public ComponentTypeHandle<CurrentBuilding> m_CurrentBuildingType;
            public BufferTypeHandle<TripNeeded> m_TripType;
            [ReadOnly]
            public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;
            [ReadOnly]
            public ComponentLookup<TravelPurpose> m_Purposes;
            [ReadOnly]
            public ComponentLookup<PropertyRenter> m_Properties;
            [ReadOnly]
            public ComponentLookup<Building> m_Buildings;
            [ReadOnly]
            public ComponentLookup<CarKeeper> m_CarKeepers;
            [ReadOnly]
            public ComponentLookup<Game.Objects.OutsideConnection> m_OutsideConnections;
            [ReadOnly]
            public ComponentLookup<AttendingMeeting> m_Attendings;
            [ReadOnly]
            public ComponentLookup<Population> m_PopulationData;
            [ReadOnly]
            public ComponentLookup<WorkplaceData> WorkplaceDataLookup;
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
            public NativeQueue<TriggerAction>.ParallelWriter m_TriggerBuffer;
            public uint m_Frame;
            public Game.Common.TimeData m_TimeData;
            public uint m_UpdateFrameIndex;
            public float m_TimeOfDay;
            public Entity m_PopulationEntity;
            public EconomyParameterData m_EconomyParameters;
            public NativeQueue<Entity>.ParallelWriter m_CarReserverQueue;
            public EntityCommandBuffer.ParallelWriter m_CommandBuffer;
            public int lunch_break_pct;
            public float vacation;
            public float holidays;
            public bool vanilla_timeoff;
            public float4 office_offdayprob;
            public float4 commercial_offdayprob;
            public float4 industry_offdayprob;
            public float4 cityservices_offdayprob;
            public float work_start_time;
            public float work_end_time;
            public float delayFactor;
            public int ticksPerDay;
            public int part_time_prob;
            public int remote_work_prob;
            public float commute_top10;
            public float overtime;
            public float part_time_reduction;
            public Setting.DTSimulationEnum dow;

            public void Execute(
              in ArchetypeChunk chunk,
              int unfilteredChunkIndex,
              bool useEnabledMask,
              in v128 chunkEnabledMask)
            {
                if ((int)chunk.GetSharedComponent<UpdateFrame>(this.m_UpdateFrameType).m_Index != (int)this.m_UpdateFrameIndex)
                    return;
                NativeArray<Entity> nativeArray1 = chunk.GetNativeArray(this.m_EntityType);
                NativeArray<Citizen> nativeArray2 = chunk.GetNativeArray<Citizen>(ref this.m_CitizenType);
                NativeArray<Worker> nativeArray3 = chunk.GetNativeArray<Worker>(ref this.m_WorkerType);
                NativeArray<HouseholdMember> nativeArray5 = chunk.GetNativeArray<HouseholdMember>(ref this.m_HouseholdMemberType);
                NativeArray<CurrentBuilding> nativeArray4 = chunk.GetNativeArray<CurrentBuilding>(ref this.m_CurrentBuildingType);
                NativeArray<CitizenSchedule> nativeArray6 = chunk.GetNativeArray<CitizenSchedule>(ref this.m_CitizenSchedule);
                BufferAccessor<TripNeeded> bufferAccessor = chunk.GetBufferAccessor<TripNeeded>(ref this.m_TripType);

                int population = this.m_PopulationData[this.m_PopulationEntity].m_Population;

                for (int index = 0; index < nativeArray1.Length; ++index)
                {
                    Entity entity1 = nativeArray1[index];
                    Citizen citizen = nativeArray2[index];
                    Worker worker = nativeArray3[index];

                    CitizenSchedule citizenSchedule = nativeArray6[index];
                    float2 time2Lunch = new float2(citizenSchedule.start_lunch, citizenSchedule.end_lunch);
                    float2 time2Work = new float2(citizenSchedule.go_to_work, citizenSchedule.end_work);
                    bool dayOff = citizenSchedule.dayoff;
                    bool lunchTime = Time2WorkWorkerSystem.IsLunchTime(this.m_TimeOfDay, time2Lunch);
                    bool workTime = Time2WorkWorkerSystem.IsTimeToWork(this.m_TimeOfDay, time2Work);
                    bool workFromHome = citizenSchedule.work_from_home;
                    float start_work = citizenSchedule.start_work;

                    if (!dayOff
                        && !lunchTime && workTime)
                    {
                        DynamicBuffer<TripNeeded> dynamicBuffer = bufferAccessor[index];
                        if (!this.m_Attendings.HasComponent(entity1) && (citizen.m_State & CitizenFlags.MovingAwayReachOC) == CitizenFlags.None)
                        {
                            Entity workplace = nativeArray3[index].m_Workplace;
                            Entity entity2 = Entity.Null;
                            if (this.m_Properties.HasComponent(workplace))
                            {
                                entity2 = this.m_Properties[workplace].m_Property;
                            }
                            else
                            {
                                if (this.m_Buildings.HasComponent(workplace))
                                {
                                    entity2 = workplace;
                                }
                                else
                                {
                                    if (this.m_OutsideConnections.HasComponent(workplace))
                                        entity2 = workplace;
                                }
                            }
                            if (entity2 != Entity.Null)
                            {
                                if (nativeArray4[index].m_CurrentBuilding != entity2)
                                {
                                    if (!this.m_CarKeepers.IsComponentEnabled(entity1))
                                    {
                                        this.m_CarReserverQueue.Enqueue(entity1);
                                    }
                                    //If too much time has passed since work start time, not go to work
                                    double threshold_start_work = Math.Min(Math.Abs(time2Work.x - this.m_TimeOfDay), Math.Abs(1 - (time2Work.x - this.m_TimeOfDay)));
                                    double threshold_resume_work = Math.Min(Math.Abs(time2Lunch.y - this.m_TimeOfDay), Math.Abs(1 - (time2Lunch.y - this.m_TimeOfDay)));
                                    Unity.Mathematics.Random random = Unity.Mathematics.Random.CreateFromIndex((uint)(citizen.m_PseudoRandom));

                                    Entity household = nativeArray5[index].m_Household;
                                    Entity home = Entity.Null;
                                    if (this.m_Properties.HasComponent(household))
                                    {
                                        home = this.m_Properties[household].m_Property;
                                    }
                                    //Mod.log.Info($"work:{timeToWork}, lunch{timeToLunch}");
                                    if (threshold_start_work <= 0.03 ||
                                        (threshold_resume_work >= 0 && threshold_resume_work <= 0.03))
                                    {
                                        if (nativeArray4[index].m_CurrentBuilding == home &&
                                            threshold_start_work > 0.03)
                                        {
                                            continue;
                                        }
                                        if (workFromHome && nativeArray3[index].m_Level >= 3)
                                        {
                                            if (nativeArray4[index].m_CurrentBuilding == home)
                                            {
                                                continue;
                                            }
                                            else
                                            {
                                                dynamicBuffer.Add(new TripNeeded()
                                                {
                                                    m_TargetAgent = home,
                                                    m_Purpose = Game.Citizens.Purpose.GoingHome
                                                });
                                            }
                                        }
                                        else
                                        {
                                            dynamicBuffer.Add(new TripNeeded()
                                            {
                                                m_TargetAgent = workplace,
                                                m_Purpose = Game.Citizens.Purpose.GoingToWork
                                            });
                                        }

                                    }
                                }
                            }
                            else
                            {
                                citizen.SetFailedEducationCount(0);
                                nativeArray2[index] = citizen;

                                if (this.m_Purposes.HasComponent(entity1) && (this.m_Purposes[entity1].m_Purpose == Game.Citizens.Purpose.GoingToWork || this.m_Purposes[entity1].m_Purpose == Game.Citizens.Purpose.Working))
                                {
                                    this.m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, entity1);
                                }
                                this.m_CommandBuffer.RemoveComponent<Worker>(unfilteredChunkIndex, entity1);
                                this.m_TriggerBuffer.Enqueue(new TriggerAction(TriggerType.CitizenBecameUnemployed, Entity.Null, entity1, workplace));
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

        [BurstCompile]
        private struct WorkJob : IJobChunk
        {
            [ReadOnly]
            public ComponentTypeHandle<Worker> m_WorkerType;
            [ReadOnly]
            public EntityTypeHandle m_EntityType;
            [ReadOnly]
            public ComponentTypeHandle<CitizenSchedule> m_CitizenSchedule;
            [ReadOnly]
            public ComponentTypeHandle<TravelPurpose> m_PurposeType;
            [ReadOnly]
            public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;
            public ComponentTypeHandle<Citizen> m_CitizenType;
            [ReadOnly]
            public ComponentLookup<WorkProvider> m_Workplaces;
            [ReadOnly]
            public ComponentLookup<AttendingMeeting> m_Attendings;
            public EconomyParameterData m_EconomyParameters;
            public NativeQueue<TriggerAction>.ParallelWriter m_TriggerBuffer;
            public float m_TimeOfDay;
            public uint m_UpdateFrameIndex;
            public uint m_Frame;
            public Game.Common.TimeData m_TimeData;
            public int m_Population;
            public EntityCommandBuffer.ParallelWriter m_CommandBuffer;
            public int lunch_break_pct;
            public float work_start_time;
            public float work_end_time;
            public float delayFactor;
            public int ticksPerDay;
            public int part_time_prob;
            public float commute_top10;
            public float overtime;
            public float part_time_reduction;

            public void Execute(
              in ArchetypeChunk chunk,
              int unfilteredChunkIndex,
              bool useEnabledMask,
              in v128 chunkEnabledMask)
            {
                if ((int)chunk.GetSharedComponent<UpdateFrame>(this.m_UpdateFrameType).m_Index != (int)this.m_UpdateFrameIndex)
                    return;
                NativeArray<Entity> nativeArray1 = chunk.GetNativeArray(this.m_EntityType);
                NativeArray<Worker> nativeArray2 = chunk.GetNativeArray<Worker>(ref this.m_WorkerType);
                NativeArray<TravelPurpose> nativeArray3 = chunk.GetNativeArray<TravelPurpose>(ref this.m_PurposeType);
                NativeArray<Citizen> nativeArray4 = chunk.GetNativeArray<Citizen>(ref this.m_CitizenType);
                NativeArray<CitizenSchedule> nativeArray6 = chunk.GetNativeArray<CitizenSchedule>(ref this.m_CitizenSchedule);

                for (int index = 0; index < nativeArray1.Length; ++index)
                {
                    Entity entity = nativeArray1[index];
                    Entity workplace = nativeArray2[index].m_Workplace;
                    Worker worker = nativeArray2[index];
                    Citizen citizen = nativeArray4[index];
                    if (!this.m_Workplaces.HasComponent(workplace))
                    {
                        citizen.SetFailedEducationCount(0);
                        nativeArray4[index] = citizen;
                        TravelPurpose travelPurpose = nativeArray3[index];
                        if (travelPurpose.m_Purpose == Game.Citizens.Purpose.GoingToWork || travelPurpose.m_Purpose == Game.Citizens.Purpose.Working)
                        {
                            this.m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, entity);
                        }
                        this.m_CommandBuffer.RemoveComponent<Worker>(unfilteredChunkIndex, entity);
                        this.m_TriggerBuffer.Enqueue(new TriggerAction(TriggerType.CitizenBecameUnemployed, Entity.Null, entity, workplace));
                    }
                    else
                    {
                        CitizenSchedule citizenSchedule = nativeArray6[index];
                        float2 time2Lunch = new float2(citizenSchedule.start_lunch, citizenSchedule.end_lunch);
                        float2 time2Work = new float2(citizenSchedule.go_to_work, citizenSchedule.end_work);
                        bool lunchTime = Time2WorkWorkerSystem.IsLunchTime(this.m_TimeOfDay, time2Lunch);
                        bool workTime = Time2WorkWorkerSystem.IsTimeToWork(this.m_TimeOfDay, time2Work);

                        if ((!workTime || this.m_Attendings.HasComponent(entity) || lunchTime) && nativeArray3[index].m_Purpose == Game.Citizens.Purpose.Working)
                        {
                            this.m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, entity);
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
            [ReadOnly]
            public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;
            public ComponentTypeHandle<Citizen> __Game_Citizens_Citizen_RW_ComponentTypeHandle;
            public ComponentTypeHandle<CitizenSchedule> __Game_Citizens_CitizenSchedule_RO_ComponentTypeHandle;
            [ReadOnly]
            public ComponentTypeHandle<CurrentBuilding> __Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle;
            [ReadOnly]
            public ComponentTypeHandle<Worker> __Game_Citizens_Worker_RO_ComponentTypeHandle;
            public BufferTypeHandle<TripNeeded> __Game_Citizens_TripNeeded_RW_BufferTypeHandle;
            public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;
            [ReadOnly]
            public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<CarKeeper> __Game_Citizens_CarKeeper_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Game.Objects.OutsideConnection> __Game_Objects_OutsideConnection_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<TravelPurpose> __Game_Citizens_TravelPurpose_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<AttendingMeeting> __Game_Citizens_AttendingMeeting_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Population> __Game_City_Population_RO_ComponentLookup;
            [ReadOnly]
            public ComponentTypeHandle<TravelPurpose> __Game_Citizens_TravelPurpose_RO_ComponentTypeHandle;
            [ReadOnly]
            public ComponentLookup<WorkProvider> __Game_Companies_WorkProvider_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<HouseholdMember> __Game_Citizens_HouseholdMember_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<WorkplaceData> WorkplaceDataLookup;
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
            public ComponentTypeHandle<HouseholdMember> __Game_Citizens_HouseholdMember_RO_ComponentTypeHandle;


            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void __AssignHandles(ref SystemState state)
            {
                this.__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
                this.__Game_Citizens_Citizen_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Citizen>();
                this.__Game_Citizens_CitizenSchedule_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CitizenSchedule>(true);
                this.__Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CurrentBuilding>(true);
                this.__Game_Citizens_Worker_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Worker>(true);
                this.__Game_Citizens_TripNeeded_RW_BufferTypeHandle = state.GetBufferTypeHandle<TripNeeded>();
                this.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
                this.__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(true);
                this.__Game_Citizens_CarKeeper_RO_ComponentLookup = state.GetComponentLookup<CarKeeper>(true);
                this.__Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(true);
                this.__Game_Objects_OutsideConnection_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.OutsideConnection>(true);
                this.__Game_Citizens_TravelPurpose_RO_ComponentLookup = state.GetComponentLookup<TravelPurpose>(true);
                this.__Game_Citizens_AttendingMeeting_RO_ComponentLookup = state.GetComponentLookup<AttendingMeeting>(true);
                this.__Game_City_Population_RO_ComponentLookup = state.GetComponentLookup<Population>(true);
                this.__Game_Citizens_TravelPurpose_RO_ComponentTypeHandle = state.GetComponentTypeHandle<TravelPurpose>(true);
                this.__Game_Companies_WorkProvider_RO_ComponentLookup = state.GetComponentLookup<WorkProvider>(true);
                this.__Game_Citizens_HouseholdMember_RO_ComponentLookup = state.GetComponentLookup<HouseholdMember>(true);
                this.__Game_Citizens_HouseholdMember_RO_ComponentTypeHandle = state.GetComponentTypeHandle<HouseholdMember>(true);
                this.WorkplaceDataLookup = state.GetComponentLookup<WorkplaceData>(true);
                this.CommercialPropertyLookup = state.GetComponentLookup<CommercialProperty>(true);
                this.IndustrialPropertyLookup = state.GetComponentLookup<IndustrialProperty>(true);
                this.OfficePropertyLookup = state.GetComponentLookup<OfficeProperty>(true);
                this.PropertyRenterLookup = state.GetComponentLookup<PropertyRenter>(true);
                this.PrefabRefLookup = state.GetComponentLookup<PrefabRef>(true);
            }
        }
    }
}
