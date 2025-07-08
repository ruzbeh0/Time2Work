using Game;
using Game.Agents;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Events;
using Game.Pathfind;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Game.Vehicles;
using Mono.Cecil;
using System;
using System.Data;
using System.Runtime.CompilerServices;
using Time2Work.Components;
using Time2Work.Systems;
using Time2Work.Utils;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.PlayerLoop;
using static Game.UI.MapMetadataSystem;
using static Time2Work.Setting;
using static Time2Work.Time2WorkWorkerSystem;
using Student = Game.Citizens.Student;

namespace Time2Work.Systems
{
    [UpdateAfter(typeof(WeekSystem))]
    public partial class TruckScheduleSystem : GameSystemBase
    {
        private EntityQuery m_NewTruckScheduleQuery;
        private EntityQuery m_TimeDataQuery;
        private EntityQuery m_EconomyParameterQuery;
        private SimulationSystem m_SimulationSystem;
        private Time2WorkTimeSystem m_TimeSystem;
        private EndFrameBarrier m_EndFrameBarrier;
        private TruckScheduleSystem.TypeHandle __TypeHandle;
        private Setting.DTSimulationEnum m_daytype;
        private int lastUpdatedDay = -1;


        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            // One day (or month) in-game is '262144' ticks
            return 262144 / 512;
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            this.m_SimulationSystem = this.World.GetOrCreateSystemManaged<SimulationSystem>();
            this.m_TimeSystem = this.World.GetOrCreateSystemManaged<Time2WorkTimeSystem>();
            this.m_EndFrameBarrier = this.World.GetOrCreateSystemManaged<EndFrameBarrier>();
            this.m_EconomyParameterQuery = this.GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
            this.m_NewTruckScheduleQuery = this.GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[4]
             {
                   ComponentType.ReadOnly<TripNeeded>(),
                   ComponentType.ReadOnly<PrefabRef>(),
                   ComponentType.ReadOnly<Game.Economy.Resources>(),
                   ComponentType.ReadOnly<OwnedVehicle>()
             },
                None = new ComponentType[3]
             {
                ComponentType.Exclude<Deleted>(),
                ComponentType.Exclude<TruckSchedule>(),
                ComponentType.Exclude<Temp>(),
             }
            });
            this.m_TimeDataQuery = this.GetEntityQuery(ComponentType.ReadOnly<Game.Common.TimeData>());
            this.RequireForUpdate( m_NewTruckScheduleQuery);
            this.m_daytype = WeekSystem.currentDayOfTheWeek;

            Mod.log.Info("TruckScheduleSystem Created");
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            if (!WeekSystem.initialized)
            {
                return; // Skip update until WeekSystem has run
            }


            DateTime currentDateTime = m_TimeSystem.GetCurrentDateTime();
            int day = currentDateTime.Day;

            //Mod.log.Info("CitizenScheduleSystem OnUpdate");
            uint frameWithInterval = SimulationUtils.GetUpdateFrameWithInterval(this.m_SimulationSystem.frameIndex, (uint)this.GetUpdateInterval(SystemUpdatePhase.GameSimulation), 16);
            this.__TypeHandle.__Game_TripNeeded_RO_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_ResourceBuyer_RW_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_TruckSchedule_RW_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.TruckScheduleLookup.Update(ref this.CheckedStateRef);
            this.m_daytype = WeekSystem.currentDayOfTheWeek;

            JobHandle jobHandle;

            TruckScheduleSystem.NewTruckScheduleJob jobDataNew = new TruckScheduleSystem.NewTruckScheduleJob()
            {
                m_TruckSchedule = this.__TypeHandle.__Game_TruckSchedule_RW_ComponentTypeHandle,
                m_EntityType = this.__TypeHandle.__Unity_Entities_Entity_TypeHandle,
                m_TripNeeded = this.__TypeHandle.__Game_TripNeeded_RO_ComponentTypeHandle,
                m_CommandBuffer = this.m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
                m_UpdateFrameIndex = frameWithInterval,
                m_SimulationFrame = this.m_SimulationSystem.frameIndex,
                m_NormalizedTime = this.m_TimeSystem.normalizedTime,
                m_TimeData = this.m_TimeDataQuery.GetSingleton<Game.Common.TimeData>(),
                dow = this.m_daytype,
                m_RandomSeed = RandomSeed.Next()
            };
            jobHandle = jobDataNew.ScheduleParallel<TruckScheduleSystem.NewTruckScheduleJob>(this.m_NewTruckScheduleQuery, this.Dependency);
            this.Dependency = jobHandle;

            this.m_EndFrameBarrier.AddJobHandleForProducer(this.Dependency);
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
        public TruckScheduleSystem()
        {
        }

        [BurstCompile]
        private struct NewTruckScheduleJob : IJobChunk
        {
            [ReadOnly]
            public EntityTypeHandle m_EntityType;
            public ComponentTypeHandle<TruckSchedule> m_TruckSchedule;
            [ReadOnly]
            public BufferTypeHandle<TripNeeded> m_TripNeeded;
            public uint m_UpdateFrameIndex;
            public float m_NormalizedTime;
            public uint m_SimulationFrame;
            public EntityCommandBuffer.ParallelWriter m_CommandBuffer;
            public Game.Common.TimeData m_TimeData;
            public Setting.DTSimulationEnum dow;
            [ReadOnly]
            public RandomSeed m_RandomSeed;


            public void Execute(
              in ArchetypeChunk chunk,
              int unfilteredChunkIndex,
              bool useEnabledMask,
              in v128 chunkEnabledMask)
            {
                NativeArray<Entity> nativeArray1 = chunk.GetNativeArray(this.m_EntityType);
                BufferAccessor<TripNeeded> bufferAccessor = chunk.GetBufferAccessor<TripNeeded>(ref this.m_TripNeeded);
                Unity.Mathematics.Random random = this.m_RandomSeed.GetRandom(unfilteredChunkIndex);

                for (int index = 0; index < nativeArray1.Length; ++index)
                {
                    Entity entity1 = nativeArray1[index];
                    DynamicBuffer<TripNeeded> trips = bufferAccessor[index];
                    if (trips.Length > 0)
                    {
                        TripNeeded trip = trips[0];

                        double mean, stdDev; 

                        switch (trip.m_Purpose)
                        {
                            case Purpose.Shopping:
                                mean = 10.0; stdDev = 2.0; break;
                            case Purpose.Exporting:
                                mean = 8.0; stdDev = 1.5; break;
                            case Purpose.StorageTransfer:
                                mean = 7.5; stdDev = 1.0; break;
                            case Purpose.Delivery:
                                mean = 9.0; stdDev = 2.0; break;
                            case Purpose.UpkeepDelivery:
                                mean = 6.5; stdDev = 1.5; break;
                            case Purpose.Collect:
                                mean = 11.0; stdDev = 1.5; break;
                            case Purpose.ReturnUnsortedMail:
                                mean = 13.0; stdDev = 1.0; break;
                            case Purpose.ReturnLocalMail:
                                mean = 12.0; stdDev = 1.0; break;
                            case Purpose.ReturnOutgoingMail:
                                mean = 15.0; stdDev = 1.0; break;
                            case Purpose.ReturnGarbage:
                                mean = 6.0; stdDev = 1.5; break;
                            case Purpose.CompanyShopping:
                                mean = 7.5; stdDev = 1.5; break;
                            default:
                                mean = 9.0; stdDev = 2.0; break;
                        }
                        switch ((int)dow)
                        {
                            case 0:
                                mean += 0.5; // shift slightly later
                                stdDev += 0.5;
                                break;
                            case 2:
                                mean += 1.0;
                                stdDev += 0.5;
                                break;
                            case 3:
                                mean += 1.5;
                                stdDev += 1.0;
                                break;
                        }

                        double startHour = math.clamp(mean + GaussianRandom.NextGaussianDouble(random) * stdDev, 4.0, 20.0);
                        double duration = math.clamp(GaussianRandom.NextGaussianDouble(random) * 2.0 + 10.0, 8.0, 12.0); // duration between 8–12h
                        double endHour = math.clamp(startHour + duration, startHour + 1.0, 22.0);

                        float normalizedStart = (float)(startHour / 24.0);
                        float normalizedEnd = (float)(endHour / 24.0);

                        var schedule = TruckSchedule.CreateDefault();
                        schedule.startTime = normalizedStart;
                        schedule.endTime = normalizedEnd;

                        m_CommandBuffer.AddComponent<TruckSchedule>(unfilteredChunkIndex, entity1, schedule);
                    }
                }
            }

            void IJobChunk.Execute(
              in ArchetypeChunk chunk,
              int unfilteredChunkIndex,
              bool useEnabledMask,
              in v128 chunkEnabledMask)
            {
                // ISSUE: reference to a compiler-generated method
                this.Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
            }
        }


        private struct TypeHandle
        {
            public ComponentTypeHandle<ResourceBuyer> __Game_ResourceBuyer_RW_ComponentTypeHandle;
            public ComponentTypeHandle<TruckSchedule> __Game_TruckSchedule_RW_ComponentTypeHandle;
            [ReadOnly]
            public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;
            public BufferTypeHandle<TripNeeded> __Game_TripNeeded_RO_ComponentTypeHandle;
            public ComponentLookup<TruckSchedule> TruckScheduleLookup;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void __AssignHandles(ref SystemState state)
            {
                this.__Game_ResourceBuyer_RW_ComponentTypeHandle = state.GetComponentTypeHandle<ResourceBuyer>();
                this.__Game_TruckSchedule_RW_ComponentTypeHandle = state.GetComponentTypeHandle<TruckSchedule>();
                this.__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
                this.__Game_TripNeeded_RO_ComponentTypeHandle = state.GetBufferTypeHandle<TripNeeded>();
                this.TruckScheduleLookup = state.GetComponentLookup<TruckSchedule>(false);
            }
        }
    }
}
