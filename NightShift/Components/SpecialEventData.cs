using System;
using Unity.Entities;
using Colossal.Serialization.Entities;
using Game.Agents;

namespace Time2Work.Components
{
    public struct SpecialEventData : IComponentData, IQueryTypeParameter, ISerializable
    {
        public int version;
        public float start_time;
        public float duration;
        public float early_start_offset;
        public LeisureType leisureType;
        public int new_attraction;
        public int day;
        public int entity_index; // entity index used to deduplicate when there multiples instances of the same location

        // Static factory method for initializing with default values
        public static SpecialEventData CreateDefault()
        {
            return new SpecialEventData
            {
                version = 2,
                start_time = 0f,
                duration = 0f,
                early_start_offset = 0f,
                leisureType = default,
                new_attraction = 0,
                day = 0,
                entity_index = 0
            };
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(version);
            writer.Write(start_time);
            writer.Write(duration);
            writer.Write(early_start_offset);
            writer.Write((int)leisureType);
            writer.Write(new_attraction);
            writer.Write(day);
            if (version >= 2)
            {
                writer.Write(entity_index);
            }
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out version);
            reader.Read(out start_time);
            reader.Read(out duration);
            reader.Read(out early_start_offset);
            int leisureTypeInt;
            reader.Read(out leisureTypeInt);
            leisureType = (LeisureType)leisureTypeInt;
            reader.Read(out new_attraction);
            reader.Read(out day);
            if (version >= 2)
            {
                reader.Read(out entity_index);
            }
            else
            {
                entity_index = 0; // default value for older versions
            }
        }
    }
}
