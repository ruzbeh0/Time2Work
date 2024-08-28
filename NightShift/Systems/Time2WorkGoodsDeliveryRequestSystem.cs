
using Game;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Net;
using Game.Pathfind;
using Game.Prefabs;
using Game.Simulation;
using System;
using System.Runtime.CompilerServices;
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
    public partial class Time2WorkGoodsDeliveryRequestSystem : GameSystemBase
    {
        private EndFrameBarrier m_EndFrameBarrier;
        private SimulationSystem m_SimulationSystem;
        private PathfindSetupSystem m_PathfindSetupSystem;
        private ResourceSystem m_ResourceSystem;
        private CitySystem m_CitySystem;
        private PropertyRenterSystem m_PropertyRenterSystem;
        private Time2WorkTimeSystem m_TimeSystem;
        private EntityQuery m_RequestGroup;
        private NativeQueue<Time2WorkGoodsDeliveryRequestSystem.DeliveryOrder> m_DeliveryQueue;
        private Time2WorkGoodsDeliveryRequestSystem.TypeHandle __TypeHandle;

        public override int GetUpdateInterval(SystemUpdatePhase phase) => 16;

        [UnityEngine.Scripting.Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();
            this.m_EndFrameBarrier = this.World.GetOrCreateSystemManaged<EndFrameBarrier>();
            this.m_SimulationSystem = this.World.GetOrCreateSystemManaged<SimulationSystem>();
            this.m_PathfindSetupSystem = this.World.GetOrCreateSystemManaged<PathfindSetupSystem>();
            this.m_ResourceSystem = this.World.GetOrCreateSystemManaged<ResourceSystem>();
            this.m_CitySystem = this.World.GetOrCreateSystemManaged<CitySystem>();
            this.m_PropertyRenterSystem = this.World.GetOrCreateSystemManaged<PropertyRenterSystem>();
            this.m_TimeSystem = this.World.GetOrCreateSystemManaged<Time2WorkTimeSystem>();
            this.m_RequestGroup = this.GetEntityQuery(ComponentType.ReadOnly<GoodsDeliveryRequest>());
            this.m_DeliveryQueue = new NativeQueue<Time2WorkGoodsDeliveryRequestSystem.DeliveryOrder>((AllocatorManager.AllocatorHandle)Allocator.Persistent);
            this.RequireForUpdate(this.m_RequestGroup);
        }

        [UnityEngine.Scripting.Preserve]
        protected override void OnDestroy()
        {
            this.m_DeliveryQueue.Dispose();
            base.OnDestroy();
        }

        [UnityEngine.Scripting.Preserve]
        protected override void OnUpdate()
        {
            this.__TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Simulation_GoodsDeliveryRequest_RO_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref this.CheckedStateRef);

            Time2WorkGoodsDeliveryRequestSystem.FindSellerJob jobData1 = new Time2WorkGoodsDeliveryRequestSystem.FindSellerJob()
            {
                m_EntityType = this.__TypeHandle.__Unity_Entities_Entity_TypeHandle,
                m_RequestType = this.__TypeHandle.__Game_Simulation_GoodsDeliveryRequest_RO_ComponentTypeHandle,
                m_PathInformationType = this.__TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentTypeHandle,
                m_ResourcePrefabs = this.m_ResourceSystem.GetPrefabs(),
                m_ResourceDatas = this.__TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup,
                m_CommandBuffer = this.m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
                m_PathfindQueue = this.m_PathfindSetupSystem.GetQueue((object)this, 64).AsParallelWriter(),
                m_DeliveryQueue = this.m_DeliveryQueue.AsParallelWriter(),
                m_NormalizedTime = this.m_TimeSystem.normalizedTime,
                m_RandomSeed = RandomSeed.Next(),
                night_trucks = Mod.m_Setting.night_trucks
            };
            this.Dependency = jobData1.ScheduleParallel<Time2WorkGoodsDeliveryRequestSystem.FindSellerJob>(this.m_RequestGroup, this.Dependency);
            this.m_PathfindSetupSystem.AddQueueWriter(this.Dependency);
            this.m_EndFrameBarrier.AddJobHandleForProducer(this.Dependency);
            this.__TypeHandle.__Game_Companies_StorageCompany_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Objects_OutsideConnection_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Economy_Resources_RW_BufferLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_TripNeeded_RW_BufferLookup.Update(ref this.CheckedStateRef);

            Time2WorkGoodsDeliveryRequestSystem.DispatchJob jobData2 = new Time2WorkGoodsDeliveryRequestSystem.DispatchJob()
            {
                m_TripNeededs = this.__TypeHandle.__Game_Citizens_TripNeeded_RW_BufferLookup,
                m_Resources = this.__TypeHandle.__Game_Economy_Resources_RW_BufferLookup,
                m_OutsideConnections = this.__TypeHandle.__Game_Objects_OutsideConnection_RO_ComponentLookup,
                m_ResourceDatas = this.__TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup,
                m_StorageCompanies = this.__TypeHandle.__Game_Companies_StorageCompany_RO_ComponentLookup,
                m_ResourcePrefabs = this.m_ResourceSystem.GetPrefabs(),
                m_DeliveryQueue = this.m_DeliveryQueue
            };
            this.Dependency = jobData2.Schedule<Time2WorkGoodsDeliveryRequestSystem.DispatchJob>(this.Dependency);
            this.m_ResourceSystem.AddPrefabsReader(this.Dependency);
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
        public Time2WorkGoodsDeliveryRequestSystem()
        {
        }

        private struct DeliveryOrder
        {
            public Entity m_Entity;
            public TripNeeded m_Trip;
        }

        [BurstCompile]
        private struct DispatchJob : IJob
        {
            public BufferLookup<TripNeeded> m_TripNeededs;
            public NativeQueue<Time2WorkGoodsDeliveryRequestSystem.DeliveryOrder> m_DeliveryQueue;
            [ReadOnly]
            public ComponentLookup<Game.Objects.OutsideConnection> m_OutsideConnections;
            [ReadOnly]
            public ComponentLookup<Game.Companies.StorageCompany> m_StorageCompanies;
            public BufferLookup<Game.Economy.Resources> m_Resources;
            [ReadOnly]
            public ResourcePrefabs m_ResourcePrefabs;
            [ReadOnly]
            public ComponentLookup<ResourceData> m_ResourceDatas;

            public void Execute()
            {
                Time2WorkGoodsDeliveryRequestSystem.DeliveryOrder deliveryOrder;
                while (this.m_DeliveryQueue.TryDequeue(out deliveryOrder))
                {
                    Entity targetAgent = deliveryOrder.m_Trip.m_TargetAgent;
                    if ((double)this.m_ResourceDatas[this.m_ResourcePrefabs[deliveryOrder.m_Trip.m_Resource]].m_Weight > 0.0 && this.m_TripNeededs.HasBuffer(deliveryOrder.m_Entity))
                    {
                        this.m_TripNeededs[deliveryOrder.m_Entity].Add(deliveryOrder.m_Trip);
                    }
                    int x = deliveryOrder.m_Trip.m_Data;
                    if (deliveryOrder.m_Trip.m_Purpose == Game.Citizens.Purpose.Collect)
                    {
                        if (this.m_Resources.HasBuffer(targetAgent))
                        {
                            DynamicBuffer<Game.Economy.Resources> resource = this.m_Resources[targetAgent];
                            x = math.min(x, EconomyUtils.GetResources(deliveryOrder.m_Trip.m_Resource, resource));
                            EconomyUtils.AddResources(deliveryOrder.m_Trip.m_Resource, -x, resource);
                        }
                    }
                    else
                    {
                        if (this.m_Resources.HasBuffer(deliveryOrder.m_Entity))
                        {
                            DynamicBuffer<Game.Economy.Resources> resource = this.m_Resources[deliveryOrder.m_Entity];
                            x = math.min(x, EconomyUtils.GetResources(deliveryOrder.m_Trip.m_Resource, resource));
                            EconomyUtils.AddResources(deliveryOrder.m_Trip.m_Resource, -x, resource);
                        }
                    }

                    int amount = Mathf.RoundToInt(EconomyUtils.GetMarketPrice(deliveryOrder.m_Trip.m_Resource, this.m_ResourcePrefabs, ref this.m_ResourceDatas) * (float)x);
                    if (deliveryOrder.m_Trip.m_Purpose == Game.Citizens.Purpose.Collect)
                        amount *= -1;
                    Entity entity = deliveryOrder.m_Entity;
                    if (!this.m_OutsideConnections.HasComponent(entity) && !this.m_StorageCompanies.HasComponent(entity) && this.m_Resources.HasBuffer(entity))
                    {
                        DynamicBuffer<Game.Economy.Resources> resource = this.m_Resources[entity];
                        EconomyUtils.AddResources(Resource.Money, amount, resource);
                    }
                }
            }
        }

        [BurstCompile]
        private struct FindSellerJob : IJobChunk
        {
            [ReadOnly]
            public EntityTypeHandle m_EntityType;
            [ReadOnly]
            public ComponentTypeHandle<GoodsDeliveryRequest> m_RequestType;
            [ReadOnly]
            public ComponentTypeHandle<PathInformation> m_PathInformationType;
            public EntityCommandBuffer.ParallelWriter m_CommandBuffer;
            public NativeQueue<SetupQueueItem>.ParallelWriter m_PathfindQueue;
            [ReadOnly]
            public ResourcePrefabs m_ResourcePrefabs;
            [ReadOnly]
            public ComponentLookup<ResourceData> m_ResourceDatas;
            public NativeQueue<Time2WorkGoodsDeliveryRequestSystem.DeliveryOrder>.ParallelWriter m_DeliveryQueue;
            public float m_NormalizedTime;
            public RandomSeed m_RandomSeed;
            public bool night_trucks;

            public void Execute(
              in ArchetypeChunk chunk,
              int unfilteredChunkIndex,
              bool useEnabledMask,
              in v128 chunkEnabledMask)
            {
                if (!chunk.Has<PathInformation>(ref this.m_PathInformationType))
                {
                    NativeArray<Entity> nativeArray1 = chunk.GetNativeArray(this.m_EntityType);
                    NativeArray<GoodsDeliveryRequest> nativeArray2 = chunk.GetNativeArray<GoodsDeliveryRequest>(ref this.m_RequestType);
                    for (int index = 0; index < nativeArray2.Length; ++index)
                    {
                        Entity requestEntity = nativeArray1[index];
                        GoodsDeliveryRequest requestData = nativeArray2[index];
                        this.FindVehicleSource(unfilteredChunkIndex, requestEntity, requestData);
                    }
                }
                else
                {
                    NativeArray<PathInformation> nativeArray3 = chunk.GetNativeArray<PathInformation>(ref this.m_PathInformationType);
                    if (nativeArray3.Length == 0)
                        return;
                    NativeArray<Entity> nativeArray4 = chunk.GetNativeArray(this.m_EntityType);
                    NativeArray<GoodsDeliveryRequest> nativeArray5 = chunk.GetNativeArray<GoodsDeliveryRequest>(ref this.m_RequestType);
                    Unity.Mathematics.Random random = this.m_RandomSeed.GetRandom(unfilteredChunkIndex);
                    for (int index = 0; index < nativeArray5.Length; ++index)
                    {
                        float windowStart = 1f - Math.Abs((float)(GaussianRandom.NextGaussianDouble(random))) * (0.375f);
                        float windowEnd = Math.Abs((float)(GaussianRandom.NextGaussianDouble(random))) * (0.25f);
                        //From midnight to 6 AM the drop of trucks is faster than the increase of trucks in the evening to midnight

                        if ((this.m_NormalizedTime > 0.25 && this.m_NormalizedTime < 0.625) && night_trucks)
                        {
                            continue;
                        }

                        Entity e = nativeArray4[index];
                        GoodsDeliveryRequest goodsDeliveryRequest = nativeArray5[index];
                        PathInformation pathInformation = nativeArray3[index];
                        if ((pathInformation.m_State & PathFlags.Pending) == (PathFlags)0)
                        {
                            if (pathInformation.m_Origin != Entity.Null)
                            {
                                Game.Citizens.Purpose purpose = (goodsDeliveryRequest.m_Flags & GoodsDeliveryFlags.ResourceExportTarget) == (GoodsDeliveryFlags)0 ? ((goodsDeliveryRequest.m_Flags & GoodsDeliveryFlags.BuildingUpkeep) == (GoodsDeliveryFlags)0 ? Game.Citizens.Purpose.Delivery : Game.Citizens.Purpose.UpkeepDelivery) : Game.Citizens.Purpose.Collect;

                                this.m_DeliveryQueue.Enqueue(new Time2WorkGoodsDeliveryRequestSystem.DeliveryOrder()
                                {
                                    m_Entity = pathInformation.m_Origin,
                                    m_Trip = new TripNeeded()
                                    {
                                        m_Data = goodsDeliveryRequest.m_Amount,
                                        m_Purpose = purpose,
                                        m_Resource = goodsDeliveryRequest.m_Resource,
                                        m_TargetAgent = goodsDeliveryRequest.m_Target
                                    }
                                });
                            }

                            this.m_CommandBuffer.DestroyEntity(unfilteredChunkIndex, e);
                        }
                    }
                }
            }

            private void FindVehicleSource(
              int jobIndex,
              Entity requestEntity,
              GoodsDeliveryRequest requestData)
            {
                float transportCost = EconomyUtils.GetTransportCost(100f, requestData.m_Amount, this.m_ResourceDatas[this.m_ResourcePrefabs[requestData.m_Resource]].m_Weight, StorageTransferFlags.Car);
                PathfindParameters parameters = new PathfindParameters()
                {
                    m_MaxSpeed = (float2)111.111115f,
                    m_WalkSpeed = (float2)5.555556f,
                    m_Weights = new PathfindWeights(1f, 1f, transportCost, 1f),
                    m_Methods = PathMethod.Road | PathMethod.CargoLoading,
                    m_IgnoredRules = RuleFlags.ForbidSlowTraffic
                };
                SetupQueueTarget setupQueueTarget = new SetupQueueTarget();
                setupQueueTarget.m_Type = (requestData.m_Flags & GoodsDeliveryFlags.ResourceExportTarget) != (GoodsDeliveryFlags)0 ? SetupTargetType.ResourceExport : SetupTargetType.ResourceSeller;
                setupQueueTarget.m_Methods = PathMethod.Road | PathMethod.CargoLoading;
                setupQueueTarget.m_RoadTypes = RoadTypes.Car;
                setupQueueTarget.m_Value = requestData.m_Amount;
                setupQueueTarget.m_Resource = requestData.m_Resource;
                SetupQueueTarget origin = setupQueueTarget;
                setupQueueTarget = new SetupQueueTarget();
                setupQueueTarget.m_Type = SetupTargetType.CurrentLocation;
                setupQueueTarget.m_Methods = PathMethod.Road | PathMethod.CargoLoading;
                setupQueueTarget.m_RoadTypes = RoadTypes.Car;
                setupQueueTarget.m_Entity = requestData.m_Target;
                SetupQueueTarget destination = setupQueueTarget;
                if ((requestData.m_Flags & GoodsDeliveryFlags.CommercialAllowed) != (GoodsDeliveryFlags)0)
                    origin.m_Flags |= SetupTargetFlags.Commercial;
                if ((requestData.m_Flags & GoodsDeliveryFlags.IndustrialAllowed) != (GoodsDeliveryFlags)0)
                    origin.m_Flags |= SetupTargetFlags.Industrial;
                if ((requestData.m_Flags & GoodsDeliveryFlags.ImportAllowed) != (GoodsDeliveryFlags)0)
                    origin.m_Flags |= SetupTargetFlags.Import;

                if ((double)this.m_ResourceDatas[this.m_ResourcePrefabs[requestData.m_Resource]].m_Weight > 0.0)
                    origin.m_Flags |= SetupTargetFlags.RequireTransport;

                this.m_PathfindQueue.Enqueue(new SetupQueueItem(requestEntity, parameters, origin, destination));

                this.m_CommandBuffer.AddComponent<PathInformation>(jobIndex, requestEntity, new PathInformation()
                {
                    m_State = PathFlags.Pending
                });

                this.m_CommandBuffer.AddBuffer<PathElement>(jobIndex, requestEntity);
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
            public ComponentTypeHandle<GoodsDeliveryRequest> __Game_Simulation_GoodsDeliveryRequest_RO_ComponentTypeHandle;
            [ReadOnly]
            public ComponentTypeHandle<PathInformation> __Game_Pathfind_PathInformation_RO_ComponentTypeHandle;
            [ReadOnly]
            public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;
            public BufferLookup<TripNeeded> __Game_Citizens_TripNeeded_RW_BufferLookup;
            public BufferLookup<Game.Economy.Resources> __Game_Economy_Resources_RW_BufferLookup;
            [ReadOnly]
            public ComponentLookup<Game.Objects.OutsideConnection> __Game_Objects_OutsideConnection_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Game.Companies.StorageCompany> __Game_Companies_StorageCompany_RO_ComponentLookup;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void __AssignHandles(ref SystemState state)
            {
                this.__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
                this.__Game_Simulation_GoodsDeliveryRequest_RO_ComponentTypeHandle = state.GetComponentTypeHandle<GoodsDeliveryRequest>(true);
                this.__Game_Pathfind_PathInformation_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PathInformation>(true);
                this.__Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(true);
                this.__Game_Citizens_TripNeeded_RW_BufferLookup = state.GetBufferLookup<TripNeeded>();
                this.__Game_Economy_Resources_RW_BufferLookup = state.GetBufferLookup<Game.Economy.Resources>();
                this.__Game_Objects_OutsideConnection_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.OutsideConnection>(true);
                this.__Game_Companies_StorageCompany_RO_ComponentLookup = state.GetComponentLookup<Game.Companies.StorageCompany>(true);
            }
        }
    }
}
