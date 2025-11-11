using Game;
using Game.Prefabs;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace Time2Work.Systems
{
    public partial class EconomyParameterUpdaterSystem : GameSystemBase
    {
        private EntityQuery _query;

        protected override void OnCreate()
        {
            base.OnCreate();

            _query = GetEntityQuery(new EntityQueryDesc()
            {
                All = new[] {
                    ComponentType.ReadWrite<EconomyParameterData>()
                }
            });

            RequireForUpdate(_query);
        }

        protected override void OnUpdate()
        {
            var prefabs = _query.ToEntityArray(Allocator.Temp);

            foreach (var tsd in prefabs)
            {
                EconomyParameterData data = EntityManager.GetComponentData<EconomyParameterData>(tsd);

                data.m_TrafficReduction = Mod.m_Setting.trafficReduction / (float)10000;
                data.m_ResourceConsumptionPerCitizen = Mod.m_Setting.resourceConsumption;
                EntityManager.SetComponentData<EconomyParameterData>(tsd, data);
            }
        }

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            // One day (or month) in-game is '262144' ticks
            return 262144 / 8;
        }
    }
}