using Colossal.Entities;
using Colossal.Mathematics;
using Game;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Creatures;
using Game.Economy;
using Game.Net;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
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
using UnityEngine.Assertions;

#nullable disable
namespace Time2Work
{
    [CompilerGenerated]
    public partial class Time2WorkTrafficSpawnerAISystem : GameSystemBase
    {
        private EntityQuery m_BuildingQuery;
        private EntityQuery m_PersonalCarQuery;
        private EntityQuery m_TransportVehicleQuery;
        private EntityQuery m_CreaturePrefabQuery;
        private SimulationSystem m_SimulationSystem;
        private ClimateSystem m_ClimateSystem;
        private CityConfigurationSystem m_CityConfigurationSystem;
        private VehicleCapacitySystem m_VehicleCapacitySystem;
        private EndFrameBarrier m_EndFrameBarrier;
        private EntityArchetype m_TrafficRequestArchetype;
        private EntityArchetype m_HandleRequestArchetype;
        private ComponentTypeSet m_CurrentLaneTypesRelative;
        private PersonalCarSelectData m_PersonalCarSelectData;
        private TransportVehicleSelectData m_TransportVehicleSelectData;
        private Time2WorkTimeSystem m_TimeSystem;
        private Time2WorkTrafficSpawnerAISystem.TypeHandle __TypeHandle;

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            return phase == SystemUpdatePhase.LoadSimulation ? 16 : 256;
        }

        public override int GetUpdateOffset(SystemUpdatePhase phase)
        {
            return phase == SystemUpdatePhase.LoadSimulation ? 2 : 32;
        }

        [UnityEngine.Scripting.Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();
            this.m_SimulationSystem = this.World.GetOrCreateSystemManaged<SimulationSystem>();
            this.m_ClimateSystem = this.World.GetOrCreateSystemManaged<ClimateSystem>();
            this.m_EndFrameBarrier = this.World.GetOrCreateSystemManaged<EndFrameBarrier>();
            this.m_CityConfigurationSystem = this.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
            this.m_VehicleCapacitySystem = this.World.GetOrCreateSystemManaged<VehicleCapacitySystem>();
            this.m_TimeSystem = this.World.GetOrCreateSystemManaged<Time2WorkTimeSystem>();
            this.m_PersonalCarSelectData = new PersonalCarSelectData((SystemBase)this);
            this.m_TransportVehicleSelectData = new TransportVehicleSelectData((SystemBase)this);
            this.m_BuildingQuery = this.GetEntityQuery(ComponentType.ReadOnly<Game.Buildings.TrafficSpawner>(), ComponentType.ReadOnly<ServiceDispatch>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Destroyed>(), ComponentType.Exclude<Deleted>());
            this.m_PersonalCarQuery = this.GetEntityQuery(PersonalCarSelectData.GetEntityQueryDesc());
            this.m_TransportVehicleQuery = this.GetEntityQuery(TransportVehicleSelectData.GetEntityQueryDesc());
            this.m_CreaturePrefabQuery = this.GetEntityQuery(ComponentType.ReadOnly<CreatureData>(), ComponentType.ReadOnly<PrefabData>());
            this.m_TrafficRequestArchetype = this.EntityManager.CreateArchetype(ComponentType.ReadWrite<ServiceRequest>(), ComponentType.ReadWrite<RandomTrafficRequest>(), ComponentType.ReadWrite<RequestGroup>());
            this.m_HandleRequestArchetype = this.EntityManager.CreateArchetype(ComponentType.ReadWrite<HandleRequest>(), ComponentType.ReadWrite<Event>());
            this.m_CurrentLaneTypesRelative = new ComponentTypeSet(new ComponentType[5]
            {
        ComponentType.ReadWrite<Moving>(),
        ComponentType.ReadWrite<TransformFrame>(),
        ComponentType.ReadWrite<HumanNavigation>(),
        ComponentType.ReadWrite<HumanCurrentLane>(),
        ComponentType.ReadWrite<Blocker>()
            });
            // ISSUE: reference to a compiler-generated field
            this.RequireForUpdate(this.m_BuildingQuery);
            Assert.IsTrue(true);
        }

        [UnityEngine.Scripting.Preserve]
        protected override void OnUpdate()
        {
            if (m_BuildingQuery != default && !m_BuildingQuery.IsEmptyIgnoreFilter)
            {
                JobHandle jobHandle1;

                this.m_PersonalCarSelectData.PreUpdate((SystemBase)this, this.m_CityConfigurationSystem, this.m_PersonalCarQuery, Allocator.TempJob, out jobHandle1);
                JobHandle jobHandle2;

                this.m_TransportVehicleSelectData.PreUpdate((SystemBase)this, this.m_CityConfigurationSystem, this.m_TransportVehicleQuery, Allocator.TempJob, out jobHandle2);
                JobHandle outJobHandle;
                NativeList<ArchetypeChunk> archetypeChunkListAsync = this.m_CreaturePrefabQuery.ToArchetypeChunkListAsync((AllocatorManager.AllocatorHandle)Allocator.TempJob, out outJobHandle);

                JobHandle jobHandle3 = new Time2WorkTrafficSpawnerAISystem.TrafficSpawnerTickJob()
                {
                    m_EntityType = this.__TypeHandle.__Unity_Entities_Entity_TypeHandle,
                    m_TrafficSpawnerType = this.__TypeHandle.__Game_Buildings_TrafficSpawner_RO_ComponentTypeHandle,
                    m_PrefabRefType = this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle,
                    m_CreatureDataType = this.__TypeHandle.__Game_Prefabs_CreatureData_RO_ComponentTypeHandle,
                    m_ResidentDataType = this.__TypeHandle.__Game_Prefabs_ResidentData_RO_ComponentTypeHandle,
                    m_ServiceDispatchType = this.__TypeHandle.__Game_Simulation_ServiceDispatch_RW_BufferTypeHandle,
                    m_PrefabTrafficSpawnerData = this.__TypeHandle.__Game_Prefabs_TrafficSpawnerData_RO_ComponentLookup,
                    m_PrefabRefData = this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup,
                    m_PrefabDeliveryTruckData = this.__TypeHandle.__Game_Prefabs_DeliveryTruckData_RO_ComponentLookup,
                    m_PrefabObjectData = this.__TypeHandle.__Game_Prefabs_ObjectData_RO_ComponentLookup,
                    m_RandomTrafficRequestData = this.__TypeHandle.__Game_Simulation_RandomTrafficRequest_RO_ComponentLookup,
                    m_ServiceRequestData = this.__TypeHandle.__Game_Simulation_ServiceRequest_RO_ComponentLookup,
                    m_PathInformationData = this.__TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentLookup,
                    m_TransformData = this.__TypeHandle.__Game_Objects_Transform_RO_ComponentLookup,
                    m_CurveData = this.__TypeHandle.__Game_Net_Curve_RO_ComponentLookup,
                    m_PathElements = this.__TypeHandle.__Game_Pathfind_PathElement_RO_BufferLookup,
                    m_ActivityLocationElements = this.__TypeHandle.__Game_Prefabs_ActivityLocationElement_RO_BufferLookup,
                    m_Loading = this.m_SimulationSystem.loadingProgress,
                    m_LeftHandTraffic = this.m_CityConfigurationSystem.leftHandTraffic,
                    m_RandomSeed = RandomSeed.Next(),
                    m_VehicleRequestArchetype = this.m_TrafficRequestArchetype,
                    m_HandleRequestArchetype = this.m_HandleRequestArchetype,
                    m_DeliveryTruckSelectData = this.m_VehicleCapacitySystem.GetDeliveryTruckSelectData(),
                    m_PersonalCarSelectData = this.m_PersonalCarSelectData,
                    m_TransportVehicleSelectData = this.m_TransportVehicleSelectData,
                    m_CreaturePrefabChunks = archetypeChunkListAsync,
                    m_CurrentLaneTypesRelative = this.m_CurrentLaneTypesRelative,
                    m_CommandBuffer = this.m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
                    m_NormalizedTime = this.m_TimeSystem.normalizedTime,
                    night_trucks = Mod.m_Setting.night_trucks
                }.ScheduleParallel<Time2WorkTrafficSpawnerAISystem.TrafficSpawnerTickJob>(this.m_BuildingQuery, JobUtils.CombineDependencies(this.Dependency, jobHandle1, jobHandle2, outJobHandle));
                this.m_PersonalCarSelectData.PostUpdate(jobHandle3);
                this.m_TransportVehicleSelectData.PostUpdate(jobHandle3);
                this.m_EndFrameBarrier.AddJobHandleForProducer(jobHandle3);
                archetypeChunkListAsync.Dispose(jobHandle3);
                this.Dependency = jobHandle3;
            }
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
        public Time2WorkTrafficSpawnerAISystem()
        {
        }

        [BurstCompile]
        private struct TrafficSpawnerTickJob : IJobChunk
        {
            [ReadOnly]
            public EntityTypeHandle m_EntityType;
            [ReadOnly]
            public ComponentTypeHandle<Game.Buildings.TrafficSpawner> m_TrafficSpawnerType;
            [ReadOnly]
            public ComponentTypeHandle<PrefabRef> m_PrefabRefType;
            [ReadOnly]
            public ComponentTypeHandle<CreatureData> m_CreatureDataType;
            [ReadOnly]
            public ComponentTypeHandle<ResidentData> m_ResidentDataType;
            public BufferTypeHandle<ServiceDispatch> m_ServiceDispatchType;
            [ReadOnly]
            public ComponentLookup<TrafficSpawnerData> m_PrefabTrafficSpawnerData;
            [ReadOnly]
            public ComponentLookup<PrefabRef> m_PrefabRefData;
            [ReadOnly]
            public ComponentLookup<DeliveryTruckData> m_PrefabDeliveryTruckData;
            [ReadOnly]
            public ComponentLookup<ObjectData> m_PrefabObjectData;
            [ReadOnly]
            public ComponentLookup<RandomTrafficRequest> m_RandomTrafficRequestData;
            [ReadOnly]
            public ComponentLookup<ServiceRequest> m_ServiceRequestData;
            [ReadOnly]
            public ComponentLookup<PathInformation> m_PathInformationData;
            [ReadOnly]
            public ComponentLookup<Transform> m_TransformData;
            [ReadOnly]
            public ComponentLookup<Curve> m_CurveData;
            [ReadOnly]
            public BufferLookup<PathElement> m_PathElements;
            [ReadOnly]
            public BufferLookup<ActivityLocationElement> m_ActivityLocationElements;
            [ReadOnly]
            public float m_Loading;
            [ReadOnly]
            public bool m_LeftHandTraffic;
            [ReadOnly]
            public RandomSeed m_RandomSeed;
            [ReadOnly]
            public EntityArchetype m_VehicleRequestArchetype;
            [ReadOnly]
            public EntityArchetype m_HandleRequestArchetype;
            [ReadOnly]
            public DeliveryTruckSelectData m_DeliveryTruckSelectData;
            [ReadOnly]
            public PersonalCarSelectData m_PersonalCarSelectData;
            [ReadOnly]
            public TransportVehicleSelectData m_TransportVehicleSelectData;
            [ReadOnly]
            public NativeList<ArchetypeChunk> m_CreaturePrefabChunks;
            [ReadOnly]
            public ComponentTypeSet m_CurrentLaneTypesRelative;
            public EntityCommandBuffer.ParallelWriter m_CommandBuffer;
            public float m_NormalizedTime;
            public bool night_trucks;

            public void Execute(
              in ArchetypeChunk chunk,
              int unfilteredChunkIndex,
              bool useEnabledMask,
              in v128 chunkEnabledMask)
            {
                NativeArray<Entity> nativeArray1 = chunk.GetNativeArray(this.m_EntityType);
                NativeArray<Game.Buildings.TrafficSpawner> nativeArray2 = chunk.GetNativeArray<Game.Buildings.TrafficSpawner>(ref this.m_TrafficSpawnerType);
                NativeArray<PrefabRef> nativeArray3 = chunk.GetNativeArray<PrefabRef>(ref this.m_PrefabRefType);
                BufferAccessor<ServiceDispatch> bufferAccessor = chunk.GetBufferAccessor<ServiceDispatch>(ref this.m_ServiceDispatchType);
                Unity.Mathematics.Random random = this.m_RandomSeed.GetRandom(unfilteredChunkIndex);
                for (int index = 0; index < nativeArray1.Length; ++index)
                {
                    Entity entity = nativeArray1[index];
                    Game.Buildings.TrafficSpawner trafficSpawner = nativeArray2[index];
                    PrefabRef prefabRef = nativeArray3[index];
                    DynamicBuffer<ServiceDispatch> dispatches = bufferAccessor[index];
                    this.Tick(unfilteredChunkIndex, entity, ref random, trafficSpawner, prefabRef, dispatches);
                }
            }

            private void Tick(
        int jobIndex,
        Entity entity,
        ref Unity.Mathematics.Random random,
        Game.Buildings.TrafficSpawner trafficSpawner,
        PrefabRef prefabRef,
        DynamicBuffer<ServiceDispatch> dispatches)
            {
                // ISSUE: reference to a compiler-generated field
                TrafficSpawnerData prefabTrafficSpawnerData = this.m_PrefabTrafficSpawnerData[prefabRef.m_Prefab];
                float num1 = prefabTrafficSpawnerData.m_SpawnRate * 4.266667f;
                float num2 = random.NextFloat(num1 * 0.5f, num1 * 1.5f);
                // ISSUE: reference to a compiler-generated field
                if (MathUtils.RoundToIntRandom(ref random, num2) > 0 && !this.m_RandomTrafficRequestData.HasComponent(trafficSpawner.m_TrafficRequest))
                {
                    // ISSUE: reference to a compiler-generated method
                    this.RequestVehicle(jobIndex, ref random, entity, prefabTrafficSpawnerData);
                }
                for (int index1 = 0; index1 < dispatches.Length; ++index1)
                {
                    Entity request = dispatches[index1].m_Request;
                    // ISSUE: reference to a compiler-generated field
                    if (this.m_RandomTrafficRequestData.HasComponent(request))
                    {
                        // ISSUE: reference to a compiler-generated field
                        int num3 = (double)this.m_Loading >= 0.89999997615814209 ? 1 : ((prefabTrafficSpawnerData.m_RoadType & RoadTypes.Airplane) == RoadTypes.None ? ((prefabTrafficSpawnerData.m_TrackType & TrackTypes.Train) == TrackTypes.None ? 2 : 0) : random.NextInt(2));
                        for (int index2 = 0; index2 < num3; ++index2)
                        {
                            // ISSUE: reference to a compiler-generated method
                            this.SpawnVehicle(jobIndex, ref random, entity, request, prefabTrafficSpawnerData);
                        }
                        dispatches.RemoveAt(index1--);
                    }
                    else
                    {
                        // ISSUE: reference to a compiler-generated field
                        if (!this.m_ServiceRequestData.HasComponent(request))
                            dispatches.RemoveAt(index1--);
                    }
                }
            }

            private void RequestVehicle(
                int jobIndex,
                ref Unity.Mathematics.Random random,
                Entity entity,
                TrafficSpawnerData prefabTrafficSpawnerData)
            {
                SizeClass sizeClass = SizeClass.Small;
                RandomTrafficRequestFlags flags = (RandomTrafficRequestFlags)0;
                if ((prefabTrafficSpawnerData.m_RoadType & RoadTypes.Car) != RoadTypes.None)
                {
                    int num = random.NextInt(100);
                    if (num < 20)
                    {
                        sizeClass = SizeClass.Large;
                        flags |= RandomTrafficRequestFlags.DeliveryTruck;
                    }
                    else if (num < 25)
                    {
                        sizeClass = SizeClass.Large;
                        flags |= RandomTrafficRequestFlags.TransportVehicle;
                    }
                }
                else
                {
                    sizeClass = SizeClass.Large;
                    flags |= RandomTrafficRequestFlags.TransportVehicle;
                }
                if (prefabTrafficSpawnerData.m_NoSlowVehicles)
                    flags |= RandomTrafficRequestFlags.NoSlowVehicles;
                Entity entity1 = this.m_CommandBuffer.CreateEntity(jobIndex, this.m_VehicleRequestArchetype);
                this.m_CommandBuffer.SetComponent<RandomTrafficRequest>(jobIndex, entity1, new RandomTrafficRequest(entity, prefabTrafficSpawnerData.m_RoadType, prefabTrafficSpawnerData.m_TrackType, EnergyTypes.FuelAndElectricity, sizeClass, flags));
                this.m_CommandBuffer.SetComponent<RequestGroup>(jobIndex, entity1, new RequestGroup(16U /*0x10*/));
            }

            private void SpawnVehicle(
      int jobIndex,
      ref Unity.Mathematics.Random random,
      Entity entity,
      Entity request,
      TrafficSpawnerData prefabTrafficSpawnerData)
            {
                RandomTrafficRequest componentData1;
                PathInformation componentData2;

                if (!this.m_RandomTrafficRequestData.TryGetComponent(request, out componentData1) || !this.m_PathInformationData.TryGetComponent(request, out componentData2) || !this.m_PrefabRefData.HasComponent(componentData2.m_Destination))
                    return;
                uint delay = random.NextUInt(256U /*0x0100*/);
                Entity source = entity;
                // ISSUE: reference to a compiler-generated field
                Transform transform = this.m_TransformData[entity];
                int num1 = 0;
                DynamicBuffer<PathElement> bufferData;
                // ISSUE: reference to a compiler-generated field
                this.m_PathElements.TryGetBuffer(request, out bufferData);
                // ISSUE: reference to a compiler-generated field
                if ((double)this.m_Loading < 0.89999997615814209)
                {
                    delay = 0U;
                    source = Entity.Null;
                    if (bufferData.IsCreated && bufferData.Length >= 5)
                    {
                        num1 = random.NextInt(2, bufferData.Length * 3 / 4);
                        PathElement pathElement = bufferData[num1];
                        Curve componentData3;
                        // ISSUE: reference to a compiler-generated field
                        if (this.m_CurveData.TryGetComponent(pathElement.m_Target, out componentData3))
                        {
                            float3 falseValue = MathUtils.Tangent(componentData3.m_Bezier, pathElement.m_TargetDelta.x);
                            float3 forward = math.select(falseValue, -falseValue, (double)pathElement.m_TargetDelta.y < (double)pathElement.m_TargetDelta.x);
                            transform.m_Position = MathUtils.Position(componentData3.m_Bezier, pathElement.m_TargetDelta.x);
                            transform.m_Rotation = quaternion.LookRotationSafe(forward, math.up());
                        }
                    }
                }
                Entity vehicle = Entity.Null;
                if ((componentData1.m_Flags & RandomTrafficRequestFlags.DeliveryTruck) != (RandomTrafficRequestFlags)0)
                {
                    float windowStart = 1f - Math.Abs((float)(GaussianRandom.NextGaussianDouble(random))) * (0.375f);
                    float windowEnd = windowStart - 0.375f;
                    if ((this.m_NormalizedTime > 0.25f && this.m_NormalizedTime < 0.625f) && night_trucks)
                    {
                        return;
                    }

                    Resource randomResource = this.GetRandomResource(ref random);
                    int max;
                    // ISSUE: reference to a compiler-generated field
                    this.m_DeliveryTruckSelectData.GetCapacityRange(Resource.NoResource, out int _, out max);
                    int amount = random.NextInt(1, max + max / 10 + 1);
                    int returnAmount = 0;
                    DeliveryTruckFlags state = DeliveryTruckFlags.DummyTraffic;
                    if (random.NextInt(100) < 75)
                        state |= DeliveryTruckFlags.Loaded;
                    DeliveryTruckSelectItem selectItem;
                    // ISSUE: reference to a compiler-generated field
                    if (this.m_DeliveryTruckSelectData.TrySelectItem(ref random, randomResource, amount, out selectItem))
                    {
                        vehicle = this.m_DeliveryTruckSelectData.CreateVehicle(this.m_CommandBuffer, jobIndex, ref random, ref this.m_PrefabDeliveryTruckData, ref this.m_PrefabObjectData, selectItem, randomResource, Resource.NoResource, ref amount, ref returnAmount, transform, source, state, delay);
                    }
                    int maxCount = 1;
                    // ISSUE: reference to a compiler-generated method
                    if (this.CreatePassengers(jobIndex, vehicle, selectItem.m_Prefab1, transform, true, ref maxCount, ref random) > 0)
                    {
                        // ISSUE: reference to a compiler-generated field
                        this.m_CommandBuffer.AddBuffer<Passenger>(jobIndex, vehicle);
                    }
                }
                else if ((componentData1.m_Flags & RandomTrafficRequestFlags.TransportVehicle) != (RandomTrafficRequestFlags)0)
                {
                    TransportType transportType = TransportType.None;
                    PublicTransportPurpose publicTransportPurpose = (PublicTransportPurpose)0;
                    Resource cargoResources = Resource.NoResource;
                    int2 passengerCapacity = (int2)0;
                    int2 cargoCapacity = (int2)0;
                    if ((componentData1.m_RoadType & RoadTypes.Car) != RoadTypes.None)
                    {
                        transportType = TransportType.Bus;
                        publicTransportPurpose = PublicTransportPurpose.TransportLine;
                        passengerCapacity = new int2(1, int.MaxValue);
                    }
                    else if ((componentData1.m_RoadType & RoadTypes.Airplane) != RoadTypes.None)
                    {
                        transportType = TransportType.Airplane;
                        if (random.NextInt(100) < 25)
                        {
                            cargoResources = Resource.Food;
                            cargoCapacity = new int2(1, int.MaxValue);
                        }
                        else
                        {
                            publicTransportPurpose = PublicTransportPurpose.TransportLine;
                            passengerCapacity = new int2(1, int.MaxValue);
                        }
                    }
                    else if ((componentData1.m_RoadType & RoadTypes.Watercraft) != RoadTypes.None)
                    {
                        transportType = TransportType.Ship;
                        if (random.NextInt(100) < 50)
                        {
                            cargoResources = Resource.Food;
                            cargoCapacity = new int2(1, int.MaxValue);
                        }
                        else
                        {
                            publicTransportPurpose = PublicTransportPurpose.TransportLine;
                            passengerCapacity = new int2(1, int.MaxValue);
                        }
                    }
                    else if ((componentData1.m_TrackType & TrackTypes.Train) != TrackTypes.None)
                    {
                        transportType = TransportType.Train;
                        if (random.NextInt(100) < 50)
                        {
                            cargoResources = Resource.Food;
                            cargoCapacity = new int2(1, int.MaxValue);
                        }
                        else
                        {
                            publicTransportPurpose = PublicTransportPurpose.TransportLine;
                            passengerCapacity = new int2(1, int.MaxValue);
                        }
                    }
                    // ISSUE: reference to a compiler-generated field
                    // ISSUE: reference to a compiler-generated field
                    vehicle = this.m_TransportVehicleSelectData.CreateVehicle(this.m_CommandBuffer, jobIndex, ref random, transform, source, Entity.Null, Entity.Null, transportType, componentData1.m_EnergyTypes, componentData1.m_SizeClass, publicTransportPurpose, cargoResources, ref passengerCapacity, ref cargoCapacity, false);
                    if (vehicle != Entity.Null)
                    {
                        if (publicTransportPurpose != (PublicTransportPurpose)0)
                        {
                            // ISSUE: reference to a compiler-generated field
                            this.m_CommandBuffer.SetComponent<Game.Vehicles.PublicTransport>(jobIndex, vehicle, new Game.Vehicles.PublicTransport()
                            {
                                m_State = PublicTransportFlags.DummyTraffic
                            });
                        }
                        if (cargoResources != Resource.NoResource)
                        {
                            // ISSUE: reference to a compiler-generated field
                            this.m_CommandBuffer.SetComponent<Game.Vehicles.CargoTransport>(jobIndex, vehicle, new Game.Vehicles.CargoTransport()
                            {
                                m_State = CargoTransportFlags.DummyTraffic
                            });
                            // ISSUE: reference to a compiler-generated field
                            DynamicBuffer<LoadingResources> dynamicBuffer = this.m_CommandBuffer.SetBuffer<LoadingResources>(jobIndex, vehicle);
                            int min = random.NextInt(1, math.min(5, cargoCapacity.y + 1));
                            int num2 = random.NextInt(min, cargoCapacity.y + cargoCapacity.y / 10 + 1);
                            int num3 = 0;
                            for (int index = 0; index < min; ++index)
                            {
                                int num4 = random.NextInt(1, 100000);
                                num3 += num4;
                                // ISSUE: reference to a compiler-generated method
                                dynamicBuffer.Add(new LoadingResources()
                                {
                                    m_Resource = this.GetRandomResource(ref random),
                                    m_Amount = num4
                                });
                            }
                            for (int index = 0; index < min; ++index)
                            {
                                LoadingResources loadingResources = dynamicBuffer[index];
                                int amount = loadingResources.m_Amount;
                                loadingResources.m_Amount = (int)(((long)amount * (long)num2 + (long)(num3 >> 1)) / (long)num3);
                                num3 -= amount;
                                num2 -= loadingResources.m_Amount;
                                dynamicBuffer[index] = loadingResources;
                            }
                        }
                    }
                }
                else
                {
                    int maxCount = random.NextInt(1, 6);
                    int baggageAmount = random.NextInt(1, 6);
                    if (random.NextInt(20) == 0)
                    {
                        maxCount += 5;
                        baggageAmount += 5;
                    }
                    else if (random.NextInt(10) == 0)
                    {
                        baggageAmount += 5;
                        if (random.NextInt(10) == 0)
                            baggageAmount += 5;
                    }
                    bool noSlowVehicles = prefabTrafficSpawnerData.m_NoSlowVehicles | (componentData1.m_Flags & RandomTrafficRequestFlags.NoSlowVehicles) != 0;
                    Entity trailer;
                    Entity vehiclePrefab;
                    Entity trailerPrefab;
                    // ISSUE: reference to a compiler-generated field
                    // ISSUE: reference to a compiler-generated field
                    vehicle = this.m_PersonalCarSelectData.CreateVehicle(this.m_CommandBuffer, jobIndex, ref random, maxCount, baggageAmount, false, noSlowVehicles, transform, source, Entity.Null, PersonalCarFlags.DummyTraffic, false, delay, out trailer, out vehiclePrefab, out trailerPrefab);
                    // ISSUE: reference to a compiler-generated method
                    this.CreatePassengers(jobIndex, vehicle, vehiclePrefab, transform, true, ref maxCount, ref random);
                    // ISSUE: reference to a compiler-generated method
                    this.CreatePassengers(jobIndex, trailer, trailerPrefab, transform, false, ref maxCount, ref random);
                }
                if (vehicle == Entity.Null)
                    return;
                // ISSUE: reference to a compiler-generated field
                this.m_CommandBuffer.SetComponent<Target>(jobIndex, vehicle, new Target(componentData2.m_Destination));
                // ISSUE: reference to a compiler-generated field
                this.m_CommandBuffer.AddComponent<Owner>(jobIndex, vehicle, new Owner(entity));
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                Entity entity1 = this.m_CommandBuffer.CreateEntity(jobIndex, this.m_HandleRequestArchetype);
                // ISSUE: reference to a compiler-generated field
                this.m_CommandBuffer.SetComponent<HandleRequest>(jobIndex, entity1, new HandleRequest(request, vehicle, true));
                if (source == Entity.Null)
                {
                    if ((componentData1.m_RoadType & RoadTypes.Car) != RoadTypes.None)
                    {
                        CarCurrentLane component = new CarCurrentLane();
                        component.m_LaneFlags |= Game.Vehicles.CarLaneFlags.ResetSpeed;
                        // ISSUE: reference to a compiler-generated field
                        this.m_CommandBuffer.SetComponent<CarCurrentLane>(jobIndex, vehicle, component);
                    }
                    else if ((componentData1.m_RoadType & RoadTypes.Airplane) != RoadTypes.None)
                    {
                        AircraftCurrentLane component = new AircraftCurrentLane();
                        component.m_LaneFlags |= AircraftLaneFlags.ResetSpeed | AircraftLaneFlags.Flying;
                        // ISSUE: reference to a compiler-generated field
                        this.m_CommandBuffer.SetComponent<AircraftCurrentLane>(jobIndex, vehicle, component);
                    }
                    else if ((componentData1.m_RoadType & RoadTypes.Watercraft) != RoadTypes.None)
                    {
                        WatercraftCurrentLane component = new WatercraftCurrentLane();
                        component.m_LaneFlags |= WatercraftLaneFlags.ResetSpeed;
                        // ISSUE: reference to a compiler-generated field
                        this.m_CommandBuffer.SetComponent<WatercraftCurrentLane>(jobIndex, vehicle, component);
                    }
                }
                if (!bufferData.IsCreated || bufferData.Length == 0)
                    return;
                // ISSUE: reference to a compiler-generated field
                DynamicBuffer<PathElement> targetElements = this.m_CommandBuffer.SetBuffer<PathElement>(jobIndex, vehicle);
                PathUtils.CopyPath(bufferData, new PathOwner(), 0, targetElements);
                // ISSUE: reference to a compiler-generated field
                this.m_CommandBuffer.SetComponent<PathOwner>(jobIndex, vehicle, new PathOwner(num1, PathFlags.Updated));
                if ((componentData1.m_Flags & RandomTrafficRequestFlags.DeliveryTruck) == (RandomTrafficRequestFlags)0)
                    return;
                // ISSUE: reference to a compiler-generated field
                this.m_CommandBuffer.SetComponent<PathInformation>(jobIndex, vehicle, componentData2);
            }
            private int CreatePassengers(
              int jobIndex,
              Entity vehicleEntity,
              Entity vehiclePrefab,
              Transform transform,
              bool driver,
              ref int maxCount,
              ref Unity.Mathematics.Random random)
            {
                int passengers = 0;
                DynamicBuffer<ActivityLocationElement> bufferData;
                if (maxCount > 0 && this.m_ActivityLocationElements.TryGetBuffer(vehiclePrefab, out bufferData))
                {
                    ActivityMask activityMask = new ActivityMask(ActivityType.Driving);
                    int num1 = 0;
                    int num2 = -1;
                    float num3 = float.MinValue;
                    for (int index = 0; index < bufferData.Length; ++index)
                    {
                        ActivityLocationElement activityLocationElement = bufferData[index];
                        if (((int)activityLocationElement.m_ActivityMask.m_Mask & (int)activityMask.m_Mask) != 0)
                        {
                            ++num1;
                            bool test = (activityLocationElement.m_ActivityFlags & ActivityFlags.InvertLefthandTraffic) != (ActivityFlags)0 && this.m_LeftHandTraffic || (activityLocationElement.m_ActivityFlags & ActivityFlags.InvertRighthandTraffic) != (ActivityFlags)0 && !this.m_LeftHandTraffic;
                            activityLocationElement.m_Position.x = math.select(activityLocationElement.m_Position.x, -activityLocationElement.m_Position.x, test);
                            if (((double)math.abs(activityLocationElement.m_Position.x) < 0.5 || (double)activityLocationElement.m_Position.x >= 0.0 == this.m_LeftHandTraffic) && (double)activityLocationElement.m_Position.z > (double)num3)
                            {
                                num2 = index;
                                num3 = activityLocationElement.m_Position.z;
                            }
                        }
                    }
                    int num4 = 100;
                    if (driver && num2 != -1)
                    {
                        --maxCount;
                        --num1;
                    }
                    if (num1 > maxCount)
                        num4 = maxCount * 100 / num1;
                    for (int index = 0; index < bufferData.Length; ++index)
                    {
                        ActivityLocationElement activityLocationElement = bufferData[index];
                        if (((int)activityLocationElement.m_ActivityMask.m_Mask & (int)activityMask.m_Mask) != 0 && (driver && index == num2 || random.NextInt(100) >= num4))
                        {
                            Relative component1;
                            component1.m_Position = activityLocationElement.m_Position;
                            component1.m_Rotation = activityLocationElement.m_Rotation;
                            component1.m_BoneIndex = new int3(0, -1, -1);
                            Citizen citizenData = new Citizen();
                            if (random.NextBool())
                                citizenData.m_State |= CitizenFlags.Male;
                            if (driver)
                                citizenData.SetAge(CitizenAge.Adult);
                            else
                                citizenData.SetAge((CitizenAge)random.NextInt(4));
                            citizenData.m_PseudoRandom = (ushort)(random.NextUInt() % 65536U);
                            PseudoRandomSeed randomSeed;

                            Entity entity1 = ObjectEmergeSystem.SelectResidentPrefab(citizenData, this.m_CreaturePrefabChunks, this.m_EntityType, ref this.m_CreatureDataType, ref this.m_ResidentDataType, out CreatureData _, out randomSeed);

                            ObjectData objectData = this.m_PrefabObjectData[entity1];
                            PrefabRef component2 = new PrefabRef()
                            {
                                m_Prefab = entity1
                            };
                            Game.Creatures.Resident component3 = new Game.Creatures.Resident();
                            component3.m_Flags |= ResidentFlags.InVehicle | ResidentFlags.DummyTraffic;
                            CurrentVehicle component4 = new CurrentVehicle();
                            component4.m_Vehicle = vehicleEntity;
                            component4.m_Flags |= CreatureVehicleFlags.Ready;
                            if (driver && index == num2)
                                component4.m_Flags |= CreatureVehicleFlags.Leader | CreatureVehicleFlags.Driver;
                            Entity entity2 = this.m_CommandBuffer.CreateEntity(jobIndex, objectData.m_Archetype);
                            this.m_CommandBuffer.RemoveComponent(jobIndex, entity2, in this.m_CurrentLaneTypesRelative);
                            this.m_CommandBuffer.SetComponent<Transform>(jobIndex, entity2, transform);
                            this.m_CommandBuffer.SetComponent<PrefabRef>(jobIndex, entity2, component2);
                            this.m_CommandBuffer.SetComponent<Game.Creatures.Resident>(jobIndex, entity2, component3);
                            this.m_CommandBuffer.SetComponent<PseudoRandomSeed>(jobIndex, entity2, randomSeed);
                            this.m_CommandBuffer.AddComponent<CurrentVehicle>(jobIndex, entity2, component4);
                            this.m_CommandBuffer.AddComponent<Relative>(jobIndex, entity2, component1);
                            ++passengers;
                        }
                    }
                }
                return passengers;
            }

            private Resource GetRandomResource(ref Unity.Mathematics.Random random)
            {
                switch (random.NextInt(31 /*0x1F*/))
                {
                    case 0:
                        return Resource.Grain;
                    case 1:
                        return Resource.ConvenienceFood;
                    case 2:
                        return Resource.Food;
                    case 3:
                        return Resource.Vegetables;
                    case 4:
                        return Resource.Meals;
                    case 5:
                        return Resource.Wood;
                    case 6:
                        return Resource.Timber;
                    case 7:
                        return Resource.Paper;
                    case 8:
                        return Resource.Furniture;
                    case 9:
                        return Resource.Vehicles;
                    case 10:
                        return Resource.UnsortedMail;
                    case 11:
                        return Resource.Oil;
                    case 12:
                        return Resource.Petrochemicals;
                    case 13:
                        return Resource.Ore;
                    case 14:
                        return Resource.Plastics;
                    case 15:
                        return Resource.Metals;
                    case 16:
                        return Resource.Electronics;
                    case 17:
                        return Resource.Coal;
                    case 18:
                        return Resource.Stone;
                    case 19:
                        return Resource.Livestock;
                    case 20:
                        return Resource.Cotton;
                    case 21:
                        return Resource.Steel;
                    case 22:
                        return Resource.Minerals;
                    case 23:
                        return Resource.Concrete;
                    case 24:
                        return Resource.Machinery;
                    case 25:
                        return Resource.Chemicals;
                    case 26:
                        return Resource.Pharmaceuticals;
                    case 27:
                        return Resource.Beverages;
                    case 28:
                        return Resource.Textiles;
                    case 29:
                        return Resource.Garbage;
                    case 30:
                        return Resource.Fish;
                    default:
                        return Resource.NoResource;
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
            [ReadOnly]
            public ComponentTypeHandle<Game.Buildings.TrafficSpawner> __Game_Buildings_TrafficSpawner_RO_ComponentTypeHandle;
            [ReadOnly]
            public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;
            [ReadOnly]
            public ComponentTypeHandle<CreatureData> __Game_Prefabs_CreatureData_RO_ComponentTypeHandle;
            [ReadOnly]
            public ComponentTypeHandle<ResidentData> __Game_Prefabs_ResidentData_RO_ComponentTypeHandle;
            public BufferTypeHandle<ServiceDispatch> __Game_Simulation_ServiceDispatch_RW_BufferTypeHandle;
            [ReadOnly]
            public ComponentLookup<TrafficSpawnerData> __Game_Prefabs_TrafficSpawnerData_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<DeliveryTruckData> __Game_Prefabs_DeliveryTruckData_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<ObjectData> __Game_Prefabs_ObjectData_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<RandomTrafficRequest> __Game_Simulation_RandomTrafficRequest_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<ServiceRequest> __Game_Simulation_ServiceRequest_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<PathInformation> __Game_Pathfind_PathInformation_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;
            [ReadOnly]
            public BufferLookup<PathElement> __Game_Pathfind_PathElement_RO_BufferLookup;
            [ReadOnly]
            public BufferLookup<ActivityLocationElement> __Game_Prefabs_ActivityLocationElement_RO_BufferLookup;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void __AssignHandles(ref SystemState state)
            {
                this.__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
                this.__Game_Buildings_TrafficSpawner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.TrafficSpawner>(true);
                this.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(true);
                this.__Game_Prefabs_CreatureData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CreatureData>(true);
                this.__Game_Prefabs_ResidentData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ResidentData>(true);
                this.__Game_Simulation_ServiceDispatch_RW_BufferTypeHandle = state.GetBufferTypeHandle<ServiceDispatch>();
                this.__Game_Prefabs_TrafficSpawnerData_RO_ComponentLookup = state.GetComponentLookup<TrafficSpawnerData>(true);
                this.__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(true);
                this.__Game_Prefabs_DeliveryTruckData_RO_ComponentLookup = state.GetComponentLookup<DeliveryTruckData>(true);
                this.__Game_Prefabs_ObjectData_RO_ComponentLookup = state.GetComponentLookup<ObjectData>(true);
                this.__Game_Simulation_RandomTrafficRequest_RO_ComponentLookup = state.GetComponentLookup<RandomTrafficRequest>(true);
                this.__Game_Simulation_ServiceRequest_RO_ComponentLookup = state.GetComponentLookup<ServiceRequest>(true);
                this.__Game_Pathfind_PathInformation_RO_ComponentLookup = state.GetComponentLookup<PathInformation>(true);
                this.__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(true);
                this.__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(true);
                this.__Game_Pathfind_PathElement_RO_BufferLookup = state.GetBufferLookup<PathElement>(true);
                this.__Game_Prefabs_ActivityLocationElement_RO_BufferLookup = state.GetBufferLookup<ActivityLocationElement>(true);
            }
        }
    }
}
