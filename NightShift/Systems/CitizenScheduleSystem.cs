using Colossal.Entities;
using Colossal.IO.AssetDatabase.Internal;
using Game;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text;
using System.Threading.Tasks;
using Time2Work.Components;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using static Time2Work.Systems.CitizenScheduleSystem;

namespace Time2Work.Systems
{
    public partial class CitizenScheduleSystem : GameSystemBase
    {
        public static Dictionary<(WorkType, Workshift, Level), NativeArray<CitizenSchedule>> workerSchedulePool = new Dictionary<(WorkType, Workshift, Level), NativeArray<CitizenSchedule>>();
        Dictionary<(WorkType, Workshift, Level), int> scheduleWriteIndices = new Dictionary<(WorkType, Workshift, Level), int>();
        public static Dictionary<Level, NativeArray<CitizenSchedule>> studentSchedulePool = new Dictionary<Level, NativeArray<CitizenSchedule>>();
        Dictionary<Level, int> studentScheduleWriteIndices = new Dictionary<Level, int>();
        public enum WorkType { Commercial, Office, Industrial, CityService }
        public enum Level { Level1 = 0, Level2 = 1, Level3 = 2 }
        public static int bin_size = 15;

        private EntityQuery _worker_query;
        private EntityQuery _student_query;
        private EntityQuery m_EconomyParameterQuery;
        private EconomyParameterData m_EconomyParameters;
        private EntityQuery m_PopulationQuery;
        private EntityQuery m_TimeDataQuery;
        private SimulationSystem m_SimulationSystem;
        private Time2WorkTimeSystem m_TimeSystem;
        private uint m_Frame;
        private Game.Common.TimeData m_TimeData;
        private int ticksPerDay;
        private Setting.DTSimulationEnum dow;
        private Entity m_PopulationEntity;
        private float m_TimeOfDay;
        private int lunch_break_pct;
        private float work_start_time;
        private float work_end_time;
        private float delayFactor;
        private int parttime_prob;
        private float commute_top10;
        private float overtime;
        private float part_time_reduction;
        private int remote_work_probability;
        private bool scheduleGenerated = false;
        private int day;
        private int population;
        private float3 school_offdayprob;
        private int3 school_start_time;
        private int3 school_end_time;

        protected override void OnCreate()
        {
            base.OnCreate();

            this.m_EconomyParameterQuery = this.GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
            this.m_EconomyParameters = this.m_EconomyParameterQuery.GetSingleton<EconomyParameterData>();
            this.m_SimulationSystem = this.World.GetOrCreateSystemManaged<SimulationSystem>();
            this.m_TimeSystem = this.World.GetOrCreateSystemManaged<Time2WorkTimeSystem>();
            this.m_TimeDataQuery = this.GetEntityQuery(ComponentType.ReadOnly<Game.Common.TimeData>());
            this.m_PopulationQuery = this.GetEntityQuery(ComponentType.ReadOnly<Population>());
            this.RequireForUpdate(this.m_EconomyParameterQuery);
            this.lunch_break_pct = Mod.m_Setting.lunch_break_percentage;
            this.work_start_time = (float)Mod.m_Setting.work_start_time;
            this.work_end_time = (float)Mod.m_Setting.work_end_time;
            this.delayFactor = Mod.m_Setting.delay_factor / 100;
            this.parttime_prob = Mod.m_Setting.part_time_percentage;
            this.commute_top10 = Mod.m_Setting.commute_top10per;
            this.overtime = ((Mod.m_Setting.avg_work_hours_ft_wd - (Mod.m_Setting.work_end_time - Mod.m_Setting.work_start_time) / 2) / 24);
            this.part_time_reduction = Mod.m_Setting.avg_work_hours_pt_wd / Mod.m_Setting.avg_work_hours_ft_wd;
            this.remote_work_probability = Mod.m_Setting.remote_percentage;
            school_start_time = new int3((int)Mod.m_Setting.school_start_time, (int)Mod.m_Setting.high_school_start_time, (int)Mod.m_Setting.univ_start_time);
            school_end_time = new int3((int)Mod.m_Setting.school_end_time, (int)Mod.m_Setting.high_school_end_time, (int)Mod.m_Setting.univ_end_time);

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
                    ComponentType.ReadOnly<Game.Citizens.Student>()
                },
                None =
                    [
                        ComponentType.Exclude<Deleted>(),
                        ComponentType.Exclude<Temp>()
                    ],
            });

            RequireForUpdate(_worker_query);
            RequireForUpdate(_student_query);

            ticksPerDay = Time2WorkTimeSystem.kTicksPerDay;
        }


        private WorkType GetWorkerOffDayAndPartTimeProb(Worker worker, out float offdayprob, out float parttime_prob)
        {
            offdayprob = 0;
            parttime_prob = 0;
            float4 office_offdayprob = WeekSystem.getOfficeOffDayProb();
            float4 commercial_offdayprob = WeekSystem.getCommercialOffDayProb();
            float4 industry_offdayprob = WeekSystem.getIndustryOffDayProb();
            float4 cityservices_offdayprob = WeekSystem.getCityServicesOffDayProb();
            WorkType workType = WorkType.CityService;

            if (EntityManager.TryGetComponent<PrefabRef>(worker.m_Workplace, out var prefab1))
            {
                if (EntityManager.TryGetComponent<PropertyRenter>(worker.m_Workplace, out var propertyRenter))
                {
                    //x = weekday, y = friday, z = saturday, w = sunday
                    if (EntityManager.HasComponent<CommercialProperty>(propertyRenter.m_Property))
                    {
                        workType = WorkType.Commercial;
                        if ((int)dow == (int)Setting.DTSimulationEnum.Weekday)
                        {
                            offdayprob = commercial_offdayprob.x;
                        }
                        else if ((int)dow == (int)Setting.DTSimulationEnum.AverageDay)
                        {
                            offdayprob = commercial_offdayprob.y;
                        }
                        else if ((int)dow == (int)Setting.DTSimulationEnum.Saturday)
                        {
                            offdayprob = commercial_offdayprob.z;
                            parttime_prob = 100;
                        }
                        else
                        {
                            offdayprob = commercial_offdayprob.w;
                            parttime_prob = 100;
                        }
                    }
                    if (EntityManager.HasComponent<IndustrialProperty>(propertyRenter.m_Property))
                    {
                        workType = WorkType.Industrial;
                        if ((int)dow == (int)Setting.DTSimulationEnum.Weekday)
                        {
                            offdayprob = industry_offdayprob.x;
                        }
                        else if ((int)dow == (int)Setting.DTSimulationEnum.AverageDay)
                        {
                            offdayprob = industry_offdayprob.y;
                        }
                        else if ((int)dow == (int)Setting.DTSimulationEnum.Saturday)
                        {
                            offdayprob = industry_offdayprob.z;
                            parttime_prob = 100;
                        }
                        else
                        {
                            offdayprob = industry_offdayprob.w;
                            parttime_prob = 100;
                        }
                    }
                    if (EntityManager.HasComponent<OfficeProperty>(propertyRenter.m_Property))
                    {
                        workType = WorkType.Office;
                        if ((int)dow == (int)Setting.DTSimulationEnum.Weekday)
                        {
                            offdayprob = office_offdayprob.x;
                        }
                        else if ((int)dow == (int)Setting.DTSimulationEnum.AverageDay)
                        {
                            offdayprob = office_offdayprob.y;
                        }
                        else if ((int)dow == (int)Setting.DTSimulationEnum.Saturday)
                        {
                            offdayprob = office_offdayprob.z;
                            parttime_prob = 100;
                        }
                        else
                        {
                            offdayprob = office_offdayprob.w;
                            parttime_prob = 100;
                        }
                    }
                    else
                    {
                        if ((int)dow == (int)Setting.DTSimulationEnum.Weekday)
                        {
                            offdayprob = cityservices_offdayprob.x;
                        }
                        else if ((int)dow == (int)Setting.DTSimulationEnum.AverageDay)
                        {
                            offdayprob = cityservices_offdayprob.y;
                        }
                        else if ((int)dow == (int)Setting.DTSimulationEnum.Saturday)
                        {
                            offdayprob = cityservices_offdayprob.z;
                            parttime_prob = 100;
                        }
                        else
                        {
                            offdayprob = cityservices_offdayprob.w;
                            parttime_prob = 100;
                        }
                    }
                }
            }
            return workType;
        }

        private void GenerateStudentSchedule()
        {
            var students = _student_query.ToEntityArray(Allocator.Temp);

            foreach (var entity in students)
            {
                Citizen citizen = EntityManager.GetComponentData<Citizen>(entity);
                Game.Citizens.Student student = EntityManager.GetComponentData<Game.Citizens.Student>(entity);

                float2 time2Lunch = new float2(-1, -1);
                float2 time2Study = new float2(-1, -1);
                float startStudy = 0f;
                CitizenSchedule citizenSchedule;

                bool lunchTime = false;
                bool studyTime = Time2WorkStudentSystem.IsTimeToStudy(citizen, student, ref this.m_EconomyParameters, this.m_TimeOfDay, this.m_Frame, this.m_TimeData, population, school_offdayprob, school_start_time, school_end_time, ticksPerDay, out time2Study, out startStudy);

                citizenSchedule = new CitizenSchedule();
                citizenSchedule.dayoff = false;
                citizenSchedule.start_work = time2Study.x;
                citizenSchedule.go_to_work = startStudy;
                citizenSchedule.end_work = time2Study.y;
                citizenSchedule.start_lunch = time2Lunch.x;
                citizenSchedule.end_lunch = time2Lunch.y;
                citizenSchedule.work_from_home = false;
                citizenSchedule.day = day;

                int level = (int)student.m_Level;
                level = level - 1;
                if (level > 2)
                    level = 2;
                var key = ((Level)level);
                // Get next index to write for this key
                if (!studentScheduleWriteIndices.TryGetValue(key, out int index))
                {
                    index = 0;
                }

                if (index >= bin_size * 3)
                {
                    continue;
                }

                NativeArray<CitizenSchedule> schedules;
                if (!studentSchedulePool.ContainsKey(key))
                {
                    schedules = new NativeArray<CitizenSchedule>(bin_size * 3, Allocator.Persistent);
                    studentSchedulePool[key] = schedules;
                }
                else
                {
                    schedules = studentSchedulePool[key];
                }

                schedules[index] = citizenSchedule;
                // Update index
                studentScheduleWriteIndices[key] = index + 1;


            }
        }
        private void GenerateWorkerSchedule()
        {
            
            var workers = _worker_query.ToEntityArray(Allocator.Temp);

            foreach (var entity in workers)
            {
                Citizen citizen = EntityManager.GetComponentData<Citizen>(entity);
                Worker worker = EntityManager.GetComponentData<Worker>(entity);
                float offdayprob;
                float partime_prob;

                WorkType workType = GetWorkerOffDayAndPartTimeProb(worker, out offdayprob, out partime_prob);
                
                float2 time2Lunch = new float2(-1, -1);
                float2 time2Work = new float2(-1, -1);
                bool dayOff = Time2WorkWorkerSystem.IsTodayOffDay(citizen, ref this.m_EconomyParameters, this.m_Frame, this.m_TimeData, population, this.m_TimeOfDay, offdayprob, ticksPerDay, day);
                float start_work = 0f;
                CitizenSchedule citizenSchedule;

                bool lunchTime = Time2WorkWorkerSystem.IsLunchTime(citizen, worker, ref this.m_EconomyParameters, this.m_TimeOfDay, lunch_break_pct, this.m_Frame, this.m_TimeData, ticksPerDay, out time2Lunch);
                bool workTime = Time2WorkWorkerSystem.IsTimeToWork(citizen, worker, ref this.m_EconomyParameters, this.m_TimeOfDay, lunch_break_pct, work_start_time, work_end_time, delayFactor, ticksPerDay, parttime_prob, commute_top10, overtime, part_time_reduction, out time2Work, out start_work);
                bool workFromHome = Time2WorkWorkerSystem.IsTodayWorkFromHome(citizen, this.m_Frame, this.m_TimeData, ticksPerDay, remote_work_probability);

                citizenSchedule = new CitizenSchedule();
                citizenSchedule.dayoff = dayOff;
                citizenSchedule.start_work = time2Work.x;
                citizenSchedule.go_to_work = start_work;
                citizenSchedule.end_work = time2Work.y;
                citizenSchedule.start_lunch = time2Lunch.x;
                citizenSchedule.end_lunch = time2Lunch.y;
                citizenSchedule.work_from_home = workFromHome;
                citizenSchedule.day = day;

                int level = (int) worker.m_Level;
                if (level > 2)
                    level = 2;
                else if(level <= 1) 
                    level = 0;
                else if (level == 2)
                    level = 1;
                var key = (workType, worker.m_Shift, (Level)level);
                // Get next index to write for this key
                if (!scheduleWriteIndices.TryGetValue(key, out int index))
                {
                    index = 0;
                }

                if(index >= bin_size * (3 - (int)worker.m_Shift))
                {
                    continue;
                }

                NativeArray<CitizenSchedule> schedules;
                if (!workerSchedulePool.ContainsKey(key))
                {
                    schedules = new NativeArray<CitizenSchedule>(bin_size*(3 - (int)worker.m_Shift), Allocator.Persistent);
                    workerSchedulePool[key] = schedules;
                } else
                {
                    schedules = workerSchedulePool[key];
                }

                schedules[index] = citizenSchedule;
                // Update index
                scheduleWriteIndices[key] = index + 1;
            }
        }

        private int selectBinSize(int population)
        {
            int bins = 5;

            if(population > 3000)
            {
                bins = 10;
            }
            else if(population > 10000)
            {
                bins = 15;
            }

            return bins;
        }
        protected override void OnUpdate()
        {
            if (!m_TimeDataQuery.TryGetSingleton(out m_TimeData) || m_SimulationSystem == null)
                return; // System not ready yet

            if (m_SimulationSystem.frameIndex == 0)
                return; // Still warming up

            if (!WeekSystem.initialized)
                return;

            DateTime currentDateTime = this.World.GetExistingSystemManaged<TimeSystem>().GetCurrentDateTime();
            dow = WeekSystem.currentDayOfTheWeek;
            m_TimeOfDay = this.m_TimeSystem.normalizedTime;
            day = Time2WorkTimeSystem.GetDay(m_Frame, m_TimeData, ticksPerDay);
            m_Frame = this.m_SimulationSystem.frameIndex;
            m_TimeData = this.m_TimeDataQuery.GetSingleton<Game.Common.TimeData>();
            m_PopulationEntity = this.m_PopulationQuery.GetSingletonEntity();
            population = EntityManager.GetComponentData<Population>(this.m_PopulationEntity).m_Population;
            school_offdayprob = WeekSystem.getSchoolOffDayProb();

            bin_size = selectBinSize(population);
                            
            if (!scheduleGenerated)
            {
                Mod.log.Info($"Generating Schedules for day: {day}, day of the week: {dow}");
                GenerateWorkerSchedule();
                GenerateStudentSchedule();  
            
                scheduleGenerated = true;

                string path = Path.Combine(Mod.DataFolder, "GeneratedSchedules");
                string fileName = path +
                    "_workers_" + day + ".csv";
                WriteWorkerSchedulesToCsv(fileName, workerSchedulePool);

                fileName = path +
                    "_students_" + day + ".csv";
                WriteStudentSchedulesToCsv(fileName, studentSchedulePool);
            } 
        }

        protected override void OnDestroy()
        {
            foreach (var entry in workerSchedulePool)
            {
                if (entry.Value.IsCreated)
                    entry.Value.Dispose();
            }
        }

        public void WriteStudentSchedulesToCsv(string filePath, Dictionary<Level, NativeArray<CitizenSchedule>> schedulePool)
        {
            var sb = new StringBuilder();

            // Write CSV header
            sb.AppendLine("Level,DayOff,StartStudy,GoToStudy,EndStudy,StartLunch,EndLunch,Remote,Day");

            foreach (var entry in schedulePool)
            {
                var level = entry.Key;
                var array = entry.Value;

                for (int i = 0; i < array.Length; i++)
                {
                    var schedule = array[i];
                    sb.AppendLine($"{level}," +
                                  $"{schedule.dayoff}," +
                                  $"{schedule.start_work}," +
                                  $"{schedule.go_to_work}," +
                                  $"{schedule.end_work}," +
                                  $"{schedule.start_lunch}," +
                                  $"{schedule.end_lunch}," +
                                  $"{schedule.work_from_home}," +
                                  $"{schedule.day}");
                }
            }

            // Write to file
            File.WriteAllText(filePath, sb.ToString());
        }


        public void WriteWorkerSchedulesToCsv(string filePath, Dictionary<(WorkType, Workshift, Level), NativeArray<CitizenSchedule>> schedulePool)
        {
            var sb = new StringBuilder();

            // Write CSV header
            sb.AppendLine("WorkType,Workshift,Level,DayOff,StartWork,GoToWork,EndWork,StartLunch,EndLunch,WorkFromHome,Day");

            foreach (var entry in schedulePool)
            {
                var key = entry.Key;
                var array = entry.Value;

                for (int i = 0; i < array.Length; i++)
                {
                    var schedule = array[i];
                    sb.AppendLine($"{key.Item1},{key.Item2},{key.Item3}," +
                                  $"{schedule.dayoff}," +
                                  $"{schedule.start_work}," +
                                  $"{schedule.go_to_work}," +
                                  $"{schedule.end_work}," +
                                  $"{schedule.start_lunch}," +
                                  $"{schedule.end_lunch}," +
                                  $"{schedule.work_from_home}," +
                                  $"{schedule.day}");
                }
            }

            // Write to file
            File.WriteAllText(filePath, sb.ToString());
        }
    }
}
