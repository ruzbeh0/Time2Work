using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Prefabs;
using Time2Work.Components;
using Unity.Entities;
using Unity.Mathematics;
using static Time2Work.Time2WorkWorkerSystem;
using Student = Game.Citizens.Student;

namespace Time2Work.Utils
{
    public static class CitizenScheduleHelper
    {
        public static CitizenSchedule CalculateScheduleForCitizen(
            Entity entity,
            Citizen citizen,
            ComponentLookup<Worker> workers,
            ComponentLookup<Student> students,
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
            float3 eventStart,
            float3 eventEnd,
            int remoteWorkProb
        )
        {
            var schedule = new CitizenSchedule();
            int day = Time2WorkTimeSystem.GetDay(simulationFrame, timeData, ticksPerDay);
            float2 time2Lunch = new float2(-1, -1);
            float2 time2Work = new float2(-1, -1);
            float startWork = 0f;
            bool workFromHome = false;
            float offdayprob = 60f;
            int parttime_prob = partTimeProb;
            WorkType work = 0;

            bool dayOff = false;

            if (workers.HasComponent(entity))
            {
                work = GetWorkerOffDayAndPartTimeProb(prefabRefs, propertyRenters, commercialLookup, industrialLookup,
                            officeLookup, workers[entity], out offdayprob, partTimeProb, out parttime_prob,
                            commercialOffdayprob, industryOffdayprob, officeOffdayprob, cityservicesOffdayprob,
                            dow, out remoteWorkProb);

                dayOff = Time2WorkWorkerSystem.IsTodayOffDay(citizen, ref economy, simulationFrame, timeData, population, normalizedTime, offdayprob, ticksPerDay, day);
                Time2WorkWorkerSystem.IsLunchTime(citizen, workers[entity], ref economy, normalizedTime, lunchBreakPct, simulationFrame, timeData, ticksPerDay, out time2Lunch);
                Time2WorkWorkerSystem.IsTimeToWork(citizen, workers[entity], ref economy, normalizedTime, lunchBreakPct, workStart, workEnd, delayFactor, ticksPerDay, parttime_prob, commuteTop10, overtime, partTimeReduction, out time2Work, out startWork);
                //Mod.log.Info($"{citizen.m_PseudoRandom},{workers[entity].m_Shift},{economy.m_WorkDayStart},{normalizedTime},{lunchBreakPct},{workStart},{workEnd},{delayFactor},{ticksPerDay},{parttime_prob},{commuteTop10},{overtime},{partTimeReduction},{time2Work},{startWork}");
                workFromHome = Time2WorkWorkerSystem.IsTodayWorkFromHome(citizen, simulationFrame, timeData, ticksPerDay, remoteWorkProb);

                schedule.work_type = (int)work;
                schedule.dayoff = dayOff;
            }

            if (students.HasComponent(entity))
            {
                float startStudy = 0f;
                bool studying = Time2WorkStudentSystem.IsTimeToStudy(citizen, students[entity], ref economy, normalizedTime, simulationFrame, timeData, population, schoolOffdayprob, schoolStart, schoolEnd, ticksPerDay, out time2Work, out startStudy);
                dayOff = studying;
            }

            schedule.start_work = time2Work.x;
            schedule.end_work = time2Work.y;
            schedule.go_to_work = startWork;
            schedule.start_lunch = time2Lunch.x;
            schedule.end_lunch = time2Lunch.y;
            schedule.work_from_home = workFromHome;
            schedule.day = day;

            return schedule;
        }
    }
}
