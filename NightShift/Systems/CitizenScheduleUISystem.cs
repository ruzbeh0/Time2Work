using Colossal.Entities;
using Colossal.PSI.Common;
using Colossal.Serialization.Entities;
using Colossal.UI.Binding;
using Game.Citizens;
using Game.Common;
using Game.Prefabs;
using Game.Rendering;
using Game.Simulation;
using Game.Tools;
using Game.Triggers;
using Game.UI;
using Game.UI.InGame;
using System;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using Time2Work.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using UnityEngine;
using UnityEngine.Scripting;
using static Unity.Entities.SystemBaseDelegates;

namespace Time2Work
{
    public partial class CitizenScheduleUISystem : UISystemBase
    {
        private const string kGroup = "citizenSchedule";
        private EntityQuery m_CitizenScheduleQuery;
        private RawValueBinding m_CitizenScheduleBinding;

        private static bool IsStudent(EntityManager entityManager, Entity citizen)
        {
            Student component;
            return entityManager.TryGetComponent<Student>(citizen, out component);
        }

        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();
            this.m_CitizenScheduleQuery = this.GetEntityQuery(ComponentType.ReadOnly<Citizen>(), ComponentType.ReadOnly<CitizenSchedule>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
            RequireForUpdate(m_CitizenScheduleQuery);

            this.AddBinding((IBinding)(this.m_CitizenScheduleBinding = new RawValueBinding("citizenSchedule", "schedule", (Action<IJsonWriter>)(binder =>
            {
                NativeArray<Entity> entityArray = this.m_CitizenScheduleQuery.ToEntityArray((AllocatorManager.AllocatorHandle)Allocator.Temp);
                binder.ArrayBegin(entityArray.Length);
                for (int index = 0; index < entityArray.Length; ++index)
                {
                    Entity entity = entityArray[index];
                    CitizenSchedule schedule;
                    if (EntityManager.TryGetComponent<CitizenSchedule>(entity, out schedule))
                    {
                        binder.TypeBegin("citizenSchedule.schedule");
                        binder.PropertyName("entity");
                        binder.Write(entity);
                        binder.PropertyName("student");
                        binder.Write(IsStudent(this.EntityManager, entity));
                        binder.PropertyName("work_start_hour");
                        binder.Write((int)(schedule.start_work * 24));
                        binder.PropertyName("work_end_hour");
                        binder.Write((int)(schedule.end_work * 24));
                        binder.PropertyName("work_start_minute");
                        binder.Write((int)((schedule.start_work * 24 - (int)(schedule.start_work * 24)) * 60));
                        binder.PropertyName("work_end_minute");
                        binder.Write((int)((schedule.end_work * 24 - (int)(schedule.end_work * 24)) * 60));
                        binder.PropertyName("lunch_start_hour");
                        binder.Write((int)(schedule.start_lunch * 24));
                        binder.PropertyName("lunch_end_hour");
                        binder.Write((int)(schedule.end_lunch * 24));
                        binder.PropertyName("lunch_start_minute");
                        binder.Write((int)((schedule.start_lunch * 24 - (int)(schedule.start_lunch * 24)) * 60));
                        binder.PropertyName("lunch_end_minute");
                        binder.Write((int)((schedule.end_lunch * 24 - (int)(schedule.end_lunch * 24)) * 60));
                        binder.PropertyName("dayOff");
                        binder.Write(schedule.dayoff);
                        binder.PropertyName("work_from_home");
                        binder.Write(schedule.work_from_home);
                        binder.TypeEnd();
                    }

                }
                binder.ArrayEnd();
                entityArray.Dispose();

            }))));

            Mod.log.Info("CitizenScheduleUISystem Created");
        }

        protected override void OnUpdate()
        {
            m_CitizenScheduleBinding.Update();
        }

        [Preserve]
        public CitizenScheduleUISystem()
        {
        }

    }
}
