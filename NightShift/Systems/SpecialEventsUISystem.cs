using Colossal.Entities;
using Game;
using Game.Buildings;
using Game.Prefabs;
using Game.Simulation;
using System;
using System.Collections.Generic;
using Time2Work.Components;
using Unity.Collections;
using Unity.Entities;
using Time2Work.Extensions;
using Colossal.UI.Binding;

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
        private struct SpecialEventInfo
        {
            public int start_hour;
            public int start_minutes;
            public int end_hour;
            public int end_minutes;
            public string event_location;

            public SpecialEventInfo() {}
        }

        private static void WriteSpecialEventInfo(IJsonWriter writer, SpecialEventInfo info)
        {
            writer.TypeBegin("SpecialEventInfo");
            writer.PropertyName("start_hour");
            writer.Write(info.start_hour);
            writer.PropertyName("start_minutes");
            writer.Write(info.start_minutes);
            writer.PropertyName("end_hour");
            writer.Write(info.end_hour);
            writer.PropertyName("end_minutes");
            writer.Write(info.end_minutes);
            writer.PropertyName("event_location");
            writer.Write(info.event_location);
            writer.TypeEnd();
        }

        private const string kGroup = "specialEventInfo";
        protected const string group = "specialEvent";

        private RawValueBinding m_uiResults;
        private RawValueBinding m_uiTransfersResults;

        private NativeArray<SpecialEventInfo> m_Results; // final results, will be filled via jobs and then written as output

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
                }
            });

            RequireForUpdate(_query);

            m_SimulationSystem = this.World.GetOrCreateSystemManaged<SimulationSystem>();
            m_TimeDataQuery = this.GetEntityQuery(ComponentType.ReadOnly<Game.Common.TimeData>());

            AddBinding(m_uiResults = new RawValueBinding(kGroup, "specialEventDetails", delegate (IJsonWriter binder)
            {
                binder.ArrayBegin(m_Results.Length);
                for (int i = 0; i < m_Results.Length; i++)
                {
                    WriteSpecialEventInfo(binder, m_Results[i]);
                }
                binder.ArrayEnd();
            }));

            m_Results = new NativeArray<SpecialEventInfo>(4, Allocator.Persistent);

        }

        protected override void OnUpdate()
        {
            var entities = _query.ToEntityArray(Allocator.Temp);

            int nEvents = SpecialEventSystem.numberEvents;
            int n = 0;

            Game.Common.TimeData m_TimeData = this.m_TimeDataQuery.GetSingleton<Game.Common.TimeData>();
            m_SimulationFrame = this.m_SimulationSystem.frameIndex;
            int day = Time2WorkTimeSystem.GetDay(this.m_SimulationFrame, m_TimeData);

            foreach (var ent in entities)
            {
                AttractivenessProvider attractivenessProvider;

                if (!_attractivenessProviderDictionary.TryGetValue(ent, out attractivenessProvider))
                {
                    attractivenessProvider = EntityManager.GetComponentData<AttractivenessProvider>(ent);
                    _attractivenessProviderDictionary.Add(ent, attractivenessProvider);
                }

                PrefabRef prefabRef;
                if (EntityManager.TryGetComponent<PrefabRef>(ent, out prefabRef))
                {
                    SpecialEventData specialEventdata;

                    if (EntityManager.TryGetComponent<SpecialEventData>(prefabRef.m_Prefab, out specialEventdata))
                    {
                        if(specialEventdata.day == day && n < nEvents)
                        {
                            //Mod.log.Info($"Special Event at: {this.World.GetOrCreateSystemManaged<PrefabSystem>().GetPrefabName(prefabRef.m_Prefab)} - Start Time:{specialEventdata.start_time}, Duration:{specialEventdata.duration}, Attraction: {attractivenessProvider.m_Attractiveness}");

                            SpecialEventInfo info = new SpecialEventInfo();
                            info.event_location = this.World.GetOrCreateSystemManaged<PrefabSystem>().GetPrefabName(prefabRef.m_Prefab);
                            info.start_hour = (int)Math.Round(24f * specialEventdata.start_time);
                            info.end_hour = (int)Math.Round(24f * (specialEventdata.start_time + specialEventdata.duration));
                            info.start_minutes = (int)(6 * (info.start_hour - (24f * (specialEventdata.start_time))));
                            info.end_minutes = (int)(6 * (info.end_hour - (24f * (specialEventdata.start_time + specialEventdata.duration))));
                            m_Results[n] = info;
                            n++;
                        }    
                    }
                }
            }

            Mod.numCurrentEvents = n;
            m_uiResults.Update();
        }

        public override int GetUpdateInterval(SystemUpdatePhase phase) => 16;
    }
}