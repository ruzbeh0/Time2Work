using Game;
using Game.Buildings;
using Game.Prefabs;
using System;
using Time2Work.Bridge;
using Time2Work.Localization;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Time2Work.Systems
{
    public partial class WeekSystem : GameSystemBase
    {
        public static Setting.DTSimulationEnum currentDayOfTheWeek;
        public static DayOfWeek dayOfWeek;
        private static DayOfWeek dayOfWeekTemp;
        private static float4 office_offdayprob;
        private static float4 commercial_offdayprob;
        private static float4 industry_offdayprob;
        private static float4 cityservices_offdayprob;
        private static float3 school_offdayprob; //x = elementary school, y = high school, z = college/univ
        private static int hour;
        private static int minute;
        private static int dayOfYear;
        private static int year;
        private static bool updated = false;
        private static Setting.months month;
        public static bool initialized = false;
        // Prevent duplicate "Happy New Year" chirps each year/day
        private int _lastNewYearChirpYear = -1;

        protected override void OnCreate()
        {
            base.OnCreate();
        }

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            // One day (or month) in-game is '262144' ticks
            return 262144 / 512;
        }

        public static string getDayOfWeek()
        {
            return dayOfWeekTemp.ToString();
        }

        public static int getDayOfWeekInt()
        {
            return (int)dayOfWeekTemp;
        }

        public static float4 getOfficeOffDayProb()
        {
            return office_offdayprob;
        }

        public static float4 getCommercialOffDayProb()
        {
            return commercial_offdayprob;
        }

        public static float4 getIndustryOffDayProb()
        {
            return industry_offdayprob;
        }

        public static float4 getCityServicesOffDayProb()
        {
            return cityservices_offdayprob;
        }

        public static float3 getSchoolOffDayProb()
        {

            return school_offdayprob;
        }

        //Get probabilities of time off for each day of the week
        private static float4 getTimeOffProbByDayOfWeek()
        {
            //52 weeks per year
            float4 offdayprob = new float4();
            offdayprob.x = 100 * (Mod.m_Setting.vacation_per_year + Mod.m_Setting.holidays_per_year) / 365f;
            offdayprob.y = 100 * (Mod.m_Setting.vacation_per_year + Mod.m_Setting.holidays_per_year) / 365f;
            offdayprob.z = 100 * (Mod.m_Setting.vacation_per_year + Mod.m_Setting.holidays_per_year) / 365f;
            offdayprob.w = 100 * (Mod.m_Setting.vacation_per_year + Mod.m_Setting.holidays_per_year) / 365f;

            return offdayprob;
        }

        public static void updateProbabilities()
        {
            //Work
            if (Mod.m_Setting.use_vanilla_timeoff)
            {
                office_offdayprob = 60f;
                commercial_offdayprob = 60f;
                industry_offdayprob = 60f;
                cityservices_offdayprob = 60f;
            }
            else
            {
                //Probability of off day per day of the week. x = weekday, y = friday, z = saturday, w = sunday

                //To calculate probability of time off we will assume the probability of being a holiday
                //and the probability of going to work on this day are independent
                office_offdayprob = 100 - getTimeOffProbByDayOfWeek();
                commercial_offdayprob = 100 - getTimeOffProbByDayOfWeek();
                industry_offdayprob = 100 - getTimeOffProbByDayOfWeek();
                cityservices_offdayprob = 100 - getTimeOffProbByDayOfWeek();

                // Weekday

                office_offdayprob.x *= Mod.m_Setting.office_weekday_pct / 100f;
                office_offdayprob.x = Math.Max(100 - office_offdayprob.x, 5f);

                commercial_offdayprob.x *= Mod.m_Setting.commercial_weekday_pct / 100f;
                commercial_offdayprob.x = Math.Max(100 - commercial_offdayprob.x, 5f);

                industry_offdayprob.x *= Mod.m_Setting.industry_weekday_pct / 100f;
                industry_offdayprob.x = Math.Max(100 - industry_offdayprob.x, 5f);

                cityservices_offdayprob.x *= Mod.m_Setting.cityServices_weekday_pct / 100f;

                cityservices_offdayprob.x = Math.Max(100 - cityservices_offdayprob.x, 5f);

                // Average Day / Friday

                office_offdayprob.y *= Mod.m_Setting.office_avgday_pct / 100f;
                office_offdayprob.y = Math.Max(100 - office_offdayprob.y, 5f);

                commercial_offdayprob.y *= Mod.m_Setting.commercial_avgday_pct / 100f;
                commercial_offdayprob.y = Math.Max(100 - commercial_offdayprob.y, 5f);

                industry_offdayprob.y *= Mod.m_Setting.industry_avgday_pct / 100f;
                industry_offdayprob.y = Math.Max(100 - industry_offdayprob.y, 5f);

                cityservices_offdayprob.y *= Mod.m_Setting.cityServices_avgday_pct / 100f;
                cityservices_offdayprob.y = Math.Max(100 - cityservices_offdayprob.y, 5f);

                // Saturday

                office_offdayprob.z *= Mod.m_Setting.office_sat_pct / 100f;
                office_offdayprob.z = Math.Max(100 - office_offdayprob.z, 5f);

                commercial_offdayprob.z *= Mod.m_Setting.commercial_sat_pct / 100f;
                commercial_offdayprob.z = Math.Max(100 - commercial_offdayprob.z, 5f);

                industry_offdayprob.z *= Mod.m_Setting.industry_sat_pct / 100f;
                industry_offdayprob.z = Math.Max(100 - industry_offdayprob.z, 5f);

                cityservices_offdayprob.z *= Mod.m_Setting.cityServices_sat_pct / 100f;
                cityservices_offdayprob.z = Math.Min(100 - cityservices_offdayprob.z, 95f);

                // Sunday

                office_offdayprob.w *= Mod.m_Setting.office_sun_pct / 100f;
                office_offdayprob.w = Math.Max(100 - office_offdayprob.w, 5f);

                commercial_offdayprob.w *= Mod.m_Setting.commercial_sun_pct / 100f;
                commercial_offdayprob.w = Math.Max(100 - commercial_offdayprob.w, 5f);

                industry_offdayprob.w *= Mod.m_Setting.industry_sun_pct / 100f;
                industry_offdayprob.w = Math.Max(100 - industry_offdayprob.w, 5f);

                cityservices_offdayprob.w *= Mod.m_Setting.cityServices_sun_pct / 100f;
                cityservices_offdayprob.w = Math.Min(100 - cityservices_offdayprob.w, 96f);
            }

            //School
            if (Mod.m_Setting.use_school_vanilla_timeoff)
            {
                school_offdayprob = 60f;
            }
            else
            {
                //float holiday_prob = (Mod.m_Setting.school_vacation_per_year + Mod.m_Setting.holidays_per_year) / 365f;
                float holiday_prob = (Mod.m_Setting.holidays_per_year) / 365f;
                if (month.Equals(Mod.m_Setting.school_vacation_month1) ||
                    month.Equals(Mod.m_Setting.school_vacation_month2))
                {
                    holiday_prob = 1;
                }
                if (currentDayOfTheWeek.Equals(Setting.DTSimulationEnum.AverageDay))
                {
                    //Value for an average day/Friday
                    school_offdayprob.x = 1 - (1 - holiday_prob) * (Mod.m_Setting.school_lv1_avgday_pct) / 100f;
                    school_offdayprob.y = 1 - (1 - holiday_prob) * (Mod.m_Setting.school_lv2_avgday_pct) / 100f;
                    school_offdayprob.z = 1 - (1 - holiday_prob) * (Mod.m_Setting.school_lv34_avgday_pct) / 100f;
                }
                else if (currentDayOfTheWeek.Equals(Setting.DTSimulationEnum.Saturday))
                {
                    school_offdayprob.x = 1 - (1 - holiday_prob) * (Mod.m_Setting.school_lv1_saturday_pct) / 100f;
                    school_offdayprob.y = 1 - (1 - holiday_prob) * (Mod.m_Setting.school_lv2_saturday_pct) / 100f;
                    school_offdayprob.z = 1 - (1 - holiday_prob) * (Mod.m_Setting.school_lv34_saturday_pct) / 100f;
                }
                else if (currentDayOfTheWeek.Equals(Setting.DTSimulationEnum.Sunday))
                {
                    school_offdayprob.x = 1 - (1 - holiday_prob) * (Mod.m_Setting.school_lv1_sunday_pct) / 100f;
                    school_offdayprob.y = 1 - (1 - holiday_prob) * (Mod.m_Setting.school_lv2_sunday_pct) / 100f;
                    school_offdayprob.z = 1 - (1 - holiday_prob) * (Mod.m_Setting.school_lv34_sunday_pct) / 100f;
                }
                else
                {
                    if (currentDayOfTheWeek.Equals(Setting.DTSimulationEnum.Weekday))
                    {
                        school_offdayprob.x = 1 - (1 - holiday_prob) * (Mod.m_Setting.school_lv1_weekday_pct) / 100f;
                        school_offdayprob.y = 1 - (1 - holiday_prob) * (Mod.m_Setting.school_lv2_weekday_pct) / 100f;
                        school_offdayprob.z = 1 - (1 - holiday_prob) * (Mod.m_Setting.school_lv34_weekday_pct) / 100f;
                    }
                }

                Mod.log.Info($"holidayprob:{holiday_prob},school_offdayprob:{school_offdayprob},month:{month}");

                school_offdayprob *= 100f;
            }
        }

        protected override void OnUpdate()
        {
            DateTime currentDateTime = World.GetExistingSystemManaged<Time2WorkTimeSystem>().GetCurrentDateTime();
            hour = currentDateTime.Hour;
            minute = currentDateTime.Minute;
            dayOfYear = currentDateTime.DayOfYear;
            year = currentDateTime.Year;
            int day = Mathf.FloorToInt(dayOfYear / (float)Mod.m_Setting.daysPerMonth);
            month = (Setting.months)((day % 12 + 12) % 12 + 1);


            if (!initialized || (hour == 0 && !updated) || dayOfWeekTemp < 0 || currentDayOfTheWeek.Equals(Setting.DTSimulationEnum.sevendayweek))
            {
                int dow = ((dayOfYear + 12 * (year - 1953)) % 7);
                dayOfWeekTemp = (DayOfWeek)dow;
                if (Mod.m_Setting.dt_simulation.Equals(Setting.DTSimulationEnum.AverageDay))
                {
                    dayOfWeekTemp = DayOfWeek.Friday;
                    if (Mod.m_Setting.dt_simulation.Equals(Setting.DTSimulationEnum.Weekday))
                    {
                        dayOfWeekTemp = DayOfWeek.Monday;
                    }
                    else
                    {
                        if (Mod.m_Setting.dt_simulation.Equals(Setting.DTSimulationEnum.Saturday))
                        {
                            dayOfWeekTemp = DayOfWeek.Saturday;
                        }
                        else if (Mod.m_Setting.dt_simulation.Equals(Setting.DTSimulationEnum.Sunday))
                        {
                            dayOfWeekTemp = DayOfWeek.Sunday;
                        }
                    }
                }
                updated = true;
            }

            //The day of the week actually changes at 3 AM since this is the hour with least activity
            if (!initialized || hour == 3 && minute < 4 || currentDayOfTheWeek.Equals(Setting.DTSimulationEnum.sevendayweek))
            {
                if (Mod.m_Setting.dt_simulation.Equals(Setting.DTSimulationEnum.sevendayweek))
                {
                    int dow = ((dayOfYear + 12 * (year - 1953)) % 7);

                    dayOfWeek = (DayOfWeek)dow;

                    if (dayOfWeek.Equals(DayOfWeek.Saturday))
                    {
                        currentDayOfTheWeek = Setting.DTSimulationEnum.Saturday;
                    }
                    else if (dayOfWeek.Equals(DayOfWeek.Sunday))
                    {
                        currentDayOfTheWeek = Setting.DTSimulationEnum.Sunday;
                    }
                    else
                    {
                        if (dayOfWeek.Equals(DayOfWeek.Friday))
                        {

                            currentDayOfTheWeek = Setting.DTSimulationEnum.AverageDay;
                        }
                        else
                        {
                            currentDayOfTheWeek = Setting.DTSimulationEnum.Weekday;
                        }
                    }
                }
                else
                {
                    if (Mod.m_Setting.dt_simulation.Equals(Setting.DTSimulationEnum.Weekday))
                    {
                        currentDayOfTheWeek = Setting.DTSimulationEnum.Weekday;
                        dayOfWeek = DayOfWeek.Monday;
                    }
                    else
                    {
                        if (Mod.m_Setting.dt_simulation.Equals(Setting.DTSimulationEnum.Saturday))
                        {
                            currentDayOfTheWeek = Setting.DTSimulationEnum.Saturday;
                            dayOfWeek = DayOfWeek.Saturday;
                        }
                        else if (Mod.m_Setting.dt_simulation.Equals(Setting.DTSimulationEnum.Sunday))
                        {
                            currentDayOfTheWeek = Setting.DTSimulationEnum.Sunday;
                            dayOfWeek = DayOfWeek.Sunday;
                        }
                        else
                        {
                            currentDayOfTheWeek = Setting.DTSimulationEnum.AverageDay;
                            dayOfWeek = DayOfWeek.Friday;
                        }
                    }
                }

                Mod.log.Info($"Day of the Week: {dayOfWeek}, Day Type: {currentDayOfTheWeek}, Month: {month}");
                updateProbabilities();
                Mod.log.Info($"Office Off Day Prob: {office_offdayprob}");
                Mod.log.Info($"Commercial Off Day Prob: {commercial_offdayprob}");
                Mod.log.Info($"Industry Off Day Prob: {industry_offdayprob}");
                Mod.log.Info($"City Services Day Prob: {cityservices_offdayprob}");
                Mod.log.Info($"School Off Day Prob: {school_offdayprob}");
                updated = false;
                initialized = true;
            }

            TrySendNewYearChirp();

        }

        private void TrySendNewYearChirp()
        {
            // We’ll post in the first few minutes after midnight to avoid missing the exact tick.
            var t2w = World.GetExistingSystemManaged<Time2WorkTimeSystem>();
            var now = t2w.GetCurrentDateTime();

            // Guard: only on Jan 1, first ~5 minutes, and once per year
            if (now.Month == 1 && now.Day == 1 && now.Hour == 0 && now.Minute < 5)
            {
                if (_lastNewYearChirpYear != now.Year)
                {
                    PostHappyNewYearChirp();
                    _lastNewYearChirpYear = now.Year;
                }
            }
        }

        private void PostHappyNewYearChirp()
        {
            if (!CustomChirpsBridge.IsAvailable)
                return;

            Entity target = Entity.Null;

            CustomChirpsBridge.PostChirp(T2WStrings.T("t2w.chirp.holiday.new_year"), DepartmentAccountBridge.Transportation, target, T2WStrings.T("t2w.chirp.mod_name"));
        }
    }
}
