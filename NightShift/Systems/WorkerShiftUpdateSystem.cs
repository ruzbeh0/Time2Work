using Colossal.Logging;
using Game;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Companies;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
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
        public ComponentLookup<PrefabRef> PrefabRefLookup;
        public ComponentLookup<CommercialProperty> CommercialPropertyLookup;
        public ComponentLookup<IndustrialProperty> IndustrialPropertyLookup;
        public ComponentLookup<OfficeProperty> OfficePropertyLookup;
        public ComponentLookup<PropertyRenter> PropertyRenterLookup;

        private EntityQuery _query;

        private int lastUpdatedDay = -1;

        protected override void OnCreate()
        {
            base.OnCreate();

            _query = GetEntityQuery(new EntityQueryDesc()
            {
                All = new[] {
                    ComponentType.ReadWrite<Worker>()
                },
                None =
                    [
                        ComponentType.Exclude<Deleted>(),
                        ComponentType.Exclude<Temp>()
                    ],
            });

            RequireForUpdate(_query);
            PrefabRefLookup = SystemAPI.GetComponentLookup<PrefabRef>(true);
            PropertyRenterLookup = SystemAPI.GetComponentLookup<PropertyRenter>(true);
            OfficePropertyLookup = SystemAPI.GetComponentLookup<OfficeProperty>(true);
            CommercialPropertyLookup = SystemAPI.GetComponentLookup<CommercialProperty>(true);
            IndustrialPropertyLookup = SystemAPI.GetComponentLookup<IndustrialProperty>(true);
        }

        private void calculateShifts(int iterations)
        {
            
            //Get probabilities
            double eveningWorkPlaceShare = (float)Mod.m_Setting.evening_share / 100;
            double nightWorkPlaceShare = (float)Mod.m_Setting.night_share / 100;
            double dayProb = 1f - eveningWorkPlaceShare - nightWorkPlaceShare;

            double nondayOfficeShare = Mod.m_Setting.nonday_office_share / 100f;
            double nondayCommercialShare = Mod.m_Setting.nonday_commercial_share / 100f;
            double nondayIndustryShare = Mod.m_Setting.nonday_industry_share / 100f;
            double nondayCityServicesShare = Mod.m_Setting.nonday_cityservices_share / 100f;

            int bin_size = (int)Math.Floor(30 * Time2WorkTimeSystem.timeReductionFactor);
            float bin_min_size = 15f / Time2WorkTimeSystem.timeReductionFactor;

            var workers = _query.ToEntityArray(Allocator.Temp);

            double day_prob2 = 0f;
            double office_day_prob2 = 0f;
            double commercial_day_prob2 = 0f;
            double industry_day_prob2 = 0f;
            double cityservices_day_prob2 = 0f;

            double day_prob2_previous = 0f;
            double office_day_prob2_previous = 0f;
            double commercial_day_prob2_previous = 0f;
            double industry_day_prob2_previous = 0f;
            double cityservices_day_prob2_previous = 0f;


            //Run iterative process
            for (int iter = 0; iter < iterations; iter++)
            {
                float sum_day_shift = 0f;
                float sum_evening_shift = 0f;
                float sum_night_shift = 0f;
                float sum_last_commute = 0f;
                float sum_day_office = 0f;
                float sum_nonday_office = 0;
                float sum_day_commercial = 0f;
                float sum_nonday_commercial = 0;
                float sum_day_industry = 0f;
                float sum_nonday_industry = 0;
                float sum_day_cityservices = 0f;
                float sum_nonday_cityservices = 0;
                int count = 0;
                int[] commute_min_bins = new int[bin_size];

                foreach (var worker in workers)
                {
                    Worker data;

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

                    if (PrefabRefLookup.TryGetComponent(data.m_Workplace, out var prefab1))
                    {
                        if (PropertyRenterLookup.TryGetComponent(data.m_Workplace, out var propertyRenter))
                        {
                            if (OfficePropertyLookup.HasComponent(propertyRenter.m_Property))
                            {
                                if (data.m_Shift == Workshift.Day)
                                {
                                    sum_day_office++;
                                }
                                else
                                {
                                    sum_nonday_office++;
                                }
                            }
                            else if (CommercialPropertyLookup.HasComponent(propertyRenter.m_Property))
                            {
                                if (data.m_Shift == Workshift.Day)
                                {
                                    sum_day_commercial++;
                                }
                                else
                                {
                                    sum_nonday_commercial++;
                                }
                            }
                            else if (IndustrialPropertyLookup.HasComponent(propertyRenter.m_Property))
                            {
                                if (data.m_Shift == Workshift.Day)
                                {
                                    sum_day_industry++;
                                }
                                else
                                {
                                    sum_nonday_industry++;
                                }
                            }
                            else
                            {
                                if (data.m_Shift == Workshift.Day)
                                {
                                    sum_day_cityservices++;
                                }
                                else
                                {
                                    sum_nonday_cityservices++;
                                }
                            }
                        }
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

                float percent_sum = 0;
                int i = bin_size - 1;
                while (percent_sum < 0.1f)
                {
                    percent_sum += (float)commute_min_bins[i] / (float)count;
                    i--;
                }
                if(sum_day_shift + sum_evening_shift + sum_night_shift > 0)
                {
                    float current_day_prob = sum_day_shift / (sum_day_shift + sum_evening_shift + sum_night_shift);
                    float current_eve_prob = sum_evening_shift / (sum_day_shift + sum_evening_shift + sum_night_shift);
                    float current_night_prob = sum_night_shift / (sum_day_shift + sum_evening_shift + sum_night_shift);

                    float current_office_nonday_prob = sum_nonday_office / (sum_day_office + sum_nonday_office);
                    float current_commercial_nonday_prob = sum_nonday_commercial / (sum_day_commercial + sum_nonday_commercial);
                    float current_industry_nonday_prob = sum_nonday_industry / (sum_day_industry + sum_nonday_industry);
                    float current_cityservices_nonday_prob = sum_nonday_cityservices / (sum_day_cityservices + sum_nonday_cityservices);

                    if(iter == 0)
                    {
                        //Mod.log.Info($"Iteration: {iter}");
                        try
                        {
                            Mod.log.Info($"Day Shift Workers %: {100 * current_day_prob}");
                            Mod.log.Info($"Evening Shift Workers %: {100 * current_eve_prob}");
                            Mod.log.Info($"Night Shift Workers %: {100 * current_night_prob}");
                            Mod.log.Info($"Office Non Day Workers %: {100 * current_office_nonday_prob}");
                            Mod.log.Info($"Commercial Non Day Workers %: {100 * current_commercial_nonday_prob}");
                            Mod.log.Info($"Industry Non Day Workers %: {100 * current_industry_nonday_prob}");
                            Mod.log.Info($"City Services Non Day Workers %: {100 * current_industry_nonday_prob}");
                            Mod.m_Setting.average_commute = sum_last_commute * 24f / count;
                            Mod.m_Setting.commute_top10per = (i * bin_min_size) / 60f;
                            Mod.log.Info($"Average Commute Time (hours): {Mod.m_Setting.average_commute}");
                            Mod.log.Info($"Commute Time Top 10% (hours): {Mod.m_Setting.commute_top10per}");
                        }
                        catch (Exception e)
                        {
                            Mod.log.Info($"Error printing Realistic Trips probabilities");
                        }
                        
                    }

                    if (!Mod.m_Setting.peak_spread)
                    {
                        Mod.m_Setting.commute_top10per = 0;
                    }

                    float new_sum_day_shift = 0f;
                    float new_sum_evening_shift = 0f;
                    float new_sum_night_shift = 0f;

                    float new_sum_office_nonday_shift = 0f;
                    float new_sum_commercial_nonday_shift = 0f;
                    float new_sum_industry_nonday_shift = 0f;
                    float new_sum_cityservices_nonday_shift = 0f;

                    float new_sum_office_day_shift = 0f;
                    float new_sum_commercial_day_shift = 0f;
                    float new_sum_industry_day_shift = 0f;
                    float new_sum_cityservices_day_shift = 0f;

                    Unity.Mathematics.Random random = new Unity.Mathematics.Random(1);

                    if(iter > 0)
                    {
                        day_prob2_previous = day_prob2;
                        office_day_prob2_previous = office_day_prob2;
                        commercial_day_prob2_previous = commercial_day_prob2;
                        industry_day_prob2_previous = industry_day_prob2;
                        cityservices_day_prob2_previous = cityservices_day_prob2;
                    }

                    day_prob2 = 1f - (eveningWorkPlaceShare + nightWorkPlaceShare + 1 - current_day_prob)/2;
                    office_day_prob2 = 1 - (nondayOfficeShare + current_office_nonday_prob)/2;
                    commercial_day_prob2 = 1 - (nondayCommercialShare + current_commercial_nonday_prob)/2;
                    industry_day_prob2 = 1 - (nondayIndustryShare + current_industry_nonday_prob)/2;
                    cityservices_day_prob2 = 1 - (nondayCityServicesShare + current_cityservices_nonday_prob)/2;

                    //Smoothing day probabilities with values from the previous iteration
                    if(iter > 0)
                    {
                        day_prob2 = 0.7f * day_prob2 + 0.3f * day_prob2_previous;
                        office_day_prob2 = 0.7f * office_day_prob2 + 0.3f * office_day_prob2_previous;
                        commercial_day_prob2 = 0.7f * commercial_day_prob2 + 0.3f * commercial_day_prob2_previous;
                        industry_day_prob2 = 0.7f * industry_day_prob2 + 0.3f * industry_day_prob2_previous;
                        cityservices_day_prob2 = 0.7f * cityservices_day_prob2 + 0.3f * cityservices_day_prob2_previous;

                    }

                    foreach (var worker in workers)
                     {
                         Worker data;

                         data = EntityManager.GetComponentData<Worker>(worker);
                         double day_prob = (1 - (eveningWorkPlaceShare + nightWorkPlaceShare));

                         float prob = random.NextFloat();
                         float prob2 = random.NextFloat();
                         if (iter % 2 == 0)
                          {
                              adjustBasedOnShifts(eveningWorkPlaceShare, nightWorkPlaceShare, day_prob, day_prob2, ref data, prob, prob2, current_day_prob);
                          }
                          else 
                          {
                              //Adjust based on workplace
                              adjustBasedOnWorkType(eveningWorkPlaceShare, nightWorkPlaceShare, office_day_prob2, commercial_day_prob2, industry_day_prob2, cityservices_day_prob2, ref data, prob, prob2);
                          }

                         updateCounters(ref data, ref new_sum_day_shift, ref new_sum_evening_shift, ref new_sum_night_shift, ref new_sum_office_nonday_shift, ref new_sum_commercial_nonday_shift, ref new_sum_industry_nonday_shift, ref new_sum_cityservices_nonday_shift, ref new_sum_office_day_shift, ref new_sum_commercial_day_shift, ref new_sum_industry_day_shift, ref new_sum_cityservices_day_shift);

                          if (iter == iterations - 1)
                          {
                              EntityManager.SetComponentData(worker, data);
                          } 
                     }


                     if (iter == iterations - 1)
                     {
                        Mod.log.Info($"New Day Shift Workers %: {new_sum_day_shift},{new_sum_day_shift},{new_sum_evening_shift},{new_sum_night_shift}");
                        Mod.log.Info($"New Day Shift Workers %: {100 * new_sum_day_shift / (new_sum_day_shift + new_sum_evening_shift + new_sum_night_shift)}");
                         Mod.log.Info($"New Evening Shift Workers %: {100 * new_sum_evening_shift / (new_sum_day_shift + new_sum_evening_shift + new_sum_night_shift)}");
                         Mod.log.Info($"New Night Shift Workers %: {100 * new_sum_night_shift / (new_sum_day_shift + new_sum_evening_shift + new_sum_night_shift)}");
                         Mod.log.Info($"New Office Non Day Shift Workers %: {100 * new_sum_office_nonday_shift / (new_sum_office_nonday_shift + new_sum_office_day_shift)}");
                         Mod.log.Info($"New Commercial Non Day Shift Workers %: {100 * new_sum_commercial_nonday_shift / (new_sum_commercial_nonday_shift + new_sum_commercial_day_shift)}");
                         Mod.log.Info($"New Industry Non Day Shift Workers %: {100 * new_sum_industry_nonday_shift / (new_sum_industry_nonday_shift + new_sum_industry_day_shift)}");
                         Mod.log.Info($"New City Services Non Day Shift Workers %: {100 * new_sum_cityservices_nonday_shift / (new_sum_cityservices_nonday_shift + new_sum_cityservices_day_shift)}");
                     }
                }
            }
        }

        private void updateCounters(ref Worker data, ref float new_sum_day_shift, ref float new_sum_evening_shift, ref float new_sum_night_shift, ref float new_sum_office_nonday_shift, ref float new_sum_commercial_nonday_shift, ref float new_sum_industry_nonday_shift, ref float new_sum_cityservices_nonday_shift, ref float new_sum_office_day_shift, ref float new_sum_commercial_day_shift, ref float new_sum_industry_day_shift, ref float new_sum_cityservices_day_shift)
        {
            if (data.m_Shift == Workshift.Day)
            {
                new_sum_day_shift++;
            }
            else if (data.m_Shift == Workshift.Evening)
            {
                new_sum_evening_shift++;
            } else
            {
                new_sum_night_shift++;
            }

            if (PrefabRefLookup.TryGetComponent(data.m_Workplace, out var prefab1))
            {
                if (PropertyRenterLookup.TryGetComponent(data.m_Workplace, out var propertyRenter))
                {
                    if (OfficePropertyLookup.HasComponent(propertyRenter.m_Property))
                    {
                        if (data.m_Shift == Workshift.Day)
                        {
                                new_sum_office_day_shift++;
                        }
                        else
                        {
                            new_sum_office_nonday_shift++;
                        }
                    }
                    else if (CommercialPropertyLookup.HasComponent(propertyRenter.m_Property))
                    {
                        if (data.m_Shift == Workshift.Day)
                        {
                            new_sum_commercial_day_shift++;
                        }
                        else
                        {
                            new_sum_commercial_nonday_shift++;
                        }
                    }
                    else if (IndustrialPropertyLookup.HasComponent(propertyRenter.m_Property))
                    {
                        if (data.m_Shift == Workshift.Day)
                        {
                            new_sum_industry_day_shift++;
                        }
                        else
                        {
                            new_sum_industry_nonday_shift++;
                        }
                    }
                    else
                    {
                        if (data.m_Shift == Workshift.Day)
                        {
                            new_sum_cityservices_day_shift++;
                        }
                        else
                        {

                            new_sum_cityservices_nonday_shift++;
                        }
                    }
                }
            }
        }

        private static void adjustBasedOnShifts(double eveningWorkPlaceShare, double nightWorkPlaceShare, double day_prob, double day_prob2, ref Worker data, float prob, float prob2, float current_day_prob)
        {
            if (data.m_Shift != Workshift.Day)
            {
                if (prob < day_prob2)
                {
                    data.m_Shift = Workshift.Day;
                    
                }
                else
                {
                    if (prob2 < eveningWorkPlaceShare/(eveningWorkPlaceShare + nightWorkPlaceShare))
                    {
                        data.m_Shift = Workshift.Evening;
                        
                    }
                    else
                    {
                        data.m_Shift = Workshift.Night;
                        
                    }
                }
            } 
        }

        private void adjustBasedOnWorkType(double eveningWorkPlaceShare, double nightWorkPlaceShare, double office_day_prob2, double commercial_day_prob2, double industry_day_prob2, double cityservices_day_prob2, ref Worker data, float prob, float prob2)
        {
            if (PrefabRefLookup.TryGetComponent(data.m_Workplace, out var prefab1))
            {
                if (PropertyRenterLookup.TryGetComponent(data.m_Workplace, out var propertyRenter))
                {
                    if (OfficePropertyLookup.HasComponent(propertyRenter.m_Property))
                    {
                        if (data.m_Shift != Workshift.Day)
                        {
                            if (prob < office_day_prob2)
                            {
                                data.m_Shift = Workshift.Day;
                            }
                        } else
                        {
                            if (prob >= office_day_prob2)
                            {
                                if(prob2 > 0.4f)
                                {
                                    data.m_Shift = Workshift.Evening;
                                } else
                                {
                                    data.m_Shift = Workshift.Night;
                                }
                            } 
                        }
                    }
                    else if (CommercialPropertyLookup.HasComponent(propertyRenter.m_Property))
                    {
                        if (data.m_Shift != Workshift.Day)
                        {
                            if (prob < commercial_day_prob2)
                            {
                                data.m_Shift = Workshift.Day;
                            }
                        } else
                        {
                            if (prob >= commercial_day_prob2)
                            {
                                if (prob2 > 0.4f)
                                {
                                    data.m_Shift = Workshift.Evening;
                                }
                                else
                                {
                                    data.m_Shift = Workshift.Night;
                                }
                            } 
                        }
                    }
                    else if (IndustrialPropertyLookup.HasComponent(propertyRenter.m_Property))
                    {
                        if (data.m_Shift != Workshift.Day)
                        {
                            if (prob < industry_day_prob2)
                            {
                                data.m_Shift = Workshift.Day;
                            }
                        } else
                        {
                            if (prob >= industry_day_prob2)
                            {
                                if (prob2 > 0.4f)
                                {
                                    data.m_Shift = Workshift.Evening;
                                }
                                else
                                {
                                    data.m_Shift = Workshift.Night;
                                }
                            }   
                        }
                    }
                    else
                    {
                        if (data.m_Shift != Workshift.Day)
                        {
                            if (prob < cityservices_day_prob2)
                            {
                                data.m_Shift = Workshift.Day;
                            }
                        } else
                        {
                            if (prob >= cityservices_day_prob2)
                            {
                                if (prob2 > 0.4f)
                                {
                                    data.m_Shift = Workshift.Evening;
                                }
                                else
                                {
                                    data.m_Shift = Workshift.Night;
                                }
                            } 
                        }
                    }
                }
            }
        }

        protected override void OnGameLoadingComplete(Colossal.Serialization.Entities.Purpose purpose, GameMode mode)
        {
            calculateShifts(16);
        }

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            // One day (or month) in-game is '262144' ticks
            return 262144 / 512;
        }
        protected override void OnUpdate()
        {
            DateTime currentDateTime = World.GetExistingSystemManaged<Time2WorkTimeSystem>().GetCurrentDateTime();
            int day = currentDateTime.Day;
            //Only run every other day
            if (currentDateTime.Hour == 3 && currentDateTime.Minute < 4 && lastUpdatedDay != day)
            {
                calculateShifts(4);
                lastUpdatedDay = day;
            }
        }
    }
}
