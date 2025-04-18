using Game;
using Game.Buildings;
using Game.Common;
using Game.Prefabs;
using Game.Tools;
using Game.Simulation;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Time2Work.Components;
using Colossal.Entities;
using static Game.Prefabs.TriggerPrefabData;
using System;

#nullable disable
namespace Time2Work.Systems
{
    //[CompilerGenerated]
    public partial class Time2WorkAttractionSystem : GameSystemBase
    {
        private SimulationSystem m_SimulationSystem;
        private Time2WorkTimeSystem m_TimeSystem;
        private TerrainAttractivenessSystem m_TerrainAttractivenessSystem;
        private TerrainSystem m_TerrainSystem;
        private EntityQuery m_BuildingGroup;
        private EntityQuery m_SettingsQuery;
        private Time2WorkAttractionSystem.TypeHandle __TypeHandle;
        EndFrameBarrier m_EndFrameBarrier;
        private Setting.DTSimulationEnum m_daytype;
        private uint m_SimulationFrame;
        private EntityQuery m_TimeDataQuery;
        private Unity.Mathematics.Random m_random;

        public override int GetUpdateInterval(SystemUpdatePhase phase) => 16;

        public static void SetFactor(
          NativeArray<int> factors,
          Time2WorkAttractionSystem.AttractivenessFactor factor,
          float attractiveness)
        {
            if (!factors.IsCreated || factors.Length != 5)
                return;
            factors[(int)factor] = Mathf.RoundToInt(attractiveness);
        }

        [UnityEngine.Scripting.Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();
            m_TimeDataQuery = this.GetEntityQuery(ComponentType.ReadOnly<Game.Common.TimeData>());
            m_EndFrameBarrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();
            this.m_SimulationSystem = this.World.GetOrCreateSystemManaged<SimulationSystem>();
            this.m_TimeSystem = this.World.GetOrCreateSystemManaged<Time2WorkTimeSystem>();
            this.m_TerrainAttractivenessSystem = this.World.GetOrCreateSystemManaged<TerrainAttractivenessSystem>();
            this.m_TerrainSystem = this.World.GetOrCreateSystemManaged<TerrainSystem>();
            this.m_SettingsQuery = this.GetEntityQuery(ComponentType.ReadOnly<AttractivenessParameterData>());
            this.m_BuildingGroup = this.GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[3]
              {
          ComponentType.ReadWrite<AttractivenessProvider>(),
          ComponentType.ReadOnly<PrefabRef>(),
          ComponentType.ReadOnly<UpdateFrame>()
              },
                None = new ComponentType[3]
              {
          ComponentType.ReadOnly<Destroyed>(),
          ComponentType.ReadOnly<Deleted>(),
          ComponentType.ReadOnly<Temp>()
              }
            });
        }

        [UnityEngine.Scripting.Preserve]
        protected override void OnUpdate()
        {
            uint frameWithInterval = SimulationUtils.GetUpdateFrameWithInterval(this.m_SimulationSystem.frameIndex, (uint)this.GetUpdateInterval(SystemUpdatePhase.GameSimulation), 16);
            this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_ParkData_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_AttractionData_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_Park_RO_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_Signature_RO_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_Efficiency_RO_BufferTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_AttractivenessProvider_RW_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            JobHandle dependencies;
         
            m_daytype = WeekSystem.currentDayOfTheWeek;

            Game.Common.TimeData m_TimeData = this.m_TimeDataQuery.GetSingleton<Game.Common.TimeData>();
            m_SimulationFrame = this.m_SimulationSystem.frameIndex;
            int day = Time2WorkTimeSystem.GetDay(this.m_SimulationFrame, m_TimeData);

            Time2WorkAttractionSystem.AttractivenessJob jobData = new Time2WorkAttractionSystem.AttractivenessJob()
            {
                ecb = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
                m_AttractivenessType = this.__TypeHandle.__Game_Buildings_AttractivenessProvider_RW_ComponentTypeHandle,
                m_PrefabType = this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle,
                m_EfficiencyType = this.__TypeHandle.__Game_Buildings_Efficiency_RO_BufferTypeHandle,
                m_SignatureType = this.__TypeHandle.__Game_Buildings_Signature_RO_ComponentTypeHandle,
                m_ParkType = this.__TypeHandle.__Game_Buildings_Park_RO_ComponentTypeHandle,
                m_InstalledUpgradeType = this.__TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle,
                m_TransformType = this.__TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle,
                m_UpdateFrameType = this.__TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle,
                m_AttractionDatas = this.__TypeHandle.__Game_Prefabs_AttractionData_RO_ComponentLookup,
                m_ParkDatas = this.__TypeHandle.__Game_Prefabs_ParkData_RO_ComponentLookup,
                m_PrefabRefData = this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup,
                m_SpecialEventData = this.__TypeHandle.__Game_Prefabs_SpecialEventData_RO_ComponentLookup,
                m_TerrainMap = this.m_TerrainAttractivenessSystem.GetData(true, out dependencies),
                m_HeightData = this.m_TerrainSystem.GetHeightData(),
                m_Parameters = this.m_SettingsQuery.GetSingleton<AttractivenessParameterData>(),
                m_UpdateFrameIndex = frameWithInterval,
                minAttraction = Mod.m_Setting.min_attraction,
                normalizedTime = this.m_TimeSystem.normalizedTime,
                day = day,
                dayOfWeek = WeekSystem.dayOfWeek,
                n = Mod.numCurrentEvents,
            };

            this.Dependency = jobData.ScheduleParallel<Time2WorkAttractionSystem.AttractivenessJob>(this.m_BuildingGroup, JobHandle.CombineDependencies(this.Dependency, dependencies));
            this.m_TerrainSystem.AddCPUHeightReader(this.Dependency);
            this.m_TerrainAttractivenessSystem.AddReader(this.Dependency);
            m_EndFrameBarrier.AddJobHandleForProducer(this.Dependency);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void __AssignQueries(ref SystemState state)
        {
        }
        protected override void OnCreateForCompiler()
        {
            base.OnCreateForCompiler();
            this.__AssignQueries(ref this.CheckedStateRef);
            this.__TypeHandle.__AssignHandles(ref this.CheckedStateRef);
        }

        [UnityEngine.Scripting.Preserve]
        public Time2WorkAttractionSystem()
        {
        }

        public enum AttractivenessFactor
        {
            Efficiency,
            Maintenance,
            Forest,
            Beach,
            Height,
            Count,
        }

        //[BurstCompile]
        private struct AttractivenessJob : IJobChunk
        {
            public EntityCommandBuffer.ParallelWriter ecb;
            public ComponentTypeHandle<AttractivenessProvider> m_AttractivenessType;
            [ReadOnly]
            public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;
            [ReadOnly]
            public ComponentTypeHandle<PrefabRef> m_PrefabType;
            [ReadOnly]
            public BufferTypeHandle<Efficiency> m_EfficiencyType;
            [ReadOnly]
            public ComponentTypeHandle<Signature> m_SignatureType;
            [ReadOnly]
            public ComponentTypeHandle<Game.Buildings.Park> m_ParkType;
            [ReadOnly]
            public BufferTypeHandle<InstalledUpgrade> m_InstalledUpgradeType;
            [ReadOnly]
            public ComponentTypeHandle<Game.Objects.Transform> m_TransformType;
            [ReadOnly]
            public ComponentLookup<AttractionData> m_AttractionDatas;
            [ReadOnly]
            public ComponentLookup<ParkData> m_ParkDatas;
            [ReadOnly]
            public ComponentLookup<PrefabRef> m_PrefabRefData;
            [ReadOnly]
            public ComponentLookup<SpecialEventData> m_SpecialEventData;
            [ReadOnly]
            public CellMapData<TerrainAttractiveness> m_TerrainMap;
            [ReadOnly]
            public TerrainHeightData m_HeightData;
            public AttractivenessParameterData m_Parameters;
            public uint m_UpdateFrameIndex;
            public Unity.Mathematics.Random random;
            public int n;
            public int minAttraction;
            public float normalizedTime;
            public int day;
            public System.DayOfWeek dayOfWeek;

            public void Execute(
              in ArchetypeChunk chunk,
              int unfilteredChunkIndex,
              bool useEnabledMask,
              in v128 chunkEnabledMask)
            {
                if ((int)chunk.GetSharedComponent<UpdateFrame>(this.m_UpdateFrameType).m_Index != (int)this.m_UpdateFrameIndex)
                    return;
                NativeArray<PrefabRef> nativeArray1 = chunk.GetNativeArray<PrefabRef>(ref this.m_PrefabType);
                NativeArray<AttractivenessProvider> nativeArray2 = chunk.GetNativeArray<AttractivenessProvider>(ref this.m_AttractivenessType);
                BufferAccessor<Efficiency> bufferAccessor1 = chunk.GetBufferAccessor<Efficiency>(ref this.m_EfficiencyType);
                NativeArray<Game.Buildings.Park> nativeArray3 = chunk.GetNativeArray<Game.Buildings.Park>(ref this.m_ParkType);
                NativeArray<Game.Objects.Transform> nativeArray4 = chunk.GetNativeArray<Game.Objects.Transform>(ref this.m_TransformType);
                BufferAccessor<InstalledUpgrade> bufferAccessor2 = chunk.GetBufferAccessor<InstalledUpgrade>(ref this.m_InstalledUpgradeType);

                bool flag = chunk.Has<Signature>(ref this.m_SignatureType);

                for (int index = 0; index < chunk.Count; ++index)
                {
                    Entity prefab = nativeArray1[index].m_Prefab;
                    AttractionData data = new AttractionData();
                    bool isPark = false;
                    if (this.m_AttractionDatas.HasComponent(prefab))
                    {
                        data = this.m_AttractionDatas[prefab];
                    }
                    if (bufferAccessor2.Length != 0)
                    {
                        UpgradeUtils.CombineStats<AttractionData>(ref data, bufferAccessor2[index], ref this.m_PrefabRefData, ref this.m_AttractionDatas);
                    }
                    float attractiveness = (float)data.m_Attractiveness;
                    if (!flag)
                        attractiveness *= BuildingUtils.GetEfficiency(bufferAccessor1, index);

                    if (chunk.Has<Game.Buildings.Park>(ref this.m_ParkType) && this.m_ParkDatas.HasComponent(prefab))
                    {
                        Game.Buildings.Park park = nativeArray3[index];
                        
                        ParkData parkData = this.m_ParkDatas[prefab];
                        float num = parkData.m_MaintenancePool > (short)0 ? (float)park.m_Maintenance / (float)parkData.m_MaintenancePool : 0.0f;
                        attractiveness *= (float)(0.800000011920929 + 0.20000000298023224 * (double)num);
                        isPark = true;
                    }

                    if (chunk.Has<Game.Objects.Transform>(ref this.m_TransformType))
                    {
                        float3 position = nativeArray4[index].m_Position;
                        attractiveness *= (float)(1.0 + 0.0099999997764825821 * (double)TerrainAttractivenessSystem.EvaluateAttractiveness(position, this.m_TerrainMap, this.m_HeightData, this.m_Parameters, new NativeArray<int>()));
                    }
                    if (attractiveness >= minAttraction && isPark)
                    {
                        SpecialEventData specialEventData;
                        if (!m_SpecialEventData.TryGetComponent(prefab, out specialEventData))
                        {
                            attractiveness *= 1000f;
                            specialEventData = new SpecialEventData();
                            specialEventData.new_attraction = Mathf.RoundToInt(attractiveness);

                            ecb.AddComponent(unfilteredChunkIndex, prefab, specialEventData);
                        } else
                        {
                            float start = specialEventData.start_time - 1f / 24f;
                            float end = specialEventData.start_time + 0.6f*specialEventData.duration;
                            if (normalizedTime >= start && normalizedTime <= end && specialEventData.day == day)  
                            {
                                if(normalizedTime <= specialEventData.start_time)
                                {
                                    attractiveness = specialEventData.new_attraction * (normalizedTime / specialEventData.start_time);
                                } else
                                {
                                    attractiveness = specialEventData.new_attraction * (specialEventData.start_time / normalizedTime);
                                }
                                //Mod.log.Info($"Loading Special Event - start: {start}, end:{end}, attractiveness:{attractiveness}");
                            }
                        }
                    }
                    AttractivenessProvider attractivenessProvider = new AttractivenessProvider()
                    {
                        m_Attractiveness = Mathf.RoundToInt(attractiveness)
                    };
                    nativeArray2[index] = attractivenessProvider;
                }
            }

            void IJobChunk.Execute(
              in ArchetypeChunk chunk,
              int unfilteredChunkIndex,
              bool useEnabledMask,
              in v128 chunkEnabledMask)
            {
                this.Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
            }
        }

        private struct TypeHandle
        {
            public ComponentTypeHandle<AttractivenessProvider> __Game_Buildings_AttractivenessProvider_RW_ComponentTypeHandle;
            [ReadOnly]
            public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;
            [ReadOnly]
            public BufferTypeHandle<Efficiency> __Game_Buildings_Efficiency_RO_BufferTypeHandle;
            [ReadOnly]
            public ComponentTypeHandle<Signature> __Game_Buildings_Signature_RO_ComponentTypeHandle;
            [ReadOnly]
            public ComponentTypeHandle<Game.Buildings.Park> __Game_Buildings_Park_RO_ComponentTypeHandle;
            [ReadOnly]
            public BufferTypeHandle<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle;
            [ReadOnly]
            public ComponentTypeHandle<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;
            public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;
            [ReadOnly]
            public ComponentLookup<AttractionData> __Game_Prefabs_AttractionData_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<ParkData> __Game_Prefabs_ParkData_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<SpecialEventData> __Game_Prefabs_SpecialEventData_RO_ComponentLookup;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void __AssignHandles(ref SystemState state)
            {
                this.__Game_Buildings_AttractivenessProvider_RW_ComponentTypeHandle = state.GetComponentTypeHandle<AttractivenessProvider>();
                this.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(true);
                this.__Game_Buildings_Efficiency_RO_BufferTypeHandle = state.GetBufferTypeHandle<Efficiency>(true);
                this.__Game_Buildings_Signature_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Signature>(true);
                this.__Game_Buildings_Park_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.Park>(true);
                this.__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle = state.GetBufferTypeHandle<InstalledUpgrade>(true);
                this.__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.Transform>(true);
                this.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
                this.__Game_Prefabs_AttractionData_RO_ComponentLookup = state.GetComponentLookup<AttractionData>(true);
                this.__Game_Prefabs_ParkData_RO_ComponentLookup = state.GetComponentLookup<ParkData>(true);
                this.__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(true);
                this.__Game_Prefabs_SpecialEventData_RO_ComponentLookup = state.GetComponentLookup<SpecialEventData>(true);
            }
        }
    }
}
