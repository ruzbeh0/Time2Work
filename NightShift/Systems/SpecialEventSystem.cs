using Colossal.Entities;
using Game;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Time2Work.Components;
using Time2Work.Utils;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.PlayerLoop;
using static Time2Work.Setting;

namespace Time2Work.Systems
{
    public partial class SpecialEventSystem : GameSystemBase
    {
        private EntityQuery _query;
        private Setting.DTSimulationEnum m_daytype;
        private uint m_SimulationFrame;
        private EntityQuery m_TimeDataQuery;
        private SimulationSystem m_SimulationSystem;
        private bool updated = false;
        public static int numberEvents = 0;
        public static NativeArray<float> startTime;
        public static NativeArray<float> endTime;
        private int _timesCount; // tracks current allocated length
        private int nEvents_NewYears = Mod.m_Setting.new_years_num_events;

        protected override void OnCreate()
        {
            base.OnCreate();
            //Mod.log.Info($"SpecialEventSystem OnCreate");

            _query = GetEntityQuery(new EntityQueryDesc()
            {
                All = new[]
                {
                    ComponentType.ReadOnly<PrefabRef>(),
                    ComponentType.ReadOnly<AttractivenessProvider>()
                },
                None = new[] { ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>() }
            });

            RequireForUpdate(_query);

            m_SimulationSystem = this.World.GetOrCreateSystemManaged<SimulationSystem>();
            m_TimeDataQuery = this.GetEntityQuery(ComponentType.ReadOnly<Game.Common.TimeData>());
            _timesCount = 0; // not allocated yet
        }

        protected override void OnUpdate()
        {
            var entities = _query.ToEntityArray(Allocator.Temp);

            try
            {
                if (entities.Length == 0)
                    return; // NEW: nothing to process this tick

                Game.Common.TimeData m_TimeData = this.m_TimeDataQuery.GetSingleton<Game.Common.TimeData>();
                m_SimulationFrame = this.m_SimulationSystem.frameIndex;

                int day = Time2WorkTimeSystem.GetDay(this.m_SimulationFrame, m_TimeData);
                DateTime currentDateTime = World.GetExistingSystemManaged<Time2WorkTimeSystem>().GetCurrentDateTime();
                int hour = currentDateTime.Hour;
                int minute = currentDateTime.Minute;

                bool isNewYearsEve = (currentDateTime.Day == (Mod.m_Setting.daysPerMonth*12));

                //Mod.log.Info($"isNewYearsEve:{isNewYearsEve}, currentDateTime.Month:{currentDateTime.Month},currentDateTime.Day:{currentDateTime.Day},Mod.m_Setting.daysPerMonth:{Mod.m_Setting.daysPerMonth*12},{currentDateTime.Month == 12},{currentDateTime.Day == (Mod.m_Setting.daysPerMonth * 12)}");

                System.DayOfWeek dayOfWeek = (System.DayOfWeek)WeekSystem.getDayOfWeekInt();
                Unity.Mathematics.Random random = Unity.Mathematics.Random.CreateFromIndex((uint)day * 100);
                int n = 0;

                int min = Mod.m_Setting.min_event_weekday;
                int max = Mod.m_Setting.max_event_weekday;

                if (dayOfWeek.Equals(DayOfWeek.Saturday) || dayOfWeek.Equals(DayOfWeek.Sunday))
                {
                    min = Mod.m_Setting.min_event_weekend;
                    max = Mod.m_Setting.max_event_weekend;
                }
                else if (dayOfWeek.Equals(DayOfWeek.Friday))
                {
                    min = Mod.m_Setting.min_event_avg_day;
                    max = Mod.m_Setting.max_event_avg_day;
                }

                numberEvents = random.NextInt(min, max + 1);

                //If it is New Years Eve double number of events of a weekend
                if (isNewYearsEve)
                {
                    numberEvents = nEvents_NewYears;
                }

                EnsureTimeBuffers(numberEvents);

                // Count how many placed entities reference each leisure prefab
                var prefabUseCount = new Dictionary<Entity, int>(entities.Length);
                for (int k = 0; k < entities.Length; k++)
                {
                    var pr = EntityManager.GetComponentData<PrefabRef>(entities[k]).m_Prefab;
                    if (!prefabUseCount.TryGetValue(pr, out int cnt)) prefabUseCount[pr] = 1;
                    else prefabUseCount[pr] = cnt + 1;
                }

                //Mod.log.Info($"Day:{day}, DayOfWeek:{dayOfWeek}, Hour:{hour}, Minute:{minute}, Number of Events Today: {numberEvents}, !updated:{!updated}");
                if ((int)dayOfWeek > -1 && (hour == 3 && minute >= 4 && minute < 10 || !updated))
                {
                    //updated = true;
                    int i = 0;

                    int validCnt = 0;
                    for (int j = 0; j < entities.Length; j++)
                    {
                        PrefabRef prefabRef = EntityManager.GetComponentData<PrefabRef>(entities[j]);

                        if (!EntityManager.HasComponent<SpecialEventData>(entities[j]))
                            continue;

                        var prefab = prefabRef.m_Prefab;
                        if (!prefabUseCount.TryGetValue(prefab, out int cnt) || cnt != 1)
                            continue; // skip non-unique prefabs

                        validCnt++;
                    }

                    if(validCnt == 0) {
                        // No valid special event locations, nothing to do
                        return;
                    }

                    // We'll keep track of unique locations for today to avoid duplicates
                    var prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
                    var seenLocations = new HashSet<string>(StringComparer.Ordinal);

                    foreach (var ent in entities)
                    {
                        PrefabRef prefabRef = EntityManager.GetComponentData<PrefabRef>(ent);
                        SpecialEventData specialEventData;
                        var prefab = prefabRef.m_Prefab;

                        // Skip non-unique prefabs
                        if (!prefabUseCount.TryGetValue(prefab, out int cnt) || cnt != 1)
                            continue;

                        if (EntityManager.TryGetComponent(ent, out specialEventData))
                        {
                            uint seed = (uint)(specialEventData.new_attraction / 100 + day * day);
                            Unity.Mathematics.Random random2 = Unity.Mathematics.Random.CreateFromIndex(seed);
                            int r = random2.NextInt(0, validCnt);
                            
                            //Mod.log.Info($"Entity {ent.Index} with attraction {specialEventData.new_attraction} gets random r={r} (validCnt/2={validCnt/2}) with seed {seed}");
                            if (specialEventData.day != day && (n < numberEvents && (r < numberEvents || i == entities.Length - 1)))
                            {
                                // Human-readable location
                                string location = prefabSystem.GetPrefabName(prefabRef.m_Prefab);

                                location = SpecialEventsUISystem.SanitizeString(location);
                                if (string.IsNullOrWhiteSpace(location) || location == "Unknown")
                                    continue; // don't surface blank locations

                                // Avoid duplicates by location
                                if (!seenLocations.Add(location))
                                    continue;

                                if(isNewYearsEve)
                                {
                                    //In New Years Eve events end at midnight
                                    specialEventData.duration = random.NextInt(2, 5) / 24f;
                                    specialEventData.start_time = 1f - specialEventData.duration;

                                } else
                                {
                                    if (dayOfWeek.Equals(DayOfWeek.Saturday) || dayOfWeek.Equals(DayOfWeek.Sunday))
                                    {
                                        specialEventData.start_time = random.NextInt(8, 20) / 24f;
                                        specialEventData.duration = random.NextInt(2, 5) / 24f;
                                    }
                                    else
                                    {
                                        float time = (float)GaussianRandom.NextGaussianDouble(random);
                                        if (time > 0)
                                        {
                                            time *= 4;
                                        }
                                        else
                                        {
                                            time *= 8;
                                        }
                                        int timeint = (int)time;
                                        specialEventData.start_time = Math.Max(8, Math.Min((timeint + 16f), 20)) / 24f;
                                        specialEventData.duration = random.NextInt(2, 4) / 24f;
                                    }
                                }
                                
                                specialEventData.day = day;
                                specialEventData.entity_index = ent.Index;
                                startTime[n] = specialEventData.start_time;
                                endTime[n] = specialEventData.start_time + specialEventData.duration;
                                n++;
                                //Mod.log.Info($"location:{location}, updated:{updated}, numberEvents:{numberEvents}, attr:{specialEventData.new_attraction}, n:{n}, day:{specialEventData.day}, enti:{specialEventData.entity_index}");
                            }
                            else
                            {
                                //specialEventData.day = -1;
                            }
                            if(specialEventData.version < 2)
                            {
                                specialEventData.version = 2; 
                                EntityManager.RemoveComponent<SpecialEventData>(prefabRef.m_Prefab);
                                EntityManager.AddComponentData(ent, specialEventData);
                            } else
                            {
                                EntityManager.SetComponentData(ent, specialEventData);
                            }   
                        }
                        i++;
                    }
                    if (n >= 1)
                    {
                        updated = true;
                        numberEvents = n;
                    }
                }
            }
            finally
            {

            }
            
        }

        protected override void OnDestroy()
        {
            if (startTime.IsCreated) startTime.Dispose();
            if (endTime.IsCreated) endTime.Dispose();
            base.OnDestroy();
        }

        private void EnsureTimeBuffers(int count)
        {
            if (count < 0) count = 0;

            // (Re)allocate only if size changed or not created yet
            if (!startTime.IsCreated || _timesCount != count)
            {
                if (startTime.IsCreated) startTime.Dispose();
                if (endTime.IsCreated) endTime.Dispose();

                startTime = count > 0
                    ? new NativeArray<float>(count, Allocator.Persistent, NativeArrayOptions.ClearMemory)
                    : default;

                endTime = count > 0
                    ? new NativeArray<float>(count, Allocator.Persistent, NativeArrayOptions.ClearMemory)
                    : default;

                _timesCount = count;
            }
        }


        public override int GetUpdateInterval(SystemUpdatePhase phase) => 16;
    }
}
