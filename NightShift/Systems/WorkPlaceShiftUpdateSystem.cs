﻿using Colossal.Logging;
using Game;
using Game.Citizens;
using Game.Companies;
using Game.Prefabs;
using Game.Simulation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;

namespace Time2Work.Systems
{
    public partial class WorkPlaceShiftUpdateSystem : GameSystemBase
    {
        private Dictionary<Entity, WorkplaceData> _workplaceToData = new Dictionary<Entity, WorkplaceData>();

        private EntityQuery _query;

        private bool updated = false;

        protected override void OnCreate()
        {
            base.OnCreate();

            _query = GetEntityQuery(new EntityQueryDesc()
            {
                All = new[] {
                    ComponentType.ReadWrite<WorkplaceData>()
                }
            });

            RequireForUpdate(_query);
        }

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            // One day (or month) in-game is '262144' ticks
            return (int)(Time2WorkTimeSystem.kTicksPerDay / Time2WorkTimeSystem.timeReductionFactor) / 512;
        }

        protected override void OnUpdate()
        {
            DateTime currentDateTime = World.GetExistingSystemManaged<Time2WorkTimeSystem>().GetCurrentDateTime();
            if (currentDateTime.Hour == 3 && currentDateTime.Minute < 4 && !updated)
            {
                var workplaces = _query.ToEntityArray(Allocator.Temp);

                float sum_W_WPD_NS = 0;
                float sum_W_WPD_ES = 0;
                float sum_W_WPD_NS_New = 0;
                float sum_W_WPD_ES_New = 0;
                float sumMW = 0;

                double eveningWorkPlaceShare = (float)Mod.m_Setting.evening_share / 100;
                double nightWorkPlaceShare = (float)Mod.m_Setting.night_share / 100;

                Mod.log.Info($"Evening Target Share: {eveningWorkPlaceShare}");
                Mod.log.Info($"Night Target Share: {nightWorkPlaceShare}");

                foreach (var workplace in workplaces)
                {
                    WorkplaceData data;

                    data = EntityManager.GetComponentData<WorkplaceData>(workplace);

                    sum_W_WPD_ES += data.m_EveningShiftProbability * data.m_MaxWorkers;
                    sum_W_WPD_NS += data.m_NightShiftProbability * data.m_MaxWorkers;
                    sumMW += data.m_MaxWorkers;
                }

                Mod.log.Info($"Evening Shift Probability Weighted Average: {sum_W_WPD_ES / sumMW}");
                Mod.log.Info($"Night Shift Probability Weighted Average: {sum_W_WPD_NS / sumMW}");

                if (sum_W_WPD_ES / sumMW > 0)
                {
                    double evening_factor = eveningWorkPlaceShare / (sum_W_WPD_ES / sumMW);
                    double night_factor = nightWorkPlaceShare / (sum_W_WPD_NS / sumMW);

                    foreach (var workplace in workplaces)
                    {
                        WorkplaceData data;

                        if (!_workplaceToData.TryGetValue(workplace, out data))
                        {
                            data = EntityManager.GetComponentData<WorkplaceData>(workplace);
                            _workplaceToData.Add(workplace, data);

                            data.m_EveningShiftProbability = (float)(evening_factor * data.m_EveningShiftProbability);
                            data.m_NightShiftProbability = (float)(night_factor * data.m_NightShiftProbability);

                            sum_W_WPD_ES_New += data.m_EveningShiftProbability * data.m_MaxWorkers;
                            sum_W_WPD_NS_New += data.m_NightShiftProbability * data.m_MaxWorkers;

                            EntityManager.SetComponentData(workplace, data);
                        }
                    }

                    Mod.log.Info($"New Evening Shift Probability Weighted Average: {sum_W_WPD_ES_New / sumMW}");
                    Mod.log.Info($"New Night Shift Probability Weighted Average: {sum_W_WPD_NS_New / sumMW}");
                }
                updated = true;
            } else
            {
                if(currentDateTime.Hour == 3 && currentDateTime.Minute < 10)
                {
                    updated = false;
                }
            }
        }
    }
}
