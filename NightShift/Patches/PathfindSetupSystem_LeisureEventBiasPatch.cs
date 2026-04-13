// PathfindSetupSystem_LeisureEventBiasPatch.cs
//
// Patch PathfindSetupSystem.FindTargets(SetupTargetType, in SetupData)
// and replace ONLY the Leisure branch with a Burst job that biases toward active SpecialEventData venues.
//
// Adds a Burst-safe counter for how many times the event-bias condition triggers,
// and logs it (managed side) via Mod.log.Info() at a throttled interval.
//
// Fixes the "Dependency is inaccessible" compile error by reading SystemBase.Dependency via reflection.
// Keeps the pathfind setup TempJob queue lifetime correct (because we return the JobHandle that FindTargets returns).

using Game;
using Game.Agents;
using Game.Buildings;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Pathfind;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using HarmonyLib;
using System;
using System.Reflection;
using Time2Work.Components;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Time2Work.Patches
{
    [HarmonyPatch]
    public static class PathfindSetupSystem_LeisureEventBiasPatch
    {
        // You said -500000 was what made it work. Keep this configurable here.
        // (You can keep your larger value if you prefer; just change the number.)
        private const float EVENT_COST_BONUS = -500000f;

        // --- Bias trigger counter (Burst-safe) ---
        // One int per worker thread; increment from Burst job via NativeThreadIndex.
        private static NativeArray<int> s_BiasCounts; // length = JobsUtility.MaxJobThreadCount

        // Throttle logging to avoid spamming
        private static long s_NextBiasLogTickMs;
        private const int LOG_EVERY_MS = 20000;

        // Reflection accessor for SystemBase.Dependency (protected)
        private static readonly MethodInfo MI_DependencyGetter =
            AccessTools.PropertyGetter(typeof(SystemBase), "Dependency");

        static MethodBase TargetMethod()
        {
            // private JobHandle FindTargets(SetupTargetType targetType, in PathfindSetupSystem.SetupData setupData)
            return AccessTools.Method(
                typeof(PathfindSetupSystem),
                "FindTargets",
                new[]
                {
                    typeof(SetupTargetType),
                    typeof(PathfindSetupSystem.SetupData).MakeByRefType()
                });
        }

        static bool Prefix(
            PathfindSetupSystem __instance,
            SetupTargetType targetType,
            ref PathfindSetupSystem.SetupData setupData, // treat "in" as ref
            ref JobHandle __result)
        {
            if (targetType != SetupTargetType.Leisure)
                return true; // run vanilla for everything else

            // Ensure counter storage exists
            if (!s_BiasCounts.IsCreated)
            {
                s_BiasCounts = new NativeArray<int>(JobsUtility.MaxJobThreadCount, Allocator.Persistent);
            }

            // Reset counters for this pass
            for (int i = 0; i < s_BiasCounts.Length; i++)
                s_BiasCounts[i] = 0;

            // Get the same dependency vanilla passes around: this.Dependency
            var dependsOnObj = MI_DependencyGetter.Invoke(__instance, null);
            JobHandle dependsOn = dependsOnObj is JobHandle h ? h : default;

            // ---- Same day logic as your project ----
            var simSystem = __instance.World.GetExistingSystemManaged<SimulationSystem>();
            var timeSystem = __instance.World.GetExistingSystemManaged<TimeSystem>();

            var timeData = __instance.EntityManager
                .CreateEntityQuery(ComponentType.ReadOnly<TimeData>())
                .GetSingleton<TimeData>();

            int day = Time2WorkTimeSystem.GetDay(simSystem.frameIndex, timeData, Time2WorkTimeSystem.kTicksPerDay);
            float normalizedTime = timeSystem.normalizedTime;

            // ---- Same query vanilla citizen pathfind setup uses for leisure providers ----
            EntityQuery leisureProviderQuery = __instance.GetSetupQuery(
                ComponentType.ReadOnly<Game.Buildings.LeisureProvider>(),
                ComponentType.Exclude<Temp>(),
                ComponentType.Exclude<Deleted>(),
                ComponentType.Exclude<Destroyed>());

            // ---- Handles/lookups (RO) ----
            var entityType = __instance.GetEntityTypeHandle();
            var serviceAvailableType = __instance.GetComponentTypeHandle<ServiceAvailable>(true);
            var prefabRefType = __instance.GetComponentTypeHandle<PrefabRef>(true);

            var leisureProviderDatas = __instance.GetComponentLookup<LeisureProviderData>(true);
            var resources = __instance.GetBufferLookup<Game.Economy.Resources>(true);
            var industrialProcessDatas = __instance.GetComponentLookup<IndustrialProcessData>(true);
            var serviceDatas = __instance.GetComponentLookup<ServiceCompanyData>(true);
            var buildingDatas = __instance.GetComponentLookup<Building>(true);

            // Your SpecialEventSystem adds SpecialEventData to BUILDING entities (confirmed)
            var specialEvents = __instance.GetComponentLookup<SpecialEventData>(true);

            var resourceSystem = __instance.World.GetExistingSystemManaged<ResourceSystem>();
            var resourcePrefabs = resourceSystem.GetPrefabs();

            var leisureSystem = __instance.World.GetExistingSystemManaged<Game.Simulation.LeisureSystem>();
            int leisureUpdateInterval = leisureSystem.GetUpdateInterval(SystemUpdatePhase.GameSimulation);

            // ---- Schedule our Burst job and RETURN it ----
            JobHandle handle = new SetupLeisureTargetJob_Biased
            {
                m_EntityType = entityType,
                m_ServiceAvailableType = serviceAvailableType,
                m_PrefabType = prefabRefType,

                m_LeisureProviderDatas = leisureProviderDatas,
                m_Resources = resources,
                m_IndustrialProcessDatas = industrialProcessDatas,
                m_ServiceDatas = serviceDatas,
                m_BuildingDatas = buildingDatas,

                m_ResourcePrefabs = resourcePrefabs,
                m_SetupData = setupData,
                m_LeisureSystemUpdateInterval = leisureUpdateInterval,

                m_SpecialEvents = specialEvents,
                m_Today = day,
                m_NormalizedTime = normalizedTime,
                m_EventCostBonus = EVENT_COST_BONUS,

                // counter
                m_BiasCounts = s_BiasCounts
            }.ScheduleParallel(leisureProviderQuery, dependsOn);

            resourceSystem.AddPrefabsReader(handle);

            // ---- Throttled managed-side log of how many times bias triggered ----
            // NOTE: This calls Complete() only every ~2 seconds, for debugging.
            // Remove this block once you’ve confirmed it triggers as expected.
            long nowMs = Environment.TickCount;
            //if (nowMs >= s_NextBiasLogTickMs)
            //{
            //    // Complete so it is safe to read s_BiasCounts on the main thread
            //    handle.Complete();
            //
            //    int total = 0;
            //    for (int i = 0; i < s_BiasCounts.Length; i++)
            //        total += s_BiasCounts[i];
            //
            //    Mod.log.Info($"[Time2Work] Leisure event bias triggered {total} times (SetupData.Length={setupData.Length}, day={day}, t={normalizedTime:0.000})");
            //
            //    s_NextBiasLogTickMs = nowMs + LOG_EVERY_MS;
            //
            //    // Since we completed it for logging, return a completed handle
            //    __result = handle;
            //    return false;
            //}

            __result = handle;
            return false; // skip vanilla leisure branch
        }

        [BurstCompile]
        private struct SetupLeisureTargetJob_Biased : IJobChunk
        {
            [ReadOnly] public EntityTypeHandle m_EntityType;
            [ReadOnly] public ComponentTypeHandle<ServiceAvailable> m_ServiceAvailableType;
            [ReadOnly] public ComponentTypeHandle<PrefabRef> m_PrefabType;

            [ReadOnly] public ComponentLookup<LeisureProviderData> m_LeisureProviderDatas;
            [ReadOnly] public BufferLookup<Game.Economy.Resources> m_Resources;
            [ReadOnly] public ComponentLookup<IndustrialProcessData> m_IndustrialProcessDatas;
            [ReadOnly] public ComponentLookup<ServiceCompanyData> m_ServiceDatas;
            [ReadOnly] public ComponentLookup<Building> m_BuildingDatas;

            [ReadOnly] public ResourcePrefabs m_ResourcePrefabs;

            public PathfindSetupSystem.SetupData m_SetupData;
            public int m_LeisureSystemUpdateInterval;

            [ReadOnly] public ComponentLookup<SpecialEventData> m_SpecialEvents;
            public int m_Today;
            public float m_NormalizedTime;
            public float m_EventCostBonus;

            // --- counter plumbing ---
            [NativeSetThreadIndex] public int NativeThreadIndex;

            [NativeDisableParallelForRestriction]
            public NativeArray<int> m_BiasCounts;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(m_EntityType);
                var serviceAvail = chunk.GetNativeArray(ref m_ServiceAvailableType);
                var prefabRefs = chunk.GetNativeArray(ref m_PrefabType);

                for (int si = 0; si < m_SetupData.Length; ++si)
                {
                    m_SetupData.GetItem(si, out Entity _, out PathfindTargetSeeker<PathfindSetupBuffer> targetSeeker);

                    LeisureType requestedType = (LeisureType)targetSeeker.m_SetupQueueTarget.m_Value;
                    float requiredService = targetSeeker.m_SetupQueueTarget.m_Value2;

                    for (int i = 0; i < entities.Length; ++i)
                    {
                        Entity buildingEntity = entities[i];

                        // Vanilla: skip inactive buildings
                        if (m_BuildingDatas.HasComponent(buildingEntity) &&
                            BuildingUtils.CheckOption(m_BuildingDatas[buildingEntity], BuildingOption.Inactive))
                            continue;

                        Entity prefab = prefabRefs[i].m_Prefab;
                        if (!m_LeisureProviderDatas.HasComponent(prefab))
                            continue;

                        LeisureProviderData lpd = m_LeisureProviderDatas[prefab];
                        if (lpd.m_LeisureType != requestedType)
                            continue;

                        float cost = 0f;

                        // Vanilla penalty for Meals/Commercial
                        if ((requestedType == LeisureType.Commercial || requestedType == LeisureType.Meals)
                            && serviceAvail.Length > 0
                            && m_ServiceDatas.HasComponent(prefab))
                        {
                            int avail = serviceAvail[i].m_ServiceAvailable;
                            if (avail < requiredService)
                                continue;

                            if (m_IndustrialProcessDatas.HasComponent(prefab))
                            {
                                var ipd = m_IndustrialProcessDatas[prefab];
                                if (ipd.m_Output.m_Resource != Resource.NoResource)
                                {
                                    int maxService = m_ServiceDatas[prefab].m_MaxService;
                                    int haveRes = EconomyUtils.GetResources(ipd.m_Output.m_Resource, m_Resources[buildingEntity]);

                                    float ratio = 1f * (float)math.min(avail, haveRes) / (float)maxService;
                                    cost = (float)(1000.0 * (1.0 - (double)math.saturate(ratio) * 2.0));
                                }
                            }
                        }

                        // Event bias (SpecialEventData is on BUILDING entity in your mod)
                        if (m_SpecialEvents.HasComponent(buildingEntity))
                        {
                            var sed = m_SpecialEvents[buildingEntity];
                            if (IsActiveEvent(in sed, requestedType, m_Today, m_NormalizedTime))
                            {
                                cost += m_EventCostBonus;

                                // Count trigger (Burst-safe: per-thread counter)
                                m_BiasCounts[NativeThreadIndex] = m_BiasCounts[NativeThreadIndex] + 1;
                            }
                        }

                        targetSeeker.FindTargets(buildingEntity, cost);
                    }
                }
            }

            private static bool IsActiveEvent(in SpecialEventData sed, LeisureType requestedType, int today, float t)
            {
                if (sed.day != today) return false;
                //if (sed.leisureType != requestedType) return false;

                float start = sed.start_time - 2f / 24f;

                float end = sed.start_time + sed.duration * 0.5f;

                if (end <= 1f)
                    return t >= start && t <= end;

                end = math.frac(end);
                return (t >= start) || (t <= end);
            }
        }
    }
}
