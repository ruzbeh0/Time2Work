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

        public string BindGroupName => nameof(Time2Work);

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

            if (m_SimulationSystem == null ||
                m_TimeDataQuery.IsEmptyIgnoreFilter ||
                Mod.m_Setting == null)
            {
                return;
            }

            _weekUpdateThrottle.Update(World.Time.DeltaTime);

            TimeSettingsData timeSettingsData = this.GetTimeSettingsData();
            TimeData singleton = TimeData.GetSingleton(this.m_TimeDataQuery);

            int ticksPerDay = Time2WorkTimeSystem.kTicksPerDay;
            int daysPerYear = timeSettingsData.m_DaysPerYear;
            int daysPerMonth = Math.Max(1, Mod.m_Setting.daysPerMonth);

            int epochTicks =
                Mathf.RoundToInt(singleton.TimeOffset * Time2WorkTimeSystem.kTicksPerDay) +
                Mathf.RoundToInt(singleton.GetDateOffset(timeSettingsData.m_DaysPerYear) *
                                 Time2WorkTimeSystem.kTicksPerDay *
                                 (float)timeSettingsData.m_DaysPerYear);

            int epochYear = singleton.m_StartingYear;

            int n = epochTicks + this.GetTicks();
            int totalElapsedDays = (int)Math.Floor((float)n / ticksPerDay);

            int year = epochYear + (int)Math.Floor((float)totalElapsedDays / daysPerYear);

            // This is the important correction:
            // derive month/day from the day INSIDE THE CURRENT YEAR,
            // not from the total elapsed day/month counter.
            int dayOfYear = totalElapsedDays % daysPerYear;
            if (dayOfYear < 0)
            {
                dayOfYear += daysPerYear;
            }

            int month = (dayOfYear / daysPerMonth) + 1;
            int dayOfMonth = (dayOfYear % daysPerMonth) + 1;

            if (month > 12)
            {
                month = 12;
            }

            if (year > 0 && WeekSystem.getDayOfWeekInt() >= 0)
            {
                Setting.months m = (Setting.months)month;
                Setting.dayOfWeek w = (Setting.dayOfWeek)WeekSystem.getDayOfWeekInt();

                Colossal.Localization.LocalizationDictionary dic = GameManager.instance.localizationManager.activeDictionary;

                string mm = "";
                string ww = "";

                dic.TryGetValue(Mod.m_Setting.GetEnumValueLocaleID(m), out mm);
                dic.TryGetValue(Mod.m_Setting.GetEnumValueLocaleID(w), out ww);

                switch (Mod.m_Setting.date_format)
                {
                    case Setting.DateFormatEnum.DayOfWeek_DDMMYYYY:
                        dateOutput = $"{ww} {dayOfMonth:00}/{month:00}/{year}";
                        break;

                    case Setting.DateFormatEnum.DayOfWeek_MMDDYYYY:
                        dateOutput = $"{ww} {month:00}/{dayOfMonth:00}/{year}";
                        break;

                    case Setting.DateFormatEnum.DayOfWeek_Month_Year:
                    default:
                        dateOutput = $"{ww} {mm} {year}";
                        break;
                }
            }
        }

        public int GetTicks()
        {
            float slowFactor = 1f;
            if (Mod.m_Setting != null)
            {
                slowFactor = Mod.m_Setting.slow_time_factor;
            }

            float num = 182.044449f * slowFactor;
            return Mathf.FloorToInt(
                Mathf.Floor((float)(this.m_SimulationSystem.frameIndex - TimeData.GetSingleton(this.m_TimeDataQuery).m_FirstFrame) / num) * num
            );
        }
    }
}
