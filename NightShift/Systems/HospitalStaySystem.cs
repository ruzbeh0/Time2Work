using Game;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Simulation;
using Game.Tools;
using Time2Work.Components;
using Unity.Collections;
using Unity.Entities;

namespace Time2Work.Systems
{
    public partial class HospitalStaySystem : GameSystemBase
    {
        private EntityQuery m_HospitalStayQuery;
        private SimulationSystem m_SimulationSystem;

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            return 64;
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            m_SimulationSystem = World.GetOrCreateSystemManaged<SimulationSystem>();

            m_HospitalStayQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadWrite<HospitalStay>(),
                    ComponentType.ReadOnly<Citizen>(),
                    ComponentType.ReadOnly<CurrentBuilding>()
                },
                None = new[]
                {
                    ComponentType.Exclude<Deleted>(),
                    ComponentType.Exclude<Temp>()
                }
            });

            RequireForUpdate(m_HospitalStayQuery);
        }

        protected override void OnUpdate()
        {
            Setting setting = Mod.m_Setting;
            NativeArray<Entity> entities = m_HospitalStayQuery.ToEntityArray(Allocator.Temp);
            try
            {
                for (int i = 0; i < entities.Length; i++)
                {
                    Entity citizen = entities[i];
                    if (setting == null || !setting.hospital_stay_duration_enabled)
                    {
                        EntityManager.RemoveComponent<HospitalStay>(citizen);
                        continue;
                    }

                    ProcessHospitalStay(citizen, EntityManager.GetComponentData<HospitalStay>(citizen));
                }
            }
            finally
            {
                if (entities.IsCreated)
                    entities.Dispose();
            }
        }

        private void ProcessHospitalStay(Entity citizen, HospitalStay stay)
        {
            if (!EntityManager.HasComponent<CurrentBuilding>(citizen))
            {
                EntityManager.RemoveComponent<HospitalStay>(citizen);
                return;
            }

            Entity hospital = EntityManager.GetComponentData<CurrentBuilding>(citizen).m_CurrentBuilding;
            if (!IsValidHospital(hospital) || IsDead(citizen))
            {
                EntityManager.RemoveComponent<HospitalStay>(citizen);
                return;
            }

            bool completed = m_SimulationSystem.frameIndex >= stay.endFrame;
            if (!completed)
            {
                EnsureInHospital(citizen, hospital);
                return;
            }

            EntityManager.RemoveComponent<HospitalStay>(citizen);
            if (EntityManager.HasComponent<HealthProblem>(citizen) && !IsDead(citizen))
                return;

            if (EntityManager.HasComponent<TravelPurpose>(citizen))
            {
                TravelPurpose travelPurpose = EntityManager.GetComponentData<TravelPurpose>(citizen);
                if (travelPurpose.m_Purpose == Purpose.InHospital)
                {
                    EntityManager.RemoveComponent<TravelPurpose>(citizen);
                }
            }

            if (EntityManager.HasComponent<Target>(citizen))
            {
                Target target = EntityManager.GetComponentData<Target>(citizen);
                if (target.m_Target == hospital)
                    EntityManager.RemoveComponent<Target>(citizen);
            }

            SendHomeIfPossible(citizen, hospital);
        }

        private void EnsureInHospital(Entity citizen, Entity hospital)
        {
            if (EntityManager.HasComponent<TravelPurpose>(citizen))
            {
                TravelPurpose travelPurpose = EntityManager.GetComponentData<TravelPurpose>(citizen);
                if (travelPurpose.m_Purpose != Purpose.InHospital)
                {
                    travelPurpose.m_Purpose = Purpose.InHospital;
                    EntityManager.SetComponentData(citizen, travelPurpose);
                }
            }
            else
            {
                EntityManager.AddComponentData(citizen, new TravelPurpose
                {
                    m_Purpose = Purpose.InHospital
                });
            }

            if (EntityManager.HasComponent<Target>(citizen))
            {
                Target target = EntityManager.GetComponentData<Target>(citizen);
                if (target.m_Target != hospital)
                {
                    target.m_Target = hospital;
                    EntityManager.SetComponentData(citizen, target);
                }
            }
            else
            {
                EntityManager.AddComponentData(citizen, new Target
                {
                    m_Target = hospital
                });
            }
        }

        private void SendHomeIfPossible(Entity citizen, Entity hospital)
        {
            if (!EntityManager.HasComponent<HouseholdMember>(citizen) ||
                !EntityManager.HasBuffer<TripNeeded>(citizen))
            {
                return;
            }

            Entity household = EntityManager.GetComponentData<HouseholdMember>(citizen).m_Household;
            if (household == Entity.Null ||
                !EntityManager.Exists(household) ||
                !EntityManager.HasComponent<PropertyRenter>(household))
            {
                return;
            }

            Entity home = EntityManager.GetComponentData<PropertyRenter>(household).m_Property;
            if (!IsValidBuilding(home) || home == hospital)
                return;

            AddTrip(citizen, home, Purpose.GoingHome);
        }

        private void AddTrip(Entity citizen, Entity target, Purpose purpose)
        {
            DynamicBuffer<TripNeeded> trips = EntityManager.GetBuffer<TripNeeded>(citizen);
            for (int i = 0; i < trips.Length; i++)
            {
                TripNeeded trip = trips[i];
                if (trip.m_TargetAgent == target && trip.m_Purpose == purpose)
                    return;
            }

            trips.Add(new TripNeeded
            {
                m_TargetAgent = target,
                m_Purpose = purpose,
                m_Priority = 128
            });
        }

        private bool IsValidHospital(Entity hospital)
        {
            return IsValidBuilding(hospital) && EntityManager.HasComponent<Hospital>(hospital);
        }

        private bool IsValidBuilding(Entity building)
        {
            return building != Entity.Null &&
                   EntityManager.Exists(building) &&
                   EntityManager.HasComponent<Building>(building) &&
                   !EntityManager.HasComponent<Deleted>(building) &&
                   !EntityManager.HasComponent<Temp>(building);
        }

        private bool IsDead(Entity citizen)
        {
            if (!EntityManager.HasComponent<HealthProblem>(citizen))
                return false;

            HealthProblem healthProblem = EntityManager.GetComponentData<HealthProblem>(citizen);
            return (healthProblem.m_Flags & HealthProblemFlags.Dead) != HealthProblemFlags.None;
        }
    }
}
