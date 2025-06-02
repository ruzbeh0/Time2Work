using Game;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Time2Work.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Time2Work.Systems
{
    public partial class CitizenScheduleSystem : GameSystemBase
    {
        public NativeArray<CitizenSchedule> sharedSchedules;
        private EntityQuery _worker_query;
        private EntityQuery _student_query;
        private EntityQuery _other_citizen_query;

        protected override void OnCreate()
        {
            base.OnCreate();

            _worker_query = GetEntityQuery(new EntityQueryDesc()
            {
                All = new[] {
                    ComponentType.ReadOnly<Citizen>(),
                    ComponentType.ReadOnly<Worker>()
                },
                None =
                    [
                        ComponentType.Exclude<Deleted>(),
                        ComponentType.Exclude<Temp>()
                    ],
            });

            _student_query = GetEntityQuery(new EntityQueryDesc()
            {
                All = new[] {
                    ComponentType.ReadOnly<Citizen>(),
                    ComponentType.ReadOnly<Student>()
                },
                None =
                    [
                        ComponentType.Exclude<Deleted>(),
                        ComponentType.Exclude<Temp>()
                    ],
            });

            _other_citizen_query = GetEntityQuery(new EntityQueryDesc()
            {
                All = new[] {
                    ComponentType.ReadOnly<Citizen>()
                },
                None =
                    [
                        ComponentType.Exclude<Deleted>(),
                        ComponentType.Exclude<Student>(),
                        ComponentType.Exclude<Deleted>(),
                        ComponentType.Exclude<Temp>()
                    ],
            });

            sharedSchedules = new NativeArray<CitizenSchedule>(1000, Allocator.Persistent);

            var workers = _worker_query.ToEntityArray(Allocator.Temp);

            for (int i = 0; i < sharedSchedules.Length; i++)
            {
                sharedSchedules[i] = GenerateRandomSchedule(i);
            }
        }

        private CitizenSchedule GenerateRandomSchedule(int i)
        {
            CitizenSchedule citizenSchedule = new CitizenSchedule();

            //float2 time2Lunch = Time2WorkWorkerSystem.GetLunchTime(citizen, worker, ref economyParameters);
            //float2 time2Work = Time2WorkWorkerSystem.GetTimeToWork(citizen, worker, ref economyParameters, true, lunch_break_pct, work_start_time, work_end_time, delayFactor, ticksPerDay, part_time_prob, commute_top10, overtime, part_time_reduction);

            
            //citizenSchedule.dayoff = dayOff;
            //citizenSchedule.start_work = time2Work.x;
            //citizenSchedule.go_to_work = start_work;
            //citizenSchedule.end_work = time2Work.y;
            //citizenSchedule.start_lunch = time2Lunch.x;
            //citizenSchedule.end_lunch = time2Lunch.y;
            //citizenSchedule.work_from_home = workFromHome;
            //citizenSchedule.day = day;

            return citizenSchedule;
        }

        protected override void OnUpdate()
        {
           
        }
    }
}
