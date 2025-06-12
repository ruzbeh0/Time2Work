
using Game;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Net;
using Game.Pathfind;
using Game.Prefabs;
using Game.Routes;
using Game.Simulation;
using Game.Tools;
using Game.Vehicles;
using System;
using System.Runtime.CompilerServices;
using Time2Work.Utils;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

#nullable disable
namespace Time2Work
{
    public partial class Time2WorkStorageTransferSystem : GameSystemBase
    {
        public static readonly float kStorageProfit = 0.01f;
        public static readonly float kMaxTransportUnitCost = 0.01f;
        private EntityQuery m_TransferGroup;
        private PathfindSetupSystem m_PathfindSetupSystem;
        private EndFrameBarrier m_EndFrameBarrier;
        private ResourceSystem m_ResourceSystem;
        private VehicleCapacitySystem m_VehicleCapacitySystem;
        private Time2WorkTimeSystem m_TimeSystem;
        private CitySystem m_CitySystem;
        private NativeQueue<Time2WorkStorageTransferSystem.StorageTransferEvent> m_TransferQueue;
        private Time2WorkStorageTransferSystem.TypeHandle __TypeHandle;

        public override int GetUpdateInterval(SystemUpdatePhase phase) => 16;

        [UnityEngine.Scripting.Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();
            this.m_PathfindSetupSystem = this.World.GetOrCreateSystemManaged<PathfindSetupSystem>();
            this.m_EndFrameBarrier = this.World.GetOrCreateSystemManaged<EndFrameBarrier>();
            this.m_ResourceSystem = this.World.GetOrCreateSystemManaged<ResourceSystem>();
            this.m_VehicleCapacitySystem = this.World.GetOrCreateSystemManaged<VehicleCapacitySystem>();
            this.m_TimeSystem = this.World.GetOrCreateSystemManaged<Time2WorkTimeSystem>();
            this.m_CitySystem = this.World.GetOrCreateSystemManaged<CitySystem>();
            this.m_TransferQueue = new NativeQueue<Time2WorkStorageTransferSystem.StorageTransferEvent>((AllocatorManager.AllocatorHandle)Allocator.Persistent);
            this.m_TransferGroup = this.GetEntityQuery(ComponentType.ReadOnly<StorageTransfer>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<Game.Economy.Resources>(), ComponentType.ReadWrite<TripNeeded>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
            this.RequireForUpdate(this.m_TransferGroup);
        }

        [UnityEngine.Scripting.Preserve]
        protected override void OnDestroy()
        {
            // ISSUE: reference to a compiler-generated field
            this.m_TransferQueue.Dispose();
            base.OnDestroy();
        }

        public static int CalculateTransferableAmount(
          int original,
          int sourceAmount,
          int sourceCapacity,
          int targetAmount,
          int targetCapacity)
        {
            if (targetCapacity == 0 && sourceCapacity == 0)
                return 0;
            return original > 0 ? math.min(targetCapacity - targetAmount, original) : -math.min(sourceCapacity - sourceAmount, -original);
        }

        [UnityEngine.Scripting.Preserve]
        protected override void OnUpdate()
        {

            JobHandle jobHandle = new Time2WorkStorageTransferSystem.TransferJob()
            {
                m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref this.__TypeHandle.__Unity_Entities_Entity_TypeHandle, ref this.CheckedStateRef),
                m_TransferType = InternalCompilerInterface.GetComponentTypeHandle<StorageTransfer>(ref this.__TypeHandle.__Game_Companies_StorageTransfer_RO_ComponentTypeHandle, ref this.CheckedStateRef),
                m_PrefabType = InternalCompilerInterface.GetComponentTypeHandle<PrefabRef>(ref this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref this.CheckedStateRef),
                m_ResourceType = InternalCompilerInterface.GetBufferTypeHandle<Game.Economy.Resources>(ref this.__TypeHandle.__Game_Economy_Resources_RO_BufferTypeHandle, ref this.CheckedStateRef),
                m_PathInformation = InternalCompilerInterface.GetComponentLookup<PathInformation>(ref this.__TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentLookup, ref this.CheckedStateRef),
                m_Properties = InternalCompilerInterface.GetComponentLookup<PropertyRenter>(ref this.__TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup, ref this.CheckedStateRef),
                m_OutsideConnections = InternalCompilerInterface.GetComponentLookup<Game.Objects.OutsideConnection>(ref this.__TypeHandle.__Game_Objects_OutsideConnection_RO_ComponentLookup, ref this.CheckedStateRef),
                m_Limits = InternalCompilerInterface.GetComponentLookup<StorageLimitData>(ref this.__TypeHandle.__Game_Companies_StorageLimitData_RO_ComponentLookup, ref this.CheckedStateRef),
                m_StorageCompanyDatas = InternalCompilerInterface.GetComponentLookup<StorageCompanyData>(ref this.__TypeHandle.__Game_Prefabs_StorageCompanyData_RO_ComponentLookup, ref this.CheckedStateRef),
                m_Prefabs = InternalCompilerInterface.GetComponentLookup<PrefabRef>(ref this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref this.CheckedStateRef),
                m_Resources = InternalCompilerInterface.GetBufferLookup<Game.Economy.Resources>(ref this.__TypeHandle.__Game_Economy_Resources_RO_BufferLookup, ref this.CheckedStateRef),
                m_InstalledUpgrades = InternalCompilerInterface.GetBufferLookup<InstalledUpgrade>(ref this.__TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferLookup, ref this.CheckedStateRef),
                m_StorageTransferRequests = InternalCompilerInterface.GetBufferLookup<StorageTransferRequest>(ref this.__TypeHandle.__Game_Companies_StorageTransferRequest_RO_BufferLookup, ref this.CheckedStateRef),
                m_ResourceDatas = InternalCompilerInterface.GetComponentLookup<ResourceData>(ref this.__TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup, ref this.CheckedStateRef),
                m_BuildingDatas = InternalCompilerInterface.GetComponentLookup<Game.Prefabs.BuildingData>(ref this.__TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref this.CheckedStateRef),
                m_SpawnableDatas = InternalCompilerInterface.GetComponentLookup<SpawnableBuildingData>(ref this.__TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup, ref this.CheckedStateRef),
                m_CargoTransportStations = InternalCompilerInterface.GetComponentLookup<Game.Buildings.CargoTransportStation>(ref this.__TypeHandle.__Game_Buildings_CargoTransportStation_RO_ComponentLookup, ref this.CheckedStateRef),
                m_GuestVehicles = InternalCompilerInterface.GetBufferLookup<GuestVehicle>(ref this.__TypeHandle.__Game_Vehicles_GuestVehicle_RO_BufferLookup, ref this.CheckedStateRef),
                m_LayoutElements = InternalCompilerInterface.GetBufferLookup<LayoutElement>(ref this.__TypeHandle.__Game_Vehicles_LayoutElement_RO_BufferLookup, ref this.CheckedStateRef),
                m_DeliveryTrucks = InternalCompilerInterface.GetComponentLookup<Game.Vehicles.DeliveryTruck>(ref this.__TypeHandle.__Game_Vehicles_DeliveryTruck_RO_ComponentLookup, ref this.CheckedStateRef),
                m_ResourcePrefabs = this.m_ResourceSystem.GetPrefabs(),
                m_PathfindQueue = this.m_PathfindSetupSystem.GetQueue((object)this, 64).AsParallelWriter(),
                m_TransferQueue = this.m_TransferQueue.AsParallelWriter(),
                m_DeliveryTruckSelectData = this.m_VehicleCapacitySystem.GetDeliveryTruckSelectData(),
                m_CommandBuffer = this.m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
                m_RandomSeed = RandomSeed.Next(),
                m_NormalizedTime = this.m_TimeSystem.normalizedTime,
                night_trucks = Mod.m_Setting.night_trucks
            }.ScheduleParallel<Time2WorkStorageTransferSystem.TransferJob>(this.m_TransferGroup, this.Dependency);
            this.m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
            this.m_PathfindSetupSystem.AddQueueWriter(jobHandle);

            JobHandle handle = new Time2WorkStorageTransferSystem.HandleTransfersJob()
            {
                m_TradeCosts = InternalCompilerInterface.GetBufferLookup<TradeCost>(ref this.__TypeHandle.__Game_Companies_TradeCost_RW_BufferLookup, ref this.CheckedStateRef),
                m_StorageCompanies = InternalCompilerInterface.GetComponentLookup<Game.Companies.StorageCompany>(ref this.__TypeHandle.__Game_Companies_StorageCompany_RW_ComponentLookup, ref this.CheckedStateRef),
                m_OwnerData = InternalCompilerInterface.GetComponentLookup<Owner>(ref this.__TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref this.CheckedStateRef),
                m_SegmentData = InternalCompilerInterface.GetComponentLookup<Game.Routes.Segment>(ref this.__TypeHandle.__Game_Routes_Segment_RO_ComponentLookup, ref this.CheckedStateRef),
                m_ConnectedData = InternalCompilerInterface.GetComponentLookup<Connected>(ref this.__TypeHandle.__Game_Routes_Connected_RO_ComponentLookup, ref this.CheckedStateRef),
                m_CarLaneData = InternalCompilerInterface.GetComponentLookup<Game.Net.CarLane>(ref this.__TypeHandle.__Game_Net_CarLane_RO_ComponentLookup, ref this.CheckedStateRef),
                m_TrackLaneData = InternalCompilerInterface.GetComponentLookup<Game.Net.TrackLane>(ref this.__TypeHandle.__Game_Net_TrackLane_RO_ComponentLookup, ref this.CheckedStateRef),
                m_PedestrianLaneData = InternalCompilerInterface.GetComponentLookup<Game.Net.PedestrianLane>(ref this.__TypeHandle.__Game_Net_PedestrianLane_RO_ComponentLookup, ref this.CheckedStateRef),
                m_ConnectionLaneData = InternalCompilerInterface.GetComponentLookup<Game.Net.ConnectionLane>(ref this.__TypeHandle.__Game_Net_ConnectionLane_RO_ComponentLookup, ref this.CheckedStateRef),
                m_Buildings = InternalCompilerInterface.GetComponentLookup<Building>(ref this.__TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref this.CheckedStateRef),
                m_Renters = InternalCompilerInterface.GetBufferLookup<Renter>(ref this.__TypeHandle.__Game_Buildings_Renter_RO_BufferLookup, ref this.CheckedStateRef),
                m_RouteWaypoints = InternalCompilerInterface.GetBufferLookup<RouteWaypoint>(ref this.__TypeHandle.__Game_Routes_RouteWaypoint_RO_BufferLookup, ref this.CheckedStateRef),
                m_PathInfos = InternalCompilerInterface.GetComponentLookup<PathInformation>(ref this.__TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentLookup, ref this.CheckedStateRef),
                m_Paths = InternalCompilerInterface.GetBufferLookup<PathElement>(ref this.__TypeHandle.__Game_Pathfind_PathElement_RO_BufferLookup, ref this.CheckedStateRef),
                m_Requests = InternalCompilerInterface.GetBufferLookup<StorageTransferRequest>(ref this.__TypeHandle.__Game_Companies_StorageTransferRequest_RW_BufferLookup, ref this.CheckedStateRef),
                m_Curves = InternalCompilerInterface.GetComponentLookup<Curve>(ref this.__TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref this.CheckedStateRef),
                m_OutsideConnections = InternalCompilerInterface.GetComponentLookup<Game.Objects.OutsideConnection>(ref this.__TypeHandle.__Game_Objects_OutsideConnection_RO_ComponentLookup, ref this.CheckedStateRef),
                m_CityServiceUpkeeps = InternalCompilerInterface.GetComponentLookup<CityServiceUpkeep>(ref this.__TypeHandle.__Game_City_CityServiceUpkeep_RO_ComponentLookup, ref this.CheckedStateRef),
                m_Resources = InternalCompilerInterface.GetBufferLookup<Game.Economy.Resources>(ref this.__TypeHandle.__Game_Economy_Resources_RW_BufferLookup, ref this.CheckedStateRef),
                m_ResourcePrefabs = this.m_ResourceSystem.GetPrefabs(),
                m_ResourceDatas = InternalCompilerInterface.GetComponentLookup<ResourceData>(ref this.__TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup, ref this.CheckedStateRef),
                m_TransferQueue = this.m_TransferQueue,
                m_City = this.m_CitySystem.City
            }.Schedule<Time2WorkStorageTransferSystem.HandleTransfersJob>(jobHandle);
            this.m_ResourceSystem.AddPrefabsReader(handle);
            this.Dependency = handle;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void __AssignQueries(ref SystemState state)
        {
            new EntityQueryBuilder((AllocatorManager.AllocatorHandle)Allocator.Temp).Dispose();
        }

        protected override void OnCreateForCompiler()
        {
            base.OnCreateForCompiler();
            this.__AssignQueries(ref this.CheckedStateRef);
            this.__TypeHandle.__AssignHandles(ref this.CheckedStateRef);
        }

        [UnityEngine.Scripting.Preserve]
        public Time2WorkStorageTransferSystem()
        {
        }

        private struct StorageTransferEvent
        {
            public Entity m_Source;
            public Entity m_Destination;
            public float m_Distance;
            public Resource m_Resource;
            public int m_Amount;
        }

        [BurstCompile]
        private struct TransferJob : IJobChunk
        {
            [ReadOnly]
            public EntityTypeHandle m_EntityType;
            [ReadOnly]
            public ComponentTypeHandle<StorageTransfer> m_TransferType;
            [ReadOnly]
            public ComponentTypeHandle<PrefabRef> m_PrefabType;
            [ReadOnly]
            public BufferTypeHandle<Game.Economy.Resources> m_ResourceType;
            [ReadOnly]
            public ComponentLookup<StorageLimitData> m_Limits;
            [ReadOnly]
            public ComponentLookup<PathInformation> m_PathInformation;
            [ReadOnly]
            public ComponentLookup<PropertyRenter> m_Properties;
            [ReadOnly]
            public ComponentLookup<Game.Objects.OutsideConnection> m_OutsideConnections;
            [ReadOnly]
            public ComponentLookup<StorageCompanyData> m_StorageCompanyDatas;
            [ReadOnly]
            public ComponentLookup<PrefabRef> m_Prefabs;
            [ReadOnly]
            public BufferLookup<Game.Economy.Resources> m_Resources;
            [ReadOnly]
            public BufferLookup<InstalledUpgrade> m_InstalledUpgrades;
            [ReadOnly]
            public BufferLookup<StorageTransferRequest> m_StorageTransferRequests;
            [ReadOnly]
            public BufferLookup<GuestVehicle> m_GuestVehicles;
            [ReadOnly]
            public BufferLookup<LayoutElement> m_LayoutElements;
            [ReadOnly]
            public ComponentLookup<Game.Vehicles.DeliveryTruck> m_DeliveryTrucks;
            [ReadOnly]
            public ComponentLookup<ResourceData> m_ResourceDatas;
            [ReadOnly]
            public ComponentLookup<SpawnableBuildingData> m_SpawnableDatas;
            [ReadOnly]
            public ComponentLookup<Game.Prefabs.BuildingData> m_BuildingDatas;
            [ReadOnly]
            public ComponentLookup<Game.Buildings.CargoTransportStation> m_CargoTransportStations;
            [ReadOnly]
            public ResourcePrefabs m_ResourcePrefabs;
            public NativeQueue<SetupQueueItem>.ParallelWriter m_PathfindQueue;
            public NativeQueue<Time2WorkStorageTransferSystem.StorageTransferEvent>.ParallelWriter m_TransferQueue;
            [ReadOnly]
            public DeliveryTruckSelectData m_DeliveryTruckSelectData;
            public EntityCommandBuffer.ParallelWriter m_CommandBuffer;
            public RandomSeed m_RandomSeed;
            public float m_NormalizedTime;
            public bool night_trucks;

            public void Execute(
        in ArchetypeChunk chunk,
        int unfilteredChunkIndex,
        bool useEnabledMask,
        in v128 chunkEnabledMask)
            {
                NativeArray<StorageTransfer> nativeArray1 = chunk.GetNativeArray<StorageTransfer>(ref this.m_TransferType);
                NativeArray<Entity> nativeArray2 = chunk.GetNativeArray(this.m_EntityType);
                NativeArray<PrefabRef> nativeArray3 = chunk.GetNativeArray<PrefabRef>(ref this.m_PrefabType);
                BufferAccessor<Game.Economy.Resources> bufferAccessor = chunk.GetBufferAccessor<Game.Economy.Resources>(ref this.m_ResourceType);
                Unity.Mathematics.Random random = this.m_RandomSeed.GetRandom(unfilteredChunkIndex);
                for (int index1 = 0; index1 < chunk.Count; ++index1)
                {
                    float windowStart = 1f - Math.Abs((float)(GaussianRandom.NextGaussianDouble(random))) * (0.375f);
                    float windowEnd = Math.Abs((float)(GaussianRandom.NextGaussianDouble(random))) * (0.25f);

                    if ((this.m_NormalizedTime > 0.25f && this.m_NormalizedTime < 0.625f) && night_trucks)
                    {
                        continue;
                    }
                    Entity entity1 = nativeArray2[index1];
                    StorageTransfer storageTransfer = nativeArray1[index1];
                    Entity prefab1 = nativeArray3[index1].m_Prefab;

                    if (this.m_Limits.HasComponent(prefab1) && this.m_StorageCompanyDatas.HasComponent(prefab1))
                    {
                        StorageCompanyData storageCompanyData1 = this.m_StorageCompanyDatas[prefab1];
                        if (this.m_InstalledUpgrades.HasBuffer(entity1))
                        {
                            UpgradeUtils.CombineStats<StorageCompanyData>(ref storageCompanyData1, this.m_InstalledUpgrades[entity1], ref this.m_Prefabs, ref this.m_StorageCompanyDatas);
                        }
                        int num1 = EconomyUtils.CountResources(storageCompanyData1.m_StoredResources);
                        if (num1 != 0)
                        {
                            int num2 = this.GetStorageLimit(entity1, prefab1) / num1;
                            DynamicBuffer<Game.Economy.Resources> resources1 = bufferAccessor[index1];
                            int resources2 = EconomyUtils.GetResources(storageTransfer.m_Resource, resources1);
                            if (this.m_PathInformation.HasComponent(entity1))
                            {
                                PathInformation pathInformation = this.m_PathInformation[entity1];
                                if ((pathInformation.m_State & PathFlags.Pending) == (PathFlags)0)
                                {
                                    Entity entity2 = storageTransfer.m_Amount < 0 ? pathInformation.m_Origin : pathInformation.m_Destination;
                                    bool flag1 = this.m_OutsideConnections.HasComponent(entity2);
                                    bool flag2 = this.m_CargoTransportStations.HasComponent(entity2);
                                    if (this.m_Properties.HasComponent(entity2) | flag1 && entity1 != entity2)
                                    {
                                        Entity prefab2 = this.m_Prefabs[entity2].m_Prefab;
                                        StorageCompanyData storageCompanyData2 = this.m_StorageCompanyDatas[prefab2];
                                        if (this.m_InstalledUpgrades.HasBuffer(entity1) && this.m_InstalledUpgrades[entity1].Length != 0)
                                        {
                                            UpgradeUtils.CombineStats<StorageCompanyData>(ref storageCompanyData2, this.m_InstalledUpgrades[entity1], ref this.m_Prefabs, ref this.m_StorageCompanyDatas);
                                        }
                                        int num3 = EconomyUtils.CountResources(storageCompanyData2.m_StoredResources);
                                        if (num3 == 0)
                                        {
                                            // ISSUE: reference to a compiler-generated field
                                            this.m_CommandBuffer.RemoveComponent<StorageTransfer>(unfilteredChunkIndex, entity1);
                                            // ISSUE: reference to a compiler-generated field
                                            this.m_CommandBuffer.RemoveComponent<PathInformation>(unfilteredChunkIndex, entity1);
                                            // ISSUE: reference to a compiler-generated field
                                            this.m_CommandBuffer.RemoveComponent<PathElement>(unfilteredChunkIndex, entity1);
                                        }
                                        else
                                        {
                                            int targetCapacity = this.GetStorageLimit(entity2, prefab2) / num3;
                                            DynamicBuffer<Game.Economy.Resources> resource = this.m_Resources[entity2];
                                            long num4 = (long)EconomyUtils.GetResources(storageTransfer.m_Resource, resource) - (long)VehicleUtils.GetAllBuyingResourcesTrucks(entity2, storageTransfer.m_Resource, ref this.m_DeliveryTrucks, ref this.m_GuestVehicles, ref this.m_LayoutElements);
                                            if (this.m_StorageTransferRequests.HasBuffer(entity2))
                                            {
                                                long num5 = 0;
                                                DynamicBuffer<StorageTransferRequest> storageTransferRequest1 = this.m_StorageTransferRequests[entity2];
                                                for (int index2 = 0; index2 < storageTransferRequest1.Length; ++index2)
                                                {
                                                    StorageTransferRequest storageTransferRequest2 = storageTransferRequest1[index2];
                                                    if (storageTransferRequest2.m_Resource == storageTransfer.m_Resource)
                                                        num5 += (storageTransferRequest2.m_Flags & StorageTransferFlags.Incoming) != (StorageTransferFlags)0 ? (long)storageTransferRequest2.m_Amount : (long)-storageTransferRequest2.m_Amount;
                                                }
                                                num4 += num5;
                                            }
                                            if (flag2 | flag1)
                                            {
                                                if (storageTransfer.m_Amount < 0)
                                                    storageTransfer.m_Amount = num4 <= 0L ? 0 : -math.min((int)num4, math.abs(storageTransfer.m_Amount));
                                            }
                                            else
                                            {
                                                storageTransfer.m_Amount = StorageTransferSystem.CalculateTransferableAmount(storageTransfer.m_Amount, resources2, num2, (int)math.max(0L, num4), targetCapacity);
                                            }
                                            DeliveryTruckSelectItem deliveryTruckSelectItem;
                                            this.m_DeliveryTruckSelectData.TrySelectItem(ref random, storageTransfer.m_Resource, math.abs(storageTransfer.m_Amount), out deliveryTruckSelectItem);
                                            if (storageTransfer.m_Amount != 0 && (double)deliveryTruckSelectItem.m_Cost / (double)math.min(math.abs(storageTransfer.m_Amount), deliveryTruckSelectItem.m_Capacity) <= (double)StorageTransferSystem.kMaxTransportUnitCost)
                                            {
                                                int num6 = math.abs(storageTransfer.m_Amount) / deliveryTruckSelectItem.m_Capacity * deliveryTruckSelectItem.m_Capacity;
                                                if (num6 != 0)
                                                {
                                                    this.m_DeliveryTruckSelectData.TrySelectItem(ref random, storageTransfer.m_Resource, math.abs(storageTransfer.m_Amount) - num6, out deliveryTruckSelectItem);
                                                    if (math.abs(storageTransfer.m_Amount) - num6 > 0 && (double)(deliveryTruckSelectItem.m_Cost / (math.abs(storageTransfer.m_Amount) - num6)) > (double)StorageTransferSystem.kMaxTransportUnitCost)
                                                        storageTransfer.m_Amount = storageTransfer.m_Amount > 0 ? num6 : -num6;
                                                }
                                                if (storageTransfer.m_Amount != 0)
                                                {
                                                    this.m_TransferQueue.Enqueue(new Time2WorkStorageTransferSystem.StorageTransferEvent()
                                                    {
                                                        m_Amount = storageTransfer.m_Amount,
                                                        m_Destination = entity2,
                                                        m_Source = entity1,
                                                        m_Distance = pathInformation.m_Distance,
                                                        m_Resource = storageTransfer.m_Resource
                                                    });
                                                }
                                            }
                                            this.m_CommandBuffer.RemoveComponent<StorageTransfer>(unfilteredChunkIndex, entity1);
                                            this.m_CommandBuffer.RemoveComponent<PathInformation>(unfilteredChunkIndex, entity1);
                                            this.m_CommandBuffer.RemoveComponent<PathElement>(unfilteredChunkIndex, entity1);
                                        }
                                    }
                                    else
                                    {
                                        this.m_CommandBuffer.RemoveComponent<StorageTransfer>(unfilteredChunkIndex, entity1);
                                        this.m_CommandBuffer.RemoveComponent<PathInformation>(unfilteredChunkIndex, entity1);
                                        this.m_CommandBuffer.RemoveComponent<PathElement>(unfilteredChunkIndex, entity1);
                                    }
                                }
                            }
                            else
                            {
                                float fillProportion = (float)resources2 / (float)num2;
                                this.FindTarget(unfilteredChunkIndex, entity1, storageTransfer.m_Resource, storageTransfer.m_Amount, fillProportion, num2);
                            }
                        }
                    }
                }
            }

            private int GetStorageLimit(Entity entity, Entity prefab)
            {
                StorageLimitData limit = this.m_Limits[prefab];
                if (this.m_InstalledUpgrades.HasBuffer(entity))
                {
                    UpgradeUtils.CombineStats<StorageLimitData>(ref limit, this.m_InstalledUpgrades[entity], ref this.m_Prefabs, ref this.m_Limits);
                }
                if (this.m_Properties.HasComponent(entity) && this.m_Prefabs.HasComponent(this.m_Properties[entity].m_Property))
                {
                    Entity prefab1 = this.m_Prefabs[this.m_Properties[entity].m_Property].m_Prefab;
                    ref StorageLimitData local = ref limit;
                    SpawnableBuildingData spawnable;
                    if (!this.m_SpawnableDatas.HasComponent(prefab1))
                    {
                        spawnable = new SpawnableBuildingData()
                        {
                            m_Level = (byte)1
                        };
                    }
                    else
                    {
                        spawnable = this.m_SpawnableDatas[prefab1];
                    }
                    Game.Prefabs.BuildingData building;
                    if (!this.m_SpawnableDatas.HasComponent(prefab1))
                    {
                        building = new Game.Prefabs.BuildingData()
                        {
                            m_LotSize = new int2(1, 1)
                        };
                    }
                    else
                    {
                        building = this.m_BuildingDatas[prefab1];
                    }
                    return local.GetAdjustedLimitForWarehouse(spawnable, building);
                }
                return this.m_OutsideConnections.HasComponent(entity) ? limit.m_Limit : 0;
            }

            private void FindTarget(
              int chunkIndex,
              Entity storage,
              Resource resource,
              int amount,
              float fillProportion,
              int capacity)
            {
                this.m_CommandBuffer.AddComponent<PathInformation>(chunkIndex, storage, new PathInformation()
                {
                    m_State = PathFlags.Pending
                });
                this.m_CommandBuffer.AddBuffer<PathElement>(chunkIndex, storage);
                float transportCost = EconomyUtils.GetTransportCost(1f, math.abs(amount), this.m_ResourceDatas[this.m_ResourcePrefabs[resource]].m_Weight, StorageTransferFlags.Car);
                PathfindParameters parameters = new PathfindParameters()
                {
                    m_MaxSpeed = (float2)111.111115f,
                    m_WalkSpeed = (float2)5.555556f,
                    m_Weights = new PathfindWeights(0.01f, 0.01f, transportCost, 0.01f),
                    m_Methods = PathMethod.Road | PathMethod.CargoTransport | PathMethod.CargoLoading,
                    m_IgnoredRules = RuleFlags.ForbidSlowTraffic
                };
                SetupQueueTarget a = new SetupQueueTarget()
                {
                    m_Type = SetupTargetType.CurrentLocation,
                    m_Methods = PathMethod.Road | PathMethod.CargoLoading,
                    m_RoadTypes = RoadTypes.Car
                };
                SetupQueueTarget b = new SetupQueueTarget()
                {
                    m_Type = SetupTargetType.StorageTransfer,
                    m_Methods = PathMethod.Road | PathMethod.CargoLoading,
                    m_RoadTypes = RoadTypes.Car,
                    m_Entity = storage,
                    m_Resource = resource,
                    m_Value = amount,
                    m_Value2 = fillProportion,
                    m_Value3 = capacity
                };
                if (amount < 0)
                    CommonUtils.Swap<SetupQueueTarget>(ref a, ref b);
                this.m_PathfindQueue.Enqueue(new SetupQueueItem(storage, parameters, a, b));
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

        [BurstCompile]
        private struct HandleTransfersJob : IJob
        {
            public NativeQueue<Time2WorkStorageTransferSystem.StorageTransferEvent> m_TransferQueue;
            public BufferLookup<TradeCost> m_TradeCosts;
            public ComponentLookup<Game.Companies.StorageCompany> m_StorageCompanies;
            [ReadOnly]
            public ComponentLookup<Game.Net.CarLane> m_CarLaneData;
            [ReadOnly]
            public ComponentLookup<Game.Net.TrackLane> m_TrackLaneData;
            [ReadOnly]
            public ComponentLookup<Game.Net.PedestrianLane> m_PedestrianLaneData;
            [ReadOnly]
            public ComponentLookup<Game.Net.ConnectionLane> m_ConnectionLaneData;
            [ReadOnly]
            public ComponentLookup<Owner> m_OwnerData;
            [ReadOnly]
            public BufferLookup<Renter> m_Renters;
            [ReadOnly]
            public ComponentLookup<Connected> m_ConnectedData;
            [ReadOnly]
            public ComponentLookup<Game.Routes.Segment> m_SegmentData;
            [ReadOnly]
            public BufferLookup<RouteWaypoint> m_RouteWaypoints;
            [ReadOnly]
            public ComponentLookup<PathInformation> m_PathInfos;
            [ReadOnly]
            public BufferLookup<PathElement> m_Paths;
            [ReadOnly]
            public ComponentLookup<Building> m_Buildings;
            [ReadOnly]
            public ComponentLookup<Curve> m_Curves;
            [ReadOnly]
            public ComponentLookup<Game.Objects.OutsideConnection> m_OutsideConnections;
            [ReadOnly]
            public ComponentLookup<CityServiceUpkeep> m_CityServiceUpkeeps;
            [ReadOnly]
            public ResourcePrefabs m_ResourcePrefabs;
            [ReadOnly]
            public ComponentLookup<ResourceData> m_ResourceDatas;
            public BufferLookup<StorageTransferRequest> m_Requests;
            public BufferLookup<Game.Economy.Resources> m_Resources;
            public Entity m_City;

            private Entity GetStorageCompanyFromLane(Entity entity)
            {
                while (this.m_OwnerData.HasComponent(entity))
                {
                    entity = this.m_OwnerData[entity].m_Owner;
                    if (this.m_StorageCompanies.HasComponent(entity))
                        return entity;
                    if (this.m_Renters.HasBuffer(entity))
                    {
                        DynamicBuffer<Renter> renter1 = this.m_Renters[entity];
                        for (int index = 0; index < renter1.Length; ++index)
                        {
                            Entity renter2 = renter1[index].m_Renter;
                            if (this.m_StorageCompanies.HasComponent(renter2))
                                return renter2;
                        }
                    }
                }
                return Entity.Null;
            }

            private Entity GetStorageCompanyFromWaypoint(Entity entity)
            {
                if (this.m_ConnectedData.HasComponent(entity))
                {
                    for (entity = this.m_ConnectedData[entity].m_Connected; !this.m_StorageCompanies.HasComponent(entity); entity = this.m_OwnerData[entity].m_Owner)
                    {
                        if (this.m_Renters.HasBuffer(entity))
                        {
                            DynamicBuffer<Renter> renter1 = this.m_Renters[entity];
                            for (int index = 0; index < renter1.Length; ++index)
                            {
                                Entity renter2 = renter1[index].m_Renter;
                                if (this.m_StorageCompanies.HasComponent(renter2))
                                    return renter2;
                            }
                        }
                        if (!this.m_OwnerData.HasComponent(entity))
                            goto label_12;
                    }
                    return entity;
                }
            label_12:
                return Entity.Null;
            }

            private void GetStorageCompaniesFromSegment(
              Entity entity,
              out Entity startCompany,
              out Entity endCompany)
            {
                Entity owner = this.m_OwnerData[entity].m_Owner;
                Game.Routes.Segment segment = this.m_SegmentData[entity];
                DynamicBuffer<RouteWaypoint> routeWaypoint = this.m_RouteWaypoints[owner];
                int index = segment.m_Index + 1;
                if (index == routeWaypoint.Length)
                    index = 0;
                startCompany = this.GetStorageCompanyFromWaypoint(routeWaypoint[segment.m_Index].m_Waypoint);
                endCompany = this.GetStorageCompanyFromWaypoint(routeWaypoint[index].m_Waypoint);
            }

            private float HandleCargoPath(
              PathInformation pathInformation,
              DynamicBuffer<PathElement> path,
              Resource resource,
              int amount,
              float weight)
            {
                float num1 = 0.0f;
                float distance = 0.0f;
                Entity origin = pathInformation.m_Origin;
                StorageTransferFlags flags = (StorageTransferFlags)0;
                int num2 = path.Length;
                int x = 0;
                for (int index = 0; index < path.Length; ++index)
                {
                    Entity target = path[index].m_Target;
                    if (this.m_Curves.HasComponent(target))
                    {
                        Curve curve = this.m_Curves[target];
                        distance += curve.m_Length * math.abs(path[index].m_TargetDelta.y - path[index].m_TargetDelta.x);
                    }
                    if (this.m_CarLaneData.HasComponent(target))
                    {
                        flags |= StorageTransferFlags.Car;
                        num2 = math.min(num2, index);
                        x = math.max(x, index + 1);
                    }
                    else
                    {
                        if (this.m_TrackLaneData.HasComponent(target))
                        {
                            flags |= StorageTransferFlags.Track;
                            num2 = math.min(num2, index);
                            x = math.max(x, index + 1);
                        }
                        else
                        {
                            if (this.m_PedestrianLaneData.HasComponent(target))
                            {
                                Entity storageCompanyFromLane = this.GetStorageCompanyFromLane(target);
                                if (storageCompanyFromLane != Entity.Null && storageCompanyFromLane != origin)
                                {
                                    num1 += this.AddCargoPathSection(origin, storageCompanyFromLane, path, num2, x - num2, flags, resource, amount, weight, distance);
                                    origin = storageCompanyFromLane;
                                    flags = (StorageTransferFlags)0;
                                    num2 = path.Length;
                                    x = 0;
                                    distance = 0.0f;
                                }
                            }
                            else
                            {
                                if (this.m_ConnectionLaneData.HasComponent(target))
                                {
                                    Game.Net.ConnectionLane connectionLane = this.m_ConnectionLaneData[target];
                                    if ((connectionLane.m_Flags & ConnectionLaneFlags.Road) != (ConnectionLaneFlags)0)
                                    {
                                        flags |= StorageTransferFlags.Car;
                                        num2 = math.min(num2, index);
                                        x = math.max(x, index + 1);
                                    }
                                    else if ((connectionLane.m_Flags & ConnectionLaneFlags.Track) != (ConnectionLaneFlags)0)
                                    {
                                        flags |= StorageTransferFlags.Track;
                                        num2 = math.min(num2, index);
                                        x = math.max(x, index + 1);
                                    }
                                }
                                else
                                {
                                    if (this.m_SegmentData.HasComponent(target))
                                    {
                                        Entity startCompany;
                                        Entity endCompany;
                                        this.GetStorageCompaniesFromSegment(target, out startCompany, out endCompany);
                                        if (startCompany != Entity.Null && startCompany != origin)
                                        {
                                            num1 += this.AddCargoPathSection(origin, startCompany, path, num2, x - num2, flags, resource, amount, weight, distance);
                                            origin = startCompany;
                                            flags = (StorageTransferFlags)0;
                                            num2 = path.Length;
                                            x = 0;
                                            distance = 0.0f;
                                        }
                                        flags |= StorageTransferFlags.Transport;
                                        if (endCompany != Entity.Null && endCompany != origin)
                                        {
                                            num1 += this.AddCargoPathSection(origin, endCompany, path, num2, x - num2, flags, resource, amount, weight, distance);
                                            origin = endCompany;
                                            flags = (StorageTransferFlags)0;
                                            num2 = path.Length;
                                            x = 0;
                                            distance = 0.0f;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                if (pathInformation.m_Destination != origin)
                {
                    num1 += this.AddCargoPathSection(origin, pathInformation.m_Destination, path, num2, x - num2, flags, resource, amount, weight, distance);
                }
                return num1;
            }

            private void AddRequest(
              DynamicBuffer<StorageTransferRequest> requests,
              Entity destination,
              StorageTransferFlags flags,
              Resource resource,
              int amount)
            {
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                if (this.m_Buildings.HasComponent(destination) && BuildingUtils.CheckOption(this.m_Buildings[destination], BuildingOption.Inactive))
                    return;
                bool flag = false;
                for (int index = 0; index < requests.Length; ++index)
                {
                    StorageTransferRequest request = requests[index];
                    if (request.m_Target == destination && request.m_Resource == resource && request.m_Flags == flags)
                    {
                        request.m_Amount += amount;
                        flag = true;
                        break;
                    }
                }
                if (flag)
                    return;
                requests.Add(new StorageTransferRequest()
                {
                    m_Amount = math.abs(amount),
                    m_Resource = resource,
                    m_Target = destination,
                    m_Flags = flags
                });
            }

            private float AddCargoPathSection(
              Entity origin,
              Entity destination,
              DynamicBuffer<PathElement> path,
              int startIndex,
              int length,
              StorageTransferFlags flags,
              Resource resource,
              int amount,
              float weight,
              float distance)
            {
                if (!this.m_Requests.HasBuffer(origin) || !this.m_Requests.HasBuffer(destination))
                    return 0.0f;
                this.AddRequest(this.m_Requests[origin], destination, flags, resource, amount);
                this.AddRequest(this.m_Requests[destination], origin, flags | StorageTransferFlags.Incoming, resource, math.abs(amount));
                double transportCost = (double)EconomyUtils.GetTransportCost(distance, math.abs(amount), weight, flags);
                return EconomyUtils.GetTransportCost(distance, math.abs(amount), weight, flags);
            }

            public void Execute()
            {
                DynamicBuffer<TradeCost> tradeCost1 = this.m_TradeCosts[this.m_City];

                Time2WorkStorageTransferSystem.StorageTransferEvent storageTransferEvent;

                while (this.m_TransferQueue.TryDequeue(out storageTransferEvent))
                {
                    if (this.m_PathInfos.HasComponent(storageTransferEvent.m_Source) && this.m_Paths.HasBuffer(storageTransferEvent.m_Source))
                    {
                        float weight = EconomyUtils.GetWeight(storageTransferEvent.m_Resource, this.m_ResourcePrefabs, ref this.m_ResourceDatas);
                        float num1 = this.HandleCargoPath(this.m_PathInfos[storageTransferEvent.m_Source], this.m_Paths[storageTransferEvent.m_Source], storageTransferEvent.m_Resource, storageTransferEvent.m_Amount, weight);
                        DynamicBuffer<TradeCost> tradeCost2 = this.m_TradeCosts[storageTransferEvent.m_Source];
                        DynamicBuffer<TradeCost> tradeCost3 = this.m_TradeCosts[storageTransferEvent.m_Destination];
                        TradeCost tradeCost4 = EconomyUtils.GetTradeCost(storageTransferEvent.m_Resource, tradeCost2);
                        TradeCost tradeCost5 = EconomyUtils.GetTradeCost(storageTransferEvent.m_Resource, tradeCost3);
                        float num2 = num1 / (1f + (float)math.abs(storageTransferEvent.m_Amount));
                        if (storageTransferEvent.m_Amount > 0)
                        {
                            tradeCost4.m_SellCost = math.lerp(tradeCost4.m_SellCost, num2 + tradeCost5.m_SellCost, 0.5f);
                            tradeCost5.m_BuyCost = math.lerp(tradeCost5.m_BuyCost, num2 + tradeCost4.m_BuyCost, 0.5f);
                        }
                        else
                        {
                            tradeCost4.m_BuyCost = math.lerp(tradeCost4.m_BuyCost, num2 + tradeCost5.m_BuyCost, 0.5f);
                            tradeCost5.m_SellCost = math.lerp(tradeCost5.m_SellCost, num2 + tradeCost4.m_SellCost, 0.5f);
                        }
                        int amount = Mathf.RoundToInt(Time2WorkStorageTransferSystem.kStorageProfit * num1);

                        EconomyUtils.GetTradeCost(storageTransferEvent.m_Resource, tradeCost1);
                        if (!this.m_OutsideConnections.HasComponent(storageTransferEvent.m_Source))
                        {
                            EconomyUtils.SetTradeCost(storageTransferEvent.m_Resource, tradeCost4, tradeCost2, true);
                            if (this.m_Resources.HasBuffer(storageTransferEvent.m_Source) && !this.m_CityServiceUpkeeps.HasComponent(storageTransferEvent.m_Source))
                            {
                                EconomyUtils.AddResources(Resource.Money, amount, this.m_Resources[storageTransferEvent.m_Source]);
                            }
                        }
                        else
                        {
                            if (storageTransferEvent.m_Amount > 0)
                            {
                                EconomyUtils.SetTradeCost(storageTransferEvent.m_Resource, tradeCost5, tradeCost1, false, 0.1f, 0.0f);
                                EconomyUtils.GetTradeCost(storageTransferEvent.m_Resource, tradeCost1);
                            }
                            else
                            {
                                EconomyUtils.SetTradeCost(storageTransferEvent.m_Resource, tradeCost5, tradeCost1, false, 0.0f, 0.1f);
                                EconomyUtils.GetTradeCost(storageTransferEvent.m_Resource, tradeCost1);
                            }
                        }
                        if (!this.m_OutsideConnections.HasComponent(storageTransferEvent.m_Destination))
                        {
                            EconomyUtils.SetTradeCost(storageTransferEvent.m_Resource, tradeCost5, tradeCost3, true);

                            if (this.m_Resources.HasBuffer(storageTransferEvent.m_Destination) && !this.m_CityServiceUpkeeps.HasComponent(storageTransferEvent.m_Destination))
                            {
                                EconomyUtils.AddResources(Resource.Money, amount, this.m_Resources[storageTransferEvent.m_Destination]);
                            }
                        }
                        else
                        {
                            if (storageTransferEvent.m_Amount > 0)
                            {
                                EconomyUtils.SetTradeCost(storageTransferEvent.m_Resource, tradeCost4, tradeCost1, false, 0.0f, 0.1f);
                                EconomyUtils.GetTradeCost(storageTransferEvent.m_Resource, tradeCost1);
                            }
                            else
                            {
                                EconomyUtils.SetTradeCost(storageTransferEvent.m_Resource, tradeCost4, tradeCost1, false, 0.1f, 0.0f);
                                EconomyUtils.GetTradeCost(storageTransferEvent.m_Resource, tradeCost1);
                            }
                        }
                        // Copy, modify, and reassign source company
                        Game.Companies.StorageCompany storageCompany1 = this.m_StorageCompanies[storageTransferEvent.m_Source];
                        storageCompany1.m_LastTradePartner = storageTransferEvent.m_Destination;
                        this.m_StorageCompanies[storageTransferEvent.m_Source] = storageCompany1;

                        // Copy, modify, and reassign destination company
                        Game.Companies.StorageCompany storageCompany2 = this.m_StorageCompanies[storageTransferEvent.m_Destination];
                        storageCompany2.m_LastTradePartner = storageTransferEvent.m_Source;
                        this.m_StorageCompanies[storageTransferEvent.m_Destination] = storageCompany2;

                        this.m_StorageCompanies[storageTransferEvent.m_Destination] = storageCompany2;
                    }
                }
            }
        }

        private struct TypeHandle
        {
            [ReadOnly]
            public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;
            [ReadOnly]
            public ComponentTypeHandle<StorageTransfer> __Game_Companies_StorageTransfer_RO_ComponentTypeHandle;
            [ReadOnly]
            public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;
            [ReadOnly]
            public BufferTypeHandle<Game.Economy.Resources> __Game_Economy_Resources_RO_BufferTypeHandle;
            [ReadOnly]
            public ComponentLookup<PathInformation> __Game_Pathfind_PathInformation_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Game.Objects.OutsideConnection> __Game_Objects_OutsideConnection_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<StorageLimitData> __Game_Companies_StorageLimitData_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<StorageCompanyData> __Game_Prefabs_StorageCompanyData_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;
            [ReadOnly]
            public BufferLookup<Game.Economy.Resources> __Game_Economy_Resources_RO_BufferLookup;
            [ReadOnly]
            public BufferLookup<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferLookup;
            [ReadOnly]
            public BufferLookup<StorageTransferRequest> __Game_Companies_StorageTransferRequest_RO_BufferLookup;
            [ReadOnly]
            public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Game.Prefabs.BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Game.Buildings.CargoTransportStation> __Game_Buildings_CargoTransportStation_RO_ComponentLookup;
            [ReadOnly]
            public BufferLookup<GuestVehicle> __Game_Vehicles_GuestVehicle_RO_BufferLookup;
            [ReadOnly]
            public BufferLookup<LayoutElement> __Game_Vehicles_LayoutElement_RO_BufferLookup;
            [ReadOnly]
            public ComponentLookup<Game.Vehicles.DeliveryTruck> __Game_Vehicles_DeliveryTruck_RO_ComponentLookup;
            public BufferLookup<TradeCost> __Game_Companies_TradeCost_RW_BufferLookup;
            public ComponentLookup<Game.Companies.StorageCompany> __Game_Companies_StorageCompany_RW_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Game.Routes.Segment> __Game_Routes_Segment_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Connected> __Game_Routes_Connected_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Game.Net.CarLane> __Game_Net_CarLane_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Game.Net.TrackLane> __Game_Net_TrackLane_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Game.Net.PedestrianLane> __Game_Net_PedestrianLane_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Game.Net.ConnectionLane> __Game_Net_ConnectionLane_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;
            [ReadOnly]
            public BufferLookup<Renter> __Game_Buildings_Renter_RO_BufferLookup;
            [ReadOnly]
            public BufferLookup<RouteWaypoint> __Game_Routes_RouteWaypoint_RO_BufferLookup;
            [ReadOnly]
            public BufferLookup<PathElement> __Game_Pathfind_PathElement_RO_BufferLookup;
            public BufferLookup<StorageTransferRequest> __Game_Companies_StorageTransferRequest_RW_BufferLookup;
            [ReadOnly]
            public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<CityServiceUpkeep> __Game_City_CityServiceUpkeep_RO_ComponentLookup;
            public BufferLookup<Game.Economy.Resources> __Game_Economy_Resources_RW_BufferLookup;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void __AssignHandles(ref SystemState state)
            {
                this.__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
                this.__Game_Companies_StorageTransfer_RO_ComponentTypeHandle = state.GetComponentTypeHandle<StorageTransfer>(true);
                this.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(true);
                this.__Game_Economy_Resources_RO_BufferTypeHandle = state.GetBufferTypeHandle<Game.Economy.Resources>(true);
                this.__Game_Pathfind_PathInformation_RO_ComponentLookup = state.GetComponentLookup<PathInformation>(true);
                this.__Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(true);
                this.__Game_Objects_OutsideConnection_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.OutsideConnection>(true);
                this.__Game_Companies_StorageLimitData_RO_ComponentLookup = state.GetComponentLookup<StorageLimitData>(true);
                this.__Game_Prefabs_StorageCompanyData_RO_ComponentLookup = state.GetComponentLookup<StorageCompanyData>(true);
                this.__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(true);
                this.__Game_Economy_Resources_RO_BufferLookup = state.GetBufferLookup<Game.Economy.Resources>(true);
                this.__Game_Buildings_InstalledUpgrade_RO_BufferLookup = state.GetBufferLookup<InstalledUpgrade>(true);
                this.__Game_Companies_StorageTransferRequest_RO_BufferLookup = state.GetBufferLookup<StorageTransferRequest>(true);
                this.__Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(true);
                this.__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<Game.Prefabs.BuildingData>(true);
                this.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup = state.GetComponentLookup<SpawnableBuildingData>(true);
                this.__Game_Buildings_CargoTransportStation_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.CargoTransportStation>(true);
                this.__Game_Vehicles_GuestVehicle_RO_BufferLookup = state.GetBufferLookup<GuestVehicle>(true);
                this.__Game_Vehicles_LayoutElement_RO_BufferLookup = state.GetBufferLookup<LayoutElement>(true);
                this.__Game_Vehicles_DeliveryTruck_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.DeliveryTruck>(true);
                this.__Game_Companies_TradeCost_RW_BufferLookup = state.GetBufferLookup<TradeCost>();
                this.__Game_Companies_StorageCompany_RW_ComponentLookup = state.GetComponentLookup<Game.Companies.StorageCompany>();
                this.__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(true);
                this.__Game_Routes_Segment_RO_ComponentLookup = state.GetComponentLookup<Game.Routes.Segment>(true);
                this.__Game_Routes_Connected_RO_ComponentLookup = state.GetComponentLookup<Connected>(true);
                this.__Game_Net_CarLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.CarLane>(true);
                this.__Game_Net_TrackLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.TrackLane>(true);
                this.__Game_Net_PedestrianLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.PedestrianLane>(true);
                this.__Game_Net_ConnectionLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.ConnectionLane>(true);
                this.__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(true);
                this.__Game_Buildings_Renter_RO_BufferLookup = state.GetBufferLookup<Renter>(true);
                this.__Game_Routes_RouteWaypoint_RO_BufferLookup = state.GetBufferLookup<RouteWaypoint>(true);
                this.__Game_Pathfind_PathElement_RO_BufferLookup = state.GetBufferLookup<PathElement>(true);
                this.__Game_Companies_StorageTransferRequest_RW_BufferLookup = state.GetBufferLookup<StorageTransferRequest>();
                this.__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(true);
                this.__Game_City_CityServiceUpkeep_RO_ComponentLookup = state.GetComponentLookup<CityServiceUpkeep>(true);
                this.__Game_Economy_Resources_RW_BufferLookup = state.GetBufferLookup<Game.Economy.Resources>();
            }
        }
    }
}
