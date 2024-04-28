using Game.Citizens;
using Game.Common;
using Game.Companies;
using Game.Prefabs;
using Game.Simulation;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Burst.Intrinsics;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using static Game.Rendering.Debug.RenderPrefabRenderer;

namespace Time2Work
{

    [HarmonyPatch]
    public class Time2WorkPatches
    {
        //[HarmonyPatch(typeof(Game.Simulation.LeisureSystem), "Execute")]
        //[HarmonyPrefix]
        //public static bool LeisureSystem_Execute_Prefix(in ArchetypeChunk chunk,
        //int unfilteredChunkIndex,
        //bool useEnabledMask,
        //in v128 chunkEnabledMask)
        //{
        //    Mod.log.Info($"Execute Pre");
        //    return true;
        //}
        //
        //[HarmonyPatch(typeof(Game.Simulation.LeisureSystem), "Execute")]
        //[HarmonyPostfix]
        //public static void LeisureSystem_Execute_Postfix(in ArchetypeChunk chunk,
        //int unfilteredChunkIndex,
        //bool useEnabledMask,
        //in v128 chunkEnabledMask)
        //{
        //    Mod.log.Info($"Execute Post"); 
        //}
        
        //[HarmonyPatch(typeof(Game.Simulation.StudentSystem), "OnUpdate")]
        //[HarmonyPrefix]
        //public static bool WorkerSystemPatches_OnUpdate_Prefix(StudentSystem __instance)
        //{
        //    Traverse.Create(__instance).Field("m_Time2WorkCitizenBehaviorSystem").SetValue(World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Time2WorkCitizenBehaviorSystem>());
        //    return true;
        //}
        
        //[HarmonyPatch(typeof(Game.Simulation.StudentSystem), "OnUpdate")]
        //[HarmonyPostfix]
        //public static void WorkerSystemPatches_OnUpdate_Postfix(StudentSystem __instance)
        //{
        //    Mod.log.Info($"OnUpdate Post");
        //}

        //static public int kTicksPerDay = (int) (Mod.m_Setting.day_factor) * 262144;
        //
        //[HarmonyPatch(typeof(Game.Simulation.TimeSystem), "GetTicks", new Type[] { typeof(uint), typeof(TimeSettingsData), typeof(TimeData) })]
        //[HarmonyPrefix]
        //static bool Time2WorkTimeSystem_GetTicks(uint frameIndex, TimeSettingsData settings, TimeData data, ref int __result, TimeSystem __instance)
        //{
        //    __result = (int)frameIndex - (int)data.m_FirstFrame + Mathf.RoundToInt(data.TimeOffset * kTicksPerDay) + Mathf.RoundToInt(data.GetDateOffset(settings.m_DaysPerYear) * kTicksPerDay * (float)settings.m_DaysPerYear);
        //    return false;
        //}
        //
        //[HarmonyPatch(typeof(Game.Simulation.TimeSystem), "GetTicks", new Type[] { typeof(TimeSettingsData), typeof(TimeData) })]
        //[HarmonyPrefix]
        //static bool Time2WorkTimeSystem_GetTicks(TimeSettingsData settings, TimeData data, ref int __result, TimeSystem __instance)
        //{
        //    SimulationSystem simsys = Traverse.Create(__instance).Field("m_SimulationSystem").GetValue() as SimulationSystem;
        //
        //    __result = (int)simsys.frameIndex - (int)data.m_FirstFrame + Mathf.RoundToInt(data.TimeOffset * kTicksPerDay) + Mathf.RoundToInt(data.GetDateOffset(settings.m_DaysPerYear) * kTicksPerDay * (float)settings.m_DaysPerYear);
        //    return false;
        //}
        //
        //[HarmonyPatch(typeof(Game.Simulation.TimeSystem), "GetTimeWithOffset")]
        //[HarmonyPrefix]
        //static bool Time2WorkTimeSystem_GetTimeWithOffset(
        //  TimeSettingsData settings,
        //  TimeData data,
        //  double renderingFrame, ref double __result, TimeSystem __instance)
        //{
        //    __result = renderingFrame + (double)data.TimeOffset * kTicksPerDay + (double)data.GetDateOffset(settings.m_DaysPerYear) * kTicksPerDay * (double)settings.m_DaysPerYear;
        //    return false;
        //}
        //
        //[HarmonyPatch(typeof(Game.Simulation.TimeSystem), "GetTimeOfDay", new Type[] { typeof(TimeSettingsData), typeof(TimeData), typeof(double) })]
        //[HarmonyPrefix]
        //static bool Time2WorkTimeSystem_GetTimeOfDay(TimeSettingsData settings, TimeData data, double renderingFrame, ref float __result, TimeSystem __instance)
        //{
        //    double offset = 0;
        //    Time2WorkTimeSystem_GetTimeWithOffset(settings, data, renderingFrame, ref offset, __instance);
        //    __result = (float)(offset % kTicksPerDay / kTicksPerDay);
        //    return false;
        //}
        //
        //[HarmonyPatch(typeof(Game.Simulation.TimeSystem), "GetTimeOfDay", new Type[] { typeof(TimeSettingsData), typeof(TimeData) })]
        //[HarmonyPrefix]
        //static bool Time2WorkTimeSystem_GetTimeOfDay(TimeSettingsData settings, TimeData data, ref float __result, TimeSystem __instance)
        //{
        //    int ticks = 0;
        //    Time2WorkTimeSystem_GetTicks(settings, data, ref ticks, __instance);
        //    __result = (float)(ticks % kTicksPerDay) / kTicksPerDay;
        //    return false;
        //}
        //
        //[HarmonyPatch(typeof(Game.Simulation.TimeSystem), "GetTimeOfYear", new Type[] { typeof(TimeSettingsData), typeof(TimeData), typeof(double) })]
        //[HarmonyPrefix]
        //static bool Time2WorkTimeSystem_GetTimeOfYear(TimeSettingsData settings, TimeData data, double renderingFrame, ref float __result, TimeSystem __instance)
        //{
        //    int num = kTicksPerDay * settings.m_DaysPerYear;
        //    double timeoffset = 0;
        //    Time2WorkTimeSystem_GetTimeWithOffset(settings, data, renderingFrame % (double)num, ref timeoffset, __instance);
        //    __result = ((float)timeoffset) / (float)num;
        //    return false;
        //}
        //
        //[HarmonyPatch(typeof(Game.Simulation.TimeSystem), "GetTimeOfYear", new Type[] { typeof(TimeSettingsData), typeof(TimeData) })]
        //[HarmonyPrefix]
        //static bool Time2WorkTimeSystem_GetTimeOfYear(TimeSettingsData settings, TimeData data, ref float __result, TimeSystem __instance)
        //{
        //    int num = kTicksPerDay * settings.m_DaysPerYear;
        //    int ticks = 0;
        //    Time2WorkTimeSystem_GetTicks(settings, data, ref ticks, __instance);
        //    __result = (float)(ticks % num) / (float)num;
        //    return false;
        //}
        //
        //[HarmonyPatch(typeof(Game.Simulation.TimeSystem), "GetElapsedYears")]
        //[HarmonyPrefix]
        //static bool Time2WorkTimeSystem_GetElapsedYears(TimeSettingsData settings, TimeData data, ref float __result, TimeSystem __instance)
        //{
        //    int num = kTicksPerDay * settings.m_DaysPerYear;
        //    SimulationSystem simsys = Traverse.Create(__instance).Field("m_SimulationSystem").GetValue() as SimulationSystem;
        //
        //    __result = (float)(simsys.frameIndex - data.m_FirstFrame) / (float)num;
        //    return false;
        //}
        //
        //[HarmonyPatch(typeof(Game.Simulation.TimeSystem), "GetStartingDate")]
        //[HarmonyPrefix]
        //static bool Time2WorkTimeSystem_GetStartingDate(TimeSettingsData settings, TimeData data, ref float __result, TimeSystem __instance)
        //{
        //    int num = kTicksPerDay * settings.m_DaysPerYear;
        //    int ticks = 0;
        //    Time2WorkTimeSystem_GetTicks(settings, data, ref ticks, __instance);
        //    __result = (float)(ticks % num) / (float)num;
        //    return false;
        //}
        //
        //[HarmonyPatch(typeof(Game.Simulation.TimeSystem), "GetYear")]
        //[HarmonyPrefix]
        //static bool Time2WorkTimeSystem_GetYear(TimeSettingsData settings, TimeData data, ref int __result, TimeSystem __instance)
        //{
        //    int num = kTicksPerDay * settings.m_DaysPerYear;
        //    int ticks = 0;
        //    Time2WorkTimeSystem_GetTicks(settings, data, ref ticks, __instance);
        //    __result = data.m_StartingYear + Mathf.FloorToInt((float)(ticks / num));
        //    return false;
        //}
    }
}
