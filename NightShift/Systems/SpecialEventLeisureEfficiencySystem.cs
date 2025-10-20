// SpecialEventLeisureEfficiencySystem.cs
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

using Game.Prefabs;                 // PrefabRef, LeisureProviderData
using Game;                         // GameSystemBase, SystemUpdatePhase
using Time2Work;                    // Time2WorkTimeSystem
using Time2Work.Components;         // SpecialEventData

namespace Time2Work.Systems
{
    /// <summary>
    /// During a special event's active time window, multiply the venue's PREFAB
    /// LeisureProviderData.m_Efficiency by 1000 (one-time). When not active,
    /// if efficiency > 100, divide by 1000 to restore. No backups, prefab-wide effect.
    /// </summary>
    public partial class SpecialEventLeisureEfficiencySystem : GameSystemBase
    {
        private EntityQuery _venueQuery;
        private const int EfficiencyBoostFactor = 1000;

        protected override void OnCreate()
        {
            base.OnCreate();

            _venueQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<PrefabRef>(),
                    ComponentType.ReadWrite<SpecialEventData>()
                }
            });

            // Only run when there are potential venues to process
            RequireForUpdate(_venueQuery);
        }

        protected override void OnUpdate()
        {
            // If our time system isn't around yet (e.g., during load), bail safely.
            var t2w = World.GetExistingSystemManaged<Time2WorkTimeSystem>();
            if (t2w == null)
                return;

            float now = t2w.normalizedTime; // 0..1, current time-of-day

            var em = EntityManager;

            // We'll collect prefabs we saw and which ones are active right now,
            // so we avoid conflicting writes when multiple venues share a prefab.
            var seenPrefabs = new HashSet<Entity>();
            var activePrefabs = new HashSet<Entity>();

            using var venues = _venueQuery.ToEntityArray(Allocator.Temp);
            foreach (var venue in venues)
            {
                if (!em.HasComponent<PrefabRef>(venue) || !em.HasComponent<SpecialEventData>(venue))
                    continue;

                var sed = em.GetComponentData<SpecialEventData>(venue);
                var prefab = em.GetComponentData<PrefabRef>(venue).m_Prefab;

                // Track that we saw this prefab
                seenPrefabs.Add(prefab);

                // Active window check (wrap-safe): start..end, where time is 0..1
                float start = sed.start_time - 1.5f/24f;
                float end = sed.start_time + sed.duration*0.6f;
                bool active = (start <= end)
                              ? (now >= start && now <= end)
                              : (now >= start || now <= math.frac(end));

                if (active)
                    activePrefabs.Add(prefab);
            }

            // Apply boosts/restores on the PREFAB (chooser reads provider from prefab)
            foreach (var prefab in seenPrefabs)
            {
                if (!em.Exists(prefab) || !em.HasComponent<LeisureProviderData>(prefab))
                    continue;

                var lp = em.GetComponentData<LeisureProviderData>(prefab);

                if (activePrefabs.Contains(prefab))
                {
                    // Boost once: only multiply if it's still in the normal range
                    if (lp.m_Efficiency <= 200)
                    {
                        lp.m_Efficiency *= EfficiencyBoostFactor;
                        em.SetComponentData(prefab, lp);
                    }
                }
                else
                {
                    // Not active: if it looks boosted, restore by dividing
                    if (lp.m_Efficiency > 200)
                    {
                        lp.m_Efficiency /= EfficiencyBoostFactor;
                        em.SetComponentData(prefab, lp);
                    }
                }
            }
        }

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            // Update ~16 times per in-game day (responsive to 2–4 hour events)
            // One in-game day ~ 262144 ticks
            return 262144 / 16;
        }
    }
}
