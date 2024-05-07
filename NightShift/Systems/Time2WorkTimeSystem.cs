using Colossal.Serialization.Entities;
using Game.Prefabs;
using Game.Simulation;
using Game.Common;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using UnityEngine;
using Game.Serialization;
using Game;
using UnityEngine.Scripting;
using System.ComponentModel;

namespace Time2Work
{
    public partial class Time2WorkTimeSystem : GameSystemBase
    {
        private SimulationSystem m_SimulationSystem;
        public static int kTicksPerDay;
        public static float timeReductionFactor;
        private float m_Time;
        private float m_Date;
        private int m_Year = 1;
        private int m_DaysPerYear = 1;
        private uint m_InitialFrame;
        private EntityQuery m_TimeSettingGroup;
        private EntityQuery m_TimeDataQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            this.m_SimulationSystem = this.World.GetOrCreateSystemManaged<SimulationSystem>();
            this.m_TimeSettingGroup = this.GetEntityQuery(ComponentType.ReadOnly<TimeSettingsData>());
            this.m_TimeDataQuery = this.GetEntityQuery(ComponentType.ReadOnly<TimeData>());
            this.RequireForUpdate(this.m_TimeSettingGroup);
            this.RequireForUpdate(this.m_TimeDataQuery);

            if(Mod.m_Setting.enable_slower_time)
            {
                timeReductionFactor = Mod.m_Setting.slow_time_factor;
                kTicksPerDay = (int)Math.Floor(timeReductionFactor * TimeSystem.kTicksPerDay);
            } else
            {
                kTicksPerDay = TimeSystem.kTicksPerDay;
                timeReductionFactor = 1f;
            }
            
            Mod.log.Info($"Day has {kTicksPerDay} ticks, Reduction Factor: {timeReductionFactor}");
        }

        protected int GetTicks(uint frameIndex, TimeSettingsData settings, TimeData data)
        {
            return (int)frameIndex - (int)data.m_FirstFrame + Mathf.RoundToInt(data.TimeOffset * kTicksPerDay) + Mathf.RoundToInt(data.GetDateOffset(settings.m_DaysPerYear) * kTicksPerDay * (float)settings.m_DaysPerYear);
        }

        protected int GetTicks(TimeSettingsData settings, TimeData data)
        {
            return (int)this.m_SimulationSystem.frameIndex - (int)data.m_FirstFrame + Mathf.RoundToInt(data.TimeOffset * kTicksPerDay) + Mathf.RoundToInt(data.GetDateOffset(settings.m_DaysPerYear) * kTicksPerDay * (float)settings.m_DaysPerYear);
        }

        protected double GetTimeWithOffset(
          TimeSettingsData settings,
          TimeData data,
          double renderingFrame)
        {
            return renderingFrame + (double)data.TimeOffset * kTicksPerDay + (double)data.GetDateOffset(settings.m_DaysPerYear) * kTicksPerDay * (double)settings.m_DaysPerYear;
        }

        public float GetTimeOfDay(TimeSettingsData settings, TimeData data, double renderingFrame)
        {
            return (float)(this.GetTimeWithOffset(settings, data, renderingFrame) % kTicksPerDay / kTicksPerDay);
        }

        protected float GetTimeOfDay(TimeSettingsData settings, TimeData data)
        {
            return (float)(this.GetTicks(settings, data) % kTicksPerDay) / kTicksPerDay;
        }

        public float GetTimeOfYear(TimeSettingsData settings, TimeData data, double renderingFrame)
        {
            int num = kTicksPerDay * settings.m_DaysPerYear;
            return (float)this.GetTimeWithOffset(settings, data, renderingFrame % (double)num) / (float)num;
        }

        protected float GetTimeOfYear(TimeSettingsData settings, TimeData data)
        {
            int num = kTicksPerDay * settings.m_DaysPerYear;
            return (float)(this.GetTicks(settings, data) % num) / (float)num;
        }

        public float GetElapsedYears(TimeSettingsData settings, TimeData data)
        {
            int num = kTicksPerDay * settings.m_DaysPerYear;
            return (float)(this.m_SimulationSystem.frameIndex - data.m_FirstFrame) / (float)num;
        }

        public float GetStartingDate(TimeSettingsData settings, TimeData data)
        {
            int num = kTicksPerDay * settings.m_DaysPerYear;
            return (float)(this.GetTicks(data.m_FirstFrame, settings, data) % num) / (float)num;
        }

        public int GetYear(TimeSettingsData settings, TimeData data)
        {
            int num = kTicksPerDay * settings.m_DaysPerYear;
            return data.m_StartingYear + Mathf.FloorToInt((float)(this.GetTicks(settings, data) / num));
        }

        public float normalizedTime => this.m_Time;

        public float normalizedDate => this.m_Date;

        public int year => this.m_Year;

        public int daysPerYear
        {
            get
            {
                if (this.m_DaysPerYear == 0 && !this.m_TimeSettingGroup.IsEmptyIgnoreFilter)
                {
                    this.m_DaysPerYear = this.m_TimeSettingGroup.GetSingleton<TimeSettingsData>().m_DaysPerYear;
                    if (this.m_DaysPerYear == 0)
                        this.m_DaysPerYear = 1;
                }
                return this.m_DaysPerYear;
            }
        }

        public static int GetDay(uint frame, TimeData data)
        {
            return Mathf.FloorToInt((float)(frame - data.m_FirstFrame) / kTicksPerDay + data.TimeOffset);
        }

        public static int GetDay(uint frame, TimeData data, int ticksPerDay)
        {
            return Mathf.FloorToInt((float)(frame - data.m_FirstFrame) / ticksPerDay + data.TimeOffset);
        }

        public void DebugAdvanceTime(int minutes)
        {
            TimeData singleton = this.m_TimeDataQuery.GetSingleton<TimeData>();
            Entity singletonEntity = this.m_TimeDataQuery.GetSingletonEntity();
            singleton.m_FirstFrame -= (uint)(minutes * kTicksPerDay) / 1440U;
            this.EntityManager.SetComponentData<TimeData>(singletonEntity, singleton);
        }

        private static DateTime CreateDateTime(int year, int day, int hour, int minute, float second)
        {
            DateTime dateTime = new DateTime(0L, DateTimeKind.Utc);
            dateTime = dateTime.AddYears(year - 1);
            dateTime = dateTime.AddDays((double)(day - 1));
            dateTime = dateTime.AddHours((double)hour);
            dateTime = dateTime.AddMinutes((double)minute);
            dateTime = dateTime.AddSeconds((double)second);
            if (dateTime.IsDaylightSavingTime())
                dateTime = dateTime.AddHours(1.0);
            return dateTime;
        }

        public DateTime GetDateTime(double renderingFrame)
        {
            TimeSettingsData singleton1 = this.m_TimeSettingGroup.GetSingleton<TimeSettingsData>();
            TimeData singleton2 = this.m_TimeDataQuery.GetSingleton<TimeData>();
            float timeOfDay = this.GetTimeOfDay(singleton1, singleton2, renderingFrame);
            float timeOfYear = this.GetTimeOfYear(singleton1, singleton2, renderingFrame);
            int hour = Mathf.FloorToInt(24f * timeOfDay);
            int minute = Mathf.FloorToInt((float)(60.0 * (24.0 * (double)timeOfDay - (double)hour)));
            return Time2WorkTimeSystem.CreateDateTime(this.year, 1 + Mathf.FloorToInt((float)this.daysPerYear * timeOfYear) % this.daysPerYear, hour, minute, Mathf.Repeat(timeOfDay, 1f));
        }

        public DateTime GetCurrentDateTime()
        {
            float normalizedTime = this.normalizedTime;
            float normalizedDate = this.normalizedDate;
            int hour = Mathf.FloorToInt(24f * normalizedTime);
            int minute = Mathf.FloorToInt((float)(60.0 * (24.0 * (double)normalizedTime - (double)hour)));
            return Time2WorkTimeSystem.CreateDateTime(this.year, 1 + Mathf.FloorToInt((float)this.daysPerYear * normalizedDate) % this.daysPerYear, hour, minute, Mathf.Repeat(normalizedTime, 1f));
        }

        [Preserve]
        protected override void OnUpdate() => this.UpdateTime();

        private void UpdateTime()
        {
            TimeSettingsData singleton1 = this.m_TimeSettingGroup.GetSingleton<TimeSettingsData>();
            TimeData singleton2 = this.m_TimeDataQuery.GetSingleton<TimeData>();
            this.m_Time = this.GetTimeOfDay(singleton1, singleton2);
            this.m_Date = this.GetTimeOfYear(singleton1, singleton2);
            this.m_Year = this.GetYear(singleton1, singleton2);
            this.m_DaysPerYear = singleton1.m_DaysPerYear;

            if (Mod.m_Setting.enable_slower_time)
            {
                timeReductionFactor = Mod.m_Setting.slow_time_factor;
                kTicksPerDay = (int)Math.Floor(timeReductionFactor * TimeSystem.kTicksPerDay);
            }
            else
            {
                kTicksPerDay = TimeSystem.kTicksPerDay;
                timeReductionFactor = 1f;
            }
        }

        [Preserve]
        public Time2WorkTimeSystem()
        {
        }
    }
}
