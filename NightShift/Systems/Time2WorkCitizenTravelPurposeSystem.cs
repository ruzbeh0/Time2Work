
using Colossal.Collections;
using Colossal.Entities;
using Game;
using Game.Agents;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Events;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using System.Text;
using Time2Work.Components;
using Time2Work.Systems;
using Time2Work.Utils;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using static Game.Prefabs.TriggerPrefabData;

#nullable disable
namespace Time2Work
{
    public partial class Time2WorkCitizenTravelPurposeSystem : GameSystemBase
    {
        private Time2WorkTimeSystem m_TimeSystem;
        private ResourceSystem m_ResourceSystem;
        private CityStatisticsSystem m_CityStatisticsSystem;
        private EndFrameBarrier m_EndFrameBarrier;
        private EntityQuery m_ArrivedGroup;
        private EntityQuery m_StuckGroup;
        private EntityQuery m_ShoppingGroup;
        private EntityQuery m_EconomyParameterGroup;
        private EntityQuery m_OutsideConnectionQuery;
        private EntityQuery m_ServiceBuildingQuery;
        private NativeQueue<ShoppingLogEvent> m_ShoppingLogQueue;
        private readonly Dictionary<Resource, ShoppingHourlyTotals> m_ShoppingHourlyTotals = new Dictionary<Resource, ShoppingHourlyTotals>();
        private DateTime m_ShoppingLogHourStart;
        private bool m_ShoppingLogHourInitialized;
        private bool m_ShoppingLogSettingInitialized;
        private bool m_LastShoppingLogEnabled;
        private Time2WorkCitizenTravelPurposeSystem.TypeHandle __TypeHandle;

        public override int GetUpdateInterval(SystemUpdatePhase phase) => 16;

        protected override void OnCreate()
        {
            base.OnCreate();
            
            this.m_TimeSystem = this.World.GetOrCreateSystemManaged<Time2WorkTimeSystem>(); 
            this.m_ResourceSystem = this.World.GetOrCreateSystemManaged<ResourceSystem>();
            this.m_CityStatisticsSystem = this.World.GetOrCreateSystemManaged<CityStatisticsSystem>(); 
            this.m_EndFrameBarrier = this.World.GetOrCreateSystemManaged<EndFrameBarrier>();
            this.m_ArrivedGroup = this.GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[4]
             {
                   ComponentType.ReadOnly<Citizen>(),
                   ComponentType.ReadWrite<TravelPurpose>(),
                   ComponentType.ReadWrite<TripNeeded>(),
                   ComponentType.ReadOnly<CurrentBuilding>(),
             },
                Any = new ComponentType[0]
               {
                    
               },
                None = new ComponentType[2]
             {
                ComponentType.Exclude<Deleted>(),
                ComponentType.Exclude<Temp>()
             }
            });
            this.m_StuckGroup = this.GetEntityQuery(ComponentType.ReadOnly<Citizen>(), ComponentType.ReadWrite<TravelPurpose>(), ComponentType.ReadWrite<TripNeeded>(), ComponentType.Exclude<CurrentTransport>(), ComponentType.Exclude<CurrentBuilding>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
            this.m_ShoppingGroup = this.GetEntityQuery(ComponentType.ReadOnly<Citizen>(), ComponentType.ReadWrite<TravelPurpose>(), ComponentType.ReadWrite<Shopper>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
            this.m_OutsideConnectionQuery = this.GetEntityQuery(ComponentType.ReadWrite<Game.Objects.OutsideConnection>(), ComponentType.Exclude<Game.Objects.ElectricityOutsideConnection>(), ComponentType.Exclude<Game.Objects.WaterPipeOutsideConnection>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
            this.m_ServiceBuildingQuery = this.GetEntityQuery(ComponentType.ReadWrite<CityServiceUpkeep>(), ComponentType.ReadWrite<Building>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Destroyed>(), ComponentType.Exclude<Temp>());
            this.m_EconomyParameterGroup = this.GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
            this.m_TimeSystem = this.World.GetOrCreateSystemManaged<Time2WorkTimeSystem>();
            this.m_ShoppingLogQueue = new NativeQueue<ShoppingLogEvent>((AllocatorManager.AllocatorHandle)Allocator.Persistent);
            
            this.RequireAnyForUpdate(this.m_ArrivedGroup, this.m_StuckGroup, this.m_ShoppingGroup);
        }

        protected override void OnDestroy()
        {
            this.Dependency.Complete();
            if (Mod.m_Setting != null && Mod.m_Setting.shopping_log_enabled)
            {
                DrainShoppingLogs(this.m_TimeSystem.GetCurrentDateTime());
                FlushShoppingHourlyTotals();
            }
            if (this.m_ShoppingLogQueue.IsCreated)
            {
                this.m_ShoppingLogQueue.Dispose();
            }
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            NativeQueue<Time2WorkCitizenTravelPurposeSystem.Arrive> nativeQueue = new NativeQueue<Time2WorkCitizenTravelPurposeSystem.Arrive>((AllocatorManager.AllocatorHandle)Allocator.Persistent);

            var now = m_TimeSystem.GetCurrentDateTime();
            bool logShopping = Mod.m_Setting != null && Mod.m_Setting.shopping_log_enabled;
            LogShoppingSettingState(logShopping, now);

            Time2WorkCitizenTravelPurposeSystem.CitizenArriveJob jobData = new Time2WorkCitizenTravelPurposeSystem.CitizenArriveJob()
            {
                m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref this.__TypeHandle.__Unity_Entities_Entity_TypeHandle, ref this.CheckedStateRef),
                m_CurrentBuildingType = InternalCompilerInterface.GetComponentTypeHandle<CurrentBuilding>(ref this.__TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle, ref this.CheckedStateRef),
                m_TravelPurposeType = InternalCompilerInterface.GetComponentTypeHandle<TravelPurpose>(ref this.__TypeHandle.__Game_Citizens_TravelPurpose_RW_ComponentTypeHandle, ref this.CheckedStateRef),
                m_HealthProblemType = InternalCompilerInterface.GetComponentTypeHandle<HealthProblem>(ref this.__TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentTypeHandle, ref this.CheckedStateRef),
                m_ArrivedType = InternalCompilerInterface.GetComponentTypeHandle<Arrived>(ref this.__TypeHandle.__Game_Citizens_Arrived_RO_ComponentTypeHandle, ref this.CheckedStateRef),
                m_BuildingData = InternalCompilerInterface.GetComponentLookup<Building>(ref this.__TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref this.CheckedStateRef),
                m_Schools = InternalCompilerInterface.GetComponentLookup<Game.Buildings.School>(ref this.__TypeHandle.__Game_Buildings_School_RO_ComponentLookup, ref this.CheckedStateRef),
                m_WorkProviders = InternalCompilerInterface.GetComponentLookup<WorkProvider>(ref this.__TypeHandle.__Game_Companies_WorkProvider_RO_ComponentLookup, ref this.CheckedStateRef),
                m_Students = InternalCompilerInterface.GetComponentLookup<Game.Citizens.Student>(ref this.__TypeHandle.__Game_Citizens_Student_RO_ComponentLookup, ref this.CheckedStateRef),
                m_Workers = InternalCompilerInterface.GetComponentLookup<Worker>(ref this.__TypeHandle.__Game_Citizens_Worker_RO_ComponentLookup, ref this.CheckedStateRef),
                m_PoliceStationData = InternalCompilerInterface.GetComponentLookup<Game.Buildings.PoliceStation>(ref this.__TypeHandle.__Game_Buildings_PoliceStation_RO_ComponentLookup, ref this.CheckedStateRef),
                m_PrisonData = InternalCompilerInterface.GetComponentLookup<Game.Buildings.Prison>(ref this.__TypeHandle.__Game_Buildings_Prison_RO_ComponentLookup, ref this.CheckedStateRef),
                m_HospitalData = InternalCompilerInterface.GetComponentLookup<Game.Buildings.Hospital>(ref this.__TypeHandle.__Game_Buildings_Hospital_RO_ComponentLookup, ref this.CheckedStateRef),
                m_DeathcareFacilityData = InternalCompilerInterface.GetComponentLookup<Game.Buildings.DeathcareFacility>(ref this.__TypeHandle.__Game_Buildings_DeathcareFacility_RO_ComponentLookup, ref this.CheckedStateRef),
                m_EmergencyShelterData = InternalCompilerInterface.GetComponentLookup<Game.Buildings.EmergencyShelter>(ref this.__TypeHandle.__Game_Buildings_EmergencyShelter_RO_ComponentLookup, ref this.CheckedStateRef),
                m_Citizens = InternalCompilerInterface.GetComponentLookup<Citizen>(ref this.__TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup, ref this.CheckedStateRef),
                m_HealthProblems = InternalCompilerInterface.GetComponentLookup<HealthProblem>(ref this.__TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentLookup, ref this.CheckedStateRef),
                m_HouseholdMembers = InternalCompilerInterface.GetComponentLookup<HouseholdMember>(ref this.__TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentLookup, ref this.CheckedStateRef),
                m_MovingAways = InternalCompilerInterface.GetComponentLookup<MovingAway>(ref this.__TypeHandle.__Game_Agents_MovingAway_RO_ComponentLookup, ref this.CheckedStateRef),
                CitizenScheduleLookup = InternalCompilerInterface.GetComponentLookup<CitizenSchedule>(ref this.__TypeHandle.CitizenScheduleLookup, ref this.CheckedStateRef),
                m_Shopping = InternalCompilerInterface.GetComponentLookup<Shopper>(ref this.__TypeHandle.__Game_Citizens_Shopping_RW_ComponentLookup, ref this.CheckedStateRef),
                m_EconomyParameters = this.m_EconomyParameterGroup.GetSingleton<EconomyParameterData>(),
                m_CommandBuffer = this.m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
                m_ArriveQueue = nativeQueue.AsParallelWriter(),
                m_ShoppingLogQueue = this.m_ShoppingLogQueue.AsParallelWriter(),
                m_NormalizedTime = this.m_TimeSystem.normalizedTime,
                m_LogShopping = logShopping,
                m_RandomSeed = RandomSeed.Next(),
                lunch_break_pct = Mod.m_Setting.lunch_break_percentage,
                school_start_time = (int)Mod.m_Setting.school_start_time,
                school_end_time = (int)Mod.m_Setting.school_end_time,
                work_start_time = (float)Mod.m_Setting.work_start_time,
                work_end_time = (float)Mod.m_Setting.work_end_time,
                delayFactor = (float)(Mod.m_Setting.delay_factor) / 100,
                ticksPerDay = Time2WorkTimeSystem.kTicksPerDay,
                part_time_prob = Mod.m_Setting.part_time_percentage,
                commute_top10 = Mod.m_Setting.commute_top10per,
                part_time_reduction = Mod.m_Setting.avg_work_hours_pt_wd / Mod.m_Setting.avg_work_hours_ft_wd,
                overtime = (Mod.m_Setting.avg_work_hours_ft_wd - (Mod.m_Setting.work_end_time - Mod.m_Setting.work_start_time) / 2)/24,
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
                newyearseve = (now.Day == (Mod.m_Setting.daysPerMonth*12)),
                dow = (int)WeekSystem.currentDayOfTheWeek
            };
            
            this.Dependency = jobData.ScheduleParallel<Time2WorkCitizenTravelPurposeSystem.CitizenArriveJob>(this.m_ArrivedGroup, this.Dependency);
            
            this.m_EndFrameBarrier.AddJobHandleForProducer(this.Dependency);

            Time2WorkCitizenTravelPurposeSystem.CitizenStopShoppingJob jobData2 = new Time2WorkCitizenTravelPurposeSystem.CitizenStopShoppingJob()
            {
                m_EntityType = this.__TypeHandle.__Unity_Entities_Entity_TypeHandle,
                m_CurrentBuildingType = this.__TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle,
                m_TravelPurposeType = this.__TypeHandle.__Game_Citizens_TravelPurpose_RW_ComponentTypeHandle,
                m_Shopping = this.__TypeHandle.__Game_Citizens_Shopping_RW_ComponentLookup,
                m_ShoppingPurchaseData = InternalCompilerInterface.GetComponentLookup<ShoppingPurchaseData>(ref this.__TypeHandle.__Time2Work_Components_ShoppingPurchaseData_RW_ComponentLookup, ref this.CheckedStateRef),
                m_ResourcePrefabs = this.m_ResourceSystem.GetPrefabs(),
                m_ResourceDatas = InternalCompilerInterface.GetComponentLookup<ResourceData>(ref this.__TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup, ref this.CheckedStateRef),
                m_CommandBuffer = this.m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
                m_NormalizedTime = this.m_TimeSystem.normalizedTime,
                m_LogShopping = logShopping,
                m_ShoppingLogQueue = this.m_ShoppingLogQueue.AsParallelWriter()
            };

            this.Dependency = jobData2.ScheduleParallel<Time2WorkCitizenTravelPurposeSystem.CitizenStopShoppingJob>(this.m_ShoppingGroup, this.Dependency);

            this.m_ResourceSystem.AddPrefabsReader(this.Dependency);
            this.m_EndFrameBarrier.AddJobHandleForProducer(this.Dependency);

            JobHandle deps;
            
            JobHandle jobHandle1 = new Time2WorkCitizenTravelPurposeSystem.ArriveJob()
            {
                m_CitizenPresenceData = InternalCompilerInterface.GetComponentLookup<CitizenPresence>(ref this.__TypeHandle.__Game_Buildings_CitizenPresence_RW_ComponentLookup, ref this.CheckedStateRef),
                m_Patients = InternalCompilerInterface.GetBufferLookup<Patient>(ref this.__TypeHandle.__Game_Buildings_Patient_RW_BufferLookup, ref this.CheckedStateRef),
                m_Occupants = InternalCompilerInterface.GetBufferLookup<Occupant>(ref this.__TypeHandle.__Game_Buildings_Occupant_RW_BufferLookup, ref this.CheckedStateRef),
                m_Households = InternalCompilerInterface.GetComponentLookup<Household>(ref this.__TypeHandle.__Game_Citizens_Household_RW_ComponentLookup, ref this.CheckedStateRef),
                m_HouseholdMembers = InternalCompilerInterface.GetComponentLookup<HouseholdMember>(ref this.__TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentLookup, ref this.CheckedStateRef),
                m_PropertyRenters = InternalCompilerInterface.GetComponentLookup<PropertyRenter>(ref this.__TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup, ref this.CheckedStateRef),
                m_HouseholdCitizens = InternalCompilerInterface.GetBufferLookup<HouseholdCitizen>(ref this.__TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup, ref this.CheckedStateRef),
                m_StatisticsQueue = this.m_CityStatisticsSystem.GetStatisticsEventQueue(out deps),
                m_ArriveQueue = nativeQueue
            }.Schedule<Time2WorkCitizenTravelPurposeSystem.ArriveJob>(JobHandle.CombineDependencies(this.Dependency, deps));
            JobHandle disposeArriveQueue = nativeQueue.Dispose(jobHandle1);
            this.m_CityStatisticsSystem.AddWriter(jobHandle1);


            this.__TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref this.CheckedStateRef);
            JobHandle outJobHandle1;
            JobHandle outJobHandle2;

            NativeList<Entity> serviceBuildings =
    this.m_ServiceBuildingQuery.ToEntityListAsync(Allocator.Persistent, out outJobHandle1);

            NativeList<Entity> outsideConnections =
                this.m_OutsideConnectionQuery.ToEntityListAsync(Allocator.Persistent, out outJobHandle2);

            Time2WorkCitizenTravelPurposeSystem.CitizenStuckJob stuckJob =
                new Time2WorkCitizenTravelPurposeSystem.CitizenStuckJob()
                {
                    m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(
                        ref this.__TypeHandle.__Unity_Entities_Entity_TypeHandle,
                        ref this.CheckedStateRef),

                    m_HouseholdMemberType = InternalCompilerInterface.GetComponentTypeHandle<HouseholdMember>(
                        ref this.__TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentTypeHandle,
                        ref this.CheckedStateRef),

                    m_HealthProblemType = InternalCompilerInterface.GetComponentTypeHandle<HealthProblem>(
                        ref this.__TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentTypeHandle,
                        ref this.CheckedStateRef),

                    m_CitizenType = InternalCompilerInterface.GetComponentTypeHandle<Citizen>(
                        ref this.__TypeHandle.__Game_Citizens_Citizen_RW_ComponentTypeHandle,
                        ref this.CheckedStateRef),

                    m_Households = InternalCompilerInterface.GetComponentLookup<Household>(
                        ref this.__TypeHandle.__Game_Citizens_Household_RO_ComponentLookup,
                        ref this.CheckedStateRef),

                    m_MovingAways = InternalCompilerInterface.GetComponentLookup<MovingAway>(
                        ref this.__TypeHandle.__Game_Agents_MovingAway_RO_ComponentLookup,
                        ref this.CheckedStateRef),

                    m_Buildings = InternalCompilerInterface.GetComponentLookup<Building>(
                        ref this.__TypeHandle.__Game_Buildings_Building_RO_ComponentLookup,
                        ref this.CheckedStateRef),

                    m_PropertyRenters = InternalCompilerInterface.GetComponentLookup<PropertyRenter>(
                        ref this.__TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup,
                        ref this.CheckedStateRef),

                    m_RandomSeed = RandomSeed.Next(),
                    m_ServiceBuildings = serviceBuildings,
                    m_OutsideConnections = outsideConnections,
                    m_CommandBuffer = this.m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter()
                };

            JobHandle stuckDeps = JobUtils.CombineDependencies(
                outJobHandle1,
                outJobHandle2,
                jobHandle1,
                this.Dependency);

            JobHandle jobHandle2 =
                stuckJob.ScheduleParallel<Time2WorkCitizenTravelPurposeSystem.CitizenStuckJob>(
                    this.m_StuckGroup,
                    stuckDeps);

            this.m_EndFrameBarrier.AddJobHandleForProducer(jobHandle2);

            JobHandle disposeServiceBuildings = serviceBuildings.Dispose(jobHandle2);
            JobHandle disposeOutsideConnections = outsideConnections.Dispose(jobHandle2);

            this.Dependency = JobHandle.CombineDependencies(disposeArriveQueue, jobHandle2);
            this.Dependency = JobHandle.CombineDependencies(
                this.Dependency,
                disposeServiceBuildings,
                disposeOutsideConnections);

            if (logShopping)
            {
                this.Dependency.Complete();
                DrainShoppingLogs(now);
            }
            else
            {
                ClearShoppingLogState();
            }
        }

        private void DrainShoppingLogs(DateTime now)
        {
            EnsureShoppingLogHour(now);

            while (this.m_ShoppingLogQueue.TryDequeue(out ShoppingLogEvent logEvent))
            {
                if (!this.m_ShoppingHourlyTotals.TryGetValue(logEvent.resource, out ShoppingHourlyTotals totals))
                {
                    totals = default;
                }

                totals.startedTrips += logEvent.startedTrips;
                totals.completedTrips += logEvent.completedTrips;
                totals.amount += logEvent.amount;
                totals.cost += logEvent.cost;
                totals.durationMinutes += logEvent.durationMinutes;
                totals.distance += logEvent.distance;
                totals.missingPurchaseData += logEvent.missingPurchaseData;
                totals.actualPurchaseData += logEvent.actualPurchaseData;
                totals.estimatedPurchaseData += logEvent.estimatedPurchaseData;
                totals.fallbackPurchaseData += logEvent.fallbackPurchaseData;
                totals.longDurationTrips += logEvent.longDurationTrips;
                this.m_ShoppingHourlyTotals[logEvent.resource] = totals;
            }
        }

        private void LogShoppingSettingState(bool logShopping, DateTime now)
        {
            if (this.m_ShoppingLogSettingInitialized && this.m_LastShoppingLogEnabled == logShopping)
                return;

            this.m_ShoppingLogSettingInitialized = true;
            this.m_LastShoppingLogEnabled = logShopping;

            if (logShopping)
            {
                EnsureShoppingLogHour(now);
                Mod.log.Info($"Shopping diagnostics log enabled at {now:yyyy-MM-dd HH:mm}; first ShoppingHourlyLog is written when the in-game hour changes.");
            }
            else
            {
                Mod.log.Info("Shopping diagnostics log disabled.");
            }
        }

        private void EnsureShoppingLogHour(DateTime now)
        {
            DateTime currentHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0);
            if (!this.m_ShoppingLogHourInitialized)
            {
                this.m_ShoppingLogHourStart = currentHour;
                this.m_ShoppingLogHourInitialized = true;
                return;
            }

            if (currentHour != this.m_ShoppingLogHourStart)
            {
                FlushShoppingHourlyTotals();
                this.m_ShoppingLogHourStart = currentHour;
            }
        }

        private void FlushShoppingHourlyTotals()
        {
            if (!this.m_ShoppingLogHourInitialized)
                return;

            int totalStartedTrips = 0;
            int totalCompletedTrips = 0;
            int totalAmount = 0;
            int totalCost = 0;
            int totalMissingPurchaseData = 0;
            int totalActualPurchaseData = 0;
            int totalEstimatedPurchaseData = 0;
            int totalFallbackPurchaseData = 0;
            int totalLongDurationTrips = 0;
            float totalDuration = 0f;
            float totalDistance = 0f;

            foreach (ShoppingHourlyTotals totals in this.m_ShoppingHourlyTotals.Values)
            {
                totalStartedTrips += totals.startedTrips;
                totalCompletedTrips += totals.completedTrips;
                totalAmount += totals.amount;
                totalCost += totals.cost;
                totalMissingPurchaseData += totals.missingPurchaseData;
                totalActualPurchaseData += totals.actualPurchaseData;
                totalEstimatedPurchaseData += totals.estimatedPurchaseData;
                totalFallbackPurchaseData += totals.fallbackPurchaseData;
                totalLongDurationTrips += totals.longDurationTrips;
                totalDuration += totals.durationMinutes;
                totalDistance += totals.distance;
            }

            StringBuilder resourceSummary = new StringBuilder();
            foreach (Resource resource in Enum.GetValues(typeof(Resource)))
            {
                if (!this.m_ShoppingHourlyTotals.TryGetValue(resource, out ShoppingHourlyTotals totals) || (totals.startedTrips == 0 && totals.completedTrips == 0))
                    continue;

                if (resourceSummary.Length > 0)
                    resourceSummary.Append("; ");

                float avgResourceDuration = totals.completedTrips > 0 ? totals.durationMinutes / totals.completedTrips : 0f;
                int purchaseDataTrips = totals.actualPurchaseData + totals.estimatedPurchaseData + totals.fallbackPurchaseData;
                float avgResourceDistance = purchaseDataTrips > 0 ? totals.distance / purchaseDataTrips : 0f;
                resourceSummary.Append(resource);
                resourceSummary.Append("(started=");
                resourceSummary.Append(totals.startedTrips);
                resourceSummary.Append(",completed=");
                resourceSummary.Append(totals.completedTrips);
                resourceSummary.Append(",amount=");
                resourceSummary.Append(totals.amount);
                resourceSummary.Append(",spend=");
                resourceSummary.Append(totals.cost);
                resourceSummary.Append(",avgDuration=");
                resourceSummary.Append(FormatLogFloat(avgResourceDuration));
                resourceSummary.Append(",avgDistance=");
                resourceSummary.Append(FormatLogFloat(avgResourceDistance));
                if (totals.missingPurchaseData > 0)
                {
                    resourceSummary.Append(",missingPurchaseData=");
                    resourceSummary.Append(totals.missingPurchaseData);
                }
                if (totals.actualPurchaseData > 0)
                {
                    resourceSummary.Append(",actualPurchaseData=");
                    resourceSummary.Append(totals.actualPurchaseData);
                }
                if (totals.estimatedPurchaseData > 0)
                {
                    resourceSummary.Append(",estimatedPurchaseData=");
                    resourceSummary.Append(totals.estimatedPurchaseData);
                }
                if (totals.fallbackPurchaseData > 0)
                {
                    resourceSummary.Append(",fallbackPurchaseData=");
                    resourceSummary.Append(totals.fallbackPurchaseData);
                }
                if (totals.longDurationTrips > 0)
                {
                    resourceSummary.Append(",longDurationTrips=");
                    resourceSummary.Append(totals.longDurationTrips);
                }
                resourceSummary.Append(")");
            }

            int totalPurchaseDataTrips = totalActualPurchaseData + totalEstimatedPurchaseData + totalFallbackPurchaseData;
            float avgDuration = totalCompletedTrips > 0 ? totalDuration / totalCompletedTrips : 0f;
            float avgDistance = totalPurchaseDataTrips > 0 ? totalDistance / totalPurchaseDataTrips : 0f;
            string resources = resourceSummary.Length > 0 ? resourceSummary.ToString() : "none";

            Mod.log.Info(
                $"ShoppingHourlyLog hour={this.m_ShoppingLogHourStart:yyyy-MM-dd HH}:00 started={totalStartedTrips} completed={totalCompletedTrips} trips={totalCompletedTrips} amount={totalAmount} spend={totalCost} avgDurationMinutes={FormatLogFloat(avgDuration)} avgDistance={FormatLogFloat(avgDistance)} actualPurchaseData={totalActualPurchaseData} estimatedPurchaseData={totalEstimatedPurchaseData} fallbackPurchaseData={totalFallbackPurchaseData} missingPurchaseData={totalMissingPurchaseData} longDurationTrips={totalLongDurationTrips} resources={resources}");

            this.m_ShoppingHourlyTotals.Clear();
        }

        private static string FormatLogFloat(float value)
        {
            return value.ToString("F1", CultureInfo.InvariantCulture);
        }

        private void ClearShoppingLogState()
        {
            while (this.m_ShoppingLogQueue.IsCreated && this.m_ShoppingLogQueue.TryDequeue(out ShoppingLogEvent _))
            {
            }

            this.m_ShoppingHourlyTotals.Clear();
            this.m_ShoppingLogHourInitialized = false;
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

        public Time2WorkCitizenTravelPurposeSystem()
        {
        }

        private struct ShoppingLogEvent
        {
            public Resource resource;
            public int startedTrips;
            public int completedTrips;
            public int amount;
            public int cost;
            public int missingPurchaseData;
            public int actualPurchaseData;
            public int estimatedPurchaseData;
            public int fallbackPurchaseData;
            public int longDurationTrips;
            public float distance;
            public float durationMinutes;
        }

        private struct ShoppingHourlyTotals
        {
            public int startedTrips;
            public int completedTrips;
            public int amount;
            public int cost;
            public int missingPurchaseData;
            public int actualPurchaseData;
            public int estimatedPurchaseData;
            public int fallbackPurchaseData;
            public int longDurationTrips;
            public float durationMinutes;
            public float distance;
        }

        [BurstCompile]
        private struct CitizenArriveJob : IJobChunk
        {
            [ReadOnly]
            public EntityTypeHandle m_EntityType;
            [ReadOnly]
            public ComponentTypeHandle<CurrentBuilding> m_CurrentBuildingType;
            public ComponentTypeHandle<TravelPurpose> m_TravelPurposeType;
            [ReadOnly]
            public ComponentTypeHandle<HealthProblem> m_HealthProblemType;
            [ReadOnly]
            public ComponentTypeHandle<Arrived> m_ArrivedType;
            public ComponentLookup<Shopper> m_Shopping;
            [ReadOnly]
            public ComponentLookup<Worker> m_Workers;
            [ReadOnly]
            public ComponentLookup<Game.Citizens.Student> m_Students;
            [ReadOnly]
            public ComponentLookup<WorkProvider> m_WorkProviders;
            [ReadOnly]
            public ComponentLookup<Building> m_BuildingData;
            [ReadOnly]
            public ComponentLookup<Game.Buildings.School> m_Schools;
            [ReadOnly]
            public ComponentLookup<Game.Buildings.PoliceStation> m_PoliceStationData;
            [ReadOnly]
            public ComponentLookup<Game.Buildings.Prison> m_PrisonData;
            [ReadOnly]
            public ComponentLookup<Game.Buildings.Hospital> m_HospitalData;
            [ReadOnly]
            public ComponentLookup<Game.Buildings.DeathcareFacility> m_DeathcareFacilityData;
            [ReadOnly]
            public ComponentLookup<Game.Buildings.EmergencyShelter> m_EmergencyShelterData;
            [ReadOnly]
            public ComponentLookup<Citizen> m_Citizens;
            [ReadOnly]
            public ComponentLookup<HealthProblem> m_HealthProblems;
            [ReadOnly]
            public ComponentLookup<HouseholdMember> m_HouseholdMembers;
            [ReadOnly]
            public ComponentLookup<MovingAway> m_MovingAways;
            public ComponentLookup<CitizenSchedule> CitizenScheduleLookup;
            public EntityCommandBuffer.ParallelWriter m_CommandBuffer;
            public NativeQueue<Time2WorkCitizenTravelPurposeSystem.Arrive>.ParallelWriter m_ArriveQueue;
            public NativeQueue<ShoppingLogEvent>.ParallelWriter m_ShoppingLogQueue;
            public EconomyParameterData m_EconomyParameters;
            public float m_NormalizedTime;
            public bool m_LogShopping;
            public RandomSeed m_RandomSeed;
            public int lunch_break_pct;
            public int school_start_time;
            public int school_end_time;
            public float work_start_time;
            public float work_end_time;
            public float delayFactor;
            public int ticksPerDay;
            public int part_time_prob;
            public float commute_top10;
            public float overtime;
            public float part_time_reduction;
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
            public bool newyearseve;
            public int dow;
            //public int avg_time_hospital;
            //public int avg_time_prison;

            private bool IsSleepAllowed(Entity citizenEntity)
            {
                HouseholdMember componentData;
                return this.m_HouseholdMembers.TryGetComponent(citizenEntity, out componentData) && !this.m_MovingAways.HasComponent(componentData.m_Household);
            }

            public void Execute(
              in ArchetypeChunk chunk,
              int unfilteredChunkIndex,
              bool useEnabledMask,
              in v128 chunkEnabledMask)
            {
                
                NativeArray<Entity> nativeArray1 = chunk.GetNativeArray(this.m_EntityType);
                
                NativeArray<TravelPurpose> nativeArray2 = chunk.GetNativeArray<TravelPurpose>(ref this.m_TravelPurposeType);
                
                NativeArray<CurrentBuilding> nativeArray3 = chunk.GetNativeArray<CurrentBuilding>(ref this.m_CurrentBuildingType);
                Unity.Mathematics.Random random = this.m_RandomSeed.GetRandom(unfilteredChunkIndex);

                bool flag1 = chunk.Has<HealthProblem>(ref this.m_HealthProblemType);
                for (int index = 0; index < chunk.Count; ++index)
                {
                    
                    bool flag2 = chunk.IsComponentEnabled<Arrived>(ref this.m_ArrivedType, index);
                    Entity entity = nativeArray1[index];
                    TravelPurpose travelPurpose = nativeArray2[index];
                    
                    if (flag1 && CitizenUtils.IsDead(entity, ref this.m_HealthProblems) && travelPurpose.m_Purpose != Game.Citizens.Purpose.Deathcare && travelPurpose.m_Purpose != Game.Citizens.Purpose.InDeathcare && travelPurpose.m_Purpose != Game.Citizens.Purpose.Hospital && travelPurpose.m_Purpose != Game.Citizens.Purpose.InHospital)
                    {
                        
                        this.m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, entity);
                    }
                    else if (travelPurpose.m_Purpose == Game.Citizens.Purpose.Sleeping)
                    {
                        float2 time2Work = new float2(-1f, -1f);

                        bool hasSchedule = this.CitizenScheduleLookup.HasComponent(entity);
                        if (hasSchedule)
                        {
                            CitizenSchedule citizenSchedule = this.CitizenScheduleLookup[entity];
                            time2Work = new float2(citizenSchedule.go_to_work, citizenSchedule.end_work);
                            Citizen citizen = this.m_Citizens[entity];

                            float2 time2Sleep;
                            if (!this.IsSleepAllowed(entity) || !Time2WorkCitizenBehaviorSystem.IsSleepTime(entity, citizen, ref this.m_EconomyParameters, this.m_NormalizedTime, ref this.m_Workers, ref this.m_Students, time2Work, out time2Sleep, newyearseve, dow))
                            {

                                this.m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, entity);

                                if (nativeArray3.Length != 0 && this.m_BuildingData.HasComponent(nativeArray3[index].m_CurrentBuilding))
                                {
                                    this.m_ArriveQueue.Enqueue(new Time2WorkCitizenTravelPurposeSystem.Arrive(entity, nativeArray3[index].m_CurrentBuilding, Time2WorkCitizenTravelPurposeSystem.ArriveType.WakeUp));
                                }
                            }
                        }
                        else
                        {
                            //If CitizenSchedule hasn't been created yet, use old code
                            Citizen citizen = this.m_Citizens[entity];
                            if (!Time2WorkCitizenBehaviorSystem.IsSleepTime(entity, citizen, ref this.m_EconomyParameters, this.m_NormalizedTime, ref this.m_Workers, ref this.m_Students, lunch_break_pct, school_start_time, school_end_time, work_start_time, work_end_time, delayFactor, ticksPerDay, part_time_prob, commute_top10, overtime, part_time_reduction,newyearseve,dow))
                            {
                                this.m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, entity);

                                if (nativeArray3.Length != 0 && this.m_BuildingData.HasComponent(nativeArray3[index].m_CurrentBuilding))
                                {
                                    this.m_ArriveQueue.Enqueue(new Time2WorkCitizenTravelPurposeSystem.Arrive(entity, nativeArray3[index].m_CurrentBuilding, Time2WorkCitizenTravelPurposeSystem.ArriveType.WakeUp));
                                }
                            }
                        }
                    }
                    else if (travelPurpose.m_Purpose == Game.Citizens.Purpose.VisitAttractions)
                    {
                        if (flag2)
                        {
                            this.m_CommandBuffer.SetComponentEnabled<Arrived>(unfilteredChunkIndex, entity, false);
                        }
                        if (random.NextInt(100) == 0)
                        {
                            this.m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, entity);

                        }
                    }
                    else if (flag2)
                    {
                        
                        this.m_CommandBuffer.SetComponentEnabled<Arrived>(unfilteredChunkIndex, entity, false);
                        switch (travelPurpose.m_Purpose)
                        {
                            case Game.Citizens.Purpose.Leisure:
                                //Mod.log.Info($"index: {entity.Index}, arrived leisure: {travelPurpose.m_Resource}");
                                Shopper shopper;
                                if (this.m_Shopping.TryGetComponent(entity, out shopper))
                                {
                                    shopper.duration += (m_NormalizedTime - shopper.start_time);
                                    this.m_CommandBuffer.SetComponent<Shopper>(unfilteredChunkIndex, entity, shopper);
                                    //Mod.log.Info($"Shopper duration: {shopper.duration}, start time: {shopper.start_time}, current time: {m_NormalizedTime}");
                                }
                                continue;
                            case Game.Citizens.Purpose.None:
                            case Game.Citizens.Purpose.Exporting:
                            case Game.Citizens.Purpose.MovingAway:
                            case Game.Citizens.Purpose.Safety:
                            case Game.Citizens.Purpose.Escape:
                            case Game.Citizens.Purpose.Traveling:
                            case Game.Citizens.Purpose.SendMail:
                            case Game.Citizens.Purpose.Disappear:
                            case Game.Citizens.Purpose.WaitingHome:
                            case Game.Citizens.Purpose.PathFailed:
                                this.m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, entity);
                                continue;
                            case Game.Citizens.Purpose.Shopping:
                                shoppingTime(unfilteredChunkIndex, entity, travelPurpose);
                                continue;
                            case Game.Citizens.Purpose.GoingHome:
                                
                                if (nativeArray3.Length != 0 && this.m_BuildingData.HasComponent(nativeArray3[index].m_CurrentBuilding))
                                {
                                    this.m_ArriveQueue.Enqueue(new Time2WorkCitizenTravelPurposeSystem.Arrive(entity, nativeArray3[index].m_CurrentBuilding, Time2WorkCitizenTravelPurposeSystem.ArriveType.Resident));
                                }
                                
                                this.m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, entity);
                                continue;
                            case Game.Citizens.Purpose.GoingToWork:
                                
                                if (nativeArray3.Length != 0 && this.m_BuildingData.HasComponent(nativeArray3[index].m_CurrentBuilding))
                                {
                                    this.m_ArriveQueue.Enqueue(new Time2WorkCitizenTravelPurposeSystem.Arrive(entity, nativeArray3[index].m_CurrentBuilding, Time2WorkCitizenTravelPurposeSystem.ArriveType.Worker));
                                }
                                
                                if (this.m_Workers.HasComponent(entity))
                                {
                                    
                                    
                                    if (this.m_WorkProviders.HasComponent(this.m_Workers[entity].m_Workplace))
                                    {
                                        travelPurpose.m_Purpose = Game.Citizens.Purpose.Working;
                                        nativeArray2[index] = travelPurpose;
                                        continue;
                                    }
                                    
                                    this.m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, entity);
                                    continue;
                                }
                                continue;
                            case Game.Citizens.Purpose.GoingToSchool:
                                
                                if (this.m_Students.HasComponent(entity))
                                {
                                    
                                    
                                    if (this.m_Schools.HasComponent(this.m_Students[entity].m_School))
                                    {
                                        travelPurpose.m_Purpose = Game.Citizens.Purpose.Studying;
                                        nativeArray2[index] = travelPurpose;
                                        continue;
                                    }
                                    
                                    this.m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, entity);
                                    continue;
                                }
                                
                                this.m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, entity);
                                continue;
                            case Game.Citizens.Purpose.Hospital:
                                
                                if (nativeArray3.Length != 0 && this.m_HospitalData.HasComponent(nativeArray3[index].m_CurrentBuilding))
                                {
                                    travelPurpose.m_Purpose = Game.Citizens.Purpose.InHospital;
                                    nativeArray2[index] = travelPurpose;
                                    this.m_ArriveQueue.Enqueue(new Time2WorkCitizenTravelPurposeSystem.Arrive(entity, nativeArray3[index].m_CurrentBuilding, Time2WorkCitizenTravelPurposeSystem.ArriveType.Patient));
                                    //buildingTime(unfilteredChunkIndex, entity, avg_time_hospital);
                                    continue;
                                }

                                this.m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, entity);
                                continue;
                            case Game.Citizens.Purpose.EmergencyShelter:
                                
                                if (nativeArray3.Length != 0 && this.m_EmergencyShelterData.HasComponent(nativeArray3[index].m_CurrentBuilding))
                                {
                                    travelPurpose.m_Purpose = Game.Citizens.Purpose.InEmergencyShelter;
                                    nativeArray2[index] = travelPurpose;
                                    this.m_ArriveQueue.Enqueue(new Time2WorkCitizenTravelPurposeSystem.Arrive(entity, nativeArray3[index].m_CurrentBuilding, Time2WorkCitizenTravelPurposeSystem.ArriveType.Occupant));
                                    continue;
                                }
                                
                                this.m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, entity);
                                continue;
                            case Game.Citizens.Purpose.GoingToJail:
                                
                                if (nativeArray3.Length != 0 && this.m_PoliceStationData.HasComponent(nativeArray3[index].m_CurrentBuilding))
                                {
                                    travelPurpose.m_Purpose = Game.Citizens.Purpose.InJail;
                                    nativeArray2[index] = travelPurpose;
                                    this.m_ArriveQueue.Enqueue(new Time2WorkCitizenTravelPurposeSystem.Arrive(entity, nativeArray3[index].m_CurrentBuilding, Time2WorkCitizenTravelPurposeSystem.ArriveType.Occupant));
                                    //buildingTime(unfilteredChunkIndex, entity, avg_time_prison);
                                    continue;
                                } 

                                this.m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, entity); 
                                continue;
                            case Game.Citizens.Purpose.GoingToPrison:
                                
                                if (nativeArray3.Length != 0 && this.m_PrisonData.HasComponent(nativeArray3[index].m_CurrentBuilding))
                                {
                                    travelPurpose.m_Purpose = Game.Citizens.Purpose.InPrison;
                                    nativeArray2[index] = travelPurpose;
                                    this.m_ArriveQueue.Enqueue(new Time2WorkCitizenTravelPurposeSystem.Arrive(entity, nativeArray3[index].m_CurrentBuilding, Time2WorkCitizenTravelPurposeSystem.ArriveType.Occupant));
                                    //buildingTime(unfilteredChunkIndex, entity, avg_time_prison);
                                    continue;
                                } 

                                this.m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, entity);
                                continue;
                            case Game.Citizens.Purpose.Deathcare:
                                
                                if (nativeArray3.Length != 0 && this.m_DeathcareFacilityData.HasComponent(nativeArray3[index].m_CurrentBuilding))
                                {
                                    travelPurpose.m_Purpose = Game.Citizens.Purpose.InDeathcare;
                                    nativeArray2[index] = travelPurpose;
                                    this.m_ArriveQueue.Enqueue(new Time2WorkCitizenTravelPurposeSystem.Arrive(entity, nativeArray3[index].m_CurrentBuilding, Time2WorkCitizenTravelPurposeSystem.ArriveType.Patient));
                                    continue;
                                }
                                
                                this.m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, entity);
                                continue;
                            default:
                                continue;
                        }
                    }
                }
            }

            private void shoppingTime(int unfilteredChunkIndex, Entity entity, TravelPurpose travelPurpose)
            {
                Resource resource = travelPurpose.m_Resource;
                Shopper shopper;
                if (!this.m_Shopping.TryGetComponent(entity, out shopper))
                {
                    float shopping_time = 0f;
                    switch (resource)
                    {
                        case Resource.Beverages:
                            shopping_time = avg_time_beverages / 1440f;
                            break;
                        case Resource.Chemicals:
                            shopping_time = avg_time_chemicals / 1440f;
                            break;
                        case Resource.ConvenienceFood:
                            shopping_time = avg_time_convenienceFood / 1440f;
                            break;
                        case Resource.Electronics:
                            shopping_time = avg_time_electronics / 1440f;
                            break;
                        case Resource.Software:
                            shopping_time = avg_time_software / 1440f;
                            break;
                        case Resource.Financial:
                            shopping_time = avg_time_financial / 1440f;
                            break;
                        case Resource.Food:
                            shopping_time = avg_time_food / 1440f;
                            break;
                        case Resource.Furniture:
                            shopping_time = avg_time_furniture / 1440f;
                            break;
                        case Resource.Meals:
                            shopping_time = avg_time_meals / 1440f;
                            break;
                        case Resource.Media:
                            shopping_time = avg_time_media / 1440f;
                            break;
                        case Resource.Paper:
                            shopping_time = avg_time_paper / 1440f;
                            break;
                        case Resource.Petrochemicals:
                            shopping_time = avg_time_petrochemicals / 1440f;
                            break;
                        case Resource.Pharmaceuticals:
                            shopping_time = avg_time_pharmaceuticals / 1440f;
                            break;
                        case Resource.Plastics:
                            shopping_time = avg_time_plastics / 1440f;
                            break;
                        case Resource.Telecom:
                            shopping_time = avg_time_telecom / 1440f;
                            break;
                        case Resource.Textiles:
                            shopping_time = avg_time_textiles / 1440f;
                            break;
                        case Resource.Recreation:
                            shopping_time = avg_time_recreation / 1440f;
                            break;
                        case Resource.Entertainment:
                            shopping_time = avg_time_entertainment / 1440f;
                            break;
                        case Resource.Vehicles:
                            shopping_time = avg_time_vehicles / 1440f;
                            break;
                        default:
                            break;
                    }


                    Citizen citizen = this.m_Citizens[entity];
                    uint seed = (uint)(citizen.m_PseudoRandom + 1000 * m_NormalizedTime);
                    Unity.Mathematics.Random random2 = Unity.Mathematics.Random.CreateFromIndex(seed);
                    // Add + or - variation on shopping time by 30% of the time defined above
                    float random_factor = 0.8f;
                    if (shopping_time <= 10f / 1440f)
                    {
                        random_factor = 0.5f;
                    }

                    shopping_time += (float)(GaussianRandom.NextGaussianDouble(random2) * random_factor * shopping_time);
                    float duration = this.m_NormalizedTime + shopping_time;
                    if (duration > 1)
                    {
                        duration -= 1f;
                    }

                    this.m_CommandBuffer.AddComponent<Shopper>(unfilteredChunkIndex, entity, new Shopper(duration, this.m_NormalizedTime));
                    if (m_LogShopping)
                    {
                        m_ShoppingLogQueue.Enqueue(new ShoppingLogEvent()
                        {
                            resource = resource,
                            startedTrips = 1
                        });
                    }
                }
            }

            private void buildingTime(int unfilteredChunkIndex, Entity entity, float avg_time)
            {
                Shopper shopper;
                if (!this.m_Shopping.TryGetComponent(entity, out shopper))
                {
                    float time = avg_time / 1440f;
                    
                    Citizen citizen = this.m_Citizens[entity];
                    uint seed = (uint)(citizen.m_PseudoRandom + 1000 * m_NormalizedTime);
                    Unity.Mathematics.Random random2 = Unity.Mathematics.Random.CreateFromIndex(seed);
                    float random_factor = 0.5f;
                    
                    time += (float)(GaussianRandom.NextGaussianDouble(random2) * random_factor * time);
                    float duration = this.m_NormalizedTime + time;
                    if (duration > 1f)
                    {
                        duration -= 1f;
                    }
                    
                    this.m_CommandBuffer.AddComponent<Shopper>(unfilteredChunkIndex, entity, new Shopper(duration, this.m_NormalizedTime));
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

        [BurstCompile]
        private struct CitizenStopShoppingJob : IJobChunk
        {
            [ReadOnly]
            public EntityTypeHandle m_EntityType;
            [ReadOnly]
            public ComponentTypeHandle<CurrentBuilding> m_CurrentBuildingType;
            public ComponentTypeHandle<TravelPurpose> m_TravelPurposeType;
            public ComponentLookup<Shopper> m_Shopping;
            [ReadOnly]
            public ComponentLookup<ShoppingPurchaseData> m_ShoppingPurchaseData;
            [ReadOnly]
            public ResourcePrefabs m_ResourcePrefabs;
            [ReadOnly]
            public ComponentLookup<ResourceData> m_ResourceDatas;
            public EntityCommandBuffer.ParallelWriter m_CommandBuffer;
            public NativeQueue<ShoppingLogEvent>.ParallelWriter m_ShoppingLogQueue;
            public float m_NormalizedTime;
            public bool m_LogShopping;

            private int EstimateShoppingCost(TravelPurpose travelPurpose, int amount)
            {
                if (travelPurpose.m_Resource == Resource.NoResource || amount <= 0)
                    return 0;

                float price = travelPurpose.m_Purpose == Game.Citizens.Purpose.CompanyShopping
                    ? EconomyUtils.GetIndustrialPrice(travelPurpose.m_Resource, this.m_ResourcePrefabs, ref this.m_ResourceDatas)
                    : EconomyUtils.GetMarketPrice(travelPurpose.m_Resource, this.m_ResourcePrefabs, ref this.m_ResourceDatas);

                return math.max(0, (int)math.round(price * amount));
            }

            private static float GetElapsed(float start, float end)
            {
                start = math.frac(start);
                end = math.frac(end);
                return end >= start ? end - start : 1f - start + end;
            }

            public void Execute(
              in ArchetypeChunk chunk,
              int unfilteredChunkIndex,
              bool useEnabledMask,
              in v128 chunkEnabledMask)
            {

                NativeArray<Entity> nativeArray1 = chunk.GetNativeArray(this.m_EntityType);
                NativeArray<TravelPurpose> nativeArray2 = chunk.GetNativeArray<TravelPurpose>(ref this.m_TravelPurposeType);

                for (int index = 0; index < chunk.Count; ++index)
                {

                    Entity entity = nativeArray1[index];
                    TravelPurpose travelPurpose = nativeArray2[index];

                    Shopper shopper;
                    if (this.m_Shopping.TryGetComponent(entity, out shopper))
                    {
                        float elapsed = GetElapsed(shopper.start_time, this.m_NormalizedTime);
                        float plannedDuration = GetElapsed(shopper.start_time, shopper.duration);
                        if (elapsed >= plannedDuration)
                        {
                            bool hasPurchaseData = m_ShoppingPurchaseData.HasComponent(entity);
                            if (m_LogShopping && (travelPurpose.m_Purpose == Game.Citizens.Purpose.Shopping || travelPurpose.m_Purpose == Game.Citizens.Purpose.CompanyShopping))
                            {
                                float durationMinutes = elapsed * 1440f;
                                ShoppingLogEvent logEvent = new ShoppingLogEvent()
                                {
                                    resource = travelPurpose.m_Resource,
                                    completedTrips = 1,
                                    amount = math.max(0, travelPurpose.m_Data),
                                    durationMinutes = durationMinutes,
                                    longDurationTrips = durationMinutes > 360f ? 1 : 0,
                                    missingPurchaseData = hasPurchaseData ? 0 : 1
                                };

                                if (hasPurchaseData)
                                {
                                    ShoppingPurchaseData purchase = m_ShoppingPurchaseData[entity];
                                    logEvent.resource = purchase.resource;
                                    logEvent.amount = purchase.amount;
                                    logEvent.cost = purchase.cost;
                                    logEvent.distance = purchase.distance;
                                    logEvent.missingPurchaseData = 0;
                                    if (purchase.source == ShoppingPurchaseData.SourceActual && !purchase.estimatedCost)
                                    {
                                        logEvent.actualPurchaseData = 1;
                                    }
                                    else if (purchase.source == ShoppingPurchaseData.SourceFallback)
                                    {
                                        logEvent.fallbackPurchaseData = 1;
                                    }
                                    else
                                    {
                                        logEvent.estimatedPurchaseData = 1;
                                    }
                                }
                                else if (logEvent.resource != Resource.NoResource && logEvent.amount > 0)
                                {
                                    logEvent.cost = EstimateShoppingCost(travelPurpose, logEvent.amount);
                                    logEvent.fallbackPurchaseData = 1;
                                    logEvent.missingPurchaseData = 0;
                                }

                                m_ShoppingLogQueue.Enqueue(logEvent);
                            }

                            if (hasPurchaseData)
                            {
                                this.m_CommandBuffer.RemoveComponent<ShoppingPurchaseData>(unfilteredChunkIndex, entity);
                            }
                            this.m_CommandBuffer.RemoveComponent<Shopper>(unfilteredChunkIndex, entity);
                            this.m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, entity);
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
                this.Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
            }
        }


        private struct Arrive
        {
            public Entity m_Citizen;
            public Entity m_Target;
            public Time2WorkCitizenTravelPurposeSystem.ArriveType m_Type;

            public Arrive(Entity citizen, Entity target, Time2WorkCitizenTravelPurposeSystem.ArriveType type)
            {
                m_Citizen = citizen;
                m_Target = target;
                m_Type = type;
            }
        }


        private enum ArriveType
        {
            Patient,
            Occupant,
            Resident,
            Worker,
            WakeUp,
        }

        [BurstCompile]
        private struct ArriveJob : IJob
        {
            public ComponentLookup<CitizenPresence> m_CitizenPresenceData;
            public BufferLookup<Patient> m_Patients;
            public BufferLookup<Occupant> m_Occupants;
            public ComponentLookup<Household> m_Households;
            public BufferLookup<HouseholdCitizen> m_HouseholdCitizens;
            [ReadOnly]
            public ComponentLookup<PropertyRenter> m_PropertyRenters;
            [ReadOnly]
            public ComponentLookup<HouseholdMember> m_HouseholdMembers;
            public NativeQueue<StatisticsEvent> m_StatisticsQueue;
            public NativeQueue<Time2WorkCitizenTravelPurposeSystem.Arrive> m_ArriveQueue;

            private void SetPresent(Time2WorkCitizenTravelPurposeSystem.Arrive arrive)
            {

                if (!this.m_CitizenPresenceData.HasComponent(arrive.m_Target))
                    return;

                CitizenPresence citizenPresence = this.m_CitizenPresenceData[arrive.m_Target];
                citizenPresence.m_Delta = (sbyte)math.min((int)sbyte.MaxValue, (int)citizenPresence.m_Delta + 1);
                
                
                this.m_CitizenPresenceData[arrive.m_Target] = citizenPresence;
            }

            public void Execute()
            {
                
                int count = this.m_ArriveQueue.Count;
                for (int index = 0; index < count; ++index)
                {
                    Time2WorkCitizenTravelPurposeSystem.Arrive arrive = this.m_ArriveQueue.Dequeue();
                    Time2WorkCitizenTravelPurposeSystem.ArriveType type = arrive.m_Type;
                    switch (type)
                    {
                        case Time2WorkCitizenTravelPurposeSystem.ArriveType.Patient:

                            if (this.m_Patients.HasBuffer(arrive.m_Target))
                            {
                                CollectionUtils.TryAddUniqueValue<Patient>(this.m_Patients[arrive.m_Target], new Patient(arrive.m_Citizen));
                                break;
                            }
                            break;
                        case Time2WorkCitizenTravelPurposeSystem.ArriveType.Occupant:
                            
                            
                            if (this.m_Occupants.HasBuffer(arrive.m_Target))
                            {
                                CollectionUtils.TryAddUniqueValue<Occupant>(this.m_Occupants[arrive.m_Target], new Occupant(arrive.m_Citizen));
                                break;
                            }
                            break;
                        case Time2WorkCitizenTravelPurposeSystem.ArriveType.Resident:
   
                            Entity household1 = this.m_HouseholdMembers[arrive.m_Citizen].m_Household;

                            if (this.m_PropertyRenters.HasComponent(household1) && this.m_PropertyRenters[household1].m_Property == arrive.m_Target)
                            {
                                
                                Household household2 = this.m_Households[household1];
                                
                                if (this.m_HouseholdCitizens.HasBuffer(household1) && (household2.m_Flags & HouseholdFlags.MovedIn) == HouseholdFlags.None)
                                {
                                    this.m_StatisticsQueue.Enqueue(new StatisticsEvent()
                                    {
                                        m_Statistic = StatisticType.CitizensMovedIn,
                                        m_Change = (float)this.m_HouseholdCitizens[household1].Length
                                    });
                                }
                                household2.m_Flags |= HouseholdFlags.MovedIn;
                                
                                this.m_Households[household1] = household2;
                            }
                            this.SetPresent(arrive);
                            break;
                        case Time2WorkCitizenTravelPurposeSystem.ArriveType.Worker:
                        case Time2WorkCitizenTravelPurposeSystem.ArriveType.WakeUp:
                            this.SetPresent(arrive);
                            break;
                    }
                }
            }
        }

        [BurstCompile]
        private struct CitizenStuckJob : IJobChunk
        {
            [ReadOnly]
            public EntityTypeHandle m_EntityType;
            [ReadOnly]
            public ComponentTypeHandle<HouseholdMember> m_HouseholdMemberType;
            [ReadOnly]
            public ComponentTypeHandle<HealthProblem> m_HealthProblemType;
            public ComponentTypeHandle<Citizen> m_CitizenType;
            [ReadOnly]
            public ComponentLookup<Household> m_Households;
            [ReadOnly]
            public ComponentLookup<MovingAway> m_MovingAways;
            [ReadOnly]
            public ComponentLookup<PropertyRenter> m_PropertyRenters;
            [ReadOnly]
            public ComponentLookup<Building> m_Buildings;
            [ReadOnly]
            public NativeList<Entity> m_OutsideConnections;
            [ReadOnly]
            public NativeList<Entity> m_ServiceBuildings;
            public EntityCommandBuffer.ParallelWriter m_CommandBuffer;
            public RandomSeed m_RandomSeed;

            public void Execute(
              in ArchetypeChunk chunk,
              int unfilteredChunkIndex,
              bool useEnabledMask,
              in v128 chunkEnabledMask)
            {
                
                NativeArray<Entity> nativeArray1 = chunk.GetNativeArray(this.m_EntityType);
                
                NativeArray<HouseholdMember> nativeArray2 = chunk.GetNativeArray<HouseholdMember>(ref this.m_HouseholdMemberType);
                
                NativeArray<HealthProblem> nativeArray3 = chunk.GetNativeArray<HealthProblem>(ref this.m_HealthProblemType);
                
                NativeArray<Citizen> nativeArray4 = chunk.GetNativeArray<Citizen>(ref this.m_CitizenType);
                
                if (nativeArray2.Length < chunk.Count || this.m_OutsideConnections.Length == 0)
                    return;
                for (int index = 0; index < chunk.Count; ++index)
                {
                    Entity entity1 = nativeArray1[index];
                    Entity household = nativeArray2[index].m_Household;
                    
                    
                    bool flag = (this.m_Households[household].m_Flags & HouseholdFlags.MovedIn) != HouseholdFlags.None && !this.m_MovingAways.HasComponent(household);
                    HealthProblem healthProblem;
                    if (CollectionUtils.TryGet<HealthProblem>(nativeArray3, index, out healthProblem) && (healthProblem.m_Flags & HealthProblemFlags.Dead) != HealthProblemFlags.None)
                    {
                        
                        this.m_CommandBuffer.AddComponent<Deleted>(unfilteredChunkIndex, nativeArray1[index]);
                    }
                    else
                    {
                        Entity entity2 = Entity.Null;
                        
                        Unity.Mathematics.Random random = this.m_RandomSeed.GetRandom((1 + index) * (entity1.Index + 1));
                        if (flag)
                        {
                            
                            if (this.m_PropertyRenters.HasComponent(household))
                            {
                                
                                entity2 = this.m_PropertyRenters[household].m_Property;
                            }
                            
                            if (entity2 == Entity.Null && this.m_ServiceBuildings.Length > 0)
                            {
                                int num = 0;
                                do
                                {
                                    ++num;
                                    entity2 = this.m_ServiceBuildings[random.NextInt(this.m_ServiceBuildings.Length)];
                                }
                                while ((!this.m_Buildings.HasComponent(entity2) || this.m_Buildings[entity2].m_RoadEdge == Entity.Null) && num < 10);
                            }
                            
                            
                            if (!this.m_Buildings.HasComponent(entity2) || this.m_Buildings[entity2].m_RoadEdge == Entity.Null)
                            {
                                
                                this.m_CommandBuffer.AddComponent<Deleted>(unfilteredChunkIndex, nativeArray1[index]);
                            }
                        }
                        else
                        {
                            entity2 = this.m_OutsideConnections[random.NextInt(this.m_OutsideConnections.Length)];
                        }
                        if (entity2 != Entity.Null)
                        {
                            
                            this.m_CommandBuffer.AddComponent<CurrentBuilding>(unfilteredChunkIndex, nativeArray1[index], new CurrentBuilding()
                            {
                                m_CurrentBuilding = entity2
                            });
                            
                            this.m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, nativeArray1[index]);
                            Citizen citizen = nativeArray4[index];
                            citizen.m_PenaltyCounter = byte.MaxValue;
                            nativeArray4[index] = citizen;
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
            public ComponentLookup<CitizenSchedule> CitizenScheduleLookup;
            [ReadOnly]
            public ComponentTypeHandle<CurrentBuilding> __Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle;
            public ComponentTypeHandle<TravelPurpose> __Game_Citizens_TravelPurpose_RW_ComponentTypeHandle;
            [ReadOnly]
            public ComponentTypeHandle<HealthProblem> __Game_Citizens_HealthProblem_RO_ComponentTypeHandle;
            [ReadOnly]
            public ComponentLookup<Shopper> __Game_Citizens_Shopping_RW_ComponentLookup;
            [ReadOnly]
            public ComponentTypeHandle<Arrived> __Game_Citizens_Arrived_RO_ComponentTypeHandle;
            [ReadOnly]
            public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Game.Buildings.School> __Game_Buildings_School_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<WorkProvider> __Game_Companies_WorkProvider_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Game.Citizens.Student> __Game_Citizens_Student_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Worker> __Game_Citizens_Worker_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Game.Buildings.PoliceStation> __Game_Buildings_PoliceStation_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Game.Buildings.Prison> __Game_Buildings_Prison_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Game.Buildings.Hospital> __Game_Buildings_Hospital_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Game.Buildings.DeathcareFacility> __Game_Buildings_DeathcareFacility_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Game.Buildings.EmergencyShelter> __Game_Buildings_EmergencyShelter_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Citizen> __Game_Citizens_Citizen_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<HealthProblem> __Game_Citizens_HealthProblem_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<HouseholdMember> __Game_Citizens_HouseholdMember_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<MovingAway> __Game_Agents_MovingAway_RO_ComponentLookup;
            public ComponentLookup<CitizenPresence> __Game_Buildings_CitizenPresence_RW_ComponentLookup;
            public BufferLookup<Patient> __Game_Buildings_Patient_RW_BufferLookup;
            public BufferLookup<Occupant> __Game_Buildings_Occupant_RW_BufferLookup;
            public ComponentLookup<Household> __Game_Citizens_Household_RW_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;
            [ReadOnly]
            public BufferLookup<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferLookup;
            [ReadOnly]
            public ComponentTypeHandle<HouseholdMember> __Game_Citizens_HouseholdMember_RO_ComponentTypeHandle;
            public ComponentTypeHandle<Citizen> __Game_Citizens_Citizen_RW_ComponentTypeHandle;
            [ReadOnly]
            public ComponentLookup<Household> __Game_Citizens_Household_RO_ComponentLookup;
            public ComponentLookup<ShoppingPurchaseData> __Time2Work_Components_ShoppingPurchaseData_RW_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void __AssignHandles(ref SystemState state)
            {
                
                this.__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
                
                this.__Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CurrentBuilding>(true);
                this.CitizenScheduleLookup = state.GetComponentLookup<CitizenSchedule>(true);
                this.__Game_Citizens_TravelPurpose_RW_ComponentTypeHandle = state.GetComponentTypeHandle<TravelPurpose>();
                
                this.__Game_Citizens_HealthProblem_RO_ComponentTypeHandle = state.GetComponentTypeHandle<HealthProblem>(true);
                
                this.__Game_Citizens_Arrived_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Arrived>(true);
                
                this.__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(true);
                
                this.__Game_Buildings_School_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.School>(true);
                
                this.__Game_Companies_WorkProvider_RO_ComponentLookup = state.GetComponentLookup<WorkProvider>(true);
                
                this.__Game_Citizens_Student_RO_ComponentLookup = state.GetComponentLookup<Game.Citizens.Student>(true);
                
                this.__Game_Citizens_Worker_RO_ComponentLookup = state.GetComponentLookup<Worker>(true);
                
                this.__Game_Citizens_Shopping_RW_ComponentLookup = state.GetComponentLookup<Shopper>(false);

                this.__Game_Buildings_PoliceStation_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.PoliceStation>(true);
                
                this.__Game_Buildings_Prison_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.Prison>(true);
                
                this.__Game_Buildings_Hospital_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.Hospital>(true);
                
                this.__Game_Buildings_DeathcareFacility_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.DeathcareFacility>(true);
                
                this.__Game_Buildings_EmergencyShelter_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.EmergencyShelter>(true);
                
                this.__Game_Citizens_Citizen_RO_ComponentLookup = state.GetComponentLookup<Citizen>(true);
                
                this.__Game_Citizens_HealthProblem_RO_ComponentLookup = state.GetComponentLookup<HealthProblem>(true);
                this.__Game_Citizens_HouseholdMember_RO_ComponentLookup = state.GetComponentLookup<HouseholdMember>(true);

                this.__Game_Agents_MovingAway_RO_ComponentLookup = state.GetComponentLookup<MovingAway>(true);

                this.__Game_Buildings_CitizenPresence_RW_ComponentLookup = state.GetComponentLookup<CitizenPresence>();
                
                this.__Game_Buildings_Patient_RW_BufferLookup = state.GetBufferLookup<Patient>();
                
                this.__Game_Buildings_Occupant_RW_BufferLookup = state.GetBufferLookup<Occupant>();
                
                this.__Game_Citizens_Household_RW_ComponentLookup = state.GetComponentLookup<Household>();

                this.__Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(true);
                
                this.__Game_Citizens_HouseholdCitizen_RO_BufferLookup = state.GetBufferLookup<HouseholdCitizen>(true);
                
                this.__Game_Citizens_HouseholdMember_RO_ComponentTypeHandle = state.GetComponentTypeHandle<HouseholdMember>(true);
                
                this.__Game_Citizens_Citizen_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Citizen>();
                
                this.__Game_Citizens_Household_RO_ComponentLookup = state.GetComponentLookup<Household>(true);
                this.__Time2Work_Components_ShoppingPurchaseData_RW_ComponentLookup = state.GetComponentLookup<ShoppingPurchaseData>();
                this.__Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(true);
            }
        }
    }
}
