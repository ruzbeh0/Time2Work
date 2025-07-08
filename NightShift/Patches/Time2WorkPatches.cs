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
using System.Text;
using System.Threading.Tasks;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
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
        [HarmonyPatch(typeof(CityServiceBudgetSystem), "GetExpense", new Type[] { typeof(ExpenseSource), typeof(NativeArray<int>)})]
        [HarmonyPostfix]
        public static void CityServiceBudgetSystemPatches_GetExpense_Postfix(ExpenseSource source, NativeArray<int> expenses, ref int __result)
        {
            DateTime currentDateTime = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Time2WorkTimeSystem>().GetCurrentDateTime();
            int hour = currentDateTime.Hour;
            if(hour >= 23 || hour <= 6)
            {
                float r = (float)__result;
                __result = (int)(r * ((float)(100 - Mod.m_Setting.service_expenses_night_reduction)/100f));
            }           
        }

        [HarmonyPatch(typeof(CityServiceBudgetSystem), "GetTotalExpenses", new Type[] { typeof(NativeArray<int>) })]
        [HarmonyPostfix]
        public static void CityServiceBudgetSystemPatches_GetTotalExpenses_Postfix(NativeArray<int> expenses, ref int __result)
        {
            DateTime currentDateTime = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Time2WorkTimeSystem>().GetCurrentDateTime();
            int hour = currentDateTime.Hour;
            if (hour >= 23 || hour <= 6)
            {
                float r = (float)__result;
                __result = (int)(r * ((float)(100 - Mod.m_Setting.service_expenses_night_reduction) / 100f));
            }
        }


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

        [HarmonyPatch(typeof(TimeSystem), "GetYear")]
        [HarmonyPrefix]
        static bool TimeSystemPatches_GetYear(TimeSettingsData settings, TimeData data, ref int __result, TimeSystem __instance)
        {
            Time2WorkTimeSystem t2wTimeSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Time2WorkTimeSystem>();

            __result = t2wTimeSystem.GetYear(settings, data);
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
    }
}
