using Game;
using Game.Prefabs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;

namespace Time2Work.Systems
{
    public partial class CompanyDisableNightNotificationSystem : GameSystemBase
    {
        private static int hour;
        private static int minute;

        protected override void OnCreate()
        {
            base.OnCreate();

            DateTime currentDateTime = World.GetExistingSystemManaged<Time2WorkTimeSystem>().GetCurrentDateTime();
            hour = currentDateTime.Hour;
        }

        protected override void OnUpdate()
        {
            DateTime currentDateTime = World.GetExistingSystemManaged<Time2WorkTimeSystem>().GetCurrentDateTime();
            hour = currentDateTime.Hour;
            if (hour > 23 || hour < 6)
            {
                World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Simulation.ServiceCompanySystem>().Enabled = false;
                World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Simulation.RentAdjustSystem>().Enabled = false;
            } else
            {
                World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Simulation.ServiceCompanySystem>().Enabled = true;
                World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Simulation.RentAdjustSystem>().Enabled = true;
            }
            
        }

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            // One day (or month) in-game is '262144' ticks
            return 262144 / 512;
        }
    }
}
