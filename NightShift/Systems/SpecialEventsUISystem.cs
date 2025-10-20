using Colossal.Entities;
using Colossal.UI;
using Colossal.UI.Binding;
using Game;
using Game.Buildings;
using Game.Prefabs;
using Game.Rendering;
using Game.Simulation;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Time2Work.Components;
using Time2Work.Extensions;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using static Time2Work.Setting;

namespace Time2Work.Systems
{
    public partial class SpecialEventsUISystem : ExtendedUISystemBase
    {
        private Dictionary<Entity, AttractivenessProvider> _attractivenessProviderDictionary = new Dictionary<Entity, AttractivenessProvider>();
        private EntityQuery _query;
        private Setting.DTSimulationEnum m_daytype;
        private uint m_SimulationFrame;
        private EntityQuery m_TimeDataQuery;
        private SimulationSystem m_SimulationSystem;
        private CameraUpdateSystem _cameraUpdateSystem;
        public static readonly ConcurrentDictionary<Entity, string> LatestEventLocations
            = new ConcurrentDictionary<Entity, string>();

        // Optional accessor if you prefer a method
        public static bool TryGetLocation(Entity e, out string location)
            => LatestEventLocations.TryGetValue(e, out location);
        private struct SpecialEventInfo
        {
            public Entity entity;
            public int start_hour;
            public int start_minutes;
            public int end_hour;
            public int end_minutes;
            public string event_location;
        }

        public static string SanitizeString(string input)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(input))
                    return "Unknown";

                return new string(input
                    .Where(c => !char.IsControl(c) || c == '\n' || c == '\r')
                    .ToArray());
            }
            catch (Exception ex)
            {
                //Mod.log.Info($"SanitizeString failed with input: {input}: {ex.Message}");
                return "Unknown";
            }
        }



        private static void WriteSpecialEventInfo(IJsonWriter writer, SpecialEventInfo info)
        {
            string safeLocation = SanitizeString(info.event_location);

            writer.TypeBegin("SpecialEventInfo");
            writer.PropertyName("entity"); writer.Write(info.entity);
            writer.PropertyName("start_hour"); writer.Write(info.start_hour);
            writer.PropertyName("start_minutes"); writer.Write(info.start_minutes);
            writer.PropertyName("end_hour"); writer.Write(info.end_hour);
            writer.PropertyName("end_minutes"); writer.Write(info.end_minutes);
            writer.PropertyName("event_location"); writer.Write(safeLocation);
            writer.TypeEnd();
        }


        private const string kGroup = "specialEventInfo";
        protected const string group = "specialEvent";

        private RawValueBinding m_uiResults;
        private RawValueBinding m_uiTransfersResults;

        private NativeArray<SpecialEventInfo> m_Results; // final results, will be filled via jobs and then written as output
        private List<SpecialEventInfo> m_ValidResults = new();


        // 240209 Set gameMode to avoid errors in the Editor
        public override GameMode gameMode => GameMode.Game;

        protected override void OnCreate()
        {
            base.OnCreate();

            _query = GetEntityQuery(new EntityQueryDesc()
            {
                All = new[] {
                    ComponentType.ReadOnly<AttractivenessProvider>(),
                    ComponentType.ReadOnly<PrefabRef>(),
                    ComponentType.ReadOnly<SpecialEventData>()
                }
            });

            RequireForUpdate(_query);

            m_SimulationSystem = this.World.GetOrCreateSystemManaged<SimulationSystem>();
            m_TimeDataQuery = this.GetEntityQuery(ComponentType.ReadOnly<Game.Common.TimeData>());
            _cameraUpdateSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<CameraUpdateSystem>();


            AddBinding(m_uiResults = new RawValueBinding(kGroup, "specialEventDetails", binder => {
                binder.ArrayBegin(m_ValidResults?.Count ?? 0);
                if (m_ValidResults != null)
                {
                    for (int i = 0; i < m_ValidResults.Count; i++)
                        WriteSpecialEventInfo(binder, m_ValidResults[i]);
                }
                binder.ArrayEnd();
            }));

            AddBinding(new TriggerBinding<Entity>(group, "NavigateTo", NavigateTo));

            if (m_Results.IsCreated)
                m_Results.Dispose();

            int count = SpecialEventSystem.numberEvents;
            m_Results = new NativeArray<SpecialEventInfo>(count, Allocator.Persistent);

        }

        protected override void OnUpdate()
        {
            // Gather today + reset per-frame buffers
            var entities = _query.ToEntityArray(Allocator.Temp);
            Game.Common.TimeData timeData = m_TimeDataQuery.GetSingleton<Game.Common.TimeData>();
            m_SimulationFrame = m_SimulationSystem.frameIndex;

            // 1) Compute the “UI day” with a 3:00 threshold
            int todaySimDay = Time2WorkTimeSystem.GetDay(m_SimulationFrame, timeData);
            DateTime now = World.GetExistingSystemManaged<Time2WorkTimeSystem>().GetCurrentDateTime();
            bool usePrevDayWindow = now.Hour < 3;             // 00:00–02:59 still show “yesterday”
            if (usePrevDayWindow)
            {
                todaySimDay -= 1;                              // shift the UI’s effective day
            }

            // 2) Size the output buffer generously during the prev-day window
            int nEvents = SpecialEventSystem.numberEvents;
            if (usePrevDayWindow)
            {
                // numberEvents may already be for the new day; use a safe upper bound
                nEvents = math.max(nEvents, entities.Length);
            }

            m_ValidResults.Clear();

            // Resize output buffer if SpecialEventSystem changed count
            if (!m_Results.IsCreated || m_Results.Length != nEvents)
            {
                if (m_Results.IsCreated) m_Results.Dispose();
                m_Results = new NativeArray<SpecialEventInfo>(nEvents, Allocator.Persistent);
                Mod.log.Info($"Reallocated m_Results with new count: {nEvents}");
            }

            // We'll keep track of unique locations for today to avoid duplicates
            var prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            var seenLocations = new HashSet<string>(StringComparer.Ordinal);
            int n = 0;

            // Clear & repopulate the shared cache for other systems (e.g., chirps)
            LatestEventLocations.Clear();

            foreach (var ent in entities)
            {
                // Lazily cache AttractivenessProvider lookups (kept from your original code)
                if (!_attractivenessProviderDictionary.TryGetValue(ent, out var attractivenessProvider))
                {
                    if (EntityManager.HasComponent<AttractivenessProvider>(ent))
                    {
                        attractivenessProvider = EntityManager.GetComponentData<AttractivenessProvider>(ent);
                        _attractivenessProviderDictionary[ent] = attractivenessProvider;
                    }
                }

                // Need a prefab to resolve SpecialEventData & a readable name
                if (!EntityManager.TryGetComponent<PrefabRef>(ent, out var prefabRef))
                    continue;

                // Event data lives on the prefab
                if (!EntityManager.TryGetComponent<SpecialEventData>(ent, out var sed))
                    continue;

                // Only show today's events
                if (sed.day != todaySimDay)
                    continue;

                // Skip uninitialized / invalid events (duration < 5 in-game minutes)
                const float fiveMinutes = 5f / (24f * 60f);
                if (sed.duration < fiveMinutes)
                    continue;

                // Human-readable location
                string location = prefabSystem.GetPrefabName(prefabRef.m_Prefab);
                location = SanitizeString(location);
                if (string.IsNullOrWhiteSpace(location) || location == "Unknown")
                    continue; // don't surface blank locations

                if (ent.Index != sed.entity_index)
                {
                    continue;
                }

                // Compute start/end HH:MM (normalized 0..1 → 24h)
                float start24 = math.frac(sed.start_time) * 24f;
                int startH = (int)math.floor(start24);
                int startM = (int)math.round((start24 - startH) * 60f);
                if (startM == 60) { startM = 0; startH = (startH + 1) % 24; }

                float endNorm = math.frac(sed.start_time + sed.duration);
                float end24 = endNorm * 24f;
                int endH = (int)math.floor(end24);
                int endM = (int)math.round((end24 - endH) * 60f);
                if (endM == 60) { endM = 0; endH = (endH + 1) % 24; }

                // Write UI row
                if (n < m_Results.Length)
                {
                    var info = new SpecialEventInfo
                    {
                        entity = ent,
                        start_hour = startH,
                        start_minutes = startM,
                        end_hour = endH,
                        end_minutes = endM,
                        event_location = location
                    };

                    m_Results[n] = info;
                    m_ValidResults.Add(info);
                    n++;

                    // Update shared cache for other systems (e.g., chirps)
                    LatestEventLocations[ent] = location;
                }

                // Stop if we've filled the announced count
                if (n >= nEvents)
                    break;
            }

            Mod.numCurrentEvents = n;
            // after you've finished building m_ValidResults (right before m_uiResults.Update())
            m_ValidResults.Sort((a, b) =>
            {
                int c = a.start_hour.CompareTo(b.start_hour);
                if (c != 0) return c;

                c = a.start_minutes.CompareTo(b.start_minutes);
                if (c != 0) return c;

                // deterministic string compare
                c = string.Compare(a.event_location, b.event_location, StringComparison.Ordinal);
                if (c != 0) return c;

                // final stable tiebreaker
                return a.entity.Index.CompareTo(b.entity.Index);
            });
            m_uiResults.Update();

        }


        public void NavigateTo(Entity entity)
        {
            if (_cameraUpdateSystem.orbitCameraController != null && entity != Entity.Null)
            {
                _cameraUpdateSystem.orbitCameraController.followedEntity = entity;
                _cameraUpdateSystem.orbitCameraController.TryMatchPosition(_cameraUpdateSystem.activeCameraController);
                _cameraUpdateSystem.activeCameraController = _cameraUpdateSystem.orbitCameraController;
            }
        }

        protected override void OnDestroy()
        {
            _cameraUpdateSystem = null;
            if (m_Results.IsCreated)
                m_Results.Dispose();
            base.OnDestroy();
        }

        public override int GetUpdateInterval(SystemUpdatePhase phase) => 16;
    }
}