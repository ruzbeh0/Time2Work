using Game;
using Game.Areas;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Creatures;
using Game.Debug;
using Game.Economy;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Routes;
using Game.Simulation;
using Game.Tools;
using Game.Vehicles;
using System.Runtime.CompilerServices;
using Time2Work.Components;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

#nullable disable
namespace Time2Work.Systems;

[CompilerGenerated]
public partial class Time2WorkTripNeededSystem : GameSystemBase
{
    private const int UPDATE_INTERVAL = 16 /*0x10*/;
    private EntityQuery m_CitizenGroup;
    private EntityQuery m_ResidentPrefabGroup;
    private EntityQuery m_CompanyGroup;
    private EntityArchetype m_HandleRequestArchetype;
    private EntityArchetype m_ResetTripArchetype;
    private ComponentTypeSet m_HumanSpawnTypes;
    private ComponentTypeSet m_PathfindTypes;
    private ComponentTypeSet m_CurrentLaneTypesRelative;
    private EndFrameBarrier m_EndFrameBarrier;
    private Time2WorkTimeSystem m_TimeSystem;
    private PathfindSetupSystem m_PathfindSetupSystem;
    private CityConfigurationSystem m_CityConfigurationSystem;
    private VehicleCapacitySystem m_VehicleCapacitySystem;
    [DebugWatchValue]
    private DebugWatchDistribution m_DebugPathCostsCar;
    [DebugWatchValue]
    private DebugWatchDistribution m_DebugPathCostsPublic;
    [DebugWatchValue]
    private DebugWatchDistribution m_DebugPathCostsPedestrian;
    [DebugWatchValue]
    private DebugWatchDistribution m_DebugPathCostsCarShort;
    [DebugWatchValue]
    private DebugWatchDistribution m_DebugPathCostsPublicShort;
    [DebugWatchValue]
    private DebugWatchDistribution m_DebugPathCostsPedestrianShort;
    [DebugWatchValue]
    private DebugWatchDistribution m_DebugPublicTransportDuration;
    [DebugWatchValue]
    private DebugWatchDistribution m_DebugTaxiDuration;
    [DebugWatchValue]
    private DebugWatchDistribution m_DebugPedestrianDuration;
    [DebugWatchValue]
    private DebugWatchDistribution m_DebugCarDuration;
    [DebugWatchValue]
    private DebugWatchDistribution m_DebugPedestrianDurationShort;
    private Time2WorkTripNeededSystem.TypeHandle __TypeHandle;

    public override int GetUpdateInterval(SystemUpdatePhase phase) => 16 /*0x10*/;

    public bool debugDisableSpawning { get; set; }

    [UnityEngine.Scripting.Preserve]
    protected override void OnCreate()
    {
        base.OnCreate();
        this.m_DebugPathCostsCar = new DebugWatchDistribution(true);
        this.m_DebugPathCostsPublic = new DebugWatchDistribution(true);
        this.m_DebugPathCostsPedestrian = new DebugWatchDistribution(true);
        this.m_DebugPathCostsCarShort = new DebugWatchDistribution(true);
        this.m_DebugPathCostsPublicShort = new DebugWatchDistribution(true);
        this.m_DebugPathCostsPedestrianShort = new DebugWatchDistribution(true);
        this.m_DebugPublicTransportDuration = new DebugWatchDistribution(true);
        this.m_DebugTaxiDuration = new DebugWatchDistribution(true);
        this.m_DebugPedestrianDuration = new DebugWatchDistribution(true);
        this.m_DebugCarDuration = new DebugWatchDistribution(true);
        this.m_DebugPedestrianDurationShort = new DebugWatchDistribution(true);
        this.m_EndFrameBarrier = this.World.GetOrCreateSystemManaged<EndFrameBarrier>();
        this.m_TimeSystem = this.World.GetOrCreateSystemManaged<Time2WorkTimeSystem>();
        this.m_CityConfigurationSystem = this.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
        this.m_VehicleCapacitySystem = this.World.GetOrCreateSystemManaged<VehicleCapacitySystem>();
        this.m_CitizenGroup = this.GetEntityQuery(ComponentType.ReadOnly<Citizen>(), ComponentType.ReadOnly<HouseholdMember>(), ComponentType.ReadWrite<TripNeeded>(), ComponentType.Exclude<TravelPurpose>(), ComponentType.ReadOnly<CurrentBuilding>(), ComponentType.Exclude<ResourceBuyer>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
        this.m_ResidentPrefabGroup = this.GetEntityQuery(ComponentType.ReadOnly<ObjectData>(), ComponentType.ReadOnly<HumanData>(), ComponentType.ReadOnly<ResidentData>(), ComponentType.ReadOnly<PrefabData>());
        this.m_CompanyGroup = this.GetEntityQuery(ComponentType.ReadWrite<TripNeeded>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadWrite<Game.Economy.Resources>(), ComponentType.ReadOnly<OwnedVehicle>(), ComponentType.ReadOnly<TruckSchedule>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
        this.m_HandleRequestArchetype = this.EntityManager.CreateArchetype(ComponentType.ReadWrite<HandleRequest>(), ComponentType.ReadWrite<Game.Events.Event>());
        this.m_ResetTripArchetype = this.EntityManager.CreateArchetype(ComponentType.ReadWrite<Game.Common.Event>(), ComponentType.ReadWrite<ResetTrip>());
        this.m_HumanSpawnTypes = new ComponentTypeSet(ComponentType.ReadWrite<HumanCurrentLane>(), ComponentType.ReadWrite<TripSource>(), ComponentType.ReadWrite<Unspawned>());
        this.m_PathfindTypes = new ComponentTypeSet(ComponentType.ReadWrite<PathInformation>(), ComponentType.ReadWrite<PathElement>());
        this.m_CurrentLaneTypesRelative = new ComponentTypeSet(new ComponentType[5]
        {
      ComponentType.ReadWrite<Moving>(),
      ComponentType.ReadWrite<TransformFrame>(),
      ComponentType.ReadWrite<HumanNavigation>(),
      ComponentType.ReadWrite<HumanCurrentLane>(),
      ComponentType.ReadWrite<Blocker>()
        });
        this.m_PathfindSetupSystem = this.World.GetOrCreateSystemManaged<PathfindSetupSystem>();
        this.RequireAnyForUpdate(this.m_CitizenGroup, this.m_CompanyGroup);
    }

    [UnityEngine.Scripting.Preserve]
    protected override void OnDestroy()
    {
        this.m_DebugPathCostsCar.Dispose();
        this.m_DebugPathCostsPublic.Dispose();
        this.m_DebugPathCostsPedestrian.Dispose();
        this.m_DebugPathCostsCarShort.Dispose();
        this.m_DebugPathCostsPublicShort.Dispose();
        this.m_DebugPathCostsPedestrianShort.Dispose();
        this.m_DebugPublicTransportDuration.Dispose();
        this.m_DebugTaxiDuration.Dispose();
        this.m_DebugCarDuration.Dispose();
        this.m_DebugPedestrianDuration.Dispose();
        this.m_DebugPedestrianDurationShort.Dispose();
        base.OnDestroy();
    }

    [UnityEngine.Scripting.Preserve]
    protected override void OnUpdate()
    {
        JobHandle outJobHandle;
        NativeList<ArchetypeChunk> archetypeChunkListAsync = this.m_ResidentPrefabGroup.ToArchetypeChunkListAsync((AllocatorManager.AllocatorHandle)Allocator.TempJob, out outJobHandle);
        JobHandle jobHandle1 = JobHandle.CombineDependencies(this.Dependency, outJobHandle);
        JobHandle jobHandle2 = new JobHandle();
        // ISSUE: reference to a compiler-generated field
        if (!this.m_CitizenGroup.IsEmptyIgnoreFilter)
        {
            NativeQueue<Time2WorkTripNeededSystem.AnimalTargetInfo> nativeQueue1 = new NativeQueue<Time2WorkTripNeededSystem.AnimalTargetInfo>((AllocatorManager.AllocatorHandle)Allocator.TempJob);
            NativeQueue<Entity> nativeQueue2 = new NativeQueue<Entity>((AllocatorManager.AllocatorHandle)Allocator.TempJob);
            NativeQueue<int> nativeQueue3 = new NativeQueue<int>();
            NativeQueue<int> nativeQueue4 = new NativeQueue<int>();
            NativeQueue<int> nativeQueue5 = new NativeQueue<int>();
            NativeQueue<int> nativeQueue6 = new NativeQueue<int>();
            NativeQueue<int> nativeQueue7 = new NativeQueue<int>();
            NativeQueue<int> nativeQueue8 = new NativeQueue<int>();
            NativeQueue<int> nativeQueue9 = new NativeQueue<int>();
            NativeQueue<int> nativeQueue10 = new NativeQueue<int>();
            NativeQueue<int> nativeQueue11 = new NativeQueue<int>();
            NativeQueue<int> nativeQueue12 = new NativeQueue<int>();
            NativeQueue<int> nativeQueue13 = new NativeQueue<int>();
            JobHandle deps = new JobHandle();
            if (this.m_DebugPathCostsCar.IsEnabled)
            {
                nativeQueue3 = this.m_DebugPathCostsCar.GetQueue(false, out deps);
                deps.Complete();
            }
            if (this.m_DebugPathCostsPublic.IsEnabled)
            {
                nativeQueue4 = this.m_DebugPathCostsPublic.GetQueue(false, out deps);
                deps.Complete();
            }
            if (this.m_DebugPathCostsPedestrian.IsEnabled)
            {
                nativeQueue5 = this.m_DebugPathCostsPedestrian.GetQueue(false, out deps);
                deps.Complete();
            }
            if (this.m_DebugPathCostsCarShort.IsEnabled)
            {
                nativeQueue6 = this.m_DebugPathCostsCarShort.GetQueue(false, out deps);
                deps.Complete();
            }
            if (this.m_DebugPathCostsPublicShort.IsEnabled)
            {
                nativeQueue7 = this.m_DebugPathCostsPublicShort.GetQueue(false, out deps);
                deps.Complete();
            }
            if (this.m_DebugPathCostsPedestrianShort.IsEnabled)
            {
                nativeQueue8 = this.m_DebugPathCostsPedestrianShort.GetQueue(false, out deps);
                deps.Complete();
            }
            if (this.m_DebugPublicTransportDuration.IsEnabled)
            {
                nativeQueue9 = this.m_DebugPublicTransportDuration.GetQueue(false, out deps);
                deps.Complete();
            }
            if (this.m_DebugTaxiDuration.IsEnabled)
            {
                nativeQueue10 = this.m_DebugTaxiDuration.GetQueue(false, out deps);
                deps.Complete();
            }
            if (this.m_DebugCarDuration.IsEnabled)
            {
                nativeQueue11 = this.m_DebugCarDuration.GetQueue(false, out deps);
                deps.Complete();
            }
            if (this.m_DebugPedestrianDuration.IsEnabled)
            {
                nativeQueue12 = this.m_DebugPedestrianDuration.GetQueue(false, out deps);
                deps.Complete();
            }
            if (this.m_DebugPedestrianDurationShort.IsEnabled)
            {
                nativeQueue13 = this.m_DebugPedestrianDurationShort.GetQueue(false, out deps);
                deps.Complete();
            }

            Time2WorkTripNeededSystem.CitizenJob jobData1 = new Time2WorkTripNeededSystem.CitizenJob()
            {
                m_DebugPathQueueCar = nativeQueue3,
                m_DebugPathQueuePublic = nativeQueue4,
                m_DebugPathQueuePedestrian = nativeQueue5,
                m_DebugPathQueueCarShort = nativeQueue6,
                m_DebugPathQueuePublicShort = nativeQueue7,
                m_DebugPathQueuePedestrianShort = nativeQueue8,
                m_DebugPublicTransportDuration = nativeQueue9,
                m_DebugTaxiDuration = nativeQueue10,
                m_DebugCarDuration = nativeQueue11,
                m_DebugPedestrianDuration = nativeQueue12,
                m_DebugPedestrianDurationShort = nativeQueue13,
                m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref this.__TypeHandle.__Unity_Entities_Entity_TypeHandle, ref this.CheckedStateRef),
                m_CitizenType = InternalCompilerInterface.GetComponentTypeHandle<Citizen>(ref this.__TypeHandle.__Game_Citizens_Citizen_RO_ComponentTypeHandle, ref this.CheckedStateRef),
                m_HealthProblemType = InternalCompilerInterface.GetComponentTypeHandle<HealthProblem>(ref this.__TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentTypeHandle, ref this.CheckedStateRef),
                m_HouseholdMemberType = InternalCompilerInterface.GetComponentTypeHandle<HouseholdMember>(ref this.__TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentTypeHandle, ref this.CheckedStateRef),
                m_MailSenderType = InternalCompilerInterface.GetComponentTypeHandle<MailSender>(ref this.__TypeHandle.__Game_Citizens_MailSender_RO_ComponentTypeHandle, ref this.CheckedStateRef),
                m_CurrentTransportType = InternalCompilerInterface.GetComponentTypeHandle<CurrentTransport>(ref this.__TypeHandle.__Game_Citizens_CurrentTransport_RO_ComponentTypeHandle, ref this.CheckedStateRef),
                m_CurrentBuildingType = InternalCompilerInterface.GetComponentTypeHandle<CurrentBuilding>(ref this.__TypeHandle.__Game_Citizens_CurrentBuilding_RW_ComponentTypeHandle, ref this.CheckedStateRef),
                m_TripNeededType = InternalCompilerInterface.GetBufferTypeHandle<TripNeeded>(ref this.__TypeHandle.__Game_Citizens_TripNeeded_RW_BufferTypeHandle, ref this.CheckedStateRef),
                m_AttendingMeetingType = InternalCompilerInterface.GetComponentTypeHandle<AttendingMeeting>(ref this.__TypeHandle.__Game_Citizens_AttendingMeeting_RO_ComponentTypeHandle, ref this.CheckedStateRef),
                m_CreatureDataType = InternalCompilerInterface.GetComponentTypeHandle<CreatureData>(ref this.__TypeHandle.__Game_Prefabs_CreatureData_RO_ComponentTypeHandle, ref this.CheckedStateRef),
                m_ResidentDataType = InternalCompilerInterface.GetComponentTypeHandle<ResidentData>(ref this.__TypeHandle.__Game_Prefabs_ResidentData_RO_ComponentTypeHandle, ref this.CheckedStateRef),
                m_ParkedCarData = InternalCompilerInterface.GetComponentLookup<ParkedCar>(ref this.__TypeHandle.__Game_Vehicles_ParkedCar_RO_ComponentLookup, ref this.CheckedStateRef),
                m_PersonalCarData = InternalCompilerInterface.GetComponentLookup<Game.Vehicles.PersonalCar>(ref this.__TypeHandle.__Game_Vehicles_PersonalCar_RO_ComponentLookup, ref this.CheckedStateRef),
                m_AmbulanceData = InternalCompilerInterface.GetComponentLookup<Game.Vehicles.Ambulance>(ref this.__TypeHandle.__Game_Vehicles_Ambulance_RO_ComponentLookup, ref this.CheckedStateRef),
                m_CurrentDistrictData = InternalCompilerInterface.GetComponentLookup<CurrentDistrict>(ref this.__TypeHandle.__Game_Areas_CurrentDistrict_RO_ComponentLookup, ref this.CheckedStateRef),
                m_PathInfos = InternalCompilerInterface.GetComponentLookup<PathInformation>(ref this.__TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentLookup, ref this.CheckedStateRef),
                m_Properties = InternalCompilerInterface.GetComponentLookup<PropertyRenter>(ref this.__TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup, ref this.CheckedStateRef),
                m_Transforms = InternalCompilerInterface.GetComponentLookup<Game.Objects.Transform>(ref this.__TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref this.CheckedStateRef),
                m_Targets = InternalCompilerInterface.GetComponentLookup<Game.Common.Target>(ref this.__TypeHandle.__Game_Common_Target_RO_ComponentLookup, ref this.CheckedStateRef),
                m_Deleteds = InternalCompilerInterface.GetComponentLookup<Deleted>(ref this.__TypeHandle.__Game_Common_Deleted_RO_ComponentLookup, ref this.CheckedStateRef),
                m_PathElements = InternalCompilerInterface.GetBufferLookup<PathElement>(ref this.__TypeHandle.__Game_Pathfind_PathElement_RO_BufferLookup, ref this.CheckedStateRef),
                m_CarKeepers = InternalCompilerInterface.GetComponentLookup<CarKeeper>(ref this.__TypeHandle.__Game_Citizens_CarKeeper_RO_ComponentLookup, ref this.CheckedStateRef),
                m_PropertyRenters = InternalCompilerInterface.GetComponentLookup<PropertyRenter>(ref this.__TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup, ref this.CheckedStateRef),
                m_Workers = InternalCompilerInterface.GetComponentLookup<Worker>(ref this.__TypeHandle.__Game_Citizens_Worker_RW_ComponentLookup, ref this.CheckedStateRef),
                m_Students = InternalCompilerInterface.GetComponentLookup<Game.Citizens.Student>(ref this.__TypeHandle.__Game_Citizens_Student_RW_ComponentLookup, ref this.CheckedStateRef),
                m_ObjectDatas = InternalCompilerInterface.GetComponentLookup<ObjectData>(ref this.__TypeHandle.__Game_Prefabs_ObjectData_RO_ComponentLookup, ref this.CheckedStateRef),
                m_PrefabRefData = InternalCompilerInterface.GetComponentLookup<PrefabRef>(ref this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref this.CheckedStateRef),
                m_ObjectGeometryData = InternalCompilerInterface.GetComponentLookup<ObjectGeometryData>(ref this.__TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref this.CheckedStateRef),
                m_PrefabCarData = InternalCompilerInterface.GetComponentLookup<CarData>(ref this.__TypeHandle.__Game_Prefabs_CarData_RO_ComponentLookup, ref this.CheckedStateRef),
                m_PrefabHumanData = InternalCompilerInterface.GetComponentLookup<HumanData>(ref this.__TypeHandle.__Game_Prefabs_HumanData_RO_ComponentLookup, ref this.CheckedStateRef),
                m_OutsideConnections = InternalCompilerInterface.GetComponentLookup<Game.Objects.OutsideConnection>(ref this.__TypeHandle.__Game_Objects_OutsideConnection_RO_ComponentLookup, ref this.CheckedStateRef),
                m_UnderConstructionData = InternalCompilerInterface.GetComponentLookup<UnderConstruction>(ref this.__TypeHandle.__Game_Objects_UnderConstruction_RO_ComponentLookup, ref this.CheckedStateRef),
                m_Meetings = InternalCompilerInterface.GetComponentLookup<CoordinatedMeeting>(ref this.__TypeHandle.__Game_Citizens_CoordinatedMeeting_RW_ComponentLookup, ref this.CheckedStateRef),
                m_Attendees = InternalCompilerInterface.GetBufferLookup<CoordinatedMeetingAttendee>(ref this.__TypeHandle.__Game_Citizens_CoordinatedMeetingAttendee_RO_BufferLookup, ref this.CheckedStateRef),
                m_HouseholdAnimals = InternalCompilerInterface.GetBufferLookup<HouseholdAnimal>(ref this.__TypeHandle.__Game_Citizens_HouseholdAnimal_RO_BufferLookup, ref this.CheckedStateRef),
                m_TravelPurposes = InternalCompilerInterface.GetComponentLookup<TravelPurpose>(ref this.__TypeHandle.__Game_Citizens_TravelPurpose_RO_ComponentLookup, ref this.CheckedStateRef),
                m_HaveCoordinatedMeetingDatas = InternalCompilerInterface.GetBufferLookup<HaveCoordinatedMeetingData>(ref this.__TypeHandle.__Game_Prefabs_HaveCoordinatedMeetingData_RO_BufferLookup, ref this.CheckedStateRef),
                m_Households = InternalCompilerInterface.GetComponentLookup<Household>(ref this.__TypeHandle.__Game_Citizens_Household_RO_ComponentLookup, ref this.CheckedStateRef),
                m_HouseholdCitizens = InternalCompilerInterface.GetBufferLookup<HouseholdCitizen>(ref this.__TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup, ref this.CheckedStateRef),
                m_OwnedVehicles = InternalCompilerInterface.GetBufferLookup<OwnedVehicle>(ref this.__TypeHandle.__Game_Vehicles_OwnedVehicle_RO_BufferLookup, ref this.CheckedStateRef),
                m_HumanChunks = archetypeChunkListAsync,
                m_RandomSeed = RandomSeed.Next(),
                m_TimeOfDay = this.m_TimeSystem.normalizedTime,
                m_ResetTripArchetype = this.m_ResetTripArchetype,
                m_HumanSpawnTypes = this.m_HumanSpawnTypes,
                m_PathfindTypes = this.m_PathfindTypes,
                m_PathQueue = this.m_PathfindSetupSystem.GetQueue((object)this, 80 /*0x50*/, 16 /*0x10*/).AsParallelWriter(),
                m_AnimalQueue = nativeQueue1.AsParallelWriter(),
                m_LeaveQueue = nativeQueue2.AsParallelWriter(),
                m_CommandBuffer = this.m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
                m_DebugDisableSpawning = this.debugDisableSpawning
            };

            Time2WorkTripNeededSystem.PetTargetJob jobData2 = new Time2WorkTripNeededSystem.PetTargetJob()
            {
                m_CurrentBuildingData = InternalCompilerInterface.GetComponentLookup<CurrentBuilding>(ref this.__TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentLookup, ref this.CheckedStateRef),
                m_AnimalQueue = nativeQueue1,
                m_CommandBuffer = this.m_EndFrameBarrier.CreateCommandBuffer()
            };

            Time2WorkTripNeededSystem.CitizeLeaveJob jobData3 = new Time2WorkTripNeededSystem.CitizeLeaveJob()
            {
                m_CurrentBuildingData = InternalCompilerInterface.GetComponentLookup<CurrentBuilding>(ref this.__TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentLookup, ref this.CheckedStateRef),
                m_CitizenPresenceData = InternalCompilerInterface.GetComponentLookup<CitizenPresence>(ref this.__TypeHandle.__Game_Buildings_CitizenPresence_RW_ComponentLookup, ref this.CheckedStateRef),
                m_LeaveQueue = nativeQueue2
            };
            jobHandle1 = jobData1.ScheduleParallel<Time2WorkTripNeededSystem.CitizenJob>(this.m_CitizenGroup, jobHandle1);
            JobHandle jobHandle3 = jobData2.Schedule<Time2WorkTripNeededSystem.PetTargetJob>(jobHandle1);
            JobHandle dependsOn = jobHandle1;
            JobHandle jobHandle4 = jobData3.Schedule<Time2WorkTripNeededSystem.CitizeLeaveJob>(dependsOn);
            jobHandle2 = JobHandle.CombineDependencies(jobHandle3, jobHandle4);
            nativeQueue1.Dispose(jobHandle3);
            nativeQueue2.Dispose(jobHandle4);
            this.m_PathfindSetupSystem.AddQueueWriter(jobHandle1);
            this.m_EndFrameBarrier.AddJobHandleForProducer(jobHandle2);
        }
        if (!this.m_CompanyGroup.IsEmptyIgnoreFilter)
        {
            jobHandle1 = new Time2WorkTripNeededSystem.CompanyJob()
            {
                m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref this.__TypeHandle.__Unity_Entities_Entity_TypeHandle, ref this.CheckedStateRef),
                m_PropertyRenterType = InternalCompilerInterface.GetComponentTypeHandle<PropertyRenter>(ref this.__TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentTypeHandle, ref this.CheckedStateRef),
                m_CreatureDataType = InternalCompilerInterface.GetComponentTypeHandle<CreatureData>(ref this.__TypeHandle.__Game_Prefabs_CreatureData_RO_ComponentTypeHandle, ref this.CheckedStateRef),
                m_ResidentDataType = InternalCompilerInterface.GetComponentTypeHandle<ResidentData>(ref this.__TypeHandle.__Game_Prefabs_ResidentData_RO_ComponentTypeHandle, ref this.CheckedStateRef),
                m_PrefabType = InternalCompilerInterface.GetComponentTypeHandle<PrefabRef>(ref this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref this.CheckedStateRef),
                m_TripNeededType = InternalCompilerInterface.GetBufferTypeHandle<TripNeeded>(ref this.__TypeHandle.__Game_Citizens_TripNeeded_RW_BufferTypeHandle, ref this.CheckedStateRef),
                m_VehicleType = InternalCompilerInterface.GetBufferTypeHandle<OwnedVehicle>(ref this.__TypeHandle.__Game_Vehicles_OwnedVehicle_RO_BufferTypeHandle, ref this.CheckedStateRef),
                m_ResourceType = InternalCompilerInterface.GetBufferTypeHandle<Game.Economy.Resources>(ref this.__TypeHandle.__Game_Economy_Resources_RW_BufferTypeHandle, ref this.CheckedStateRef),
                m_PrefabDeliveryTruckData = InternalCompilerInterface.GetComponentLookup<DeliveryTruckData>(ref this.__TypeHandle.__Game_Prefabs_DeliveryTruckData_RO_ComponentLookup, ref this.CheckedStateRef),
                m_PrefabObjectData = InternalCompilerInterface.GetComponentLookup<ObjectData>(ref this.__TypeHandle.__Game_Prefabs_ObjectData_RO_ComponentLookup, ref this.CheckedStateRef),
                m_Prefabs = InternalCompilerInterface.GetComponentLookup<PrefabRef>(ref this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref this.CheckedStateRef),
                m_Transforms = InternalCompilerInterface.GetComponentLookup<Game.Objects.Transform>(ref this.__TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref this.CheckedStateRef),
                m_TransportCompanyDatas = InternalCompilerInterface.GetComponentLookup<TransportCompanyData>(ref this.__TypeHandle.__Game_Companies_TransportCompanyData_RO_ComponentLookup, ref this.CheckedStateRef),
                m_ServiceRequestData = InternalCompilerInterface.GetComponentLookup<ServiceRequest>(ref this.__TypeHandle.__Game_Simulation_ServiceRequest_RO_ComponentLookup, ref this.CheckedStateRef),
                m_PathInformationData = InternalCompilerInterface.GetComponentLookup<PathInformation>(ref this.__TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentLookup, ref this.CheckedStateRef),
                m_UnderConstructionData = InternalCompilerInterface.GetComponentLookup<UnderConstruction>(ref this.__TypeHandle.__Game_Objects_UnderConstruction_RO_ComponentLookup, ref this.CheckedStateRef),
                m_PropertyRenterData = InternalCompilerInterface.GetComponentLookup<PropertyRenter>(ref this.__TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup, ref this.CheckedStateRef),
                m_PathElements = InternalCompilerInterface.GetBufferLookup<PathElement>(ref this.__TypeHandle.__Game_Pathfind_PathElement_RO_BufferLookup, ref this.CheckedStateRef),
                m_ActivityLocationElements = InternalCompilerInterface.GetBufferLookup<ActivityLocationElement>(ref this.__TypeHandle.__Game_Prefabs_ActivityLocationElement_RO_BufferLookup, ref this.CheckedStateRef),
                TruckSchedules = InternalCompilerInterface.GetComponentTypeHandle<TruckSchedule>(ref this.__TypeHandle.__Game_Vehicles_TruckSchedule_RW_ComponentLookup, ref this.CheckedStateRef),
                m_HumanChunks = archetypeChunkListAsync,
                m_LeftHandTraffic = this.m_CityConfigurationSystem.leftHandTraffic,
                m_RandomSeed = RandomSeed.Next(),
                m_HandleRequestArchetype = this.m_HandleRequestArchetype,
                m_DeliveryTruckSelectData = this.m_VehicleCapacitySystem.GetDeliveryTruckSelectData(),
                m_CurrentLaneTypesRelative = this.m_CurrentLaneTypesRelative,
                m_CommandBuffer = this.m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
                m_DebugDisableSpawning = this.debugDisableSpawning,
                m_TimeOfDay = this.m_TimeSystem.normalizedTime
            }.ScheduleParallel<Time2WorkTripNeededSystem.CompanyJob>(this.m_CompanyGroup, jobHandle1);
            // ISSUE: reference to a compiler-generated field
            this.m_EndFrameBarrier.AddJobHandleForProducer(jobHandle1);
            jobHandle2 = JobHandle.CombineDependencies(jobHandle2, jobHandle1);
        }
        archetypeChunkListAsync.Dispose(jobHandle1);
        this.Dependency = jobHandle2;
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
    public Time2WorkTripNeededSystem()
    {
    }

    [BurstCompile]
    private struct CompanyJob : IJobChunk
    {
        [ReadOnly]
        public EntityTypeHandle m_EntityType;
        [ReadOnly]
        public ComponentTypeHandle<PrefabRef> m_PrefabType;
        public BufferTypeHandle<TripNeeded> m_TripNeededType;
        [ReadOnly]
        public ComponentTypeHandle<PropertyRenter> m_PropertyRenterType;
        [ReadOnly]
        public ComponentTypeHandle<CreatureData> m_CreatureDataType;
        [ReadOnly]
        public ComponentTypeHandle<ResidentData> m_ResidentDataType;
        [ReadOnly]
        public BufferTypeHandle<OwnedVehicle> m_VehicleType;
        public BufferTypeHandle<Game.Economy.Resources> m_ResourceType;
        [ReadOnly]
        public ComponentLookup<TransportCompanyData> m_TransportCompanyDatas;
        [ReadOnly]
        public ComponentLookup<PrefabRef> m_Prefabs;
        [ReadOnly]
        public ComponentLookup<DeliveryTruckData> m_PrefabDeliveryTruckData;
        [ReadOnly]
        public ComponentLookup<ObjectData> m_PrefabObjectData;
        [ReadOnly]
        public ComponentLookup<Game.Objects.Transform> m_Transforms;
        [ReadOnly]
        public ComponentLookup<ServiceRequest> m_ServiceRequestData;
        [ReadOnly]
        public ComponentLookup<PathInformation> m_PathInformationData;
        [ReadOnly]
        public ComponentLookup<UnderConstruction> m_UnderConstructionData;
        [ReadOnly]
        public ComponentLookup<PropertyRenter> m_PropertyRenterData;
        [ReadOnly]
        public BufferLookup<PathElement> m_PathElements;
        [ReadOnly]
        public BufferLookup<ActivityLocationElement> m_ActivityLocationElements;
        [ReadOnly]
        public NativeList<ArchetypeChunk> m_HumanChunks;
        [ReadOnly]
        public bool m_LeftHandTraffic;
        [ReadOnly]
        public RandomSeed m_RandomSeed;
        [ReadOnly]
        public EntityArchetype m_HandleRequestArchetype;
        [ReadOnly]
        public DeliveryTruckSelectData m_DeliveryTruckSelectData;
        [ReadOnly]
        public ComponentTypeSet m_CurrentLaneTypesRelative;
        public EntityCommandBuffer.ParallelWriter m_CommandBuffer;
        public bool m_DebugDisableSpawning;
        public ComponentTypeHandle<TruckSchedule> TruckSchedules;
        [ReadOnly]
        public float m_TimeOfDay;

        private void SpawnDeliveryTruck(
          int index,
          Entity owner,
          Entity from,
          ref Game.Objects.Transform transform,
          TripNeeded trip,
          TruckSchedule schedule,
          float currentTimeOfDay)
        {
            Entity entity1;
            Entity targetAgent;
            if (this.m_ServiceRequestData.HasComponent(trip.m_TargetAgent))
            {
                PathInformation componentData;
                if (!this.m_PathInformationData.TryGetComponent(trip.m_TargetAgent, out componentData))
                    return;
                entity1 = componentData.m_Destination;
                targetAgent = trip.m_TargetAgent;
            }
            else
            {
                entity1 = trip.m_TargetAgent;
                targetAgent = Entity.Null;
            }
            if (!this.m_Prefabs.HasComponent(entity1))
                return;
            //Check if delivery is in delivery window
            if (currentTimeOfDay < schedule.startTime || currentTimeOfDay > schedule.endTime)
            {
                return; // Outside delivery window
            }
            Entity entity2 = entity1;
            PropertyRenter componentData1;
            if (this.m_PropertyRenterData.TryGetComponent(entity2, out componentData1))
                entity2 = componentData1.m_Property;
            uint num = 0;
            UnderConstruction componentData2;
            if (this.m_UnderConstructionData.TryGetComponent(entity2, out componentData2) && componentData2.m_NewPrefab == Entity.Null)
            {
                PathInformation componentData3;
                this.m_PathInformationData.TryGetComponent(targetAgent, out componentData3);
                num = ObjectUtils.GetTripDelayFrames(componentData2, componentData3);
            }
            if (this.m_UnderConstructionData.TryGetComponent(from, out componentData2) && componentData2.m_NewPrefab == Entity.Null)
                num = math.max(num, ObjectUtils.GetRemainingConstructionFrames(componentData2));
            Unity.Mathematics.Random random = this.m_RandomSeed.GetRandom(owner.Index);
            DeliveryTruckFlags state = (DeliveryTruckFlags)0;
            Resource resource = trip.m_Resource;
            Resource returnResource = Resource.NoResource;
            int amount = math.abs(trip.m_Data);
            int returnAmount = 0;
            Game.Citizens.Purpose purpose = trip.m_Purpose;
            if ((uint)purpose <= 14U)
            {
                switch (purpose)
                {
                    case Game.Citizens.Purpose.Shopping:
                        state |= DeliveryTruckFlags.Buying;
                        break;
                    case Game.Citizens.Purpose.Exporting:
                        state |= DeliveryTruckFlags.Loaded;
                        break;
                    case Game.Citizens.Purpose.StorageTransfer:
                        if (trip.m_Data > 0)
                        {
                            state |= DeliveryTruckFlags.Loaded | DeliveryTruckFlags.StorageTransfer;
                            break;
                        }
                        state |= DeliveryTruckFlags.Buying | DeliveryTruckFlags.StorageTransfer;
                        break;
                    case Game.Citizens.Purpose.Delivery:
                        state |= DeliveryTruckFlags.Loaded | DeliveryTruckFlags.Delivering;
                        break;
                    case Game.Citizens.Purpose.UpkeepDelivery:
                        state |= DeliveryTruckFlags.Loaded | DeliveryTruckFlags.Delivering | DeliveryTruckFlags.NoUnloading;
                        break;
                }
            }
            else if ((uint)purpose <= 29U)
            {
                switch (purpose)
                {
                    case Game.Citizens.Purpose.Collect:
                        state |= DeliveryTruckFlags.Buying;
                        break;
                    case Game.Citizens.Purpose.ReturnUnsortedMail:
                        state |= DeliveryTruckFlags.Loaded;
                        returnResource = Resource.UnsortedMail;
                        returnAmount = amount;
                        amount = math.select(amount, 0, trip.m_Resource == Resource.NoResource);
                        break;
                    case Game.Citizens.Purpose.ReturnLocalMail:
                        state |= DeliveryTruckFlags.Loaded;
                        returnResource = Resource.LocalMail;
                        returnAmount = amount;
                        amount = math.select(amount, 0, trip.m_Resource == Resource.NoResource);
                        break;
                    case Game.Citizens.Purpose.ReturnOutgoingMail:
                        state |= DeliveryTruckFlags.Loaded;
                        returnResource = Resource.OutgoingMail;
                        returnAmount = amount;
                        amount = math.select(amount, 0, trip.m_Resource == Resource.NoResource);
                        break;
                }
            }
            else
            {
                switch (purpose)
                {
                    case Game.Citizens.Purpose.ReturnGarbage:
                        state |= DeliveryTruckFlags.Loaded;
                        returnResource = Resource.Garbage;
                        returnAmount = amount;
                        amount = math.select(amount, 0, trip.m_Resource == Resource.NoResource);
                        break;
                    case Game.Citizens.Purpose.CompanyShopping:
                        state |= DeliveryTruckFlags.Buying | DeliveryTruckFlags.UpdateSellerQuantity;
                        break;
                }
            }
            if (amount > 0)
                state |= DeliveryTruckFlags.UpdateOwnerQuantity;
            Resource resources = resource | returnResource;
            int capacity = math.max(amount, returnAmount);
            DeliveryTruckSelectItem selectItem;
            if (!this.m_DeliveryTruckSelectData.TrySelectItem(ref random, resources, capacity, out selectItem))
                return;

            Entity vehicle = this.m_DeliveryTruckSelectData.CreateVehicle(this.m_CommandBuffer, index, ref random, ref this.m_PrefabDeliveryTruckData, ref this.m_PrefabObjectData, selectItem, resource, returnResource, ref amount, ref returnAmount, transform, from, state, num);
            int maxCount = 1;
            if (this.CreatePassengers(index, vehicle, selectItem.m_Prefab1, transform, true, ref maxCount, ref random) > 0)
            {
                this.m_CommandBuffer.AddBuffer<Passenger>(index, vehicle);
            }
            this.m_CommandBuffer.SetComponent<Game.Common.Target>(index, vehicle, new Game.Common.Target(entity1));
            this.m_CommandBuffer.AddComponent<Owner>(index, vehicle, new Owner(owner));
            if (!(targetAgent != Entity.Null))
                return;
            Entity entity3 = this.m_CommandBuffer.CreateEntity(index, this.m_HandleRequestArchetype);
            this.m_CommandBuffer.SetComponent<HandleRequest>(index, entity3, new HandleRequest(targetAgent, vehicle, true));
            if (!this.m_PathElements.HasBuffer(targetAgent))
                return;
            DynamicBuffer<PathElement> pathElement = this.m_PathElements[targetAgent];
            if (pathElement.Length == 0)
                return;
            DynamicBuffer<PathElement> targetElements = this.m_CommandBuffer.SetBuffer<PathElement>(index, vehicle);
            PathUtils.CopyPath(pathElement, new PathOwner(), 0, targetElements);
            this.m_CommandBuffer.SetComponent<PathOwner>(index, vehicle, new PathOwner(PathFlags.Updated));
            this.m_CommandBuffer.SetComponent<PathInformation>(index, vehicle, this.m_PathInformationData[targetAgent]);

            this.m_CommandBuffer.RemoveComponent<TruckSchedule>(index, owner);
        }

        private int CreatePassengers(
          int jobIndex,
          Entity vehicleEntity,
          Entity vehiclePrefab,
          Game.Objects.Transform transform,
          bool driver,
          ref int maxCount,
          ref Unity.Mathematics.Random random)
        {
            int passengers = 0;
            DynamicBuffer<ActivityLocationElement> bufferData;
            // ISSUE: reference to a compiler-generated field
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
                        // ISSUE: reference to a compiler-generated field
                        // ISSUE: reference to a compiler-generated field
                        bool test = (activityLocationElement.m_ActivityFlags & ActivityFlags.InvertLefthandTraffic) != (ActivityFlags)0 && this.m_LeftHandTraffic || (activityLocationElement.m_ActivityFlags & ActivityFlags.InvertRighthandTraffic) != (ActivityFlags)0 && !this.m_LeftHandTraffic;
                        activityLocationElement.m_Position.x = math.select(activityLocationElement.m_Position.x, -activityLocationElement.m_Position.x, test);
                        // ISSUE: reference to a compiler-generated field
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
                        citizenData.m_PseudoRandom = (ushort)(random.NextUInt() % 65536U /*0x010000*/);
                        PseudoRandomSeed randomSeed;
                        // ISSUE: reference to a compiler-generated field
                        // ISSUE: reference to a compiler-generated field
                        // ISSUE: reference to a compiler-generated field
                        // ISSUE: reference to a compiler-generated field
                        // ISSUE: reference to a compiler-generated method
                        Entity entity1 = ObjectEmergeSystem.SelectResidentPrefab(citizenData, this.m_HumanChunks, this.m_EntityType, ref this.m_CreatureDataType, ref this.m_ResidentDataType, out CreatureData _, out randomSeed);
                        // ISSUE: reference to a compiler-generated field
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
                        // ISSUE: reference to a compiler-generated field
                        Entity entity2 = this.m_CommandBuffer.CreateEntity(jobIndex, objectData.m_Archetype);
                        // ISSUE: reference to a compiler-generated field
                        // ISSUE: reference to a compiler-generated field
                        this.m_CommandBuffer.RemoveComponent(jobIndex, entity2, in this.m_CurrentLaneTypesRelative);
                        // ISSUE: reference to a compiler-generated field
                        this.m_CommandBuffer.SetComponent<Game.Objects.Transform>(jobIndex, entity2, transform);
                        // ISSUE: reference to a compiler-generated field
                        this.m_CommandBuffer.SetComponent<PrefabRef>(jobIndex, entity2, component2);
                        // ISSUE: reference to a compiler-generated field
                        this.m_CommandBuffer.SetComponent<Game.Creatures.Resident>(jobIndex, entity2, component3);
                        // ISSUE: reference to a compiler-generated field
                        this.m_CommandBuffer.SetComponent<PseudoRandomSeed>(jobIndex, entity2, randomSeed);
                        // ISSUE: reference to a compiler-generated field
                        this.m_CommandBuffer.AddComponent<CurrentVehicle>(jobIndex, entity2, component4);
                        // ISSUE: reference to a compiler-generated field
                        this.m_CommandBuffer.AddComponent<Relative>(jobIndex, entity2, component1);
                        ++passengers;
                    }
                }
            }
            return passengers;
        }

        public void Execute(
          in ArchetypeChunk chunk,
          int unfilteredChunkIndex,
          bool useEnabledMask,
          in v128 chunkEnabledMask)
        {
            NativeArray<Entity> nativeArray1 = chunk.GetNativeArray(this.m_EntityType);
            NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray<PrefabRef>(ref this.m_PrefabType);
            NativeArray<TruckSchedule> nativeArray4 = chunk.GetNativeArray<TruckSchedule>(ref this.TruckSchedules);
            BufferAccessor<OwnedVehicle> bufferAccessor1 = chunk.GetBufferAccessor<OwnedVehicle>(ref this.m_VehicleType);
            BufferAccessor<TripNeeded> bufferAccessor2 = chunk.GetBufferAccessor<TripNeeded>(ref this.m_TripNeededType);
            BufferAccessor<Game.Economy.Resources> bufferAccessor3 = chunk.GetBufferAccessor<Game.Economy.Resources>(ref this.m_ResourceType);
            NativeArray<PropertyRenter> nativeArray3 = chunk.GetNativeArray<PropertyRenter>(ref this.m_PropertyRenterType);
            for (int index = 0; index < chunk.Count; ++index)
            {
                Entity prefab = nativeArray2[index].m_Prefab;
                if (!this.m_TransportCompanyDatas.HasComponent(prefab) || bufferAccessor1[index].Length < this.m_TransportCompanyDatas[prefab].m_MaxTransports)
                {
                    Entity owner = nativeArray1[index];
                    DynamicBuffer<TripNeeded> dynamicBuffer1 = bufferAccessor2[index];
                    if (dynamicBuffer1.Length > 0)
                    {
                        TripNeeded trip = dynamicBuffer1[0];
                        dynamicBuffer1.RemoveAt(0);
                        if (!this.m_DebugDisableSpawning)
                        {
                            DynamicBuffer<Game.Economy.Resources> dynamicBuffer2 = bufferAccessor3[index];
                            Entity entity = !chunk.Has<PropertyRenter>(ref this.m_PropertyRenterType) ? owner : nativeArray3[index].m_Property;
                            if (this.m_Transforms.HasComponent(entity))
                            {
                                Game.Objects.Transform transform = this.m_Transforms[entity];
                                this.SpawnDeliveryTruck(unfilteredChunkIndex, owner, entity, ref transform, trip, nativeArray4[index], m_TimeOfDay);
                            }
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
            // ISSUE: reference to a compiler-generated method
            this.Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
        }
    }

    private struct AnimalTargetInfo
    {
        public Entity m_Animal;
        public Entity m_Source;
        public Entity m_Target;
    }

    [BurstCompile]
    private struct PetTargetJob : IJob
    {
        [ReadOnly]
        public ComponentLookup<CurrentBuilding> m_CurrentBuildingData;
        public NativeQueue<Time2WorkTripNeededSystem.AnimalTargetInfo> m_AnimalQueue;
        public EntityCommandBuffer m_CommandBuffer;

        public void Execute()
        {
            // ISSUE: reference to a compiler-generated field
            int count = this.m_AnimalQueue.Count;
            if (count == 0)
                return;
            NativeParallelHashSet<Entity> nativeParallelHashSet = new NativeParallelHashSet<Entity>(count, (AllocatorManager.AllocatorHandle)Allocator.Temp);
            for (int index = 0; index < count; ++index)
            {
                // ISSUE: reference to a compiler-generated field
                // ISSUE: variable of a compiler-generated type
                Time2WorkTripNeededSystem.AnimalTargetInfo animalTargetInfo = this.m_AnimalQueue.Dequeue();
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                if (this.m_CurrentBuildingData.HasComponent(animalTargetInfo.m_Animal) && !(this.m_CurrentBuildingData[animalTargetInfo.m_Animal].m_CurrentBuilding != animalTargetInfo.m_Source) && nativeParallelHashSet.Add(animalTargetInfo.m_Animal))
                {
                    // ISSUE: reference to a compiler-generated field
                    // ISSUE: reference to a compiler-generated field
                    // ISSUE: reference to a compiler-generated field
                    this.m_CommandBuffer.AddComponent<Game.Common.Target>(animalTargetInfo.m_Animal, new Game.Common.Target(animalTargetInfo.m_Target));
                }
            }
            nativeParallelHashSet.Dispose();
        }
    }

    [BurstCompile]
    private struct CitizeLeaveJob : IJob
    {
        [ReadOnly]
        public ComponentLookup<CurrentBuilding> m_CurrentBuildingData;
        public ComponentLookup<CitizenPresence> m_CitizenPresenceData;
        public NativeQueue<Entity> m_LeaveQueue;

        public void Execute()
        {
            Entity entity;
            // ISSUE: reference to a compiler-generated field
            while (this.m_LeaveQueue.TryDequeue(out entity))
            {
                // ISSUE: reference to a compiler-generated field
                if (this.m_CurrentBuildingData.HasComponent(entity))
                {
                    // ISSUE: reference to a compiler-generated field
                    CurrentBuilding currentBuilding = this.m_CurrentBuildingData[entity];
                    // ISSUE: reference to a compiler-generated field
                    if (this.m_CitizenPresenceData.HasComponent(currentBuilding.m_CurrentBuilding))
                    {
                        // ISSUE: reference to a compiler-generated field
                        CitizenPresence citizenPresence = this.m_CitizenPresenceData[currentBuilding.m_CurrentBuilding];
                        citizenPresence.m_Delta = (sbyte)math.max(-127, (int)citizenPresence.m_Delta - 1);
                        // ISSUE: reference to a compiler-generated field
                        this.m_CitizenPresenceData[currentBuilding.m_CurrentBuilding] = citizenPresence;
                    }
                }
            }
        }
    }

    [BurstCompile]
    private struct CitizenJob : IJobChunk
    {
        [NativeDisableContainerSafetyRestriction]
        public NativeQueue<int> m_DebugPathQueueCar;
        [NativeDisableContainerSafetyRestriction]
        public NativeQueue<int> m_DebugPathQueuePublic;
        [NativeDisableContainerSafetyRestriction]
        public NativeQueue<int> m_DebugPathQueuePedestrian;
        [NativeDisableContainerSafetyRestriction]
        public NativeQueue<int> m_DebugPathQueueCarShort;
        [NativeDisableContainerSafetyRestriction]
        public NativeQueue<int> m_DebugPathQueuePublicShort;
        [NativeDisableContainerSafetyRestriction]
        public NativeQueue<int> m_DebugPathQueuePedestrianShort;
        [NativeDisableContainerSafetyRestriction]
        public NativeQueue<int> m_DebugPublicTransportDuration;
        [NativeDisableContainerSafetyRestriction]
        public NativeQueue<int> m_DebugTaxiDuration;
        [NativeDisableContainerSafetyRestriction]
        public NativeQueue<int> m_DebugCarDuration;
        [NativeDisableContainerSafetyRestriction]
        public NativeQueue<int> m_DebugPedestrianDuration;
        [NativeDisableContainerSafetyRestriction]
        public NativeQueue<int> m_DebugPedestrianDurationShort;
        [ReadOnly]
        public EntityTypeHandle m_EntityType;
        public BufferTypeHandle<TripNeeded> m_TripNeededType;
        public ComponentTypeHandle<CurrentBuilding> m_CurrentBuildingType;
        [ReadOnly]
        public ComponentTypeHandle<CurrentTransport> m_CurrentTransportType;
        [ReadOnly]
        public ComponentTypeHandle<HouseholdMember> m_HouseholdMemberType;
        [ReadOnly]
        public ComponentTypeHandle<MailSender> m_MailSenderType;
        [ReadOnly]
        public ComponentTypeHandle<Citizen> m_CitizenType;
        [ReadOnly]
        public ComponentTypeHandle<HealthProblem> m_HealthProblemType;
        [ReadOnly]
        public ComponentTypeHandle<AttendingMeeting> m_AttendingMeetingType;
        [ReadOnly]
        public ComponentTypeHandle<CreatureData> m_CreatureDataType;
        [ReadOnly]
        public ComponentTypeHandle<ResidentData> m_ResidentDataType;
        [ReadOnly]
        public ComponentLookup<PropertyRenter> m_Properties;
        [ReadOnly]
        public ComponentLookup<Game.Objects.Transform> m_Transforms;
        [ReadOnly]
        public ComponentLookup<PrefabRef> m_PrefabRefData;
        [ReadOnly]
        public ComponentLookup<ObjectGeometryData> m_ObjectGeometryData;
        [ReadOnly]
        public ComponentLookup<ObjectData> m_ObjectDatas;
        [ReadOnly]
        public ComponentLookup<CarData> m_PrefabCarData;
        [ReadOnly]
        public ComponentLookup<HumanData> m_PrefabHumanData;
        [ReadOnly]
        public ComponentLookup<PathInformation> m_PathInfos;
        [ReadOnly]
        public ComponentLookup<ParkedCar> m_ParkedCarData;
        [ReadOnly]
        public ComponentLookup<Game.Vehicles.PersonalCar> m_PersonalCarData;
        [ReadOnly]
        public ComponentLookup<Game.Vehicles.Ambulance> m_AmbulanceData;
        [ReadOnly]
        public ComponentLookup<CurrentDistrict> m_CurrentDistrictData;
        [ReadOnly]
        public ComponentLookup<Game.Common.Target> m_Targets;
        [ReadOnly]
        public ComponentLookup<Deleted> m_Deleteds;
        [ReadOnly]
        public BufferLookup<PathElement> m_PathElements;
        [ReadOnly]
        public ComponentLookup<CarKeeper> m_CarKeepers;
        [ReadOnly]
        public ComponentLookup<PropertyRenter> m_PropertyRenters;
        [ReadOnly]
        public ComponentLookup<Game.Objects.OutsideConnection> m_OutsideConnections;
        [ReadOnly]
        public ComponentLookup<UnderConstruction> m_UnderConstructionData;
        [ReadOnly]
        public BufferLookup<CoordinatedMeetingAttendee> m_Attendees;
        [ReadOnly]
        public BufferLookup<HouseholdAnimal> m_HouseholdAnimals;
        [ReadOnly]
        public ComponentLookup<TravelPurpose> m_TravelPurposes;
        [ReadOnly]
        public BufferLookup<HaveCoordinatedMeetingData> m_HaveCoordinatedMeetingDatas;
        [ReadOnly]
        public ComponentLookup<Household> m_Households;
        [ReadOnly]
        public BufferLookup<HouseholdCitizen> m_HouseholdCitizens;
        [ReadOnly]
        public BufferLookup<OwnedVehicle> m_OwnedVehicles;
        [NativeDisableParallelForRestriction]
        public ComponentLookup<CoordinatedMeeting> m_Meetings;
        [NativeDisableParallelForRestriction]
        public ComponentLookup<Worker> m_Workers;
        [NativeDisableParallelForRestriction]
        public ComponentLookup<Game.Citizens.Student> m_Students;
        [ReadOnly]
        public NativeList<ArchetypeChunk> m_HumanChunks;
        [ReadOnly]
        public RandomSeed m_RandomSeed;
        [ReadOnly]
        public float m_TimeOfDay;
        [ReadOnly]
        public EntityArchetype m_ResetTripArchetype;
        [ReadOnly]
        public ComponentTypeSet m_HumanSpawnTypes;
        [ReadOnly]
        public ComponentTypeSet m_PathfindTypes;
        public EntityCommandBuffer.ParallelWriter m_CommandBuffer;
        public NativeQueue<SetupQueueItem>.ParallelWriter m_PathQueue;
        public NativeQueue<Time2WorkTripNeededSystem.AnimalTargetInfo>.ParallelWriter m_AnimalQueue;
        public NativeQueue<Entity>.ParallelWriter m_LeaveQueue;
        public bool m_DebugDisableSpawning;

        private void GetResidentFlags(
          Entity citizen,
          Entity currentBuilding,
          bool isMailSender,
          bool pathFailed,
          ref Game.Common.Target target,
          ref Game.Citizens.Purpose purpose,
          ref Game.Citizens.Purpose divertPurpose,
          ref uint timer,
          ref bool hasDivertPath)
        {
            if (pathFailed)
            {
                divertPurpose = Game.Citizens.Purpose.PathFailed;
            }
            else
            {
                Game.Citizens.Purpose purpose1 = purpose;
                if ((uint)purpose1 <= 15U)
                {
                    switch (purpose1)
                    {
                        case Game.Citizens.Purpose.Hospital:
                            // ISSUE: reference to a compiler-generated field
                            if (!this.m_AmbulanceData.HasComponent(target.m_Target))
                                return;
                            timer = 0U;
                            return;
                        case Game.Citizens.Purpose.Safety:
                            break;
                        default:
                            goto label_11;
                    }
                }
                else
                {
                    switch (purpose1)
                    {
                        case Game.Citizens.Purpose.Escape:
                            break;
                        case Game.Citizens.Purpose.Deathcare:
                            timer = 0U;
                            return;
                        default:
                            goto label_11;
                    }
                }
                target.m_Target = currentBuilding;
                divertPurpose = purpose;
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                purpose = !this.m_TravelPurposes.HasComponent(citizen) ? Game.Citizens.Purpose.None : this.m_TravelPurposes[citizen].m_Purpose;
                timer = 0U;
                hasDivertPath = true;
                return;
            label_11:
                if (!isMailSender)
                    return;
                divertPurpose = Game.Citizens.Purpose.SendMail;
            }
        }

        private Entity SpawnResident(
          int index,
          Entity citizen,
          Entity fromBuilding,
          Citizen citizenData,
          Game.Common.Target target,
          ResidentFlags flags,
          Game.Citizens.Purpose divertPurpose,
          uint timer,
          bool hasDivertPath,
          bool isDead,
          bool isCarried)
        {
            PseudoRandomSeed randomSeed;
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated method
            Entity entity1 = ObjectEmergeSystem.SelectResidentPrefab(citizenData, this.m_HumanChunks, this.m_EntityType, ref this.m_CreatureDataType, ref this.m_ResidentDataType, out CreatureData _, out randomSeed);
            // ISSUE: reference to a compiler-generated field
            ObjectData objectData = this.m_ObjectDatas[entity1];
            PrefabRef component1 = new PrefabRef()
            {
                m_Prefab = entity1
            };
            Game.Objects.Transform component2;
            // ISSUE: reference to a compiler-generated field
            if (this.m_Transforms.HasComponent(fromBuilding))
            {
                // ISSUE: reference to a compiler-generated field
                component2 = this.m_Transforms[fromBuilding];
            }
            else
                component2 = new Game.Objects.Transform()
                {
                    m_Position = new float3(),
                    m_Rotation = new quaternion(0.0f, 0.0f, 0.0f, 1f)
                };
            Game.Creatures.Resident component3 = new Game.Creatures.Resident();
            component3.m_Citizen = citizen;
            component3.m_Flags = flags;
            Human component4 = new Human();
            if (isDead)
                component4.m_Flags |= HumanFlags.Dead;
            if (isCarried)
                component4.m_Flags |= HumanFlags.Carried;
            PathOwner component5 = new PathOwner(PathFlags.Updated);
            TripSource component6 = new TripSource(fromBuilding, timer);
            // ISSUE: reference to a compiler-generated field
            Entity entity2 = this.m_CommandBuffer.CreateEntity(index, objectData.m_Archetype);
            HumanCurrentLane component7 = new HumanCurrentLane();
            DynamicBuffer<PathElement> bufferData;
            // ISSUE: reference to a compiler-generated field
            if (this.m_PathElements.TryGetBuffer(citizen, out bufferData) && bufferData.Length > 0)
            {
                // ISSUE: reference to a compiler-generated field
                DynamicBuffer<PathElement> targetElements = this.m_CommandBuffer.SetBuffer<PathElement>(index, entity2);
                PathUtils.CopyPath(bufferData, new PathOwner(), 0, targetElements);
                component7 = new HumanCurrentLane(bufferData[0], (CreatureLaneFlags)0);
                component5.m_State |= PathFlags.Updated;
            }
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            this.m_CommandBuffer.AddComponent(index, entity2, in this.m_HumanSpawnTypes);
            if (divertPurpose != Game.Citizens.Purpose.None)
            {
                if (hasDivertPath)
                    component5.m_State |= PathFlags.CachedObsolete;
                else
                    component5.m_State |= PathFlags.DivertObsolete;
                // ISSUE: reference to a compiler-generated field
                this.m_CommandBuffer.AddComponent<Divert>(index, entity2, new Divert()
                {
                    m_Purpose = divertPurpose
                });
            }
            // ISSUE: reference to a compiler-generated field
            this.m_CommandBuffer.SetComponent<Game.Objects.Transform>(index, entity2, component2);
            // ISSUE: reference to a compiler-generated field
            this.m_CommandBuffer.SetComponent<PrefabRef>(index, entity2, component1);
            // ISSUE: reference to a compiler-generated field
            this.m_CommandBuffer.SetComponent<Game.Common.Target>(index, entity2, target);
            // ISSUE: reference to a compiler-generated field
            this.m_CommandBuffer.SetComponent<Game.Creatures.Resident>(index, entity2, component3);
            // ISSUE: reference to a compiler-generated field
            this.m_CommandBuffer.SetComponent<Human>(index, entity2, component4);
            // ISSUE: reference to a compiler-generated field
            this.m_CommandBuffer.SetComponent<PathOwner>(index, entity2, component5);
            // ISSUE: reference to a compiler-generated field
            this.m_CommandBuffer.SetComponent<PseudoRandomSeed>(index, entity2, randomSeed);
            // ISSUE: reference to a compiler-generated field
            this.m_CommandBuffer.SetComponent<HumanCurrentLane>(index, entity2, component7);
            // ISSUE: reference to a compiler-generated field
            this.m_CommandBuffer.SetComponent<TripSource>(index, entity2, component6);
            return entity2;
        }

        private void ResetTrip(
          int index,
          Entity creature,
          Entity citizen,
          Entity fromBuilding,
          Game.Common.Target target,
          ResidentFlags flags,
          Game.Citizens.Purpose divertPurpose,
          uint timer,
          bool hasDivertPath)
        {
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            Entity entity = this.m_CommandBuffer.CreateEntity(index, this.m_ResetTripArchetype);
            // ISSUE: reference to a compiler-generated field
            this.m_CommandBuffer.SetComponent<ResetTrip>(index, entity, new ResetTrip()
            {
                m_Creature = creature,
                m_Source = fromBuilding,
                m_Target = target.m_Target,
                m_ResidentFlags = flags,
                m_DivertPurpose = divertPurpose,
                m_Delay = timer,
                m_HasDivertPath = hasDivertPath
            });
            DynamicBuffer<PathElement> bufferData;
            // ISSUE: reference to a compiler-generated field
            if (!this.m_PathElements.TryGetBuffer(citizen, out bufferData) || bufferData.Length <= 0)
                return;
            // ISSUE: reference to a compiler-generated field
            DynamicBuffer<PathElement> targetElements = this.m_CommandBuffer.AddBuffer<PathElement>(index, entity);
            PathUtils.CopyPath(bufferData, new PathOwner(), 0, targetElements);
        }

        private void RemoveAllTrips(DynamicBuffer<TripNeeded> trips)
        {
            if (trips.Length <= 0)
                return;
            Game.Citizens.Purpose purpose = trips[0].m_Purpose;
            for (int index = trips.Length - 1; index >= 0; --index)
            {
                if (trips[index].m_Purpose == purpose)
                    trips.RemoveAt(index);
            }
        }

        private Entity FindDistrict(Entity building)
        {
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            return this.m_CurrentDistrictData.HasComponent(building) ? this.m_CurrentDistrictData[building].m_District : Entity.Null;
        }

        private void AddPetTargets(Entity household, Entity source, Entity target)
        {
            // ISSUE: reference to a compiler-generated field
            if (!this.m_HouseholdAnimals.HasBuffer(household))
                return;
            // ISSUE: reference to a compiler-generated field
            DynamicBuffer<HouseholdAnimal> householdAnimal1 = this.m_HouseholdAnimals[household];
            for (int index = 0; index < householdAnimal1.Length; ++index)
            {
                HouseholdAnimal householdAnimal2 = householdAnimal1[index];
                // ISSUE: reference to a compiler-generated field
                // ISSUE: object of a compiler-generated type is created
                this.m_AnimalQueue.Enqueue(new Time2WorkTripNeededSystem.AnimalTargetInfo()
                {
                    m_Animal = householdAnimal2.m_HouseholdPet,
                    m_Source = source,
                    m_Target = target
                });
            }
        }

        public void Execute(
          in ArchetypeChunk chunk,
          int unfilteredChunkIndex,
          bool useEnabledMask,
          in v128 chunkEnabledMask)
        {
            // ISSUE: reference to a compiler-generated field
            NativeArray<Entity> nativeArray1 = chunk.GetNativeArray(this.m_EntityType);
            // ISSUE: reference to a compiler-generated field
            BufferAccessor<TripNeeded> bufferAccessor = chunk.GetBufferAccessor<TripNeeded>(ref this.m_TripNeededType);
            // ISSUE: reference to a compiler-generated field
            NativeArray<HouseholdMember> nativeArray2 = chunk.GetNativeArray<HouseholdMember>(ref this.m_HouseholdMemberType);
            // ISSUE: reference to a compiler-generated field
            NativeArray<CurrentBuilding> nativeArray3 = chunk.GetNativeArray<CurrentBuilding>(ref this.m_CurrentBuildingType);
            // ISSUE: reference to a compiler-generated field
            NativeArray<CurrentTransport> nativeArray4 = chunk.GetNativeArray<CurrentTransport>(ref this.m_CurrentTransportType);
            // ISSUE: reference to a compiler-generated field
            NativeArray<Citizen> nativeArray5 = chunk.GetNativeArray<Citizen>(ref this.m_CitizenType);
            // ISSUE: reference to a compiler-generated field
            NativeArray<HealthProblem> nativeArray6 = chunk.GetNativeArray<HealthProblem>(ref this.m_HealthProblemType);
            // ISSUE: reference to a compiler-generated field
            NativeArray<AttendingMeeting> nativeArray7 = chunk.GetNativeArray<AttendingMeeting>(ref this.m_AttendingMeetingType);
            for (int index = 0; index < nativeArray1.Length; ++index)
            {
                Entity entity1 = nativeArray1[index];
                DynamicBuffer<TripNeeded> trips = bufferAccessor[index];
                Entity household1 = nativeArray2[index].m_Household;
                Entity currentBuilding1 = nativeArray3[index].m_CurrentBuilding;
                if (trips.Length > 0)
                {
                    bool test = trips[0].m_Purpose == Game.Citizens.Purpose.MovingAway;
                    bool flag1 = trips[0].m_Purpose == Game.Citizens.Purpose.Safety || trips[0].m_Purpose == Game.Citizens.Purpose.Escape;
                    // ISSUE: reference to a compiler-generated field
                    bool isMailSender = chunk.IsComponentEnabled<MailSender>(ref this.m_MailSenderType, index);
                    bool isDead = false;
                    bool flag2 = false;
                    PathInformation componentData1;
                    // ISSUE: reference to a compiler-generated field
                    bool component1 = this.m_PathInfos.TryGetComponent(entity1, out componentData1);
                    if (nativeArray6.Length != 0)
                    {
                        HealthProblem healthProblem = nativeArray6[index];
                        if ((healthProblem.m_Flags & (HealthProblemFlags.Dead | HealthProblemFlags.RequireTransport | HealthProblemFlags.InDanger | HealthProblemFlags.Trapped)) != HealthProblemFlags.None)
                        {
                            isDead = (healthProblem.m_Flags & HealthProblemFlags.Dead) != 0;
                            flag2 = (healthProblem.m_Flags & HealthProblemFlags.RequireTransport) != 0;
                            if (isDead | flag2)
                            {
                                while (trips.Length > 0 && trips[0].m_Purpose != Game.Citizens.Purpose.Deathcare && trips[0].m_Purpose != Game.Citizens.Purpose.Hospital)
                                    trips.RemoveAt(0);
                                if (trips.Length == 0)
                                {
                                    if (component1)
                                    {
                                        // ISSUE: reference to a compiler-generated field
                                        // ISSUE: reference to a compiler-generated field
                                        this.m_CommandBuffer.RemoveComponent(unfilteredChunkIndex, entity1, in this.m_PathfindTypes);
                                        continue;
                                    }
                                    continue;
                                }
                            }
                            else
                            {
                                if (component1)
                                {
                                    // ISSUE: reference to a compiler-generated field
                                    // ISSUE: reference to a compiler-generated field
                                    this.m_CommandBuffer.RemoveComponent(unfilteredChunkIndex, entity1, in this.m_PathfindTypes);
                                    continue;
                                }
                                continue;
                            }
                        }
                    }
                    if (!test && nativeArray7.Length != 0)
                    {
                        Entity meeting1 = nativeArray7[index].m_Meeting;
                        // ISSUE: reference to a compiler-generated field
                        if (this.m_PrefabRefData.HasComponent(meeting1))
                        {
                            // ISSUE: reference to a compiler-generated field
                            Entity prefab = this.m_PrefabRefData[meeting1].m_Prefab;
                            // ISSUE: reference to a compiler-generated field
                            CoordinatedMeeting meeting2 = this.m_Meetings[meeting1];
                            // ISSUE: reference to a compiler-generated field
                            if (this.m_HaveCoordinatedMeetingDatas.HasBuffer(prefab))
                            {
                                // ISSUE: reference to a compiler-generated field
                                DynamicBuffer<HaveCoordinatedMeetingData> coordinatedMeetingData1 = this.m_HaveCoordinatedMeetingDatas[prefab];
                                if (coordinatedMeetingData1.Length > meeting2.m_Phase)
                                {
                                    HaveCoordinatedMeetingData coordinatedMeetingData2 = coordinatedMeetingData1[meeting2.m_Phase];
                                    while (trips.Length > 0 && trips[0].m_Purpose != coordinatedMeetingData2.m_TravelPurpose.m_Purpose)
                                        trips.RemoveAt(0);
                                    if (trips.Length == 0)
                                    {
                                        if (component1)
                                        {
                                            // ISSUE: reference to a compiler-generated field
                                            // ISSUE: reference to a compiler-generated field
                                            this.m_CommandBuffer.RemoveComponent(unfilteredChunkIndex, entity1, in this.m_PathfindTypes);
                                            continue;
                                        }
                                        continue;
                                    }
                                }
                            }
                        }
                    }
                    if ((nativeArray5[index].m_State & CitizenFlags.MovingAwayReachOC) != CitizenFlags.None)
                    {
                        if (component1)
                        {
                            // ISSUE: reference to a compiler-generated field
                            // ISSUE: reference to a compiler-generated field
                            this.m_CommandBuffer.RemoveComponent(unfilteredChunkIndex, entity1, in this.m_PathfindTypes);
                        }
                    }
                    else
                    {
                        if (component1)
                        {
                            if ((componentData1.m_State & PathFlags.Pending) == (PathFlags)0)
                            {
                                // ISSUE: reference to a compiler-generated field
                                if ((componentData1.m_Origin != Entity.Null && componentData1.m_Origin == componentData1.m_Destination || nativeArray3[index].m_CurrentBuilding == componentData1.m_Destination) && !flag1 || !this.m_Targets.HasComponent(entity1))
                                {
                                    // ISSUE: reference to a compiler-generated field
                                    // ISSUE: reference to a compiler-generated field
                                    this.m_CommandBuffer.RemoveComponent(unfilteredChunkIndex, entity1, in this.m_PathfindTypes);
                                    // ISSUE: reference to a compiler-generated method
                                    this.RemoveAllTrips(trips);
                                    continue;
                                }
                            }
                            else
                                continue;
                        }
                        PseudoRandomSeed randomSeed;
                        PathfindParameters pathfindParameters;
                        SetupQueueTarget setupQueueTarget;
                        // ISSUE: reference to a compiler-generated field
                        if (!this.m_DebugDisableSpawning)
                        {
                            // ISSUE: reference to a compiler-generated field
                            if (this.m_Targets.HasComponent(entity1))
                            {
                                // ISSUE: reference to a compiler-generated field
                                Game.Common.Target target = this.m_Targets[entity1];
                                if (target.m_Target == Entity.Null)
                                {
                                    if (!component1)
                                    {
                                        // ISSUE: reference to a compiler-generated field
                                        this.m_CommandBuffer.RemoveComponent<Game.Common.Target>(unfilteredChunkIndex, entity1);
                                        continue;
                                    }
                                    Entity destination = componentData1.m_Destination;
                                    if (destination == Entity.Null)
                                    {
                                        // ISSUE: reference to a compiler-generated field
                                        this.m_CommandBuffer.RemoveComponent<Game.Common.Target>(unfilteredChunkIndex, entity1);
                                        // ISSUE: reference to a compiler-generated method
                                        this.RemoveAllTrips(trips);
                                        // ISSUE: reference to a compiler-generated field
                                        // ISSUE: reference to a compiler-generated field
                                        this.m_CommandBuffer.RemoveComponent(unfilteredChunkIndex, entity1, in this.m_PathfindTypes);
                                        continue;
                                    }
                                    target.m_Target = destination;
                                }
                                Entity entity2 = target.m_Target;
                                PropertyRenter componentData2;
                                // ISSUE: reference to a compiler-generated field
                                if (this.m_Properties.TryGetComponent(entity2, out componentData2))
                                    entity2 = componentData2.m_Property;
                                TravelPurpose travelPurpose;
                                if (currentBuilding1 == entity2 && !flag1)
                                {
                                    // ISSUE: reference to a compiler-generated field
                                    this.m_CommandBuffer.SetComponentEnabled<Arrived>(unfilteredChunkIndex, entity1, true);
                                    // ISSUE: reference to a compiler-generated field
                                    ref EntityCommandBuffer.ParallelWriter local = ref this.m_CommandBuffer;
                                    int sortKey = unfilteredChunkIndex;
                                    Entity e = entity1;
                                    travelPurpose = new TravelPurpose();
                                    travelPurpose.m_Data = trips[0].m_Data;
                                    travelPurpose.m_Purpose = trips[0].m_Purpose;
                                    travelPurpose.m_Resource = trips[0].m_Resource;
                                    TravelPurpose component2 = travelPurpose;
                                    local.AddComponent<TravelPurpose>(sortKey, e, component2);
                                    // ISSUE: reference to a compiler-generated field
                                    this.m_CommandBuffer.RemoveComponent<Game.Common.Target>(unfilteredChunkIndex, entity1);
                                    if (component1)
                                    {
                                        // ISSUE: reference to a compiler-generated field
                                        // ISSUE: reference to a compiler-generated field
                                        this.m_CommandBuffer.RemoveComponent(unfilteredChunkIndex, entity1, in this.m_PathfindTypes);
                                    }
                                    // ISSUE: reference to a compiler-generated method
                                    this.RemoveAllTrips(trips);
                                }
                                else
                                {
                                    bool isCarried = isDead && trips[0].m_Purpose == Game.Citizens.Purpose.Deathcare || flag2 && trips[0].m_Purpose == Game.Citizens.Purpose.Hospital;
                                    if (!component1 && !isCarried)
                                    {
                                        // ISSUE: reference to a compiler-generated field
                                        // ISSUE: reference to a compiler-generated field
                                        this.m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity1, in this.m_PathfindTypes);
                                        // ISSUE: reference to a compiler-generated field
                                        this.m_CommandBuffer.SetComponent<PathInformation>(unfilteredChunkIndex, entity1, new PathInformation()
                                        {
                                            m_State = PathFlags.Pending
                                        });
                                        Citizen citizen = nativeArray5[index];
                                        CreatureData creatureData;
                                        // ISSUE: reference to a compiler-generated field
                                        // ISSUE: reference to a compiler-generated field
                                        // ISSUE: reference to a compiler-generated field
                                        // ISSUE: reference to a compiler-generated field
                                        // ISSUE: reference to a compiler-generated method
                                        Entity entity3 = ObjectEmergeSystem.SelectResidentPrefab(citizen, this.m_HumanChunks, this.m_EntityType, ref this.m_CreatureDataType, ref this.m_ResidentDataType, out creatureData, out randomSeed);
                                        HumanData humanData = new HumanData();
                                        if (entity3 != Entity.Null)
                                        {
                                            // ISSUE: reference to a compiler-generated field
                                            humanData = this.m_PrefabHumanData[entity3];
                                        }
                                        // ISSUE: reference to a compiler-generated field
                                        Household household2 = this.m_Households[household1];
                                        // ISSUE: reference to a compiler-generated field
                                        DynamicBuffer<HouseholdCitizen> householdCitizen = this.m_HouseholdCitizens[household1];
                                        pathfindParameters = new PathfindParameters();
                                        pathfindParameters.m_MaxSpeed = (float2)277.777771f;
                                        pathfindParameters.m_WalkSpeed = (float2)humanData.m_WalkSpeed;
                                        pathfindParameters.m_Weights = CitizenUtils.GetPathfindWeights(citizen, household2, householdCitizen.Length);
                                        // ISSUE: reference to a compiler-generated field
                                        pathfindParameters.m_Methods = PathMethod.Pedestrian | PathMethod.Taxi | RouteUtils.GetPublicTransportMethods(this.m_TimeOfDay);
                                        pathfindParameters.m_SecondaryIgnoredRules = VehicleUtils.GetIgnoredPathfindRulesTaxiDefaults();
                                        // ISSUE: reference to a compiler-generated field
                                        // ISSUE: reference to a compiler-generated field
                                        pathfindParameters.m_MaxCost = math.select(CitizenBehaviorSystem.kMaxPathfindCost, CitizenBehaviorSystem.kMaxMovingAwayCost, test);
                                        PathfindParameters parameters = pathfindParameters;
                                        setupQueueTarget = new SetupQueueTarget();
                                        setupQueueTarget.m_Type = SetupTargetType.CurrentLocation;
                                        setupQueueTarget.m_Methods = PathMethod.Pedestrian;
                                        setupQueueTarget.m_RandomCost = 30f;
                                        SetupQueueTarget origin = setupQueueTarget;
                                        setupQueueTarget = new SetupQueueTarget();
                                        setupQueueTarget.m_Type = SetupTargetType.CurrentLocation;
                                        setupQueueTarget.m_Methods = PathMethod.Pedestrian;
                                        setupQueueTarget.m_Entity = target.m_Target;
                                        setupQueueTarget.m_RandomCost = 30f;
                                        setupQueueTarget.m_ActivityMask = creatureData.m_SupportedActivities;
                                        SetupQueueTarget destination = setupQueueTarget;
                                        // ISSUE: reference to a compiler-generated field
                                        if (this.m_PropertyRenters.HasComponent(household1))
                                        {
                                            // ISSUE: reference to a compiler-generated field
                                            parameters.m_Authorization1 = this.m_PropertyRenters[household1].m_Property;
                                        }
                                        // ISSUE: reference to a compiler-generated field
                                        if (this.m_Workers.HasComponent(entity1))
                                        {
                                            // ISSUE: reference to a compiler-generated field
                                            Worker worker = this.m_Workers[entity1];
                                            // ISSUE: reference to a compiler-generated field
                                            // ISSUE: reference to a compiler-generated field
                                            parameters.m_Authorization2 = !this.m_PropertyRenters.HasComponent(worker.m_Workplace) ? worker.m_Workplace : this.m_PropertyRenters[worker.m_Workplace].m_Property;
                                        }
                                        // ISSUE: reference to a compiler-generated field
                                        if (this.m_CarKeepers.IsComponentEnabled(entity1))
                                        {
                                            // ISSUE: reference to a compiler-generated field
                                            Entity car = this.m_CarKeepers[entity1].m_Car;
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
                                        // ISSUE: reference to a compiler-generated field
                                        this.m_PathQueue.Enqueue(new SetupQueueItem(entity1, parameters, origin, destination));
                                    }
                                    else
                                    {
                                        DynamicBuffer<PathElement> dynamicBuffer = new DynamicBuffer<PathElement>();
                                        if (!isCarried)
                                        {
                                            // ISSUE: reference to a compiler-generated field
                                            dynamicBuffer = this.m_PathElements[entity1];
                                        }
                                        TripNeeded tripNeeded = trips[0];
                                        // ISSUE: reference to a compiler-generated field
                                        if (!isCarried && dynamicBuffer.Length > 0 || this.m_PrefabRefData.HasComponent(tripNeeded.m_TargetAgent))
                                        {
                                            Entity currentBuilding2 = nativeArray3[index].m_CurrentBuilding;
                                            Entity entity4 = Entity.Null;
                                            PropertyRenter componentData4;
                                            // ISSUE: reference to a compiler-generated field
                                            bool component3 = this.m_PropertyRenters.TryGetComponent(household1, out componentData4);
                                            if (!isCarried & component3 && currentBuilding2.Equals(componentData4.m_Property))
                                            {
                                                if (componentData1.m_Destination != Entity.Null)
                                                {
                                                    if ((componentData1.m_Methods & (PathMethod.PublicTransportDay | PathMethod.Taxi | PathMethod.PublicTransportNight)) != (PathMethod)0)
                                                    {
                                                        // ISSUE: reference to a compiler-generated field
                                                        if (this.m_DebugPathQueuePublic.IsCreated)
                                                        {
                                                            // ISSUE: reference to a compiler-generated field
                                                            this.m_DebugPathQueuePublic.Enqueue(Mathf.RoundToInt(componentData1.m_TotalCost));
                                                        }
                                                        if ((componentData1.m_Methods & PathMethod.Taxi) != (PathMethod)0)
                                                        {
                                                            // ISSUE: reference to a compiler-generated field
                                                            if (this.m_DebugTaxiDuration.IsCreated)
                                                            {
                                                                // ISSUE: reference to a compiler-generated field
                                                                this.m_DebugTaxiDuration.Enqueue(Mathf.RoundToInt(componentData1.m_Duration));
                                                            }
                                                        }
                                                        else
                                                        {
                                                            // ISSUE: reference to a compiler-generated field
                                                            if (this.m_DebugPublicTransportDuration.IsCreated)
                                                            {
                                                                // ISSUE: reference to a compiler-generated field
                                                                this.m_DebugPublicTransportDuration.Enqueue(Mathf.RoundToInt(componentData1.m_Duration));
                                                            }
                                                        }
                                                    }
                                                    else if ((componentData1.m_Methods & (PathMethod.Road | PathMethod.MediumRoad)) != (PathMethod)0)
                                                    {
                                                        // ISSUE: reference to a compiler-generated field
                                                        if (this.m_DebugPathQueueCar.IsCreated)
                                                        {
                                                            // ISSUE: reference to a compiler-generated field
                                                            this.m_DebugPathQueueCar.Enqueue(Mathf.RoundToInt(componentData1.m_TotalCost));
                                                        }
                                                        // ISSUE: reference to a compiler-generated field
                                                        if (this.m_DebugCarDuration.IsCreated)
                                                        {
                                                            // ISSUE: reference to a compiler-generated field
                                                            this.m_DebugCarDuration.Enqueue(Mathf.RoundToInt(componentData1.m_Duration));
                                                        }
                                                    }
                                                    else if ((componentData1.m_Methods & PathMethod.Pedestrian) != (PathMethod)0)
                                                    {
                                                        if ((double)componentData1.m_Distance > 3000.0)
                                                        {
                                                            // ISSUE: reference to a compiler-generated field
                                                            if (this.m_DebugPathQueuePedestrian.IsCreated)
                                                            {
                                                                // ISSUE: reference to a compiler-generated field
                                                                this.m_DebugPathQueuePedestrian.Enqueue(Mathf.RoundToInt(componentData1.m_TotalCost));
                                                            }
                                                            // ISSUE: reference to a compiler-generated field
                                                            if (this.m_DebugPedestrianDuration.IsCreated)
                                                            {
                                                                // ISSUE: reference to a compiler-generated field
                                                                this.m_DebugPedestrianDuration.Enqueue(Mathf.RoundToInt(componentData1.m_Duration));
                                                            }
                                                        }
                                                        else
                                                        {
                                                            // ISSUE: reference to a compiler-generated field
                                                            if (this.m_DebugPathQueuePedestrianShort.IsCreated)
                                                            {
                                                                // ISSUE: reference to a compiler-generated field
                                                                this.m_DebugPathQueuePedestrianShort.Enqueue(Mathf.RoundToInt(componentData1.m_TotalCost));
                                                            }
                                                            // ISSUE: reference to a compiler-generated field
                                                            if (this.m_DebugPedestrianDurationShort.IsCreated)
                                                            {
                                                                // ISSUE: reference to a compiler-generated field
                                                                this.m_DebugPedestrianDurationShort.Enqueue(Mathf.RoundToInt(componentData1.m_Duration));
                                                            }
                                                        }
                                                    }
                                                }
                                                // ISSUE: reference to a compiler-generated field
                                                if (tripNeeded.m_Purpose == Game.Citizens.Purpose.GoingToWork && this.m_Workers.HasComponent(entity1))
                                                {
                                                    if (componentData1.m_Destination == Entity.Null)
                                                    {
                                                        // ISSUE: reference to a compiler-generated field
                                                        this.m_CommandBuffer.RemoveComponent<Worker>(unfilteredChunkIndex, entity1);
                                                    }
                                                    else
                                                    {
                                                        // ISSUE: reference to a compiler-generated field
                                                        Worker worker = this.m_Workers[entity1] with
                                                        {
                                                            m_LastCommuteTime = componentData1.m_Duration
                                                        };
                                                        // ISSUE: reference to a compiler-generated field
                                                        this.m_Workers[entity1] = worker;
                                                    }
                                                }
                                                else
                                                {
                                                    // ISSUE: reference to a compiler-generated field
                                                    if (tripNeeded.m_Purpose == Game.Citizens.Purpose.GoingToSchool && this.m_Students.HasComponent(entity1))
                                                    {
                                                        if (componentData1.m_Destination == Entity.Null)
                                                        {
                                                            // ISSUE: reference to a compiler-generated field
                                                            // ISSUE: reference to a compiler-generated field
                                                            this.m_CommandBuffer.AddComponent<StudentsRemoved>(unfilteredChunkIndex, this.m_Students[entity1].m_School);
                                                            // ISSUE: reference to a compiler-generated field
                                                            this.m_CommandBuffer.RemoveComponent<Game.Citizens.Student>(unfilteredChunkIndex, entity1);
                                                        }
                                                        else
                                                        {
                                                            // ISSUE: reference to a compiler-generated field
                                                            Game.Citizens.Student student = this.m_Students[entity1] with
                                                            {
                                                                m_LastCommuteTime = componentData1.m_Duration
                                                            };
                                                            // ISSUE: reference to a compiler-generated field
                                                            this.m_Students[entity1] = student;
                                                        }
                                                    }
                                                }
                                            }
                                            ResidentFlags flags = ResidentFlags.None;
                                            if (nativeArray7.Length > 0)
                                            {
                                                Entity meeting3 = nativeArray7[index].m_Meeting;
                                                // ISSUE: reference to a compiler-generated field
                                                if (this.m_PrefabRefData.HasComponent(meeting3))
                                                {
                                                    // ISSUE: reference to a compiler-generated field
                                                    CoordinatedMeeting meeting4 = this.m_Meetings[meeting3];
                                                    // ISSUE: reference to a compiler-generated field
                                                    // ISSUE: reference to a compiler-generated field
                                                    DynamicBuffer<HaveCoordinatedMeetingData> coordinatedMeetingData3 = this.m_HaveCoordinatedMeetingDatas[this.m_PrefabRefData[meeting3].m_Prefab];
                                                    if (meeting4.m_Status != MeetingStatus.Done)
                                                    {
                                                        HaveCoordinatedMeetingData coordinatedMeetingData4 = coordinatedMeetingData3[meeting4.m_Phase];
                                                        if (tripNeeded.m_Purpose == coordinatedMeetingData4.m_TravelPurpose.m_Purpose && (coordinatedMeetingData4.m_TravelPurpose.m_Resource == Resource.NoResource || coordinatedMeetingData4.m_TravelPurpose.m_Resource == tripNeeded.m_Resource) && meeting4.m_Target == Entity.Null)
                                                        {
                                                            // ISSUE: reference to a compiler-generated field
                                                            DynamicBuffer<CoordinatedMeetingAttendee> attendee = this.m_Attendees[meeting3];
                                                            if (attendee.Length > 0 && attendee[0].m_Attendee == entity1)
                                                            {
                                                                meeting4.m_Target = target.m_Target;
                                                                // ISSUE: reference to a compiler-generated field
                                                                this.m_Meetings[meeting3] = meeting4;
                                                                flags |= ResidentFlags.PreferredLeader;
                                                            }
                                                            else
                                                                continue;
                                                        }
                                                    }
                                                    else
                                                        continue;
                                                }
                                            }
                                            // ISSUE: reference to a compiler-generated field
                                            if (this.m_Workers.HasComponent(entity1))
                                            {
                                                // ISSUE: reference to a compiler-generated field
                                                Worker worker = this.m_Workers[entity1];
                                                // ISSUE: reference to a compiler-generated field
                                                // ISSUE: reference to a compiler-generated field
                                                entity4 = !this.m_PropertyRenters.HasComponent(worker.m_Workplace) ? worker.m_Workplace : this.m_PropertyRenters[worker.m_Workplace].m_Property;
                                            }
                                            if (currentBuilding2.Equals(componentData4.m_Property) || currentBuilding2.Equals(entity4))
                                            {
                                                // ISSUE: reference to a compiler-generated field
                                                this.m_LeaveQueue.Enqueue(entity1);
                                            }
                                            Entity currentTransport = Entity.Null;
                                            if (nativeArray4.Length != 0)
                                                currentTransport = nativeArray4[index].m_CurrentTransport;
                                            uint timer = 512 /*0x0200*/;
                                            Game.Citizens.Purpose divertPurpose = Game.Citizens.Purpose.None;
                                            bool pathFailed = !isCarried && dynamicBuffer.Length == 0;
                                            bool hasDivertPath = false;
                                            // ISSUE: reference to a compiler-generated method
                                            this.GetResidentFlags(entity1, currentBuilding2, isMailSender, pathFailed, ref target, ref tripNeeded.m_Purpose, ref divertPurpose, ref timer, ref hasDivertPath);
                                            UnderConstruction componentData5;
                                            // ISSUE: reference to a compiler-generated field
                                            if (this.m_UnderConstructionData.TryGetComponent(entity2, out componentData5) && componentData5.m_NewPrefab == Entity.Null)
                                                timer = math.max(timer, ObjectUtils.GetTripDelayFrames(componentData5, componentData1));
                                            // ISSUE: reference to a compiler-generated field
                                            // ISSUE: reference to a compiler-generated field
                                            if (this.m_PrefabRefData.HasComponent(currentTransport) && !this.m_Deleteds.HasComponent(currentTransport))
                                            {
                                                // ISSUE: reference to a compiler-generated method
                                                this.ResetTrip(unfilteredChunkIndex, currentTransport, entity1, currentBuilding1, target, flags, divertPurpose, timer, hasDivertPath);
                                            }
                                            else
                                            {
                                                Citizen citizenData = nativeArray5[index];
                                                // ISSUE: reference to a compiler-generated method
                                                Entity transport = this.SpawnResident(unfilteredChunkIndex, entity1, currentBuilding1, citizenData, target, flags, divertPurpose, timer, hasDivertPath, isDead, isCarried);
                                                // ISSUE: reference to a compiler-generated field
                                                this.m_CommandBuffer.AddComponent<CurrentTransport>(unfilteredChunkIndex, entity1, new CurrentTransport(transport));
                                            }
                                            if (tripNeeded.m_Purpose != Game.Citizens.Purpose.GoingToWork && tripNeeded.m_Purpose != Game.Citizens.Purpose.GoingToSchool || currentBuilding1 != componentData4.m_Property)
                                            {
                                                // ISSUE: reference to a compiler-generated method
                                                this.AddPetTargets(household1, currentBuilding1, target.m_Target);
                                            }
                                            // ISSUE: reference to a compiler-generated field
                                            ref EntityCommandBuffer.ParallelWriter local = ref this.m_CommandBuffer;
                                            int sortKey = unfilteredChunkIndex;
                                            Entity e = entity1;
                                            travelPurpose = new TravelPurpose();
                                            travelPurpose.m_Data = tripNeeded.m_Data;
                                            travelPurpose.m_Purpose = tripNeeded.m_Purpose;
                                            travelPurpose.m_Resource = tripNeeded.m_Resource;
                                            TravelPurpose component4 = travelPurpose;
                                            local.AddComponent<TravelPurpose>(sortKey, e, component4);
                                            // ISSUE: reference to a compiler-generated field
                                            this.m_CommandBuffer.RemoveComponent<CurrentBuilding>(unfilteredChunkIndex, entity1);
                                        }
                                        else
                                        {
                                            // ISSUE: reference to a compiler-generated field
                                            if ((this.m_Households[household1].m_Flags & HouseholdFlags.MovedIn) == HouseholdFlags.None)
                                            {
                                                // ISSUE: reference to a compiler-generated field
                                                CitizenUtils.HouseholdMoveAway(this.m_CommandBuffer, unfilteredChunkIndex, household1);
                                            }
                                        }
                                        // ISSUE: reference to a compiler-generated method
                                        this.RemoveAllTrips(trips);
                                        // ISSUE: reference to a compiler-generated field
                                        // ISSUE: reference to a compiler-generated field
                                        this.m_CommandBuffer.RemoveComponent(unfilteredChunkIndex, entity1, in this.m_PathfindTypes);
                                        // ISSUE: reference to a compiler-generated field
                                        this.m_CommandBuffer.RemoveComponent<Game.Common.Target>(unfilteredChunkIndex, entity1);
                                    }
                                }
                            }
                            else
                            {
                                // ISSUE: reference to a compiler-generated field
                                if (!component1 && this.m_HumanChunks.Length != 0)
                                {
                                    // ISSUE: reference to a compiler-generated field
                                    if (!this.m_Transforms.HasComponent(currentBuilding1))
                                    {
                                        // ISSUE: reference to a compiler-generated method
                                        this.RemoveAllTrips(trips);
                                    }
                                    else if (trips[0].m_TargetAgent != Entity.Null)
                                    {
                                        // ISSUE: reference to a compiler-generated field
                                        this.m_CommandBuffer.AddComponent<Game.Common.Target>(unfilteredChunkIndex, entity1, new Game.Common.Target()
                                        {
                                            m_Target = trips[0].m_TargetAgent
                                        });
                                    }
                                    else if (PathUtils.IsPathfindingPurpose(trips[0].m_Purpose))
                                    {
                                        Citizen citizen = nativeArray5[index];
                                        if (trips[0].m_Purpose == Game.Citizens.Purpose.GoingHome)
                                        {
                                            if ((citizen.m_State & CitizenFlags.Commuter) == CitizenFlags.None)
                                            {
                                                // ISSUE: reference to a compiler-generated method
                                                this.RemoveAllTrips(trips);
                                                continue;
                                            }
                                            // ISSUE: reference to a compiler-generated field
                                            if (this.m_OutsideConnections.HasComponent(nativeArray3[index].m_CurrentBuilding))
                                            {
                                                // ISSUE: reference to a compiler-generated method
                                                this.RemoveAllTrips(trips);
                                                continue;
                                            }
                                        }
                                        // ISSUE: reference to a compiler-generated field
                                        // ISSUE: reference to a compiler-generated field
                                        this.m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity1, in this.m_PathfindTypes);
                                        // ISSUE: reference to a compiler-generated field
                                        this.m_CommandBuffer.SetComponent<PathInformation>(unfilteredChunkIndex, entity1, new PathInformation()
                                        {
                                            m_State = PathFlags.Pending
                                        });
                                        CreatureData creatureData;
                                        // ISSUE: reference to a compiler-generated field
                                        // ISSUE: reference to a compiler-generated field
                                        // ISSUE: reference to a compiler-generated field
                                        // ISSUE: reference to a compiler-generated field
                                        // ISSUE: reference to a compiler-generated method
                                        Entity entity5 = ObjectEmergeSystem.SelectResidentPrefab(citizen, this.m_HumanChunks, this.m_EntityType, ref this.m_CreatureDataType, ref this.m_ResidentDataType, out creatureData, out randomSeed);
                                        HumanData humanData = new HumanData();
                                        if (entity5 != Entity.Null)
                                        {
                                            // ISSUE: reference to a compiler-generated field
                                            humanData = this.m_PrefabHumanData[entity5];
                                        }
                                        // ISSUE: reference to a compiler-generated field
                                        Household household3 = this.m_Households[household1];
                                        // ISSUE: reference to a compiler-generated field
                                        DynamicBuffer<HouseholdCitizen> householdCitizen = this.m_HouseholdCitizens[household1];
                                        pathfindParameters = new PathfindParameters();
                                        pathfindParameters.m_MaxSpeed = (float2)277.777771f;
                                        pathfindParameters.m_WalkSpeed = (float2)humanData.m_WalkSpeed;
                                        pathfindParameters.m_Weights = CitizenUtils.GetPathfindWeights(citizen, household3, householdCitizen.Length);
                                        // ISSUE: reference to a compiler-generated field
                                        pathfindParameters.m_Methods = PathMethod.Pedestrian | PathMethod.Taxi | RouteUtils.GetPublicTransportMethods(this.m_TimeOfDay);
                                        pathfindParameters.m_SecondaryIgnoredRules = VehicleUtils.GetIgnoredPathfindRulesTaxiDefaults();
                                        // ISSUE: reference to a compiler-generated field
                                        pathfindParameters.m_MaxCost = CitizenBehaviorSystem.kMaxPathfindCost;
                                        PathfindParameters parameters = pathfindParameters;
                                        setupQueueTarget = new SetupQueueTarget();
                                        setupQueueTarget.m_Type = SetupTargetType.CurrentLocation;
                                        setupQueueTarget.m_Methods = PathMethod.Pedestrian;
                                        setupQueueTarget.m_RandomCost = 30f;
                                        SetupQueueTarget origin = setupQueueTarget;
                                        setupQueueTarget = new SetupQueueTarget();
                                        setupQueueTarget.m_Methods = PathMethod.Pedestrian;
                                        setupQueueTarget.m_RandomCost = 30f;
                                        setupQueueTarget.m_ActivityMask = creatureData.m_SupportedActivities;
                                        SetupQueueTarget destination = setupQueueTarget;
                                        Game.Citizens.Purpose purpose = trips[0].m_Purpose;
                                        if ((uint)purpose <= 17U)
                                        {
                                            switch (purpose)
                                            {
                                                case Game.Citizens.Purpose.GoingHome:
                                                    destination.m_Type = SetupTargetType.OutsideConnection;
                                                    goto label_138;
                                                case Game.Citizens.Purpose.Hospital:
                                                    // ISSUE: reference to a compiler-generated method
                                                    destination.m_Entity = this.FindDistrict(currentBuilding1);
                                                    destination.m_Type = SetupTargetType.Hospital;
                                                    goto label_138;
                                                case Game.Citizens.Purpose.Safety:
                                                    break;
                                                case Game.Citizens.Purpose.EmergencyShelter:
                                                    parameters.m_Weights = new PathfindWeights(1f, 0.0f, 0.0f, 0.0f);
                                                    // ISSUE: reference to a compiler-generated method
                                                    destination.m_Entity = this.FindDistrict(currentBuilding1);
                                                    destination.m_Type = SetupTargetType.EmergencyShelter;
                                                    goto label_138;
                                                case Game.Citizens.Purpose.Crime:
                                                    destination.m_Type = SetupTargetType.CrimeProducer;
                                                    goto label_138;
                                                default:
                                                    goto label_138;
                                            }
                                        }
                                        else
                                        {
                                            switch (purpose)
                                            {
                                                case Game.Citizens.Purpose.Escape:
                                                    break;
                                                case Game.Citizens.Purpose.Sightseeing:
                                                    destination.m_Type = SetupTargetType.Sightseeing;
                                                    goto label_138;
                                                case Game.Citizens.Purpose.VisitAttractions:
                                                    destination.m_Type = SetupTargetType.Attraction;
                                                    goto label_138;
                                                default:
                                                    goto label_138;
                                            }
                                        }
                                        destination.m_Type = SetupTargetType.Safety;
                                    label_138:
                                        // ISSUE: reference to a compiler-generated field
                                        if (this.m_PropertyRenters.HasComponent(household1))
                                        {
                                            // ISSUE: reference to a compiler-generated field
                                            parameters.m_Authorization1 = this.m_PropertyRenters[household1].m_Property;
                                        }
                                        // ISSUE: reference to a compiler-generated field
                                        if (this.m_Workers.HasComponent(entity1))
                                        {
                                            // ISSUE: reference to a compiler-generated field
                                            Worker worker = this.m_Workers[entity1];
                                            // ISSUE: reference to a compiler-generated field
                                            // ISSUE: reference to a compiler-generated field
                                            parameters.m_Authorization2 = !this.m_PropertyRenters.HasComponent(worker.m_Workplace) ? worker.m_Workplace : this.m_PropertyRenters[worker.m_Workplace].m_Property;
                                        }
                                        // ISSUE: reference to a compiler-generated field
                                        if (this.m_CarKeepers.IsComponentEnabled(entity1))
                                        {
                                            // ISSUE: reference to a compiler-generated field
                                            Entity car = this.m_CarKeepers[entity1].m_Car;
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
                                                Game.Vehicles.PersonalCar componentData6;
                                                // ISSUE: reference to a compiler-generated field
                                                if (this.m_PersonalCarData.TryGetComponent(car, out componentData6) && (componentData6.m_State & PersonalCarFlags.HomeTarget) == (PersonalCarFlags)0)
                                                    parameters.m_PathfindFlags |= PathfindFlags.ParkingReset;
                                            }
                                        }
                                        // ISSUE: reference to a compiler-generated field
                                        this.m_PathQueue.Enqueue(new SetupQueueItem(entity1, parameters, origin, destination));
                                        // ISSUE: reference to a compiler-generated field
                                        this.m_CommandBuffer.AddComponent<Game.Common.Target>(unfilteredChunkIndex, entity1, new Game.Common.Target()
                                        {
                                            m_Target = Entity.Null
                                        });
                                    }
                                    else
                                    {
                                        // ISSUE: reference to a compiler-generated method
                                        this.RemoveAllTrips(trips);
                                    }
                                }
                            }
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
            // ISSUE: reference to a compiler-generated method
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
        public ComponentTypeHandle<HealthProblem> __Game_Citizens_HealthProblem_RO_ComponentTypeHandle;
        [ReadOnly]
        public ComponentTypeHandle<HouseholdMember> __Game_Citizens_HouseholdMember_RO_ComponentTypeHandle;
        [ReadOnly]
        public ComponentTypeHandle<MailSender> __Game_Citizens_MailSender_RO_ComponentTypeHandle;
        [ReadOnly]
        public ComponentTypeHandle<CurrentTransport> __Game_Citizens_CurrentTransport_RO_ComponentTypeHandle;
        public ComponentTypeHandle<CurrentBuilding> __Game_Citizens_CurrentBuilding_RW_ComponentTypeHandle;
        public BufferTypeHandle<TripNeeded> __Game_Citizens_TripNeeded_RW_BufferTypeHandle;
        [ReadOnly]
        public ComponentTypeHandle<AttendingMeeting> __Game_Citizens_AttendingMeeting_RO_ComponentTypeHandle;
        [ReadOnly]
        public ComponentTypeHandle<CreatureData> __Game_Prefabs_CreatureData_RO_ComponentTypeHandle;
        [ReadOnly]
        public ComponentTypeHandle<ResidentData> __Game_Prefabs_ResidentData_RO_ComponentTypeHandle;
        [ReadOnly]
        public ComponentLookup<ParkedCar> __Game_Vehicles_ParkedCar_RO_ComponentLookup;
        [ReadOnly]
        public ComponentLookup<Game.Vehicles.PersonalCar> __Game_Vehicles_PersonalCar_RO_ComponentLookup;
        [ReadOnly]
        public ComponentLookup<Game.Vehicles.Ambulance> __Game_Vehicles_Ambulance_RO_ComponentLookup;
        [ReadOnly]
        public ComponentLookup<CurrentDistrict> __Game_Areas_CurrentDistrict_RO_ComponentLookup;
        [ReadOnly]
        public ComponentLookup<PathInformation> __Game_Pathfind_PathInformation_RO_ComponentLookup;
        [ReadOnly]
        public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;
        [ReadOnly]
        public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;
        [ReadOnly]
        public ComponentLookup<Game.Common.Target> __Game_Common_Target_RO_ComponentLookup;
        [ReadOnly]
        public ComponentLookup<Deleted> __Game_Common_Deleted_RO_ComponentLookup;
        [ReadOnly]
        public BufferLookup<PathElement> __Game_Pathfind_PathElement_RO_BufferLookup;
        [ReadOnly]
        public ComponentLookup<CarKeeper> __Game_Citizens_CarKeeper_RO_ComponentLookup;
        public ComponentLookup<Worker> __Game_Citizens_Worker_RW_ComponentLookup;
        public ComponentLookup<Game.Citizens.Student> __Game_Citizens_Student_RW_ComponentLookup;
        [ReadOnly]
        public ComponentLookup<ObjectData> __Game_Prefabs_ObjectData_RO_ComponentLookup;
        [ReadOnly]
        public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;
        [ReadOnly]
        public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;
        [ReadOnly]
        public ComponentLookup<CarData> __Game_Prefabs_CarData_RO_ComponentLookup;
        [ReadOnly]
        public ComponentLookup<HumanData> __Game_Prefabs_HumanData_RO_ComponentLookup;
        [ReadOnly]
        public ComponentLookup<Game.Objects.OutsideConnection> __Game_Objects_OutsideConnection_RO_ComponentLookup;
        [ReadOnly]
        public ComponentLookup<UnderConstruction> __Game_Objects_UnderConstruction_RO_ComponentLookup;
        public ComponentLookup<CoordinatedMeeting> __Game_Citizens_CoordinatedMeeting_RW_ComponentLookup;
        [ReadOnly]
        public BufferLookup<CoordinatedMeetingAttendee> __Game_Citizens_CoordinatedMeetingAttendee_RO_BufferLookup;
        [ReadOnly]
        public BufferLookup<HouseholdAnimal> __Game_Citizens_HouseholdAnimal_RO_BufferLookup;
        [ReadOnly]
        public ComponentLookup<TravelPurpose> __Game_Citizens_TravelPurpose_RO_ComponentLookup;
        [ReadOnly]
        public BufferLookup<HaveCoordinatedMeetingData> __Game_Prefabs_HaveCoordinatedMeetingData_RO_BufferLookup;
        [ReadOnly]
        public ComponentLookup<Household> __Game_Citizens_Household_RO_ComponentLookup;
        [ReadOnly]
        public BufferLookup<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferLookup;
        [ReadOnly]
        public BufferLookup<OwnedVehicle> __Game_Vehicles_OwnedVehicle_RO_BufferLookup;
        [ReadOnly]
        public ComponentLookup<CurrentBuilding> __Game_Citizens_CurrentBuilding_RO_ComponentLookup;
        public ComponentLookup<CitizenPresence> __Game_Buildings_CitizenPresence_RW_ComponentLookup;
        [ReadOnly]
        public ComponentTypeHandle<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentTypeHandle;
        [ReadOnly]
        public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;
        [ReadOnly]
        public BufferTypeHandle<OwnedVehicle> __Game_Vehicles_OwnedVehicle_RO_BufferTypeHandle;
        public BufferTypeHandle<Game.Economy.Resources> __Game_Economy_Resources_RW_BufferTypeHandle;
        [ReadOnly]
        public ComponentLookup<DeliveryTruckData> __Game_Prefabs_DeliveryTruckData_RO_ComponentLookup;
        [ReadOnly]
        public ComponentLookup<TransportCompanyData> __Game_Companies_TransportCompanyData_RO_ComponentLookup;
        [ReadOnly]
        public ComponentLookup<ServiceRequest> __Game_Simulation_ServiceRequest_RO_ComponentLookup;
        [ReadOnly]
        public BufferLookup<ActivityLocationElement> __Game_Prefabs_ActivityLocationElement_RO_BufferLookup;
        [ReadOnly]
        public ComponentTypeHandle<TruckSchedule> __Game_Vehicles_TruckSchedule_RW_ComponentLookup;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void __AssignHandles(ref SystemState state)
        {
            this.__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
            this.__Game_Citizens_Citizen_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Citizen>(true);
            this.__Game_Citizens_HealthProblem_RO_ComponentTypeHandle = state.GetComponentTypeHandle<HealthProblem>(true);
            this.__Game_Citizens_HouseholdMember_RO_ComponentTypeHandle = state.GetComponentTypeHandle<HouseholdMember>(true);
            this.__Game_Citizens_MailSender_RO_ComponentTypeHandle = state.GetComponentTypeHandle<MailSender>(true);
            this.__Game_Citizens_CurrentTransport_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CurrentTransport>(true);
            this.__Game_Citizens_CurrentBuilding_RW_ComponentTypeHandle = state.GetComponentTypeHandle<CurrentBuilding>();
            this.__Game_Citizens_TripNeeded_RW_BufferTypeHandle = state.GetBufferTypeHandle<TripNeeded>();
            this.__Game_Citizens_AttendingMeeting_RO_ComponentTypeHandle = state.GetComponentTypeHandle<AttendingMeeting>(true);
            this.__Game_Prefabs_CreatureData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CreatureData>(true);
            this.__Game_Prefabs_ResidentData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ResidentData>(true);
            this.__Game_Vehicles_ParkedCar_RO_ComponentLookup = state.GetComponentLookup<ParkedCar>(true);
            this.__Game_Vehicles_PersonalCar_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.PersonalCar>(true);
            this.__Game_Vehicles_Ambulance_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.Ambulance>(true);
            this.__Game_Areas_CurrentDistrict_RO_ComponentLookup = state.GetComponentLookup<CurrentDistrict>(true);
            this.__Game_Pathfind_PathInformation_RO_ComponentLookup = state.GetComponentLookup<PathInformation>(true);
            this.__Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(true);
            this.__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(true);
            this.__Game_Common_Target_RO_ComponentLookup = state.GetComponentLookup<Game.Common.Target>(true);
            this.__Game_Common_Deleted_RO_ComponentLookup = state.GetComponentLookup<Deleted>(true);
            this.__Game_Pathfind_PathElement_RO_BufferLookup = state.GetBufferLookup<PathElement>(true);
            this.__Game_Citizens_CarKeeper_RO_ComponentLookup = state.GetComponentLookup<CarKeeper>(true);
            this.__Game_Citizens_Worker_RW_ComponentLookup = state.GetComponentLookup<Worker>();
            this.__Game_Citizens_Student_RW_ComponentLookup = state.GetComponentLookup<Game.Citizens.Student>();
            this.__Game_Prefabs_ObjectData_RO_ComponentLookup = state.GetComponentLookup<ObjectData>(true);
            this.__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(true);
            this.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(true);
            this.__Game_Prefabs_CarData_RO_ComponentLookup = state.GetComponentLookup<CarData>(true);
            this.__Game_Prefabs_HumanData_RO_ComponentLookup = state.GetComponentLookup<HumanData>(true);
            this.__Game_Objects_OutsideConnection_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.OutsideConnection>(true);
            this.__Game_Objects_UnderConstruction_RO_ComponentLookup = state.GetComponentLookup<UnderConstruction>(true);
            this.__Game_Citizens_CoordinatedMeeting_RW_ComponentLookup = state.GetComponentLookup<CoordinatedMeeting>();
            this.__Game_Citizens_CoordinatedMeetingAttendee_RO_BufferLookup = state.GetBufferLookup<CoordinatedMeetingAttendee>(true);
            this.__Game_Citizens_HouseholdAnimal_RO_BufferLookup = state.GetBufferLookup<HouseholdAnimal>(true);
            this.__Game_Citizens_TravelPurpose_RO_ComponentLookup = state.GetComponentLookup<TravelPurpose>(true);
            this.__Game_Prefabs_HaveCoordinatedMeetingData_RO_BufferLookup = state.GetBufferLookup<HaveCoordinatedMeetingData>(true);
            this.__Game_Citizens_Household_RO_ComponentLookup = state.GetComponentLookup<Household>(true);
            this.__Game_Citizens_HouseholdCitizen_RO_BufferLookup = state.GetBufferLookup<HouseholdCitizen>(true);
            this.__Game_Vehicles_OwnedVehicle_RO_BufferLookup = state.GetBufferLookup<OwnedVehicle>(true);
            this.__Game_Citizens_CurrentBuilding_RO_ComponentLookup = state.GetComponentLookup<CurrentBuilding>(true);
            this.__Game_Buildings_CitizenPresence_RW_ComponentLookup = state.GetComponentLookup<CitizenPresence>();
            this.__Game_Buildings_PropertyRenter_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PropertyRenter>(true);
            this.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(true);
            this.__Game_Vehicles_OwnedVehicle_RO_BufferTypeHandle = state.GetBufferTypeHandle<OwnedVehicle>(true);
            this.__Game_Economy_Resources_RW_BufferTypeHandle = state.GetBufferTypeHandle<Game.Economy.Resources>();
            this.__Game_Prefabs_DeliveryTruckData_RO_ComponentLookup = state.GetComponentLookup<DeliveryTruckData>(true);
            this.__Game_Companies_TransportCompanyData_RO_ComponentLookup = state.GetComponentLookup<TransportCompanyData>(true);
            this.__Game_Simulation_ServiceRequest_RO_ComponentLookup = state.GetComponentLookup<ServiceRequest>(true);
            this.__Game_Prefabs_ActivityLocationElement_RO_BufferLookup = state.GetBufferLookup<ActivityLocationElement>(true);
            this.__Game_Vehicles_TruckSchedule_RW_ComponentLookup = state.GetComponentTypeHandle<TruckSchedule>(false);
        }
    }
}
