using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Prefabs;
using Game.Prefabs.Climate;
using Game.Simulation;
using Game.UI.InGame;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.PlayerLoop;
using static Game.Rendering.Debug.RenderPrefabRenderer;
using static Game.Simulation.ClimateSystem;

namespace Time2Work.Patches
{

    [HarmonyPatch]
    public class Time2WorkPatches
    {

        [HarmonyPatch(typeof(TimeSystem), "OnUpdate")]
        [HarmonyPostfix]
        public static void TimeSystemPatches_OnUpdate_Postfix(TimeSystem __instance)
        {
            Time2WorkTimeSystem t2wTimeSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Time2WorkTimeSystem>();
            Traverse.Create(__instance).Field("m_Time").SetValue(t2wTimeSystem.normalizedTime);
            Traverse.Create(__instance).Field("m_Date").SetValue(t2wTimeSystem.normalizedDate);
            Traverse.Create(__instance).Field("m_Year").SetValue(t2wTimeSystem.year);
        }

        //[HarmonyPatch(typeof(StorageTransferSystem), "OnUpdate")]
        //[HarmonyPrefix]
        //public static bool StorageTransferSystem_OnUpdate_PreFix()
        //{
        //    Time2WorkTimeSystem t2wTimeSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Time2WorkTimeSystem>();
        //    if ((t2wTimeSystem.normalizedTime > 0.25f && (t2wTimeSystem.normalizedTime < 0.625f) && Mod.m_Setting.better_trucks))
        //    {
        //        return false;
        //    } else
        //    {
        //        return true;
        //    }
        //}

        // Overload 1: GetYear(TimeSettingsData settings, TimeData data)
        [HarmonyPatch(typeof(TimeSystem), "GetYear", new Type[] { typeof(TimeSettingsData), typeof(TimeData) })]
        [HarmonyPrefix]
        static bool TimeSystemPatches_GetYear(TimeSettingsData settings, TimeData data, ref int __result, TimeSystem __instance)
        {
            Time2WorkTimeSystem t2wTimeSystem = World.DefaultGameObjectInjectionWorld
                .GetOrCreateSystemManaged<Time2WorkTimeSystem>();

            __result = t2wTimeSystem.GetYear(settings, data);
            return false;
        }

        // Overload 2: GetYear(TimeSettingsData settings, TimeData data, double renderingFrame)
        [HarmonyPatch(typeof(TimeSystem), "GetYear", new Type[] { typeof(TimeSettingsData), typeof(TimeData), typeof(double) })]
        [HarmonyPrefix]
        static bool TimeSystemPatches_GetYear_Rendering(TimeSettingsData settings, TimeData data, double renderingFrame, ref int __result, TimeSystem __instance)
        {
            Time2WorkTimeSystem t2wTimeSystem = World.DefaultGameObjectInjectionWorld
                .GetOrCreateSystemManaged<Time2WorkTimeSystem>();

            __result = t2wTimeSystem.GetYear(settings, data, renderingFrame);
            return false;
        }


        [HarmonyPatch(typeof(TimeSystem), "get_normalizedDate")]
        [HarmonyPrefix]
        static bool TimeSystemPatches_normalizedDate(ref float __result)
        {
            Time2WorkTimeSystem t2wTimeSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Time2WorkTimeSystem>();
            __result = t2wTimeSystem.normalizedDate;
            return false; // Skip original getter
        }


        [HarmonyPatch(typeof(TimeSystem), "GetDay")]
        [HarmonyPrefix]
        static bool TimeSystemPatches_GetDay(uint frame, TimeData data, ref int __result, TimeSystem __instance)
        {
            __result = Time2WorkTimeSystem.GetDay(frame, data);
            return false;
        }

        [HarmonyPatch(typeof(TimeUISystem), "GetDay")]
        [HarmonyPrefix]
        static bool TimeUISystemPatches_GetDay(ref int __result, TimeUISystem __instance)
        {
            Time2WorkTimeUISystem t2wTimeUISystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Time2WorkTimeUISystem>();

            __result = t2wTimeUISystem.GetDay();
            return false;
        }

        [HarmonyPatch(typeof(TimeUISystem), "GetTicks")]
        [HarmonyPrefix]
        static bool TimeUISystemPatches_GetTicks(ref int __result, TimeUISystem __instance)
        {
            Time2WorkTimeUISystem t2wTimeUISystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Time2WorkTimeUISystem>();

            __result = t2wTimeUISystem.GetTicks();
            return false;
        }

        [HarmonyPatch(typeof(TimeSystem), "GetCurrentDateTime")]
        [HarmonyPrefix]
        static bool TimeSystemPatches_GetCurrentDateTime(ref DateTime __result, TimeSystem __instance)
        {
            Time2WorkTimeSystem t2wTimeSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Time2WorkTimeSystem>();

            __result = t2wTimeSystem.GetCurrentDateTime();
            return false;
        }

        [HarmonyPatch(typeof(TimeSystem), "GetStartingDate")]
        [HarmonyPrefix]
        static bool TimeSystemPatches_GetStartingDate(TimeSettingsData settings, TimeData data, ref float __result, TimeSystem __instance)
        {
            Time2WorkTimeSystem t2wTimeSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Time2WorkTimeSystem>();

            __result = t2wTimeSystem.GetStartingDate(settings, data);
            return false;
        }

        [HarmonyPatch(typeof(TimeSystem), "GetElapsedYears")]
        [HarmonyPrefix]
        static bool TimeSystemPatches_GetElapsedYears(TimeSettingsData settings, TimeData data, ref float __result, TimeSystem __instance)
        {
            Time2WorkTimeSystem t2wTimeSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Time2WorkTimeSystem>();

            __result = t2wTimeSystem.GetElapsedYears(settings, data);
            return false;
        }

        [HarmonyPatch(typeof(TimeSystem), "GetTimeOfYear", new Type[] { typeof(TimeSettingsData), typeof(TimeData), typeof(double) })]
        [HarmonyPrefix]
        static bool TimeSystemPatches_GetTimeOfYear(TimeSettingsData settings, TimeData data, double renderingFrame, ref float __result, TimeSystem __instance)
        {
            Time2WorkTimeSystem t2wTimeSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Time2WorkTimeSystem>();

            __result = t2wTimeSystem.GetTimeOfYear(settings, data, renderingFrame);
            return false;
        }

        [HarmonyPatch(typeof(TimeSystem), "GetTimeOfDay", new Type[] { typeof(TimeSettingsData), typeof(TimeData), typeof(double) })]
        [HarmonyPrefix]
        static bool TimeSystemPatches_GetTimeOfDay(TimeSettingsData settings, TimeData data, double renderingFrame, ref float __result, TimeSystem __instance)
        {
            Time2WorkTimeSystem t2wTimeSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Time2WorkTimeSystem>();

            __result = t2wTimeSystem.GetTimeOfDay(settings, data, renderingFrame);
            return false;
        }

        [HarmonyPatch(typeof(ClimateSystem), "SampleClimate", new Type[] { typeof(ClimatePrefab), typeof(float)})]
        [HarmonyPrefix]
        public static bool ClimateSystemPatches_SampleClimate_Prefix(ClimatePrefab prefab, float t, ref ClimateSample __result, ClimateSystem __instance)
        {
            float time = t * 12;
            float num1 = prefab.m_Temperature.Evaluate(time);
            float num2 = prefab.m_Precipitation.Evaluate(time);
            float num3 = prefab.m_Cloudiness.Evaluate(time);
            float num4 = prefab.m_Aurora.Evaluate(time);
            float num5 = prefab.m_Aurora.Evaluate(time);
            __result = new ClimateSystem.ClimateSample()
            {
                temperature = num1,
                precipitation = num2,
                cloudiness = num3,
                aurora = num4,
                fog = num5
            };
        
            return false;
        }

        [HarmonyPatch(typeof(CityServiceUpkeepSystem),
                  nameof(CityServiceUpkeepSystem.CalculateUpkeep))]
        public static class CityServiceUpkeepSystem_CalculateUpkeep_NightDiscount
        {
            [HarmonyPostfix]
            public static void Postfix(ref int __result)
            {
                // No setting or 0% reduction → do nothing
                int pct = math.clamp(Mod.m_Setting.service_expenses_night_reduction, 0, 100);
                if (pct <= 0)
                    return;

                // Only at night (23–06)
                var world = World.DefaultGameObjectInjectionWorld;
                if (world == null)
                    return;

                var timeSys = world.GetExistingSystemManaged<Time2WorkTimeSystem>();
                if (timeSys == null)
                    return;

                int hour = timeSys.GetCurrentDateTime().Hour;
                if (!(hour >= 23 || hour <= 6))
                    return;

                float factor = (100f - pct) / 100f;
                if (factor >= 0.9999f)
                    return;

                __result = (int)math.round(__result * factor);
            }
        }

        [HarmonyPatch(typeof(CityServiceBudgetSystem), "OnUpdate")]
        public static class CityServiceBudgetSystem_OnUpdate_NightDiscount
        {
            private static readonly FieldInfo s_ExpensesField =
                AccessTools.Field(typeof(CityServiceBudgetSystem), "m_Expenses");
            private static readonly FieldInfo s_ExpensesTempField =
                AccessTools.Field(typeof(CityServiceBudgetSystem), "m_ExpensesTemp");

            [HarmonyPostfix]
            public static void Postfix(CityServiceBudgetSystem __instance)
            {
                int pct = math.clamp(Mod.m_Setting.service_expenses_night_reduction, 0, 100);
                if (pct <= 0)
                    return;

                var world = World.DefaultGameObjectInjectionWorld;
                if (world == null)
                    return;

                var timeSys = world.GetExistingSystemManaged<Time2WorkTimeSystem>();
                if (timeSys == null)
                    return;

                int hour = timeSys.GetCurrentDateTime().Hour;
                if (!(hour >= 23 || hour <= 6))
                    return;

                float factor = (100f - pct) / 100f;
                if (factor >= 0.9999f)
                    return;

                // Grab the arrays via reflection
                var expenses = (NativeArray<int>)s_ExpensesField.GetValue(__instance);
                var expensesTemp = (NativeArray<int>)s_ExpensesTempField.GetValue(__instance);

                int idx = (int)ExpenseSource.ServiceUpkeep; // uses the enum from Game.Economy

                if (idx >= 0 && idx < expenses.Length)
                    expenses[idx] = (int)math.round(expenses[idx] * factor);

                if (idx >= 0 && idx < expensesTemp.Length)
                    expensesTemp[idx] = (int)math.round(expensesTemp[idx] * factor);
            }
        }
    }
}
