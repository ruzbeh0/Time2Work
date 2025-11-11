using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Prefabs;
using System.Runtime.CompilerServices;
using Time2Work.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using static Time2Work.Time2WorkWorkerSystem;
using Student = Game.Citizens.Student;

namespace Time2Work.Utils
{
    public static class CitizenScheduleHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CitizenSchedule CalculateScheduleForCitizen(
            Entity entity,
            Citizen citizen,
            bool isWorker,
            Worker workerData,
            Student studentData,
            ComponentLookup<PrefabRef> prefabRefs,
            ComponentLookup<PropertyRenter> propertyRenters,
            ComponentLookup<CommercialProperty> commercialLookup,
            ComponentLookup<IndustrialProperty> industrialLookup,
            ComponentLookup<OfficeProperty> officeLookup,
            ComponentLookup<Population> populationLookup,
            EconomyParameterData economy,
            int population,
            float normalizedTime,
            uint simulationFrame,
            Game.Common.TimeData timeData,
            int ticksPerDay,
            int lunchBreakPct,
            float4 officeOffdayprob,
            float4 commercialOffdayprob,
            float4 industryOffdayprob,
            float4 cityservicesOffdayprob,
            int3 schoolStart,
            int3 schoolEnd,
            float workStart,
            float workEnd,
            bool useVanillaSchoolTimeOff,
            float delayFactor,
            bool disableEarlyShopLeisure,
            float3 schoolOffdayprob,
            int partTimeProb,
            float commuteTop10,
            Setting.DTSimulationEnum dow,
            float overtime,
            float partTimeReduction,
            NativeArray<float> eventStart,
            NativeArray<float> eventEnd,
            int remoteWorkProb,
            ref CitizenSchedule schedule
        )
        {
            int day = Time2WorkTimeSystem.GetDay(simulationFrame, timeData, ticksPerDay);
            float2 time2Lunch = new float2(-1, -1);
            float2 time2Work = new float2(-1, -1);
            float startWork = 0f;
            bool workFromHome = false;
            float offdayprob = cityservicesOffdayprob.x;
            int parttime_prob = partTimeProb;
            WorkType work = 0;

            bool dayOff = false;
            bool updateDay = true;

            if (isWorker)
            {
                work = GetWorkerOffDayAndPartTimeProb(
                    prefabRefs, propertyRenters, commercialLookup, industrialLookup,
                    officeLookup, workerData, out offdayprob, partTimeProb, out parttime_prob,
                    commercialOffdayprob, industryOffdayprob, officeOffdayprob, cityservicesOffdayprob,
                    dow, out remoteWorkProb);

                dayOff = Time2WorkWorkerSystem.IsTodayOffDay(citizen, ref economy, timeData, population, offdayprob, day);
                Time2WorkWorkerSystem.IsLunchTime(citizen, workerData, ref economy, normalizedTime, lunchBreakPct, simulationFrame, timeData, ticksPerDay, out time2Lunch);
                time2Work = Time2WorkWorkerSystem.GetTimeToWork(citizen, workerData, ref economy, true, lunchBreakPct, workStart, workEnd, delayFactor, ticksPerDay, parttime_prob, commuteTop10, overtime, partTimeReduction, out startWork);

                if(time2Lunch.y > time2Work.y)
                {
                    time2Lunch.y = time2Lunch.x + 0.05f;
                }
                workFromHome = Time2WorkWorkerSystem.IsTodayWorkFromHome(citizen, simulationFrame, timeData, ticksPerDay, remoteWorkProb);

                schedule.work_type = (int)work;  
            }
            else
            {
                time2Work = Time2WorkStudentSystem.GetTimeToStudy(citizen, studentData, ref economy, schoolStart, schoolEnd, ticksPerDay, out startWork);
                dayOff = Time2WorkStudentSystem.IsStudyDayOff(citizen, studentData, ref economy, day, population, schoolOffdayprob, schoolStart, schoolEnd, ticksPerDay);
            }

            schedule.dayoff = dayOff;
            schedule.start_lunch = time2Lunch.x;
            schedule.end_lunch = time2Lunch.y;
            schedule.work_from_home = workFromHome;
            schedule.start_work = startWork;
            schedule.end_work = time2Work.y;
            schedule.go_to_work = time2Work.x;
            schedule.day = day;

            return schedule;
        }
    }
}
