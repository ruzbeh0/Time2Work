using Game;
using Game.Prefabs;
using Game.Simulation;
using System;
using System.Collections.Generic;
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

        protected override void OnCreate()
        {
            base.OnCreate();
            currentDayOfTheWeek = Mod.m_Setting.dt_simulation;
        }

        public override int GetUpdateOffset(SystemUpdatePhase phase)
        {
            // One day (or month) in-game is '262144' ticks
            return TimeSystem.kTicksPerDay / 8;
        }
        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            // One day (or month) in-game is '262144' ticks
            return TimeSystem.kTicksPerDay / 1;
        }

        protected override void OnUpdate()
        {
            DateTime currentDateTime = this.World.GetExistingSystemManaged<TimeSystem>().GetCurrentDateTime();

            if(Mod.m_Setting.dt_simulation.Equals(Setting.DTSimulationEnum.sevendayweek))

            {
                System.DayOfWeek dayOfWeek;

                dayOfWeek = (DayOfWeek)((currentDateTime.DayOfYear + 12*(currentDateTime.Year-2023))% 7);

                if(dayOfWeek.Equals(System.DayOfWeek.Saturday) || dayOfWeek.Equals(System.DayOfWeek.Sunday))
                {
                    currentDayOfTheWeek = Setting.DTSimulationEnum.Weekend;
                } else
                {
                    currentDayOfTheWeek = Setting.DTSimulationEnum.Weekday;
                }

                Mod.log.Info($"Day of the Week: {dayOfWeek}, Day Type: {currentDayOfTheWeek}");
            }
        }
    }
}
