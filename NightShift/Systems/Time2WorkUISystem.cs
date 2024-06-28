using Colossal.UI.Binding;
using Game.UI;
using Game.Simulation;
using Unity.Mathematics;
using System;
using Time2Work.Systems;
using Time2Work.Utils;
using Game.Prefabs;
using UnityEngine;
using Unity.Entities;
using Game.Common;
using Game.SceneFlow;
using System.Collections.Generic;

namespace Time2Work
{
    public partial class Time2WorkUISystem : UISystemBase
    {
        private Throttle _weekUpdateThrottle;
        private EntityQuery m_TimeSettingsQuery;
        private EntityQuery m_TimeDataQuery;
        private SimulationSystem m_SimulationSystem;
        private ValueBinding<string> _weekDay;
        private string dateOutput = "";

        public string BindGroupName => Mod.harmonyID;


        private void Refresh()
        {
            
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            this.m_TimeSettingsQuery = this.GetEntityQuery(ComponentType.ReadOnly<TimeSettingsData>());
            this.m_TimeDataQuery = this.GetEntityQuery(ComponentType.ReadOnly<TimeData>());
            this.m_SimulationSystem = this.World.GetOrCreateSystemManaged<SimulationSystem>();

            AddBinding(_weekDay = new ValueBinding<string>(BindGroupName, "dayOfWeek", dateOutput));
            _weekUpdateThrottle = Throttle.BySeconds(1, () => { _weekDay.Update(dateOutput); });
        }
        private TimeSettingsData GetTimeSettingsData()
        {
            if (!this.m_TimeSettingsQuery.IsEmptyIgnoreFilter)
            {
                return this.m_TimeSettingsQuery.GetSingleton<TimeSettingsData>();
            }
            return new TimeSettingsData() { m_DaysPerYear = 12 };
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            _weekUpdateThrottle.Update(World.Time.DeltaTime);
            TimeSettingsData timeSettingsData = this.GetTimeSettingsData();
            TimeData singleton = TimeData.GetSingleton(this.m_TimeDataQuery);
            int ticksPerDay = Time2WorkTimeSystem.kTicksPerDay;
            int daysPerYear = timeSettingsData.m_DaysPerYear;
            int epochTicks = Mathf.RoundToInt(singleton.TimeOffset * Time2WorkTimeSystem.kTicksPerDay) + Mathf.RoundToInt(singleton.GetDateOffset(timeSettingsData.m_DaysPerYear) * Time2WorkTimeSystem.kTicksPerDay * (float)timeSettingsData.m_DaysPerYear);
            int epochYear = singleton.m_StartingYear;

            //Mod.log.Info($"{ticksPerDay},{daysPerYear},{epochTicks},{epochYear}");
            int n = epochTicks + this.GetTicks();
            int r = (int)Math.Floor((float)n / ticksPerDay);
            int year = (int)epochYear + (int)Math.Floor((float)r / daysPerYear);
            //Mod.log.Info($"year: {epochYear + Math.Floor((float)r / daysPerYear)}");
            int monthsPerYear = 12;
            int day = (int)(Math.Floor((float)(r / Mod.m_Setting.daysPerMonth)));
            //Mod.log.Info($"monthsPerYear: {monthsPerYear}, day: {day}, {daysPerYear}, {Mod.m_Setting.daysPerMonth}");
            int o = (day % monthsPerYear + monthsPerYear) % monthsPerYear + 1;
            int daysMonth = 30;
            if(o == 2)
            {
                daysMonth = 28;
            }
            day = day % daysMonth + 1;
            
            //Mod.log.Info($"month: {o}");

            if (year > 0 && WeekSystem.getDayOfWeekInt() >= 0)
            {
                try
                {
                    DateTime date = new DateTime(year, o, day, 0, 0, 0);
                    Setting.months m = (Setting.months)date.Month;
                    Setting.dayOfWeek w = (Setting.dayOfWeek)WeekSystem.getDayOfWeekInt();

                    Colossal.Localization.LocalizationDictionary dic = GameManager.instance.localizationManager.activeDictionary;

                    string mm = "";
                    string ww = "";
                    dic.TryGetValue(Mod.m_Setting.GetEnumValueLocaleID(m), out mm);
                    dic.TryGetValue(Mod.m_Setting.GetEnumValueLocaleID(w), out ww);
                    dateOutput = ww + " " + mm + " " + date.Year.ToString();
                }
                catch (Exception)
                {
                    Mod.log.Error($"Invalid Date - year:{year}, month:{o}, day:{day}, Days Per Month: {Mod.m_Setting.daysPerMonth}");
                    throw;
                }
            }
        }

        public int GetTicks()
        {
            float num = 182.044449f * 3f;
            return Mathf.FloorToInt(Mathf.Floor((float)(this.m_SimulationSystem.frameIndex - TimeData.GetSingleton(this.m_TimeDataQuery).m_FirstFrame) / num) * num);
        }
    }
}
