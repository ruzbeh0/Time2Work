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

        protected override void OnCreate()
        {
            base.OnCreate();

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
            Unity.Mathematics.Random random = new Unity.Mathematics.Random((uint)day);
            int n = 0;

            int numberEvents = random.NextInt(Mod.m_Setting.min_event_weekday, Mod.m_Setting.max_event_weekday);

            if (dayOfWeek.Equals(DayOfWeek.Saturday) || dayOfWeek.Equals(DayOfWeek.Sunday))
            {
                numberEvents = random.NextInt(Mod.m_Setting.min_event_weekend, Mod.m_Setting.max_event_weekend);
            }
            else if (dayOfWeek.Equals(DayOfWeek.Friday))
            {
                numberEvents = random.NextInt(Mod.m_Setting.min_event_avg_day, Mod.m_Setting.max_event_avg_day);
            }

            if ((int)dayOfWeek > -1 && (hour == 0 && minute < 4 || !updated))
            {
                updated = true;

                int i = 0;
                foreach (var ent in entities)
                {
                    SpecialEventData specialEventData;
                    if(EntityManager.TryGetComponent(ent, out specialEventData))
                    {
                        Unity.Mathematics.Random random2 = new Unity.Mathematics.Random((uint)(specialEventData.new_attraction+day));
                        int r = random2.NextInt(0,entities.Length);

                        //Mod.log.Info($"r:{r}, enti:{entities.Length}, numberEvents:{numberEvents}, attr:{specialEventData.new_attraction}, n:{n}");
                        if (n < numberEvents && (r < numberEvents || i == entities.Length - 1))
                        {
                            if (dayOfWeek.Equals(DayOfWeek.Saturday) || dayOfWeek.Equals(DayOfWeek.Sunday))
                            {
                                specialEventData.start_time = random.NextInt(8, 20) / 24f;
                                specialEventData.duration = random.NextInt(2, 4) / 24f;
                            }
                            else
                            {
                                float time = (float)GaussianRandom.NextGaussianDouble(random);
                                Mod.log.Info($"time:{time}");
                                if(time > 0)
                                {
                                    time *= 4;
                                } else
                                {
                                    time *= 8;
                                }
                                int timeint = (int) time;
                                Mod.log.Info($"timeint:{timeint}");
                                specialEventData.start_time = Math.Max(8,Math.Min((timeint + 16f),20)) / 24f;
                                specialEventData.duration = random.NextInt(2, 3) / 24f;
                            }
                            specialEventData.day = day;
                            n++;
                        } else
                        {
                            specialEventData.day = -1;
                        }
                        EntityManager.SetComponentData(ent, specialEventData);
                    }
                    i++;
                }
                updated = true;
            }
        }

        public override int GetUpdateInterval(SystemUpdatePhase phase) => 16;
    }
}
