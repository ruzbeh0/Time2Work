using Colossal.Entities;
using Game;
using Game.Agents;
using Game.Areas;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Events;
using Game.Net;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Routes;
using Game.Simulation;
using Game.Tools;
using Game.Vehicles;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Time2Work.Bridge;
using Time2Work.Components;
using Time2Work.Systems;
using Time2Work.Utils;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static Game.Prefabs.TriggerPrefabData;
using static Time2Work.Time2WorkWorkerSystem;

namespace Time2Work
{
    public partial class Time2WorkLeisureSystem : GameSystemBase
    {
        public static readonly int kUpdatePerDay = 4096 /*0x1000*/;
        public static readonly float kUpdateInterval = 5f;
        private SimulationSystem m_SimulationSystem;
        private EndFrameBarrier m_EndFrameBarrier;
        private PathfindSetupSystem m_PathFindSetupSystem;
        private Time2WorkTimeSystem m_TimeSystem;
        private ResourceSystem m_ResourceSystem;
        private ClimateSystem m_ClimateSystem;
        private AddMeetingSystem m_AddMeetingSystem;
        private CityProductionStatisticSystem m_CityProductionStatisticSystem;
        private CityConfigurationSystem m_CityConfigurationSystem;
        private EntityQuery m_LeisureQuery;
        private EntityQuery m_EconomyParameterQuery;
        private EntityQuery m_LeisureParameterQuery;
        private EntityQuery m_TripPriorityParametersQuery;
        private EntityQuery m_ResidentPrefabQuery;
        private EntityQuery m_TimeDataQuery;
        private EntityQuery m_PopulationQuery;
        private EntityQuery m_CarPrefabQuery;
        private ComponentTypeSet m_PathfindTypes;
        private NativeQueue<LeisureEvent> m_LeisureQueue;
        private NativeQueue<LeisureLogEvent> m_LeisureLogQueue;
        private readonly Dictionary<LeisureType, LeisureHourlyTotals> m_LeisureHourlyTotals = new Dictionary<LeisureType, LeisureHourlyTotals>();
        private DateTime m_LeisureLogHourStart;
        private bool m_LeisureLogHourInitialized;
        private bool m_LeisureLogSettingInitialized;
        private bool m_LastLeisureLogEnabled;
        private PersonalCarSelectData m_PersonalCarSelectData;
        private Time2WorkLeisureSystem.TypeHandle __TypeHandle;
        private Setting.DTSimulationEnum m_daytype;

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            return 262144 /*0x040000*/ / Time2WorkLeisureSystem.kUpdatePerDay;
        }

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
            this.m_CityProductionStatisticSystem = this.World.GetOrCreateSystemManaged<CityProductionStatisticSystem>();
            this.m_CityConfigurationSystem = this.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
            this.m_PersonalCarSelectData = new PersonalCarSelectData((SystemBase)this);
            this.m_EconomyParameterQuery = this.GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
            this.m_LeisureParameterQuery = this.GetEntityQuery(ComponentType.ReadOnly<LeisureParametersData>());
            this.m_TripPriorityParametersQuery = this.GetEntityQuery(ComponentType.ReadOnly<TripPriorityParametersData>());
            this.m_LeisureQuery = this.GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[4]
             {
                   ComponentType.ReadWrite<Citizen>(),
                   ComponentType.ReadWrite<Leisure>(),
                   ComponentType.ReadWrite<TripNeeded>(),
                   ComponentType.ReadWrite<CurrentBuilding>(),
             },
                Any = new ComponentType[0]
               {

               },
                None = new ComponentType[3]
             {
                ComponentType.ReadOnly<HealthProblem>(),
                ComponentType.ReadOnly<Deleted>(),
                ComponentType.ReadOnly<Temp>(),
             }
            });
            this.m_ResidentPrefabQuery = this.GetEntityQuery(ComponentType.ReadOnly<ObjectData>(), ComponentType.ReadOnly<HumanData>(), ComponentType.ReadOnly<ResidentData>(), ComponentType.ReadOnly<PrefabData>());
            this.m_TimeDataQuery = this.GetEntityQuery(ComponentType.ReadOnly<TimeData>());
            this.m_PopulationQuery = this.GetEntityQuery(ComponentType.ReadOnly<Population>());
            this.m_CarPrefabQuery = this.GetEntityQuery(PersonalCarSelectData.GetEntityQueryDesc());
            this.m_PathfindTypes = new ComponentTypeSet(ComponentType.ReadWrite<PathInformation>(), ComponentType.ReadWrite<PathElement>());
            this.m_LeisureQueue = new NativeQueue<LeisureEvent>((AllocatorManager.AllocatorHandle)Allocator.Persistent);
            this.m_LeisureLogQueue = new NativeQueue<LeisureLogEvent>((AllocatorManager.AllocatorHandle)Allocator.Persistent);
            this.RequireForUpdate(this.m_LeisureQuery);
            this.RequireForUpdate(this.m_EconomyParameterQuery);
            this.RequireForUpdate(this.m_LeisureParameterQuery);
            this.RequireForUpdate(this.m_TripPriorityParametersQuery);
            this.m_daytype = WeekSystem.currentDayOfTheWeek;
        }

        protected override void OnDestroy()
        {
            this.Dependency.Complete();

            if (this.m_LeisureQueue.IsCreated)
            {
                this.m_LeisureQueue.Dispose();
            }
            if (this.m_LeisureLogQueue.IsCreated)
            {
                this.m_LeisureLogQueue.Dispose();
            }

            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            uint frameWithInterval = SimulationUtils.GetUpdateFrameWithInterval(this.m_SimulationSystem.frameIndex, (uint)this.GetUpdateInterval(SystemUpdatePhase.GameSimulation), 16);
            float num = this.m_ClimateSystem.precipitation.value;

            this.m_daytype = WeekSystem.currentDayOfTheWeek;
            JobHandle jobHandle1;
            this.m_PersonalCarSelectData.PreUpdate((SystemBase)this, this.m_CityConfigurationSystem, this.m_CarPrefabQuery, Allocator.TempJob, out jobHandle1);

            JobHandle outJobHandle;
            JobHandle deps1;

            NativeList<ArchetypeChunk> humanChunks =
                this.m_ResidentPrefabQuery.ToArchetypeChunkListAsync(
                Allocator.Persistent,
                out outJobHandle);

            DateTime currentDateTime = World.GetExistingSystemManaged<Time2WorkTimeSystem>().GetCurrentDateTime();
            int hour = currentDateTime.Hour;
            bool logLeisure = Mod.m_Setting != null && Mod.m_Setting.shopping_log_enabled;
            int socialTripsConversionChancePercent = SocialTripsBridge.GetLeisureTripConversionChancePercent();
            LogLeisureSettingState(logLeisure, currentDateTime);

            JobHandle jobHandle2 = new Time2WorkLeisureSystem.LeisureJob()
            {
                CitizenScheduleLookup = InternalCompilerInterface.GetComponentLookup<CitizenSchedule>(ref this.__TypeHandle.CitizenScheduleLookup, ref this.CheckedStateRef),
                m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref this.__TypeHandle.__Unity_Entities_Entity_TypeHandle, ref this.CheckedStateRef),
                m_LeisureType = InternalCompilerInterface.GetComponentTypeHandle<Leisure>(ref this.__TypeHandle.__Game_Citizens_Leisure_RW_ComponentTypeHandle, ref this.CheckedStateRef),
                m_HouseholdMemberType = InternalCompilerInterface.GetComponentTypeHandle<HouseholdMember>(ref this.__TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentTypeHandle, ref this.CheckedStateRef),
                m_UpdateFrameType = InternalCompilerInterface.GetSharedComponentTypeHandle<UpdateFrame>(ref this.__TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, ref this.CheckedStateRef),
                m_TripType = InternalCompilerInterface.GetBufferTypeHandle<TripNeeded>(ref this.__TypeHandle.__Game_Citizens_TripNeeded_RW_BufferTypeHandle, ref this.CheckedStateRef),
                m_CreatureDataType = InternalCompilerInterface.GetComponentTypeHandle<CreatureData>(ref this.__TypeHandle.__Game_Prefabs_CreatureData_RO_ComponentTypeHandle, ref this.CheckedStateRef),
                m_ResidentDataType = InternalCompilerInterface.GetComponentTypeHandle<ResidentData>(ref this.__TypeHandle.__Game_Prefabs_ResidentData_RO_ComponentTypeHandle, ref this.CheckedStateRef),
                m_PathInfos = InternalCompilerInterface.GetComponentLookup<PathInformation>(ref this.__TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentLookup, ref this.CheckedStateRef),
                m_CurrentBuildings = InternalCompilerInterface.GetComponentLookup<CurrentBuilding>(ref this.__TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentLookup, ref this.CheckedStateRef),
                m_BuildingData = InternalCompilerInterface.GetComponentLookup<Building>(ref this.__TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref this.CheckedStateRef),
                m_PropertyRenters = InternalCompilerInterface.GetComponentLookup<PropertyRenter>(ref this.__TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup, ref this.CheckedStateRef),
                m_CarKeepers = InternalCompilerInterface.GetComponentLookup<CarKeeper>(ref this.__TypeHandle.__Game_Citizens_CarKeeper_RO_ComponentLookup, ref this.CheckedStateRef),
                m_BicycleOwners = InternalCompilerInterface.GetComponentLookup<BicycleOwner>(ref this.__TypeHandle.__Game_Citizens_BicycleOwner_RO_ComponentLookup, ref this.CheckedStateRef),
                m_ParkedCarData = InternalCompilerInterface.GetComponentLookup<ParkedCar>(ref this.__TypeHandle.__Game_Vehicles_ParkedCar_RO_ComponentLookup, ref this.CheckedStateRef),
                m_PersonalCarData = InternalCompilerInterface.GetComponentLookup<Game.Vehicles.PersonalCar>(ref this.__TypeHandle.__Game_Vehicles_PersonalCar_RO_ComponentLookup, ref this.CheckedStateRef),
                m_Targets = InternalCompilerInterface.GetComponentLookup<Game.Common.Target>(ref this.__TypeHandle.__Game_Common_Target_RO_ComponentLookup, ref this.CheckedStateRef),
                m_PrefabRefs = InternalCompilerInterface.GetComponentLookup<PrefabRef>(ref this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref this.CheckedStateRef),
                m_LeisureProviderDatas = InternalCompilerInterface.GetComponentLookup<LeisureProviderData>(ref this.__TypeHandle.__Game_Prefabs_LeisureProviderData_RO_ComponentLookup, ref this.CheckedStateRef),
                m_Students = InternalCompilerInterface.GetComponentLookup<Game.Citizens.Student>(ref this.__TypeHandle.__Game_Citizens_Student_RO_ComponentLookup, ref this.CheckedStateRef),
                m_Workers = InternalCompilerInterface.GetComponentLookup<Worker>(ref this.__TypeHandle.__Game_Citizens_Worker_RO_ComponentLookup, ref this.CheckedStateRef),
                m_Households = InternalCompilerInterface.GetComponentLookup<Household>(ref this.__TypeHandle.__Game_Citizens_Household_RO_ComponentLookup, ref this.CheckedStateRef),
                m_Resources = InternalCompilerInterface.GetBufferLookup<Game.Economy.Resources>(ref this.__TypeHandle.__Game_Economy_Resources_RO_BufferLookup, ref this.CheckedStateRef),
                m_CitizenDatas = InternalCompilerInterface.GetComponentLookup<Citizen>(ref this.__TypeHandle.__Game_Citizens_Citizen_RW_ComponentLookup, ref this.CheckedStateRef),
                m_Renters = InternalCompilerInterface.GetComponentLookup<PropertyRenter>(ref this.__TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup, ref this.CheckedStateRef),
                m_PrefabCarData = InternalCompilerInterface.GetComponentLookup<CarData>(ref this.__TypeHandle.__Game_Prefabs_CarData_RO_ComponentLookup, ref this.CheckedStateRef),
                m_ObjectGeometryData = InternalCompilerInterface.GetComponentLookup<ObjectGeometryData>(ref this.__TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref this.CheckedStateRef),
                m_PrefabHumanData = InternalCompilerInterface.GetComponentLookup<HumanData>(ref this.__TypeHandle.__Game_Prefabs_HumanData_RO_ComponentLookup, ref this.CheckedStateRef),
                m_Purposes = InternalCompilerInterface.GetComponentLookup<TravelPurpose>(ref this.__TypeHandle.__Game_Citizens_TravelPurpose_RO_ComponentLookup, ref this.CheckedStateRef),
                m_OutsideConnectionDatas = InternalCompilerInterface.GetComponentLookup<OutsideConnectionData>(ref this.__TypeHandle.__Game_Prefabs_OutsideConnectionData_RO_ComponentLookup, ref this.CheckedStateRef),
                m_TouristHouseholds = InternalCompilerInterface.GetComponentLookup<TouristHousehold>(ref this.__TypeHandle.__Game_Citizens_TouristHousehold_RO_ComponentLookup, ref this.CheckedStateRef),
                m_IndustrialProcesses = InternalCompilerInterface.GetComponentLookup<IndustrialProcessData>(ref this.__TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup, ref this.CheckedStateRef),
                m_ServiceAvailables = InternalCompilerInterface.GetComponentLookup<ServiceAvailable>(ref this.__TypeHandle.__Game_Companies_ServiceAvailable_RO_ComponentLookup, ref this.CheckedStateRef),
                m_PopulationData = InternalCompilerInterface.GetComponentLookup<Population>(ref this.__TypeHandle.__Game_City_Population_RO_ComponentLookup, ref this.CheckedStateRef),
                m_HouseholdCitizens = InternalCompilerInterface.GetBufferLookup<HouseholdCitizen>(ref this.__TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup, ref this.CheckedStateRef),
                m_RenterBufs = InternalCompilerInterface.GetBufferLookup<Renter>(ref this.__TypeHandle.__Game_Buildings_Renter_RO_BufferLookup, ref this.CheckedStateRef),
                m_ConsumptionDatas = InternalCompilerInterface.GetComponentLookup<ConsumptionData>(ref this.__TypeHandle.__Game_Prefabs_ConsumptionData_RO_ComponentLookup, ref this.CheckedStateRef),
                m_CurrentDistrictData = InternalCompilerInterface.GetComponentLookup<CurrentDistrict>(ref this.__TypeHandle.__Game_Areas_CurrentDistrict_RO_ComponentLookup, ref this.CheckedStateRef),
                m_DistrictModifiers = InternalCompilerInterface.GetBufferLookup<DistrictModifier>(ref this.__TypeHandle.__Game_Areas_DistrictModifier_RO_BufferLookup, ref this.CheckedStateRef),
                m_Shopping = InternalCompilerInterface.GetComponentLookup<Shopper>(ref this.__TypeHandle.__Game_Citizens_Shopping_RW_ComponentLookup, ref this.CheckedStateRef),
                m_SpecialEventDatas = InternalCompilerInterface.GetComponentLookup<SpecialEventData>(ref this.__TypeHandle.__Game_Citizens_SpecialEventData_RO_ComponentLookup, ref this.CheckedStateRef),
                m_ResourceBuyers = InternalCompilerInterface.GetComponentLookup<ResourceBuyer>(ref this.__TypeHandle.__Game_Companies_ResourceBuyer_RO_ComponentLookup, ref this.CheckedStateRef),
                m_Transforms = InternalCompilerInterface.GetComponentLookup<Game.Objects.Transform>(ref this.__TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref this.CheckedStateRef),
                m_SocialLeisureOpportunities = InternalCompilerInterface.GetComponentLookup<SocialLeisureOpportunity>(ref this.__TypeHandle.__Time2Work_Components_SocialLeisureOpportunity_RO_ComponentLookup, ref this.CheckedStateRef),
                m_EconomyParameters = this.m_EconomyParameterQuery.GetSingleton<EconomyParameterData>(),
                m_TripPriorityParameters = this.m_TripPriorityParametersQuery.GetSingleton<TripPriorityParametersData>(),
                m_SimulationFrame = this.m_SimulationSystem.frameIndex,
                m_TimeOfDay = this.m_TimeSystem.normalizedTime,
                m_SocialTripsMacroAvailable = socialTripsConversionChancePercent > 0,
                m_SocialTripsConversionChancePercent = socialTripsConversionChancePercent,
                CommercialPropertyLookup = InternalCompilerInterface.GetComponentLookup<CommercialProperty>(ref this.__TypeHandle.CommercialPropertyLookup, ref this.CheckedStateRef),
                IndustrialPropertyLookup = InternalCompilerInterface.GetComponentLookup<IndustrialProperty>(ref this.__TypeHandle.IndustrialPropertyLookup, ref this.CheckedStateRef),
                OfficePropertyLookup = InternalCompilerInterface.GetComponentLookup<OfficeProperty>(ref this.__TypeHandle.OfficePropertyLookup, ref this.CheckedStateRef),
                PropertyRenterLookup = InternalCompilerInterface.GetComponentLookup<PropertyRenter>(ref this.__TypeHandle.PropertyRenterLookup, ref this.CheckedStateRef),
                PrefabRefLookup = InternalCompilerInterface.GetComponentLookup<PrefabRef>(ref this.__TypeHandle.PrefabRefLookup, ref this.CheckedStateRef),
                m_UpdateFrameIndex = frameWithInterval,
                m_Weather = num,
                m_Temperature = ((float)this.m_ClimateSystem.temperature),
                m_RandomSeed = RandomSeed.Next(),
                m_PathfindTypes = this.m_PathfindTypes,
                m_HumanChunks = humanChunks,
                m_PersonalCarSelectData = this.m_PersonalCarSelectData,
                m_PathfindQueue = this.m_PathFindSetupSystem.GetQueue((object)this, 512).AsParallelWriter(),
                m_CommandBuffer = this.m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
                m_MeetingQueue = this.m_AddMeetingSystem.GetMeetingQueue(out deps1).AsParallelWriter(),
                m_LeisureQueue = this.m_LeisureQueue.AsParallelWriter(),
                m_LeisureLogQueue = this.m_LeisureLogQueue.AsParallelWriter(),
                m_LogLeisure = logLeisure,
                m_TimeData = this.m_TimeDataQuery.GetSingleton<TimeData>(),
                m_PopulationEntity = this.m_PopulationQuery.GetSingletonEntity(),
                lunch_break_pct = Mod.m_Setting.lunch_break_percentage,
                office_offdayprob = WeekSystem.getOfficeOffDayProb(),
                commercial_offdayprob = WeekSystem.getCommercialOffDayProb(),
                industry_offdayprob = WeekSystem.getIndustryOffDayProb(),
                cityservices_offdayprob = WeekSystem.getCityServicesOffDayProb(),
                school_start_time = new int3((int)Mod.m_Setting.school_start_time, (int)Mod.m_Setting.high_school_start_time, (int)Mod.m_Setting.univ_start_time),
                school_end_time = new int3((int)Mod.m_Setting.school_end_time, (int)Mod.m_Setting.high_school_end_time, (int)Mod.m_Setting.univ_end_time),
                work_start_time = (float)Mod.m_Setting.work_start_time,
                work_end_time = (float)Mod.m_Setting.work_end_time,
                delayFactor = (float)(Mod.m_Setting.delay_factor) / 100,
                school_offdayprob = WeekSystem.getSchoolOffDayProb(),
                ticksPerDay = Time2WorkTimeSystem.kTicksPerDay,
                part_time_prob = Mod.m_Setting.part_time_percentage,
                commute_top10 = Mod.m_Setting.commute_top10per,
                dow = this.m_daytype,
                part_time_reduction = Mod.m_Setting.avg_work_hours_pt_wd / Mod.m_Setting.avg_work_hours_ft_wd,
                overtime = (Mod.m_Setting.avg_work_hours_ft_wd - (Mod.m_Setting.work_end_time - Mod.m_Setting.work_start_time) / 2)/24,
                meals_leisure = new float4(Mod.m_Setting.meals_weekday, Mod.m_Setting.meals_avgday, Mod.m_Setting.meals_saturday, Mod.m_Setting.meals_sunday),
                entertainment_leisure = new float4(Mod.m_Setting.entertainment_weekday, Mod.m_Setting.entertainment_avgday, Mod.m_Setting.entertainment_saturday, Mod.m_Setting.entertainment_sunday),
                shopping_leisure = new float4(Mod.m_Setting.shopping_weekday, Mod.m_Setting.shopping_avgday, Mod.m_Setting.shopping_saturday, Mod.m_Setting.shopping_sunday),
                park_leisure = new float4(Mod.m_Setting.park_weekday, Mod.m_Setting.park_avgday, Mod.m_Setting.park_saturday, Mod.m_Setting.park_sunday),
                travel_leisure = new float4(Mod.m_Setting.travel_weekday, Mod.m_Setting.travel_avgday, Mod.m_Setting.travel_saturday, Mod.m_Setting.travel_sunday),
                day = Time2WorkTimeSystem.GetDay(m_SimulationSystem.frameIndex, m_TimeDataQuery.GetSingleton<TimeData>(), Time2WorkTimeSystem.kTicksPerDay),
                avg_time_beverages = Mod.m_Setting.avg_time_beverages,
                avg_time_chemicals = Mod.m_Setting.avg_time_chemicals,
                avg_time_convenienceFood = Mod.m_Setting.avg_time_convenienceFood,
                avg_time_electronics = Mod.m_Setting.avg_time_electronics,
                avg_time_software = Mod.m_Setting.avg_time_software,
                avg_time_financial = Mod.m_Setting.avg_time_financial,
                avg_time_food = Mod.m_Setting.avg_time_food,
                avg_time_furniture = Mod.m_Setting.avg_time_furniture,
                avg_time_meals = Mod.m_Setting.avg_time_meals,
                avg_time_media = Mod.m_Setting.avg_time_media,
                avg_time_paper = Mod.m_Setting.avg_time_paper,
                avg_time_petrochemicals = Mod.m_Setting.avg_time_petrochemicals,
                avg_time_pharmaceuticals = Mod.m_Setting.avg_time_pharmaceuticals,
                avg_time_plastics = Mod.m_Setting.avg_time_plastics,
                avg_time_telecom = Mod.m_Setting.avg_time_telecom,
                avg_time_textiles = Mod.m_Setting.avg_time_textiles,
                avg_time_recreation = Mod.m_Setting.avg_time_recreation,
                avg_time_entertainment = Mod.m_Setting.avg_time_entertainment,
                avg_time_vehicles = Mod.m_Setting.avg_time_vehicles,
                remote_work_prob = Mod.m_Setting.remote_percentage,
                //Meals = 0, Entertainment = 1,Shopping = 2,Park = 3,Travel = 4
                meal_hourly_factor = LeisureProbabilityCalculator.GetMealsProbability((int)Mod.m_Setting.settings_choice, (int)this.m_daytype, hour),
                entertainment_hourly_factor = LeisureProbabilityCalculator.GetEntertainmentProbability((int)Mod.m_Setting.settings_choice, (int)this.m_daytype, hour),
                shopping_hourly_factor = LeisureProbabilityCalculator.GetShoppingProbability((int)Mod.m_Setting.settings_choice, (int)this.m_daytype, hour),
                park_hourly_factor = LeisureProbabilityCalculator.GetParkProbability((int)Mod.m_Setting.settings_choice, (int)this.m_daytype, hour),
                travel_hourly_factor = LeisureProbabilityCalculator.GetTravelProbability((int)Mod.m_Setting.settings_choice, (int)this.m_daytype, hour)
            }.ScheduleParallel<Time2WorkLeisureSystem.LeisureJob>(
                this.m_LeisureQuery,
                JobUtils.CombineDependencies(this.Dependency, outJobHandle, deps1, jobHandle1));

            this.m_EndFrameBarrier.AddJobHandleForProducer(jobHandle2);
            this.m_PathFindSetupSystem.AddQueueWriter(jobHandle2);

            JobHandle disposeHumanChunks = humanChunks.Dispose(jobHandle2);
            JobHandle deps2;
            JobHandle jobHandle3 = new Time2WorkLeisureSystem.SpendLeisurejob()
            {
                m_ServiceAvailables = this.__TypeHandle.__Game_Companies_ServiceAvailable_RW_ComponentLookup,
                m_CompanyStatisticDatas = InternalCompilerInterface.GetComponentLookup<CompanyStatisticData>(ref this.__TypeHandle.__Game_Companies_CompanyStatisticData_RW_ComponentLookup, ref this.CheckedStateRef),
                m_Resources = this.__TypeHandle.__Game_Economy_Resources_RW_BufferLookup,
                m_CitizenDatas = InternalCompilerInterface.GetComponentLookup<Citizen>(ref this.__TypeHandle.__Game_Citizens_Citizen_RW_ComponentLookup, ref this.CheckedStateRef),
                m_HouseholdMembers = this.__TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentLookup,
                m_IndustrialProcesses = this.__TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup,
                m_Prefabs = this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup,
                m_ResourceDatas = this.__TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup,
                m_ServiceCompanyDatas = this.__TypeHandle.__Game_Companies_ServiceCompanyData_RO_ComponentLookup,
                m_LeisureProviderDatas = this.__TypeHandle.__Game_Prefabs_LeisureProviderData_RO_ComponentLookup,
                m_ResourcePrefabs = this.m_ResourceSystem.GetPrefabs(),
                m_CitizensConsumptionAccumulator = this.m_CityProductionStatisticSystem.GetCityResourceUsageAccumulator(CityProductionStatisticSystem.CityResourceUsage.Consumer.Citizens, out deps2),
                m_LeisureQueue = this.m_LeisureQueue,
                m_LeisureLogQueue = this.m_LeisureLogQueue.AsParallelWriter(),
                m_LogLeisure = logLeisure
            }.Schedule<Time2WorkLeisureSystem.SpendLeisurejob>(JobHandle.CombineDependencies(jobHandle2, deps2));
            this.m_ResourceSystem.AddPrefabsReader(jobHandle3);

            this.m_CityProductionStatisticSystem.AddCityUsageAccumulatorWriter(
                CityProductionStatisticSystem.CityResourceUsage.Consumer.Citizens,
                jobHandle3);

            this.Dependency = JobHandle.CombineDependencies(jobHandle3, disposeHumanChunks);
            this.m_PersonalCarSelectData.PostUpdate(this.Dependency);

            if (logLeisure)
            {
                this.Dependency.Complete();
                DrainLeisureLogs(currentDateTime);
            }
            else
            {
                ClearLeisureLogState();
            }
        }

        private void DrainLeisureLogs(DateTime now)
        {
            EnsureLeisureLogHour(now);

            while (this.m_LeisureLogQueue.TryDequeue(out LeisureLogEvent logEvent))
            {
                if (!this.m_LeisureHourlyTotals.TryGetValue(logEvent.leisureType, out LeisureHourlyTotals totals))
                {
                    totals = default;
                }

                totals.startedTrips += logEvent.startedTrips;
                totals.completedTrips += logEvent.completedTrips;
                totals.failedTrips += logEvent.failedTrips;
                totals.failedInactive += logEvent.failedInactive;
                totals.failedNoService += logEvent.failedNoService;
                totals.failedNoResources += logEvent.failedNoResources;
                totals.mealVisits += logEvent.mealVisits;
                totals.resourceAmount += logEvent.resourceAmount;
                totals.cost += logEvent.cost;
                totals.durationMinutes += logEvent.durationMinutes;
                totals.resourceEvents += logEvent.resourceEvents;
                this.m_LeisureHourlyTotals[logEvent.leisureType] = totals;
            }
        }

        private void LogLeisureSettingState(bool logLeisure, DateTime now)
        {
            if (this.m_LeisureLogSettingInitialized && this.m_LastLeisureLogEnabled == logLeisure)
                return;

            this.m_LeisureLogSettingInitialized = true;
            this.m_LastLeisureLogEnabled = logLeisure;

            if (logLeisure)
            {
                EnsureLeisureLogHour(now);
                Mod.log.Info($"Leisure diagnostics log enabled at {now:yyyy-MM-dd HH:mm}; first LeisureHourlyLog is written when the in-game hour changes.");
            }
            else
            {
                Mod.log.Info("Leisure diagnostics log disabled.");
            }
        }

        private void EnsureLeisureLogHour(DateTime now)
        {
            DateTime currentHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0);
            if (!this.m_LeisureLogHourInitialized)
            {
                this.m_LeisureLogHourStart = currentHour;
                this.m_LeisureLogHourInitialized = true;
                return;
            }

            if (currentHour != this.m_LeisureLogHourStart)
            {
                FlushLeisureHourlyTotals();
                this.m_LeisureLogHourStart = currentHour;
            }
        }

        private void FlushLeisureHourlyTotals()
        {
            if (!this.m_LeisureLogHourInitialized)
                return;

            int totalStartedTrips = 0;
            int totalCompletedTrips = 0;
            int totalFailedTrips = 0;
            int totalFailedInactive = 0;
            int totalFailedNoService = 0;
            int totalFailedNoResources = 0;
            int totalMealVisits = 0;
            int totalResourceAmount = 0;
            int totalCost = 0;
            int totalResourceEvents = 0;
            float totalDuration = 0f;

            foreach (LeisureHourlyTotals totals in this.m_LeisureHourlyTotals.Values)
            {
                totalStartedTrips += totals.startedTrips;
                totalCompletedTrips += totals.completedTrips;
                totalFailedTrips += totals.failedTrips;
                totalFailedInactive += totals.failedInactive;
                totalFailedNoService += totals.failedNoService;
                totalFailedNoResources += totals.failedNoResources;
                totalMealVisits += totals.mealVisits;
                totalResourceAmount += totals.resourceAmount;
                totalCost += totals.cost;
                totalResourceEvents += totals.resourceEvents;
                totalDuration += totals.durationMinutes;
            }

            StringBuilder typeSummary = new StringBuilder();
            foreach (LeisureType leisureType in Enum.GetValues(typeof(LeisureType)))
            {
                if (!this.m_LeisureHourlyTotals.TryGetValue(leisureType, out LeisureHourlyTotals totals) || (totals.startedTrips == 0 && totals.completedTrips == 0 && totals.failedTrips == 0 && totals.resourceEvents == 0))
                    continue;

                if (typeSummary.Length > 0)
                    typeSummary.Append("; ");

                float avgTypeDuration = totals.completedTrips > 0 ? totals.durationMinutes / totals.completedTrips : 0f;
                typeSummary.Append(leisureType);
                typeSummary.Append("(started=");
                typeSummary.Append(totals.startedTrips);
                typeSummary.Append(",completed=");
                typeSummary.Append(totals.completedTrips);
                typeSummary.Append(",failed=");
                typeSummary.Append(totals.failedTrips);
                if (totals.failedInactive > 0)
                {
                    typeSummary.Append(",failedInactive=");
                    typeSummary.Append(totals.failedInactive);
                }
                if (totals.failedNoService > 0)
                {
                    typeSummary.Append(",failedNoService=");
                    typeSummary.Append(totals.failedNoService);
                }
                if (totals.failedNoResources > 0)
                {
                    typeSummary.Append(",failedNoResources=");
                    typeSummary.Append(totals.failedNoResources);
                }
                if (totals.mealVisits > 0)
                {
                    typeSummary.Append(",mealVisits=");
                    typeSummary.Append(totals.mealVisits);
                }
                typeSummary.Append(",amount=");
                typeSummary.Append(totals.resourceAmount);
                typeSummary.Append(",spend=");
                typeSummary.Append(totals.cost);
                typeSummary.Append(",resourceEvents=");
                typeSummary.Append(totals.resourceEvents);
                typeSummary.Append(",avgDuration=");
                typeSummary.Append(FormatLogFloat(avgTypeDuration));
                typeSummary.Append(")");
            }

            float avgDuration = totalCompletedTrips > 0 ? totalDuration / totalCompletedTrips : 0f;
            string types = typeSummary.Length > 0 ? typeSummary.ToString() : "none";

            Mod.log.Info(
                $"LeisureHourlyLog hour={this.m_LeisureLogHourStart:yyyy-MM-dd HH}:00 started={totalStartedTrips} completed={totalCompletedTrips} failed={totalFailedTrips} failedInactive={totalFailedInactive} failedNoService={totalFailedNoService} failedNoResources={totalFailedNoResources} mealVisits={totalMealVisits} amount={totalResourceAmount} spend={totalCost} resourceEvents={totalResourceEvents} avgDurationMinutes={FormatLogFloat(avgDuration)} types={types}");

            this.m_LeisureHourlyTotals.Clear();
        }

        private static string FormatLogFloat(float value)
        {
            return value.ToString("F1", CultureInfo.InvariantCulture);
        }

        private void ClearLeisureLogState()
        {
            while (this.m_LeisureLogQueue.IsCreated && this.m_LeisureLogQueue.TryDequeue(out LeisureLogEvent _))
            {
            }

            this.m_LeisureHourlyTotals.Clear();
            this.m_LeisureLogHourInitialized = false;
        }

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

        public Time2WorkLeisureSystem()
        {
        }

        private struct LeisureLogEvent
        {
            public LeisureType leisureType;
            public Resource resource;
            public int startedTrips;
            public int completedTrips;
            public int failedTrips;
            public int failedInactive;
            public int failedNoService;
            public int failedNoResources;
            public int mealVisits;
            public int resourceAmount;
            public int cost;
            public int resourceEvents;
            public float durationMinutes;
        }

        private struct LeisureHourlyTotals
        {
            public int startedTrips;
            public int completedTrips;
            public int failedTrips;
            public int failedInactive;
            public int failedNoService;
            public int failedNoResources;
            public int mealVisits;
            public int resourceAmount;
            public int cost;
            public int resourceEvents;
            public float durationMinutes;
        }

        [BurstCompile]
        private struct SpendLeisurejob : IJob
        {
            public NativeQueue<LeisureEvent> m_LeisureQueue;
            public NativeQueue<LeisureLogEvent>.ParallelWriter m_LeisureLogQueue;
            public ComponentLookup<ServiceAvailable> m_ServiceAvailables;
            public ComponentLookup<CompanyStatisticData> m_CompanyStatisticDatas;
            public BufferLookup<Game.Economy.Resources> m_Resources;
            public ComponentLookup<Citizen> m_CitizenDatas;
            [ReadOnly]
            public ComponentLookup<PrefabRef> m_Prefabs;
            [ReadOnly]
            public ComponentLookup<IndustrialProcessData> m_IndustrialProcesses;
            [ReadOnly]
            public ComponentLookup<LeisureProviderData> m_LeisureProviderDatas;
            [ReadOnly]
            public ComponentLookup<HouseholdMember> m_HouseholdMembers;
            [ReadOnly]
            public ComponentLookup<ResourceData> m_ResourceDatas;
            [ReadOnly]
            public ComponentLookup<ServiceCompanyData> m_ServiceCompanyDatas;
            [ReadOnly]
            public ResourcePrefabs m_ResourcePrefabs;
            public NativeArray<int> m_CitizensConsumptionAccumulator;
            public bool m_LogLeisure;

            public void Execute()
            {
                LeisureEvent leisureEvent;

                while (this.m_LeisureQueue.TryDequeue(out leisureEvent))
                {
                    if (this.m_CitizenDatas.HasComponent(leisureEvent.m_Citizen))
                    {
                        Citizen citizenData = this.m_CitizenDatas[leisureEvent.m_Citizen];
                        int num = (int)math.ceil((float)leisureEvent.m_Efficiency / Time2WorkLeisureSystem.kUpdateInterval);
                        citizenData.m_LeisureCounter = (byte)math.min((int)byte.MaxValue, (int)citizenData.m_LeisureCounter + num);
                        this.m_CitizenDatas[leisureEvent.m_Citizen] = citizenData;
                    }
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
                                int y = 0;
                                float num1 = 1f;
                                if (this.m_ServiceAvailables.HasComponent(leisureEvent.m_Provider) && this.m_ServiceCompanyDatas.HasComponent(prefab))
                                {
                                    ServiceAvailable serviceAvailable = this.m_ServiceAvailables[leisureEvent.m_Provider];
                                    ServiceCompanyData serviceCompanyData = this.m_ServiceCompanyDatas[prefab];
                                    y = math.max((int)((double)serviceCompanyData.m_ServiceConsuming / (double)Time2WorkLeisureSystem.kUpdateInterval), 1);
                                    if (serviceAvailable.m_ServiceAvailable > 0)
                                    {
                                        serviceAvailable.m_ServiceAvailable -= y;
                                        serviceAvailable.m_MeanPriority = math.lerp(serviceAvailable.m_MeanPriority, (float)serviceAvailable.m_ServiceAvailable / (float)serviceCompanyData.m_MaxService, 0.1f);
                                        this.m_ServiceAvailables[leisureEvent.m_Provider] = serviceAvailable;
                                        num1 = EconomyUtils.GetServicePriceMultiplier((float)serviceAvailable.m_ServiceAvailable, serviceCompanyData.m_MaxService);
                                        if (this.m_CompanyStatisticDatas.HasComponent(leisureEvent.m_Provider))
                                        {
                                            CompanyStatisticData companyStatisticData = this.m_CompanyStatisticDatas[leisureEvent.m_Provider];
                                            ++companyStatisticData.m_CurrentNumberOfCustomers;
                                            this.m_CompanyStatisticDatas[leisureEvent.m_Provider] = companyStatisticData;
                                        }
                                    }
                                    else
                                        flag = true;
                                }
                                if (!flag)
                                {
                                    DynamicBuffer<Game.Economy.Resources> resource2 = this.m_Resources[leisureEvent.m_Provider];
                                    int num2 = math.min(EconomyUtils.GetResources(resource1, resource2), y);
                                    int f = (int)((double)num2 * (double)marketPrice * (double)num1);
                                    DynamicBuffer<Game.Economy.Resources> resource3 = this.m_Resources[household];
                                    EconomyUtils.AddResources(resource1, -num2, resource2);
                                    EconomyUtils.AddResources(Resource.Money, Mathf.RoundToInt((float)f), resource2);
                                    EconomyUtils.AddResources(Resource.Money, -Mathf.RoundToInt((float)f), resource3);
                                    this.m_CitizensConsumptionAccumulator[EconomyUtils.GetResourceIndex(resource1)] += num2;
                                    if (this.m_LogLeisure)
                                    {
                                        LeisureType leisureType = LeisureType.Count;
                                        if (this.m_LeisureProviderDatas.HasComponent(prefab))
                                        {
                                            leisureType = this.m_LeisureProviderDatas[prefab].m_LeisureType;
                                        }

                                        this.m_LeisureLogQueue.Enqueue(new LeisureLogEvent()
                                        {
                                            leisureType = leisureType,
                                            resource = resource1,
                                            resourceAmount = num2,
                                            cost = Mathf.RoundToInt((float)f),
                                            resourceEvents = 1
                                        });
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
            [ReadOnly]
            public ComponentLookup<CitizenSchedule> CitizenScheduleLookup;
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
            public ComponentLookup<BicycleOwner> m_BicycleOwners;
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
            public ComponentLookup<SpecialEventData> m_SpecialEventDatas;
            [ReadOnly]
            public ComponentLookup<ResourceBuyer> m_ResourceBuyers;
            [ReadOnly]
            public ComponentLookup<Game.Objects.Transform> m_Transforms;
            [ReadOnly]
            public ComponentLookup<SocialLeisureOpportunity> m_SocialLeisureOpportunities;
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
            public ComponentLookup<CurrentDistrict> m_CurrentDistrictData;
            [ReadOnly]
            public BufferLookup<DistrictModifier> m_DistrictModifiers;
            [ReadOnly]
            public RandomSeed m_RandomSeed;
            [ReadOnly]
            public ComponentTypeSet m_PathfindTypes;
            [ReadOnly]
            public NativeList<ArchetypeChunk> m_HumanChunks;
            [ReadOnly]
            public PersonalCarSelectData m_PersonalCarSelectData;
            public ComponentLookup<Shopper> m_Shopping;
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
            public TripPriorityParametersData m_TripPriorityParameters;
            public EntityCommandBuffer.ParallelWriter m_CommandBuffer;
            public NativeQueue<SetupQueueItem>.ParallelWriter m_PathfindQueue;
            public NativeQueue<LeisureEvent>.ParallelWriter m_LeisureQueue;
            public NativeQueue<LeisureLogEvent>.ParallelWriter m_LeisureLogQueue;
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
            public int3 school_start_time;
            public int3 school_end_time;
            public float work_start_time;
            public float work_end_time;
            public float delayFactor;
            public float3 school_offdayprob;
            public int ticksPerDay;
            public int part_time_prob;
            public float commute_top10;
            public Setting.DTSimulationEnum dow;
            public float overtime;
            public float part_time_reduction;
            public float4 meals_leisure;
            public float4 entertainment_leisure;
            public float4 shopping_leisure;
            public float4 park_leisure;
            public float4 travel_leisure;
            private float meals_avghour;
            private float entertainment_avghour;
            private float shopping_avghour;
            private float park_avghour;
            private float travel_avghour;
            public int day;
            public int avg_time_beverages;
            public int avg_time_chemicals;
            public int avg_time_convenienceFood;
            public int avg_time_electronics;
            public int avg_time_software;
            public int avg_time_financial;
            public int avg_time_food;
            public int avg_time_furniture;
            public int avg_time_meals;
            public int avg_time_media;
            public int avg_time_paper;
            public int avg_time_petrochemicals;
            public int avg_time_pharmaceuticals;
            public int avg_time_plastics;
            public int avg_time_telecom;
            public int avg_time_textiles;
            public int avg_time_recreation;
            public int avg_time_entertainment;
            public int avg_time_vehicles;
            public float meal_hourly_factor;
            public float entertainment_hourly_factor;
            public float shopping_hourly_factor;
            public float travel_hourly_factor;
            public float park_hourly_factor;
            public int remote_work_prob;
            public bool m_LogLeisure;
            public bool m_SocialTripsMacroAvailable;
            public int m_SocialTripsConversionChancePercent;

            private static float GetElapsed(float start, float end)
            {
                start = math.frac(start);
                end = math.frac(end);
                return end >= start ? end - start : 1f - start + end;
            }

            private static bool IsInWindow(float start, float end, float time)
            {
                start = math.frac(start);
                end = math.frac(end);
                time = math.frac(time);
                return start <= end ? time >= start && time <= end : time >= start || time <= end;
            }

            private bool ShouldCreateSocialLeisureOpportunity(Entity citizen, Citizen citizenData, Entity destination)
            {
                int chancePercent = math.clamp(this.m_SocialTripsConversionChancePercent, 0, 100);
                if (!this.m_SocialTripsMacroAvailable || chancePercent <= 0)
                    return false;
                if (chancePercent >= 100)
                    return true;

                uint seed = (uint)math.max(
                    1,
                    math.abs(citizenData.m_PseudoRandom) +
                    citizen.Index * 397 +
                    destination.Index * 1009 +
                    (int)(this.m_SimulationFrame % 65521u) +
                    17);
                Unity.Mathematics.Random random = Unity.Mathematics.Random.CreateFromIndex(seed);
                return random.NextInt(100) < chancePercent;
            }

            private void StartNormalLeisureTrip(
                int unfilteredChunkIndex,
                Entity citizen,
                DynamicBuffer<TripNeeded> trips,
                Citizen citizenData,
                LeisureProviderData provider,
                Entity destination)
            {
                trips.Add(new TripNeeded()
                {
                    m_TargetAgent = destination,
                    m_Purpose = Game.Citizens.Purpose.Leisure,
                    m_Priority = 128
                });

                if (this.m_LogLeisure)
                {
                    this.m_LeisureLogQueue.Enqueue(new LeisureLogEvent()
                    {
                        leisureType = provider.m_LeisureType,
                        startedTrips = 1
                    });
                }

                this.m_CommandBuffer.AddComponent<Game.Common.Target>(unfilteredChunkIndex, citizen, new Game.Common.Target()
                {
                    m_Target = destination
                });
                this.m_CommandBuffer.RemoveComponent<LeisureSeekerCooldown>(unfilteredChunkIndex, citizen);

                if (destination != Entity.Null && this.m_CurrentBuildings.HasComponent(citizen))
                {
                    shoppingTime(unfilteredChunkIndex, citizen, citizenData, provider.m_LeisureType);
                }
            }

            private float GetPostEventDepartureDelay(Citizen citizen, Entity eventEntity, int day)
            {
                uint seed = (uint)(citizen.m_PseudoRandom + eventEntity.Index * 397 + day * 1009 + 17);
                Unity.Mathematics.Random random = Unity.Mathematics.Random.CreateFromIndex(seed);
                float minutes = math.max(0f, (float)GaussianRandom.NextGaussianDouble(random) * 10f);
                return math.min(minutes, 45f) / 1440f;
            }

            private bool IsEventAttendanceWindow(in SpecialEventData specialEventData, Citizen citizen, Entity eventEntity, int day)
            {
                if (specialEventData.day != day)
                    return false;

                float start = math.frac(specialEventData.start_time);
                float end = math.frac(specialEventData.start_time + specialEventData.duration);
                float departureEnd = math.frac(specialEventData.start_time + specialEventData.duration + GetPostEventDepartureDelay(citizen, eventEntity, day));
                return IsInWindow(start, departureEnd, m_TimeOfDay);
            }

            private bool IsPostEventDepartureDue(in SpecialEventData specialEventData, Citizen citizen, Entity eventEntity, int day)
            {
                if (specialEventData.day != day)
                    return false;

                float start = math.frac(specialEventData.start_time);
                float end = math.frac(specialEventData.start_time + specialEventData.duration);
                if (IsInWindow(start, end, m_TimeOfDay))
                    return false;

                float elapsedSinceEnd = GetElapsed(end, m_TimeOfDay);
                float delay = GetPostEventDepartureDelay(citizen, eventEntity, day);
                return elapsedSinceEnd >= delay && elapsedSinceEnd <= (90f / 1440f);
            }

            private bool TryStartPostEventMeal(
                int index,
                Entity entity,
                Entity household,
                Entity currentBuilding,
                Entity providerEntity,
                Citizen citizen,
                LeisureProviderData provider,
                in SpecialEventData specialEventData,
                int day)
            {
                if (!IsPostEventDepartureDue(in specialEventData, citizen, providerEntity, day) ||
                    this.m_ResourceBuyers.HasComponent(entity) ||
                    !this.m_Households.HasComponent(household))
                {
                    return false;
                }

                float hour = m_TimeOfDay * 24f;
                int chance = 15;
                if (hour >= 17f && hour <= 23.5f)
                    chance += 15;
                else if (hour < 1.5f)
                    chance += 5;
                else
                    chance -= 10;

                if (this.m_TouristHouseholds.HasComponent(household))
                    chance += 10;

                CitizenAge age = citizen.GetAge();
                if (age == CitizenAge.Child)
                    chance = math.min(chance, 5);
                else if (age == CitizenAge.Teen)
                    chance = math.max(5, chance - 5);
                else if (age == CitizenAge.Elderly)
                    chance = math.max(5, chance - 5);

                if (provider.m_LeisureType == LeisureType.Meals)
                    chance = math.min(chance, 8);

                chance = math.clamp(chance, 0, 60);
                uint seed = (uint)(citizen.m_PseudoRandom + providerEntity.Index * 521 + day * 1543 + 83);
                Unity.Mathematics.Random random = Unity.Mathematics.Random.CreateFromIndex(seed);
                if (random.NextInt(100) >= chance)
                    return false;

                Entity positionEntity = this.m_Transforms.HasComponent(currentBuilding) ? currentBuilding : providerEntity;
                if (!this.m_Transforms.HasComponent(positionEntity))
                    return false;

                this.m_CommandBuffer.AddComponent<ResourceBuyer>(index, entity, new ResourceBuyer()
                {
                    m_Payer = household,
                    m_Flags = SetupTargetFlags.Commercial,
                    m_Location = this.m_Transforms[positionEntity].m_Position,
                    m_ResourceNeeded = Resource.Meals,
                    m_AmountNeeded = random.NextInt(1, 3)
                });

                if (this.m_Purposes.HasComponent(entity))
                    this.m_CommandBuffer.RemoveComponent<TravelPurpose>(index, entity);
                if (this.m_Targets.HasComponent(entity))
                    this.m_CommandBuffer.RemoveComponent<Game.Common.Target>(index, entity);
                if (this.m_Shopping.HasComponent(entity))
                    this.m_CommandBuffer.RemoveComponent<Shopper>(index, entity);

                return true;
            }

            private void SpendLeisure(
              int index,
              Entity entity,
              Entity household,
              Entity currentBuilding,
              ref Citizen citizen,
              ref Leisure leisure,
              Entity providerEntity,
              LeisureProviderData provider,
              Entity specialEventDataEntity,
              int day)
            {
                bool flag = false;
                bool failedInactive = false;
                bool failedNoService = false;
                bool failedNoResources = false;

                if (this.m_BuildingData.HasComponent(providerEntity) && BuildingUtils.CheckOption(this.m_BuildingData[providerEntity], BuildingOption.Inactive))
                {
                    flag = true;
                    failedInactive = true;
                }

                if (!flag && this.m_ServiceAvailables.HasComponent(providerEntity) && this.m_ServiceAvailables[providerEntity].m_ServiceAvailable <= 0)
                {
                    flag = true;
                    failedNoService = true;
                }

                Entity prefab = this.m_PrefabRefs[providerEntity].m_Prefab;

                Resource resource = Resource.NoResource;
                if (!flag && this.m_IndustrialProcesses.HasComponent(prefab))
                {

                    resource = this.m_IndustrialProcesses[prefab].m_Output.m_Resource;
                    if (resource != Resource.NoResource && this.m_Resources.HasBuffer(providerEntity) && EconomyUtils.GetResources(resource, this.m_Resources[providerEntity]) <= 0)
                    {
                        flag = true;
                        failedNoResources = true;
                    }
                }
                if (!flag)
                {
                    this.m_LeisureQueue.Enqueue(new LeisureEvent()
                    {
                        m_Citizen = entity,
                        m_Provider = providerEntity,
                        m_Efficiency = provider.m_Efficiency
                    });
                } 
                

                SpecialEventData specialEventdata;

                bool leisureCounterCondition = (double)citizen.m_LeisureCounter > (double)byte.MaxValue - (double)provider.m_Efficiency / (double)Time2WorkLeisureSystem.kUpdateInterval;
                if (m_SpecialEventDatas.TryGetComponent(specialEventDataEntity, out specialEventdata))
                {
                    if (specialEventdata.day == day)
                    {  
                        if(IsEventAttendanceWindow(in specialEventdata, citizen, specialEventDataEntity, day))
                        {
                            leisureCounterCondition = false;
                            //Mod.log.Info($"Special event active: index:{entity.Index}, hour:{(int)Math.Round(this.m_TimeOfDay*24)}, timeOfDay: {this.m_TimeOfDay}, event start: {specialEventdata.start_time}, event end: {specialEventdata.start_time + specialEventdata.duration}");
                        }
                    }
                }

                if (((leisureCounterCondition ? 1 : (this.m_SimulationFrame >= leisure.m_LastPossibleFrame ? 1 : 0)) | (flag ? 1 : 0)) == 0)
                    return;

                if (this.m_LogLeisure)
                {
                    float durationMinutes = 0f;
                    Shopper shopper;
                    if (this.m_Shopping.TryGetComponent(entity, out shopper))
                    {
                        durationMinutes = GetElapsed(shopper.start_time, this.m_TimeOfDay) * 1440f;
                    }

                    this.m_LeisureLogQueue.Enqueue(new LeisureLogEvent()
                    {
                        leisureType = provider.m_LeisureType,
                        completedTrips = flag ? 0 : 1,
                        failedTrips = flag ? 1 : 0,
                        failedInactive = failedInactive ? 1 : 0,
                        failedNoService = failedNoService ? 1 : 0,
                        failedNoResources = failedNoResources ? 1 : 0,
                        mealVisits = !flag && provider.m_LeisureType == LeisureType.Meals ? 1 : 0,
                        durationMinutes = flag ? 0f : durationMinutes
                    });
                }

                if (m_SpecialEventDatas.TryGetComponent(specialEventDataEntity, out specialEventdata))
                {
                    TryStartPostEventMeal(index, entity, household, currentBuilding, providerEntity, citizen, provider, in specialEventdata, day);
                }

                this.m_CommandBuffer.RemoveComponent<Leisure>(index, entity);
            }

            private void shoppingTime(int unfilteredChunkIndex, Entity entity, Citizen citizen, LeisureType leisureType)
            {
                Shopper shopper;
                if (!this.m_Shopping.TryGetComponent(entity, out shopper))
                {
                    float shopping_time = 0f;
                    switch (leisureType)
                    {
                        case LeisureType.Meals:
                            shopping_time = avg_time_meals / 1440f;
                            break;
                        case LeisureType.Entertainment:
                            shopping_time = avg_time_entertainment / 1440f;
                            break;
                        default:
                            shopping_time = (avg_time_beverages+avg_time_chemicals+avg_time_convenienceFood+avg_time_electronics+avg_time_food+avg_time_media+avg_time_paper+avg_time_plastics) / (8*1440f);
                            break;
                    }

                    uint seed = (uint)(citizen.m_PseudoRandom + 1000 * m_TimeOfDay);
                    Unity.Mathematics.Random random2 = Unity.Mathematics.Random.CreateFromIndex(seed);
                    // Add + or - variation on shopping time by 30% of the time defined above
                    float random_factor = 0.8f;
                    if (shopping_time <= 10f / 1440f)
                    {
                        random_factor = 0.5f;
                    }

                    shopping_time += (float)(GaussianRandom.NextGaussianDouble(random2) * random_factor * shopping_time);
                    float duration = this.m_TimeOfDay + shopping_time;
                    if (duration > 1)
                    {
                        duration -= 1f;
                    }

                    //Mod.log.Info($"Add shopping: index:{entity.Index}, hour:{(int)Math.Round(this.m_TimeOfDay*24)}, type:{leisureType}, timeOfDay: {this.m_TimeOfDay}");
                    this.m_CommandBuffer.AddComponent<Shopper>(unfilteredChunkIndex, entity, new Shopper(duration, this.m_TimeOfDay));
                }
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

                //Select leisure variables based on day of the week
                if ((int)dow == (int)Setting.DTSimulationEnum.Weekday)
                {
                    meals_avghour = meals_leisure.x;
                    entertainment_avghour = entertainment_leisure.x;
                    shopping_avghour = shopping_leisure.x;
                    park_avghour = park_leisure.x;
                    travel_avghour = travel_leisure.x;
                }
                else if ((int)dow == (int)Setting.DTSimulationEnum.AverageDay)
                {
                    meals_avghour = meals_leisure.y;
                    entertainment_avghour = entertainment_leisure.y;
                    shopping_avghour = shopping_leisure.y;
                    park_avghour = park_leisure.y;
                    travel_avghour = travel_leisure.y;
                }
                else if ((int)dow == (int)Setting.DTSimulationEnum.Saturday)
                {
                    meals_avghour = meals_leisure.z;
                    entertainment_avghour = entertainment_leisure.z;
                    shopping_avghour = shopping_leisure.z;
                    park_avghour = park_leisure.z;
                    travel_avghour = travel_leisure.z;
                }
                else
                {
                    meals_avghour = meals_leisure.w;
                    entertainment_avghour = entertainment_leisure.w;
                    shopping_avghour = shopping_leisure.w;
                    park_avghour = park_leisure.w;
                    travel_avghour = travel_leisure.w;
                }

                meals_avghour *= meal_hourly_factor;
                entertainment_avghour *= entertainment_hourly_factor;
                shopping_avghour *= shopping_hourly_factor;
                park_avghour *= park_hourly_factor;
                travel_avghour *= travel_hourly_factor;

                SpecialEventData specialEventdata;

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

                    //During special event parks will attract more people
                    if (m_SpecialEventDatas.TryGetComponent(providerEntity, out specialEventdata))
                    {
                        if (specialEventdata.day == day)
                        {
                            float start = specialEventdata.start_time - 1.5f / 24f;
                            if (m_TimeOfDay >= start && m_TimeOfDay <= (specialEventdata.start_time + specialEventdata.duration))
                            {
                                switch (provider.m_LeisureType)
                                {
                                    case LeisureType.CityPark:
                                        park_avghour *= 10f;              // parks get the big bump
                                        break;
                                    case LeisureType.Entertainment:
                                        entertainment_avghour *= 5f;     // strong bump
                                        break;
                                    case LeisureType.Meals:
                                        meals_avghour *= 5f;             // modest bump
                                        break;
                                    default:
                                        // If you have a "travel" or other, you can choose to leave it unchanged
                                        break;
                                }
                            }
                        }
                    }

                    if (entity2 != Entity.Null)
                    {
                        Entity currentBuildingForMeal = this.m_CurrentBuildings.HasComponent(entity1) ? this.m_CurrentBuildings[entity1].m_CurrentBuilding : Entity.Null;
                        this.SpendLeisure(unfilteredChunkIndex, entity1, nativeArray3[index].m_Household, currentBuildingForMeal, ref citizenData, ref leisure, providerEntity, provider, providerEntity, day);
                        nativeArray2[index] = leisure;
                        this.m_CitizenDatas[entity1] = citizenData;
                    }
                    else
                    {
                       Unity.Mathematics.Random rand = Unity.Mathematics.Random.CreateFromIndex((uint)(citizenData.m_PseudoRandom + day));

                        if (!flag && this.m_PathInfos.HasComponent(entity1))
                        {
                            PathInformation pathInfo = this.m_PathInfos[entity1];
                            if ((pathInfo.m_State & PathFlags.Pending) == (PathFlags)0)
                            {
                                Entity destination = pathInfo.m_Destination;
                                if ((this.m_PropertyRenters.HasComponent(destination) || this.m_PrefabRefs.HasComponent(destination)) && !this.m_Targets.HasComponent(entity1))
                                {
                                    float2 time2Lunch = new float2(-1f, -1f);
                                    float2 time2Work = new float2(-1f, -1f);
                                    bool workFromHome = false;
                                    bool lunchTime = false;
                                    bool workTime = false;
                                    float start_work = 0f;
                                    bool dayOff = false;

                                    bool hasSchedule = this.CitizenScheduleLookup.HasComponent(entity1);
                                    if (hasSchedule)
                                    {
                                        CitizenSchedule citizenSchedule = this.CitizenScheduleLookup[entity1];
                                        time2Lunch = new float2(citizenSchedule.start_lunch, citizenSchedule.end_lunch);
                                        time2Work = new float2(citizenSchedule.go_to_work, citizenSchedule.end_work);
                                        workFromHome = citizenSchedule.work_from_home;
                                        lunchTime = Time2WorkWorkerSystem.IsLunchTime(this.m_TimeOfDay, time2Lunch);
                                        workTime = Time2WorkWorkerSystem.IsTimeToWork(this.m_TimeOfDay, time2Work);
                                        start_work = citizenSchedule.start_work;
                                        dayOff = citizenSchedule.dayoff;
                                    }

                                    if ((!this.m_Workers.HasComponent(entity1) || dayOff || !workTime ||
                                        (!dayOff && workTime && lunchTime)) && (!this.m_Students.HasComponent(entity1) || this.m_Students.HasComponent(entity1) && workTime))
                                    {
                                        provider = this.m_LeisureProviderDatas[this.m_PrefabRefs[destination].m_Prefab];
                                        if (provider.m_Efficiency == 0)
                                            UnityEngine.Debug.LogWarning((object)string.Format("Warning: Leisure provider {0} has zero efficiency", (object)destination.Index));
                                        leisure.m_TargetAgent = destination;
                                        nativeArray2[index] = leisure;

                                        if (ShouldCreateSocialLeisureOpportunity(entity1, citizenData, destination))
                                        {
                                            if (!this.m_SocialLeisureOpportunities.HasComponent(entity1))
                                            {
                                                this.m_CommandBuffer.AddComponent<SocialLeisureOpportunity>(unfilteredChunkIndex, entity1, new SocialLeisureOpportunity()
                                                {
                                                    version = 1,
                                                    originalTarget = destination,
                                                    originalLeisureType = (int)provider.m_LeisureType,
                                                    requestedFrame = this.m_SimulationFrame
                                                });
                                            }
                                            this.m_CommandBuffer.RemoveComponent<LeisureSeekerCooldown>(unfilteredChunkIndex, entity1);
                                        }
                                        else
                                        {
                                            StartNormalLeisureTrip(unfilteredChunkIndex, entity1, dynamicBuffer, citizenData, provider, destination);
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
                                        this.m_CommandBuffer.AddComponent<LeisureSeekerCooldown>(unfilteredChunkIndex, entity1, new LeisureSeekerCooldown()
                                        {
                                            m_SimulationFrame = this.m_SimulationFrame
                                        });
                                    }
                                }
                                else
                                {
                                    if (!this.m_Targets.HasComponent(entity1))
                                    {
                                        if (this.m_Purposes.HasComponent(entity1) && (this.m_Purposes[entity1].m_Purpose == Game.Citizens.Purpose.Leisure || this.m_Purposes[entity1].m_Purpose == Game.Citizens.Purpose.Traveling))
                                        {
                                            //Mod.log.Info($"Resource: {this.m_Purposes[entity1].m_Resource}");
                                            this.m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, entity1);
                                        }
                                        this.m_CommandBuffer.RemoveComponent<Leisure>(unfilteredChunkIndex, entity1);
                                        this.m_CommandBuffer.RemoveComponent(unfilteredChunkIndex, entity1, in this.m_PathfindTypes);
                                        this.m_CommandBuffer.AddComponent<LeisureSeekerCooldown>(unfilteredChunkIndex, entity1, new LeisureSeekerCooldown()
                                        {
                                            m_SimulationFrame = this.m_SimulationFrame
                                        });
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

                float factor_meal = 5f*meals_avghour / (meals_avghour + entertainment_avghour + shopping_avghour + park_avghour + travel_avghour);
                float factor_entertainment = 5f * entertainment_avghour / (meals_avghour + entertainment_avghour + shopping_avghour + park_avghour + travel_avghour);
                float factor_shopping = 5f * shopping_avghour / (meals_avghour + entertainment_avghour + shopping_avghour + park_avghour + travel_avghour);
                float factor_park = 5f * park_avghour / (meals_avghour + entertainment_avghour + shopping_avghour + park_avghour + travel_avghour);
                float factor_travel = 5f * travel_avghour / (meals_avghour + entertainment_avghour + shopping_avghour + park_avghour + travel_avghour);
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
                        num3 *= factor_meal;
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
                        num3 *= factor_entertainment;
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
                        num3 *= factor_shopping;
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
                        num3 *= factor_park;
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
                        num3 *= factor_travel;
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
                //Mod.log.Info($"Leisure type: {leisureType.ToString()}, hour: {Math.Floor(24*m_TimeOfDay)}");
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
                        m_TaxiIgnoredRules = VehicleUtils.GetIgnoredPathfindRulesTaxiDefaults(),
                        m_MaxCost = this.m_TripPriorityParameters.GetMaxCost(this.m_TripPriorityParameters.GetPriority(Game.Citizens.Purpose.Leisure, citizenData))
                    };
                    SetupQueueTarget setupQueueTarget = new SetupQueueTarget();
                    setupQueueTarget.m_Type = SetupTargetType.CurrentLocation;
                    setupQueueTarget.m_Methods = PathMethod.Pedestrian;
                    setupQueueTarget.m_RandomCost = 30f;
                    SetupQueueTarget origin = setupQueueTarget;
                    setupQueueTarget = new SetupQueueTarget();
                    setupQueueTarget.m_Type = SetupTargetType.Leisure;
                    setupQueueTarget.m_Methods = PathMethod.Pedestrian;
                    setupQueueTarget.m_Value = (int)leisureType;
                    setupQueueTarget.m_Value2 = num;
                    setupQueueTarget.m_RandomCost = 30f;
                    setupQueueTarget.m_ActivityMask = creatureData.m_SupportedActivities;
                    SetupQueueTarget destination = setupQueueTarget;
                    PropertyRenter componentData1;

                    if (this.m_PropertyRenters.TryGetComponent(household, out componentData1))
                        parameters.m_Authorization1 = componentData1.m_Property;
                    if (this.m_Workers.HasComponent(citizen))
                    {
                        Worker worker = this.m_Workers[citizen];
                        parameters.m_Authorization2 = !this.m_PropertyRenters.HasComponent(worker.m_Workplace) ? worker.m_Workplace : this.m_PropertyRenters[worker.m_Workplace].m_Property;
                    }
                    float bikeProbability = 20f;
                    CurrentDistrict currentDistrict;
                    DynamicBuffer<DistrictModifier> districtModifiers;
                    if (this.m_CurrentDistrictData.TryGetComponent(componentData1.m_Property, out currentDistrict) && this.m_DistrictModifiers.TryGetBuffer(currentDistrict.m_District, out districtModifiers))
                        AreaUtils.ApplyModifier(ref bikeProbability, districtModifiers, DistrictModifierType.BikeProbability);
                    bool preferBike = (double)random.NextFloat(100f) < (double)bikeProbability;
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
                            parameters.m_ParkingSize = VehicleUtils.GetParkingSize(car, ref this.m_PrefabRefs, ref this.m_ObjectGeometryData);
                            parameters.m_Methods |= VehicleUtils.GetPathMethods(carData) | PathMethod.Parking;
                            parameters.m_IgnoredRules = VehicleUtils.GetIgnoredPathfindRules(carData);
                            Game.Vehicles.PersonalCar componentData2;
                            if (this.m_PersonalCarData.TryGetComponent(car, out componentData2) && (componentData2.m_State & PersonalCarFlags.HomeTarget) == (PersonalCarFlags)0)
                                parameters.m_PathfindFlags |= PathfindFlags.ParkingReset;
                        }
                    }
                    else
                    {
                        if (this.m_BicycleOwners.IsComponentEnabled(citizen) && preferBike)
                        {
                            Entity bicycle = this.m_BicycleOwners[citizen].m_Bicycle;
                            PrefabRef componentData3;
                            CurrentBuilding componentData4;
                            if (!this.m_PrefabRefs.TryGetComponent(bicycle, out componentData3) && this.m_CurrentBuildings.TryGetComponent(citizen, out componentData4) && componentData4.m_CurrentBuilding == componentData1.m_Property)
                            {
                                Unity.Mathematics.Random pseudoRandom = citizenData.GetPseudoRandom(CitizenPseudoRandom.BicycleModel);
                                componentData3.m_Prefab = this.m_PersonalCarSelectData.SelectVehiclePrefab(ref pseudoRandom, 1, 0, true, false, true, out Entity _);
                            }
                            CarData componentData5;
                            ObjectGeometryData componentData6;
                            if (this.m_PrefabCarData.TryGetComponent(componentData3.m_Prefab, out componentData5) && this.m_ObjectGeometryData.TryGetComponent(componentData3.m_Prefab, out componentData6))
                            {
                                parameters.m_MaxSpeed.x = componentData5.m_MaxSpeed;
                                parameters.m_ParkingSize = VehicleUtils.GetParkingSize(componentData6, out float _);
                                parameters.m_Methods |= PathMethod.Bicycle | PathMethod.BicycleParking;
                                parameters.m_IgnoredRules = VehicleUtils.GetIgnoredPathfindRulesBicycleDefaults();
                                ParkedCar componentData7;
                                if (this.m_ParkedCarData.TryGetComponent(bicycle, out componentData7))
                                {
                                    parameters.m_ParkingTarget = componentData7.m_Lane;
                                    parameters.m_ParkingDelta = componentData7.m_CurvePosition;
                                    Game.Vehicles.PersonalCar componentData8;
                                    if (this.m_PersonalCarData.TryGetComponent(bicycle, out componentData8) && (componentData8.m_State & PersonalCarFlags.HomeTarget) == (PersonalCarFlags)0)
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
            public ComponentTypeHandle<CitizenSchedule> __Game_Citizens_CitizenSchedule_RO_ComponentTypeHandle;
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
            public ComponentLookup<BicycleOwner> __Game_Citizens_BicycleOwner_RO_ComponentLookup;
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
            public ComponentLookup<SpecialEventData> __Game_Citizens_SpecialEventData_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<ResourceBuyer> __Game_Companies_ResourceBuyer_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<SocialLeisureOpportunity> __Time2Work_Components_SocialLeisureOpportunity_RO_ComponentLookup;
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
            [ReadOnly]
            public ComponentLookup<CurrentDistrict> __Game_Areas_CurrentDistrict_RO_ComponentLookup;
            [ReadOnly]
            public BufferLookup<DistrictModifier> __Game_Areas_DistrictModifier_RO_BufferLookup;
            public ComponentLookup<ServiceAvailable> __Game_Companies_ServiceAvailable_RW_ComponentLookup;
            public ComponentLookup<CompanyStatisticData> __Game_Companies_CompanyStatisticData_RW_ComponentLookup;
            public BufferLookup<Game.Economy.Resources> __Game_Economy_Resources_RW_BufferLookup;
            public ComponentLookup<TaxPayer> __Game_Agents_TaxPayer_RW_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Shopper> __Game_Citizens_Shopping_RW_ComponentLookup;
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
            [ReadOnly]
            public ComponentLookup<CitizenSchedule> CitizenScheduleLookup;


            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void __AssignHandles(ref SystemState state)
            {
                this.__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
                this.__Game_Citizens_CitizenSchedule_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CitizenSchedule>(true);
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
                this.__Game_Citizens_BicycleOwner_RO_ComponentLookup = state.GetComponentLookup<BicycleOwner>(true);
                this.__Game_Vehicles_ParkedCar_RO_ComponentLookup = state.GetComponentLookup<ParkedCar>(true);
                this.__Game_Vehicles_PersonalCar_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.PersonalCar>(true);
                this.__Game_Common_Target_RO_ComponentLookup = state.GetComponentLookup<Game.Common.Target>(true);
                this.__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(true);
                this.__Game_Prefabs_LeisureProviderData_RO_ComponentLookup = state.GetComponentLookup<LeisureProviderData>(true);
                this.__Game_Citizens_Student_RO_ComponentLookup = state.GetComponentLookup<Game.Citizens.Student>(true);
                this.__Game_Citizens_Worker_RO_ComponentLookup = state.GetComponentLookup<Worker>(true);
                this.__Game_Citizens_Household_RO_ComponentLookup = state.GetComponentLookup<Household>(true);
                this.__Game_Citizens_SpecialEventData_RO_ComponentLookup = state.GetComponentLookup<SpecialEventData>(true);
                this.__Game_Companies_ResourceBuyer_RO_ComponentLookup = state.GetComponentLookup<ResourceBuyer>(true);
                this.__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(true);
                this.__Time2Work_Components_SocialLeisureOpportunity_RO_ComponentLookup = state.GetComponentLookup<SocialLeisureOpportunity>(true);
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
                this.__Game_Companies_CompanyStatisticData_RW_ComponentLookup = state.GetComponentLookup<CompanyStatisticData>();
                this.__Game_Economy_Resources_RW_BufferLookup = state.GetBufferLookup<Game.Economy.Resources>();
                this.__Game_Citizens_HouseholdMember_RO_ComponentLookup = state.GetComponentLookup<HouseholdMember>(true);
                this.__Game_Companies_ServiceCompanyData_RO_ComponentLookup = state.GetComponentLookup<ServiceCompanyData>(true);
                this.__Game_Buildings_Renter_RO_BufferLookup = state.GetBufferLookup<Renter>(true);
                this.__Game_Prefabs_ConsumptionData_RO_ComponentLookup = state.GetComponentLookup<ConsumptionData>(true);
                this.__Game_Areas_CurrentDistrict_RO_ComponentLookup = state.GetComponentLookup<CurrentDistrict>(true);
                this.__Game_Areas_DistrictModifier_RO_BufferLookup = state.GetBufferLookup<DistrictModifier>(true);
                this.__Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(true);
                this.__Game_Companies_ServiceCompanyData_RO_ComponentLookup = state.GetComponentLookup<ServiceCompanyData>(true);
                this.__Game_Citizens_Shopping_RW_ComponentLookup = state.GetComponentLookup<Shopper>(false);
                this.CommercialPropertyLookup = state.GetComponentLookup<CommercialProperty>(true);
                this.IndustrialPropertyLookup = state.GetComponentLookup<IndustrialProperty>(true);
                this.OfficePropertyLookup = state.GetComponentLookup<OfficeProperty>(true);
                this.PropertyRenterLookup = state.GetComponentLookup<PropertyRenter>(true);
                this.PrefabRefLookup = state.GetComponentLookup<PrefabRef>(true);
                this.CitizenScheduleLookup = state.GetComponentLookup<CitizenSchedule>(true);
            }
        }
    }
}
