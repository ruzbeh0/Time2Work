using System;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Citizens;
using Game.Common;
using Game.Tools;
using Time2Work.Components;
using Time2Work.Extensions;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Time2Work.Systems
{
    public struct CitizenScheduleUI
    {
        public bool student;
        public int work_start_hour;
        public int work_end_hour;
        public int work_start_minute;
        public int work_end_minute;
        public int lunch_start_hour;
        public int lunch_end_hour;
        public int lunch_start_minute;
        public int lunch_end_minute;
        public bool dayOff;
        public bool work_from_home;
    }

    [Preserve]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class CitizenScheduleSection : ExtendedInfoSectionBase
    {
        protected override string group => "CitizenScheduleSection";
        private EntityQuery m_CitizenScheduleQuery;
        private ValueBindingHelper<CitizenScheduleUI> m_ScheduleBinding;
        protected override void OnCreate()
        {
            base.OnCreate();
            m_InfoUISystem.AddMiddleSection(this);
            m_CitizenScheduleQuery = GetEntityQuery(
                ComponentType.ReadOnly<Citizen>(),
                ComponentType.ReadOnly<CitizenSchedule>(),
                ComponentType.Exclude<Temp>(),
                ComponentType.Exclude<Deleted>()
            );
            m_ScheduleBinding = CreateBinding<CitizenScheduleUI>("schedule", new CitizenScheduleUI());
            Mod.log.Info("CitizenScheduleSection Created");
        }
        private bool Visible()
        {
            var citizenScheduleArray = m_CitizenScheduleQuery.ToEntityArray(Allocator.Temp);
            try
            {
                if (EntityManager.HasComponent<Game.Citizens.Citizen>(selectedEntity))
                {
                    // Check if entity is a worker or student
                    bool isStudent = IsStudent(EntityManager, selectedEntity);
                    bool isWorker = IsWorker(EntityManager, selectedEntity);
                    if (!isStudent && !isWorker)
                        return false;
                        
                    for (int i = 0; i < citizenScheduleArray.Length; i++)
                    {
                        if (citizenScheduleArray[i] == selectedEntity &&
                            EntityManager.HasComponent<CitizenSchedule>(selectedEntity))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
            finally
            {
                citizenScheduleArray.Dispose();
            }
        }
        protected override void OnUpdate()
        {
            base.OnUpdate();
           base.visible = Visible();
        }
        protected override void Reset()
        {
            CitizenScheduleUI scheduleUI = new CitizenScheduleUI
            {
                student = false,
                work_start_hour = 0,
                work_end_hour = 0,
                work_start_minute = 0,
                work_end_minute = 0,
                lunch_start_hour = 0,
                lunch_end_hour = 0,
                lunch_start_minute = 0,
                lunch_end_minute = 0,
                dayOff = false,
                work_from_home = false
            };
        }
        protected override void OnProcess()
        {
            if (!EntityManager.TryGetComponent<CitizenSchedule>(selectedEntity, out var schedule))
                return;
            bool isStudent = IsStudent(EntityManager, selectedEntity);
            var scheduleUI = new CitizenScheduleUI
            {
                student = isStudent,
                work_start_hour = (int)(schedule.start_work * 24),
                work_end_hour = (int)(schedule.end_work * 24),
                work_start_minute = (int)((schedule.start_work * 24 - (int)(schedule.start_work * 24)) * 60),
                work_end_minute = (int)((schedule.end_work * 24 - (int)(schedule.end_work * 24)) * 60),
                lunch_start_hour = (int)(schedule.start_lunch * 24),
                lunch_end_hour = (int)(schedule.end_lunch * 24),
                lunch_start_minute = (int)((schedule.start_lunch * 24 - (int)(schedule.start_lunch * 24)) * 60),
                lunch_end_minute = (int)((schedule.end_lunch * 24 - (int)(schedule.end_lunch * 24)) * 60),
                dayOff = schedule.dayoff,
                work_from_home = schedule.work_from_home
            };
            m_ScheduleBinding.Value = scheduleUI;
            m_ScheduleBinding.Binding.TriggerUpdate();
            RequestUpdate();
        }
        public override void OnWriteProperties(IJsonWriter writer) { }
        private static bool IsStudent(EntityManager entityManager, Entity citizen)
        {
            Student component;
            return entityManager.TryGetComponent<Student>(citizen, out component);
        }
        private static bool IsWorker(EntityManager entityManager, Entity citizen)
        {
            Worker component;
            return entityManager.TryGetComponent<Worker>(citizen, out component);
        }    
    }
}