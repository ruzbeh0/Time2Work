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
    public struct SpecialEventData : IComponentData, IQueryTypeParameter, ISerializable
    {
        public int version = 1;
        public SpecialEventData()
        {

        }

        public float start_time = default;
        public float duration = default;
        public float early_start_offset = default;
        public LeisureType leisureType = default;
        public int new_attraction = default;
        public int day = default;

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(version);
            writer.Write(start_time);
            writer.Write(duration);
            writer.Write(early_start_offset);
            writer.Write((int)leisureType);
            writer.Write(new_attraction);
            writer.Write(day);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out version);
            reader.Read(out start_time);
            reader.Read(out duration);
            reader.Read(out early_start_offset);
            int leisureTypeInt;
            reader.Read(out leisureTypeInt);
            leisureType = (LeisureType) leisureTypeInt;
            reader.Read(out new_attraction);
            reader.Read(out day); 
        }
    }
}
