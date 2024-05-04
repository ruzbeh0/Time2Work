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

namespace Time2Work
{
    public partial class WeekSystem : GameSystemBase
    {
        public static Setting.DTSimulationEnum currentDayOfTheWeek;
        private static float offdayprob;
        private static float school_offdayprob;

        protected override void OnCreate()
        {
            base.OnCreate();
            currentDayOfTheWeek = Mod.m_Setting.dt_simulation;
        }

        //public override int GetUpdateOffset(SystemUpdatePhase phase)
        //{
        //    // One day (or month) in-game is '262144' ticks
        //    return TimeSystem.kTicksPerDay / 8;
        //}
        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            // One day (or month) in-game is '262144' ticks
            return TimeSystem.kTicksPerDay / 32;
        }

        public static float getOffDayProb()
        {
            return offdayprob;
        }

        public static float getSchoolOffDayProb()
        {
            
            return school_offdayprob;
        }

        protected override void OnUpdate()
        {
            DateTime currentDateTime = this.World.GetExistingSystemManaged<TimeSystem>().GetCurrentDateTime();

            if (currentDateTime.Hour == 3)
            {
                System.DayOfWeek dayOfWeek;

                if(Mod.m_Setting.dt_simulation.Equals(Setting.DTSimulationEnum.sevendayweek))
                {
                    dayOfWeek = (DayOfWeek)((currentDateTime.DayOfYear + 12 * (currentDateTime.Year - 2023)) % 7);

                    if (dayOfWeek.Equals(System.DayOfWeek.Saturday) || dayOfWeek.Equals(System.DayOfWeek.Sunday))
                    {
                        currentDayOfTheWeek = Setting.DTSimulationEnum.Weekend;
                    }
                    else
                    {
                        currentDayOfTheWeek = Setting.DTSimulationEnum.Weekday;
                    }
                } else
                {
                    if (Mod.m_Setting.dt_simulation.Equals(Setting.DTSimulationEnum.Weekday))
                    {
                        currentDayOfTheWeek = Setting.DTSimulationEnum.Weekday;
                        dayOfWeek = System.DayOfWeek.Monday;
                    }
                    else
                    {
                        if (Mod.m_Setting.dt_simulation.Equals(Setting.DTSimulationEnum.Weekend))
                        {
                            currentDayOfTheWeek = Setting.DTSimulationEnum.Weekend;
                            dayOfWeek = System.DayOfWeek.Sunday;
                        } else
                        {
                            currentDayOfTheWeek = Setting.DTSimulationEnum.AverageDay;
                            dayOfWeek = System.DayOfWeek.Friday;
                        }
                    }
                }
                
                Mod.log.Info($"Day of the Week: {dayOfWeek}, Day Type: {currentDayOfTheWeek}");
            }

            //Work
            if (Mod.m_Setting.use_school_vanilla_timeoff)
            {
                offdayprob = 60f;
            }
            else
            {
                offdayprob = 100 * (Mod.m_Setting.vacation_per_year + Mod.m_Setting.holidays_per_year + 104f) / 365f;
                if (WeekSystem.currentDayOfTheWeek.Equals(Setting.DTSimulationEnum.Weekday))
                {
                    offdayprob /= 1.8f;
                    offdayprob = Math.Max(offdayprob, 5f);
                }
                else
                {
                    if (WeekSystem.currentDayOfTheWeek.Equals(Setting.DTSimulationEnum.Weekend))
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
                if (WeekSystem.currentDayOfTheWeek.Equals(Setting.DTSimulationEnum.Weekend))
                {
                    school_offdayprob = 100f;
                }
                else
                {
                    //On weekday, the probability of being on vacation does not include the number of weekends per year
                    if (WeekSystem.currentDayOfTheWeek.Equals(Setting.DTSimulationEnum.Weekday))
                    {
                        school_offdayprob = 100 * (Mod.m_Setting.school_vacation_per_year + Mod.m_Setting.holidays_per_year) / 365f;
                    }
                }
            }

            if (currentDateTime.Hour == 3)
            {
                Mod.log.Info($"Off Day Prob: {offdayprob}, School Off Day Prob: {school_offdayprob}");
            }
        }
    }
}
