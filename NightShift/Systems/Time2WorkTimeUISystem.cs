using Colossal.PSI.Common;
using Colossal.Serialization.Entities;
using Colossal.UI.Binding;
using Game.Common;
using Game.Prefabs;
using Game.Rendering;
using Game.Simulation;
using Game.UI;
using System;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Scripting;

namespace Time2Work
{
    public partial class Time2WorkTimeUISystem : UISystemBase
    {
        private const string kGroup = "time";
        private SimulationSystem m_SimulationSystem;
        private Time2WorkTimeSystem m_TimeSystem;
        private LightingSystem m_LightingSystem;
        private EntityQuery m_TimeSettingsQuery;
        private EntityQuery m_TimeDataQuery;
        private EventBinding<bool> m_SimulationPausedBarrierBinding;
        private float m_SpeedBeforePause = 1f;
        private bool m_UnpausedBeforeForcedPause;
        private bool m_HasFocus = true;

        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();
            this.m_SimulationSystem = this.World.GetOrCreateSystemManaged<SimulationSystem>();
            this.m_TimeSystem = this.World.GetOrCreateSystemManaged<Time2WorkTimeSystem>();
            this.m_LightingSystem = this.World.GetOrCreateSystemManaged<LightingSystem>();
            this.m_TimeSettingsQuery = this.GetEntityQuery(ComponentType.ReadOnly<TimeSettingsData>());
            this.m_TimeDataQuery = this.GetEntityQuery(ComponentType.ReadOnly<TimeData>());
            this.AddUpdateBinding((IUpdateBinding)new GetterValueBinding<Time2WorkTimeUISystem.TimeSettings>("time", "timeSettings", (Func<Time2WorkTimeUISystem.TimeSettings>)(() =>
            {
                TimeSettingsData timeSettingsData = this.GetTimeSettingsData();
                TimeData singleton = TimeData.GetSingleton(this.m_TimeDataQuery);
                return new Time2WorkTimeUISystem.TimeSettings()
                {
                    ticksPerDay = Time2WorkTimeSystem.kTicksPerDay,
                    daysPerYear = timeSettingsData.m_DaysPerYear,
                    epochTicks = Mathf.RoundToInt(singleton.TimeOffset * Time2WorkTimeSystem.kTicksPerDay) + Mathf.RoundToInt(singleton.GetDateOffset(timeSettingsData.m_DaysPerYear) * Time2WorkTimeSystem.kTicksPerDay * (float)timeSettingsData.m_DaysPerYear),
                    epochYear = singleton.m_StartingYear
                };
            }), (IWriter<Time2WorkTimeUISystem.TimeSettings>)new ValueWriter<Time2WorkTimeUISystem.TimeSettings>()));
            this.AddUpdateBinding((IUpdateBinding)new GetterValueBinding<int>("time", "ticks", (Func<int>)(() =>
            {
                float num = 182.044449f*3f;
                return Mathf.FloorToInt(Mathf.Floor((float)(this.m_SimulationSystem.frameIndex - TimeData.GetSingleton(this.m_TimeDataQuery).m_FirstFrame) / num) * num);
            })));
            this.AddUpdateBinding((IUpdateBinding)new GetterValueBinding<int>("time", "day", (Func<int>)(() => Time2WorkTimeSystem.GetDay(this.m_SimulationSystem.frameIndex, TimeData.GetSingleton(this.m_TimeDataQuery)))));
            this.AddUpdateBinding((IUpdateBinding)new GetterValueBinding<LightingSystem.State>("time", "lightingState", (Func<LightingSystem.State>)(() =>
            {
                LightingSystem.State state = this.m_LightingSystem.state;
                if (state != LightingSystem.State.Invalid)
                    return state;
                float normalizedTime = this.m_TimeSystem.normalizedTime;
                return (double)normalizedTime >= 0.2916666567325592 && (double)normalizedTime <= 0.875 ? LightingSystem.State.Day : LightingSystem.State.Night;
            }), (IWriter<LightingSystem.State>)new DelegateWriter<LightingSystem.State>((WriterDelegate<LightingSystem.State>)((writer, value) => writer.Write((int)value)))));
            this.AddUpdateBinding((IUpdateBinding)new GetterValueBinding<bool>("time", "simulationPaused", (Func<bool>)(() => (double)this.m_SimulationSystem.selectedSpeed == 0.0)));
            this.AddUpdateBinding((IUpdateBinding)new GetterValueBinding<int>("time", "simulationSpeed", (Func<int>)(() => Time2WorkTimeUISystem.SpeedToIndex(this.IsPaused() ? this.m_SpeedBeforePause : this.m_SimulationSystem.selectedSpeed))));
            this.AddBinding((IBinding)(this.m_SimulationPausedBarrierBinding = new EventBinding<bool>("time", "simulationPausedBarrier")));
            this.AddBinding((IBinding)new TriggerBinding<bool>("time", "setSimulationPaused", (Action<bool>)(paused =>
            {
                if (!this.pausedBarrierActive)
                {
                    this.m_SimulationSystem.selectedSpeed = paused ? 0.0f : this.m_SpeedBeforePause;
                }
                else
                {
                    this.m_UnpausedBeforeForcedPause = !paused;
                }
            })));
            this.AddBinding((IBinding)new TriggerBinding<int>("time", "setSimulationSpeed", (Action<int>)(speedIndex =>
            {
                if (!this.pausedBarrierActive)
                {
                    this.m_SimulationSystem.selectedSpeed = Time2WorkTimeUISystem.IndexToSpeed(speedIndex);
                }
                else
                {
                    this.m_SpeedBeforePause = Time2WorkTimeUISystem.IndexToSpeed(speedIndex);
                    this.m_UnpausedBeforeForcedPause = true;
                }
            })));
            PlatformManager.instance.onAppStateChanged += (OnAppStateChanged)((psi, state) =>
            {
                if (state == AppState.Default)
                {
                    this.m_HasFocus = true;
                }
                else
                {
                    if (state != AppState.Constrained)
                        return;
                    this.m_HasFocus = false;
                }
            });
        }

        private void HandleAppStateChanged(IPlatformServiceIntegration psi, AppState state)
        {
            if (state == AppState.Default)
            {
                this.m_HasFocus = true;
            }
            else
            {
                if (state != AppState.Constrained)
                    return;
                this.m_HasFocus = false;
            }
        }

        protected override void OnGameLoaded(Context serializationContext)
        {
            base.OnGameLoaded(serializationContext);
            this.m_SpeedBeforePause = 1f;
        }

        [Preserve]
        protected override void OnUpdate()
        {
            base.OnUpdate();
            if ((double)this.m_SimulationSystem.selectedSpeed > 0.0)
            {
                this.m_SpeedBeforePause = this.m_SimulationSystem.selectedSpeed;
            }
            if ((!this.m_HasFocus ? 1 : (this.m_SimulationPausedBarrierBinding.observerCount > 0 ? 1 : 0)) != 0)
            {
                if (!this.IsPaused())
                {
                    this.m_UnpausedBeforeForcedPause = true;
                }
                this.m_SimulationSystem.selectedSpeed = 0.0f;
            }
            else
            {
                if (this.m_UnpausedBeforeForcedPause)
                {
                    this.m_SimulationSystem.selectedSpeed = this.m_SpeedBeforePause;
                }
                this.m_UnpausedBeforeForcedPause = false;
            }
        }

        private Time2WorkTimeUISystem.TimeSettings GetTimeSettings()
        {
            TimeSettingsData timeSettingsData = this.GetTimeSettingsData();
            TimeData singleton = TimeData.GetSingleton(this.m_TimeDataQuery);
            return new Time2WorkTimeUISystem.TimeSettings()
            {
                ticksPerDay = Time2WorkTimeSystem.kTicksPerDay,
                daysPerYear = timeSettingsData.m_DaysPerYear,
                epochTicks = Mathf.RoundToInt(singleton.TimeOffset * Time2WorkTimeSystem.kTicksPerDay) + Mathf.RoundToInt(singleton.GetDateOffset(timeSettingsData.m_DaysPerYear) * Time2WorkTimeSystem.kTicksPerDay * (float)timeSettingsData.m_DaysPerYear),
                epochYear = singleton.m_StartingYear
            };
        }

        public int GetTicks()
        {
            float num = 182.044449f*3f;
            return Mathf.FloorToInt(Mathf.Floor((float)(this.m_SimulationSystem.frameIndex - TimeData.GetSingleton(this.m_TimeDataQuery).m_FirstFrame) / num) * num);
        }

        public int GetDay()
        {
            return Time2WorkTimeSystem.GetDay(this.m_SimulationSystem.frameIndex, TimeData.GetSingleton(this.m_TimeDataQuery));
        }

        public LightingSystem.State GetLightingState()
        {
            LightingSystem.State state = this.m_LightingSystem.state;
            if (state != LightingSystem.State.Invalid)
                return state;
            float normalizedTime = this.m_TimeSystem.normalizedTime;
            return (double)normalizedTime >= 0.2916666567325592 && (double)normalizedTime <= 0.875 ? LightingSystem.State.Day : LightingSystem.State.Night;
        }

        public bool IsPaused() => (double)this.m_SimulationSystem.selectedSpeed == 0.0;

        public int GetSimulationSpeed()
        {
            return Time2WorkTimeUISystem.SpeedToIndex(this.IsPaused() ? this.m_SpeedBeforePause : this.m_SimulationSystem.selectedSpeed);
        }

        private TimeSettingsData GetTimeSettingsData()
        {
            if (!this.m_TimeSettingsQuery.IsEmptyIgnoreFilter)
            {
                return this.m_TimeSettingsQuery.GetSingleton<TimeSettingsData>();
            }
            return new TimeSettingsData() { m_DaysPerYear = 12 };
        }

        private void SetSimulationPaused(bool paused)
        {
            if (!this.pausedBarrierActive)
            {
                this.m_SimulationSystem.selectedSpeed = paused ? 0.0f : this.m_SpeedBeforePause;
            }
            else
            {
                this.m_UnpausedBeforeForcedPause = !paused;
            }
        }

        private void SetSimulationSpeed(int speedIndex)
        {
            if (!this.pausedBarrierActive)
            {
                this.m_SimulationSystem.selectedSpeed = Time2WorkTimeUISystem.IndexToSpeed(speedIndex);
            }
            else
            {
                this.m_SpeedBeforePause = Time2WorkTimeUISystem.IndexToSpeed(speedIndex);
                this.m_UnpausedBeforeForcedPause = true;
            }
        }

        private bool pausedBarrierActive => this.m_SimulationPausedBarrierBinding.observerCount > 0;

        private static float IndexToSpeed(int index) => Mathf.Pow(2f, (float)Mathf.Clamp(index, 0, 2));

        private static int SpeedToIndex(float speed)
        {
            return (double)speed <= 0.0 ? 0 : Mathf.Clamp((int)Mathf.Log(speed, 2f), 0, 2);
        }

        [Preserve]
        public Time2WorkTimeUISystem()
        {
        }

        private struct TimeSettings : IJsonWritable, IEquatable<Time2WorkTimeUISystem.TimeSettings>
        {
            public int ticksPerDay;
            public int daysPerYear;
            public int epochTicks;
            public int epochYear;

            public void Write(IJsonWriter writer)
            {
                writer.TypeBegin(this.GetType().FullName);
                writer.PropertyName("ticksPerDay");
                writer.Write(this.ticksPerDay);
                writer.PropertyName("daysPerYear");
                writer.Write(this.daysPerYear);
                writer.PropertyName("epochTicks");
                writer.Write(this.epochTicks);
                writer.PropertyName("epochYear");
                writer.Write(this.epochYear);
                writer.TypeEnd();
            }

            public bool Equals(Time2WorkTimeUISystem.TimeSettings other)
            {
                return this.ticksPerDay == other.ticksPerDay && this.daysPerYear == other.daysPerYear && this.epochTicks == other.epochTicks && this.epochYear == other.epochYear;
            }
        }
    }
}
