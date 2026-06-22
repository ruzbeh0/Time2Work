using Colossal.Mathematics;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using Game;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Creatures;
using Game.Notifications;
using Game.Prefabs;
using Game.Serialization;
using Game.Simulation;
using Game.Tools;
using Game.Triggers;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;

#nullable disable
namespace Time2Work.Systems
{
    public partial class Time2WorkDeathCheckSystem : GameSystemBase, IDefaultSerializable, ISerializable
    {
        public const int kUpdatesPerDay = 16;
        public const int kMaxAgeInGameYear = 9;

        private CityStatisticsSystem m_CityStatisticsSystem;
        private CitySystem m_CitySystem;
        private EndFrameBarrier m_EndFrameBarrier;
        private IconCommandSystem m_IconCommandSystem;
        private SimulationSystem m_SimulationSystem;
        private Time2WorkTimeSystem m_TimeSystem;
        private TriggerSystem m_TriggerSystem;

        private EntityQuery m_DeathCheckQuery;
        private EntityQuery m_HealthcareSettingsQuery;
        private EntityQuery m_TimeDataQuery;
        private EntityQuery m_TimeSettingsQuery;

        private bool m_UseNewCurve = true;
        private TypeHandle __TypeHandle;

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            return 262144 / (kUpdatesPerDay * 16);
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            m_CityStatisticsSystem = World.GetOrCreateSystemManaged<CityStatisticsSystem>();
            m_CitySystem = World.GetOrCreateSystemManaged<CitySystem>();
            m_EndFrameBarrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();
            m_IconCommandSystem = World.GetOrCreateSystemManaged<IconCommandSystem>();
            m_SimulationSystem = World.GetOrCreateSystemManaged<SimulationSystem>();
            m_TimeSystem = World.GetOrCreateSystemManaged<Time2WorkTimeSystem>();
            m_TriggerSystem = World.GetOrCreateSystemManaged<TriggerSystem>();

            m_DeathCheckQuery = GetEntityQuery(
                ComponentType.ReadOnly<Citizen>(),
                ComponentType.ReadOnly<UpdateFrame>(),
                ComponentType.Exclude<Deleted>(),
                ComponentType.Exclude<Temp>());
            m_HealthcareSettingsQuery = GetEntityQuery(ComponentType.ReadOnly<HealthcareParameterData>());
            m_TimeDataQuery = GetEntityQuery(ComponentType.ReadOnly<TimeData>());
            m_TimeSettingsQuery = GetEntityQuery(ComponentType.ReadOnly<TimeSettingsData>());

            RequireForUpdate(m_DeathCheckQuery);
            RequireForUpdate(m_HealthcareSettingsQuery);
            RequireForUpdate(m_TimeDataQuery);
            RequireForUpdate(m_TimeSettingsQuery);
        }

        protected override void OnUpdate()
        {
            HealthcareParameterData healthcareParameterData = m_HealthcareSettingsQuery.GetSingleton<HealthcareParameterData>();
            if (EntityManager.HasEnabledComponent<Locked>(healthcareParameterData.m_HealthcareServicePrefab))
                return;

            uint updateFrame = SimulationUtils.GetUpdateFrame(m_SimulationSystem.frameIndex, kUpdatesPerDay, 16);
            TimeData timeData = m_TimeDataQuery.GetSingleton<TimeData>();

            JobHandle deps;
            DeathCheckJob jobData = new DeathCheckJob()
            {
                m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref CheckedStateRef),
                m_CitizenType = InternalCompilerInterface.GetComponentTypeHandle<Citizen>(ref __TypeHandle.__Game_Citizens_Citizen_RO_ComponentTypeHandle, ref CheckedStateRef),
                m_WorkerType = InternalCompilerInterface.GetComponentTypeHandle<Worker>(ref __TypeHandle.__Game_Citizens_Worker_RO_ComponentTypeHandle, ref CheckedStateRef),
                m_HouseholdMemberType = InternalCompilerInterface.GetComponentTypeHandle<HouseholdMember>(ref __TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentTypeHandle, ref CheckedStateRef),
                m_HealthProblemType = InternalCompilerInterface.GetComponentTypeHandle<HealthProblem>(ref __TypeHandle.__Game_Citizens_HealthProblem_RW_ComponentTypeHandle, ref CheckedStateRef),
                m_UpdateFrameType = InternalCompilerInterface.GetSharedComponentTypeHandle<UpdateFrame>(ref __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, ref CheckedStateRef),
                m_LeisureType = InternalCompilerInterface.GetComponentTypeHandle<Leisure>(ref __TypeHandle.__Game_Citizens_Leisure_RO_ComponentTypeHandle, ref CheckedStateRef),
                m_ResourceBuyerType = InternalCompilerInterface.GetComponentTypeHandle<ResourceBuyer>(ref __TypeHandle.__Game_Companies_ResourceBuyer_RO_ComponentTypeHandle, ref CheckedStateRef),
                m_StudentType = InternalCompilerInterface.GetComponentTypeHandle<Game.Citizens.Student>(ref __TypeHandle.__Game_Citizens_Student_RO_ComponentTypeHandle, ref CheckedStateRef),
                m_CurrentBuildings = InternalCompilerInterface.GetComponentLookup<CurrentBuilding>(ref __TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentLookup, ref CheckedStateRef),
                m_HospitalData = InternalCompilerInterface.GetComponentLookup<Game.Buildings.Hospital>(ref __TypeHandle.__Game_Buildings_Hospital_RO_ComponentLookup, ref CheckedStateRef),
                m_BuildingData = InternalCompilerInterface.GetComponentLookup<Building>(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref CheckedStateRef),
                m_CurrentTransport = InternalCompilerInterface.GetComponentLookup<CurrentTransport>(ref __TypeHandle.__Game_Citizens_CurrentTransport_RO_ComponentLookup, ref CheckedStateRef),
                m_ResidentData = InternalCompilerInterface.GetComponentLookup<Game.Creatures.Resident>(ref __TypeHandle.__Game_Creatures_Resident_RO_ComponentLookup, ref CheckedStateRef),
                m_CityModifiers = InternalCompilerInterface.GetBufferLookup<CityModifier>(ref __TypeHandle.__Game_City_CityModifier_RO_BufferLookup, ref CheckedStateRef),
                m_Students = InternalCompilerInterface.GetBufferLookup<Game.Buildings.Student>(ref __TypeHandle.__Game_Buildings_Student_RO_BufferLookup, ref CheckedStateRef),
                m_HouseholdCitizens = InternalCompilerInterface.GetBufferLookup<HouseholdCitizen>(ref __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup, ref CheckedStateRef),
                m_UpdateFrameIndex = updateFrame,
                m_RandomSeed = RandomSeed.Next(),
                m_HealthcareParameterData = healthcareParameterData,
                m_City = m_CitySystem.City,
                m_StatisticsEventQueue = m_CityStatisticsSystem.GetStatisticsEventQueue(out deps).AsParallelWriter(),
                m_TriggerBuffer = m_TriggerSystem.CreateActionBuffer().AsParallelWriter(),
                m_IconCommandBuffer = m_IconCommandSystem.CreateCommandBuffer(),
                m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
                m_TimeSettings = m_TimeSettingsQuery.GetSingleton<TimeSettingsData>(),
                m_TimeData = timeData,
                m_SimulationFrame = m_SimulationSystem.frameIndex,
                m_NormalizedTime = m_TimeSystem.normalizedTime,
                m_TicksPerDay = math.max(1, Time2WorkTimeSystem.kTicksPerDay),
                m_DaysPerMonth = math.max(1, Mod.m_Setting?.daysPerMonth ?? 1),
                m_SlowTimeFactor = math.max(1f, Mod.m_Setting?.slow_time_factor ?? 1f),
                m_NewDeathRate = m_UseNewCurve
            };

            Dependency = jobData.ScheduleParallel(m_DeathCheckQuery, JobHandle.CombineDependencies(Dependency, deps));
            m_IconCommandSystem.AddCommandBufferWriter(Dependency);
            m_EndFrameBarrier.AddJobHandleForProducer(Dependency);
            m_CityStatisticsSystem.AddWriter(Dependency);
            m_TriggerSystem.AddActionBufferWriter(Dependency);
        }

        public static void PerformAfterDeathActions(
            Entity citizen,
            Entity household,
            NativeQueue<TriggerAction>.ParallelWriter triggerBuffer,
            NativeQueue<StatisticsEvent>.ParallelWriter statisticsEventQueue,
            ref BufferLookup<HouseholdCitizen> householdCitizens)
        {
            triggerBuffer.Enqueue(new TriggerAction(TriggerType.CitizenDied, Entity.Null, citizen, Entity.Null));

            if (household != Entity.Null && householdCitizens.TryGetBuffer(household, out DynamicBuffer<HouseholdCitizen> bufferData))
            {
                for (int index = 0; index < bufferData.Length; ++index)
                {
                    if (bufferData[index].m_Citizen != citizen)
                    {
                        triggerBuffer.Enqueue(new TriggerAction(TriggerType.CitizensFamilyMemberDied, Entity.Null, bufferData[index].m_Citizen, citizen));
                    }
                }
            }

            statisticsEventQueue.Enqueue(new StatisticsEvent()
            {
                m_Statistic = StatisticType.DeathRate,
                m_Change = 1f
            });
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(m_UseNewCurve);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            if (reader.context.format.Has<FormatTags>(FormatTags.EasyModeDeathRateFix))
            {
                reader.Read(out m_UseNewCurve);
            }
            else
            {
                m_UseNewCurve = false;
            }
        }

        public void SetDefaults(Context context)
        {
            m_UseNewCurve = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void __AssignQueries(ref SystemState state)
        {
            new EntityQueryBuilder((AllocatorManager.AllocatorHandle)Allocator.Temp).Dispose();
        }

        protected override void OnCreateForCompiler()
        {
            base.OnCreateForCompiler();
            __AssignQueries(ref CheckedStateRef);
            __TypeHandle.__AssignHandles(ref CheckedStateRef);
        }

        [BurstCompile]
        private struct DeathCheckJob : IJobChunk
        {
            [ReadOnly] public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;
            [ReadOnly] public EntityTypeHandle m_EntityType;
            [ReadOnly] public ComponentTypeHandle<Citizen> m_CitizenType;
            [ReadOnly] public ComponentTypeHandle<Worker> m_WorkerType;
            [ReadOnly] public ComponentTypeHandle<HouseholdMember> m_HouseholdMemberType;
            [ReadOnly] public ComponentTypeHandle<Game.Citizens.Student> m_StudentType;
            [ReadOnly] public ComponentTypeHandle<ResourceBuyer> m_ResourceBuyerType;
            [ReadOnly] public ComponentTypeHandle<Leisure> m_LeisureType;
            public ComponentTypeHandle<HealthProblem> m_HealthProblemType;

            [ReadOnly] public ComponentLookup<CurrentBuilding> m_CurrentBuildings;
            [ReadOnly] public ComponentLookup<Game.Buildings.Hospital> m_HospitalData;
            [ReadOnly] public ComponentLookup<Building> m_BuildingData;
            [ReadOnly] public ComponentLookup<Game.Creatures.Resident> m_ResidentData;
            [ReadOnly] public ComponentLookup<CurrentTransport> m_CurrentTransport;
            [ReadOnly] public BufferLookup<CityModifier> m_CityModifiers;
            [ReadOnly] public BufferLookup<Game.Buildings.Student> m_Students;
            [ReadOnly] public BufferLookup<HouseholdCitizen> m_HouseholdCitizens;

            [ReadOnly] public uint m_UpdateFrameIndex;
            [ReadOnly] public RandomSeed m_RandomSeed;
            [ReadOnly] public HealthcareParameterData m_HealthcareParameterData;
            [ReadOnly] public Entity m_City;

            public NativeQueue<StatisticsEvent>.ParallelWriter m_StatisticsEventQueue;
            public NativeQueue<TriggerAction>.ParallelWriter m_TriggerBuffer;
            public IconCommandBuffer m_IconCommandBuffer;
            public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

            public TimeSettingsData m_TimeSettings;
            public TimeData m_TimeData;
            public uint m_SimulationFrame;
            public float m_NormalizedTime;
            public int m_TicksPerDay;
            public int m_DaysPerMonth;
            public float m_SlowTimeFactor;
            public bool m_NewDeathRate;

            private void Die(
                ArchetypeChunk chunk,
                int chunkIndex,
                int i,
                Entity citizen,
                Entity household,
                NativeArray<Game.Citizens.Student> students,
                NativeArray<HealthProblem> healthProblems)
            {
                if (!healthProblems.IsCreated)
                {
                    m_CommandBuffer.AddComponent(chunkIndex, citizen, new HealthProblem()
                    {
                        m_Flags = HealthProblemFlags.Dead | HealthProblemFlags.RequireTransport
                    });
                }
                else
                {
                    HealthProblem healthProblem = healthProblems[i];
                    if ((healthProblem.m_Flags & HealthProblemFlags.RequireTransport) != HealthProblemFlags.None)
                    {
                        m_IconCommandBuffer.Remove(citizen, m_HealthcareParameterData.m_AmbulanceNotificationPrefab);
                        healthProblem.m_Timer = 0;
                    }

                    healthProblem.m_Flags &= ~(HealthProblemFlags.Sick | HealthProblemFlags.Injured);
                    healthProblem.m_Flags |= HealthProblemFlags.Dead | HealthProblemFlags.RequireTransport;
                    healthProblems[i] = healthProblem;
                }

                PerformAfterDeathActions(citizen, household, m_TriggerBuffer, m_StatisticsEventQueue, ref m_HouseholdCitizens);

                if (chunk.Has<Game.Citizens.Student>(ref m_StudentType))
                {
                    Entity school = students[i].m_School;
                    if (m_Students.HasBuffer(school))
                    {
                        m_CommandBuffer.AddComponent<StudentsRemoved>(chunkIndex, school);
                    }

                    m_CommandBuffer.RemoveComponent<Game.Citizens.Student>(chunkIndex, citizen);
                }

                if (chunk.Has<Worker>(ref m_WorkerType))
                {
                    m_CommandBuffer.RemoveComponent<Worker>(chunkIndex, citizen);
                }

                if (chunk.Has<ResourceBuyer>(ref m_ResourceBuyerType))
                {
                    m_CommandBuffer.RemoveComponent<ResourceBuyer>(chunkIndex, citizen);
                }

                if (chunk.Has<Leisure>(ref m_LeisureType))
                {
                    m_CommandBuffer.RemoveComponent<Leisure>(chunkIndex, citizen);
                }
            }

            private int GetCurrentDay()
            {
                return (int)math.floor((float)(m_SimulationFrame - m_TimeData.m_FirstFrame) / m_TicksPerDay + m_TimeData.TimeOffset);
            }

            public void Execute(
                in ArchetypeChunk chunk,
                int unfilteredChunkIndex,
                bool useEnabledMask,
                in v128 chunkEnabledMask)
            {
                if (chunk.GetSharedComponent(m_UpdateFrameType).m_Index != m_UpdateFrameIndex)
                    return;

                Unity.Mathematics.Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
                DynamicBuffer<CityModifier> cityModifiers = m_CityModifiers[m_City];
                NativeArray<Entity> entities = chunk.GetNativeArray(m_EntityType);
                NativeArray<HealthProblem> healthProblems = chunk.GetNativeArray(ref m_HealthProblemType);
                NativeArray<Citizen> citizens = chunk.GetNativeArray(ref m_CitizenType);
                NativeArray<Game.Citizens.Student> students = chunk.GetNativeArray(ref m_StudentType);
                NativeArray<HouseholdMember> householdMembers = chunk.GetNativeArray(ref m_HouseholdMemberType);
                int currentDay = GetCurrentDay();

                for (int index = 0; index < chunk.Count; ++index)
                {
                    Entity entity = entities[index];
                    Entity household = householdMembers.Length != 0 ? householdMembers[index].m_Household : Entity.Null;
                    Citizen citizen = citizens[index];

                    if (m_CurrentTransport.HasComponent(entity))
                    {
                        CurrentTransport currentTransport = m_CurrentTransport[entity];
                        if (m_ResidentData.HasComponent(currentTransport.m_CurrentTransport) &&
                            (m_ResidentData[currentTransport.m_CurrentTransport].m_Flags & ResidentFlags.InVehicle) != ResidentFlags.None)
                        {
                            continue;
                        }
                    }

                    if (healthProblems.IsCreated && (healthProblems[index].m_Flags & HealthProblemFlags.Dead) != HealthProblemFlags.None)
                        continue;

                    float oldAgeDeathChance = GetOldAgeDeathChance(citizen, currentDay);
                    if (citizen.GetPseudoRandom(CitizenPseudoRandom.Death).NextFloat() < oldAgeDeathChance)
                    {
                        Die(chunk, unfilteredChunkIndex, index, entity, household, students, healthProblems);
                    }
                    else if (healthProblems.IsCreated && (healthProblems[index].m_Flags & (HealthProblemFlags.Sick | HealthProblemFlags.Injured)) != HealthProblemFlags.None)
                    {
                        ProcessHealthProblem(chunk, unfilteredChunkIndex, index, entity, household, citizen, students, healthProblems, random, cityModifiers);
                    }
                }
            }

            private float GetOldAgeDeathChance(Citizen citizen, int currentDay)
            {
                float timeOfDayOffset = m_NormalizedTime - 0.5f;
                float mortalityDaysPerYear = math.max(1f, (float)m_TimeSettings.m_DaysPerYear / math.max(1, m_DaysPerMonth));
                float normalizedAge = ((float)(currentDay - citizen.m_BirthDay) + timeOfDayOffset) / mortalityDaysPerYear / kMaxAgeInGameYear;
                float curveChance = (m_NewDeathRate ? m_HealthcareParameterData.m_DeathRate : m_HealthcareParameterData.m_LegacyDeathRate).Evaluate(normalizedAge);
                return math.saturate(curveChance * math.max(1f, m_SlowTimeFactor));
            }

            private void ProcessHealthProblem(
                ArchetypeChunk chunk,
                int chunkIndex,
                int index,
                Entity entity,
                Entity household,
                Citizen citizen,
                NativeArray<Game.Citizens.Student> students,
                NativeArray<HealthProblem> healthProblems,
                Unity.Mathematics.Random random,
                DynamicBuffer<CityModifier> cityModifiers)
            {
                HealthProblem healthProblem = healthProblems[index];
                int healthPenalty = 10 - citizen.m_Health / 10;
                int deathThreshold = healthPenalty * healthPenalty + 8;

                if (random.NextInt(kUpdatesPerDay * 1000) <= deathThreshold)
                {
                    Die(chunk, chunkIndex, index, entity, household, students, healthProblems);
                    return;
                }

                float recoveryFailChance = MathUtils.Logistic(3f, 1000f, 6f, healthPenalty / 10f - 0.35f);
                int treatmentBonus = 0;
                if (m_CurrentBuildings.HasComponent(entity))
                {
                    Entity currentBuilding = m_CurrentBuildings[entity].m_CurrentBuilding;
                    if (m_BuildingData.HasComponent(currentBuilding) &&
                        !BuildingUtils.CheckOption(m_BuildingData[currentBuilding], BuildingOption.Inactive) &&
                        m_HospitalData.HasComponent(currentBuilding))
                    {
                        treatmentBonus = (int)m_HospitalData[currentBuilding].m_TreatmentBonus;
                    }
                }

                recoveryFailChance -= 10f * treatmentBonus;
                CityUtils.ApplyModifier(ref recoveryFailChance, cityModifiers, CityModifierType.RecoveryFailChange);
                if (random.NextFloat(1000f) >= recoveryFailChance)
                {
                    if ((healthProblem.m_Flags & HealthProblemFlags.RequireTransport) != HealthProblemFlags.None)
                    {
                        m_IconCommandBuffer.Remove(entity, m_HealthcareParameterData.m_AmbulanceNotificationPrefab);
                        healthProblem.m_Timer = 0;
                    }

                    healthProblem.m_Flags &= ~(HealthProblemFlags.Sick | HealthProblemFlags.Injured | HealthProblemFlags.RequireTransport);
                    healthProblems[index] = healthProblem;
                }
            }

            void IJobChunk.Execute(
                in ArchetypeChunk chunk,
                int unfilteredChunkIndex,
                bool useEnabledMask,
                in v128 chunkEnabledMask)
            {
                Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
            }
        }

        private struct TypeHandle
        {
            [ReadOnly] public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;
            [ReadOnly] public ComponentTypeHandle<Citizen> __Game_Citizens_Citizen_RO_ComponentTypeHandle;
            [ReadOnly] public ComponentTypeHandle<Worker> __Game_Citizens_Worker_RO_ComponentTypeHandle;
            [ReadOnly] public ComponentTypeHandle<HouseholdMember> __Game_Citizens_HouseholdMember_RO_ComponentTypeHandle;
            public ComponentTypeHandle<HealthProblem> __Game_Citizens_HealthProblem_RW_ComponentTypeHandle;
            public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;
            [ReadOnly] public ComponentTypeHandle<Leisure> __Game_Citizens_Leisure_RO_ComponentTypeHandle;
            [ReadOnly] public ComponentTypeHandle<ResourceBuyer> __Game_Companies_ResourceBuyer_RO_ComponentTypeHandle;
            [ReadOnly] public ComponentTypeHandle<Game.Citizens.Student> __Game_Citizens_Student_RO_ComponentTypeHandle;
            [ReadOnly] public ComponentLookup<CurrentBuilding> __Game_Citizens_CurrentBuilding_RO_ComponentLookup;
            [ReadOnly] public ComponentLookup<Game.Buildings.Hospital> __Game_Buildings_Hospital_RO_ComponentLookup;
            [ReadOnly] public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;
            [ReadOnly] public ComponentLookup<CurrentTransport> __Game_Citizens_CurrentTransport_RO_ComponentLookup;
            [ReadOnly] public ComponentLookup<Game.Creatures.Resident> __Game_Creatures_Resident_RO_ComponentLookup;
            [ReadOnly] public BufferLookup<CityModifier> __Game_City_CityModifier_RO_BufferLookup;
            [ReadOnly] public BufferLookup<Game.Buildings.Student> __Game_Buildings_Student_RO_BufferLookup;
            [ReadOnly] public BufferLookup<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferLookup;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void __AssignHandles(ref SystemState state)
            {
                __Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
                __Game_Citizens_Citizen_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Citizen>(true);
                __Game_Citizens_Worker_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Worker>(true);
                __Game_Citizens_HouseholdMember_RO_ComponentTypeHandle = state.GetComponentTypeHandle<HouseholdMember>(true);
                __Game_Citizens_HealthProblem_RW_ComponentTypeHandle = state.GetComponentTypeHandle<HealthProblem>();
                __Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
                __Game_Citizens_Leisure_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Leisure>(true);
                __Game_Companies_ResourceBuyer_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ResourceBuyer>(true);
                __Game_Citizens_Student_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Citizens.Student>(true);
                __Game_Citizens_CurrentBuilding_RO_ComponentLookup = state.GetComponentLookup<CurrentBuilding>(true);
                __Game_Buildings_Hospital_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.Hospital>(true);
                __Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(true);
                __Game_Citizens_CurrentTransport_RO_ComponentLookup = state.GetComponentLookup<CurrentTransport>(true);
                __Game_Creatures_Resident_RO_ComponentLookup = state.GetComponentLookup<Game.Creatures.Resident>(true);
                __Game_City_CityModifier_RO_BufferLookup = state.GetBufferLookup<CityModifier>(true);
                __Game_Buildings_Student_RO_BufferLookup = state.GetBufferLookup<Game.Buildings.Student>(true);
                __Game_Citizens_HouseholdCitizen_RO_BufferLookup = state.GetBufferLookup<HouseholdCitizen>(true);
            }
        }
    }
}
