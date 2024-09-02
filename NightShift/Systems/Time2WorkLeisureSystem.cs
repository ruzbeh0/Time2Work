﻿using Game.Agents;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Events;
using Game.Pathfind;
using Game.Prefabs;
using Game.Tools;
using Game.Vehicles;
using Game.Simulation;
using Game.Areas;
using Game;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Game.Objects;
using Game.Routes;
using Time2Work.Systems;

namespace Time2Work
{
    public partial class Time2WorkLeisureSystem : GameSystemBase
    {
        private static readonly int kLeisureConsumeAmount = 2;
        private SimulationSystem m_SimulationSystem;
        private EndFrameBarrier m_EndFrameBarrier;
        private PathfindSetupSystem m_PathFindSetupSystem;
        private Time2WorkTimeSystem m_TimeSystem;
        private ResourceSystem m_ResourceSystem;
        private ClimateSystem m_ClimateSystem;
        private AddMeetingSystem m_AddMeetingSystem;
        private EntityQuery m_LeisureQuery;
        private EntityQuery m_EconomyParameterQuery;
        private EntityQuery m_LeisureParameterQuery;
        private EntityQuery m_ResidentPrefabQuery;
        private EntityQuery m_TimeDataQuery;
        private EntityQuery m_PopulationQuery;
        private ComponentTypeSet m_PathfindTypes;
        private NativeQueue<LeisureEvent> m_LeisureQueue;
        private Time2WorkLeisureSystem.TypeHandle __TypeHandle;
        private Setting.DTSimulationEnum m_daytype;

        public override int GetUpdateInterval(SystemUpdatePhase phase) => 16;

        protected override void OnCreate()
        {
            base.OnCreate();
            this.m_SimulationSystem = this.World.GetOrCreateSystemManaged<SimulationSystem>();
            this.m_EndFrameBarrier = this.World.GetOrCreateSystemManaged<EndFrameBarrier>();
            this.m_PathFindSetupSystem = this.World.GetOrCreateSystemManaged<PathfindSetupSystem>();
            this.m_TimeSystem = this.World.GetOrCreateSystemManaged<Time2WorkTimeSystem>();
            this.m_ResourceSystem = this.World.GetOrCreateSystemManaged<ResourceSystem>();
            this.m_ClimateSystem = this.World.GetOrCreateSystemManaged<ClimateSystem>();
            this.m_AddMeetingSystem = this.World.GetOrCreateSystemManaged<AddMeetingSystem>();
            this.m_EconomyParameterQuery = this.GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
            this.m_LeisureParameterQuery = this.GetEntityQuery(ComponentType.ReadOnly<LeisureParametersData>());
            this.m_LeisureQuery = this.GetEntityQuery(ComponentType.ReadWrite<Citizen>(), ComponentType.ReadWrite<Leisure>(), ComponentType.ReadWrite<TripNeeded>(), ComponentType.ReadWrite<CurrentBuilding>(), ComponentType.Exclude<HealthProblem>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
            this.m_ResidentPrefabQuery = this.GetEntityQuery(ComponentType.ReadOnly<ObjectData>(), ComponentType.ReadOnly<HumanData>(), ComponentType.ReadOnly<ResidentData>(), ComponentType.ReadOnly<PrefabData>());
            this.m_TimeDataQuery = this.GetEntityQuery(ComponentType.ReadOnly<TimeData>());
            this.m_PopulationQuery = this.GetEntityQuery(ComponentType.ReadOnly<Population>());
            this.m_PathfindTypes = new ComponentTypeSet(ComponentType.ReadWrite<PathInformation>(), ComponentType.ReadWrite<PathElement>());
            this.m_LeisureQueue = new NativeQueue<LeisureEvent>((AllocatorManager.AllocatorHandle)Allocator.Persistent);
            this.RequireForUpdate(this.m_LeisureQuery);
            this.RequireForUpdate(this.m_EconomyParameterQuery);
            this.RequireForUpdate(this.m_LeisureParameterQuery);
            this.m_daytype = WeekSystem.currentDayOfTheWeek;
        }

        protected override void OnDestroy()
        {
            this.m_LeisureQueue.Dispose();
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            uint frameWithInterval = SimulationUtils.GetUpdateFrameWithInterval(this.m_SimulationSystem.frameIndex, (uint)this.GetUpdateInterval(SystemUpdatePhase.GameSimulation), 16);
            float num = this.m_ClimateSystem.precipitation.value;
            this.__TypeHandle.__Game_Prefabs_ConsumptionData_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_Renter_RO_BufferLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_City_Population_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Companies_ServiceAvailable_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_TouristHousehold_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_OutsideConnectionData_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_TravelPurpose_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_HumanData_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_CarData_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_Citizen_RW_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Economy_Resources_RO_BufferLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_Household_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_Worker_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_Student_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_LeisureProviderData_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Common_Target_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Vehicles_PersonalCar_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Vehicles_ParkedCar_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_CarKeeper_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_Building_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_ResidentData_RO_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_CreatureData_RO_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_TripNeeded_RW_BufferTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_Leisure_RW_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref this.CheckedStateRef);
            this.m_daytype = WeekSystem.currentDayOfTheWeek;
            JobHandle outJobHandle;
            JobHandle deps;

            JobHandle jobHandle = new Time2WorkLeisureSystem.LeisureJob()
            {
                m_EntityType = this.__TypeHandle.__Unity_Entities_Entity_TypeHandle,
                m_LeisureType = this.__TypeHandle.__Game_Citizens_Leisure_RW_ComponentTypeHandle,
                m_HouseholdMemberType = this.__TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentTypeHandle,
                m_UpdateFrameType = this.__TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle,
                m_TripType = this.__TypeHandle.__Game_Citizens_TripNeeded_RW_BufferTypeHandle,
                m_CreatureDataType = this.__TypeHandle.__Game_Prefabs_CreatureData_RO_ComponentTypeHandle,
                m_ResidentDataType = this.__TypeHandle.__Game_Prefabs_ResidentData_RO_ComponentTypeHandle,
                m_PathInfos = this.__TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentLookup,
                m_CurrentBuildings = this.__TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentLookup,
                m_BuildingData = this.__TypeHandle.__Game_Buildings_Building_RO_ComponentLookup,
                m_PropertyRenters = this.__TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup,
                m_CarKeepers = this.__TypeHandle.__Game_Citizens_CarKeeper_RO_ComponentLookup,
                m_ParkedCarData = this.__TypeHandle.__Game_Vehicles_ParkedCar_RO_ComponentLookup,
                m_PersonalCarData = this.__TypeHandle.__Game_Vehicles_PersonalCar_RO_ComponentLookup,
                m_Targets = this.__TypeHandle.__Game_Common_Target_RO_ComponentLookup,
                m_PrefabRefs = this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup,
                m_LeisureProviderDatas = this.__TypeHandle.__Game_Prefabs_LeisureProviderData_RO_ComponentLookup,
                m_Students = this.__TypeHandle.__Game_Citizens_Student_RO_ComponentLookup,
                m_Workers = this.__TypeHandle.__Game_Citizens_Worker_RO_ComponentLookup,
                m_Households = this.__TypeHandle.__Game_Citizens_Household_RO_ComponentLookup,
                m_Resources = this.__TypeHandle.__Game_Economy_Resources_RO_BufferLookup,
                m_CitizenDatas = this.__TypeHandle.__Game_Citizens_Citizen_RW_ComponentLookup,
                m_Renters = this.__TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup,
                m_PrefabCarData = this.__TypeHandle.__Game_Prefabs_CarData_RO_ComponentLookup,
                m_ObjectGeometryData = this.__TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup,
                m_PrefabHumanData = this.__TypeHandle.__Game_Prefabs_HumanData_RO_ComponentLookup,
                m_Purposes = this.__TypeHandle.__Game_Citizens_TravelPurpose_RO_ComponentLookup,
                m_OutsideConnectionDatas = this.__TypeHandle.__Game_Prefabs_OutsideConnectionData_RO_ComponentLookup,
                m_TouristHouseholds = this.__TypeHandle.__Game_Citizens_TouristHousehold_RO_ComponentLookup,
                m_IndustrialProcesses = this.__TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup,
                m_ServiceAvailables = this.__TypeHandle.__Game_Companies_ServiceAvailable_RO_ComponentLookup,
                m_PopulationData = this.__TypeHandle.__Game_City_Population_RO_ComponentLookup,
                m_HouseholdCitizens = this.__TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup,
                m_RenterBufs = this.__TypeHandle.__Game_Buildings_Renter_RO_BufferLookup,
                m_ConsumptionDatas = this.__TypeHandle.__Game_Prefabs_ConsumptionData_RO_ComponentLookup,
                m_EconomyParameters = this.m_EconomyParameterQuery.GetSingleton<EconomyParameterData>(),
                m_SimulationFrame = this.m_SimulationSystem.frameIndex,
                m_TimeOfDay = this.m_TimeSystem.normalizedTime,
                CommercialPropertyLookup = this.__TypeHandle.CommercialPropertyLookup,
                IndustrialPropertyLookup = this.__TypeHandle.IndustrialPropertyLookup,
                OfficePropertyLookup = this.__TypeHandle.OfficePropertyLookup,
                PropertyRenterLookup = this.__TypeHandle.PropertyRenterLookup,
                PrefabRefLookup = this.__TypeHandle.PrefabRefLookup,
                m_UpdateFrameIndex = frameWithInterval,
                m_Weather = num,
                m_Temperature = ((float)this.m_ClimateSystem.temperature),
                m_RandomSeed = RandomSeed.Next(),
                m_PathfindTypes = this.m_PathfindTypes,
                m_HumanChunks = this.m_ResidentPrefabQuery.ToArchetypeChunkListAsync((AllocatorManager.AllocatorHandle)this.World.UpdateAllocator.ToAllocator, out outJobHandle),
                m_PathfindQueue = this.m_PathFindSetupSystem.GetQueue((object)this, 64).AsParallelWriter(),
                m_CommandBuffer = this.m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
                m_MeetingQueue = this.m_AddMeetingSystem.GetMeetingQueue(out deps).AsParallelWriter(),
                m_LeisureQueue = this.m_LeisureQueue.AsParallelWriter(),
                m_TimeData = this.m_TimeDataQuery.GetSingleton<TimeData>(),
                m_PopulationEntity = this.m_PopulationQuery.GetSingletonEntity(),
                lunch_break_pct = Mod.m_Setting.lunch_break_percentage,
                office_offdayprob = WeekSystem.getOfficeOffDayProb(),
                commercial_offdayprob = WeekSystem.getCommercialOffDayProb(),
                industry_offdayprob = WeekSystem.getIndustryOffDayProb(),
                cityservices_offdayprob = WeekSystem.getCityServicesOffDayProb(),
                school_start_time = (int)Mod.m_Setting.school_start_time,
                school_end_time = (int)Mod.m_Setting.school_end_time,
                work_start_time = (float)Mod.m_Setting.work_start_time,
                work_end_time = (float)Mod.m_Setting.work_end_time,
                delayFactor = (float)(Mod.m_Setting.delay_factor) / 100,
                school_offdayprob = WeekSystem.getSchoolOffDayProb(),
                ticksPerDay = Time2WorkTimeSystem.kTicksPerDay,
                part_time_prob = Mod.m_Setting.part_time_percentage,
                commute_top10 = Mod.m_ModData.commute_top10per,
                dow = this.m_daytype
            }.ScheduleParallel<Time2WorkLeisureSystem.LeisureJob>(this.m_LeisureQuery, JobHandle.CombineDependencies(this.Dependency, JobHandle.CombineDependencies(outJobHandle, deps)));
            this.m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
            this.m_PathFindSetupSystem.AddQueueWriter(jobHandle);
            this.__TypeHandle.__Game_Companies_ServiceCompanyData_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Economy_Resources_RW_BufferLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Companies_ServiceAvailable_RW_ComponentLookup.Update(ref this.CheckedStateRef);

            JobHandle handle = new Time2WorkLeisureSystem.SpendLeisurejob()
            {
                m_ServiceAvailables = this.__TypeHandle.__Game_Companies_ServiceAvailable_RW_ComponentLookup,
                m_Resources = this.__TypeHandle.__Game_Economy_Resources_RW_BufferLookup,
                m_HouseholdMembers = this.__TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentLookup,
                m_IndustrialProcesses = this.__TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup,
                m_Prefabs = this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup,
                m_ResourceDatas = this.__TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup,
                m_ServiceCompanyDatas = this.__TypeHandle.__Game_Companies_ServiceCompanyData_RO_ComponentLookup,
                m_ResourcePrefabs = this.m_ResourceSystem.GetPrefabs(),
                m_LeisureQueue = this.m_LeisureQueue
            }.Schedule<Time2WorkLeisureSystem.SpendLeisurejob>(jobHandle);
            this.m_ResourceSystem.AddPrefabsReader(handle);
            this.Dependency = handle;
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

        public Time2WorkLeisureSystem()
        {
        }

        [BurstCompile]
        private struct SpendLeisurejob : IJob
        {
            public NativeQueue<LeisureEvent> m_LeisureQueue;
            public ComponentLookup<ServiceAvailable> m_ServiceAvailables;
            public BufferLookup<Game.Economy.Resources> m_Resources;
            [ReadOnly]
            public ComponentLookup<PrefabRef> m_Prefabs;
            [ReadOnly]
            public ComponentLookup<IndustrialProcessData> m_IndustrialProcesses;
            [ReadOnly]
            public ComponentLookup<HouseholdMember> m_HouseholdMembers;
            [ReadOnly]
            public ComponentLookup<ResourceData> m_ResourceDatas;
            [ReadOnly]
            public ComponentLookup<ServiceCompanyData> m_ServiceCompanyDatas;
            [ReadOnly]
            public ResourcePrefabs m_ResourcePrefabs;

            public void Execute()
            {
                LeisureEvent leisureEvent;

                while (this.m_LeisureQueue.TryDequeue(out leisureEvent))
                {
                    if (this.m_HouseholdMembers.HasComponent(leisureEvent.m_Citizen) && this.m_Prefabs.HasComponent(leisureEvent.m_Provider))
                    {
                        Entity household = this.m_HouseholdMembers[leisureEvent.m_Citizen].m_Household;
                        Entity prefab = this.m_Prefabs[leisureEvent.m_Provider].m_Prefab;
                        if (this.m_IndustrialProcesses.HasComponent(prefab))
                        {
                            Resource resource1 = this.m_IndustrialProcesses[prefab].m_Output.m_Resource;

                            if (resource1 != Resource.NoResource && this.m_Resources.HasBuffer(leisureEvent.m_Provider) && this.m_Resources.HasBuffer(household))
                            {
                                bool flag = false;

                                float marketPrice = EconomyUtils.GetMarketPrice(resource1, this.m_ResourcePrefabs, ref this.m_ResourceDatas);
                                if (this.m_ServiceAvailables.HasComponent(leisureEvent.m_Provider) && this.m_ServiceCompanyDatas.HasComponent(prefab))
                                {
                                    ServiceAvailable serviceAvailable = this.m_ServiceAvailables[leisureEvent.m_Provider];
                                    ServiceCompanyData serviceCompanyData = this.m_ServiceCompanyDatas[prefab];
                                    marketPrice *= (float)serviceCompanyData.m_ServiceConsuming;
                                    if (serviceAvailable.m_ServiceAvailable > 0)
                                    {
                                        serviceAvailable.m_ServiceAvailable -= serviceCompanyData.m_ServiceConsuming;
                                        serviceAvailable.m_MeanPriority = math.lerp(serviceAvailable.m_MeanPriority, (float)serviceAvailable.m_ServiceAvailable / (float)serviceCompanyData.m_MaxService, 0.1f);
                                        this.m_ServiceAvailables[leisureEvent.m_Provider] = serviceAvailable;
                                        marketPrice *= EconomyUtils.GetServicePriceMultiplier((float)serviceAvailable.m_ServiceAvailable, serviceCompanyData.m_MaxService);
                                    }
                                    else
                                        flag = true;
                                }
                                if (!flag)
                                {
                                    DynamicBuffer<Game.Economy.Resources> resource2 = this.m_Resources[leisureEvent.m_Provider];

                                    if (EconomyUtils.GetResources(resource1, resource2) > Time2WorkLeisureSystem.kLeisureConsumeAmount)
                                    {
                                        DynamicBuffer<Game.Economy.Resources> resource3 = this.m_Resources[household];
                                        EconomyUtils.AddResources(resource1, -Time2WorkLeisureSystem.kLeisureConsumeAmount, resource2);
                                        float f = marketPrice * (float)Time2WorkLeisureSystem.kLeisureConsumeAmount;
                                        EconomyUtils.AddResources(Resource.Money, Mathf.RoundToInt(f), resource2);
                                        EconomyUtils.AddResources(Resource.Money, -Mathf.RoundToInt(f), resource3);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        
        [BurstCompile]
        private struct LeisureJob : IJobChunk
        {
            public ComponentTypeHandle<Leisure> m_LeisureType;
            [ReadOnly]
            public EntityTypeHandle m_EntityType;
            [ReadOnly]
            public ComponentTypeHandle<HouseholdMember> m_HouseholdMemberType;
            [ReadOnly]
            public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;
            public BufferTypeHandle<TripNeeded> m_TripType;
            [ReadOnly]
            public ComponentTypeHandle<CreatureData> m_CreatureDataType;
            [ReadOnly]
            public ComponentTypeHandle<ResidentData> m_ResidentDataType;
            [ReadOnly]
            public ComponentLookup<PathInformation> m_PathInfos;
            [ReadOnly]
            public ComponentLookup<PropertyRenter> m_PropertyRenters;
            [ReadOnly]
            public ComponentLookup<Game.Common.Target> m_Targets;
            [ReadOnly]
            public ComponentLookup<CarKeeper> m_CarKeepers;
            [ReadOnly]
            public ComponentLookup<ParkedCar> m_ParkedCarData;
            [ReadOnly]
            public ComponentLookup<Game.Vehicles.PersonalCar> m_PersonalCarData;
            [ReadOnly]
            public ComponentLookup<CurrentBuilding> m_CurrentBuildings;
            [ReadOnly]
            public ComponentLookup<Building> m_BuildingData;
            [ReadOnly]
            public ComponentLookup<PrefabRef> m_PrefabRefs;
            [ReadOnly]
            public ComponentLookup<LeisureProviderData> m_LeisureProviderDatas;
            [ReadOnly]
            public ComponentLookup<Worker> m_Workers;
            [ReadOnly]
            public ComponentLookup<Game.Citizens.Student> m_Students;
            [ReadOnly]
            public BufferLookup<Game.Economy.Resources> m_Resources;
            [ReadOnly]
            public ComponentLookup<Household> m_Households;
            [ReadOnly]
            public ComponentLookup<PropertyRenter> m_Renters;
            [NativeDisableParallelForRestriction]
            public ComponentLookup<Citizen> m_CitizenDatas;
            [ReadOnly]
            public BufferLookup<HouseholdCitizen> m_HouseholdCitizens;
            [ReadOnly]
            public ComponentLookup<CarData> m_PrefabCarData;
            [ReadOnly]
            public ComponentLookup<ObjectGeometryData> m_ObjectGeometryData;
            [ReadOnly]
            public ComponentLookup<HumanData> m_PrefabHumanData;
            [ReadOnly]
            public ComponentLookup<TravelPurpose> m_Purposes;
            [ReadOnly]
            public ComponentLookup<OutsideConnectionData> m_OutsideConnectionDatas;
            [ReadOnly]
            public ComponentLookup<TouristHousehold> m_TouristHouseholds;
            [ReadOnly]
            public ComponentLookup<IndustrialProcessData> m_IndustrialProcesses;
            [ReadOnly]
            public ComponentLookup<ServiceAvailable> m_ServiceAvailables;
            [ReadOnly]
            public ComponentLookup<Population> m_PopulationData;
            [ReadOnly]
            public BufferLookup<Renter> m_RenterBufs;
            [ReadOnly]
            public ComponentLookup<ConsumptionData> m_ConsumptionDatas;
            [ReadOnly]
            public RandomSeed m_RandomSeed;
            [ReadOnly]
            public ComponentTypeSet m_PathfindTypes;
            [ReadOnly]
            public NativeList<ArchetypeChunk> m_HumanChunks;
            [ReadOnly]
            public ComponentLookup<CommercialProperty> CommercialPropertyLookup;
            [ReadOnly]
            public ComponentLookup<IndustrialProperty> IndustrialPropertyLookup;
            [ReadOnly]
            public ComponentLookup<OfficeProperty> OfficePropertyLookup;
            [ReadOnly]
            public ComponentLookup<PropertyRenter> PropertyRenterLookup;
            [ReadOnly]
            public ComponentLookup<PrefabRef> PrefabRefLookup;
            public EconomyParameterData m_EconomyParameters;
            public EntityCommandBuffer.ParallelWriter m_CommandBuffer;
            public NativeQueue<SetupQueueItem>.ParallelWriter m_PathfindQueue;
            public NativeQueue<LeisureEvent>.ParallelWriter m_LeisureQueue;
            public NativeQueue<AddMeetingSystem.AddMeeting>.ParallelWriter m_MeetingQueue;
            public uint m_SimulationFrame;
            public uint m_UpdateFrameIndex;
            public float m_TimeOfDay;
            public float m_Weather;
            public float m_Temperature;
            public Entity m_PopulationEntity;
            public TimeData m_TimeData;
            public int lunch_break_pct;
            public float4 office_offdayprob;
            public float4 commercial_offdayprob;
            public float4 industry_offdayprob;
            public float4 cityservices_offdayprob;
            public int school_start_time;
            public int school_end_time;
            public float work_start_time;
            public float work_end_time;
            public float delayFactor;
            public float3 school_offdayprob;
            public int ticksPerDay;
            public int part_time_prob;
            public float commute_top10;
            public Setting.DTSimulationEnum dow;

            private void SpendLeisure(
              int index,
              Entity entity,
              ref Citizen citizen,
              ref Leisure leisure,
              Entity providerEntity,
              LeisureProviderData provider)
            {
                bool flag = this.m_BuildingData.HasComponent(providerEntity) && BuildingUtils.CheckOption(this.m_BuildingData[providerEntity], BuildingOption.Inactive);

                if (this.m_ServiceAvailables.HasComponent(providerEntity) && this.m_ServiceAvailables[providerEntity].m_ServiceAvailable <= 0)
                    flag = true;

                Entity prefab = this.m_PrefabRefs[providerEntity].m_Prefab;

                if (!flag && this.m_IndustrialProcesses.HasComponent(prefab))
                {

                    Resource resource = this.m_IndustrialProcesses[prefab].m_Output.m_Resource;
                    if (resource != Resource.NoResource && this.m_Resources.HasBuffer(providerEntity) && EconomyUtils.GetResources(resource, this.m_Resources[providerEntity]) <= 0)
                        flag = true;
                }
                if (!flag)
                {
                    citizen.m_LeisureCounter = (byte)math.min((int)byte.MaxValue, (int)citizen.m_LeisureCounter + provider.m_Efficiency);
                    this.m_LeisureQueue.Enqueue(new LeisureEvent()
                    {
                        m_Citizen = entity,
                        m_Provider = providerEntity
                    });
                }

                if (((citizen.m_LeisureCounter > (byte)250 ? 1 : (this.m_SimulationFrame >= leisure.m_LastPossibleFrame ? 1 : 0)) | (flag ? 1 : 0)) == 0)
                    return;

                this.m_CommandBuffer.RemoveComponent<Leisure>(index, entity);
            }

            public void Execute(
              in ArchetypeChunk chunk,
              int unfilteredChunkIndex,
              bool useEnabledMask,
              in v128 chunkEnabledMask)
            {
                if ((int)chunk.GetSharedComponent<UpdateFrame>(this.m_UpdateFrameType).m_Index != (int)this.m_UpdateFrameIndex)
                    return;
                NativeArray<Entity> nativeArray1 = chunk.GetNativeArray(this.m_EntityType);
                NativeArray<Leisure> nativeArray2 = chunk.GetNativeArray<Leisure>(ref this.m_LeisureType);
                NativeArray<HouseholdMember> nativeArray3 = chunk.GetNativeArray<HouseholdMember>(ref this.m_HouseholdMemberType);
                BufferAccessor<TripNeeded> bufferAccessor = chunk.GetBufferAccessor<TripNeeded>(ref this.m_TripType);
                int population = this.m_PopulationData[this.m_PopulationEntity].m_Population;
                Unity.Mathematics.Random random = this.m_RandomSeed.GetRandom(unfilteredChunkIndex);
                for (int index = 0; index < nativeArray1.Length; ++index)
                {
                    Entity entity1 = nativeArray1[index];
                    Leisure leisure = nativeArray2[index];
                    DynamicBuffer<TripNeeded> dynamicBuffer = bufferAccessor[index];
                    Citizen citizenData = this.m_CitizenDatas[entity1];
                    bool flag = this.m_Purposes.HasComponent(entity1) && this.m_Purposes[entity1].m_Purpose == Game.Citizens.Purpose.Traveling;
                    Entity providerEntity = leisure.m_TargetAgent;
                    Entity entity2 = Entity.Null;
                    LeisureProviderData provider = new LeisureProviderData();
                    if (leisure.m_TargetAgent != Entity.Null && this.m_CurrentBuildings.HasComponent(entity1))
                    {
                        Entity currentBuilding = this.m_CurrentBuildings[entity1].m_CurrentBuilding;
                        if (this.m_PropertyRenters.HasComponent(leisure.m_TargetAgent) && this.m_PropertyRenters[leisure.m_TargetAgent].m_Property == currentBuilding && this.m_PrefabRefs.HasComponent(leisure.m_TargetAgent))
                        {
                            Entity prefab = this.m_PrefabRefs[leisure.m_TargetAgent].m_Prefab;
                            if (this.m_LeisureProviderDatas.HasComponent(prefab))
                            {
                                entity2 = prefab;
                                provider = this.m_LeisureProviderDatas[entity2];
                            }
                        }
                        else
                        {
                            if (this.m_PrefabRefs.HasComponent(currentBuilding))
                            {
                                Entity prefab = this.m_PrefabRefs[currentBuilding].m_Prefab;
                                providerEntity = currentBuilding;
                                if (this.m_LeisureProviderDatas.HasComponent(prefab))
                                {
                                    entity2 = prefab;
                                    provider = this.m_LeisureProviderDatas[entity2];
                                }
                                else
                                {
                                    if (flag && this.m_OutsideConnectionDatas.HasComponent(prefab))
                                    {
                                        entity2 = prefab;
                                        provider = new LeisureProviderData()
                                        {
                                            m_Efficiency = 20,
                                            m_LeisureType = LeisureType.Travel,
                                            m_Resources = Resource.NoResource
                                        };
                                    }
                                }
                            }
                        }
                    }
                    if (entity2 != Entity.Null)
                    {
                        this.SpendLeisure(unfilteredChunkIndex, entity1, ref citizenData, ref leisure, providerEntity, provider);
                        nativeArray2[index] = leisure;
                        this.m_CitizenDatas[entity1] = citizenData;
                    }
                    else
                    {
                       Unity.Mathematics.Random rand = Unity.Mathematics.Random.CreateFromIndex((uint)(citizenData.m_PseudoRandom + Time2WorkTimeSystem.GetDay(this.m_SimulationFrame, this.m_TimeData, ticksPerDay)));
                       int simulate_prob = rand.NextInt(100);
                       if (simulate_prob > (100 - Mod.simulation_reduction))
                       {
                           if(this.m_Purposes.HasComponent(entity1) && (this.m_Purposes[entity1].m_Purpose == Game.Citizens.Purpose.Leisure || this.m_Purposes[entity1].m_Purpose == Game.Citizens.Purpose.Traveling))
                                       {
                               this.m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, entity1);
                           }
                           this.m_CommandBuffer.RemoveComponent<Leisure>(unfilteredChunkIndex, entity1);
                           this.m_CommandBuffer.RemoveComponent(unfilteredChunkIndex, entity1, in this.m_PathfindTypes);
                           continue;
                       }

                        if (!flag && this.m_PathInfos.HasComponent(entity1))
                        {
                            PathInformation pathInfo = this.m_PathInfos[entity1];
                            if ((pathInfo.m_State & PathFlags.Pending) == (PathFlags)0)
                            {
                                Entity destination = pathInfo.m_Destination;
                                if ((this.m_PropertyRenters.HasComponent(destination) || this.m_PrefabRefs.HasComponent(destination)) && !this.m_Targets.HasComponent(entity1))
                                {
                                    float offdayprob = 60f;
                                    int parttime_prob = part_time_prob;
                                    if (m_Workers.TryGetComponent(entity1, out var worker))
                                    {
                                        if (PrefabRefLookup.TryGetComponent(worker.m_Workplace, out var prefab1))
                                        {
                                            if (PropertyRenterLookup.TryGetComponent(worker.m_Workplace, out var propertyRenter))
                                            {
                                                //x = weekday, y = friday, z = saturday, w = sunday
                                                if (CommercialPropertyLookup.HasComponent(propertyRenter.m_Property))
                                                {
                                                    //Mod.log.Info($"Commercial Property");
                                                    if ((int)dow == (int)Setting.DTSimulationEnum.Weekday)
                                                    {
                                                        offdayprob = commercial_offdayprob.x;
                                                    }
                                                    else if ((int)dow == (int)Setting.DTSimulationEnum.AverageDay)
                                                    {
                                                        offdayprob = commercial_offdayprob.y;
                                                    }
                                                    else if ((int)dow == (int)Setting.DTSimulationEnum.Saturday)
                                                    {
                                                        offdayprob = commercial_offdayprob.z;
                                                        parttime_prob = 100;
                                                    }
                                                    else
                                                    {
                                                        offdayprob = commercial_offdayprob.w;
                                                        parttime_prob = 100;
                                                    }
                                                }
                                                if (IndustrialPropertyLookup.HasComponent(propertyRenter.m_Property))
                                                {
                                                    if ((int)dow == (int)Setting.DTSimulationEnum.Weekday)
                                                    {
                                                        offdayprob = industry_offdayprob.x;
                                                    }
                                                    else if ((int)dow == (int)Setting.DTSimulationEnum.AverageDay)
                                                    {
                                                        offdayprob = industry_offdayprob.y;
                                                    }
                                                    else if ((int)dow == (int)Setting.DTSimulationEnum.Saturday)
                                                    {
                                                        offdayprob = industry_offdayprob.z;
                                                        parttime_prob = 100;
                                                    }
                                                    else
                                                    {
                                                        offdayprob = industry_offdayprob.w;
                                                        parttime_prob = 100;
                                                    }
                                                }
                                                if (OfficePropertyLookup.HasComponent(propertyRenter.m_Property))
                                                {
                                                    if ((int)dow == (int)Setting.DTSimulationEnum.Weekday)
                                                    {
                                                        offdayprob = office_offdayprob.x;
                                                    }
                                                    else if ((int)dow == (int)Setting.DTSimulationEnum.AverageDay)
                                                    {
                                                        offdayprob = office_offdayprob.y;
                                                    }
                                                    else if ((int)dow == (int)Setting.DTSimulationEnum.Saturday)
                                                    {
                                                        offdayprob = office_offdayprob.z;
                                                        parttime_prob = 100;
                                                    }
                                                    else
                                                    {
                                                        offdayprob = office_offdayprob.w;
                                                        parttime_prob = 100;
                                                    }
                                                }
                                                else
                                                {
                                                    if ((int)dow == (int)Setting.DTSimulationEnum.Weekday)
                                                    {
                                                        offdayprob = cityservices_offdayprob.x;
                                                    }
                                                    else if ((int)dow == (int)Setting.DTSimulationEnum.AverageDay)
                                                    {
                                                        offdayprob = cityservices_offdayprob.y;
                                                    }
                                                    else if ((int)dow == (int)Setting.DTSimulationEnum.Saturday)
                                                    {
                                                        offdayprob = cityservices_offdayprob.z;
                                                        parttime_prob = 100;
                                                    }
                                                    else
                                                    {
                                                        offdayprob = cityservices_offdayprob.w;
                                                        parttime_prob = 100;
                                                    }
                                                }
                                            }
                                        }

                                    }

                                    if ((!this.m_Workers.HasComponent(entity1) || Time2WorkWorkerSystem.IsTodayOffDay(citizenData, ref this.m_EconomyParameters, this.m_SimulationFrame, this.m_TimeData, population, this.m_TimeOfDay, offdayprob, ticksPerDay) || !Time2WorkWorkerSystem.IsTimeToWork(citizenData, this.m_Workers[entity1], ref this.m_EconomyParameters, this.m_TimeOfDay, lunch_break_pct, work_start_time, work_end_time, delayFactor, ticksPerDay, parttime_prob, commute_top10) ||
                                        (!Time2WorkWorkerSystem.IsTodayOffDay(citizenData, ref this.m_EconomyParameters, this.m_SimulationFrame, this.m_TimeData, population, this.m_TimeOfDay, offdayprob, ticksPerDay) && Time2WorkWorkerSystem.IsTimeToWork(citizenData, this.m_Workers[entity1], ref this.m_EconomyParameters, this.m_TimeOfDay, lunch_break_pct, work_start_time, work_end_time, delayFactor, ticksPerDay, parttime_prob, commute_top10) && Time2WorkWorkerSystem.IsLunchTime(citizenData, this.m_Workers[entity1], ref this.m_EconomyParameters, this.m_TimeOfDay, lunch_break_pct, this.m_SimulationFrame, this.m_TimeData, ticksPerDay))) && (!this.m_Students.HasComponent(entity1) || Time2WorkStudentSystem.IsTimeToStudy(citizenData, this.m_Students[entity1], ref this.m_EconomyParameters, this.m_TimeOfDay, this.m_SimulationFrame, this.m_TimeData, population, school_offdayprob, school_start_time, school_end_time, ticksPerDay)))
                                    {
                                        if (this.m_LeisureProviderDatas[this.m_PrefabRefs[destination].m_Prefab].m_Efficiency == 0)
                                            UnityEngine.Debug.LogWarning((object)string.Format("Warning: Leisure provider {0} has zero efficiency", (object)destination.Index));
                                        leisure.m_TargetAgent = destination;
                                        nativeArray2[index] = leisure;

                                        dynamicBuffer.Add(new TripNeeded()
                                        {
                                            m_TargetAgent = destination,
                                            m_Purpose = Game.Citizens.Purpose.Leisure
                                        });
                                        this.m_CommandBuffer.AddComponent<Game.Common.Target>(unfilteredChunkIndex, entity1, new Game.Common.Target()
                                        {
                                            m_Target = destination
                                        });
                                    }
                                    else
                                    {
                                        if (this.m_Purposes.HasComponent(entity1) && (this.m_Purposes[entity1].m_Purpose == Game.Citizens.Purpose.Leisure || this.m_Purposes[entity1].m_Purpose == Game.Citizens.Purpose.Traveling))
                                        {
                                            this.m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, entity1);
                                        }
                                        this.m_CommandBuffer.RemoveComponent<Leisure>(unfilteredChunkIndex, entity1);
                                        this.m_CommandBuffer.RemoveComponent(unfilteredChunkIndex, entity1, in this.m_PathfindTypes);
                                    }
                                }
                                else
                                {
                                    if (!this.m_Targets.HasComponent(entity1))
                                    {
                                        if (this.m_Purposes.HasComponent(entity1) && (this.m_Purposes[entity1].m_Purpose == Game.Citizens.Purpose.Leisure || this.m_Purposes[entity1].m_Purpose == Game.Citizens.Purpose.Traveling))
                                        {
                                            this.m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, entity1);
                                        }
                                        this.m_CommandBuffer.RemoveComponent<Leisure>(unfilteredChunkIndex, entity1);
                                        this.m_CommandBuffer.RemoveComponent(unfilteredChunkIndex, entity1, in this.m_PathfindTypes);
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (!this.m_Purposes.HasComponent(entity1))
                            {
                                Entity household = nativeArray3[index].m_Household;
                                this.FindLeisure(unfilteredChunkIndex, entity1, household, citizenData, ref random, this.m_TouristHouseholds.HasComponent(household));
                                nativeArray2[index] = leisure;
                            }
                        }
                    }
                }
            }

            private float GetWeight(LeisureType type, int wealth, CitizenAge age)
            {
                float num1 = 1f;
                float num2;
                float a;
                float num3;
                switch (type)
                {
                    case LeisureType.Meals:
                        num2 = 10f;
                        a = 0.2f;
                        switch (age)
                        {
                            case CitizenAge.Child:
                                num3 = 10f;
                                break;
                            case CitizenAge.Teen:
                                num3 = 25f;
                                break;
                            case CitizenAge.Elderly:
                                num3 = 35f;
                                break;
                            default:
                                num3 = 35f;
                                break;
                        }
                        break;
                    case LeisureType.Entertainment:
                        num2 = 10f;
                        a = 0.3f;
                        switch (age)
                        {
                            case CitizenAge.Child:
                                num3 = 0.0f;
                                break;
                            case CitizenAge.Teen:
                                num3 = 45f;
                                break;
                            case CitizenAge.Elderly:
                                num3 = 10f;
                                break;
                            default:
                                num3 = 45f;
                                break;
                        }
                        break;
                    case LeisureType.Commercial:
                        num2 = 10f;
                        a = 0.4f;
                        switch (age)
                        {
                            case CitizenAge.Child:
                                num3 = 20f;
                                break;
                            case CitizenAge.Teen:
                                num3 = 25f;
                                break;
                            case CitizenAge.Elderly:
                                num3 = 25f;
                                break;
                            default:
                                num3 = 30f;
                                break;
                        }
                        break;
                    case LeisureType.CityIndoors:
                    case LeisureType.CityPark:
                    case LeisureType.CityBeach:
                        num2 = 10f;
                        a = 0.0f;
                        switch (age)
                        {
                            case CitizenAge.Child:
                                num3 = 30f;
                                break;
                            case CitizenAge.Teen:
                                num3 = 25f;
                                break;
                            case CitizenAge.Elderly:
                                num3 = 15f;
                                break;
                            default:
                                num3 = 30f;
                                break;
                        }
                        switch (type)
                        {
                            case LeisureType.CityIndoors:
                                num1 = 1f;
                                break;
                            case LeisureType.CityPark:
                                num1 = (float)(2.0 * (1.0 - 0.949999988079071 * (double)this.m_Weather));
                                break;
                            default:
                                num1 = (float)(0.05000000074505806 + 4.0 * (double)math.saturate(0.35f - this.m_Weather) * (double)math.saturate((float)(((double)this.m_Temperature - 20.0) / 30.0)));
                                break;
                        }
                        break;
                    case LeisureType.Travel:
                        num2 = 1f;
                        a = 0.5f;
                        num1 = 0.5f + math.saturate((float)((30.0 - (double)this.m_Temperature) / 50.0));
                        switch (age)
                        {
                            case CitizenAge.Child:
                                num3 = 15f;
                                break;
                            case CitizenAge.Teen:
                                num3 = 15f;
                                break;
                            case CitizenAge.Elderly:
                                num3 = 30f;
                                break;
                            default:
                                num3 = 40f;
                                break;
                        }
                        break;
                    default:
                        num2 = 0.0f;
                        a = 0.0f;
                        num3 = 0.0f;
                        num1 = 0.0f;
                        break;
                }
                return num3 * num1 * num2 * math.smoothstep(a, 1f, (float)(((double)wealth + 5000.0) / 10000.0));
            }

            private LeisureType SelectLeisureType(
              Entity household,
              bool tourist,
              Citizen citizenData,
              ref Unity.Mathematics.Random random)
            {
                PropertyRenter propertyRenter = this.m_Renters.HasComponent(household) ? this.m_Renters[household] : new PropertyRenter();
                if (tourist && (double)random.NextFloat() < 0.30000001192092896)
                    return LeisureType.Attractions;
                if (this.m_Households.HasComponent(household) && this.m_Resources.HasBuffer(household) && this.m_HouseholdCitizens.HasBuffer(household))
                {
                    int wealth = !tourist ? EconomyUtils.GetHouseholdSpendableMoney(this.m_Households[household], this.m_Resources[household], ref this.m_RenterBufs, ref this.m_ConsumptionDatas, ref this.m_PrefabRefs, propertyRenter) : EconomyUtils.GetResources(Resource.Money, this.m_Resources[household]);
                    float num1 = 0.0f;
                    CitizenAge age = citizenData.GetAge();
                    for (int type = 0; type < 10; ++type)
                    {
                        num1 += this.GetWeight((LeisureType)type, wealth, age);
                    }
                    float num2 = num1 * random.NextFloat();
                    for (int type = 0; type < 10; ++type)
                    {
                        num2 -= this.GetWeight((LeisureType)type, wealth, age);
                        if ((double)num2 <= 1.0 / 1000.0)
                            return (LeisureType)type;
                    }
                }
                UnityEngine.Debug.LogWarning((object)"Leisure type randomization failed");
                return LeisureType.Count;
            }

            private void FindLeisure(
              int chunkIndex,
              Entity citizen,
              Entity household,
              Citizen citizenData,
              ref Unity.Mathematics.Random random,
              bool tourist)
            {
                LeisureType leisureType = this.SelectLeisureType(household, tourist, citizenData, ref random);
                float num = (float)byte.MaxValue - (float)citizenData.m_LeisureCounter;
                if (leisureType == LeisureType.Travel || leisureType == LeisureType.Sightseeing || leisureType == LeisureType.Attractions)
                {
                    if (this.m_Purposes.HasComponent(citizen))
                    {
                        this.m_CommandBuffer.RemoveComponent<TravelPurpose>(chunkIndex, citizen);
                    }
                    this.m_MeetingQueue.Enqueue(new AddMeetingSystem.AddMeeting()
                    {
                        m_Household = household,
                        m_Type = leisureType
                    });
                }
                else
                {
                    this.m_CommandBuffer.AddComponent(chunkIndex, citizen, in this.m_PathfindTypes);
                    this.m_CommandBuffer.SetComponent<PathInformation>(chunkIndex, citizen, new PathInformation()
                    {
                        m_State = PathFlags.Pending
                    });
                    CreatureData creatureData;

                    Entity entity = ObjectEmergeSystem.SelectResidentPrefab(citizenData, this.m_HumanChunks, this.m_EntityType, ref this.m_CreatureDataType, ref this.m_ResidentDataType, out creatureData, out PseudoRandomSeed _);
                    HumanData humanData = new HumanData();
                    if (entity != Entity.Null)
                    {
                        humanData = this.m_PrefabHumanData[entity];
                    }
                    Household household1 = this.m_Households[household];
                    DynamicBuffer<HouseholdCitizen> householdCitizen = this.m_HouseholdCitizens[household];
                    PathfindParameters parameters = new PathfindParameters()
                    {
                        m_MaxSpeed = (float2)277.777771f,
                        m_WalkSpeed = (float2)humanData.m_WalkSpeed,
                        m_Weights = CitizenUtils.GetPathfindWeights(citizenData, household1, householdCitizen.Length),
                        m_Methods = PathMethod.Pedestrian | PathMethod.Taxi | RouteUtils.GetPublicTransportMethods(this.m_TimeOfDay),
                        m_SecondaryIgnoredRules = VehicleUtils.GetIgnoredPathfindRulesTaxiDefaults(),
                        m_MaxCost = CitizenBehaviorSystem.kMaxPathfindCost
                    };
                    if (this.m_PropertyRenters.HasComponent(household))
                    {
                        parameters.m_Authorization1 = this.m_PropertyRenters[household].m_Property;
                    }
                    if (this.m_Workers.HasComponent(citizen))
                    {
                        Worker worker = this.m_Workers[citizen];
                        parameters.m_Authorization2 = !this.m_PropertyRenters.HasComponent(worker.m_Workplace) ? worker.m_Workplace : this.m_PropertyRenters[worker.m_Workplace].m_Property;
                    }
                    if (this.m_CarKeepers.IsComponentEnabled(citizen))
                    {
                        Entity car = this.m_CarKeepers[citizen].m_Car;
                        if (this.m_ParkedCarData.HasComponent(car))
                        {
                            PrefabRef prefabRef = this.m_PrefabRefs[car];
                            ParkedCar parkedCar = this.m_ParkedCarData[car];
                            CarData carData = this.m_PrefabCarData[prefabRef.m_Prefab];
                            parameters.m_MaxSpeed.x = carData.m_MaxSpeed;
                            parameters.m_ParkingTarget = parkedCar.m_Lane;
                            parameters.m_ParkingDelta = parkedCar.m_CurvePosition;
                            parameters.m_ParkingLength = VehicleUtils.GetParkingLength(car, ref this.m_PrefabRefs, ref this.m_ObjectGeometryData);
                            parameters.m_Methods |= PathMethod.Road | PathMethod.Parking;
                            parameters.m_IgnoredRules = VehicleUtils.GetIgnoredPathfindRules(carData);
                            Game.Vehicles.PersonalCar componentData;
                            if (this.m_PersonalCarData.TryGetComponent(car, out componentData) && (componentData.m_State & PersonalCarFlags.HomeTarget) == (PersonalCarFlags)0)
                                parameters.m_PathfindFlags |= PathfindFlags.ParkingReset;
                        }
                    }
                    SetupQueueTarget origin = new SetupQueueTarget()
                    {
                        m_Type = SetupTargetType.CurrentLocation,
                        m_Methods = PathMethod.Pedestrian,
                        m_RandomCost = 30f
                    };
                    SetupQueueTarget destination = new SetupQueueTarget()
                    {
                        m_Type = SetupTargetType.Leisure,
                        m_Methods = PathMethod.Pedestrian,
                        m_Value = (int)leisureType,
                        m_Value2 = num,
                        m_RandomCost = 30f,
                        m_ActivityMask = creatureData.m_SupportedActivities
                    };
                    this.m_PathfindQueue.Enqueue(new SetupQueueItem(citizen, parameters, origin, destination));
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
            public ComponentTypeHandle<Leisure> __Game_Citizens_Leisure_RW_ComponentTypeHandle;
            [ReadOnly]
            public ComponentTypeHandle<HouseholdMember> __Game_Citizens_HouseholdMember_RO_ComponentTypeHandle;
            public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;
            public BufferTypeHandle<TripNeeded> __Game_Citizens_TripNeeded_RW_BufferTypeHandle;
            [ReadOnly]
            public ComponentTypeHandle<CreatureData> __Game_Prefabs_CreatureData_RO_ComponentTypeHandle;
            [ReadOnly]
            public ComponentTypeHandle<ResidentData> __Game_Prefabs_ResidentData_RO_ComponentTypeHandle;
            [ReadOnly]
            public ComponentLookup<PathInformation> __Game_Pathfind_PathInformation_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<CurrentBuilding> __Game_Citizens_CurrentBuilding_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<CarKeeper> __Game_Citizens_CarKeeper_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<ParkedCar> __Game_Vehicles_ParkedCar_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Game.Vehicles.PersonalCar> __Game_Vehicles_PersonalCar_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Game.Common.Target> __Game_Common_Target_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<LeisureProviderData> __Game_Prefabs_LeisureProviderData_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Game.Citizens.Student> __Game_Citizens_Student_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Worker> __Game_Citizens_Worker_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Household> __Game_Citizens_Household_RO_ComponentLookup;
            [ReadOnly]
            public BufferLookup<Game.Economy.Resources> __Game_Economy_Resources_RO_BufferLookup;
            public ComponentLookup<Citizen> __Game_Citizens_Citizen_RW_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<CarData> __Game_Prefabs_CarData_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<HumanData> __Game_Prefabs_HumanData_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<TravelPurpose> __Game_Citizens_TravelPurpose_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<OutsideConnectionData> __Game_Prefabs_OutsideConnectionData_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<TouristHousehold> __Game_Citizens_TouristHousehold_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<IndustrialProcessData> __Game_Prefabs_IndustrialProcessData_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<ServiceAvailable> __Game_Companies_ServiceAvailable_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Population> __Game_City_Population_RO_ComponentLookup;
            [ReadOnly]
            public BufferLookup<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferLookup;
            [ReadOnly]
            public BufferLookup<Renter> __Game_Buildings_Renter_RO_BufferLookup;
            [ReadOnly]
            public ComponentLookup<ConsumptionData> __Game_Prefabs_ConsumptionData_RO_ComponentLookup;
            public ComponentLookup<ServiceAvailable> __Game_Companies_ServiceAvailable_RW_ComponentLookup;
            public BufferLookup<Game.Economy.Resources> __Game_Economy_Resources_RW_BufferLookup;
            public ComponentLookup<TaxPayer> __Game_Agents_TaxPayer_RW_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<HouseholdMember> __Game_Citizens_HouseholdMember_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<ServiceCompanyData> __Game_Companies_ServiceCompanyData_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<CommercialProperty> CommercialPropertyLookup;
            [ReadOnly]
            public ComponentLookup<IndustrialProperty> IndustrialPropertyLookup;
            [ReadOnly]
            public ComponentLookup<OfficeProperty> OfficePropertyLookup;
            [ReadOnly]
            public ComponentLookup<PropertyRenter> PropertyRenterLookup;
            [ReadOnly]
            public ComponentLookup<PrefabRef> PrefabRefLookup;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void __AssignHandles(ref SystemState state)
            {
                this.__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
                this.__Game_Citizens_Leisure_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Leisure>();
                this.__Game_Citizens_HouseholdMember_RO_ComponentTypeHandle = state.GetComponentTypeHandle<HouseholdMember>(true);
                this.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
                this.__Game_Citizens_TripNeeded_RW_BufferTypeHandle = state.GetBufferTypeHandle<TripNeeded>();
                this.__Game_Prefabs_CreatureData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CreatureData>(true);
                this.__Game_Prefabs_ResidentData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ResidentData>(true);
                this.__Game_Pathfind_PathInformation_RO_ComponentLookup = state.GetComponentLookup<PathInformation>(true);
                this.__Game_Citizens_CurrentBuilding_RO_ComponentLookup = state.GetComponentLookup<CurrentBuilding>(true);
                this.__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(true);
                this.__Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(true);
                this.__Game_Citizens_CarKeeper_RO_ComponentLookup = state.GetComponentLookup<CarKeeper>(true);
                this.__Game_Vehicles_ParkedCar_RO_ComponentLookup = state.GetComponentLookup<ParkedCar>(true);
                this.__Game_Vehicles_PersonalCar_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.PersonalCar>(true);
                this.__Game_Common_Target_RO_ComponentLookup = state.GetComponentLookup<Game.Common.Target>(true);
                this.__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(true);
                this.__Game_Prefabs_LeisureProviderData_RO_ComponentLookup = state.GetComponentLookup<LeisureProviderData>(true);
                this.__Game_Citizens_Student_RO_ComponentLookup = state.GetComponentLookup<Game.Citizens.Student>(true);
                this.__Game_Citizens_Worker_RO_ComponentLookup = state.GetComponentLookup<Worker>(true);
                this.__Game_Citizens_Household_RO_ComponentLookup = state.GetComponentLookup<Household>(true);
                this.__Game_Economy_Resources_RO_BufferLookup = state.GetBufferLookup<Game.Economy.Resources>(true);
                this.__Game_Citizens_Citizen_RW_ComponentLookup = state.GetComponentLookup<Citizen>();
                this.__Game_Prefabs_CarData_RO_ComponentLookup = state.GetComponentLookup<CarData>(true);
                this.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(true);
                this.__Game_Prefabs_HumanData_RO_ComponentLookup = state.GetComponentLookup<HumanData>(true);
                this.__Game_Citizens_TravelPurpose_RO_ComponentLookup = state.GetComponentLookup<TravelPurpose>(true);
                this.__Game_Prefabs_OutsideConnectionData_RO_ComponentLookup = state.GetComponentLookup<OutsideConnectionData>(true);
                this.__Game_Citizens_TouristHousehold_RO_ComponentLookup = state.GetComponentLookup<TouristHousehold>(true);
                this.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup = state.GetComponentLookup<IndustrialProcessData>(true);
                this.__Game_Companies_ServiceAvailable_RO_ComponentLookup = state.GetComponentLookup<ServiceAvailable>(true);
                this.__Game_City_Population_RO_ComponentLookup = state.GetComponentLookup<Population>(true);
                this.__Game_Citizens_HouseholdCitizen_RO_BufferLookup = state.GetBufferLookup<HouseholdCitizen>(true);
                this.__Game_Companies_ServiceAvailable_RW_ComponentLookup = state.GetComponentLookup<ServiceAvailable>();
                this.__Game_Economy_Resources_RW_BufferLookup = state.GetBufferLookup<Game.Economy.Resources>();
                this.__Game_Citizens_HouseholdMember_RO_ComponentLookup = state.GetComponentLookup<HouseholdMember>(true);
                this.__Game_Companies_ServiceCompanyData_RO_ComponentLookup = state.GetComponentLookup<ServiceCompanyData>(true);
                this.__Game_Buildings_Renter_RO_BufferLookup = state.GetBufferLookup<Renter>(true);
                this.__Game_Prefabs_ConsumptionData_RO_ComponentLookup = state.GetComponentLookup<ConsumptionData>(true);
                this.__Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(true);
                this.__Game_Companies_ServiceCompanyData_RO_ComponentLookup = state.GetComponentLookup<ServiceCompanyData>(true);
                this.CommercialPropertyLookup = state.GetComponentLookup<CommercialProperty>(true);
                this.IndustrialPropertyLookup = state.GetComponentLookup<IndustrialProperty>(true);
                this.OfficePropertyLookup = state.GetComponentLookup<OfficeProperty>(true);
                this.PropertyRenterLookup = state.GetComponentLookup<PropertyRenter>(true);
                this.PrefabRefLookup = state.GetComponentLookup<PrefabRef>(true);
            }
        }
    }
}
