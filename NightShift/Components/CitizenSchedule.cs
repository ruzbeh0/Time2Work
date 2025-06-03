using System;
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
        public int version = 2;
        public int day = -1000;
        public bool dayoff = default;
        public float go_to_work = default;
        public float start_work = default;
        public float end_work = default;
        public float start_lunch = default;
        public float end_lunch = default;
        public bool work_from_home = false;
        public int work_type = default;

        public CitizenSchedule()
        {
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
            reader.Read(out version);
            reader.Read(out day);
            reader.Read(out dayoff);
            reader.Read(out go_to_work);
            reader.Read(out start_work);
            reader.Read(out end_work);
            reader.Read(out start_lunch);
            reader.Read(out end_lunch);
            reader.Read(out work_from_home);
            if(version > 1)
            {
                reader.Read(out work_type);
            }
        }
    }
}
