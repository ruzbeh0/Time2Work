// SpecialEventChirpSystem.cs

// Bridge (entity/prefab link support)
using Colossal.Entities;
using Game;
using Game.Buildings;
using Game.Common;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using System;
using System.Collections.Generic;
using Time2Work.Bridge; // CustomChirpsBridge, DepartmentAccountBridge
using Time2Work.Components;  // SpecialEventData
using Time2Work.Localization;
using Time2Work.Systems;     // WeekSystem, Time2WorkTimeSystem, SpecialEventsUISystem
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using static Game.Prefabs.TriggerPrefabData;

namespace Time2Work.Systems
{
    /// <summary>
    /// Posts chirps for special events:
    ///  - Daily announcement per event (once per sim day) AFTER events actually exist
    ///  - 5 in-game minutes before start
    ///  - 5 in-game minutes before end
    /// Uses Building link if available; otherwise Prefab link; finally plain text if neither is present.
    /// </summary>
    public partial class SpecialEventChirpSystem : GameSystemBase
    {
        private EntityQuery _eventsQ;
        private EntityQuery _timeDataQ;

        private Time2WorkTimeSystem _time;
        private SimulationSystem _sim;

        private ComponentTypeHandle<PrefabRef> _prefabType;
        private EntityTypeHandle _entityType;

        private float _lastTimeNorm = -1f;

        // Daily state
        private int _currentSimDay = -1;
        private bool _dailyAnnounced = false; // announce when first valid event appears

        private const float FiveMinutes = 5f / (24f * 60f);
        private const float MinValidDuration = FiveMinutes;

        private struct SpecialEventChirpState : IComponentData
        {
            public int lastAnnounceDay;
            public byte startSoonSent;
            public byte endSoonSent;
        }

        public override int GetUpdateInterval(SystemUpdatePhase phase) => 16;

        protected override void OnCreate()
        {
            base.OnCreate();

            _time = World.GetExistingSystemManaged<Time2WorkTimeSystem>();
            _sim = World.GetOrCreateSystemManaged<SimulationSystem>();

            _eventsQ = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<PrefabRef>(),
                    ComponentType.ReadOnly<AttractivenessProvider>(),
                    ComponentType.ReadOnly<SpecialEventData>()
                },
                None = new[] { ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>() }
            });

            _timeDataQ = GetEntityQuery(ComponentType.ReadOnly<TimeData>());

            RequireForUpdate(_eventsQ);
            RequireForUpdate(_timeDataQ);
        }

        protected override void OnUpdate()
        {
            if (!WeekSystem.initialized)
                return; // wait until WeekSystem has produced daily state/events

            if (_eventsQ.IsEmptyIgnoreFilter)
                return;

            int simDay = GetCurrentSimDay();
            if (simDay != _currentSimDay)
            {
                _currentSimDay = simDay;
                _dailyAnnounced = false;
            }

           
            DateTime currentDateTime = World.GetExistingSystemManaged<Time2WorkTimeSystem>().GetCurrentDateTime();
            int hour = currentDateTime.Hour;

            // Count valid events for today
            using var ents = _eventsQ.ToEntityArray(Allocator.Temp);

            try
            {
                int nEvents = SpecialEventSystem.numberEvents;

                // Announce once we actually have at least one valid event this day
                if (!_dailyAnnounced && nEvents > 0 && hour >= 5)
                {
                    Mod.log.Info($"Posting daily chirp for {nEvents} events on day {simDay}");
                    TryPostDailyAnnouncements(simDay);
                    _dailyAnnounced = true;
                }

                // Imminent warnings
                TryPostImminentWarnings(simDay);

                _lastTimeNorm = _time.normalizedTime;
            }
            finally
            {
                ents.Dispose();

            }
        }

        // ---- Daily announcements ----

        private void TryPostDailyAnnouncements(int todaySimDay)
        {
            if (!CustomChirpsBridge.IsAvailable)
                return;

            using var entities = _eventsQ.ToEntityArray(Allocator.Temp);
            using var prefabRefs = _eventsQ.ToComponentDataArray<PrefabRef>(Allocator.Temp);

            // We'll keep track of unique locations for today to avoid duplicates
            var prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();

            for (int i = 0; i < entities.Length; i++)
            {
                var ent = entities[i];
                var prefabRef = prefabRefs[i];

                // Event data lives on the prefab
                if (!EntityManager.TryGetComponent<SpecialEventData>(ent, out var sed))
                    continue;

                if (!IsValidForToday(sed, todaySimDay))
                    continue;

                // Human-readable location
                string location = prefabSystem.GetPrefabName(prefabRef.m_Prefab);
                location = SpecialEventsUISystem.SanitizeString(location);
                if (string.IsNullOrWhiteSpace(location) || location == "Unknown")
                    continue; // don't surface blank locations

                if(ent.Index != sed.entity_index)
                {
                    continue;
                }

                var (sh, sm) = ToHM(sed.start_time);
                var (eh, em) = ToHM(math.frac(sed.start_time + sed.duration));
                string msg = T2WStrings.T("t2w.chirp.special_event.today",
                          ("start", $"{Two(sh)}:{Two(sm)}"),
                          ("end", $"{Two(eh)}:{Two(em)}"));

                Mod.log.Info($"Posting chirp for event entity {ent.Index}");
                // Use prefab link (parks & attractions without a discrete Building entity)
                CustomChirpsBridge.PostChirp(
                    text: msg,
                    department: DepartmentAccountBridge.Transportation,
                    entity: ent,
                    customSenderName: T2WStrings.T("t2w.chirp.mod_name")
                );
            }
        }

        // ---- Imminent warnings ----

        private void TryPostImminentWarnings(int todaySimDay)
        {
            if (!CustomChirpsBridge.IsAvailable)
                return;

            float tPrev = _lastTimeNorm;
            float tNow = _time.normalizedTime;
            if (tPrev < 0f) return; // first tick: no edge detect

            using var entities = _eventsQ.ToEntityArray(Allocator.Temp);
            using var prefabRefs = _eventsQ.ToComponentDataArray<PrefabRef>(Allocator.Temp);

            // We'll keep track of unique locations for today to avoid duplicates
            var prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();

            var stateLookup = GetComponentLookup<SpecialEventChirpState>(isReadOnly: false);

            for (int i = 0; i < entities.Length; i++)
            {
                var ent = entities[i];
                var prefabRef = prefabRefs[i];

                // Event data lives on the prefab
                if (!EntityManager.TryGetComponent<SpecialEventData>(ent, out var sed))
                    continue;

                if (!IsValidForToday(sed, todaySimDay))
                    continue;

                // Human-readable location
                string location = prefabSystem.GetPrefabName(prefabRef.m_Prefab);
                location = SpecialEventsUISystem.SanitizeString(location);
                if (string.IsNullOrWhiteSpace(location) || location == "Unknown")
                    continue; // don't surface blank locations

                if (ent.Index != sed.entity_index)
                {
                    continue;
                }


                // get/create per-event state
                SpecialEventChirpState st;
                if (!stateLookup.HasComponent(ent))
                {
                    st = new SpecialEventChirpState { lastAnnounceDay = -1, startSoonSent = 0, endSoonSent = 0 };
                    EntityManager.AddComponentData(ent, st);
                }
                else
                {
                    st = stateLookup[ent];
                }

                if (st.lastAnnounceDay != todaySimDay)
                {
                    st.lastAnnounceDay = todaySimDay;
                    st.startSoonSent = 0;
                    st.endSoonSent = 0;
                }

                float tStart = math.frac(sed.start_time);
                float tEnd = math.frac(sed.start_time + sed.duration);
                float tStartWarn = math.frac(tStart - 3*FiveMinutes);
                float tEndWarn = math.frac(tEnd - 3*FiveMinutes);

                string startSoon = T2WStrings.T("t2w.chirp.special_event.starting");
                string endSoon = T2WStrings.T("t2w.chirp.special_event.ending");

                if (st.startSoonSent == 0 && Crossed(tPrev, tNow, tStartWarn))
                {
                    CustomChirpsBridge.PostChirp(startSoon, DepartmentAccountBridge.Transportation, ent, T2WStrings.T("t2w.chirp.mod_name"));

                    st.startSoonSent = 1;
                }

                if (st.endSoonSent == 0 && Crossed(tPrev, tNow, tEndWarn))
                {
                    CustomChirpsBridge.PostChirp(endSoon, DepartmentAccountBridge.Transportation, ent, T2WStrings.T("t2w.chirp.mod_name"));

                    st.endSoonSent = 1;
                }

                stateLookup[ent] = st;
            }
        }

        // ---- Helpers ----

        private int GetCurrentSimDay()
        {
            var timeData = _timeDataQ.GetSingleton<TimeData>();
            return Time2WorkTimeSystem.GetDay(_sim.frameIndex, timeData);
        }

        private static (int H, int M) ToHM(float normalizedTime)
        {
            float hours = normalizedTime * 24f;
            int h = (int)math.floor(hours);
            int m = (int)math.round((hours - h) * 60f);
            if (m == 60) { m = 0; h = (h + 1) % 24; }
            return (h, m);
        }

        private static string Two(int v) => v < 10 ? $"0{v}" : v.ToString();

        private static bool Crossed(float prev, float now, float threshold)
        {
            prev = math.frac(prev);
            now = math.frac(now);
            return (prev <= now) ? (prev < threshold && threshold <= now)
                                 : (prev < threshold || threshold <= now); // midnight wrap
        }

        private bool IsValidForToday(in SpecialEventData sed, int todaySimDay)
        {
            if (sed.duration < MinValidDuration)
                return false;

            float s = math.frac(sed.start_time);
            float e = math.frac(sed.start_time + sed.duration);
            if (!(s >= 0f && s < 1f))
                return false;
            if (math.abs(e - s) < 1e-5f)
                return false;

            return sed.day == todaySimDay;
        }
    }
}
