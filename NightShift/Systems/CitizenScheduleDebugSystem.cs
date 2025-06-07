using Game;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Companies;
using Game.Prefabs;
using Game.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Time2Work.Components;
using Unity.Collections;
using Unity.Entities;

namespace Time2Work.Systems
{
    public partial class CitizenScheduleDebugSystem : GameSystemBase
    {
        private EntityQuery _query;

        protected override void OnCreate()
        {
            base.OnCreate();

            _query = GetEntityQuery(new EntityQueryDesc()
            {
                All = new[] {
                    ComponentType.ReadOnly<Citizen>(),
                    ComponentType.ReadOnly<Worker>(),
                    ComponentType.ReadOnly<CitizenSchedule>()
                },
                None =
                    [
                        ComponentType.Exclude<Deleted>(),
                        ComponentType.Exclude<Temp>()
                    ],
            });

            RequireForUpdate(_query);
        }

        protected override void OnUpdate()
        {
            using var entities = _query.ToEntityArray(Allocator.Temp);
            int[] workersPerHour = new int[24];
            int dayOffWorkers = 0;

            foreach (var entity in entities)
            {
                //Worker worker = EntityManager.GetComponentData<Worker>(entity);
                CitizenSchedule citizenSchedule = EntityManager.GetComponentData<CitizenSchedule>(entity);
                if(!citizenSchedule.dayoff && citizenSchedule.start_work >= 0)
                {
                    int hour = (int)(citizenSchedule.start_work * 24);
                    workersPerHour[hour]++;
                }
                if (!citizenSchedule.dayoff && citizenSchedule.start_work >= 0)
                {
                    dayOffWorkers++;
                }


            }

            for (int h = 0; h < 24; h++)
            {
                Mod.log.Info($"Hour:{h},Workers:{workersPerHour[h]}");
            }
            Mod.log.Info($"Workers with day Off:{dayOffWorkers}");
        }

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            // One day (or month) in-game is '262144' ticks
            return 262144 / 512;
        }
    }
}
