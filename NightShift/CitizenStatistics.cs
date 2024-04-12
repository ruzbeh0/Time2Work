using Game.Citizens;
using Game.Companies;
using Game.Simulation;
using Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Game.Assets;
using System.IO;
using Game.Prefabs;
using Colossal.Entities;
using Colossal.IO.AssetDatabase.Internal;
using static System.Net.Mime.MediaTypeNames;

namespace Time2Work
{
    public partial class CitizenStatistics : GameSystemBase
    {
        private Dictionary<Entity, TravelPurpose> _CitizenToData = new Dictionary<Entity, TravelPurpose>();
        private Dictionary<int, Purpose[]> _outputData = new Dictionary<int, Purpose[]>();

        private EntityQuery _query;

        protected override void OnCreate()
        {
            base.OnCreate();

            _query = GetEntityQuery(new EntityQueryDesc()
            {
                All = new[] {
                    ComponentType.ReadWrite<TravelPurpose>()
                }
            });

            RequireForUpdate(_query);
        }

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            // One day (or month) in-game is '262144' ticks
            return TimeSystem.kTicksPerDay / 32;
        }
        protected override void OnUpdate()
        {
            DateTime currentDateTime = this.World.GetExistingSystemManaged<TimeSystem>().GetCurrentDateTime();

            var citizens = _query.ToEntityArray(Allocator.Temp);

            foreach (var cim in citizens)
            {
                Citizen data1;
                TravelPurpose data2;

                data2 = EntityManager.GetComponentData<TravelPurpose>(cim);
                if (EntityManager.TryGetComponent<Citizen>(cim, out data1))
                {
                    Purpose[] purp;
                    if (!_outputData.ContainsKey(cim.Index))
                    {
                        // 0 - 23 YESTERDAY, 24 to 47 TODAY
                        purp = new Purpose[48];
                        purp[currentDateTime.Hour + 24] = data2.m_Purpose;
                        _outputData.Add(cim.Index, purp);
                    } else
                    {
                        if(_outputData.TryGetValue(cim.Index, out purp)) 
                        {
                            purp[currentDateTime.Hour + 24] = data2.m_Purpose;
                            _outputData[cim.Index] = purp;
                        }
                    }                  
                }
            }

            if (currentDateTime.Hour == 0) 
            {
                int[] hbw = new int[24];
                int[] hbsch = new int[24];
                int[] hbo = new int[24];
                int[] nhb = new int[24];

                for(int h = 0; h < 24; h++)
                {
                    hbw[h] = 0;
                    hbsch[h] = 0;
                    hbo[h] = 0;
                    nhb[h] = 0;
                }

                foreach (var key in _outputData.Keys)
                {
                    Purpose[] purp = _outputData[key];
                    int i = 0;
                    List<Purpose> tpurp = new List<Purpose>();
                    List<int> thour = new List<int>();
                    while (i < 48)
                    {
                        Purpose current = purp[i];
                        if (i < 47)
                        {
                            int j = i + 1;
                            Purpose next = purp[j];

                            while (current.Equals(next) && j < 47)
                            {
                                j++;
                                next = purp[j];
                            }

                            if (!current.Equals(next))
                            {
                                tpurp.Add(current);
                                thour.Add(i);
                            }

                            i = j;
                        }
                        else
                        {
                            if (!current.Equals(purp[i - 1]))
                            {
                                tpurp.Add(current);
                                thour.Add(i);
                            }
                            i++;
                        }
                    }
                    for (int k = 0; k < tpurp.Count; k++)
                    {
                        Purpose current = tpurp[k];
                        Purpose previous;
                        Purpose next;

                        int previous_hour;

                        if (k == 0)
                        {
                            previous = tpurp[tpurp.Count - 1];
                            previous_hour = thour[thour.Count - 1];
                        }
                        else
                        {
                            previous = tpurp[k - 1];
                            previous_hour = thour[k - 1];
                        }

                        if (k == (tpurp.Count - 1))
                        {
                            next = tpurp[0];
                        }
                        else
                        {
                            next = tpurp[k + 1];
                        }

                        int h = thour[k];
                        if (h > 23 && (h - previous_hour) < 12)
                        {
                            // Going to Work
                            if (current.Equals(Purpose.GoingToWork))
                            {
                                if (previous.Equals(Purpose.Sleeping) || previous.Equals(Purpose.None) || previous.Equals(Purpose.GoingHome))
                                {
                                    hbw[h - 24]++;
                                }
                                else
                                {
                                    nhb[h - 24]++;
                                }
                            }
                            // For cases where status is "Working", but there is no "GoingToWork" status before between now and previous location
                            if (current.Equals(Purpose.Working))
                            {
                                if (previous.Equals(Purpose.Sleeping) || previous.Equals(Purpose.None))
                                {
                                    hbw[h - 24]++;
                                }
                                else
                                {
                                    if (!previous.Equals(Purpose.GoingToWork))
                                    {
                                        nhb[h - 24]++;
                                    }

                                }
                            }
                            if (current.Equals(Purpose.GoingToSchool))
                            {
                                if (previous.Equals(Purpose.Sleeping) || previous.Equals(Purpose.None))
                                {
                                    hbsch[h - 24]++;
                                }
                                else
                                {
                                    nhb[h - 24]++;
                                }
                            }
                            // For cases where status is "Studying", but there is no "GoingToSchool" status before between now and previous location
                            if (current.Equals(Purpose.Studying))
                            {
                                if (previous.Equals(Purpose.Sleeping) || previous.Equals(Purpose.None))
                                {
                                    hbsch[h - 24]++;
                                }
                                else
                                {
                                    if (!previous.Equals(Purpose.GoingToSchool))
                                    {
                                        nhb[h - 24]++;
                                    }

                                }
                            }
                            //Going Home
                            if (current.Equals(Purpose.GoingHome))
                            {
                                if (previous.Equals(Purpose.Working))
                                {
                                    if (next.Equals(Purpose.Sleeping) || next.Equals(Purpose.None))
                                    {
                                        hbw[h - 24]++;
                                    }
                                    else
                                    {
                                        nhb[h - 24]++;
                                    }
                                }
                                else
                                {
                                    if (previous.Equals(Purpose.Studying))
                                    {
                                        if (next.Equals(Purpose.Sleeping) || next.Equals(Purpose.None))
                                        {
                                            hbsch[h - 24]++;
                                        }
                                        else
                                        {
                                            nhb[h - 24]++;
                                        }
                                    }
                                    else
                                    {
                                        if (next.Equals(Purpose.Sleeping) || next.Equals(Purpose.None))
                                        {
                                            hbo[h - 24]++;
                                        }
                                        else
                                        {
                                            nhb[h - 24]++;
                                        }
                                    }
                                }
                            }
                            if (previous.Equals(Purpose.Sleeping) || previous.Equals(Purpose.None))
                            {
                                if(!(current.Equals(Purpose.Sleeping) || current.Equals(Purpose.None)))
                                {
                                    if (!(current.Equals(Purpose.GoingToSchool) || current.Equals(Purpose.GoingToWork) || current.Equals(Purpose.Studying) || current.Equals(Purpose.Working)))
                                    {
                                        hbo[h - 24]++;
                                    }
                                    else
                                    {
                                        nhb[h - 24]++;
                                    }
                                }                               
                            }
                        }

                    }
                    thour = null;
                    tpurp = null;                    
                }
                Dictionary<int, Purpose[]> _outputDataTemp = new Dictionary<int, Purpose[]>();
                int[] keys = _outputData.Keys.ToArray<int>();
                foreach (var key in keys)
                {
                    Purpose[] purp;
                    if (_outputData.TryGetValue(key, out purp))
                    {
                        for (int h = 0; h < 24; h++)
                        {
                            purp[h] = purp[h+24];
                            purp[h+24] = 0;
                            _outputData[key] = purp;
                        }
                    }
                }
  
                if (File.Exists(Mod.output))
                {
                    File.Delete(Mod.output);
                }

                using (StreamWriter sw = File.AppendText(Mod.output))
                {
                    sw.WriteLine($"hour,trip_purpose,trips");
                    for (int h = 0; h < 24; h++)
                    {
                        sw.WriteLine($"{h},hbw,{hbw[h]}");
                        sw.WriteLine($"{h},hbsch,{hbsch[h]}");
                        sw.WriteLine($"{h},hbo,{hbo[h]}");
                        sw.WriteLine($"{h},nhb,{nhb[h]}");
                    }
                }
            }
        }
    }
}
