﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Colossal.Serialization.Entities;
using Game.Agents;

namespace Time2Work.Components
{
    public struct CitizenSchedule : IComponentData, IQueryTypeParameter, ISerializable
    {
        public int version;
        public int day;
        public bool dayoff;
        public float go_to_work;
        public float start_work;
        public float end_work;
        public float start_lunch;
        public float end_lunch;
        public bool work_from_home;
        public int work_type;

        // Factory method to create a CitizenSchedule with default values
        public static CitizenSchedule CreateDefault()
        {
            return new CitizenSchedule
            {
                version = 2,
                day = -1000,
                dayoff = false,
                go_to_work = 0,
                start_work = 0,
                end_work = 0,
                start_lunch = 0,
                end_lunch = 0,
                work_from_home = false,
                work_type = 0
            };
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(version);
            writer.Write(day);
            writer.Write(dayoff);
            writer.Write(go_to_work);
            writer.Write(start_work);
            writer.Write(end_work);
            writer.Write(start_lunch);
            writer.Write(end_lunch);
            writer.Write(work_from_home);
            writer.Write(work_type);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            try
            {
                reader.Read(out version);
                reader.Read(out day);
                reader.Read(out dayoff);
                reader.Read(out go_to_work);
                reader.Read(out start_work);
                reader.Read(out end_work);
                reader.Read(out start_lunch);
                reader.Read(out end_lunch);
                reader.Read(out work_from_home);
                reader.Read(out work_type);
            }
            catch
            {
                // fallback to legacy-compatible deserialization
                version = 2;
                work_type = 0; // default fallback
            }
        }
    }
}
