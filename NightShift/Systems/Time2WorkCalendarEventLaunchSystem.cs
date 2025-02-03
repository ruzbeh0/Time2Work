// Decompiled with JetBrains decompiler
// Type: Game.Simulation.CalendarEventLaunchSystem
// Assembly: Game, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 3C8C3C1D-D7EB-4536-8BE0-6F4028D2725F
// Assembly location: C:\Program Files (x86)\Steam\steamapps\common\Cities Skylines II\Cities2_Data\Managed\Game.dll

using Game;
using Game.Common;
using Game.Prefabs;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

#nullable disable
namespace Time2Work.Systems
{
    [CompilerGenerated]
    public partial class Time2WorkCalendarEventLaunchSystem : GameSystemBase
    {
        private const int UPDATES_PER_DAY = 4;
        private EndFrameBarrier m_EndFrameBarrier;
        private Time2WorkTimeSystem m_TimeSystem;
        private EntityQuery m_CalendarEventQuery;
        private Time2WorkCalendarEventLaunchSystem.TypeHandle __TypeHandle;

        public override int GetUpdateInterval(SystemUpdatePhase phase) => 65536;

        [UnityEngine.Scripting.Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();
            this.m_TimeSystem = this.World.GetOrCreateSystemManaged<Time2WorkTimeSystem>();
            this.m_EndFrameBarrier = this.World.GetOrCreateSystemManaged<EndFrameBarrier>();
            this.m_CalendarEventQuery = this.GetEntityQuery(ComponentType.ReadOnly<CalendarEventData>());
            this.GetEntityQuery(ComponentType.ReadOnly<TimeSettingsData>());
            this.RequireForUpdate(this.m_CalendarEventQuery);
        }

        [UnityEngine.Scripting.Preserve]
        protected override void OnUpdate()
        {
            CalendarEventMonths calendarEventMonths = (CalendarEventMonths)(1 << Mathf.FloorToInt(this.m_TimeSystem.normalizedDate * (12f/Mod.m_Setting.daysPerMonth)));
            CalendarEventTimes calendarEventTimes = (CalendarEventTimes)(1 << Mathf.FloorToInt(this.m_TimeSystem.normalizedTime * 4f));
            this.__TypeHandle.__Game_Prefabs_CalendarEventData_RO_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_EventData_RO_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref this.CheckedStateRef);

            Time2WorkCalendarEventLaunchSystem.CheckEventLaunchJob jobData = new Time2WorkCalendarEventLaunchSystem.CheckEventLaunchJob()
            {
                m_EntityType = this.__TypeHandle.__Unity_Entities_Entity_TypeHandle,
                m_EventType = this.__TypeHandle.__Game_Prefabs_EventData_RO_ComponentTypeHandle,
                m_CalendarEventType = this.__TypeHandle.__Game_Prefabs_CalendarEventData_RO_ComponentTypeHandle,
                m_Month = calendarEventMonths,
                m_Time = calendarEventTimes,
                m_RandomSeed = RandomSeed.Next(),
                m_CommandBuffer = this.m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter()
            };
            this.Dependency = jobData.ScheduleParallel<Time2WorkCalendarEventLaunchSystem.CheckEventLaunchJob>(this.m_CalendarEventQuery, this.Dependency);
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
        public Time2WorkCalendarEventLaunchSystem()
        {
        }

        [BurstCompile]
        private struct CheckEventLaunchJob : IJobChunk
        {
            [ReadOnly]
            public EntityTypeHandle m_EntityType;
            [ReadOnly]
            public ComponentTypeHandle<CalendarEventData> m_CalendarEventType;
            [ReadOnly]
            public ComponentTypeHandle<EventData> m_EventType;
            [ReadOnly]
            public RandomSeed m_RandomSeed;
            public CalendarEventMonths m_Month;
            public CalendarEventTimes m_Time;
            public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

            public void Execute(
              in ArchetypeChunk chunk,
              int unfilteredChunkIndex,
              bool useEnabledMask,
              in v128 chunkEnabledMask)
            {
                Unity.Mathematics.Random random = this.m_RandomSeed.GetRandom(unfilteredChunkIndex);
                NativeArray<Entity> nativeArray1 = chunk.GetNativeArray(this.m_EntityType);
                NativeArray<CalendarEventData> nativeArray2 = chunk.GetNativeArray<CalendarEventData>(ref this.m_CalendarEventType);
                NativeArray<EventData> nativeArray3 = chunk.GetNativeArray<EventData>(ref this.m_EventType);
                for (int index = 0; index < nativeArray1.Length; ++index)
                {
                    Entity prefab = nativeArray1[index];
                    CalendarEventData calendarEventData = nativeArray2[index];
                    EventData eventData = nativeArray3[index];

                    if ((this.m_Month & calendarEventData.m_AllowedMonths) != (CalendarEventMonths)0 && (this.m_Time & calendarEventData.m_AllowedTimes) != (CalendarEventTimes)0 && (double)random.NextInt(100) < (double)calendarEventData.m_OccurenceProbability.min)
                    {
                        Entity entity = this.m_CommandBuffer.CreateEntity(unfilteredChunkIndex, eventData.m_Archetype);
                        this.m_CommandBuffer.SetComponent<PrefabRef>(unfilteredChunkIndex, entity, new PrefabRef(prefab));
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

        private struct TypeHandle
        {
            [ReadOnly]
            public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;
            [ReadOnly]
            public ComponentTypeHandle<EventData> __Game_Prefabs_EventData_RO_ComponentTypeHandle;
            [ReadOnly]
            public ComponentTypeHandle<CalendarEventData> __Game_Prefabs_CalendarEventData_RO_ComponentTypeHandle;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void __AssignHandles(ref SystemState state)
            {
                this.__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
                this.__Game_Prefabs_EventData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<EventData>(true);
                this.__Game_Prefabs_CalendarEventData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CalendarEventData>(true);
            }
        }
    }
}
