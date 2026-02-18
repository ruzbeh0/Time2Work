using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Notifications;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Vehicles;
using Game.Simulation;
using Game;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Mathematics;

#nullable disable
namespace Time2Work.Systems;

[CompilerGenerated]
public partial class Time2WorkBuyingCompanySystem : GameSystemBase
{
    private static readonly float kNotificationCostLimit = 5f;
    private static readonly int kResourceLowStockAmount = 2000;
    private static readonly int kResourceMinimumRequestAmount = 4000;
    private SimulationSystem m_SimulationSystem;
    private ResourceSystem m_ResourceSystem;
    private VehicleCapacitySystem m_VehicleCapacitySystem;
    private EndFrameBarrier m_EndFrameBarrier;
    private IconCommandSystem m_IconCommandSystem;
    private EntityQuery m_CompanyNotificationParameterQuery;
    private EntityQuery m_CompanyGroup;
    private Time2WorkBuyingCompanySystem.TypeHandle __TypeHandle;

    public override int GetUpdateInterval(SystemUpdatePhase phase) => 256 /*0x0100*/;

    [UnityEngine.Scripting.Preserve]
    protected override void OnCreate()
    {
        base.OnCreate();
        // ISSUE: reference to a compiler-generated field
        this.m_SimulationSystem = this.World.GetOrCreateSystemManaged<SimulationSystem>();
        // ISSUE: reference to a compiler-generated field
        this.m_ResourceSystem = this.World.GetOrCreateSystemManaged<ResourceSystem>();
        // ISSUE: reference to a compiler-generated field
        this.m_VehicleCapacitySystem = this.World.GetOrCreateSystemManaged<VehicleCapacitySystem>();
        // ISSUE: reference to a compiler-generated field
        this.m_EndFrameBarrier = this.World.GetOrCreateSystemManaged<EndFrameBarrier>();
        // ISSUE: reference to a compiler-generated field
        this.m_IconCommandSystem = this.World.GetOrCreateSystemManaged<IconCommandSystem>();
        // ISSUE: reference to a compiler-generated field
        this.m_CompanyGroup = this.GetEntityQuery(ComponentType.ReadOnly<BuyingCompany>(), ComponentType.ReadOnly<Resources>(), ComponentType.ReadWrite<OwnedVehicle>(), ComponentType.ReadOnly<PropertyRenter>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<TradeCost>(), ComponentType.ReadWrite<CompanyNotifications>(), ComponentType.ReadWrite<TripNeeded>(), ComponentType.Exclude<ResourceBuyer>(), ComponentType.Exclude<Deleted>(), ComponentType.ReadOnly<UpdateFrame>());
        // ISSUE: reference to a compiler-generated field
        this.m_CompanyNotificationParameterQuery = this.GetEntityQuery(ComponentType.ReadOnly<CompanyNotificationParameterData>());
        // ISSUE: reference to a compiler-generated field
        this.RequireForUpdate(this.m_CompanyGroup);
        // ISSUE: reference to a compiler-generated field
        this.RequireForUpdate(this.m_CompanyNotificationParameterQuery);
    }

    [UnityEngine.Scripting.Preserve]
    protected override void OnUpdate()
    {
        // ISSUE: reference to a compiler-generated field
        uint frameWithInterval = SimulationUtils.GetUpdateFrameWithInterval(this.m_SimulationSystem.frameIndex, (uint)this.GetUpdateInterval(SystemUpdatePhase.GameSimulation), 16 /*0x10*/);
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
        // ISSUE: reference to a compiler-generated method
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated method
        // ISSUE: object of a compiler-generated type is created
        // ISSUE: variable of a compiler-generated type
        Time2WorkBuyingCompanySystem.CompanyBuyJob jobData = new Time2WorkBuyingCompanySystem.CompanyBuyJob()
        {
            m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref this.__TypeHandle.__Unity_Entities_Entity_TypeHandle, ref this.CheckedStateRef),
            m_ResourceBufType = InternalCompilerInterface.GetBufferTypeHandle<Resources>(ref this.__TypeHandle.__Game_Economy_Resources_RO_BufferTypeHandle, ref this.CheckedStateRef),
            m_PrefabType = InternalCompilerInterface.GetComponentTypeHandle<PrefabRef>(ref this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref this.CheckedStateRef),
            m_VehicleBufType = InternalCompilerInterface.GetBufferTypeHandle<OwnedVehicle>(ref this.__TypeHandle.__Game_Vehicles_OwnedVehicle_RO_BufferTypeHandle, ref this.CheckedStateRef),
            m_TripNeededBufType = InternalCompilerInterface.GetBufferTypeHandle<TripNeeded>(ref this.__TypeHandle.__Game_Citizens_TripNeeded_RO_BufferTypeHandle, ref this.CheckedStateRef),
            m_TradeCostBufType = InternalCompilerInterface.GetBufferTypeHandle<TradeCost>(ref this.__TypeHandle.__Game_Companies_TradeCost_RO_BufferTypeHandle, ref this.CheckedStateRef),
            m_PropertyRenterType = InternalCompilerInterface.GetComponentTypeHandle<PropertyRenter>(ref this.__TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentTypeHandle, ref this.CheckedStateRef),
            m_CompanyNotificationsType = InternalCompilerInterface.GetComponentTypeHandle<CompanyNotifications>(ref this.__TypeHandle.__Game_Companies_CompanyNotifications_RW_ComponentTypeHandle, ref this.CheckedStateRef),
            m_UpdateFrameType = InternalCompilerInterface.GetSharedComponentTypeHandle<UpdateFrame>(ref this.__TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, ref this.CheckedStateRef),
            m_IndustrialProcessDatas = InternalCompilerInterface.GetComponentLookup<IndustrialProcessData>(ref this.__TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup, ref this.CheckedStateRef),
            m_PropertyRenters = InternalCompilerInterface.GetComponentLookup<PropertyRenter>(ref this.__TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup, ref this.CheckedStateRef),
            m_StorageLimits = InternalCompilerInterface.GetComponentLookup<StorageLimitData>(ref this.__TypeHandle.__Game_Companies_StorageLimitData_RO_ComponentLookup, ref this.CheckedStateRef),
            m_Trucks = InternalCompilerInterface.GetComponentLookup<Game.Vehicles.DeliveryTruck>(ref this.__TypeHandle.__Game_Vehicles_DeliveryTruck_RO_ComponentLookup, ref this.CheckedStateRef),
            m_Transforms = InternalCompilerInterface.GetComponentLookup<Transform>(ref this.__TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref this.CheckedStateRef),
            m_Layouts = InternalCompilerInterface.GetBufferLookup<LayoutElement>(ref this.__TypeHandle.__Game_Vehicles_LayoutElement_RO_BufferLookup, ref this.CheckedStateRef),
            m_ResourcePrefabs = this.m_ResourceSystem.GetPrefabs(),
            m_ResourceDatas = InternalCompilerInterface.GetComponentLookup<ResourceData>(ref this.__TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup, ref this.CheckedStateRef),
            m_CompanyNotificationParameters = this.m_CompanyNotificationParameterQuery.GetSingleton<CompanyNotificationParameterData>(),
            m_CommandBuffer = this.m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
            m_DeliveryTruckSelectData = this.m_VehicleCapacitySystem.GetDeliveryTruckSelectData(),
            m_UpdateFrameIndex = frameWithInterval,
            m_IconCommandBuffer = this.m_IconCommandSystem.CreateCommandBuffer(),
            m_RandomSeed = RandomSeed.Next()
        };
        // ISSUE: reference to a compiler-generated field
        this.Dependency = jobData.ScheduleParallel<Time2WorkBuyingCompanySystem.CompanyBuyJob>(this.m_CompanyGroup, this.Dependency);
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated method
        this.m_ResourceSystem.AddPrefabsReader(this.Dependency);
        // ISSUE: reference to a compiler-generated field
        this.m_EndFrameBarrier.AddJobHandleForProducer(this.Dependency);
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated method
        this.m_IconCommandSystem.AddCommandBufferWriter(this.Dependency);
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
    public Time2WorkBuyingCompanySystem()
    {
    }

    [BurstCompile]
    private struct CompanyBuyJob : IJobChunk
    {
        [ReadOnly]
        public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;
        [ReadOnly]
        public EntityTypeHandle m_EntityType;
        [ReadOnly]
        public BufferTypeHandle<OwnedVehicle> m_VehicleBufType;
        [ReadOnly]
        public BufferTypeHandle<Resources> m_ResourceBufType;
        [ReadOnly]
        public BufferTypeHandle<TripNeeded> m_TripNeededBufType;
        [ReadOnly]
        public BufferTypeHandle<TradeCost> m_TradeCostBufType;
        [ReadOnly]
        public ComponentTypeHandle<PrefabRef> m_PrefabType;
        [ReadOnly]
        public ComponentTypeHandle<PropertyRenter> m_PropertyRenterType;
        public ComponentTypeHandle<CompanyNotifications> m_CompanyNotificationsType;
        [ReadOnly]
        public ComponentLookup<StorageLimitData> m_StorageLimits;
        [ReadOnly]
        public ComponentLookup<IndustrialProcessData> m_IndustrialProcessDatas;
        [ReadOnly]
        public ComponentLookup<Game.Vehicles.DeliveryTruck> m_Trucks;
        [ReadOnly]
        public ComponentLookup<PropertyRenter> m_PropertyRenters;
        [ReadOnly]
        public ComponentLookup<Transform> m_Transforms;
        [ReadOnly]
        public ComponentLookup<ResourceData> m_ResourceDatas;
        [ReadOnly]
        public BufferLookup<LayoutElement> m_Layouts;
        [ReadOnly]
        public DeliveryTruckSelectData m_DeliveryTruckSelectData;
        [ReadOnly]
        public ResourcePrefabs m_ResourcePrefabs;
        [ReadOnly]
        public uint m_UpdateFrameIndex;
        [ReadOnly]
        public CompanyNotificationParameterData m_CompanyNotificationParameters;
        [ReadOnly]
        public RandomSeed m_RandomSeed;
        public EntityCommandBuffer.ParallelWriter m_CommandBuffer;
        public IconCommandBuffer m_IconCommandBuffer;

        public void Execute(
          in ArchetypeChunk chunk,
          int unfilteredChunkIndex,
          bool useEnabledMask,
          in v128 chunkEnabledMask)
        {
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            if ((int)chunk.GetSharedComponent<UpdateFrame>(this.m_UpdateFrameType).m_Index != (int)this.m_UpdateFrameIndex)
                return;
            // ISSUE: reference to a compiler-generated field
            Random random = this.m_RandomSeed.GetRandom(unfilteredChunkIndex);
            // ISSUE: reference to a compiler-generated field
            NativeArray<Entity> nativeArray1 = chunk.GetNativeArray(this.m_EntityType);
            // ISSUE: reference to a compiler-generated field
            BufferAccessor<OwnedVehicle> bufferAccessor1 = chunk.GetBufferAccessor<OwnedVehicle>(ref this.m_VehicleBufType);
            // ISSUE: reference to a compiler-generated field
            BufferAccessor<TripNeeded> bufferAccessor2 = chunk.GetBufferAccessor<TripNeeded>(ref this.m_TripNeededBufType);
            // ISSUE: reference to a compiler-generated field
            BufferAccessor<Resources> bufferAccessor3 = chunk.GetBufferAccessor<Resources>(ref this.m_ResourceBufType);
            // ISSUE: reference to a compiler-generated field
            NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray<PrefabRef>(ref this.m_PrefabType);
            // ISSUE: reference to a compiler-generated field
            BufferAccessor<TradeCost> bufferAccessor4 = chunk.GetBufferAccessor<TradeCost>(ref this.m_TradeCostBufType);
            // ISSUE: reference to a compiler-generated field
            NativeArray<CompanyNotifications> nativeArray3 = chunk.GetNativeArray<CompanyNotifications>(ref this.m_CompanyNotificationsType);
            // ISSUE: reference to a compiler-generated field
            NativeArray<PropertyRenter> nativeArray4 = chunk.GetNativeArray<PropertyRenter>(ref this.m_PropertyRenterType);
            for (int index = 0; index < nativeArray1.Length; ++index)
            {
                Entity entity = nativeArray1[index];
                CompanyNotifications companyNotifications = nativeArray3[index];
                DynamicBuffer<OwnedVehicle> vehicles = bufferAccessor1[index];
                DynamicBuffer<TradeCost> tradeCosts = bufferAccessor4[index];
                DynamicBuffer<Resources> resourceBuffers = bufferAccessor3[index];
                DynamicBuffer<TripNeeded> trips = bufferAccessor2[index];
                int num1 = int.MaxValue;
                Entity prefab = nativeArray2[index].m_Prefab;
                // ISSUE: reference to a compiler-generated field
                if (this.m_StorageLimits.HasComponent(prefab))
                {
                    // ISSUE: reference to a compiler-generated field
                    num1 = this.m_StorageLimits[prefab].m_Limit;
                }
                // ISSUE: reference to a compiler-generated field
                IndustrialProcessData industrialProcessData = this.m_IndustrialProcessDatas[prefab];
                Entity owner = entity;
                if (nativeArray4.Length > 0)
                    owner = nativeArray4[index].m_Property;
                Resource needResource = Resource.NoResource;
                int needResourceLeft = 0;
                int storageLeft = num1;
                bool expensive = false;
                bool flag1 = industrialProcessData.m_Input2.m_Resource > Resource.NoResource;
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                bool flag2 = !flag1 && (industrialProcessData.m_Output.m_Resource == industrialProcessData.m_Input1.m_Resource || (double)this.m_ResourceDatas[this.m_ResourcePrefabs[industrialProcessData.m_Output.m_Resource]].m_Weight <= 0.0);
                int num2 = num1;
                int num3 = flag1 ? 2 : 1;
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                if (industrialProcessData.m_Output.m_Resource != industrialProcessData.m_Input1.m_Resource && (double)this.m_ResourceDatas[this.m_ResourcePrefabs[industrialProcessData.m_Output.m_Resource]].m_Weight > 0.0)
                    ++num3;
                int maxCapacity = num2 / num3;
                // ISSUE: reference to a compiler-generated method
                this.CalculateResourceNeeded(true, industrialProcessData.m_Input1.m_Resource, maxCapacity, ref needResource, ref needResourceLeft, ref storageLeft, ref expensive, tradeCosts, resourceBuffers, vehicles, trips);
                if (flag1)
                {
                    // ISSUE: reference to a compiler-generated method
                    this.CalculateResourceNeeded(true, industrialProcessData.m_Input2.m_Resource, maxCapacity, ref needResource, ref needResourceLeft, ref storageLeft, ref expensive, tradeCosts, resourceBuffers, vehicles, trips);
                }
                if (industrialProcessData.m_Output.m_Resource != industrialProcessData.m_Input1.m_Resource)
                {
                    // ISSUE: reference to a compiler-generated method
                    this.CalculateResourceNeeded(false, industrialProcessData.m_Output.m_Resource, maxCapacity, ref needResource, ref needResourceLeft, ref storageLeft, ref expensive, tradeCosts, resourceBuffers, vehicles, trips);
                }
                if (companyNotifications.m_NoInputEntity == new Entity() & expensive)
                {
                    // ISSUE: reference to a compiler-generated field
                    // ISSUE: reference to a compiler-generated field
                    this.m_IconCommandBuffer.Add(owner, this.m_CompanyNotificationParameters.m_NoInputsNotificationPrefab, IconPriority.Problem);
                    companyNotifications.m_NoInputEntity = owner;
                    nativeArray3[index] = companyNotifications;
                }
                else if (companyNotifications.m_NoInputEntity != new Entity())
                {
                    if (!expensive)
                    {
                        // ISSUE: reference to a compiler-generated field
                        // ISSUE: reference to a compiler-generated field
                        this.m_IconCommandBuffer.Remove(companyNotifications.m_NoInputEntity, this.m_CompanyNotificationParameters.m_NoInputsNotificationPrefab);
                        companyNotifications.m_NoInputEntity = Entity.Null;
                        nativeArray3[index] = companyNotifications;
                    }
                    else if (owner != companyNotifications.m_NoInputEntity)
                    {
                        // ISSUE: reference to a compiler-generated field
                        // ISSUE: reference to a compiler-generated field
                        this.m_IconCommandBuffer.Remove(companyNotifications.m_NoInputEntity, this.m_CompanyNotificationParameters.m_NoInputsNotificationPrefab);
                        // ISSUE: reference to a compiler-generated field
                        // ISSUE: reference to a compiler-generated field
                        this.m_IconCommandBuffer.Add(owner, this.m_CompanyNotificationParameters.m_NoInputsNotificationPrefab, IconPriority.Problem);
                        companyNotifications.m_NoInputEntity = owner;
                        nativeArray3[index] = companyNotifications;
                    }
                }
                if (needResource != Resource.NoResource)
                {
                    int max;
                    // ISSUE: reference to a compiler-generated field
                    this.m_DeliveryTruckSelectData.GetCapacityRange(needResource, out int _, out max);
                    // ISSUE: reference to a compiler-generated field
                    int num4 = Time2WorkBuyingCompanySystem.kResourceMinimumRequestAmount;
                    DeliveryTruckSelectItem deliveryTruckSelectItem = new DeliveryTruckSelectItem();
                    // ISSUE: reference to a compiler-generated field
                    // ISSUE: reference to a compiler-generated field
                    if ((double)this.m_ResourceDatas[this.m_ResourcePrefabs[needResource]].m_Weight > 0.0)
                    {
                        num4 = math.min(storageLeft, max);
                        // ISSUE: reference to a compiler-generated field
                        if (num4 > Time2WorkBuyingCompanySystem.kResourceMinimumRequestAmount)
                        {
                            // ISSUE: reference to a compiler-generated field
                            this.m_DeliveryTruckSelectData.TrySelectItem(ref random, needResource, num4, out deliveryTruckSelectItem);
                        }
                        else
                            continue;
                    }
                    else
                        deliveryTruckSelectItem.m_Capacity = num4;
                    // ISSUE: reference to a compiler-generated field
                    if (this.m_PropertyRenters.HasComponent(entity))
                    {
                        // ISSUE: reference to a compiler-generated field
                        Entity property = this.m_PropertyRenters[entity].m_Property;
                        // ISSUE: reference to a compiler-generated field
                        if (this.m_Transforms.HasComponent(property))
                        {
                            // ISSUE: reference to a compiler-generated field
                            ResourceBuyer component = new ResourceBuyer()
                            {
                                m_Payer = entity,
                                m_AmountNeeded = math.min(num4, deliveryTruckSelectItem.m_Capacity),
                                m_Flags = SetupTargetFlags.Industrial | SetupTargetFlags.Import,
                                m_Location = this.m_Transforms[property].m_Position,
                                m_ResourceNeeded = needResource
                            };
                            // ISSUE: reference to a compiler-generated field
                            this.m_CommandBuffer.AddComponent<ResourceBuyer>(unfilteredChunkIndex, entity, component);
                        }
                    }
                }
            }
        }

        private void CalculateResourceNeeded(
          bool isInput,
          Resource resource,
          int maxCapacity,
          ref Resource needResource,
          ref int needResourceLeft,
          ref int storageLeft,
          ref bool expensive,
          DynamicBuffer<TradeCost> tradeCosts,
          DynamicBuffer<Resources> resourceBuffers,
          DynamicBuffer<OwnedVehicle> vehicles,
          DynamicBuffer<TripNeeded> trips)
        {
            int resources = EconomyUtils.GetResources(resource, resourceBuffers);
            if (isInput)
            {
                // ISSUE: reference to a compiler-generated field
                if ((double)EconomyUtils.GetTradeCost(resource, tradeCosts).m_BuyCost > (double)Time2WorkBuyingCompanySystem.kNotificationCostLimit)
                    expensive = true;
                for (int index = 0; index < vehicles.Length; ++index)
                {
                    Entity vehicle = vehicles[index].m_Vehicle;
                    // ISSUE: reference to a compiler-generated field
                    // ISSUE: reference to a compiler-generated field
                    resources += VehicleUtils.GetBuyingTrucksLoad(vehicle, resource, ref this.m_Trucks, ref this.m_Layouts);
                }
                for (int index = 0; index < trips.Length; ++index)
                {
                    TripNeeded trip = trips[index];
                    if (trip.m_Purpose == Game.Citizens.Purpose.Shopping && trip.m_Resource == resource)
                        resources += trip.m_Data;
                }
                // ISSUE: reference to a compiler-generated field
                int num = (int)math.max((float)Time2WorkBuyingCompanySystem.kResourceLowStockAmount, (float)maxCapacity * 0.15f);
                if (needResource == Resource.NoResource && resources < num)
                {
                    needResource = resource;
                    needResourceLeft = resources;
                }
            }
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            if (!EconomyUtils.IsResourceHasWeight(resource, this.m_ResourcePrefabs, ref this.m_ResourceDatas))
                return;
            storageLeft -= resources;
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
        public BufferTypeHandle<Resources> __Game_Economy_Resources_RO_BufferTypeHandle;
        [ReadOnly]
        public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;
        [ReadOnly]
        public BufferTypeHandle<OwnedVehicle> __Game_Vehicles_OwnedVehicle_RO_BufferTypeHandle;
        [ReadOnly]
        public BufferTypeHandle<TripNeeded> __Game_Citizens_TripNeeded_RO_BufferTypeHandle;
        [ReadOnly]
        public BufferTypeHandle<TradeCost> __Game_Companies_TradeCost_RO_BufferTypeHandle;
        [ReadOnly]
        public ComponentTypeHandle<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentTypeHandle;
        public ComponentTypeHandle<CompanyNotifications> __Game_Companies_CompanyNotifications_RW_ComponentTypeHandle;
        public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;
        [ReadOnly]
        public ComponentLookup<IndustrialProcessData> __Game_Prefabs_IndustrialProcessData_RO_ComponentLookup;
        [ReadOnly]
        public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;
        [ReadOnly]
        public ComponentLookup<StorageLimitData> __Game_Companies_StorageLimitData_RO_ComponentLookup;
        [ReadOnly]
        public ComponentLookup<Game.Vehicles.DeliveryTruck> __Game_Vehicles_DeliveryTruck_RO_ComponentLookup;
        [ReadOnly]
        public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;
        [ReadOnly]
        public BufferLookup<LayoutElement> __Game_Vehicles_LayoutElement_RO_BufferLookup;
        [ReadOnly]
        public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void __AssignHandles(ref SystemState state)
        {
            // ISSUE: reference to a compiler-generated field
            this.__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
            // ISSUE: reference to a compiler-generated field
            this.__Game_Economy_Resources_RO_BufferTypeHandle = state.GetBufferTypeHandle<Resources>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Vehicles_OwnedVehicle_RO_BufferTypeHandle = state.GetBufferTypeHandle<OwnedVehicle>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Citizens_TripNeeded_RO_BufferTypeHandle = state.GetBufferTypeHandle<TripNeeded>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Companies_TradeCost_RO_BufferTypeHandle = state.GetBufferTypeHandle<TradeCost>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Buildings_PropertyRenter_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PropertyRenter>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Companies_CompanyNotifications_RW_ComponentTypeHandle = state.GetComponentTypeHandle<CompanyNotifications>();
            // ISSUE: reference to a compiler-generated field
            this.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
            // ISSUE: reference to a compiler-generated field
            this.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup = state.GetComponentLookup<IndustrialProcessData>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Companies_StorageLimitData_RO_ComponentLookup = state.GetComponentLookup<StorageLimitData>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Vehicles_DeliveryTruck_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.DeliveryTruck>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Vehicles_LayoutElement_RO_BufferLookup = state.GetBufferLookup<LayoutElement>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(true);
        }
    }
}
