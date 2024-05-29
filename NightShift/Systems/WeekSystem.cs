using Game;
using Game.Prefabs;
using Game.Simulation;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using static System.Net.Mime.MediaTypeNames;

namespace Time2Work.Systems
{
    public partial class WeekSystem : GameSystemBase
    {
        public static Setting.DTSimulationEnum currentDayOfTheWeek;
        public static DayOfWeek dayOfWeek;
        private static DayOfWeek dayOfWeekTemp;
        private static float offdayprob;
        private static float school_offdayprob;
        private static int hour;
        private static int minute;
        private static int dayOfYear;
        private static int year;
        private static bool updated = false; 

        protected override void OnCreate()
        {
            base.OnCreate();
            currentDayOfTheWeek = Mod.m_Setting.dt_simulation;

            DateTime currentDateTime = World.GetExistingSystemManaged<Time2WorkTimeSystem>().GetCurrentDateTime();
            hour = currentDateTime.Hour;
            dayOfYear = currentDateTime.DayOfYear;
            year = currentDateTime.Year;

            dayOfWeekTemp = (DayOfWeek)((dayOfYear + 12 * (year - 2023)) % 7);
            if (Mod.m_Setting.dt_simulation.Equals(Setting.DTSimulationEnum.AverageDay))
            {
                dayOfWeekTemp = DayOfWeek.Friday;
                if (Mod.m_Setting.dt_simulation.Equals(Setting.DTSimulationEnum.Weekday))
                {
                    dayOfWeekTemp = DayOfWeek.Monday;
                }
                else
                {
                    if (Mod.m_Setting.dt_simulation.Equals(Setting.DTSimulationEnum.Weekend))
                    {
                        dayOfWeekTemp = DayOfWeek.Sunday;
                    }
                }
            }

            updateProbabilities();
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

        public static float getOffDayProb()
        {
            return offdayprob;
        }

        public static float getSchoolOffDayProb()
        {

            return school_offdayprob;
        }

        public static void updateProbabilities()
        {
            //Work
            if (Mod.m_Setting.use_school_vanilla_timeoff)
            {
                offdayprob = 60f;
            }
            else
            {
                offdayprob = 100 * (Mod.m_Setting.vacation_per_year + Mod.m_Setting.holidays_per_year + 104f) / 365f;
                if (currentDayOfTheWeek.Equals(Setting.DTSimulationEnum.Weekday))
                {
                    offdayprob /= 1.8f;
                    offdayprob = Math.Max(offdayprob, 5f);
                }
                else
                {
                    if (currentDayOfTheWeek.Equals(Setting.DTSimulationEnum.Weekend))
                    {
                        offdayprob *= 2.1f;
                        offdayprob = Math.Min(offdayprob, 95f);
                    }
                }
            }

            //School
            //Value for an average day
            school_offdayprob = 100 * (Mod.m_Setting.school_vacation_per_year + Mod.m_Setting.holidays_per_year + 104f) / 365f;
            if (Mod.m_Setting.use_school_vanilla_timeoff)
            {
                school_offdayprob = 60f;
            }
            else
            {
                //On weekend, schools are closed
                if (currentDayOfTheWeek.Equals(Setting.DTSimulationEnum.Weekend))
                {
                    school_offdayprob = 100f;
                }
                else
                {
                    //On weekday, the probability of being on vacation does not include the number of weekends per year
                    if (currentDayOfTheWeek.Equals(Setting.DTSimulationEnum.Weekday))
                    {
                        school_offdayprob = 100 * (Mod.m_Setting.school_vacation_per_year + Mod.m_Setting.holidays_per_year) / 365f;
                    }
                }
            }
        }

        protected override void OnUpdate()
        {
            DateTime currentDateTime = World.GetExistingSystemManaged<Time2WorkTimeSystem>().GetCurrentDateTime();
            hour = currentDateTime.Hour;
            minute = currentDateTime.Minute;
            dayOfYear = currentDateTime.DayOfYear;
            year = currentDateTime.Year;

            if ((hour == 0 && minute < 4 && !updated) || dayOfWeekTemp < 0)
            {
                dayOfWeekTemp = (DayOfWeek)((dayOfYear + 12 * (year - 2023)) % 7);
                if (Mod.m_Setting.dt_simulation.Equals(Setting.DTSimulationEnum.AverageDay))
                {
                    dayOfWeekTemp = DayOfWeek.Friday;
                    if(Mod.m_Setting.dt_simulation.Equals(Setting.DTSimulationEnum.Weekday))
                    {
                        dayOfWeekTemp = DayOfWeek.Monday;
                    } else
                    {
                        if (Mod.m_Setting.dt_simulation.Equals(Setting.DTSimulationEnum.Weekend))
                        {
                            dayOfWeekTemp = DayOfWeek.Sunday;
                        }
                    }
                }
            }

            //The day of the week actually changes at 3 AM since this is the hour with least activity
            if (hour == 3 && minute < 4)
            {
                if (Mod.m_Setting.dt_simulation.Equals(Setting.DTSimulationEnum.sevendayweek))
                {
                    dayOfWeek = (DayOfWeek)((dayOfYear + 12 * (year - 2023)) % 7);

                    if (dayOfWeek.Equals(DayOfWeek.Saturday) || dayOfWeek.Equals(DayOfWeek.Sunday))
                    {
                        currentDayOfTheWeek = Setting.DTSimulationEnum.Weekend;
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
                        if (Mod.m_Setting.dt_simulation.Equals(Setting.DTSimulationEnum.Weekend))
                        {
                            currentDayOfTheWeek = Setting.DTSimulationEnum.Weekend;
                            dayOfWeek = DayOfWeek.Sunday;
                        }
                        else
                        {
                            currentDayOfTheWeek = Setting.DTSimulationEnum.AverageDay;
                            dayOfWeek = DayOfWeek.Friday;
                        }
                    }
                }

                Mod.log.Info($"Day of the Week: {dayOfWeek}, Day Type: {currentDayOfTheWeek}");
                updateProbabilities();
                Mod.log.Info($"Off Day Prob: {offdayprob}, School Off Day Prob: {school_offdayprob}");
                updated = true;
            } else
            {
                if(hour == 3 && minute < 10)
                {
                    updated = false;
                } 
            }
        }
    }
}
