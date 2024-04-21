using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Prefabs;
using Game.Tools;
using Game.Triggers;
using Game.Simulation;
using Game;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using System;
namespace Time2Work
{
    public partial class Time2WorkWorkerSystem : GameSystemBase
    {
        private EndFrameBarrier m_EndFrameBarrier;
        private TimeSystem m_TimeSystem;
        private Time2WorkCitizenBehaviorSystem m_Time2WorkCitizenBehaviorSystem;
        private EntityQuery m_EconomyParameterQuery;
        private EntityQuery m_GotoWorkQuery;
        private EntityQuery m_WorkerQuery;
        private EntityQuery m_TimeDataQuery;
        private EntityQuery m_PopulationQuery;
        private SimulationSystem m_SimulationSystem;
        private TriggerSystem m_TriggerSystem;
        private Time2WorkWorkerSystem.TypeHandle __TypeHandle;
        private static double delayFactor = (float)(Mod.m_Setting.delay_factor) / 100;

        public override int GetUpdateInterval(SystemUpdatePhase phase) => 16;

        //public static float GetWorkOffset(Citizen citizen)
        //{
        //    return (float)(citizen.GetPseudoRandom(CitizenPseudoRandom.WorkOffset).NextInt(21845) - 10922) / 262144f;
        //}

        public static bool IsTodayOffDay(
          Citizen citizen,
          ref EconomyParameterData economyParameters,
          uint frame,
          TimeData timeData,
          int population)
        {
            return WorkerSystem.IsTodayOffDay(citizen, ref economyParameters, frame, timeData, population);
        }

        public static bool IsTimeToWork(
          Citizen citizen,
          Worker worker,
          ref EconomyParameterData economyParameters,
          float timeOfDay)
        {
            float2 timeToWork = Time2WorkWorkerSystem.GetTimeToWork(citizen, worker, ref economyParameters, true);
            return (double)timeToWork.x >= (double)timeToWork.y ? (double)timeOfDay >= (double)timeToWork.x || (double)timeOfDay <= (double)timeToWork.y : (double)timeOfDay >= (double)timeToWork.x && (double)timeOfDay <= (double)timeToWork.y;
        }

        public static bool IsTodayLunchBreak(Citizen citizen)
        {
            int num = 100 - Mod.m_Setting.lunch_break_percentage;
            if (Unity.Mathematics.Random.CreateFromIndex((uint)(citizen.m_PseudoRandom)).NextInt(100) > num)
            {
                return true;
            }

            return false;
        }

        public static bool IsLunchTime(
          Citizen citizen,
          Worker worker,
          ref EconomyParameterData economyParameters,
          float timeOfDay)
        {
            if(!Time2WorkWorkerSystem.IsTodayLunchBreak(citizen))
            {
                return false;
            }
            float2 timeToLunch = Time2WorkWorkerSystem.GetLunchTime(citizen, worker, ref economyParameters);
            if(timeToLunch.x < 0)
            {
                return false;
            }
            else
            {
                return (double)timeToLunch.x >= (double)timeToLunch.y ? (double)timeOfDay >= (double)timeToLunch.x || (double)timeOfDay <= (double)timeToLunch.y : (double)timeOfDay >= (double)timeToLunch.x && (double)timeOfDay <= (double)timeToLunch.y;
            }          
        }

        public static float2 GetLunchTime(
        Citizen citizen,
          Worker worker,
          ref EconomyParameterData economyParameters)
        {
            float lunch_median = 0.5f;
            float lunch_duration = 0.05f;
            Unity.Mathematics.Random random = citizen.GetPseudoRandom(CitizenPseudoRandom.WorkOffset);
            double startOnTime = GaussianRandom.NextGaussianDouble(random)*0.02;
            double endOnTime = GaussianRandom.NextGaussianDouble(random)*0.02;

            if (worker.m_Shift == Workshift.Day)
            {
                return new float2((float)(lunch_median + startOnTime), (float)(lunch_median + lunch_duration + endOnTime));
            }
            else
            {
                return new float2(-1, -1);
            }
        }

        public static float2 GetTimeToWork(
        Citizen citizen,
          Worker worker,
          ref EconomyParameterData economyParameters,
          bool includeCommute)
        {
            Unity.Mathematics.Random random = citizen.GetPseudoRandom(CitizenPseudoRandom.WorkOffset);
            double startOnTime = GaussianRandom.NextGaussianDouble(random) * delayFactor;
            double endOnTime = (GaussianRandom.NextGaussianDouble(random) + 0.1) * delayFactor;
            if (endOnTime > 0)
            {
                endOnTime *= 2f;
            }

            float workOffset = WorkerSystem.GetWorkOffset(citizen);
            double lateShiftOffset = GaussianRandom.NextGaussianDouble(random);
            if (worker.m_Shift == Workshift.Day)
            {
                workOffset *= 0.5f;
                if(Time2WorkWorkerSystem.IsTodayLunchBreak(citizen))
                {
                    endOnTime += random.NextFloat(0f, 0.5f);
                }
            }   
            else if (worker.m_Shift == Workshift.Evening)
            {
                workOffset *= 2f;
                startOnTime *= 2f;
                endOnTime *= 2f;
                workOffset += random.NextFloat(0.2f, 0.5f) + (float)(lateShiftOffset * delayFactor * 4);
            }   
            else if (worker.m_Shift == Workshift.Night)
            {
                workOffset *= 4f;
                startOnTime *= 4f;
                endOnTime *= 4f;
                workOffset += random.NextFloat(0.4f,0.7f) + (float)(lateShiftOffset * delayFactor * 10);
            }
                
            double num1 = (double)math.frac((float)(((double)economyParameters.m_WorkDayStart + (double)workOffset + startOnTime)));
            float y = math.frac((float)(((double)economyParameters.m_WorkDayEnd + (double)workOffset + endOnTime)));

            float num2 = 0.0f;
            if (includeCommute)
            {
                float num3 = 60f * worker.m_LastCommuteTime;
                if ((double)num3 < 60.0)
                    num3 = 40000f;
                num2 = num3 / Time2WorkTimeSystem.kTicksPerDay;
            }
            double num4 = (double)num2;
            return new float2(math.frac((float)(num1 - num4)), y);
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            this.m_Time2WorkCitizenBehaviorSystem = this.World.GetOrCreateSystemManaged<Time2WorkCitizenBehaviorSystem>();
            this.m_EndFrameBarrier = this.World.GetOrCreateSystemManaged<EndFrameBarrier>();
            this.m_TimeSystem = this.World.GetOrCreateSystemManaged<TimeSystem>();
            this.m_SimulationSystem = this.World.GetOrCreateSystemManaged<SimulationSystem>();
            this.m_TriggerSystem = this.World.GetOrCreateSystemManaged<TriggerSystem>();
            this.m_WorkerQuery = this.GetEntityQuery(ComponentType.ReadOnly<Worker>(), ComponentType.ReadOnly<Citizen>(), ComponentType.ReadOnly<TravelPurpose>(), ComponentType.ReadOnly<CurrentBuilding>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
            this.m_GotoWorkQuery = this.GetEntityQuery(ComponentType.ReadOnly<Worker>(), ComponentType.ReadOnly<Citizen>(), ComponentType.ReadOnly<CurrentBuilding>(), ComponentType.Exclude<TravelPurpose>(), ComponentType.Exclude<HealthProblem>(), ComponentType.Exclude<ResourceBuyer>(), ComponentType.ReadWrite<TripNeeded>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
            this.m_EconomyParameterQuery = this.GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
            this.m_TimeDataQuery = this.GetEntityQuery(ComponentType.ReadOnly<TimeData>());
            this.m_PopulationQuery = this.GetEntityQuery(ComponentType.ReadOnly<Population>());
            this.RequireAnyForUpdate(this.m_GotoWorkQuery, this.m_WorkerQuery);
            this.RequireForUpdate(this.m_EconomyParameterQuery);
        }

        protected override void OnUpdate()
        {
            double delayFactor = (float)(Mod.m_Setting.delay_factor) / 100;
            uint frameWithInterval = SimulationUtils.GetUpdateFrameWithInterval(this.m_SimulationSystem.frameIndex, (uint)this.GetUpdateInterval(SystemUpdatePhase.GameSimulation), 16);
            this.__TypeHandle.__Game_City_Population_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_AttendingMeeting_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_TravelPurpose_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Objects_OutsideConnection_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_CarKeeper_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_Building_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_TripNeeded_RW_BufferTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_Worker_RO_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_Citizen_RW_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref this.CheckedStateRef);
            JobHandle deps;
           
            JobHandle jobHandle1 = new Time2WorkWorkerSystem.GoToWorkJob()
            {
                m_EntityType = this.__TypeHandle.__Unity_Entities_Entity_TypeHandle,
                m_CitizenType = this.__TypeHandle.__Game_Citizens_Citizen_RW_ComponentTypeHandle,
                m_CurrentBuildingType = this.__TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle,
                m_WorkerType = this.__TypeHandle.__Game_Citizens_Worker_RO_ComponentTypeHandle,
                m_TripType = this.__TypeHandle.__Game_Citizens_TripNeeded_RW_BufferTypeHandle,
                m_UpdateFrameType = this.__TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle,
                m_Buildings = this.__TypeHandle.__Game_Buildings_Building_RO_ComponentLookup,
                m_CarKeepers = this.__TypeHandle.__Game_Citizens_CarKeeper_RO_ComponentLookup,
                m_Properties = this.__TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup,
                m_OutsideConnections = this.__TypeHandle.__Game_Objects_OutsideConnection_RO_ComponentLookup,
                m_Purposes = this.__TypeHandle.__Game_Citizens_TravelPurpose_RO_ComponentLookup,
                m_Attendings = this.__TypeHandle.__Game_Citizens_AttendingMeeting_RO_ComponentLookup,
                m_PopulationData = this.__TypeHandle.__Game_City_Population_RO_ComponentLookup,
                m_TriggerBuffer = this.m_TriggerSystem.CreateActionBuffer().AsParallelWriter(),
                m_EconomyParameters = this.m_EconomyParameterQuery.GetSingleton<EconomyParameterData>(),
                m_TimeOfDay = this.m_TimeSystem.normalizedTime,
                m_UpdateFrameIndex = frameWithInterval,
                m_Frame = this.m_SimulationSystem.frameIndex,
                m_TimeData = this.m_TimeDataQuery.GetSingleton<TimeData>(),
                m_PopulationEntity = this.m_PopulationQuery.GetSingletonEntity(),
                m_CarReserverQueue = this.m_Time2WorkCitizenBehaviorSystem.GetCarReserverQueue(out deps),
                m_CommandBuffer = this.m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter()
            }.ScheduleParallel<Time2WorkWorkerSystem.GoToWorkJob>(this.m_GotoWorkQuery, JobHandle.CombineDependencies(this.Dependency, deps));
            this.m_EndFrameBarrier.AddJobHandleForProducer(jobHandle1);
            this.m_Time2WorkCitizenBehaviorSystem.AddCarReserveWriter(jobHandle1);
            this.m_TriggerSystem.AddActionBufferWriter(jobHandle1);
            this.__TypeHandle.__Game_Companies_WorkProvider_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_AttendingMeeting_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_Citizen_RW_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_TravelPurpose_RO_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_Worker_RO_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref this.CheckedStateRef);
           
            JobHandle jobHandle2 = new Time2WorkWorkerSystem.WorkJob()
            {
                m_EntityType = this.__TypeHandle.__Unity_Entities_Entity_TypeHandle,
                m_WorkerType = this.__TypeHandle.__Game_Citizens_Worker_RO_ComponentTypeHandle,
                m_PurposeType = this.__TypeHandle.__Game_Citizens_TravelPurpose_RO_ComponentTypeHandle,
                m_UpdateFrameType = this.__TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle,
                m_CitizenType = this.__TypeHandle.__Game_Citizens_Citizen_RW_ComponentTypeHandle,
                m_Attendings = this.__TypeHandle.__Game_Citizens_AttendingMeeting_RO_ComponentLookup,
                m_Workplaces = this.__TypeHandle.__Game_Companies_WorkProvider_RO_ComponentLookup,
                m_TriggerBuffer = this.m_TriggerSystem.CreateActionBuffer().AsParallelWriter(),
                m_EconomyParameters = this.m_EconomyParameterQuery.GetSingleton<EconomyParameterData>(),
                m_UpdateFrameIndex = frameWithInterval,
                m_TimeOfDay = this.m_TimeSystem.normalizedTime,
                m_Frame = this.m_SimulationSystem.frameIndex,
                m_TimeData = this.m_TimeDataQuery.GetSingleton<TimeData>(),
                m_CommandBuffer = this.m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter()
            }.ScheduleParallel<Time2WorkWorkerSystem.WorkJob>(this.m_WorkerQuery, JobHandle.CombineDependencies(this.Dependency, jobHandle1));
            this.m_EndFrameBarrier.AddJobHandleForProducer(jobHandle2);
            this.m_TriggerSystem.AddActionBufferWriter(jobHandle2);
            this.Dependency = jobHandle2;
        }

        private void __AssignQueries(ref SystemState state)
        {
        }

        protected override void OnCreateForCompiler()
        {
            base.OnCreateForCompiler();
            this.__AssignQueries(ref this.CheckedStateRef);
            this.__TypeHandle.__AssignHandles(ref this.CheckedStateRef);
        }

        public Time2WorkWorkerSystem()
        {
        }
        private struct GoToWorkJob : IJobChunk
        {
            [ReadOnly]
            public EntityTypeHandle m_EntityType;
            public ComponentTypeHandle<Citizen> m_CitizenType;
            [ReadOnly]
            public ComponentTypeHandle<Worker> m_WorkerType;
            [ReadOnly]
            public ComponentTypeHandle<CurrentBuilding> m_CurrentBuildingType;
            public BufferTypeHandle<TripNeeded> m_TripType;
            [ReadOnly]
            public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;
            [ReadOnly]
            public ComponentLookup<TravelPurpose> m_Purposes;
            [ReadOnly]
            public ComponentLookup<PropertyRenter> m_Properties;
            [ReadOnly]
            public ComponentLookup<Building> m_Buildings;
            [ReadOnly]
            public ComponentLookup<CarKeeper> m_CarKeepers;
            [ReadOnly]
            public ComponentLookup<Game.Objects.OutsideConnection> m_OutsideConnections;
            [ReadOnly]
            public ComponentLookup<AttendingMeeting> m_Attendings;
            [ReadOnly]
            public ComponentLookup<Population> m_PopulationData;
            public NativeQueue<TriggerAction>.ParallelWriter m_TriggerBuffer;
            public uint m_Frame;
            public TimeData m_TimeData;
            public uint m_UpdateFrameIndex;
            public float m_TimeOfDay;
            public Entity m_PopulationEntity;
            public EconomyParameterData m_EconomyParameters;
            public NativeQueue<Entity>.ParallelWriter m_CarReserverQueue;
            public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

            public void Execute(
              in ArchetypeChunk chunk,
              int unfilteredChunkIndex,
              bool useEnabledMask,
              in v128 chunkEnabledMask)
            {
                if ((int)chunk.GetSharedComponent<UpdateFrame>(this.m_UpdateFrameType).m_Index != (int)this.m_UpdateFrameIndex)
                    return;
                NativeArray<Entity> nativeArray1 = chunk.GetNativeArray(this.m_EntityType);
                NativeArray<Citizen> nativeArray2 = chunk.GetNativeArray<Citizen>(ref this.m_CitizenType);
                NativeArray<Worker> nativeArray3 = chunk.GetNativeArray<Worker>(ref this.m_WorkerType);
                NativeArray<CurrentBuilding> nativeArray4 = chunk.GetNativeArray<CurrentBuilding>(ref this.m_CurrentBuildingType);
                BufferAccessor<TripNeeded> bufferAccessor = chunk.GetBufferAccessor<TripNeeded>(ref this.m_TripType);

                int population = this.m_PopulationData[this.m_PopulationEntity].m_Population;
                for (int index = 0; index < nativeArray1.Length; ++index)
                {
                    Entity entity1 = nativeArray1[index];
                    Citizen citizen = nativeArray2[index];

                    if (!Time2WorkWorkerSystem.IsTodayOffDay(citizen, ref this.m_EconomyParameters, this.m_Frame, this.m_TimeData, population) && !Time2WorkWorkerSystem.IsLunchTime(citizen, nativeArray3[index], ref this.m_EconomyParameters, this.m_TimeOfDay) && Time2WorkWorkerSystem.IsTimeToWork(citizen, nativeArray3[index], ref this.m_EconomyParameters, this.m_TimeOfDay))
                    {
                        DynamicBuffer<TripNeeded> dynamicBuffer = bufferAccessor[index];
                        if (!this.m_Attendings.HasComponent(entity1) && (citizen.m_State & CitizenFlags.MovingAway) == CitizenFlags.None)
                        {
                            Entity workplace = nativeArray3[index].m_Workplace;
                            Entity entity2 = Entity.Null;
                            if (this.m_Properties.HasComponent(workplace))
                            {
                                entity2 = this.m_Properties[workplace].m_Property;
                            }
                            else
                            {
                                if (this.m_Buildings.HasComponent(workplace))
                                {
                                    entity2 = workplace;
                                }
                                else
                                {
                                    if (this.m_OutsideConnections.HasComponent(workplace))
                                        entity2 = workplace;
                                }
                            }
                            if (entity2 != Entity.Null)
                            {
                                if (nativeArray4[index].m_CurrentBuilding != entity2)
                                {
                                    if (!this.m_CarKeepers.IsComponentEnabled(entity1))
                                    {
                                        this.m_CarReserverQueue.Enqueue(entity1);
                                    }
                                    dynamicBuffer.Add(new TripNeeded()
                                    {
                                        m_TargetAgent = workplace,
                                        m_Purpose = Game.Citizens.Purpose.GoingToWork
                                    });
                                }
                            }
                            else
                            {
                                citizen.SetFailedEducationCount(0);
                                nativeArray2[index] = citizen;

                                if (this.m_Purposes.HasComponent(entity1) && (this.m_Purposes[entity1].m_Purpose == Game.Citizens.Purpose.GoingToWork || this.m_Purposes[entity1].m_Purpose == Game.Citizens.Purpose.Working))
                                {
                                    this.m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, entity1);
                                }
                                this.m_CommandBuffer.RemoveComponent<Worker>(unfilteredChunkIndex, entity1);
                                this.m_TriggerBuffer.Enqueue(new TriggerAction(TriggerType.CitizenBecameUnemployed, Entity.Null, entity1, workplace));
                            }
                        }
                    }
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

        private struct WorkJob : IJobChunk
        {
            [ReadOnly]
            public ComponentTypeHandle<Worker> m_WorkerType;
            [ReadOnly]
            public EntityTypeHandle m_EntityType;
            [ReadOnly]
            public ComponentTypeHandle<TravelPurpose> m_PurposeType;
            [ReadOnly]
            public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;
            public ComponentTypeHandle<Citizen> m_CitizenType;
            [ReadOnly]
            public ComponentLookup<WorkProvider> m_Workplaces;
            [ReadOnly]
            public ComponentLookup<AttendingMeeting> m_Attendings;
            public EconomyParameterData m_EconomyParameters;
            public NativeQueue<TriggerAction>.ParallelWriter m_TriggerBuffer;
            public float m_TimeOfDay;
            public uint m_UpdateFrameIndex;
            public uint m_Frame;
            public TimeData m_TimeData;
            public int m_Population;
            public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

            public void Execute(
              in ArchetypeChunk chunk,
              int unfilteredChunkIndex,
              bool useEnabledMask,
              in v128 chunkEnabledMask)
            {
                if ((int)chunk.GetSharedComponent<UpdateFrame>(this.m_UpdateFrameType).m_Index != (int)this.m_UpdateFrameIndex)
                    return;
                NativeArray<Entity> nativeArray1 = chunk.GetNativeArray(this.m_EntityType);
                NativeArray<Worker> nativeArray2 = chunk.GetNativeArray<Worker>(ref this.m_WorkerType);
                NativeArray<TravelPurpose> nativeArray3 = chunk.GetNativeArray<TravelPurpose>(ref this.m_PurposeType);
                NativeArray<Citizen> nativeArray4 = chunk.GetNativeArray<Citizen>(ref this.m_CitizenType);
                for (int index = 0; index < nativeArray1.Length; ++index)
                {
                    Entity entity = nativeArray1[index];
                    Entity workplace = nativeArray2[index].m_Workplace;
                    Worker worker = nativeArray2[index];
                    Citizen citizen = nativeArray4[index];
                    if (!this.m_Workplaces.HasComponent(workplace))
                    {
                        citizen.SetFailedEducationCount(0);
                        nativeArray4[index] = citizen;
                        TravelPurpose travelPurpose = nativeArray3[index];
                        if (travelPurpose.m_Purpose == Game.Citizens.Purpose.GoingToWork || travelPurpose.m_Purpose == Game.Citizens.Purpose.Working)
                        {
                            this.m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, entity);
                        }
                        this.m_CommandBuffer.RemoveComponent<Worker>(unfilteredChunkIndex, entity);
                        this.m_TriggerBuffer.Enqueue(new TriggerAction(TriggerType.CitizenBecameUnemployed, Entity.Null, entity, workplace));
                    }
                    else
                    {
                        if ((!Time2WorkWorkerSystem.IsTimeToWork(citizen, worker, ref this.m_EconomyParameters, this.m_TimeOfDay) || this.m_Attendings.HasComponent(entity) || Time2WorkWorkerSystem.IsLunchTime(citizen, worker, ref this.m_EconomyParameters, this.m_TimeOfDay)) && nativeArray3[index].m_Purpose == Game.Citizens.Purpose.Working)
                        {
                            this.m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, entity);
                        }
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
            [ReadOnly]
            public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;
            public ComponentTypeHandle<Citizen> __Game_Citizens_Citizen_RW_ComponentTypeHandle;
            [ReadOnly]
            public ComponentTypeHandle<CurrentBuilding> __Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle;
            [ReadOnly]
            public ComponentTypeHandle<Worker> __Game_Citizens_Worker_RO_ComponentTypeHandle;
            public BufferTypeHandle<TripNeeded> __Game_Citizens_TripNeeded_RW_BufferTypeHandle;
            public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;
            [ReadOnly]
            public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<CarKeeper> __Game_Citizens_CarKeeper_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Game.Objects.OutsideConnection> __Game_Objects_OutsideConnection_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<TravelPurpose> __Game_Citizens_TravelPurpose_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<AttendingMeeting> __Game_Citizens_AttendingMeeting_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Population> __Game_City_Population_RO_ComponentLookup;
            [ReadOnly]
            public ComponentTypeHandle<TravelPurpose> __Game_Citizens_TravelPurpose_RO_ComponentTypeHandle;
            [ReadOnly]
            public ComponentLookup<WorkProvider> __Game_Companies_WorkProvider_RO_ComponentLookup;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void __AssignHandles(ref SystemState state)
            {
                this.__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
                this.__Game_Citizens_Citizen_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Citizen>();
                this.__Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CurrentBuilding>(true);
                this.__Game_Citizens_Worker_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Worker>(true);
                this.__Game_Citizens_TripNeeded_RW_BufferTypeHandle = state.GetBufferTypeHandle<TripNeeded>();
                this.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
                this.__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(true);
                this.__Game_Citizens_CarKeeper_RO_ComponentLookup = state.GetComponentLookup<CarKeeper>(true);
                this.__Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(true);
                this.__Game_Objects_OutsideConnection_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.OutsideConnection>(true);
                this.__Game_Citizens_TravelPurpose_RO_ComponentLookup = state.GetComponentLookup<TravelPurpose>(true);
                this.__Game_Citizens_AttendingMeeting_RO_ComponentLookup = state.GetComponentLookup<AttendingMeeting>(true);
                this.__Game_City_Population_RO_ComponentLookup = state.GetComponentLookup<Population>(true);
                this.__Game_Citizens_TravelPurpose_RO_ComponentTypeHandle = state.GetComponentTypeHandle<TravelPurpose>(true);
                this.__Game_Companies_WorkProvider_RO_ComponentLookup = state.GetComponentLookup<WorkProvider>(true);
            }
        }
    }
}
