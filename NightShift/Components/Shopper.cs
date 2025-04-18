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
    public struct Shopper : IComponentData, IQueryTypeParameter, ISerializable
    {
        public int version = 2;
        public Shopper(float duration)
        {
            this.duration = duration;
        }

        public Shopper(float duration, float start_time)
        {
            this.duration = duration;
            this.start_time = start_time;
        }

        public float duration = default;
        public float start_time = default;

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(version);
            writer.Write(duration);
            if(version >= 2)
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
