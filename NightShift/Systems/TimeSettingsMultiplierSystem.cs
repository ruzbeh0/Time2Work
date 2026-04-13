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
        private readonly Dictionary<Entity, TimeSettingsData> _baseTimeSettingsData = new();

        private EntityQuery _query;

        protected override void OnCreate()
        {
            base.OnCreate();

            _query = GetEntityQuery(new EntityQueryDesc()
            {
                All = new[]
                {
                    ComponentType.ReadWrite<TimeSettingsData>()
                }
            });

            RequireForUpdate(_query);
        }

        protected override void OnUpdate()
        {
            if (Mod.m_Setting == null)
            {
                return;
            }

            using var prefabs = _query.ToEntityArray(Allocator.Temp);

            foreach (var tsd in prefabs)
            {
                // Cache the original/base game value once
                if (!_baseTimeSettingsData.TryGetValue(tsd, out var baseData))
                {
                    baseData = EntityManager.GetComponentData<TimeSettingsData>(tsd);
                    _baseTimeSettingsData[tsd] = baseData;
                }

                var updatedData = baseData;
                int factor = Math.Max(Mod.m_Setting.daysPerMonth, 1);

                updatedData.m_DaysPerYear = Math.Max(baseData.m_DaysPerYear * factor, 1);

                Mod.log.Info($"DaysPerMonth={factor}, baseDaysPerYear={baseData.m_DaysPerYear}, newDaysPerYear={updatedData.m_DaysPerYear}");

                EntityManager.SetComponentData(tsd, updatedData);
            }

            // Run once, then wait until Apply() re-enables us
            Enabled = false;
        }
    }
}