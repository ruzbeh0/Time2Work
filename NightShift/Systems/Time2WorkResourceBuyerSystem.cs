
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Net;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Routes;
using Game.Tools;
using Game.Vehicles;
using Game.Simulation;
using System;
using System.Runtime.CompilerServices;
using Unity.Assertions;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Game;

#nullable disable
namespace Time2Work.Systems;

//[CompilerGenerated]
public partial class Time2WorkResourceBuyerSystem : GameSystemBase
{
    private const int UPDATE_INTERVAL = 16 /*0x10*/;
    private EntityQuery m_BuyerQuery;
    private EntityQuery m_CarPrefabQuery;
    private EntityQuery m_EconomyParameterQuery;
    private EntityQuery m_ResidentPrefabQuery;
    private EntityQuery m_PopulationQuery;
    private ComponentTypeSet m_PathfindTypes;
    private EndFrameBarrier m_EndFrameBarrier;
    private PathfindSetupSystem m_PathfindSetupSystem;
    private ResourceSystem m_ResourceSystem;
    private SimulationSystem m_SimulationSystem;
    private TaxSystem m_TaxSystem;
    private TimeSystem m_TimeSystem;
    private CityConfigurationSystem m_CityConfigurationSystem;
    private PersonalCarSelectData m_PersonalCarSelectData;
    private CitySystem m_CitySystem;
    private CityProductionStatisticSystem m_CityProductionStatisticSystem;
    private NativeQueue<Time2WorkResourceBuyerSystem.SalesEvent> m_SalesQueue;
    private Time2WorkResourceBuyerSystem.TypeHandle __TypeHandle;
    private EntityArchetype m_GoodsDeliveryRequestArchetype;


    public override int GetUpdateInterval(SystemUpdatePhase phase) => 16 /*0x10*/;

    [UnityEngine.Scripting.Preserve]
    protected override void OnCreate()
    {
        base.OnCreate();
        // ISSUE: reference to a compiler-generated field
        this.m_EndFrameBarrier = this.World.GetOrCreateSystemManaged<EndFrameBarrier>();
        // ISSUE: reference to a compiler-generated field
        this.m_PathfindSetupSystem = this.World.GetOrCreateSystemManaged<PathfindSetupSystem>();
        // ISSUE: reference to a compiler-generated field
        this.m_ResourceSystem = this.World.GetOrCreateSystemManaged<ResourceSystem>();
        // ISSUE: reference to a compiler-generated field
        this.m_TaxSystem = this.World.GetOrCreateSystemManaged<TaxSystem>();
        // ISSUE: reference to a compiler-generated field
        this.m_TimeSystem = this.World.GetOrCreateSystemManaged<TimeSystem>();
        // ISSUE: reference to a compiler-generated field
        this.m_CityConfigurationSystem = this.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
        // ISSUE: reference to a compiler-generated field
        this.m_PersonalCarSelectData = new PersonalCarSelectData((SystemBase)this);
        // ISSUE: reference to a compiler-generated field
        this.m_CitySystem = this.World.GetOrCreateSystemManaged<CitySystem>();
        // ISSUE: reference to a compiler-generated field
        this.m_CityProductionStatisticSystem = this.World.GetOrCreateSystemManaged<CityProductionStatisticSystem>();
        // ISSUE: reference to a compiler-generated field
        this.m_SimulationSystem = this.World.GetOrCreateSystemManaged<SimulationSystem>();
        // ISSUE: reference to a compiler-generated field
        this.m_SalesQueue = new NativeQueue<Time2WorkResourceBuyerSystem.SalesEvent>((AllocatorManager.AllocatorHandle)Allocator.Persistent);
        // ISSUE: reference to a compiler-generated field
        this.m_BuyerQuery = this.GetEntityQuery(new EntityQueryDesc()
        {
            All = new ComponentType[2]
          {
        ComponentType.ReadWrite<ResourceBuyer>(),
        ComponentType.ReadWrite<TripNeeded>()
          },
            None = new ComponentType[3]
          {
        ComponentType.ReadOnly<TravelPurpose>(),
        ComponentType.ReadOnly<Deleted>(),
        ComponentType.ReadOnly<Temp>()
          }
        }, new EntityQueryDesc()
        {
            All = new ComponentType[1]
          {
        ComponentType.ReadOnly<ResourceBought>()
          },
            None = new ComponentType[2]
          {
        ComponentType.ReadOnly<Deleted>(),
        ComponentType.ReadOnly<Temp>()
          }
        });
        // ISSUE: reference to a compiler-generated field
        this.m_CarPrefabQuery = this.GetEntityQuery(PersonalCarSelectData.GetEntityQueryDesc());
        // ISSUE: reference to a compiler-generated field
        this.m_EconomyParameterQuery = this.GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
        // ISSUE: reference to a compiler-generated field
        this.m_PopulationQuery = this.GetEntityQuery(ComponentType.ReadOnly<Population>());
        // ISSUE: reference to a compiler-generated field
        this.m_ResidentPrefabQuery = this.GetEntityQuery(ComponentType.ReadOnly<ObjectData>(), ComponentType.ReadOnly<HumanData>(), ComponentType.ReadOnly<ResidentData>(), ComponentType.ReadOnly<PrefabData>());
        // ISSUE: reference to a compiler-generated field
        this.m_PathfindTypes = new ComponentTypeSet(ComponentType.ReadWrite<PathInformation>(), ComponentType.ReadWrite<PathElement>());

        m_GoodsDeliveryRequestArchetype = EntityManager.CreateArchetype(
        ComponentType.ReadWrite<ServiceRequest>(),
        ComponentType.ReadWrite<GoodsDeliveryRequest>(),
        ComponentType.ReadWrite<RequestGroup>()
        );
        // ISSUE: reference to a compiler-generated field
        this.RequireForUpdate(this.m_BuyerQuery);
        // ISSUE: reference to a compiler-generated field
        this.RequireForUpdate(this.m_EconomyParameterQuery);
        // ISSUE: reference to a compiler-generated field
        this.RequireForUpdate(this.m_PopulationQuery);
    }

    [UnityEngine.Scripting.Preserve]
    protected override void OnDestroy()
    {
        // ISSUE: reference to a compiler-generated field
        this.m_SalesQueue.Dispose();
        base.OnDestroy();
    }

    [UnityEngine.Scripting.Preserve]
    protected override void OnStopRunning() => base.OnStopRunning();

    [UnityEngine.Scripting.Preserve]
    protected override void OnUpdate()
    {
        // ISSUE: reference to a compiler-generated field
        if (this.m_BuyerQuery.CalculateEntityCount() <= 0)
            return;
        JobHandle jobHandle;
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        this.m_PersonalCarSelectData.PreUpdate((SystemBase)this, this.m_CityConfigurationSystem, this.m_CarPrefabQuery, Allocator.TempJob, out jobHandle);
        JobHandle outJobHandle;
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated method
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated method
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: object of a compiler-generated type is created
        // ISSUE: variable of a compiler-generated type
        Time2WorkResourceBuyerSystem.HandleBuyersJob jobData1 = new Time2WorkResourceBuyerSystem.HandleBuyersJob()
        {
            m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref this.__TypeHandle.__Unity_Entities_Entity_TypeHandle, ref this.CheckedStateRef),
            m_BuyerType = InternalCompilerInterface.GetComponentTypeHandle<ResourceBuyer>(ref this.__TypeHandle.__Game_Companies_ResourceBuyer_RO_ComponentTypeHandle, ref this.CheckedStateRef),
            m_BoughtType = InternalCompilerInterface.GetComponentTypeHandle<ResourceBought>(ref this.__TypeHandle.__Game_Citizens_ResourceBought_RO_ComponentTypeHandle, ref this.CheckedStateRef),
            m_TripType = InternalCompilerInterface.GetBufferTypeHandle<TripNeeded>(ref this.__TypeHandle.__Game_Citizens_TripNeeded_RW_BufferTypeHandle, ref this.CheckedStateRef),
            m_CitizenType = InternalCompilerInterface.GetComponentTypeHandle<Citizen>(ref this.__TypeHandle.__Game_Citizens_Citizen_RO_ComponentTypeHandle, ref this.CheckedStateRef),
            m_CreatureDataType = InternalCompilerInterface.GetComponentTypeHandle<CreatureData>(ref this.__TypeHandle.__Game_Prefabs_CreatureData_RO_ComponentTypeHandle, ref this.CheckedStateRef),
            m_ResidentDataType = InternalCompilerInterface.GetComponentTypeHandle<ResidentData>(ref this.__TypeHandle.__Game_Prefabs_ResidentData_RO_ComponentTypeHandle, ref this.CheckedStateRef),
            m_AttendingMeetingType = InternalCompilerInterface.GetComponentTypeHandle<AttendingMeeting>(ref this.__TypeHandle.__Game_Citizens_AttendingMeeting_RO_ComponentTypeHandle, ref this.CheckedStateRef),
            m_ServiceAvailables = InternalCompilerInterface.GetComponentLookup<ServiceAvailable>(ref this.__TypeHandle.__Game_Companies_ServiceAvailable_RO_ComponentLookup, ref this.CheckedStateRef),
            m_PathInformation = InternalCompilerInterface.GetComponentLookup<PathInformation>(ref this.__TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentLookup, ref this.CheckedStateRef),
            m_Properties = InternalCompilerInterface.GetComponentLookup<PropertyRenter>(ref this.__TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup, ref this.CheckedStateRef),
            m_CarKeepers = InternalCompilerInterface.GetComponentLookup<CarKeeper>(ref this.__TypeHandle.__Game_Citizens_CarKeeper_RO_ComponentLookup, ref this.CheckedStateRef),
            m_BicycleOwners = InternalCompilerInterface.GetComponentLookup<BicycleOwner>(ref this.__TypeHandle.__Game_Citizens_BicycleOwner_RO_ComponentLookup, ref this.CheckedStateRef),
            m_ParkedCarData = InternalCompilerInterface.GetComponentLookup<ParkedCar>(ref this.__TypeHandle.__Game_Vehicles_ParkedCar_RO_ComponentLookup, ref this.CheckedStateRef),
            m_PersonalCarData = InternalCompilerInterface.GetComponentLookup<Game.Vehicles.PersonalCar>(ref this.__TypeHandle.__Game_Vehicles_PersonalCar_RO_ComponentLookup, ref this.CheckedStateRef),
            m_Targets = InternalCompilerInterface.GetComponentLookup<Game.Common.Target>(ref this.__TypeHandle.__Game_Common_Target_RO_ComponentLookup, ref this.CheckedStateRef),
            m_CurrentBuildings = InternalCompilerInterface.GetComponentLookup<CurrentBuilding>(ref this.__TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentLookup, ref this.CheckedStateRef),
            m_OutsideConnections = InternalCompilerInterface.GetComponentLookup<Game.Objects.OutsideConnection>(ref this.__TypeHandle.__Game_Objects_OutsideConnection_RO_ComponentLookup, ref this.CheckedStateRef),
            m_HouseholdMembers = InternalCompilerInterface.GetComponentLookup<HouseholdMember>(ref this.__TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentLookup, ref this.CheckedStateRef),
            m_Households = InternalCompilerInterface.GetComponentLookup<Household>(ref this.__TypeHandle.__Game_Citizens_Household_RO_ComponentLookup, ref this.CheckedStateRef),
            m_TouristHouseholds = InternalCompilerInterface.GetComponentLookup<TouristHousehold>(ref this.__TypeHandle.__Game_Citizens_TouristHousehold_RO_ComponentLookup, ref this.CheckedStateRef),
            m_CommuterHouseholds = InternalCompilerInterface.GetComponentLookup<CommuterHousehold>(ref this.__TypeHandle.__Game_Citizens_CommuterHousehold_RO_ComponentLookup, ref this.CheckedStateRef),
            m_Workers = InternalCompilerInterface.GetComponentLookup<Worker>(ref this.__TypeHandle.__Game_Citizens_Worker_RO_ComponentLookup, ref this.CheckedStateRef),
            m_DeliveryTrucks = InternalCompilerInterface.GetComponentLookup<Game.Vehicles.DeliveryTruck>(ref this.__TypeHandle.__Game_Vehicles_DeliveryTruck_RO_ComponentLookup, ref this.CheckedStateRef),
            m_StorageCompanies = InternalCompilerInterface.GetComponentLookup<Game.Companies.StorageCompany>(ref this.__TypeHandle.__Game_Companies_StorageCompany_RO_ComponentLookup, ref this.CheckedStateRef),
            m_Resources = InternalCompilerInterface.GetBufferLookup<Game.Economy.Resources>(ref this.__TypeHandle.__Game_Economy_Resources_RO_BufferLookup, ref this.CheckedStateRef),
            m_HouseholdCitizens = InternalCompilerInterface.GetBufferLookup<HouseholdCitizen>(ref this.__TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup, ref this.CheckedStateRef),
            m_OwnedVehicles = InternalCompilerInterface.GetBufferLookup<OwnedVehicle>(ref this.__TypeHandle.__Game_Vehicles_OwnedVehicle_RO_BufferLookup, ref this.CheckedStateRef),
            m_GuestVehicles = InternalCompilerInterface.GetBufferLookup<GuestVehicle>(ref this.__TypeHandle.__Game_Vehicles_GuestVehicle_RO_BufferLookup, ref this.CheckedStateRef),
            m_LayoutElements = InternalCompilerInterface.GetBufferLookup<LayoutElement>(ref this.__TypeHandle.__Game_Vehicles_LayoutElement_RO_BufferLookup, ref this.CheckedStateRef),
            m_ResourcePrefabs = this.m_ResourceSystem.GetPrefabs(),
            m_ResourceDatas = InternalCompilerInterface.GetComponentLookup<ResourceData>(ref this.__TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup, ref this.CheckedStateRef),
            m_PrefabRefData = InternalCompilerInterface.GetComponentLookup<PrefabRef>(ref this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref this.CheckedStateRef),
            m_PrefabCarData = InternalCompilerInterface.GetComponentLookup<CarData>(ref this.__TypeHandle.__Game_Prefabs_CarData_RO_ComponentLookup, ref this.CheckedStateRef),
            m_ObjectGeometryData = InternalCompilerInterface.GetComponentLookup<ObjectGeometryData>(ref this.__TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref this.CheckedStateRef),
            m_PrefabHumanData = InternalCompilerInterface.GetComponentLookup<HumanData>(ref this.__TypeHandle.__Game_Prefabs_HumanData_RO_ComponentLookup, ref this.CheckedStateRef),
            m_CoordinatedMeetings = InternalCompilerInterface.GetComponentLookup<CoordinatedMeeting>(ref this.__TypeHandle.__Game_Citizens_CoordinatedMeeting_RW_ComponentLookup, ref this.CheckedStateRef),
            m_HaveCoordinatedMeetingDatas = InternalCompilerInterface.GetBufferLookup<HaveCoordinatedMeetingData>(ref this.__TypeHandle.__Game_Prefabs_HaveCoordinatedMeetingData_RO_BufferLookup, ref this.CheckedStateRef),
            m_OutsideConnectionDatas = InternalCompilerInterface.GetComponentLookup<OutsideConnectionData>(ref this.__TypeHandle.__Game_Prefabs_OutsideConnectionData_RO_ComponentLookup, ref this.CheckedStateRef),
            m_Populations = InternalCompilerInterface.GetComponentLookup<Population>(ref this.__TypeHandle.__Game_City_Population_RW_ComponentLookup, ref this.CheckedStateRef),
            m_TimeOfDay = this.m_TimeSystem.normalizedTime,
            m_FrameIndex = this.m_SimulationSystem.frameIndex,
            m_RandomSeed = RandomSeed.Next(),
            m_PathfindTypes = this.m_PathfindTypes,
            m_HumanChunks = this.m_ResidentPrefabQuery.ToArchetypeChunkListAsync((AllocatorManager.AllocatorHandle)this.World.UpdateAllocator.ToAllocator, out outJobHandle),
            m_PersonalCarSelectData = this.m_PersonalCarSelectData,
            m_PathfindQueue = this.m_PathfindSetupSystem.GetQueue((object)this, 80 /*0x50*/, 16 /*0x10*/).AsParallelWriter(),
            m_CommandBuffer = this.m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
            m_EconomyParameterData = this.m_EconomyParameterQuery.GetSingleton<EconomyParameterData>(),
            m_City = this.m_CitySystem.City,
            m_GoodsDeliveryRequestArchetype = this.m_GoodsDeliveryRequestArchetype,
            m_SalesQueue = this.m_SalesQueue.AsParallelWriter()
        };
        // ISSUE: reference to a compiler-generated field
        this.Dependency = jobData1.ScheduleParallel<Time2WorkResourceBuyerSystem.HandleBuyersJob>(this.m_BuyerQuery, JobHandle.CombineDependencies(this.Dependency, outJobHandle, jobHandle));
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated method
        this.m_ResourceSystem.AddPrefabsReader(this.Dependency);
        // ISSUE: reference to a compiler-generated field
        this.m_EndFrameBarrier.AddJobHandleForProducer(this.Dependency);
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated method
        this.m_PathfindSetupSystem.AddQueueWriter(this.Dependency);
        JobHandle deps;
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated method
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: object of a compiler-generated type is created
        // ISSUE: variable of a compiler-generated type
        Time2WorkResourceBuyerSystem.BuyJob jobData2 = new Time2WorkResourceBuyerSystem.BuyJob()
        {
            m_Resources = InternalCompilerInterface.GetBufferLookup<Game.Economy.Resources>(ref this.__TypeHandle.__Game_Economy_Resources_RW_BufferLookup, ref this.CheckedStateRef),
            m_SalesQueue = this.m_SalesQueue,
            m_Services = InternalCompilerInterface.GetComponentLookup<ServiceAvailable>(ref this.__TypeHandle.__Game_Companies_ServiceAvailable_RW_ComponentLookup, ref this.CheckedStateRef),
            m_TransformDatas = InternalCompilerInterface.GetComponentLookup<Game.Objects.Transform>(ref this.__TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref this.CheckedStateRef),
            m_PropertyRenters = InternalCompilerInterface.GetComponentLookup<PropertyRenter>(ref this.__TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup, ref this.CheckedStateRef),
            m_OwnedVehicles = InternalCompilerInterface.GetBufferLookup<OwnedVehicle>(ref this.__TypeHandle.__Game_Vehicles_OwnedVehicle_RO_BufferLookup, ref this.CheckedStateRef),
            m_HouseholdCitizens = InternalCompilerInterface.GetBufferLookup<HouseholdCitizen>(ref this.__TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup, ref this.CheckedStateRef),
            m_HouseholdAnimals = InternalCompilerInterface.GetBufferLookup<HouseholdAnimal>(ref this.__TypeHandle.__Game_Citizens_HouseholdAnimal_RO_BufferLookup, ref this.CheckedStateRef),
            m_Prefabs = InternalCompilerInterface.GetComponentLookup<PrefabRef>(ref this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref this.CheckedStateRef),
            m_ServiceCompanies = InternalCompilerInterface.GetComponentLookup<ServiceCompanyData>(ref this.__TypeHandle.__Game_Companies_ServiceCompanyData_RO_ComponentLookup, ref this.CheckedStateRef),
            m_Storages = InternalCompilerInterface.GetComponentLookup<Game.Companies.StorageCompany>(ref this.__TypeHandle.__Game_Companies_StorageCompany_RO_ComponentLookup, ref this.CheckedStateRef),
            m_Households = InternalCompilerInterface.GetComponentLookup<Household>(ref this.__TypeHandle.__Game_Citizens_Household_RW_ComponentLookup, ref this.CheckedStateRef),
            m_BuyingCompanies = InternalCompilerInterface.GetComponentLookup<BuyingCompany>(ref this.__TypeHandle.__Game_Companies_BuyingCompany_RW_ComponentLookup, ref this.CheckedStateRef),
            m_ResourceDatas = InternalCompilerInterface.GetComponentLookup<ResourceData>(ref this.__TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup, ref this.CheckedStateRef),
            m_TradeCosts = InternalCompilerInterface.GetBufferLookup<TradeCost>(ref this.__TypeHandle.__Game_Companies_TradeCost_RW_BufferLookup, ref this.CheckedStateRef),
            m_CompanyStatistics = InternalCompilerInterface.GetComponentLookup<CompanyStatisticData>(ref this.__TypeHandle.__Game_Companies_CompanyStatisticData_RW_ComponentLookup, ref this.CheckedStateRef),
            m_OutsideConnections = InternalCompilerInterface.GetComponentLookup<Game.Objects.OutsideConnection>(ref this.__TypeHandle.__Game_Objects_OutsideConnection_RO_ComponentLookup, ref this.CheckedStateRef),
            m_ResourcePrefabs = this.m_ResourceSystem.GetPrefabs(),
            m_RandomSeed = RandomSeed.Next(),
            m_FrameIndex = this.m_SimulationSystem.frameIndex,
            m_PersonalCarSelectData = this.m_PersonalCarSelectData,
            m_PopulationData = InternalCompilerInterface.GetComponentLookup<Population>(ref this.__TypeHandle.__Game_City_Population_RO_ComponentLookup, ref this.CheckedStateRef),
            m_PopulationEntity = this.m_PopulationQuery.GetSingletonEntity(),
            m_CitizenConsumptionAccumulator = this.m_CityProductionStatisticSystem.GetCityResourceUsageAccumulator(CityProductionStatisticSystem.CityResourceUsage.Consumer.Citizens, out deps),
            m_CommandBuffer = this.m_EndFrameBarrier.CreateCommandBuffer()
        };
        this.Dependency = jobData2.Schedule<Time2WorkResourceBuyerSystem.BuyJob>(JobHandle.CombineDependencies(this.Dependency, deps));
        // ISSUE: reference to a compiler-generated field
        this.m_PersonalCarSelectData.PostUpdate(this.Dependency);
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated method
        this.m_ResourceSystem.AddPrefabsReader(this.Dependency);
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated method
        this.m_TaxSystem.AddReader(this.Dependency);
        // ISSUE: reference to a compiler-generated field
        this.m_CityProductionStatisticSystem.AddCityUsageAccumulatorWriter(CityProductionStatisticSystem.CityResourceUsage.Consumer.Citizens, this.Dependency);
        // ISSUE: reference to a compiler-generated field
        this.m_EndFrameBarrier.AddJobHandleForProducer(this.Dependency);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void __AssignQueries(ref SystemState state)
    {
        new EntityQueryBuilder((AllocatorManager.AllocatorHandle)Allocator.Temp).Dispose();
    }

    protected override void OnCreateForCompiler()
    {
        base.OnCreateForCompiler();
        // ISSUE: reference to a compiler-generated method
        this.__AssignQueries(ref this.CheckedStateRef);
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated method
        this.__TypeHandle.__AssignHandles(ref this.CheckedStateRef);
    }

    [UnityEngine.Scripting.Preserve]
    public Time2WorkResourceBuyerSystem()
    {
    }

    [Flags]
    private enum SaleFlags : byte
    {
        None = 0,
        CommercialSeller = 1,
        ImportFromOC = 2,
        Virtual = 4,
    }

    private struct SalesEvent
    {
        public Time2WorkResourceBuyerSystem.SaleFlags m_Flags;
        public Entity m_Buyer;
        public Entity m_Seller;
        public Resource m_Resource;
        public int m_Amount;
        public float m_Distance;
    }

    [BurstCompile]
    private struct BuyJob : IJob
    {
        public NativeQueue<Time2WorkResourceBuyerSystem.SalesEvent> m_SalesQueue;
        public BufferLookup<Game.Economy.Resources> m_Resources;
        public ComponentLookup<ServiceAvailable> m_Services;
        [NativeDisableParallelForRestriction]
        public ComponentLookup<Household> m_Households;
        [NativeDisableParallelForRestriction]
        public ComponentLookup<BuyingCompany> m_BuyingCompanies;
        [ReadOnly]
        public ComponentLookup<Game.Objects.Transform> m_TransformDatas;
        [ReadOnly]
        public ComponentLookup<PropertyRenter> m_PropertyRenters;
        [ReadOnly]
        public ComponentLookup<PrefabRef> m_Prefabs;
        [ReadOnly]
        public ComponentLookup<ServiceCompanyData> m_ServiceCompanies;
        [ReadOnly]
        public BufferLookup<OwnedVehicle> m_OwnedVehicles;
        [ReadOnly]
        public BufferLookup<HouseholdCitizen> m_HouseholdCitizens;
        [ReadOnly]
        public BufferLookup<HouseholdAnimal> m_HouseholdAnimals;
        [ReadOnly]
        public ComponentLookup<ResourceData> m_ResourceDatas;
        [ReadOnly]
        public ComponentLookup<Game.Companies.StorageCompany> m_Storages;
        public ComponentLookup<CompanyStatisticData> m_CompanyStatistics;
        public BufferLookup<TradeCost> m_TradeCosts;
        [ReadOnly]
        public ComponentLookup<Game.Objects.OutsideConnection> m_OutsideConnections;
        [ReadOnly]
        public ResourcePrefabs m_ResourcePrefabs;
        [ReadOnly]
        public PersonalCarSelectData m_PersonalCarSelectData;
        [ReadOnly]
        public ComponentLookup<Population> m_PopulationData;
        public NativeArray<int> m_CitizenConsumptionAccumulator;
        public Entity m_PopulationEntity;
        public RandomSeed m_RandomSeed;
        public EntityCommandBuffer m_CommandBuffer;
        [ReadOnly]
        public uint m_FrameIndex;

        public void Execute()
        {
            // ISSUE: reference to a compiler-generated field
            Unity.Mathematics.Random random = this.m_RandomSeed.GetRandom(0);
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            Population population = this.m_PopulationData[this.m_PopulationEntity];
            // ISSUE: variable of a compiler-generated type
            Time2WorkResourceBuyerSystem.SalesEvent salesEvent;
            // ISSUE: reference to a compiler-generated field
            while (this.m_SalesQueue.TryDequeue(out salesEvent))
            {
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                if (this.m_Resources.HasBuffer(salesEvent.m_Buyer) && salesEvent.m_Amount != 0)
                {
                    // ISSUE: reference to a compiler-generated field
                    bool flag = (salesEvent.m_Flags & Time2WorkResourceBuyerSystem.SaleFlags.CommercialSeller) != 0;
                    // ISSUE: reference to a compiler-generated field
                    // ISSUE: reference to a compiler-generated field
                    // ISSUE: reference to a compiler-generated field
                    // ISSUE: reference to a compiler-generated field
                    // ISSUE: reference to a compiler-generated field
                    // ISSUE: reference to a compiler-generated field
                    // ISSUE: reference to a compiler-generated field
                    float num1 = (flag ? EconomyUtils.GetMarketPrice(salesEvent.m_Resource, this.m_ResourcePrefabs, ref this.m_ResourceDatas) : EconomyUtils.GetIndustrialPrice(salesEvent.m_Resource, this.m_ResourcePrefabs, ref this.m_ResourceDatas)) * (float)salesEvent.m_Amount;
                    // ISSUE: reference to a compiler-generated field
                    // ISSUE: reference to a compiler-generated field
                    if (this.m_TradeCosts.HasBuffer(salesEvent.m_Seller))
                    {
                        // ISSUE: reference to a compiler-generated field
                        // ISSUE: reference to a compiler-generated field
                        DynamicBuffer<TradeCost> tradeCost1 = this.m_TradeCosts[salesEvent.m_Seller];
                        // ISSUE: reference to a compiler-generated field
                        TradeCost tradeCost2 = EconomyUtils.GetTradeCost(salesEvent.m_Resource, tradeCost1);
                        // ISSUE: reference to a compiler-generated field
                        num1 += (float)salesEvent.m_Amount * tradeCost2.m_BuyCost;
                        // ISSUE: reference to a compiler-generated field
                        // ISSUE: reference to a compiler-generated field
                        // ISSUE: reference to a compiler-generated field
                        float weight = EconomyUtils.GetWeight(salesEvent.m_Resource, this.m_ResourcePrefabs, ref this.m_ResourceDatas);
                        // ISSUE: reference to a compiler-generated field
                        Assert.IsTrue(salesEvent.m_Amount != -1);
                        // ISSUE: reference to a compiler-generated field
                        // ISSUE: reference to a compiler-generated field
                        // ISSUE: reference to a compiler-generated field
                        // ISSUE: reference to a compiler-generated field
                        float num2 = (float)EconomyUtils.GetTransportCost(salesEvent.m_Distance, salesEvent.m_Resource, salesEvent.m_Amount, weight) / (1f + (float)salesEvent.m_Amount);
                        TradeCost newcost = new TradeCost();
                        // ISSUE: reference to a compiler-generated field
                        // ISSUE: reference to a compiler-generated field
                        if (this.m_TradeCosts.HasBuffer(salesEvent.m_Buyer))
                        {
                            // ISSUE: reference to a compiler-generated field
                            // ISSUE: reference to a compiler-generated field
                            // ISSUE: reference to a compiler-generated field
                            newcost = EconomyUtils.GetTradeCost(salesEvent.m_Resource, this.m_TradeCosts[salesEvent.m_Buyer]);
                        }
                        // ISSUE: reference to a compiler-generated field
                        // ISSUE: reference to a compiler-generated field
                        if (!this.m_OutsideConnections.HasComponent(salesEvent.m_Seller) && !flag)
                        {
                            tradeCost2.m_SellCost = math.lerp(tradeCost2.m_SellCost, num2 + newcost.m_SellCost, 0.5f);
                            // ISSUE: reference to a compiler-generated field
                            EconomyUtils.SetTradeCost(salesEvent.m_Resource, tradeCost2, tradeCost1, true);
                        }
                        // ISSUE: reference to a compiler-generated field
                        // ISSUE: reference to a compiler-generated field
                        // ISSUE: reference to a compiler-generated field
                        // ISSUE: reference to a compiler-generated field
                        if (this.m_TradeCosts.HasBuffer(salesEvent.m_Buyer) && !this.m_OutsideConnections.HasComponent(salesEvent.m_Buyer))
                        {
                            newcost.m_BuyCost = (double)num2 + (double)tradeCost2.m_BuyCost >= (double)newcost.m_BuyCost ? math.lerp(newcost.m_BuyCost, num2 + tradeCost2.m_BuyCost, 0.5f) : num2 + tradeCost2.m_BuyCost;
                            // ISSUE: reference to a compiler-generated field
                            // ISSUE: reference to a compiler-generated field
                            // ISSUE: reference to a compiler-generated field
                            EconomyUtils.SetTradeCost(salesEvent.m_Resource, newcost, this.m_TradeCosts[salesEvent.m_Buyer], true);
                        }
                    }
                    // ISSUE: reference to a compiler-generated field
                    // ISSUE: reference to a compiler-generated field
                    // ISSUE: reference to a compiler-generated field
                    // ISSUE: reference to a compiler-generated field
                    // ISSUE: reference to a compiler-generated field
                    if (!this.m_Resources.HasBuffer(salesEvent.m_Seller) || EconomyUtils.GetResources(salesEvent.m_Resource, this.m_Resources[salesEvent.m_Seller]) > 0)
                    {
                        // ISSUE: reference to a compiler-generated field
                        // ISSUE: reference to a compiler-generated field
                        // ISSUE: reference to a compiler-generated field
                        // ISSUE: reference to a compiler-generated field
                        if (flag && this.m_Services.HasComponent(salesEvent.m_Seller) && this.m_PropertyRenters.HasComponent(salesEvent.m_Seller))
                        {
                            // ISSUE: reference to a compiler-generated field
                            // ISSUE: reference to a compiler-generated field
                            Entity prefab = this.m_Prefabs[salesEvent.m_Seller].m_Prefab;
                            // ISSUE: reference to a compiler-generated field
                            // ISSUE: reference to a compiler-generated field
                            ServiceAvailable service = this.m_Services[salesEvent.m_Seller];
                            // ISSUE: reference to a compiler-generated field
                            ServiceCompanyData serviceCompany = this.m_ServiceCompanies[prefab];
                            num1 *= EconomyUtils.GetServicePriceMultiplier((float)service.m_ServiceAvailable, serviceCompany.m_MaxService);
                            // ISSUE: reference to a compiler-generated field
                            service.m_ServiceAvailable = math.max(0, Mathf.RoundToInt((float)(service.m_ServiceAvailable - salesEvent.m_Amount)));
                            service.m_MeanPriority = (double)service.m_MeanPriority <= 0.0 ? math.min(1f, (float)service.m_ServiceAvailable / (float)serviceCompany.m_MaxService) : math.min(1f, math.lerp(service.m_MeanPriority, (float)service.m_ServiceAvailable / (float)serviceCompany.m_MaxService, 0.1f));
                            // ISSUE: reference to a compiler-generated field
                            // ISSUE: reference to a compiler-generated field
                            this.m_Services[salesEvent.m_Seller] = service;
                        }
                        // ISSUE: reference to a compiler-generated field
                        // ISSUE: reference to a compiler-generated field
                        // ISSUE: reference to a compiler-generated field
                        // ISSUE: reference to a compiler-generated field
                        if (this.m_Resources.HasBuffer(salesEvent.m_Seller) && !this.m_Storages.HasComponent(salesEvent.m_Seller))
                        {
                            // ISSUE: reference to a compiler-generated field
                            // ISSUE: reference to a compiler-generated field
                            DynamicBuffer<Game.Economy.Resources> resource = this.m_Resources[salesEvent.m_Seller];
                            // ISSUE: reference to a compiler-generated field
                            int resources = EconomyUtils.GetResources(salesEvent.m_Resource, resource);
                            // ISSUE: reference to a compiler-generated field
                            // ISSUE: reference to a compiler-generated field
                            EconomyUtils.AddResources(salesEvent.m_Resource, -math.min(resources, Mathf.RoundToInt((float)salesEvent.m_Amount)), resource);
                        }
                        // ISSUE: reference to a compiler-generated field
                        // ISSUE: reference to a compiler-generated field
                        EconomyUtils.AddResources(Resource.Money, -Mathf.RoundToInt(num1), this.m_Resources[salesEvent.m_Buyer]);
                        // ISSUE: reference to a compiler-generated field
                        // ISSUE: reference to a compiler-generated field
                        if (this.m_Households.HasComponent(salesEvent.m_Buyer))
                        {
                            // ISSUE: reference to a compiler-generated field
                            // ISSUE: reference to a compiler-generated field
                            Household household = this.m_Households[salesEvent.m_Buyer];
                            household.m_Resources = (int)math.clamp((long)((double)household.m_Resources + (double)num1), (long)int.MinValue, (long)int.MaxValue);
                            household.m_ShoppedValuePerDay += (uint)num1;
                            // ISSUE: reference to a compiler-generated field
                            // ISSUE: reference to a compiler-generated field
                            this.m_Households[salesEvent.m_Buyer] = household;
                            // ISSUE: reference to a compiler-generated field
                            // ISSUE: reference to a compiler-generated field
                            // ISSUE: reference to a compiler-generated field
                            this.m_CitizenConsumptionAccumulator[EconomyUtils.GetResourceIndex(salesEvent.m_Resource)] += salesEvent.m_Amount;
                        }
                        else
                        {
                            // ISSUE: reference to a compiler-generated field
                            // ISSUE: reference to a compiler-generated field
                            if (this.m_BuyingCompanies.HasComponent(salesEvent.m_Buyer))
                            {
                                // ISSUE: reference to a compiler-generated field
                                // ISSUE: reference to a compiler-generated field
                                // ISSUE: reference to a compiler-generated field
                                BuyingCompany buyingCompany = this.m_BuyingCompanies[salesEvent.m_Buyer] with
                                {
                                    m_LastTradePartner = salesEvent.m_Seller
                                };
                                // ISSUE: reference to a compiler-generated field
                                // ISSUE: reference to a compiler-generated field
                                this.m_BuyingCompanies[salesEvent.m_Buyer] = buyingCompany;
                                // ISSUE: reference to a compiler-generated field
                                if ((salesEvent.m_Flags & Time2WorkResourceBuyerSystem.SaleFlags.Virtual) != Time2WorkResourceBuyerSystem.SaleFlags.None)
                                {
                                    // ISSUE: reference to a compiler-generated field
                                    // ISSUE: reference to a compiler-generated field
                                    // ISSUE: reference to a compiler-generated field
                                    // ISSUE: reference to a compiler-generated field
                                    EconomyUtils.AddResources(salesEvent.m_Resource, salesEvent.m_Amount, this.m_Resources[salesEvent.m_Buyer]);
                                }
                            }
                        }
                        // ISSUE: reference to a compiler-generated field
                        // ISSUE: reference to a compiler-generated field
                        // ISSUE: reference to a compiler-generated field
                        // ISSUE: reference to a compiler-generated field
                        if (!this.m_Storages.HasComponent(salesEvent.m_Seller) && this.m_PropertyRenters.HasComponent(salesEvent.m_Seller))
                        {
                            // ISSUE: reference to a compiler-generated field
                            // ISSUE: reference to a compiler-generated field
                            DynamicBuffer<Game.Economy.Resources> resource = this.m_Resources[salesEvent.m_Seller];
                            EconomyUtils.AddResources(Resource.Money, Mathf.RoundToInt(num1), resource);
                        }
                        // ISSUE: reference to a compiler-generated field
                        // ISSUE: reference to a compiler-generated field
                        if (this.m_CompanyStatistics.HasComponent(salesEvent.m_Seller))
                        {
                            // ISSUE: reference to a compiler-generated field
                            // ISSUE: reference to a compiler-generated field
                            CompanyStatisticData companyStatistic = this.m_CompanyStatistics[salesEvent.m_Seller];
                            ++companyStatistic.m_CurrentNumberOfCustomers;
                            // ISSUE: reference to a compiler-generated field
                            // ISSUE: reference to a compiler-generated field
                            this.m_CompanyStatistics[salesEvent.m_Seller] = companyStatistic;
                        }
                        // ISSUE: reference to a compiler-generated field
                        // ISSUE: reference to a compiler-generated field
                        if (this.m_CompanyStatistics.HasComponent(salesEvent.m_Buyer))
                        {
                            // ISSUE: reference to a compiler-generated field
                            // ISSUE: reference to a compiler-generated field
                            CompanyStatisticData companyStatistic = this.m_CompanyStatistics[salesEvent.m_Buyer];
                            companyStatistic.m_CurrentCostOfBuyingResources += math.abs((int)num1);
                            // ISSUE: reference to a compiler-generated field
                            // ISSUE: reference to a compiler-generated field
                            this.m_CompanyStatistics[salesEvent.m_Buyer] = companyStatistic;
                        }
                        // ISSUE: reference to a compiler-generated field
                        // ISSUE: reference to a compiler-generated field
                        // ISSUE: reference to a compiler-generated field
                        // ISSUE: reference to a compiler-generated field
                        // ISSUE: reference to a compiler-generated field
                        if (salesEvent.m_Resource == Resource.Vehicles && salesEvent.m_Amount == HouseholdBehaviorSystem.kCarAmount && this.m_PropertyRenters.HasComponent(salesEvent.m_Seller))
                        {
                            // ISSUE: reference to a compiler-generated field
                            // ISSUE: reference to a compiler-generated field
                            Entity property = this.m_PropertyRenters[salesEvent.m_Seller].m_Property;
                            // ISSUE: reference to a compiler-generated field
                            // ISSUE: reference to a compiler-generated field
                            // ISSUE: reference to a compiler-generated field
                            if (this.m_TransformDatas.HasComponent(property) && this.m_HouseholdCitizens.HasBuffer(salesEvent.m_Buyer))
                            {
                                // ISSUE: reference to a compiler-generated field
                                Entity buyer = salesEvent.m_Buyer;
                                // ISSUE: reference to a compiler-generated field
                                Game.Objects.Transform transformData = this.m_TransformDatas[property];
                                // ISSUE: reference to a compiler-generated field
                                int length1 = this.m_HouseholdCitizens[buyer].Length;
                                // ISSUE: reference to a compiler-generated field
                                // ISSUE: reference to a compiler-generated field
                                int length2 = this.m_HouseholdAnimals.HasBuffer(buyer) ? this.m_HouseholdAnimals[buyer].Length : 0;
                                int passengerAmount;
                                int baggageAmount;
                                // ISSUE: reference to a compiler-generated field
                                // ISSUE: reference to a compiler-generated field
                                if (this.m_OwnedVehicles.HasBuffer(buyer) && this.m_OwnedVehicles[buyer].Length >= 1)
                                {
                                    passengerAmount = random.NextInt(1, 1 + length1);
                                    baggageAmount = random.NextInt(1, 2 + length2);
                                }
                                else
                                {
                                    passengerAmount = length1;
                                    baggageAmount = 1 + length2;
                                }
                                if (random.NextInt(20) == 0)
                                    baggageAmount += 5;
                                // ISSUE: reference to a compiler-generated field
                                // ISSUE: reference to a compiler-generated field
                                Entity vehicle = this.m_PersonalCarSelectData.CreateVehicle(this.m_CommandBuffer, ref random, passengerAmount, baggageAmount, true, false, false, transformData, property, Entity.Null, (PersonalCarFlags)0, true);
                                if (vehicle != Entity.Null)
                                {
                                    // ISSUE: reference to a compiler-generated field
                                    this.m_CommandBuffer.AddComponent<Owner>(vehicle, new Owner(buyer));
                                    // ISSUE: reference to a compiler-generated field
                                    if (!this.m_OwnedVehicles.HasBuffer(buyer))
                                    {
                                        // ISSUE: reference to a compiler-generated field
                                        this.m_CommandBuffer.AddBuffer<OwnedVehicle>(buyer);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    [BurstCompile]
    private struct HandleBuyersJob : IJobChunk
    {
        [ReadOnly]
        public ComponentTypeHandle<ResourceBuyer> m_BuyerType;
        [ReadOnly]
        public ComponentTypeHandle<ResourceBought> m_BoughtType;
        [ReadOnly]
        public EntityTypeHandle m_EntityType;
        public BufferTypeHandle<TripNeeded> m_TripType;
        [ReadOnly]
        public ComponentTypeHandle<Citizen> m_CitizenType;
        [ReadOnly]
        public ComponentTypeHandle<CreatureData> m_CreatureDataType;
        [ReadOnly]
        public ComponentTypeHandle<ResidentData> m_ResidentDataType;
        [ReadOnly]
        public ComponentTypeHandle<AttendingMeeting> m_AttendingMeetingType;
        [ReadOnly]
        public ComponentLookup<PathInformation> m_PathInformation;
        [ReadOnly]
        public ComponentLookup<PropertyRenter> m_Properties;
        [ReadOnly]
        public ComponentLookup<ServiceAvailable> m_ServiceAvailables;
        [ReadOnly]
        public ComponentLookup<CarKeeper> m_CarKeepers;
        [ReadOnly]
        public ComponentLookup<BicycleOwner> m_BicycleOwners;
        [ReadOnly]
        public ComponentLookup<ParkedCar> m_ParkedCarData;
        [ReadOnly]
        public ComponentLookup<Game.Vehicles.PersonalCar> m_PersonalCarData;
        [ReadOnly]
        public ComponentLookup<Game.Common.Target> m_Targets;
        [ReadOnly]
        public ComponentLookup<CurrentBuilding> m_CurrentBuildings;
        [ReadOnly]
        public ComponentLookup<Game.Objects.OutsideConnection> m_OutsideConnections;
        [ReadOnly]
        public ComponentLookup<HouseholdMember> m_HouseholdMembers;
        [ReadOnly]
        public ComponentLookup<Household> m_Households;
        [ReadOnly]
        public ComponentLookup<TouristHousehold> m_TouristHouseholds;
        [ReadOnly]
        public ComponentLookup<CommuterHousehold> m_CommuterHouseholds;
        [ReadOnly]
        public ComponentLookup<Worker> m_Workers;
        [ReadOnly]
        public ComponentLookup<Game.Vehicles.DeliveryTruck> m_DeliveryTrucks;
        [ReadOnly]
        public ComponentLookup<Game.Companies.StorageCompany> m_StorageCompanies;
        [ReadOnly]
        public BufferLookup<Game.Economy.Resources> m_Resources;
        [ReadOnly]
        public BufferLookup<HouseholdCitizen> m_HouseholdCitizens;
        [ReadOnly]
        public BufferLookup<OwnedVehicle> m_OwnedVehicles;
        [ReadOnly]
        public BufferLookup<GuestVehicle> m_GuestVehicles;
        [ReadOnly]
        public BufferLookup<LayoutElement> m_LayoutElements;
        [NativeDisableParallelForRestriction]
        public ComponentLookup<CoordinatedMeeting> m_CoordinatedMeetings;
        [ReadOnly]
        public BufferLookup<HaveCoordinatedMeetingData> m_HaveCoordinatedMeetingDatas;
        [ReadOnly]
        public ResourcePrefabs m_ResourcePrefabs;
        [ReadOnly]
        public ComponentLookup<ResourceData> m_ResourceDatas;
        [ReadOnly]
        public ComponentLookup<PrefabRef> m_PrefabRefData;
        [ReadOnly]
        public ComponentLookup<CarData> m_PrefabCarData;
        [ReadOnly]
        public ComponentLookup<ObjectGeometryData> m_ObjectGeometryData;
        [ReadOnly]
        public ComponentLookup<OutsideConnectionData> m_OutsideConnectionDatas;
        [ReadOnly]
        public ComponentLookup<HumanData> m_PrefabHumanData;
        [ReadOnly]
        public ComponentLookup<Population> m_Populations;
        [ReadOnly]
        public float m_TimeOfDay;
        [ReadOnly]
        public uint m_FrameIndex;
        [ReadOnly]
        public RandomSeed m_RandomSeed;
        [ReadOnly]
        public ComponentTypeSet m_PathfindTypes;
        [ReadOnly]
        public NativeList<ArchetypeChunk> m_HumanChunks;
        [ReadOnly]
        public PersonalCarSelectData m_PersonalCarSelectData;
        public EntityCommandBuffer.ParallelWriter m_CommandBuffer;
        public NativeQueue<SetupQueueItem>.ParallelWriter m_PathfindQueue;
        public EconomyParameterData m_EconomyParameterData;
        public Entity m_City;
        public NativeQueue<Time2WorkResourceBuyerSystem.SalesEvent>.ParallelWriter m_SalesQueue;
        public EntityArchetype m_GoodsDeliveryRequestArchetype;


        public void Execute(
          in ArchetypeChunk chunk,
          int unfilteredChunkIndex,
          bool useEnabledMask,
          in v128 chunkEnabledMask)
        {
            // ISSUE: reference to a compiler-generated field
            NativeArray<Entity> nativeArray1 = chunk.GetNativeArray(this.m_EntityType);
            // ISSUE: reference to a compiler-generated field
            NativeArray<ResourceBuyer> nativeArray2 = chunk.GetNativeArray<ResourceBuyer>(ref this.m_BuyerType);
            // ISSUE: reference to a compiler-generated field
            NativeArray<ResourceBought> nativeArray3 = chunk.GetNativeArray<ResourceBought>(ref this.m_BoughtType);
            // ISSUE: reference to a compiler-generated field
            BufferAccessor<TripNeeded> bufferAccessor = chunk.GetBufferAccessor<TripNeeded>(ref this.m_TripType);
            // ISSUE: reference to a compiler-generated field
            NativeArray<Citizen> nativeArray4 = chunk.GetNativeArray<Citizen>(ref this.m_CitizenType);
            // ISSUE: reference to a compiler-generated field
            NativeArray<AttendingMeeting> nativeArray5 = chunk.GetNativeArray<AttendingMeeting>(ref this.m_AttendingMeetingType);
            // ISSUE: reference to a compiler-generated field
            Unity.Mathematics.Random random = this.m_RandomSeed.GetRandom(unfilteredChunkIndex);
            // ISSUE: reference to a compiler-generated method
            this.ProcessResourceBought(unfilteredChunkIndex, nativeArray3, nativeArray1);
            // ISSUE: reference to a compiler-generated method
            this.ProcessResourceBuyer(chunk, unfilteredChunkIndex, nativeArray2, nativeArray1, bufferAccessor, nativeArray4, random, nativeArray5);
        }

        private void ProcessResourceBought(
          int unfilteredChunkIndex,
          NativeArray<ResourceBought> resourceBuyingWithTargets,
          NativeArray<Entity> entities)
        {
            for (int index = 0; index < resourceBuyingWithTargets.Length; ++index)
            {
                Entity entity = entities[index];
                ResourceBought buyingWithTarget = resourceBuyingWithTargets[index];
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                if (this.m_PrefabRefData.HasComponent(buyingWithTarget.m_Payer) && this.m_PrefabRefData.HasComponent(buyingWithTarget.m_Seller))
                {
                    // ISSUE: object of a compiler-generated type is created
                    // ISSUE: variable of a compiler-generated type
                    Time2WorkResourceBuyerSystem.SalesEvent salesEvent = new Time2WorkResourceBuyerSystem.SalesEvent()
                    {
                        m_Amount = buyingWithTarget.m_Amount,
                        m_Buyer = buyingWithTarget.m_Payer,
                        m_Seller = buyingWithTarget.m_Seller,
                        m_Resource = buyingWithTarget.m_Resource,
                        m_Flags = Time2WorkResourceBuyerSystem.SaleFlags.None,
                        m_Distance = buyingWithTarget.m_Distance
                    };
                    // ISSUE: reference to a compiler-generated field
                    this.m_SalesQueue.Enqueue(salesEvent);
                }
                // ISSUE: reference to a compiler-generated field
                this.m_CommandBuffer.RemoveComponent<ResourceBought>(unfilteredChunkIndex, entity);
            }
        }

        private void ProcessResourceBuyer(
          ArchetypeChunk chunk,
          int unfilteredChunkIndex,
          NativeArray<ResourceBuyer> resourceBuyingRequests,
          NativeArray<Entity> entities,
          BufferAccessor<TripNeeded> tripBuffers,
          NativeArray<Citizen> citizens,
          Unity.Mathematics.Random random,
          NativeArray<AttendingMeeting> meetings)
        {
            for (int index = 0; index < resourceBuyingRequests.Length; ++index)
            {
                ResourceBuyer resourceBuyingRequest = resourceBuyingRequests[index];
                Entity entity = entities[index];
                DynamicBuffer<TripNeeded> tripBuffer = tripBuffers[index];
                bool isCompanyChunk = citizens.Length == 0;
                bool virtualGood = false;
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                if (this.m_ResourceDatas.HasComponent(this.m_ResourcePrefabs[resourceBuyingRequest.m_ResourceNeeded]))
                {
                    // ISSUE: reference to a compiler-generated field
                    // ISSUE: reference to a compiler-generated field
                    virtualGood = (double)EconomyUtils.GetWeight(resourceBuyingRequest.m_ResourceNeeded, this.m_ResourcePrefabs, ref this.m_ResourceDatas) == 0.0;
                }
                // ISSUE: variable of a compiler-generated type
                Time2WorkResourceBuyerSystem.SalesEvent salesEvent1;
                // ISSUE: reference to a compiler-generated field
                if (this.m_PathInformation.HasComponent(entity))
                {
                    // ISSUE: reference to a compiler-generated field
                    PathInformation pathInformation = this.m_PathInformation[entity];
                    if ((pathInformation.m_State & PathFlags.Pending) == (PathFlags)0)
                    {
                        Entity destination = pathInformation.m_Destination;
                        // ISSUE: reference to a compiler-generated field
                        // ISSUE: reference to a compiler-generated field
                        if (this.m_Properties.HasComponent(destination) || this.m_OutsideConnections.HasComponent(destination))
                        {
                            // ISSUE: reference to a compiler-generated field
                            DynamicBuffer<Game.Economy.Resources> resource = this.m_Resources[destination];
                            int resources = EconomyUtils.GetResources(resourceBuyingRequest.m_ResourceNeeded, resource);
                            // ISSUE: reference to a compiler-generated field
                            if (this.m_StorageCompanies.HasComponent(destination))
                            {
                                // ISSUE: reference to a compiler-generated field
                                // ISSUE: reference to a compiler-generated field
                                // ISSUE: reference to a compiler-generated field
                                int buyingResourcesTrucks = VehicleUtils.GetAllBuyingResourcesTrucks(destination, resourceBuyingRequest.m_ResourceNeeded, ref this.m_DeliveryTrucks, ref this.m_GuestVehicles, ref this.m_LayoutElements);
                                resources -= buyingResourcesTrucks;
                            }
                            int y1 = math.max(resources, 0);
                            if (y1 <= resourceBuyingRequest.m_AmountNeeded / 2)
                            {
                                // ISSUE: reference to a compiler-generated field
                                // ISSUE: reference to a compiler-generated field
                                this.m_CommandBuffer.RemoveComponent(unfilteredChunkIndex, entity, in this.m_PathfindTypes);
                            }
                            else
                            {
                                resourceBuyingRequest.m_AmountNeeded = math.min(resourceBuyingRequest.m_AmountNeeded, y1);
                                // ISSUE: reference to a compiler-generated field
                                int num1 = this.m_ServiceAvailables.HasComponent(destination) ? 1 : 0;
                                // ISSUE: reference to a compiler-generated field
                                bool flag1 = this.m_StorageCompanies.HasComponent(destination);
                                // ISSUE: variable of a compiler-generated type
                                Time2WorkResourceBuyerSystem.SaleFlags saleFlags = num1 != 0 ? Time2WorkResourceBuyerSystem.SaleFlags.CommercialSeller : Time2WorkResourceBuyerSystem.SaleFlags.None;
                                if (virtualGood)
                                    saleFlags |= Time2WorkResourceBuyerSystem.SaleFlags.Virtual;
                                // ISSUE: reference to a compiler-generated field
                                if (this.m_OutsideConnections.HasComponent(destination))
                                    saleFlags |= Time2WorkResourceBuyerSystem.SaleFlags.ImportFromOC;
                                // ISSUE: reference to a compiler-generated field
                                // ISSUE: reference to a compiler-generated field
                                if (this.m_Households.HasComponent(resourceBuyingRequest.m_Payer) && this.m_Resources.HasBuffer(resourceBuyingRequest.m_Payer))
                                {
                                    // ISSUE: reference to a compiler-generated field
                                    // ISSUE: reference to a compiler-generated field
                                    int num2 = math.max(0, EconomyUtils.GetResources(Resource.Money, this.m_Resources[resourceBuyingRequest.m_Payer]) - HouseholdBehaviorSystem.kMinimumShoppingMoney);
                                    // ISSUE: reference to a compiler-generated field
                                    // ISSUE: reference to a compiler-generated field
                                    float marketPrice = EconomyUtils.GetMarketPrice(resourceBuyingRequest.m_ResourceNeeded, this.m_ResourcePrefabs, ref this.m_ResourceDatas);
                                    float num3 = 1.4f;
                                    int y2 = (double)num2 > 0.0 ? (int)((double)num2 / ((double)marketPrice * (double)num3)) : 0;
                                    resourceBuyingRequest.m_AmountNeeded = math.min(resourceBuyingRequest.m_AmountNeeded, y2);
                                }
                                bool flag2 = resourceBuyingRequest.m_AmountNeeded > 0;
                                if (flag2)
                                {
                                    // For citizens and for virtual goods, keep the old behavior
                                    // For company + physical goods, the delivery truck system will handle the money
                                    if (!isCompanyChunk || virtualGood)
                                    {
                                        // ISSUE: object of a compiler-generated type is created
                                        salesEvent1 = new Time2WorkResourceBuyerSystem.SalesEvent();
                                        // ISSUE: reference to a compiler-generated field
                                        salesEvent1.m_Amount = resourceBuyingRequest.m_AmountNeeded;
                                        // ISSUE: reference to a compiler-generated field
                                        salesEvent1.m_Buyer = resourceBuyingRequest.m_Payer;
                                        // ISSUE: reference to a compiler-generated field
                                        salesEvent1.m_Seller = destination;
                                        // ISSUE: reference to a compiler-generated field
                                        salesEvent1.m_Resource = resourceBuyingRequest.m_ResourceNeeded;
                                        // ISSUE: reference to a compiler-generated field
                                        salesEvent1.m_Flags = saleFlags;
                                        // ISSUE: reference to a compiler-generated field
                                        salesEvent1.m_Distance = pathInformation.m_Distance;
                                        // ISSUE: variable of a compiler-generated type
                                        Time2WorkResourceBuyerSystem.SalesEvent salesEvent2 = salesEvent1;
                                        // ISSUE: reference to a compiler-generated field
                                        this.m_SalesQueue.Enqueue(salesEvent2);
                                    }
                                }
                                // ISSUE: reference to a compiler-generated field
                                // ISSUE: reference to a compiler-generated field
                                this.m_CommandBuffer.RemoveComponent(unfilteredChunkIndex, entity, in this.m_PathfindTypes);
                                // ISSUE: reference to a compiler-generated field
                                this.m_CommandBuffer.RemoveComponent<ResourceBuyer>(unfilteredChunkIndex, entity);
                                // ISSUE: reference to a compiler-generated field
                                // ISSUE: reference to a compiler-generated field
                                int population = this.m_Populations[this.m_City].m_Population;
                                // ISSUE: reference to a compiler-generated field
                                bool flag3 = citizens.Length > 0 && random.NextInt(100) < 100 - Mathf.RoundToInt(100f / math.max(1f, math.sqrt((float)((double)this.m_EconomyParameterData.m_TrafficReduction * (double)population * 0.10000000149011612))));

                                if (((virtualGood ? 0 : (!flag3 ? 1 : 0)) & (flag2 ? 1 : 0)) != 0)
                                {
                                    if (isCompanyChunk && !virtualGood)
                                    {
                                        // NEW: use the same goods-delivery pattern as BuildingUpkeepSystem

                                        // Figure out which entity should receive the goods.
                                        // For companies, we want the property/building they occupy.
                                        Entity resourceNeeder = resourceBuyingRequest.m_Payer;
                                        if (this.m_Properties.HasComponent(resourceNeeder))
                                        {
                                            resourceNeeder = this.m_Properties[resourceNeeder].m_Property;
                                        }

                                        Entity request = this.m_CommandBuffer.CreateEntity(
                                            unfilteredChunkIndex,
                                            this.m_GoodsDeliveryRequestArchetype
                                        );

                                        this.m_CommandBuffer.SetComponent(unfilteredChunkIndex, request,
                                            new GoodsDeliveryRequest
                                            {
                                                m_ResourceNeeder = resourceNeeder,
                                                m_Amount = resourceBuyingRequest.m_AmountNeeded,
                                                m_Resource = resourceBuyingRequest.m_ResourceNeeded
                                            });

                                        // Use the same group as BuildingUpkeepSystem (32U) so these
                                        // requests participate in the same multi-stop routing logic.
                                        this.m_CommandBuffer.SetComponent(unfilteredChunkIndex, request,
                                            new RequestGroup(32u));

                                        // IMPORTANT: do NOT add TripNeeded or CurrentTrading here.
                                        // The goods delivery / truck AI will pick up this request and
                                        // bundle it with other GoodsDeliveryRequest entities.
                                    }
                                    else
                                    {
                                        // EXISTING BEHAVIOR for households & citizens (and any virtual goods):
                                        //   - Add CurrentTrading on the buyer
                                        //   - Add TripNeeded to tripBuffer
                                        //   - Add Target if needed

                                        this.m_CommandBuffer.AddBuffer<CurrentTrading>(unfilteredChunkIndex, entity).Add(new CurrentTrading()
                                        {
                                            m_TradingResource = resourceBuyingRequest.m_ResourceNeeded,
                                            m_TradingResourceAmount = resourceBuyingRequest.m_AmountNeeded,
                                            m_OutsideConnectionType = this.m_OutsideConnections.HasComponent(destination) ? BuildingUtils.GetOutsideConnectionType(destination, ref this.m_PrefabRefData, ref this.m_OutsideConnectionDatas) : OutsideConnectionTransferType.None,
                                            m_TradingStartFrameIndex = this.m_FrameIndex
                                        });
                                        tripBuffer.Add(new TripNeeded()
                                        {
                                            m_TargetAgent = destination,
                                            m_Purpose = flag1 ? Game.Citizens.Purpose.CompanyShopping : Game.Citizens.Purpose.Shopping,
                                            m_Data = resourceBuyingRequest.m_AmountNeeded,
                                            m_Resource = resourceBuyingRequest.m_ResourceNeeded
                                        });
                                        // ISSUE: reference to a compiler-generated field
                                        if (!this.m_Targets.HasComponent(entities[index]))
                                        {
                                            // ISSUE: reference to a compiler-generated field
                                            this.m_CommandBuffer.AddComponent<Game.Common.Target>(unfilteredChunkIndex, entity, new Game.Common.Target()
                                            {
                                                m_Target = destination
                                            });
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            // ISSUE: reference to a compiler-generated field
                            this.m_CommandBuffer.RemoveComponent<ResourceBuyer>(unfilteredChunkIndex, entity);
                            // ISSUE: reference to a compiler-generated field
                            // ISSUE: reference to a compiler-generated field
                            this.m_CommandBuffer.RemoveComponent(unfilteredChunkIndex, entity, in this.m_PathfindTypes);
                            if (meetings.IsCreated)
                            {
                                AttendingMeeting meeting = meetings[index];
                                // ISSUE: reference to a compiler-generated field
                                Entity prefab = this.m_PrefabRefData[meeting.m_Meeting].m_Prefab;
                                // ISSUE: reference to a compiler-generated field
                                CoordinatedMeeting coordinatedMeeting = this.m_CoordinatedMeetings[meeting.m_Meeting];
                                // ISSUE: reference to a compiler-generated field
                                if (this.m_HaveCoordinatedMeetingDatas[prefab][coordinatedMeeting.m_Phase].m_TravelPurpose.m_Purpose == Game.Citizens.Purpose.Shopping)
                                {
                                    coordinatedMeeting.m_Status = MeetingStatus.Done;
                                    // ISSUE: reference to a compiler-generated field
                                    this.m_CoordinatedMeetings[meeting.m_Meeting] = coordinatedMeeting;
                                }
                            }
                        }
                    }
                }
                else
                {
                    // ISSUE: reference to a compiler-generated field
                    // ISSUE: reference to a compiler-generated field
                    // ISSUE: reference to a compiler-generated field
                    // ISSUE: reference to a compiler-generated field
                    // ISSUE: reference to a compiler-generated field
                    // ISSUE: reference to a compiler-generated field
                    // ISSUE: reference to a compiler-generated field
                    // ISSUE: reference to a compiler-generated field
                    if ((!this.m_HouseholdMembers.HasComponent(entity) || !this.m_TouristHouseholds.HasComponent(this.m_HouseholdMembers[entity].m_Household) && !this.m_CommuterHouseholds.HasComponent(this.m_HouseholdMembers[entity].m_Household)) && this.m_CurrentBuildings.HasComponent(entity) && this.m_OutsideConnections.HasComponent(this.m_CurrentBuildings[entity].m_CurrentBuilding) && !meetings.IsCreated)
                    {
                        // ISSUE: variable of a compiler-generated type
                        Time2WorkResourceBuyerSystem.SaleFlags saleFlags = Time2WorkResourceBuyerSystem.SaleFlags.ImportFromOC;
                        // ISSUE: object of a compiler-generated type is created
                        salesEvent1 = new Time2WorkResourceBuyerSystem.SalesEvent();
                        // ISSUE: reference to a compiler-generated field
                        salesEvent1.m_Amount = resourceBuyingRequest.m_AmountNeeded;
                        // ISSUE: reference to a compiler-generated field
                        salesEvent1.m_Buyer = resourceBuyingRequest.m_Payer;
                        // ISSUE: reference to a compiler-generated field
                        // ISSUE: reference to a compiler-generated field
                        salesEvent1.m_Seller = this.m_CurrentBuildings[entity].m_CurrentBuilding;
                        // ISSUE: reference to a compiler-generated field
                        salesEvent1.m_Resource = resourceBuyingRequest.m_ResourceNeeded;
                        // ISSUE: reference to a compiler-generated field
                        salesEvent1.m_Flags = saleFlags;
                        // ISSUE: reference to a compiler-generated field
                        salesEvent1.m_Distance = 0.0f;
                        // ISSUE: variable of a compiler-generated type
                        Time2WorkResourceBuyerSystem.SalesEvent salesEvent3 = salesEvent1;
                        // ISSUE: reference to a compiler-generated field
                        this.m_SalesQueue.Enqueue(salesEvent3);
                        // ISSUE: reference to a compiler-generated field
                        this.m_CommandBuffer.RemoveComponent<ResourceBuyer>(unfilteredChunkIndex, entity);
                    }
                    else
                    {
                        Citizen citizen1 = new Citizen();
                        if (citizens.Length > 0)
                        {
                            Citizen citizen2 = citizens[index];
                            // ISSUE: reference to a compiler-generated field
                            Entity household1 = this.m_HouseholdMembers[entity].m_Household;
                            // ISSUE: reference to a compiler-generated field
                            Household household2 = this.m_Households[household1];
                            // ISSUE: reference to a compiler-generated field
                            DynamicBuffer<HouseholdCitizen> householdCitizen = this.m_HouseholdCitizens[household1];
                            // ISSUE: reference to a compiler-generated method
                            this.FindShopForCitizen(chunk, unfilteredChunkIndex, entity, resourceBuyingRequest.m_ResourceNeeded, resourceBuyingRequest.m_AmountNeeded, resourceBuyingRequest.m_Flags, citizen2, household2, householdCitizen.Length, virtualGood);
                        }
                        else
                        {
                            // ISSUE: reference to a compiler-generated method
                            this.FindShopForCompany(chunk, unfilteredChunkIndex, entity, resourceBuyingRequest.m_ResourceNeeded, resourceBuyingRequest.m_AmountNeeded, resourceBuyingRequest.m_Flags, virtualGood);
                        }
                    }
                }
            }
        }

        private void FindShopForCitizen(
          ArchetypeChunk chunk,
          int index,
          Entity buyer,
          Resource resource,
          int amount,
          SetupTargetFlags flags,
          Citizen citizenData,
          Household householdData,
          int householdCitizenCount,
          bool virtualGood)
        {
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            this.m_CommandBuffer.AddComponent(index, buyer, in this.m_PathfindTypes);
            // ISSUE: reference to a compiler-generated field
            this.m_CommandBuffer.SetComponent<PathInformation>(index, buyer, new PathInformation()
            {
                m_State = PathFlags.Pending
            });
            CreatureData creatureData;
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated method
            Entity entity = ObjectEmergeSystem.SelectResidentPrefab(citizenData, this.m_HumanChunks, this.m_EntityType, ref this.m_CreatureDataType, ref this.m_ResidentDataType, out creatureData, out PseudoRandomSeed _);
            HumanData humanData = new HumanData();
            if (entity != Entity.Null)
            {
                // ISSUE: reference to a compiler-generated field
                humanData = this.m_PrefabHumanData[entity];
            }
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            PathfindParameters parameters = new PathfindParameters()
            {
                m_MaxSpeed = (float2)277.777771f,
                m_WalkSpeed = (float2)humanData.m_WalkSpeed,
                m_Weights = CitizenUtils.GetPathfindWeights(citizenData, householdData, householdCitizenCount),
                m_Methods = PathMethod.Pedestrian | PathMethod.Taxi | RouteUtils.GetPublicTransportMethods(this.m_TimeOfDay),
                m_TaxiIgnoredRules = VehicleUtils.GetIgnoredPathfindRulesTaxiDefaults(),
                m_MaxCost = CitizenBehaviorSystem.kMaxPathfindCost
            };
            SetupQueueTarget setupQueueTarget = new SetupQueueTarget();
            setupQueueTarget.m_Type = SetupTargetType.CurrentLocation;
            setupQueueTarget.m_Methods = PathMethod.Pedestrian;
            setupQueueTarget.m_RandomCost = 30f;
            SetupQueueTarget origin = setupQueueTarget;
            setupQueueTarget = new SetupQueueTarget();
            setupQueueTarget.m_Type = SetupTargetType.ResourceSeller;
            setupQueueTarget.m_Methods = PathMethod.Pedestrian;
            setupQueueTarget.m_Resource = resource;
            setupQueueTarget.m_Value = amount;
            setupQueueTarget.m_Flags = flags;
            setupQueueTarget.m_RandomCost = 30f;
            setupQueueTarget.m_ActivityMask = creatureData.m_SupportedActivities;
            SetupQueueTarget destination = setupQueueTarget;
            if (virtualGood)
                parameters.m_PathfindFlags |= PathfindFlags.SkipPathfind;
            Entity property = Entity.Null;
            HouseholdMember componentData1;
            PropertyRenter componentData2;
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            if (this.m_HouseholdMembers.TryGetComponent(buyer, out componentData1) && this.m_Properties.TryGetComponent(componentData1.m_Household, out componentData2))
            {
                property = componentData2.m_Property;
                parameters.m_Authorization1 = componentData2.m_Property;
            }
            // ISSUE: reference to a compiler-generated field
            if (this.m_Workers.HasComponent(buyer))
            {
                // ISSUE: reference to a compiler-generated field
                Worker worker = this.m_Workers[buyer];
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                parameters.m_Authorization2 = !this.m_Properties.HasComponent(worker.m_Workplace) ? worker.m_Workplace : this.m_Properties[worker.m_Workplace].m_Property;
            }
            // ISSUE: reference to a compiler-generated field
            if (this.m_CarKeepers.IsComponentEnabled(buyer))
            {
                // ISSUE: reference to a compiler-generated field
                Entity car = this.m_CarKeepers[buyer].m_Car;
                // ISSUE: reference to a compiler-generated field
                if (this.m_ParkedCarData.HasComponent(car))
                {
                    // ISSUE: reference to a compiler-generated field
                    PrefabRef prefabRef = this.m_PrefabRefData[car];
                    // ISSUE: reference to a compiler-generated field
                    ParkedCar parkedCar = this.m_ParkedCarData[car];
                    // ISSUE: reference to a compiler-generated field
                    CarData carData = this.m_PrefabCarData[prefabRef.m_Prefab];
                    parameters.m_MaxSpeed.x = carData.m_MaxSpeed;
                    parameters.m_ParkingTarget = parkedCar.m_Lane;
                    parameters.m_ParkingDelta = parkedCar.m_CurvePosition;
                    // ISSUE: reference to a compiler-generated field
                    // ISSUE: reference to a compiler-generated field
                    parameters.m_ParkingSize = VehicleUtils.GetParkingSize(car, ref this.m_PrefabRefData, ref this.m_ObjectGeometryData);
                    parameters.m_Methods |= VehicleUtils.GetPathMethods(carData) | PathMethod.Parking;
                    parameters.m_IgnoredRules = VehicleUtils.GetIgnoredPathfindRules(carData);
                    Game.Vehicles.PersonalCar componentData3;
                    // ISSUE: reference to a compiler-generated field
                    if (this.m_PersonalCarData.TryGetComponent(car, out componentData3) && (componentData3.m_State & PersonalCarFlags.HomeTarget) == (PersonalCarFlags)0)
                        parameters.m_PathfindFlags |= PathfindFlags.ParkingReset;
                }
            }
            else
            {
                // ISSUE: reference to a compiler-generated field
                if (this.m_BicycleOwners.IsComponentEnabled(buyer))
                {
                    // ISSUE: reference to a compiler-generated field
                    Entity bicycle = this.m_BicycleOwners[buyer].m_Bicycle;
                    PrefabRef componentData4;
                    CurrentBuilding componentData5;
                    // ISSUE: reference to a compiler-generated field
                    // ISSUE: reference to a compiler-generated field
                    if (!this.m_PrefabRefData.TryGetComponent(bicycle, out componentData4) && this.m_CurrentBuildings.TryGetComponent(buyer, out componentData5) && componentData5.m_CurrentBuilding == property)
                    {
                        Unity.Mathematics.Random pseudoRandom = citizenData.GetPseudoRandom(CitizenPseudoRandom.BicycleModel);
                        // ISSUE: reference to a compiler-generated field
                        componentData4.m_Prefab = this.m_PersonalCarSelectData.SelectVehiclePrefab(ref pseudoRandom, 1, 0, true, false, true, out Entity _);
                    }
                    CarData componentData6;
                    ObjectGeometryData componentData7;
                    // ISSUE: reference to a compiler-generated field
                    // ISSUE: reference to a compiler-generated field
                    if (this.m_PrefabCarData.TryGetComponent(componentData4.m_Prefab, out componentData6) && this.m_ObjectGeometryData.TryGetComponent(componentData4.m_Prefab, out componentData7))
                    {
                        parameters.m_MaxSpeed.x = componentData6.m_MaxSpeed;
                        parameters.m_ParkingSize = VehicleUtils.GetParkingSize(componentData7, out float _);
                        parameters.m_Methods |= PathMethod.Bicycle | PathMethod.BicycleParking;
                        parameters.m_IgnoredRules = VehicleUtils.GetIgnoredPathfindRulesBicycleDefaults();
                        ParkedCar componentData8;
                        // ISSUE: reference to a compiler-generated field
                        if (this.m_ParkedCarData.TryGetComponent(bicycle, out componentData8))
                        {
                            parameters.m_ParkingTarget = componentData8.m_Lane;
                            parameters.m_ParkingDelta = componentData8.m_CurvePosition;
                            Game.Vehicles.PersonalCar componentData9;
                            // ISSUE: reference to a compiler-generated field
                            if (this.m_PersonalCarData.TryGetComponent(bicycle, out componentData9) && (componentData9.m_State & PersonalCarFlags.HomeTarget) == (PersonalCarFlags)0)
                                parameters.m_PathfindFlags |= PathfindFlags.ParkingReset;
                        }
                        else
                        {
                            origin.m_Methods |= PathMethod.Bicycle;
                            origin.m_RoadTypes |= RoadTypes.Bicycle;
                        }
                    }
                }
            }
            // ISSUE: reference to a compiler-generated field
            this.m_PathfindQueue.Enqueue(new SetupQueueItem(buyer, parameters, origin, destination));
        }

        private void FindShopForCompany(
          ArchetypeChunk chunk,
          int index,
          Entity buyer,
          Resource resource,
          int amount,
          SetupTargetFlags flags,
          bool virtualGood)
        {
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            this.m_CommandBuffer.AddComponent(index, buyer, in this.m_PathfindTypes);
            // ISSUE: reference to a compiler-generated field
            this.m_CommandBuffer.SetComponent<PathInformation>(index, buyer, new PathInformation()
            {
                m_State = PathFlags.Pending
            });
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            float transportCost = EconomyUtils.GetTransportCost(100f, amount, this.m_ResourceDatas[this.m_ResourcePrefabs[resource]].m_Weight, StorageTransferFlags.Car);
            PathfindParameters parameters = new PathfindParameters()
            {
                m_MaxSpeed = (float2)111.111115f,
                m_WalkSpeed = (float2)5.555556f,
                m_Weights = new PathfindWeights(1f, 1f, transportCost, 1f),
                m_Methods = PathMethod.Road | PathMethod.CargoLoading,
                m_IgnoredRules = RuleFlags.ForbidSlowTraffic | RuleFlags.AvoidBicycles
            };
            SetupQueueTarget origin = new SetupQueueTarget()
            {
                m_Type = SetupTargetType.CurrentLocation,
                m_Methods = PathMethod.Road | PathMethod.CargoLoading,
                m_RoadTypes = RoadTypes.Car
            };
            SetupQueueTarget destination = new SetupQueueTarget()
            {
                m_Type = SetupTargetType.ResourceSeller,
                m_Methods = PathMethod.Road | PathMethod.CargoLoading,
                m_RoadTypes = RoadTypes.Car,
                m_Resource = resource,
                m_Value = amount,
                m_Flags = flags
            };
            if (virtualGood)
                parameters.m_PathfindFlags |= PathfindFlags.SkipPathfind;
            // ISSUE: reference to a compiler-generated field
            this.m_PathfindQueue.Enqueue(new SetupQueueItem(buyer, parameters, origin, destination));
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
        [ReadOnly]
        public ComponentTypeHandle<ResourceBuyer> __Game_Companies_ResourceBuyer_RO_ComponentTypeHandle;
        [ReadOnly]
        public ComponentTypeHandle<ResourceBought> __Game_Citizens_ResourceBought_RO_ComponentTypeHandle;
        public BufferTypeHandle<TripNeeded> __Game_Citizens_TripNeeded_RW_BufferTypeHandle;
        [ReadOnly]
        public ComponentTypeHandle<Citizen> __Game_Citizens_Citizen_RO_ComponentTypeHandle;
        [ReadOnly]
        public ComponentTypeHandle<CreatureData> __Game_Prefabs_CreatureData_RO_ComponentTypeHandle;
        [ReadOnly]
        public ComponentTypeHandle<ResidentData> __Game_Prefabs_ResidentData_RO_ComponentTypeHandle;
        [ReadOnly]
        public ComponentTypeHandle<AttendingMeeting> __Game_Citizens_AttendingMeeting_RO_ComponentTypeHandle;
        [ReadOnly]
        public ComponentLookup<ServiceAvailable> __Game_Companies_ServiceAvailable_RO_ComponentLookup;
        [ReadOnly]
        public ComponentLookup<PathInformation> __Game_Pathfind_PathInformation_RO_ComponentLookup;
        [ReadOnly]
        public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;
        [ReadOnly]
        public ComponentLookup<CarKeeper> __Game_Citizens_CarKeeper_RO_ComponentLookup;
        [ReadOnly]
        public ComponentLookup<BicycleOwner> __Game_Citizens_BicycleOwner_RO_ComponentLookup;
        [ReadOnly]
        public ComponentLookup<ParkedCar> __Game_Vehicles_ParkedCar_RO_ComponentLookup;
        [ReadOnly]
        public ComponentLookup<Game.Vehicles.PersonalCar> __Game_Vehicles_PersonalCar_RO_ComponentLookup;
        [ReadOnly]
        public ComponentLookup<Game.Common.Target> __Game_Common_Target_RO_ComponentLookup;
        [ReadOnly]
        public ComponentLookup<CurrentBuilding> __Game_Citizens_CurrentBuilding_RO_ComponentLookup;
        [ReadOnly]
        public ComponentLookup<Game.Objects.OutsideConnection> __Game_Objects_OutsideConnection_RO_ComponentLookup;
        [ReadOnly]
        public ComponentLookup<HouseholdMember> __Game_Citizens_HouseholdMember_RO_ComponentLookup;
        [ReadOnly]
        public ComponentLookup<Household> __Game_Citizens_Household_RO_ComponentLookup;
        [ReadOnly]
        public ComponentLookup<TouristHousehold> __Game_Citizens_TouristHousehold_RO_ComponentLookup;
        [ReadOnly]
        public ComponentLookup<CommuterHousehold> __Game_Citizens_CommuterHousehold_RO_ComponentLookup;
        [ReadOnly]
        public ComponentLookup<Worker> __Game_Citizens_Worker_RO_ComponentLookup;
        [ReadOnly]
        public ComponentLookup<Game.Vehicles.DeliveryTruck> __Game_Vehicles_DeliveryTruck_RO_ComponentLookup;
        [ReadOnly]
        public ComponentLookup<Game.Companies.StorageCompany> __Game_Companies_StorageCompany_RO_ComponentLookup;
        [ReadOnly]
        public BufferLookup<Game.Economy.Resources> __Game_Economy_Resources_RO_BufferLookup;
        [ReadOnly]
        public BufferLookup<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferLookup;
        [ReadOnly]
        public BufferLookup<OwnedVehicle> __Game_Vehicles_OwnedVehicle_RO_BufferLookup;
        [ReadOnly]
        public BufferLookup<GuestVehicle> __Game_Vehicles_GuestVehicle_RO_BufferLookup;
        [ReadOnly]
        public BufferLookup<LayoutElement> __Game_Vehicles_LayoutElement_RO_BufferLookup;
        [ReadOnly]
        public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;
        [ReadOnly]
        public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;
        [ReadOnly]
        public ComponentLookup<CarData> __Game_Prefabs_CarData_RO_ComponentLookup;
        [ReadOnly]
        public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;
        [ReadOnly]
        public ComponentLookup<HumanData> __Game_Prefabs_HumanData_RO_ComponentLookup;
        public ComponentLookup<CoordinatedMeeting> __Game_Citizens_CoordinatedMeeting_RW_ComponentLookup;
        [ReadOnly]
        public BufferLookup<HaveCoordinatedMeetingData> __Game_Prefabs_HaveCoordinatedMeetingData_RO_BufferLookup;
        [ReadOnly]
        public ComponentLookup<OutsideConnectionData> __Game_Prefabs_OutsideConnectionData_RO_ComponentLookup;
        public ComponentLookup<Population> __Game_City_Population_RW_ComponentLookup;
        public BufferLookup<Game.Economy.Resources> __Game_Economy_Resources_RW_BufferLookup;
        public ComponentLookup<ServiceAvailable> __Game_Companies_ServiceAvailable_RW_ComponentLookup;
        [ReadOnly]
        public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;
        [ReadOnly]
        public BufferLookup<HouseholdAnimal> __Game_Citizens_HouseholdAnimal_RO_BufferLookup;
        [ReadOnly]
        public ComponentLookup<ServiceCompanyData> __Game_Companies_ServiceCompanyData_RO_ComponentLookup;
        public ComponentLookup<Household> __Game_Citizens_Household_RW_ComponentLookup;
        public ComponentLookup<BuyingCompany> __Game_Companies_BuyingCompany_RW_ComponentLookup;
        public BufferLookup<TradeCost> __Game_Companies_TradeCost_RW_BufferLookup;
        public ComponentLookup<CompanyStatisticData> __Game_Companies_CompanyStatisticData_RW_ComponentLookup;
        [ReadOnly]
        public ComponentLookup<Population> __Game_City_Population_RO_ComponentLookup;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void __AssignHandles(ref SystemState state)
        {
            // ISSUE: reference to a compiler-generated field
            this.__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
            // ISSUE: reference to a compiler-generated field
            this.__Game_Companies_ResourceBuyer_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ResourceBuyer>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Citizens_ResourceBought_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ResourceBought>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Citizens_TripNeeded_RW_BufferTypeHandle = state.GetBufferTypeHandle<TripNeeded>();
            // ISSUE: reference to a compiler-generated field
            this.__Game_Citizens_Citizen_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Citizen>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Prefabs_CreatureData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CreatureData>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Prefabs_ResidentData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ResidentData>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Citizens_AttendingMeeting_RO_ComponentTypeHandle = state.GetComponentTypeHandle<AttendingMeeting>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Companies_ServiceAvailable_RO_ComponentLookup = state.GetComponentLookup<ServiceAvailable>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Pathfind_PathInformation_RO_ComponentLookup = state.GetComponentLookup<PathInformation>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Citizens_CarKeeper_RO_ComponentLookup = state.GetComponentLookup<CarKeeper>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Citizens_BicycleOwner_RO_ComponentLookup = state.GetComponentLookup<BicycleOwner>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Vehicles_ParkedCar_RO_ComponentLookup = state.GetComponentLookup<ParkedCar>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Vehicles_PersonalCar_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.PersonalCar>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Common_Target_RO_ComponentLookup = state.GetComponentLookup<Game.Common.Target>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Citizens_CurrentBuilding_RO_ComponentLookup = state.GetComponentLookup<CurrentBuilding>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Objects_OutsideConnection_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.OutsideConnection>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Citizens_HouseholdMember_RO_ComponentLookup = state.GetComponentLookup<HouseholdMember>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Citizens_Household_RO_ComponentLookup = state.GetComponentLookup<Household>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Citizens_TouristHousehold_RO_ComponentLookup = state.GetComponentLookup<TouristHousehold>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Citizens_CommuterHousehold_RO_ComponentLookup = state.GetComponentLookup<CommuterHousehold>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Citizens_Worker_RO_ComponentLookup = state.GetComponentLookup<Worker>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Vehicles_DeliveryTruck_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.DeliveryTruck>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Companies_StorageCompany_RO_ComponentLookup = state.GetComponentLookup<Game.Companies.StorageCompany>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Economy_Resources_RO_BufferLookup = state.GetBufferLookup<Game.Economy.Resources>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Citizens_HouseholdCitizen_RO_BufferLookup = state.GetBufferLookup<HouseholdCitizen>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Vehicles_OwnedVehicle_RO_BufferLookup = state.GetBufferLookup<OwnedVehicle>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Vehicles_GuestVehicle_RO_BufferLookup = state.GetBufferLookup<GuestVehicle>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Vehicles_LayoutElement_RO_BufferLookup = state.GetBufferLookup<LayoutElement>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Prefabs_CarData_RO_ComponentLookup = state.GetComponentLookup<CarData>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Prefabs_HumanData_RO_ComponentLookup = state.GetComponentLookup<HumanData>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Citizens_CoordinatedMeeting_RW_ComponentLookup = state.GetComponentLookup<CoordinatedMeeting>();
            // ISSUE: reference to a compiler-generated field
            this.__Game_Prefabs_HaveCoordinatedMeetingData_RO_BufferLookup = state.GetBufferLookup<HaveCoordinatedMeetingData>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Prefabs_OutsideConnectionData_RO_ComponentLookup = state.GetComponentLookup<OutsideConnectionData>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_City_Population_RW_ComponentLookup = state.GetComponentLookup<Population>();
            // ISSUE: reference to a compiler-generated field
            this.__Game_Economy_Resources_RW_BufferLookup = state.GetBufferLookup<Game.Economy.Resources>();
            // ISSUE: reference to a compiler-generated field
            this.__Game_Companies_ServiceAvailable_RW_ComponentLookup = state.GetComponentLookup<ServiceAvailable>();
            // ISSUE: reference to a compiler-generated field
            this.__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Citizens_HouseholdAnimal_RO_BufferLookup = state.GetBufferLookup<HouseholdAnimal>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Companies_ServiceCompanyData_RO_ComponentLookup = state.GetComponentLookup<ServiceCompanyData>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Citizens_Household_RW_ComponentLookup = state.GetComponentLookup<Household>();
            // ISSUE: reference to a compiler-generated field
            this.__Game_Companies_BuyingCompany_RW_ComponentLookup = state.GetComponentLookup<BuyingCompany>();
            // ISSUE: reference to a compiler-generated field
            this.__Game_Companies_TradeCost_RW_BufferLookup = state.GetBufferLookup<TradeCost>();
            // ISSUE: reference to a compiler-generated field
            this.__Game_Companies_CompanyStatisticData_RW_ComponentLookup = state.GetComponentLookup<CompanyStatisticData>();
            // ISSUE: reference to a compiler-generated field
            this.__Game_City_Population_RO_ComponentLookup = state.GetComponentLookup<Population>(true);
        }
    }
}
