using Game.City;
using Game.Simulation;
using HarmonyLib;
using System.Linq;
using System.Reflection;
using Time2Work.Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Time2Work.Patches
{
    [HarmonyPatch(typeof(CityServiceBudgetSystem),
                  nameof(CityServiceBudgetSystem.GetExpense),
                  new[] { typeof(ExpenseSource), typeof(NativeArray<int>) })]
    public static class GetExpense_Static_Postfix
    {
        [HarmonyPostfix]
        public static void Postfix(ExpenseSource source, NativeArray<int> expenses, ref int __result)
        {

            //Mod.log.Info($"GetExpense called for {source}, original expense: {__result}");
            // night window
            var t2w = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Time2WorkTimeSystem>();
            int hour = t2w.GetCurrentDateTime().Hour;
            if (!(hour >= 23 || hour <= 6)) return;

            // scale regardless of sign so subsidies (negative) move toward zero too
            int pct = math.clamp(Mod.m_Setting.service_expenses_night_reduction, 0, 100);
            float factor = (100f - pct) / 100f;
            if (factor < 0.9999f)
                __result = (int)math.round(__result * factor);
        }
    }

    //[HarmonyPatch(typeof(CityServiceBudgetSystem), nameof(CityServiceBudgetSystem.GetMoneyDelta))]
    //public static class MoneyDeltaPatch
    //{
    //    // Postfix: replace the vanilla estimate with our rolling ledger-based value
    //    public static void Postfix(CityServiceBudgetSystem __instance, ref int __result)
    //    {
    //        // Ensure our sampler system exists on the same world
    //        var world = __instance.World;
    //        LedgerTrendSystem.EnsureCreated(world);
    //
    //        var sampler = world.GetExistingSystemManaged<LedgerTrendSystem>();
    //        if (sampler != null && sampler.TryGetDailyRateEstimate(out int dailyRate))
    //        {
    //            __result = dailyRate;
    //        }
    //        else
    //        {
    //            // Fallback: keep vanilla result
    //        }
    //    }
    //}
}
