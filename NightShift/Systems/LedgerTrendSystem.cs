using Game;
using Game.Common;
using Game.Simulation;
using HarmonyLib;
using System;
using Time2Work;
using Unity.Entities;
using Unity.Mathematics;

// Realistic Trips — Ledger-based budget trend override (Build-safe + Smoothed)
// ---------------------------------------------------------------------------
// - NO references to TimeData.m_Tick (uses SimulationSystem.frameIndex)
// - NO references to ResourceSystem/PlayerResource
// - Smooths the trend via windowed average + EMA to avoid jitter
//
// Drop this in your project and patch Harmony. Optionally call
//   LedgerTrendSystem.EnsureCreated(World.DefaultGameObjectInjectionWorld);
// from your bootstrap. Otherwise it will lazy-create when GetMoneyDelta is called.

namespace Time2Work.Systems
{
    public sealed partial class LedgerTrendSystem : GameSystemBase
    {
        // Sample once per in-game minute (ticksPerDay/1440), using your mod's constant
        private int SampleIntervalTicks => math.max(1, Time2WorkTimeSystem.kTicksPerDay / 1440);

        private const int HoursInWindow = 6;  // keep 6 hours of history
        private const int SmoothingMinutes = 60; // compute rate over last N minutes
        private const float EmaAlpha = 0.15f; // EMA factor (0..1). Lower = smoother

        private CityServiceBudgetSystem m_Budget;

        // Wallet samples (if we bind the real wallet later)
        private readonly long[] _walletSamples = new long[HoursInWindow * 60 + 2];
        private int _wHead, _wCount;

        // Fallback: panel balance samples
        private readonly int[] _balanceSamples = new int[HoursInWindow * 60 + 2];
        private int _bHead, _bCount;

        // EMA state
        private int _emaDailyRate;
        private bool _emaInitialized;

        // last sampled frame (we use SimulationSystem.frameIndex)
        private uint _lastFrame;

        private MoneyReader _moneyReader; // can be bound later to real wallet

        protected override void OnCreate()
        {
            base.OnCreate();
            // We only use TimeData to ensure the sim world is running
            RequireForUpdate(GetEntityQuery(ComponentType.ReadOnly<TimeData>()));
            m_Budget = World.GetOrCreateSystemManaged<CityServiceBudgetSystem>();
            _moneyReader = new MoneyReader(World); // unbound by default
        }

        public static void EnsureCreated(World world)
        {
            if (world.GetExistingSystemManaged<LedgerTrendSystem>() == null)
                world.CreateSystemManaged<LedgerTrendSystem>();
        }

        protected override void OnUpdate()
        {
            // Stable time source across builds
            uint frame = (uint)World.GetExistingSystemManaged<SimulationSystem>().frameIndex;
            if (_lastFrame != 0 && frame - _lastFrame < SampleIntervalTicks)
                return;
            _lastFrame = frame;

            if (_moneyReader.TryReadCityMoney(out long money))
                PushWallet(money);

            int panelBalance = 0;
            try { panelBalance = m_Budget?.GetBalance() ?? 0; } catch { }
            PushBalance(panelBalance);
        }

        private void PushWallet(long v)
        {
            _walletSamples[_wHead] = v; _wHead = (_wHead + 1) % _walletSamples.Length; _wCount = math.min(_wCount + 1, _walletSamples.Length);
        }
        private void PushBalance(int v)
        {
            _balanceSamples[_bHead] = v; _bHead = (_bHead + 1) % _balanceSamples.Length; _bCount = math.min(_bCount + 1, _balanceSamples.Length);
        }

        public bool TryGetDailyRateEstimate(out int dailyRate)
        {
            // Prefer true wallet if available
            if (_wCount >= (SmoothingMinutes + 1) && _moneyReader.IsValid)
            {
                int a = (_wHead - 1 + _walletSamples.Length) % _walletSamples.Length;
                int b = (_wHead - 1 - SmoothingMinutes + _walletSamples.Length) % _walletSamples.Length;
                long d = _walletSamples[a] - _walletSamples[b];
                int rawDaily = ScaleMinutesToDay(d, SmoothingMinutes);
                dailyRate = ApplyEma(rawDaily);
                return true;
            }

            // Fallback: use panel balance snapshots
            if (_bCount >= (SmoothingMinutes + 1))
            {
                int a = (_bHead - 1 + _balanceSamples.Length) % _balanceSamples.Length;
                int b = (_bHead - 1 - SmoothingMinutes + _balanceSamples.Length) % _balanceSamples.Length;
                int d = _balanceSamples[a] - _balanceSamples[b];
                int rawDaily = ScaleMinutesToDay(d, SmoothingMinutes);
                dailyRate = ApplyEma(rawDaily);
                return true;
            }

            dailyRate = 0; return false;
        }

        private static int ScaleMinutesToDay(long deltaOverMinutes, int minutes)
        {
            double perMinute = deltaOverMinutes / (double)minutes;
            double perDay = perMinute * 1440.0; // 60 * 24
            long clamped = (long)math.round(perDay);
            return (int)math.clamp(clamped, int.MinValue, int.MaxValue);
        }

        private int ApplyEma(int rawDaily)
        {
            if (!_emaInitialized) { _emaDailyRate = rawDaily; _emaInitialized = true; }
            else { _emaDailyRate = (int)math.round(_emaDailyRate + EmaAlpha * (rawDaily - _emaDailyRate)); }
            return _emaDailyRate;
        }
    }

    // Build-safe placeholder. Bind this when you know the API that returns city money (long).
    internal sealed class MoneyReader
    {
        private readonly World _world;
        private Func<long> _getter;
        public bool IsValid => _getter != null;
        public MoneyReader(World world) { _world = world; }
        public bool TryReadCityMoney(out long money)
        {
            if (_getter != null) { try { money = _getter(); return true; } catch { } }
            money = 0; return false;
        }
        public void Bind(Func<long> getter) { _getter = getter; }
    }
}
