using Colossal.Entities;
using Game;
using Game.Buildings;
using Game.Prefabs;
using Game.Simulation;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace Time2Work.Systems
{
    public partial class SpecialEventsSystem : GameSystemBase
    {
        private Dictionary<Entity, AttractivenessProvider> _attractivenessProviderDictionary = new Dictionary<Entity, AttractivenessProvider>();

        private EntityQuery _query;

        private Setting.DTSimulationEnum m_daytype;

        private bool once = true;

        protected override void OnCreate()
        {
            base.OnCreate();

            _query = GetEntityQuery(new EntityQueryDesc()
            {
                All = new[] {
                    ComponentType.ReadWrite<AttractivenessProvider>(),
                    //ComponentType.ReadWrite<ModifiedServiceCoverage>(),
                    //ComponentType.ReadWrite<CrimeProducer>(),
                    //ComponentType.ReadWrite<WaterConsumer>(),
                    //ComponentType.ReadWrite<GarbageProducer>(),
                    //ComponentType.ReadWrite<ElectricityConsumer>(),
                    //ComponentType.ReadWrite<MaintenanceConsumer>(),
                    ComponentType.ReadOnly<PrefabRef>(),
                }
            });

            RequireForUpdate(_query);
        }

        protected override void OnUpdate()
        {
            var entities = _query.ToEntityArray(Allocator.Temp);

            Mod.log.Info($"Special Event OnUpdate");
            if(!once)
            {
                return;
            }

            foreach (var ent in entities)
            {
                AttractivenessProvider attractivenessProvider;

                if (!_attractivenessProviderDictionary.TryGetValue(ent, out attractivenessProvider))
                {
                    attractivenessProvider = EntityManager.GetComponentData<AttractivenessProvider>(ent);
                    _attractivenessProviderDictionary.Add(ent, attractivenessProvider);
                }

                
                
                //attractivenessProvider.m_Attractiveness = 70;
                //d.log.Info($"New Attract:{attractivenessProvider.m_Attractiveness}");

                //if (((int)this.m_daytype) == (int)Setting.DTSimulationEnum.AverageDay)
                //{
                //    attractivenessProvider.m_CommuterWorkerRatioLimit = 8;
                //} else if (((int)this.m_daytype) == (int)Setting.DTSimulationEnum.Saturday ||
                //    ((int)this.m_daytype) == (int)Setting.DTSimulationEnum.Sunday)
                //{
                //    attractivenessProvider.m_CommuterWorkerRatioLimit = 9;
                //} else
                //{
                //    attractivenessProvider.m_CommuterWorkerRatioLimit = 7;
                //}

                //EntityManager.SetComponentData<AttractivenessProvider>(ent, attractivenessProvider);

                PrefabRef prefabRef;
                if(EntityManager.TryGetComponent<PrefabRef>(ent, out prefabRef))
                {
                    if (attractivenessProvider.m_Attractiveness > 500)
                    {
                        Mod.log.Info($"Special Event at: {this.World.GetOrCreateSystemManaged<PrefabSystem>().GetPrefabName(prefabRef.m_Prefab)}");
                    }
                    //Mod.log.Info($"Prefab:{prefabRef},{prefabRef.m_Prefab}");
                    //PrefabData prefabData;
                    //if (EntityManager.TryGetComponent<PrefabData>(prefabRef.m_Prefab, out prefabData))
                    //{
                    //    Mod.log.Info($"Prefab:{prefabData},{prefabData.m_Index}");
                    //}
                }

                //once = false;
                //return;
            }
        }

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            // One day (or month) in-game is '262144' ticks
            return 262144 / 8;
        }
    }
}