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
        private TaxSystem m_TaxSystem;
        private AddMeetingSystem m_AddMeetingSystem;
        private CountConsumptionSystem m_CountConsumptionSystem;
        private EntityQuery m_LeisureQuery;
        private EntityQuery m_EconomyParameterQuery;
        private EntityQuery m_LeisureParameterQuery;
        private EntityQuery m_ResidentPrefabQuery;
        private EntityQuery m_TimeDataQuery;
        private EntityQuery m_PopulationQuery;
        private ComponentTypeSet m_PathfindTypes;
        private NativeQueue<LeisureEvent> m_LeisureQueue;
        private NativeQueue<ResourceStack> m_ConsumptionQueue;
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
            this.m_TaxSystem = this.World.GetOrCreateSystemManaged<TaxSystem>();
            this.m_AddMeetingSystem = this.World.GetOrCreateSystemManaged<AddMeetingSystem>();
            this.m_CountConsumptionSystem = this.World.GetOrCreateSystemManaged<CountConsumptionSystem>();
            this.m_EconomyParameterQuery = this.GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
            this.m_LeisureParameterQuery = this.GetEntityQuery(ComponentType.ReadOnly<LeisureParametersData>());
            this.m_LeisureQuery = this.GetEntityQuery(ComponentType.ReadWrite<Citizen>(), ComponentType.ReadWrite<Leisure>(), ComponentType.ReadWrite<TripNeeded>(), ComponentType.ReadWrite<CurrentBuilding>(), ComponentType.Exclude<HealthProblem>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
            this.m_ResidentPrefabQuery = this.GetEntityQuery(ComponentType.ReadOnly<ObjectData>(), ComponentType.ReadOnly<HumanData>(), ComponentType.ReadOnly<ResidentData>(), ComponentType.ReadOnly<PrefabData>());
            this.m_TimeDataQuery = this.GetEntityQuery(ComponentType.ReadOnly<TimeData>());
            this.m_PopulationQuery = this.GetEntityQuery(ComponentType.ReadOnly<Population>());
            this.m_PathfindTypes = new ComponentTypeSet(ComponentType.ReadWrite<PathInformation>(), ComponentType.ReadWrite<PathElement>());
            this.m_LeisureQueue = new NativeQueue<LeisureEvent>((AllocatorManager.AllocatorHandle)Allocator.Persistent);
            this.m_ConsumptionQueue = new NativeQueue<ResourceStack>((AllocatorManager.AllocatorHandle)Allocator.Persistent);
            this.RequireForUpdate(this.m_LeisureQuery);
            this.RequireForUpdate(this.m_EconomyParameterQuery);
            this.RequireForUpdate(this.m_LeisureParameterQuery);
            this.m_daytype = WeekSystem.currentDayOfTheWeek;
        }

        protected override void OnDestroy()
        {
            this.m_LeisureQueue.Dispose();
            this.m_ConsumptionQueue.Dispose();
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            uint frameWithInterval = SimulationUtils.GetUpdateFrameWithInterval(this.m_SimulationSystem.frameIndex, (uint)this.GetUpdateInterval(SystemUpdatePhase.GameSimulation), 16);
            float num = this.m_ClimateSystem.precipitation.value;
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
            this.__TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup.Update(ref this.CheckedStateRef);
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
            JobHandle deps1;

            JobHandle jobHandle1 = new Time2WorkLeisureSystem.LeisureJob()
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
                m_Prefabs = this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup,
                m_LeisureProviderDatas = this.__TypeHandle.__Game_Prefabs_LeisureProviderData_RO_ComponentLookup,
                m_Students = this.__TypeHandle.__Game_Citizens_Student_RO_ComponentLookup,
                m_Workers = this.__TypeHandle.__Game_Citizens_Worker_RO_ComponentLookup,
                m_Households = this.__TypeHandle.__Game_Citizens_Household_RO_ComponentLookup,
                m_Resources = this.__TypeHandle.__Game_Economy_Resources_RO_BufferLookup,
                m_CitizenDatas = this.__TypeHandle.__Game_Citizens_Citizen_RW_ComponentLookup,
                m_Renters = this.__TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup,
                m_ResourceDatas = this.__TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup,
                m_HealthProblems = this.__TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentLookup,
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
                m_TaxRates = this.m_TaxSystem.GetTaxRates(),
                m_ResourcePrefabs = this.m_ResourceSystem.GetPrefabs(),
                m_EconomyParameters = this.m_EconomyParameterQuery.GetSingleton<EconomyParameterData>(),
                m_SimulationFrame = this.m_SimulationSystem.frameIndex,
                m_TimeOfDay = this.m_TimeSystem.normalizedTime,
                m_UpdateFrameIndex = frameWithInterval,
                m_BaseConsumptionSum = this.m_ResourceSystem.BaseConsumptionSum,
                m_Weather = num,
                m_Temperature = ((float)this.m_ClimateSystem.temperature),
                m_RandomSeed = RandomSeed.Next(),
                m_PathfindTypes = this.m_PathfindTypes,
                m_HumanChunks = this.m_ResidentPrefabQuery.ToArchetypeChunkListAsync((AllocatorManager.AllocatorHandle)this.World.UpdateAllocator.ToAllocator, out outJobHandle),
                m_PathfindQueue = this.m_PathFindSetupSystem.GetQueue((object)this, 64).AsParallelWriter(),
                m_CommandBuffer = this.m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
                m_MeetingQueue = this.m_AddMeetingSystem.GetMeetingQueue(out deps1).AsParallelWriter(),
                m_LeisureQueue = this.m_LeisureQueue.AsParallelWriter(),
                m_TimeData = this.m_TimeDataQuery.GetSingleton<TimeData>(),
                m_PopulationEntity = this.m_PopulationQuery.GetSingletonEntity(),
                m_ConsumptionQueue = this.m_ConsumptionQueue.AsParallelWriter(),
                lunch_break_pct = Mod.m_Setting.lunch_break_percentage,
                offdayprob  = WeekSystem.getOffDayProb(),
                school_start_time = (int)Mod.m_Setting.school_start_time,
                school_end_time = (int)Mod.m_Setting.school_end_time,
                work_start_time = (float)Mod.m_Setting.work_start_time,
                work_end_time = (float)Mod.m_Setting.work_end_time,
                delayFactor = (float)(Mod.m_Setting.delay_factor) / 100,
                school_offdayprob = WeekSystem.getSchoolOffDayProb(),
                ticksPerDay = Time2WorkTimeSystem.kTicksPerDay,
                part_time_prob = Mod.m_Setting.part_time_percentage
            }.ScheduleParallel<Time2WorkLeisureSystem.LeisureJob>(this.m_LeisureQuery, JobHandle.CombineDependencies(this.Dependency, JobHandle.CombineDependencies(outJobHandle, deps1)));
            this.m_EndFrameBarrier.AddJobHandleForProducer(jobHandle1);
            this.m_PathFindSetupSystem.AddQueueWriter(jobHandle1);
            this.m_TaxSystem.AddReader(jobHandle1);
            this.__TypeHandle.__Game_Companies_WorkProvider_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_WorkplaceData_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Companies_TradeCost_RO_BufferLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Objects_OutsideConnection_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Companies_Employee_RO_BufferLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Areas_CurrentDistrict_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Areas_DistrictModifier_RO_BufferLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_Efficiency_RO_BufferLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Companies_ServiceCompanyData_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Agents_TaxPayer_RW_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Economy_Resources_RW_BufferLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Companies_ServiceAvailable_RW_ComponentLookup.Update(ref this.CheckedStateRef);
            JobHandle deps2;

            JobHandle jobHandle2 = new Time2WorkLeisureSystem.SpendLeisurejob()
            {
                m_ServiceAvailables = this.__TypeHandle.__Game_Companies_ServiceAvailable_RW_ComponentLookup,
                m_Resources = this.__TypeHandle.__Game_Economy_Resources_RW_BufferLookup,
                m_TaxPayers = this.__TypeHandle.__Game_Agents_TaxPayer_RW_ComponentLookup,
                m_HouseholdMembers = this.__TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentLookup,
                m_IndustrialProcesses = this.__TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup,
                m_Prefabs = this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup,
                m_ResourceDatas = this.__TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup,
                m_ServiceCompanyDatas = this.__TypeHandle.__Game_Companies_ServiceCompanyData_RO_ComponentLookup,
                m_BuildingDatas = this.__TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup,
                m_BuildingEfficiencies = this.__TypeHandle.__Game_Buildings_Efficiency_RO_BufferLookup,
                m_DistrictModifiers = this.__TypeHandle.__Game_Areas_DistrictModifier_RO_BufferLookup,
                m_Districts = this.__TypeHandle.__Game_Areas_CurrentDistrict_RO_ComponentLookup,
                m_Employees = this.__TypeHandle.__Game_Companies_Employee_RO_BufferLookup,
                m_OutsideConnections = this.__TypeHandle.__Game_Objects_OutsideConnection_RO_ComponentLookup,
                m_ProcessDatas = this.__TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup,
                m_PropertyRenters = this.__TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup,
                m_Spawnables = this.__TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup,
                m_TradeCosts = this.__TypeHandle.__Game_Companies_TradeCost_RO_BufferLookup,
                m_WorkplaceDatas = this.__TypeHandle.__Game_Prefabs_WorkplaceData_RO_ComponentLookup,
                m_WorkProviders = this.__TypeHandle.__Game_Companies_WorkProvider_RO_ComponentLookup,
                m_TaxRates = this.m_TaxSystem.GetTaxRates(),
                m_EconomyParameters = this.m_EconomyParameterQuery.GetSingleton<EconomyParameterData>(),
                m_ResourcePrefabs = this.m_ResourceSystem.GetPrefabs(),
                m_LeisureQueue = this.m_LeisureQueue,
                m_ConsumptionQueue = this.m_ConsumptionQueue,
                m_ConsumptionAccumulator = this.m_CountConsumptionSystem.GetConsumptionAccumulator(out deps2)
            }.Schedule<Time2WorkLeisureSystem.SpendLeisurejob>(JobHandle.CombineDependencies(deps2, jobHandle1));
            this.m_CountConsumptionSystem.AddConsumptionWriter(jobHandle2);
            this.m_ResourceSystem.AddPrefabsReader(jobHandle2);
            this.m_TaxSystem.AddReader(jobHandle2);
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

        public Time2WorkLeisureSystem()
        {
        }

        [BurstCompile]
        private struct SpendLeisurejob : IJob
        {
            public NativeQueue<LeisureEvent> m_LeisureQueue;
            public ComponentLookup<ServiceAvailable> m_ServiceAvailables;
            public BufferLookup<Game.Economy.Resources> m_Resources;
            public ComponentLookup<TaxPayer> m_TaxPayers;
            [ReadOnly]
            public BufferLookup<TradeCost> m_TradeCosts;
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
            public ComponentLookup<PropertyRenter> m_PropertyRenters;
            [ReadOnly]
            public ComponentLookup<Game.Prefabs.BuildingData> m_BuildingDatas;
            [ReadOnly]
            public ComponentLookup<IndustrialProcessData> m_ProcessDatas;
            [ReadOnly]
            public BufferLookup<Employee> m_Employees;
            [ReadOnly]
            public ComponentLookup<WorkplaceData> m_WorkplaceDatas;
            [ReadOnly]
            public ComponentLookup<SpawnableBuildingData> m_Spawnables;
            [ReadOnly]
            public BufferLookup<Efficiency> m_BuildingEfficiencies;
            [ReadOnly]
            public ComponentLookup<WorkProvider> m_WorkProviders;
            [ReadOnly]
            public ComponentLookup<Game.Objects.OutsideConnection> m_OutsideConnections;
            [ReadOnly]
            public ComponentLookup<CurrentDistrict> m_Districts;
            [ReadOnly]
            public BufferLookup<DistrictModifier> m_DistrictModifiers;
            [ReadOnly]
            public ResourcePrefabs m_ResourcePrefabs;
            public EconomyParameterData m_EconomyParameters;
            [ReadOnly]
            public NativeArray<int> m_TaxRates;
            public NativeQueue<ResourceStack> m_ConsumptionQueue;
            public NativeArray<int> m_ConsumptionAccumulator;

            public void Execute()
            {
                LeisureEvent leisureEvent;

                while (this.m_LeisureQueue.TryDequeue(out leisureEvent))
                {
                    if (this.m_HouseholdMembers.HasComponent(leisureEvent.m_Citizen) && this.m_Prefabs.HasComponent(leisureEvent.m_Provider))
                    {
                        Entity household = this.m_HouseholdMembers[leisureEvent.m_Citizen].m_Household;
                        Entity prefab1 = this.m_Prefabs[leisureEvent.m_Provider].m_Prefab;
                        if (this.m_IndustrialProcesses.HasComponent(prefab1))
                        {
                            IndustrialProcessData industrialProcess = this.m_IndustrialProcesses[prefab1];
                            Resource resource1 = industrialProcess.m_Output.m_Resource;

                            if (resource1 != Resource.NoResource && this.m_Resources.HasBuffer(leisureEvent.m_Provider) && this.m_Resources.HasBuffer(household))
                            {
                                bool flag = false;

                                float marketPrice = EconomyUtils.GetMarketPrice(resource1, this.m_ResourcePrefabs, ref this.m_ResourceDatas);
                                if (this.m_ServiceAvailables.HasComponent(leisureEvent.m_Provider) && this.m_ServiceCompanyDatas.HasComponent(prefab1))
                                {
                                    ServiceAvailable serviceAvailable = this.m_ServiceAvailables[leisureEvent.m_Provider];
                                    ServiceCompanyData serviceCompanyData = this.m_ServiceCompanyDatas[prefab1];
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

                                        if (this.m_TaxPayers.HasComponent(leisureEvent.m_Provider) && this.m_PropertyRenters.HasComponent(leisureEvent.m_Provider) && this.m_WorkProviders.HasComponent(leisureEvent.m_Provider))
                                        {
                                            Entity property = this.m_PropertyRenters[leisureEvent.m_Provider].m_Property;
                                            Entity prefab2 = this.m_Prefabs[property].m_Prefab;
                                            Game.Prefabs.BuildingData buildingData = this.m_BuildingDatas[prefab2];
                                            DynamicBuffer<Employee> employee = this.m_Employees[leisureEvent.m_Provider];
                                            WorkplaceData workplaceData = this.m_WorkplaceDatas[prefab1];
                                            SpawnableBuildingData spawnable = this.m_Spawnables[prefab2];
                                            WorkProvider workProvider = this.m_WorkProviders[leisureEvent.m_Provider];
                                            int commercialTaxRate;
                                            if (this.m_Districts.HasComponent(property))
                                            {
                                                Entity district = this.m_Districts[property].m_District;
                                                commercialTaxRate = TaxSystem.GetModifiedCommercialTaxRate(industrialProcess.m_Output.m_Resource, this.m_TaxRates, district, this.m_DistrictModifiers);
                                            }
                                            else
                                            {
                                                commercialTaxRate = TaxSystem.GetCommercialTaxRate(industrialProcess.m_Output.m_Resource, this.m_TaxRates);
                                            }
                                            TaxPayer taxPayer = this.m_TaxPayers[leisureEvent.m_Provider];
                                            if (this.m_ServiceAvailables.HasComponent(leisureEvent.m_Provider))
                                            {
                                                ServiceCompanyData serviceCompanyData = this.m_ServiceCompanyDatas[prefab1];
                                                double efficiency = (double)BuildingUtils.GetEfficiency(property, ref this.m_BuildingEfficiencies);
                                                DynamicBuffer<TradeCost> tradeCost = this.m_TradeCosts[leisureEvent.m_Provider];
                                                float num1 = (float)ServiceCompanySystem.EstimateDailyProduction((float)efficiency, workProvider.m_MaxWorkers, (int)spawnable.m_Level, serviceCompanyData, workplaceData, ref this.m_EconomyParameters, industrialProcess.m_Output.m_Resource, buildingData.m_LotSize.x * buildingData.m_LotSize.y);

                                                float num2 = (float)ServiceCompanySystem.EstimateDailyProfit((float)efficiency, workProvider.m_MaxWorkers, employee, this.m_ServiceAvailables[leisureEvent.m_Provider], serviceCompanyData, buildingData, industrialProcess, ref this.m_EconomyParameters, workplaceData, spawnable, this.m_ResourcePrefabs, this.m_ResourceDatas, tradeCost);
                                                if ((double)num1 > 0.0)
                                                {
                                                    int num3 = Mathf.CeilToInt(math.max(0.0f, num2 / num1));
                                                    taxPayer.m_UntaxedIncome += num3;
                                                    if (num3 > 0)
                                                        taxPayer.m_AverageTaxRate = Mathf.RoundToInt(math.lerp((float)taxPayer.m_AverageTaxRate, (float)commercialTaxRate, (float)num3 / (float)(num3 + taxPayer.m_UntaxedIncome)));
                                                    this.m_TaxPayers[leisureEvent.m_Provider] = taxPayer;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                ResourceStack resourceStack;
                while (this.m_ConsumptionQueue.TryDequeue(out resourceStack))
                {
                    this.m_ConsumptionAccumulator[EconomyUtils.GetResourceIndex(resourceStack.m_Resource)] += resourceStack.m_Amount;
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
            public ComponentLookup<PrefabRef> m_Prefabs;
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
            public ComponentLookup<ResourceData> m_ResourceDatas;
            [ReadOnly]
            public ComponentLookup<HealthProblem> m_HealthProblems;
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
            public NativeArray<int> m_TaxRates;
            [ReadOnly]
            public ResourcePrefabs m_ResourcePrefabs;
            [ReadOnly]
            public RandomSeed m_RandomSeed;
            [ReadOnly]
            public ComponentTypeSet m_PathfindTypes;
            [ReadOnly]
            public NativeList<ArchetypeChunk> m_HumanChunks;
            public NativeQueue<ResourceStack>.ParallelWriter m_ConsumptionQueue;
            public EconomyParameterData m_EconomyParameters;
            public EntityCommandBuffer.ParallelWriter m_CommandBuffer;
            public NativeQueue<SetupQueueItem>.ParallelWriter m_PathfindQueue;
            public NativeQueue<LeisureEvent>.ParallelWriter m_LeisureQueue;
            public NativeQueue<AddMeetingSystem.AddMeeting>.ParallelWriter m_MeetingQueue;
            public uint m_SimulationFrame;
            public uint m_UpdateFrameIndex;
            public float m_TimeOfDay;
            public int m_BaseConsumptionSum;
            public float m_Weather;
            public float m_Temperature;
            public Entity m_PopulationEntity;
            public TimeData m_TimeData;
            public int lunch_break_pct;
            public float offdayprob;
            public int school_start_time;
            public int school_end_time;
            public float work_start_time;
            public float work_end_time;
            public float delayFactor;
            public float school_offdayprob;
            public int ticksPerDay;
            public int part_time_prob;

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

                Entity prefab = this.m_Prefabs[providerEntity].m_Prefab;

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
                        if (this.m_PropertyRenters.HasComponent(leisure.m_TargetAgent) && this.m_PropertyRenters[leisure.m_TargetAgent].m_Property == currentBuilding && this.m_Prefabs.HasComponent(leisure.m_TargetAgent))
                        {
                            Entity prefab = this.m_Prefabs[leisure.m_TargetAgent].m_Prefab;
                            if (this.m_LeisureProviderDatas.HasComponent(prefab))
                            {
                                entity2 = prefab;
                                provider = this.m_LeisureProviderDatas[entity2];
                            }
                        }
                        else
                        {
                            if (this.m_Prefabs.HasComponent(currentBuilding))
                            {
                                Entity prefab = this.m_Prefabs[currentBuilding].m_Prefab;
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
                        if (!flag && this.m_PathInfos.HasComponent(entity1))
                        {
                            PathInformation pathInfo = this.m_PathInfos[entity1];
                            if ((pathInfo.m_State & PathFlags.Pending) == (PathFlags)0)
                            {
                                Entity destination = pathInfo.m_Destination;
                                if ((this.m_PropertyRenters.HasComponent(destination) || this.m_Prefabs.HasComponent(destination)) && !this.m_Targets.HasComponent(entity1))
                                {
                                    if ((!this.m_Workers.HasComponent(entity1) || Time2WorkWorkerSystem.IsTodayOffDay(citizenData, ref this.m_EconomyParameters, this.m_SimulationFrame, this.m_TimeData, population, this.m_TimeOfDay, offdayprob, ticksPerDay) || !Time2WorkWorkerSystem.IsTimeToWork(citizenData, this.m_Workers[entity1], ref this.m_EconomyParameters, this.m_TimeOfDay, lunch_break_pct, work_start_time, work_end_time, delayFactor, ticksPerDay, part_time_prob) ||
                                        (!Time2WorkWorkerSystem.IsTodayOffDay(citizenData, ref this.m_EconomyParameters, this.m_SimulationFrame, this.m_TimeData, population, this.m_TimeOfDay, offdayprob, ticksPerDay) && Time2WorkWorkerSystem.IsTimeToWork(citizenData, this.m_Workers[entity1], ref this.m_EconomyParameters, this.m_TimeOfDay, lunch_break_pct, work_start_time, work_end_time, delayFactor, ticksPerDay, part_time_prob) && Time2WorkWorkerSystem.IsLunchTime(citizenData, this.m_Workers[entity1], ref this.m_EconomyParameters, this.m_TimeOfDay, lunch_break_pct, this.m_SimulationFrame, this.m_TimeData, ticksPerDay))) && (!this.m_Students.HasComponent(entity1) || Time2WorkStudentSystem.IsTimeToStudy(citizenData, this.m_Students[entity1], ref this.m_EconomyParameters, this.m_TimeOfDay, this.m_SimulationFrame, this.m_TimeData, population, school_offdayprob, school_start_time, school_end_time, ticksPerDay)))
                                    {
                                        LeisureProviderData leisureProviderData = this.m_LeisureProviderDatas[this.m_Prefabs[destination].m_Prefab];
                                        if (leisureProviderData.m_Efficiency == 0)
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
                                        if (this.m_ServiceAvailables.HasComponent(destination))
                                        {
                                            IndustrialProcessData industrialProcess = this.m_IndustrialProcesses[this.m_Prefabs[destination].m_Prefab];
                                            this.m_ConsumptionQueue.Enqueue(new ResourceStack()
                                            {
                                                m_Resource = industrialProcess.m_Output.m_Resource,
                                                m_Amount = ((int)byte.MaxValue - (int)citizenData.m_LeisureCounter) / math.max(1, leisureProviderData.m_Efficiency)
                                            });
                                        }
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
                    int wealth = !tourist ? EconomyUtils.GetHouseholdWealth(household, this.m_Households[household], this.m_Resources[household], this.m_HouseholdCitizens[household], ref this.m_Workers, ref this.m_CitizenDatas, ref this.m_HealthProblems, propertyRenter, ref this.m_EconomyParameters, this.m_ResourcePrefabs, ref this.m_ResourceDatas, this.m_BaseConsumptionSum, this.m_TaxRates) : EconomyUtils.GetResources(Resource.Money, this.m_Resources[household]);
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
                        m_Weights = 0.01f * CitizenUtils.GetPathfindWeights(citizenData, household1, householdCitizen.Length),
                        m_Methods = PathMethod.Pedestrian | PathMethod.Taxi | RouteUtils.GetPublicTransportMethods(this.m_TimeOfDay),
                        m_SecondaryIgnoredFlags = VehicleUtils.GetIgnoredPathfindFlagsTaxiDefaults(),
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
                            PrefabRef prefab = this.m_Prefabs[car];
                            ParkedCar parkedCar = this.m_ParkedCarData[car];
                            CarData carData = this.m_PrefabCarData[prefab.m_Prefab];
                            parameters.m_MaxSpeed.x = carData.m_MaxSpeed;
                            parameters.m_ParkingTarget = parkedCar.m_Lane;
                            parameters.m_ParkingDelta = parkedCar.m_CurvePosition;
                            parameters.m_ParkingLength = VehicleUtils.GetParkingLength(car, ref this.m_Prefabs, ref this.m_ObjectGeometryData);
                            parameters.m_Methods |= PathMethod.Road | PathMethod.Parking;
                            parameters.m_IgnoredFlags = VehicleUtils.GetIgnoredPathfindFlags(carData);
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
            public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<HealthProblem> __Game_Citizens_HealthProblem_RO_ComponentLookup;
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
            public ComponentLookup<ServiceAvailable> __Game_Companies_ServiceAvailable_RW_ComponentLookup;
            public BufferLookup<Game.Economy.Resources> __Game_Economy_Resources_RW_BufferLookup;
            public ComponentLookup<TaxPayer> __Game_Agents_TaxPayer_RW_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<HouseholdMember> __Game_Citizens_HouseholdMember_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<ServiceCompanyData> __Game_Companies_ServiceCompanyData_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Game.Prefabs.BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;
            [ReadOnly]
            public BufferLookup<Efficiency> __Game_Buildings_Efficiency_RO_BufferLookup;
            [ReadOnly]
            public BufferLookup<DistrictModifier> __Game_Areas_DistrictModifier_RO_BufferLookup;
            [ReadOnly]
            public ComponentLookup<CurrentDistrict> __Game_Areas_CurrentDistrict_RO_ComponentLookup;
            [ReadOnly]
            public BufferLookup<Employee> __Game_Companies_Employee_RO_BufferLookup;
            [ReadOnly]
            public ComponentLookup<Game.Objects.OutsideConnection> __Game_Objects_OutsideConnection_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;
            [ReadOnly]
            public BufferLookup<TradeCost> __Game_Companies_TradeCost_RO_BufferLookup;
            [ReadOnly]
            public ComponentLookup<WorkplaceData> __Game_Prefabs_WorkplaceData_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<WorkProvider> __Game_Companies_WorkProvider_RO_ComponentLookup;

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
                this.__Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(true);
                this.__Game_Citizens_HealthProblem_RO_ComponentLookup = state.GetComponentLookup<HealthProblem>(true);
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
                this.__Game_Agents_TaxPayer_RW_ComponentLookup = state.GetComponentLookup<TaxPayer>();
                this.__Game_Citizens_HouseholdMember_RO_ComponentLookup = state.GetComponentLookup<HouseholdMember>(true);
                this.__Game_Companies_ServiceCompanyData_RO_ComponentLookup = state.GetComponentLookup<ServiceCompanyData>(true);
                this.__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<Game.Prefabs.BuildingData>(true);
                this.__Game_Buildings_Efficiency_RO_BufferLookup = state.GetBufferLookup<Efficiency>(true);
                this.__Game_Areas_DistrictModifier_RO_BufferLookup = state.GetBufferLookup<DistrictModifier>(true);
                this.__Game_Areas_CurrentDistrict_RO_ComponentLookup = state.GetComponentLookup<CurrentDistrict>(true);
                this.__Game_Companies_Employee_RO_BufferLookup = state.GetBufferLookup<Employee>(true);
                this.__Game_Objects_OutsideConnection_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.OutsideConnection>(true);
                this.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup = state.GetComponentLookup<SpawnableBuildingData>(true);
                this.__Game_Companies_TradeCost_RO_BufferLookup = state.GetBufferLookup<TradeCost>(true);
                this.__Game_Prefabs_WorkplaceData_RO_ComponentLookup = state.GetComponentLookup<WorkplaceData>(true);
                this.__Game_Companies_WorkProvider_RO_ComponentLookup = state.GetComponentLookup<WorkProvider>(true);
            }
        }
    }
}
