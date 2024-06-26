﻿using Colossal.Logging;
using Game;
using Game.Citizens;
using Game.Companies;
using Game.Prefabs;
using Game.Simulation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Time2Work.Systems
{
    public partial class WorkerShiftUpdateSystem : GameSystemBase
    {
        private Dictionary<Entity, Worker> _WorkerToData = new Dictionary<Entity, Worker>();

        private EntityQuery _query;

        private bool updated = false;

        protected override void OnCreate()
        {
            base.OnCreate();

            _query = GetEntityQuery(new EntityQueryDesc()
            {
                All = new[] {
                    ComponentType.ReadWrite<Worker>()
                }
            });

            RequireForUpdate(_query);
        }

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            // One day (or month) in-game is '262144' ticks
            return 262144/512;
        }
        protected override void OnUpdate()
        {
            DateTime currentDateTime = World.GetExistingSystemManaged<Time2WorkTimeSystem>().GetCurrentDateTime();
            if (currentDateTime.Hour == 3 && currentDateTime.Minute < 4 && !updated)
            {
                var workers = _query.ToEntityArray(Allocator.Temp);
                float sum_day_shift = 0f;
                float sum_evening_shift = 0f;
                float sum_night_shift = 0f;
                float sum_last_commute = 0f;
                int count = 0;

                double eveningWorkPlaceShare = (float)Mod.m_Setting.evening_share / 100;
                double nightWorkPlaceShare = (float)Mod.m_Setting.night_share / 100;
                double dayProb = 1f - eveningWorkPlaceShare - nightWorkPlaceShare;

                int bin_size = (int)Math.Floor(30*Time2WorkTimeSystem.timeReductionFactor);
                float bin_min_size = 15f / Time2WorkTimeSystem.timeReductionFactor;
                int[] commute_min_bins = new int[bin_size];

                foreach (var worker in workers)
                {
                    Worker data;

                    if (!_WorkerToData.TryGetValue(worker, out data))
                    {
                        data = EntityManager.GetComponentData<Worker>(worker);

                        if (data.m_Shift == Workshift.Day)
                        {
                            sum_day_shift++;
                        }
                        if (data.m_Shift == Workshift.Evening)
                        {
                            sum_evening_shift++;
                        }
                        if (data.m_Shift == Workshift.Night)
                        {
                            sum_night_shift++;
                        }

                        float commute = 24f * 60f * 60f * (data.m_LastCommuteTime / Time2WorkTimeSystem.kTicksPerDay);

                        int b = (int)Math.Floor(commute / bin_min_size);
                        if (b > (bin_size - 1))
                        {
                            b = bin_size - 1;
                        }
                        commute_min_bins[b]++;

                        sum_last_commute += data.m_LastCommuteTime * 60f / Time2WorkTimeSystem.kTicksPerDay;
                        count++;
                    }
                }

                float percent_sum = 0;
                int i = bin_size - 1;
                while(percent_sum < 0.1f)
                {
                    percent_sum += (float)commute_min_bins[i] / (float)count;
                    i--;
                }

                float current_day_prob = sum_day_shift / (sum_day_shift + sum_evening_shift + sum_night_shift);
                float current_eve_prob = sum_evening_shift / (sum_day_shift + sum_evening_shift + sum_night_shift);
                float current_night_prob = sum_night_shift / (sum_day_shift + sum_evening_shift + sum_night_shift);
                Mod.log.Info($"Day Shift Workers %: {100 * current_day_prob}");
                Mod.log.Info($"Evening Shift Workers %: {100 * current_eve_prob}");
                Mod.log.Info($"Night Shift Workers %: {100 * current_night_prob}");
                Mod.m_ModData.average_commute = sum_last_commute * 24f / count;
                Mod.m_ModData.commute_top10per = (i * bin_min_size) /60f;
                Mod.log.Info($"Average Commute Time (hours): {Mod.m_ModData.average_commute}");
                Mod.log.Info($"Commute Time Top 10% (hours): {Mod.m_ModData.commute_top10per}");

                if(!Mod.m_Setting.peak_spread)
                {
                    Mod.m_ModData.commute_top10per = 0;
                }

                float new_sum_day_shift = 0f;
                float new_sum_evening_shift = 0f;
                float new_sum_night_shift = 0f;

                Unity.Mathematics.Random random = new Unity.Mathematics.Random(1);

                double day_prob2 = (1 - (eveningWorkPlaceShare + nightWorkPlaceShare) - current_day_prob) / (1f - current_day_prob);

                if (Math.Abs(current_night_prob - nightWorkPlaceShare) > 0.01 || Math.Abs(current_eve_prob - eveningWorkPlaceShare) > 0.01)
                {
                    foreach (var worker in workers)
                    {
                        Worker data;

                        if (!_WorkerToData.TryGetValue(worker, out data))
                        {
                            data = EntityManager.GetComponentData<Worker>(worker);
                            _WorkerToData.Add(worker, data);

                            if (data.m_Shift != Workshift.Day)
                            {
                                float prob = random.NextFloat();
                                if (prob < day_prob2)
                                {
                                    data.m_Shift = Workshift.Day;
                                    new_sum_day_shift++;
                                }
                                else
                                {
                                    if (prob >= day_prob2 && prob < day_prob2 + eveningWorkPlaceShare / (1f - current_day_prob))
                                    {
                                        data.m_Shift = Workshift.Evening;
                                        new_sum_evening_shift++;
                                    }
                                    else
                                    {
                                        data.m_Shift = Workshift.Night;
                                        new_sum_night_shift++;
                                    }
                                }
                            }
                            else
                            {
                                new_sum_day_shift++;
                            }

                            EntityManager.SetComponentData(worker, data);
                        }
                    }

                    Mod.log.Info($"New Day Shift Workers %: {100 * new_sum_day_shift / (new_sum_day_shift + new_sum_evening_shift + new_sum_night_shift)}");
                    Mod.log.Info($"New Evening Shift Workers %: {100 * new_sum_evening_shift / (new_sum_day_shift + new_sum_evening_shift + new_sum_night_shift)}");
                    Mod.log.Info($"New Night Shift Workers %: {100 * new_sum_night_shift / (new_sum_day_shift + new_sum_evening_shift + new_sum_night_shift)}");
                    updated = true;
                }
            } else
            {
                if (currentDateTime.Hour == 3 && currentDateTime.Minute < 10)
                {
                    updated = false;
                }
            }
        }
    }
}
