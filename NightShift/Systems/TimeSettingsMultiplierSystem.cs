using Game;
using Game.Prefabs;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace Time2Work.Systems
{
    public partial class TimeSettingsMultiplierSystem : GameSystemBase
    {
        private Dictionary<Entity, TimeSettingsData> _timeSettingsData = new Dictionary<Entity, TimeSettingsData>();

        private EntityQuery _query;

        protected override void OnCreate()
        {
            base.OnCreate();

            _query = GetEntityQuery(new EntityQueryDesc()
            {
                All = new[] {
                    ComponentType.ReadWrite<TimeSettingsData>()
                }
            });

            RequireForUpdate(_query);
        }

        protected override void OnUpdate()
        {
            var prefabs = _query.ToEntityArray(Allocator.Temp);

            foreach (var tsd in prefabs)
            {
                TimeSettingsData data;

                if (!_timeSettingsData.TryGetValue(tsd, out data))
                {
                    data = EntityManager.GetComponentData<TimeSettingsData>(tsd);
                    _timeSettingsData.Add(tsd, data);
                }

                float factor = Mod.m_Setting.daysPerMonth;

                Mod.log.Info($"Days Per Year Factor: {factor}");

                data.m_DaysPerYear = Math.Max((int)(Math.Floor(factor * data.m_DaysPerYear)), 1);
                EntityManager.SetComponentData<TimeSettingsData>(tsd, data);
            }

            Enabled = false;
        }
    }
}