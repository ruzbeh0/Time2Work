using Game;
using Game.Agents;
using Game.Citizens;
using Game.Common;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using System;
using Time2Work.Bridge;
using Time2Work.Components;
using Time2Work.Utils;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Time2Work.Systems
{
    public partial class SocialLeisureOpportunitySystem : GameSystemBase
    {
        private EntityQuery m_OpportunityQuery;
        private SimulationSystem m_SimulationSystem;
        private Time2WorkTimeSystem m_TimeSystem;

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            return 16;
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            m_SimulationSystem = World.GetOrCreateSystemManaged<SimulationSystem>();
            m_TimeSystem = World.GetOrCreateSystemManaged<Time2WorkTimeSystem>();

            m_OpportunityQuery = GetEntityQuery(new EntityQueryDesc()
            {
                All = new[]
                {
                    ComponentType.ReadWrite<SocialLeisureOpportunity>(),
                    ComponentType.ReadWrite<Citizen>(),
                    ComponentType.ReadOnly<CurrentBuilding>(),
                    ComponentType.ReadWrite<TripNeeded>()
                },
                None = new[]
                {
                    ComponentType.Exclude<Deleted>(),
                    ComponentType.Exclude<Temp>(),
                    ComponentType.Exclude<HealthProblem>()
                }
            });

            RequireForUpdate(m_OpportunityQuery);
        }

        protected override void OnUpdate()
        {
            NativeArray<Entity> entities = m_OpportunityQuery.ToEntityArray(Allocator.Temp);
            try
            {
                for (int i = 0; i < entities.Length; i++)
                {
                    Entity citizen = entities[i];
                    if (!EntityManager.HasComponent<SocialLeisureOpportunity>(citizen))
                        continue;

                    SocialLeisureOpportunity opportunity = EntityManager.GetComponentData<SocialLeisureOpportunity>(citizen);
                    bool handled = TryConvertToSocialTrip(citizen, opportunity);

                    if (!handled)
                    {
                        RestoreOriginalLeisureTrip(citizen, opportunity);
                    }

                    if (EntityManager.HasComponent<SocialLeisureOpportunity>(citizen))
                    {
                        EntityManager.RemoveComponent<SocialLeisureOpportunity>(citizen);
                    }
                }
            }
            finally
            {
                if (entities.IsCreated)
                    entities.Dispose();
            }
        }

        private bool TryConvertToSocialTrip(Entity citizen, SocialLeisureOpportunity opportunity)
        {
            if (!SocialTripsBridge.IsMacroProviderAvailable)
                return false;

            if (!SocialTripsBridge.TryConvertLeisureTrip(
                citizen,
                opportunity.originalTarget,
                opportunity.originalLeisureType,
                out Entity targetBuilding,
                out Entity hostCitizen,
                out int tripType,
                out float durationMinutes,
                out int priority))
            {
                return false;
            }

            bool accepted = SocialTripsBridge.RequestSocialTrip(
                citizen,
                targetBuilding,
                hostCitizen,
                tripType,
                durationMinutes,
                priority);

            if (!accepted)
                return false;

            SocialTripsBridge.NotifySocialTripStarted(citizen, targetBuilding, hostCitizen, tripType);
            return true;
        }

        private void RestoreOriginalLeisureTrip(Entity citizen, SocialLeisureOpportunity opportunity)
        {
            Entity target = opportunity.originalTarget;
            if (!IsValidTarget(target))
                return;

            AddTrip(citizen, target, Purpose.Leisure);
            SetTarget(citizen, target);

            if (EntityManager.HasComponent<Citizen>(citizen))
            {
                Citizen citizenData = EntityManager.GetComponentData<Citizen>(citizen);
                AddShopper(citizen, citizenData, (LeisureType)opportunity.originalLeisureType);
            }
        }

        private bool IsValidTarget(Entity target)
        {
            return target != Entity.Null &&
                   EntityManager.Exists(target) &&
                   !EntityManager.HasComponent<Deleted>(target) &&
                   !EntityManager.HasComponent<Temp>(target);
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

            trips.Add(new TripNeeded()
            {
                m_TargetAgent = target,
                m_Purpose = purpose
            });
        }

        private void SetTarget(Entity citizen, Entity target)
        {
            Target component = new Target()
            {
                m_Target = target
            };

            if (EntityManager.HasComponent<Target>(citizen))
            {
                EntityManager.SetComponentData(citizen, component);
            }
            else
            {
                EntityManager.AddComponentData(citizen, component);
            }
        }

        private void AddShopper(Entity citizen, Citizen citizenData, LeisureType leisureType)
        {
            if (EntityManager.HasComponent<Shopper>(citizen))
                return;

            Setting setting = Mod.m_Setting;
            if (setting == null)
                return;

            float shoppingTime;
            switch (leisureType)
            {
                case LeisureType.Meals:
                    shoppingTime = setting.avg_time_meals / 1440f;
                    break;
                case LeisureType.Entertainment:
                    shoppingTime = setting.avg_time_entertainment / 1440f;
                    break;
                default:
                    shoppingTime = (setting.avg_time_beverages +
                                    setting.avg_time_chemicals +
                                    setting.avg_time_convenienceFood +
                                    setting.avg_time_electronics +
                                    setting.avg_time_food +
                                    setting.avg_time_media +
                                    setting.avg_time_paper +
                                    setting.avg_time_plastics) / (8f * 1440f);
                    break;
            }

            uint seed = (uint)math.max(1, math.abs(citizenData.m_PseudoRandom) + (int)(m_TimeSystem.normalizedTime * 100000f));
            Unity.Mathematics.Random random = Unity.Mathematics.Random.CreateFromIndex(seed);
            float randomFactor = shoppingTime <= 10f / 1440f ? 0.5f : 0.8f;
            shoppingTime += (float)(GaussianRandom.NextGaussianDouble(random) * randomFactor * shoppingTime);

            float startTime = m_TimeSystem.normalizedTime;
            float duration = startTime + math.max(1f / 1440f, shoppingTime);
            if (duration > 1f)
            {
                duration -= 1f;
            }

            EntityManager.AddComponentData(citizen, new Shopper(duration, startTime));
        }
    }
}
