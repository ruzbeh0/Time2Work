using Game;
using Game.Prefabs;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace Time2Work.Systems
{
    public partial class DemandParameterUpdaterSystem : GameSystemBase
    {
        private Dictionary<Entity, DemandParameterData> _demandParameterData = new Dictionary<Entity, DemandParameterData>();

        private EntityQuery _query;

        private Setting.DTSimulationEnum m_daytype;

        protected override void OnCreate()
        {
            base.OnCreate();

            _query = GetEntityQuery(new EntityQueryDesc()
            {
                All = new[] {
                    ComponentType.ReadWrite<DemandParameterData>()
                }
            });

            RequireForUpdate(_query);
        }

        protected override void OnUpdate()
        {
            var prefabs = _query.ToEntityArray(Allocator.Temp);

            foreach (var tsd in prefabs)
            {
                DemandParameterData data;

                if (!_demandParameterData.TryGetValue(tsd, out data))
                {
                    data = EntityManager.GetComponentData<DemandParameterData>(tsd);
                    _demandParameterData.Add(tsd, data);
                }

                bool updateCommuters = Mod.m_Setting.commuter_trips;

                if(updateCommuters)
                {
                    this.m_daytype = WeekSystem.currentDayOfTheWeek;
                    data.m_CommuterOCSpawnParameters = new Unity.Mathematics.float4(0.5f, 0.3f, 0.2f, 0.0f);
                } else
                {
                    this.m_daytype = Setting.DTSimulationEnum.AverageDay;
                    data.m_CommuterOCSpawnParameters = new Unity.Mathematics.float4(0.8f, 0.2f, 0.0f, 0.0f);
                }


                if (((int)this.m_daytype) == (int)Setting.DTSimulationEnum.AverageDay)
                {
                    data.m_CommuterWorkerRatioLimit = 8;
                } else if (((int)this.m_daytype) == (int)Setting.DTSimulationEnum.Weekend)
                {
                    data.m_CommuterWorkerRatioLimit = 9;
                } else
                {
                    data.m_CommuterWorkerRatioLimit = 7;
                }

                EntityManager.SetComponentData<DemandParameterData>(tsd, data);
            }
        }

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            // One day (or month) in-game is '262144' ticks
            return 262144 / 8;
        }
    }
}