using Game;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using System;
using System.Runtime.CompilerServices;
using Time2Work.Systems;
using Time2Work.Utils;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

#nullable disable
namespace Time2Work
{
    public partial class Time2WorkStudentSystem : GameSystemBase
    {
        private EndFrameBarrier m_EndFrameBarrier;
        private Time2WorkTimeSystem m_TimeSystem;
        private Time2WorkCitizenBehaviorSystem m_CitizenBehaviorSystem;
        private SimulationSystem m_SimulationSystem;
        private EntityQuery m_EconomyParameterQuery;
        private EntityQuery m_GotoSchoolQuery;
        private EntityQuery m_StudentQuery;
        private EntityQuery m_TimeDataQuery;
        private EntityQuery m_PopulationQuery;
        private Time2WorkStudentSystem.TypeHandle __TypeHandle;

        public override int GetUpdateInterval(SystemUpdatePhase phase) => 16;

        public static float GetStudyOffset(Citizen citizen, int ticksPerDay)
        {
            return (float)(citizen.GetPseudoRandom(CitizenPseudoRandom.WorkOffset).NextInt((int)(21845* (ticksPerDay / TimeSystem.kTicksPerDay))) - ((int)10922*(ticksPerDay/TimeSystem.kTicksPerDay))) / ticksPerDay;
        }

        public static bool IsTimeToStudy(
          Citizen citizen,
          Game.Citizens.Student student,
          ref EconomyParameterData economyParameters,
          float timeOfDay,
          uint frame,
          TimeData timeData,
          int population,
          float3 offdayprob3,
          int3 school_start_time,
          int3 school_end_time,
          int ticksPerDay)
        {
            float offdayprob = offdayprob3.x;
            if(student.m_Level == 1)
            {
                offdayprob = offdayprob3.y;
            }
            if (student.m_Level > 2)
            {
                offdayprob = offdayprob3.z;
            }
            //int num = (int)Math.Round(offdayprob);
            int num = math.min((int)Math.Round(offdayprob), Mathf.RoundToInt(100f / math.max(1f, math.sqrt(economyParameters.m_TrafficReduction * (float)population))));
            int day = Time2WorkTimeSystem.GetDay(frame, timeData, ticksPerDay);
            float2 timeToStudy = Time2WorkStudentSystem.GetTimeToStudy(citizen, student, ref economyParameters, school_start_time, school_end_time, ticksPerDay);
            if (Unity.Mathematics.Random.CreateFromIndex((uint)citizen.m_PseudoRandom + (uint)day).NextInt(100) <= num)
                return false;
            return (double)timeToStudy.x >= (double)timeToStudy.y ? (double)timeOfDay >= (double)timeToStudy.x || (double)timeOfDay <= (double)timeToStudy.y : (double)timeOfDay >= (double)timeToStudy.x && (double)timeOfDay <= (double)timeToStudy.y;
        }

        public static float2 GetTimeToStudy(
          Citizen citizen,
          Game.Citizens.Student student,
          ref EconomyParameterData economyParameters,
          int3 school_start_time_,
          int3 school_end_time_,
          int ticksPerDay)
        {
            //x = elementary schoo, y = high school, z = college and university
            int school_start_time = school_start_time_.z;
            int school_end_time = school_end_time_.z;
            float studyOffset = Time2WorkStudentSystem.GetStudyOffset(citizen, ticksPerDay);
            if (student.m_Level == 0)
            {
                school_start_time = school_start_time_.x;
                school_end_time = school_end_time_.x;
            } else if((student.m_Level == 1))
            {
                school_start_time = school_start_time_.y;
                school_end_time = school_end_time_.y;
            } 
            float startTimeOffset = ((float)school_start_time - 4f) * (1 / 48f);
            float endTimeOffset = ((float)school_end_time - 19f) * (1 / 48f);

            //Adding variation on students schedule, the higher the education level, the higher the variation
            Unity.Mathematics.Random random = Unity.Mathematics.Random.CreateFromIndex((uint)(citizen.m_PseudoRandom));
            float startOnTime = (float)GaussianRandom.NextGaussianDouble(random) * (student.m_Level+1)/100f;
            float endOnTime = ((float)GaussianRandom.NextGaussianDouble(random)) * (student.m_Level+1)/100f;
            if(startOnTime < 0)
            {
                //We don't want students to arrive too early
                startOnTime /= (student.m_Level + 2);
            }

            startTimeOffset += startOnTime;
            endTimeOffset += endOnTime;

            float num1 = 60f * student.m_LastCommuteTime;
            if ((double)num1 < 60.0)
                num1 = 1800f;
            float num2 = num1 / ticksPerDay;

            return new float2(math.frac(economyParameters.m_WorkDayStart + studyOffset + startTimeOffset - num2), math.frac(economyParameters.m_WorkDayEnd + studyOffset + endTimeOffset));
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            this.m_CitizenBehaviorSystem = this.World.GetOrCreateSystemManaged<Time2WorkCitizenBehaviorSystem>();
            this.m_EndFrameBarrier = this.World.GetOrCreateSystemManaged<EndFrameBarrier>();
            this.m_TimeSystem = this.World.GetOrCreateSystemManaged<Time2WorkTimeSystem>();
            this.m_SimulationSystem = this.World.GetOrCreateSystemManaged<SimulationSystem>();
            this.m_StudentQuery = this.GetEntityQuery(ComponentType.ReadOnly<Game.Citizens.Student>(), ComponentType.ReadOnly<Citizen>(), ComponentType.ReadOnly<TravelPurpose>(), ComponentType.ReadOnly<CurrentBuilding>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
            this.m_GotoSchoolQuery = this.GetEntityQuery(ComponentType.ReadOnly<Game.Citizens.Student>(), ComponentType.ReadOnly<Citizen>(), ComponentType.ReadOnly<CurrentBuilding>(), ComponentType.Exclude<ResourceBuyer>(), ComponentType.Exclude<TravelPurpose>(), ComponentType.Exclude<HealthProblem>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
            this.m_EconomyParameterQuery = this.GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
            this.m_TimeDataQuery = this.GetEntityQuery(ComponentType.ReadOnly<TimeData>());
            this.m_PopulationQuery = this.GetEntityQuery(ComponentType.ReadOnly<Population>());
            this.RequireAnyForUpdate(this.m_StudentQuery, this.m_GotoSchoolQuery);
            this.RequireForUpdate(this.m_EconomyParameterQuery);
        }

        protected override void OnUpdate()
        {
            this.__TypeHandle.__Game_City_Population_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_AttendingMeeting_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Objects_OutsideConnection_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_CarKeeper_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_Building_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_TravelPurpose_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_TripNeeded_RW_BufferTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_Student_RO_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_Citizen_RO_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref this.CheckedStateRef);
            JobHandle deps;

            JobHandle jobHandle = new Time2WorkStudentSystem.GoToSchoolJob()
            {
                m_EntityType = this.__TypeHandle.__Unity_Entities_Entity_TypeHandle,
                m_CitizenType = this.__TypeHandle.__Game_Citizens_Citizen_RO_ComponentTypeHandle,
                m_CurrentBuildingType = this.__TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle,
                m_StudentType = this.__TypeHandle.__Game_Citizens_Student_RO_ComponentTypeHandle,
                m_TripType = this.__TypeHandle.__Game_Citizens_TripNeeded_RW_BufferTypeHandle,
                m_Purposes = this.__TypeHandle.__Game_Citizens_TravelPurpose_RO_ComponentLookup,
                m_Buildings = this.__TypeHandle.__Game_Buildings_Building_RO_ComponentLookup,
                m_CarKeepers = this.__TypeHandle.__Game_Citizens_CarKeeper_RO_ComponentLookup,
                m_Properties = this.__TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup,
                m_OutsideConnections = this.__TypeHandle.__Game_Objects_OutsideConnection_RO_ComponentLookup,
                m_Attendings = this.__TypeHandle.__Game_Citizens_AttendingMeeting_RO_ComponentLookup,
                m_PopulationData = this.__TypeHandle.__Game_City_Population_RO_ComponentLookup,
                m_EconomyParameters = this.m_EconomyParameterQuery.GetSingleton<EconomyParameterData>(),
                m_TimeOfDay = this.m_TimeSystem.normalizedTime,
                m_Frame = this.m_SimulationSystem.frameIndex,
                m_PopulationEntity = this.m_PopulationQuery.GetSingletonEntity(),
                m_TimeData = this.m_TimeDataQuery.GetSingleton<TimeData>(),
                m_CarReserverQueue = this.m_CitizenBehaviorSystem.GetCarReserverQueue(out deps),
                m_CommandBuffer = this.m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
                school_offdayprob = WeekSystem.getSchoolOffDayProb(),
                school_start_time = new int3((int)Mod.m_Setting.school_start_time, (int)Mod.m_Setting.high_school_start_time, (int)Mod.m_Setting.univ_start_time),
                school_end_time = new int3((int)Mod.m_Setting.school_end_time, (int)Mod.m_Setting.high_school_end_time, (int)Mod.m_Setting.univ_end_time),
                ticksPerDay = Time2WorkTimeSystem.kTicksPerDay
            }.ScheduleParallel<Time2WorkStudentSystem.GoToSchoolJob>(this.m_GotoSchoolQuery, JobHandle.CombineDependencies(this.Dependency, deps));
            this.m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
            this.m_CitizenBehaviorSystem.AddCarReserveWriter(jobHandle);
            this.__TypeHandle.__Game_Buildings_School_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Common_Target_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_AttendingMeeting_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_Citizen_RO_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_TravelPurpose_RO_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_Student_RO_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref this.CheckedStateRef);

            JobHandle producerJob = new Time2WorkStudentSystem.StudyJob()
            {
                m_EntityType = this.__TypeHandle.__Unity_Entities_Entity_TypeHandle,
                m_StudentType = this.__TypeHandle.__Game_Citizens_Student_RO_ComponentTypeHandle,
                m_PurposeType = this.__TypeHandle.__Game_Citizens_TravelPurpose_RO_ComponentTypeHandle,
                m_CitizenType = this.__TypeHandle.__Game_Citizens_Citizen_RO_ComponentTypeHandle,
                m_Attendings = this.__TypeHandle.__Game_Citizens_AttendingMeeting_RO_ComponentLookup,
                m_CurrentBuildings = this.__TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentLookup,
                m_Targets = this.__TypeHandle.__Game_Common_Target_RO_ComponentLookup,
                m_Schools = this.__TypeHandle.__Game_Buildings_School_RO_ComponentLookup,
                m_EconomyParameters = this.m_EconomyParameterQuery.GetSingleton<EconomyParameterData>(),
                m_TimeOfDay = this.m_TimeSystem.normalizedTime,
                m_CommandBuffer = this.m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
                ticksPerDay = Time2WorkTimeSystem.kTicksPerDay
            }.ScheduleParallel<Time2WorkStudentSystem.StudyJob>(this.m_StudentQuery, JobHandle.CombineDependencies(this.Dependency, jobHandle));

            this.m_EndFrameBarrier.AddJobHandleForProducer(producerJob);
            this.Dependency = producerJob;
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

        public Time2WorkStudentSystem()
        {
        }

        private struct GoToSchoolJob : IJobChunk
        {
            public EntityTypeHandle m_EntityType;
            public ComponentTypeHandle<Citizen> m_CitizenType;
            public ComponentTypeHandle<Game.Citizens.Student> m_StudentType;
            public ComponentTypeHandle<CurrentBuilding> m_CurrentBuildingType;
            public BufferTypeHandle<TripNeeded> m_TripType;
            public ComponentLookup<PropertyRenter> m_Properties;
            public ComponentLookup<Building> m_Buildings;
            public ComponentLookup<CarKeeper> m_CarKeepers;
            public ComponentLookup<TravelPurpose> m_Purposes;
            public ComponentLookup<Game.Objects.OutsideConnection> m_OutsideConnections;
            public ComponentLookup<AttendingMeeting> m_Attendings;
            public ComponentLookup<Population> m_PopulationData;
            public float m_TimeOfDay;
            public uint m_Frame;
            public TimeData m_TimeData;
            public Entity m_PopulationEntity;
            public EconomyParameterData m_EconomyParameters;
            public NativeQueue<Entity>.ParallelWriter m_CarReserverQueue;
            public EntityCommandBuffer.ParallelWriter m_CommandBuffer;
            public float3 school_offdayprob;
            public int3 school_start_time;
            public int3 school_end_time;
            public int ticksPerDay;
            public void Execute(
              in ArchetypeChunk chunk,
              int unfilteredChunkIndex,
              bool useEnabledMask,
              in v128 chunkEnabledMask)
            {
                NativeArray<Entity> nativeArray1 = chunk.GetNativeArray(this.m_EntityType);
                NativeArray<Citizen> nativeArray2 = chunk.GetNativeArray<Citizen>(ref this.m_CitizenType);
                NativeArray<Game.Citizens.Student> nativeArray3 = chunk.GetNativeArray<Game.Citizens.Student>(ref this.m_StudentType);
                NativeArray<CurrentBuilding> nativeArray4 = chunk.GetNativeArray<CurrentBuilding>(ref this.m_CurrentBuildingType);
                BufferAccessor<TripNeeded> bufferAccessor = chunk.GetBufferAccessor<TripNeeded>(ref this.m_TripType);

                int population = this.m_PopulationData[this.m_PopulationEntity].m_Population;
                for (int index = 0; index < nativeArray1.Length; ++index)
                {
                    Entity entity1 = nativeArray1[index];
                    Citizen citizen = nativeArray2[index];

                    Unity.Mathematics.Random rand = Unity.Mathematics.Random.CreateFromIndex((uint)(citizen.m_PseudoRandom + Time2WorkTimeSystem.GetDay(this.m_Frame, this.m_TimeData, ticksPerDay)));
                    int simulate_prob = rand.NextInt(100);
                    if (simulate_prob > (100 - Mod.simulation_reduction))
                    {
                        continue;
                    }

                    if (Time2WorkStudentSystem.IsTimeToStudy(citizen, nativeArray3[index], ref this.m_EconomyParameters, this.m_TimeOfDay, this.m_Frame, this.m_TimeData, population, school_offdayprob, school_start_time, school_end_time, ticksPerDay))
                    {
                        DynamicBuffer<TripNeeded> dynamicBuffer = bufferAccessor[index];
                        if (!this.m_Attendings.HasComponent(entity1) && (citizen.m_State & CitizenFlags.MovingAwayReachOC) == CitizenFlags.None)
                        {
                            Entity school = nativeArray3[index].m_School;
                            Entity entity2 = Entity.Null;
                            if (this.m_Properties.HasComponent(school))
                            {
                                entity2 = this.m_Properties[school].m_Property;
                            }
                            else
                            {
                                if (this.m_Buildings.HasComponent(school) || this.m_OutsideConnections.HasComponent(school))
                                    entity2 = school;
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
                                        m_TargetAgent = school,
                                        m_Purpose = Game.Citizens.Purpose.GoingToSchool
                                    });
                                }
                            }
                            else
                            {
                                if (this.m_Purposes.HasComponent(entity1) && (this.m_Purposes[entity1].m_Purpose == Game.Citizens.Purpose.Studying || this.m_Purposes[entity1].m_Purpose == Game.Citizens.Purpose.GoingToSchool))
                                {
                                    this.m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, entity1);
                                }
                                this.m_CommandBuffer.AddComponent<StudentsRemoved>(unfilteredChunkIndex, school);
                                this.m_CommandBuffer.RemoveComponent<Game.Citizens.Student>(unfilteredChunkIndex, entity1);
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

        private struct StudyJob : IJobChunk
        {
            [ReadOnly]
            public ComponentTypeHandle<Game.Citizens.Student> m_StudentType;
            [ReadOnly]
            public EntityTypeHandle m_EntityType;
            [ReadOnly]
            public ComponentTypeHandle<TravelPurpose> m_PurposeType;
            [ReadOnly]
            public ComponentTypeHandle<Citizen> m_CitizenType;
            [ReadOnly]
            public ComponentLookup<Game.Buildings.School> m_Schools;
            [ReadOnly]
            public ComponentLookup<Game.Common.Target> m_Targets;
            [ReadOnly]
            public ComponentLookup<CurrentBuilding> m_CurrentBuildings;
            [ReadOnly]
            public ComponentLookup<AttendingMeeting> m_Attendings;
            public EconomyParameterData m_EconomyParameters;
            public float m_TimeOfDay;
            public EntityCommandBuffer.ParallelWriter m_CommandBuffer;
            public int ticksPerDay;
            public void Execute(
              in ArchetypeChunk chunk,
              int unfilteredChunkIndex,
              bool useEnabledMask,
              in v128 chunkEnabledMask)
            {
                NativeArray<Entity> nativeArray1 = chunk.GetNativeArray(this.m_EntityType);
                NativeArray<Game.Citizens.Student> nativeArray2 = chunk.GetNativeArray<Game.Citizens.Student>(ref this.m_StudentType);
                NativeArray<TravelPurpose> nativeArray3 = chunk.GetNativeArray<TravelPurpose>(ref this.m_PurposeType);
                NativeArray<Citizen> nativeArray4 = chunk.GetNativeArray<Citizen>(ref this.m_CitizenType);
                for (int index = 0; index < nativeArray1.Length; ++index)
                {
                    Entity entity = nativeArray1[index];
                    Entity school = nativeArray2[index].m_School;
                    float studyOffset = Time2WorkStudentSystem.GetStudyOffset(nativeArray4[index], ticksPerDay);
                    if (!this.m_Schools.HasComponent(school))
                    {
                        TravelPurpose travelPurpose = nativeArray3[index];
                        if (travelPurpose.m_Purpose == Game.Citizens.Purpose.GoingToSchool || travelPurpose.m_Purpose == Game.Citizens.Purpose.Studying)
                        {
                            this.m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, entity);
                        }
                        this.m_CommandBuffer.RemoveComponent<Game.Citizens.Student>(unfilteredChunkIndex, entity);
                    }
                    else
                    {

                        if (!this.m_Targets.HasComponent(entity) && this.m_CurrentBuildings.HasComponent(entity) && this.m_CurrentBuildings[entity].m_CurrentBuilding != school)
                        {
                            if (nativeArray3[index].m_Purpose == Game.Citizens.Purpose.Studying)
                            {
                                this.m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, entity);
                            }
                        }
                        else
                        {
                            if (((double)this.m_TimeOfDay > (double)this.m_EconomyParameters.m_WorkDayEnd + (double)studyOffset || (double)this.m_TimeOfDay < (double)this.m_EconomyParameters.m_WorkDayStart + (double)studyOffset || this.m_Attendings.HasComponent(entity)) && nativeArray3[index].m_Purpose == Game.Citizens.Purpose.Studying)
                            {
                                this.m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, entity);
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

        private struct TypeHandle
        {
            [ReadOnly]
            public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;
            [ReadOnly]
            public ComponentTypeHandle<Citizen> __Game_Citizens_Citizen_RO_ComponentTypeHandle;
            [ReadOnly]
            public ComponentTypeHandle<CurrentBuilding> __Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle;
            [ReadOnly]
            public ComponentTypeHandle<Game.Citizens.Student> __Game_Citizens_Student_RO_ComponentTypeHandle;
            public BufferTypeHandle<TripNeeded> __Game_Citizens_TripNeeded_RW_BufferTypeHandle;
            [ReadOnly]
            public ComponentLookup<TravelPurpose> __Game_Citizens_TravelPurpose_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<CarKeeper> __Game_Citizens_CarKeeper_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Game.Objects.OutsideConnection> __Game_Objects_OutsideConnection_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<AttendingMeeting> __Game_Citizens_AttendingMeeting_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Population> __Game_City_Population_RO_ComponentLookup;
            [ReadOnly]
            public ComponentTypeHandle<TravelPurpose> __Game_Citizens_TravelPurpose_RO_ComponentTypeHandle;
            [ReadOnly]
            public ComponentLookup<CurrentBuilding> __Game_Citizens_CurrentBuilding_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Game.Common.Target> __Game_Common_Target_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Game.Buildings.School> __Game_Buildings_School_RO_ComponentLookup;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void __AssignHandles(ref SystemState state)
            {
                this.__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
                this.__Game_Citizens_Citizen_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Citizen>(true);
                this.__Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CurrentBuilding>(true);
                this.__Game_Citizens_Student_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Citizens.Student>(true);
                this.__Game_Citizens_TripNeeded_RW_BufferTypeHandle = state.GetBufferTypeHandle<TripNeeded>();
                this.__Game_Citizens_TravelPurpose_RO_ComponentLookup = state.GetComponentLookup<TravelPurpose>(true);
                this.__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(true);
                this.__Game_Citizens_CarKeeper_RO_ComponentLookup = state.GetComponentLookup<CarKeeper>(true);
                this.__Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(true);
                this.__Game_Objects_OutsideConnection_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.OutsideConnection>(true);
                this.__Game_Citizens_AttendingMeeting_RO_ComponentLookup = state.GetComponentLookup<AttendingMeeting>(true);
                this.__Game_City_Population_RO_ComponentLookup = state.GetComponentLookup<Population>(true);
                this.__Game_Citizens_TravelPurpose_RO_ComponentTypeHandle = state.GetComponentTypeHandle<TravelPurpose>(true);
                this.__Game_Citizens_CurrentBuilding_RO_ComponentLookup = state.GetComponentLookup<CurrentBuilding>(true);
                this.__Game_Common_Target_RO_ComponentLookup = state.GetComponentLookup<Game.Common.Target>(true);
                this.__Game_Buildings_School_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.School>(true);
            }
        }
    }
}
