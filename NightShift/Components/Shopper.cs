using System;
using Unity.Entities;
using Colossal.Serialization.Entities;
using Game.Agents;

namespace Time2Work.Components
{
    public struct Shopper : IComponentData, IQueryTypeParameter, ISerializable
    {
        public int version;
        public float duration;
        public float start_time;

        // Constructor that sets version manually (since no field initializers allowed)
        public Shopper(float duration)
        {
            this.version = 2;
            this.duration = duration;
            this.start_time = 0f;
        }

        public Shopper(float duration, float start_time)
        {
            this.version = 2;
            this.duration = duration;
            this.start_time = start_time;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(version);
            writer.Write(duration);
            if (version >= 2)
            {
                writer.Write(start_time);
            }
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out version);
            reader.Read(out duration);
            if (version >= 2)
            {
                reader.Read(out start_time);
            }
        }
    }
}
