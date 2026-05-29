using Colossal.Mathematics;
using Game;
using Game.Prefabs;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace Time2Work.Systems
{
    public partial class HealthEventProbabilityScalerSystem : GameSystemBase
    {
        private readonly Dictionary<Entity, Bounds1> _baseOccurrenceProbabilities = new();

        private EntityQuery _query;
        private float _lastFactor = -1f;

        protected override void OnCreate()
        {
            base.OnCreate();

            _query = GetEntityQuery(new EntityQueryDesc()
            {
                All = new[]
                {
                    ComponentType.ReadWrite<HealthEventData>()
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

            float slowTimeFactor = Math.Max(Mod.m_Setting.slow_time_factor, 1f);
            if (Math.Abs(slowTimeFactor - _lastFactor) < 0.0001f)
            {
                Enabled = false;
                return;
            }

            using var prefabs = _query.ToEntityArray(Allocator.Temp);

            foreach (var prefab in prefabs)
            {
                var data = EntityManager.GetComponentData<HealthEventData>(prefab);

                if (!_baseOccurrenceProbabilities.TryGetValue(prefab, out var baseProbability))
                {
                    baseProbability = data.m_OccurenceProbability;
                    _baseOccurrenceProbabilities[prefab] = baseProbability;
                }

                data.m_OccurenceProbability = new Bounds1(
                    baseProbability.min / slowTimeFactor,
                    baseProbability.max / slowTimeFactor);

                EntityManager.SetComponentData(prefab, data);
            }

            _lastFactor = slowTimeFactor;
            Mod.log.Info($"Scaled healthcare event occurrence probabilities by 1/{slowTimeFactor:0.###}");

            Enabled = false;
        }

        protected override void OnDestroy()
        {
            if (_query != null && !_query.IsEmptyIgnoreFilter)
            {
                using var prefabs = _query.ToEntityArray(Allocator.Temp);

                foreach (var prefab in prefabs)
                {
                    if (!_baseOccurrenceProbabilities.TryGetValue(prefab, out var baseProbability))
                    {
                        continue;
                    }

                    var data = EntityManager.GetComponentData<HealthEventData>(prefab);
                    data.m_OccurenceProbability = baseProbability;
                    EntityManager.SetComponentData(prefab, data);
                }
            }

            _baseOccurrenceProbabilities.Clear();
            base.OnDestroy();
        }
    }
}
