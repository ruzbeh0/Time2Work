using Game.Buildings;
using Game.Prefabs;
using Game.Simulation;
using Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Time2Work.Components;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Colossal.Entities;
using static Time2Work.Setting;
using Unity.Mathematics;
using Game.Citizens;
using System.Net;
using Time2Work.Utils;
using Unity.Entities.UniversalDelegates;

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
        public static float3 startTime;
        public static float3 endTime;

        protected override void OnCreate()
        {
            base.OnCreate();
            //Mod.log.Info($"SpecialEventSystem OnCreate");

            _query = GetEntityQuery(new EntityQueryDesc()
            {
                All = new[] {
                    ComponentType.ReadWrite<SpecialEventData>(),
                }
            });

            RequireForUpdate(_query);

            m_SimulationSystem = this.World.GetOrCreateSystemManaged<SimulationSystem>();
            m_TimeDataQuery = this.GetEntityQuery(ComponentType.ReadOnly<Game.Common.TimeData>());

        }

        protected override void OnUpdate()
        {
            var entities = _query.ToEntityArray(Allocator.Temp);

            Game.Common.TimeData m_TimeData = this.m_TimeDataQuery.GetSingleton<Game.Common.TimeData>();
            m_SimulationFrame = this.m_SimulationSystem.frameIndex;
            int day = Time2WorkTimeSystem.GetDay(this.m_SimulationFrame, m_TimeData);
            DateTime currentDateTime = World.GetExistingSystemManaged<Time2WorkTimeSystem>().GetCurrentDateTime();
            int hour = currentDateTime.Hour;
            int minute = currentDateTime.Minute;
            System.DayOfWeek dayOfWeek = (System.DayOfWeek)WeekSystem.getDayOfWeekInt();
            Unity.Mathematics.Random random = Unity.Mathematics.Random.CreateFromIndex((uint)day*100);
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

            //Mod.log.Info($"dayOfWeek:{dayOfWeek}, day: {day}, hour:{hour}, minute:{minute}, numberEvents:{numberEvents}, entities:{entities.Length}, min: {min}, max: {max}");

            if ((int)dayOfWeek > -1 && (hour == 0 && minute >= 4 && minute < 10 || !updated))
            {
                updated = true;
                //Mod.log.Info($"Number of Events Today: {numberEvents}");
                int i = 0;
                foreach (var ent in entities)
                {
                    SpecialEventData specialEventData;
                    if(EntityManager.TryGetComponent(ent, out specialEventData))
                    {
                        uint seed = (uint)(specialEventData.new_attraction / 100 + day * day);
                        Unity.Mathematics.Random random2 = Unity.Mathematics.Random.CreateFromIndex(seed);
                        int r = random2.NextInt(0,entities.Length);

                        
                        if (specialEventData.day != day && (n < numberEvents && (r < numberEvents || i == entities.Length - 1)))
                        {
                            if (dayOfWeek.Equals(DayOfWeek.Saturday) || dayOfWeek.Equals(DayOfWeek.Sunday))
                            {
                                specialEventData.start_time = random.NextInt(8, 20) / 24f;
                                specialEventData.duration = random.NextInt(2, 5) / 24f;
                            }
                            else
                            {
                                float time = (float)GaussianRandom.NextGaussianDouble(random);
                                if(time > 0)
                                {
                                    time *= 4;
                                } else
                                {
                                    time *= 8;
                                }
                                int timeint = (int) time;
                                specialEventData.start_time = Math.Max(8,Math.Min((timeint + 16f),20)) / 24f;
                                specialEventData.duration = random.NextInt(2, 4) / 24f;
                            }
                            specialEventData.day = day;
                            startTime[n] = specialEventData.start_time;
                            endTime[n] = specialEventData.start_time + specialEventData.duration;
                            n++;
                            //Mod.log.Info($"startTime:{startTime[n]}, endTime:{endTime[n]}, enti:{entities.Length}, numberEvents:{numberEvents}, attr:{specialEventData.new_attraction}, n:{n}, day:{specialEventData.day}");
                        } else
                        {
                            specialEventData.day = -1;
                        }
                        EntityManager.SetComponentData(ent, specialEventData);
                    }
                    i++;
                }
                if(n == numberEvents)
                {
                    updated = true;
                }
            }
        }

        public override int GetUpdateInterval(SystemUpdatePhase phase) => 16;
    }
}
