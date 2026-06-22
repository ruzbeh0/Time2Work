using Colossal.Mathematics;
using Game;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Net;
using Game.Simulation;
using Game.Tools;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Time2Work.Systems
{
    public partial class PersonalCarDebugSystem : GameSystemBase
    {
        private const uint kLogIntervalFrames = 4096;
        private const float kRemotePersonalCarDistanceSq = 1000f * 1000f;

        private EntityQuery m_HouseholdVehicleQuery;
        private SimulationSystem m_SimulationSystem;
        private Time2WorkTimeSystem m_TimeSystem;
        private uint m_LastLogFrame;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_SimulationSystem = World.GetOrCreateSystemManaged<SimulationSystem>();
            m_TimeSystem = World.GetOrCreateSystemManaged<Time2WorkTimeSystem>();
            m_HouseholdVehicleQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<Household>(),
                    ComponentType.ReadOnly<PropertyRenter>(),
                    ComponentType.ReadOnly<OwnedVehicle>()
                },
                None = new ComponentType[]
                {
                    ComponentType.ReadOnly<Deleted>(),
                    ComponentType.ReadOnly<Temp>()
                }
            });
            RequireForUpdate(m_HouseholdVehicleQuery);
        }

        protected override void OnUpdate()
        {
            uint frame = m_SimulationSystem.frameIndex;
            if (m_LastLogFrame != 0 && frame - m_LastLogFrame < kLogIntervalFrames)
                return;

            m_LastLogFrame = frame;

            NativeArray<Entity> households = m_HouseholdVehicleQuery.ToEntityArray(Allocator.Temp);
            try
            {
                BufferLookup<OwnedVehicle> ownedVehicles = SystemAPI.GetBufferLookup<OwnedVehicle>(true);
                ComponentLookup<PropertyRenter> propertyRenters = SystemAPI.GetComponentLookup<PropertyRenter>(true);
                ComponentLookup<Game.Vehicles.PersonalCar> personalCars = SystemAPI.GetComponentLookup<Game.Vehicles.PersonalCar>(true);
                ComponentLookup<ParkedCar> parkedCars = SystemAPI.GetComponentLookup<ParkedCar>(true);
                ComponentLookup<Game.Objects.Transform> transforms = SystemAPI.GetComponentLookup<Game.Objects.Transform>(true);
                ComponentLookup<Curve> curves = SystemAPI.GetComponentLookup<Curve>(true);
                ComponentLookup<CurrentBuilding> currentBuildings = SystemAPI.GetComponentLookup<CurrentBuilding>(true);
                ComponentLookup<FixParkingLocation> fixParkingLocations = SystemAPI.GetComponentLookup<FixParkingLocation>(true);
                ComponentLookup<Updated> updateds = SystemAPI.GetComponentLookup<Updated>(true);
                ComponentLookup<Deleted> deleteds = SystemAPI.GetComponentLookup<Deleted>(true);
                ComponentLookup<Temp> temps = SystemAPI.GetComponentLookup<Temp>(true);

                int ownedPersonalCars = 0;
                int parkedPersonalCars = 0;
                int homeTargetCars = 0;
                int fixPendingCars = 0;
                int fixPendingWithoutUpdated = 0;
                int freeCars = 0;
                int freeRemoteCars = 0;
                int keptCars = 0;
                int keptRemoteCars = 0;
                int keptRemoteKeeperAtHome = 0;
                int keptRemoteKeeperNearCar = 0;
                int keptRemoteKeeperAwayFromCar = 0;
                int keptRemoteKeeperLocationUnknown = 0;
                int unknownParkedLocation = 0;

                for (int i = 0; i < households.Length; i++)
                {
                    Entity household = households[i];
                    if (!ownedVehicles.HasBuffer(household) || !propertyRenters.TryGetComponent(household, out PropertyRenter propertyRenter))
                        continue;

                    DynamicBuffer<OwnedVehicle> vehicles = ownedVehicles[household];
                    for (int j = 0; j < vehicles.Length; j++)
                    {
                        Entity car = vehicles[j].m_Vehicle;
                        if (deleteds.HasComponent(car) || temps.HasComponent(car))
                            continue;

                        if (!personalCars.TryGetComponent(car, out Game.Vehicles.PersonalCar personalCar))
                            continue;

                        ownedPersonalCars++;
                        if (parkedCars.HasComponent(car))
                            parkedPersonalCars++;
                        if ((personalCar.m_State & PersonalCarFlags.HomeTarget) != 0)
                            homeTargetCars++;
                        if (fixParkingLocations.HasComponent(car))
                        {
                            fixPendingCars++;
                            if (!updateds.HasComponent(car))
                                fixPendingWithoutUpdated++;
                        }

                        bool locationKnown;
                        bool homeCompatible = IsHomeCompatible(car, personalCar, propertyRenter.m_Property, parkedCars, transforms, curves, out locationKnown);
                        if (!locationKnown)
                            unknownParkedLocation++;

                        if (personalCar.m_Keeper == Entity.Null)
                        {
                            freeCars++;
                            if (!homeCompatible)
                                freeRemoteCars++;
                        }
                        else
                        {
                            keptCars++;
                            if (!homeCompatible)
                            {
                                keptRemoteCars++;
                                if (currentBuildings.TryGetComponent(personalCar.m_Keeper, out CurrentBuilding currentBuilding))
                                {
                                    if (currentBuilding.m_CurrentBuilding == propertyRenter.m_Property)
                                        keptRemoteKeeperAtHome++;

                                    if (parkedCars.TryGetComponent(car, out ParkedCar parkedCar))
                                    {
                                        bool keeperLocationKnown;
                                        bool parkedNearKeeper = IsParkedNearEntity(parkedCar, currentBuilding.m_CurrentBuilding, transforms, curves, out keeperLocationKnown);
                                        if (!keeperLocationKnown)
                                            keptRemoteKeeperLocationUnknown++;
                                        else if (parkedNearKeeper)
                                            keptRemoteKeeperNearCar++;
                                        else
                                            keptRemoteKeeperAwayFromCar++;
                                    }
                                }
                                else
                                {
                                    keptRemoteKeeperLocationUnknown++;
                                }
                            }
                        }
                    }
                }

                Mod.log.Info($"[RT][PersonalCarDebug] frame={frame} time={m_TimeSystem.normalizedTime:0.000} households={households.Length} owned_personal={ownedPersonalCars} parked={parkedPersonalCars} home_target={homeTargetCars} fix_pending={fixPendingCars} fix_pending_without_updated={fixPendingWithoutUpdated} free={freeCars} free_remote={freeRemoteCars} kept={keptCars} kept_remote={keptRemoteCars} kept_remote_keeper_at_home={keptRemoteKeeperAtHome} kept_remote_keeper_near_car={keptRemoteKeeperNearCar} kept_remote_keeper_away_from_car={keptRemoteKeeperAwayFromCar} kept_remote_keeper_location_unknown={keptRemoteKeeperLocationUnknown} unknown_parked_location={unknownParkedLocation}");
            }
            finally
            {
                households.Dispose();
            }
        }

        private static bool IsHomeCompatible(
            Entity car,
            Game.Vehicles.PersonalCar personalCar,
            Entity home,
            ComponentLookup<ParkedCar> parkedCars,
            ComponentLookup<Game.Objects.Transform> transforms,
            ComponentLookup<Curve> curves,
            out bool locationKnown)
        {
            locationKnown = true;

            if ((personalCar.m_State & PersonalCarFlags.HomeTarget) != 0)
                return true;

            if (!parkedCars.TryGetComponent(car, out ParkedCar parkedCar))
                return true;

            if (home == Entity.Null || !transforms.TryGetComponent(home, out Game.Objects.Transform homeTransform) || !curves.TryGetComponent(parkedCar.m_Lane, out Curve curve))
            {
                locationKnown = false;
                return true;
            }

            float3 parkedPosition = MathUtils.Position(curve.m_Bezier, parkedCar.m_CurvePosition);
            return math.distancesq(parkedPosition, homeTransform.m_Position) <= kRemotePersonalCarDistanceSq;
        }

        private static bool IsParkedNearEntity(
            ParkedCar parkedCar,
            Entity target,
            ComponentLookup<Game.Objects.Transform> transforms,
            ComponentLookup<Curve> curves,
            out bool locationKnown)
        {
            locationKnown = true;

            if (target == Entity.Null || !transforms.TryGetComponent(target, out Game.Objects.Transform targetTransform) || !curves.TryGetComponent(parkedCar.m_Lane, out Curve curve))
            {
                locationKnown = false;
                return false;
            }

            float3 parkedPosition = MathUtils.Position(curve.m_Bezier, parkedCar.m_CurvePosition);
            return math.distancesq(parkedPosition, targetTransform.m_Position) <= kRemotePersonalCarDistanceSq;
        }
    }
}
