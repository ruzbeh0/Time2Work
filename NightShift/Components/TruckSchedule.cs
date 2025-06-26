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
    public struct TruckSchedule : IComponentData, IQueryTypeParameter, ISerializable
    {
        public int version;
        public float startTime; // normalized 0.0 to 1.0
        public float endTime;   // normalized 0.0 to 1.0

        // Factory method to create a CitizenSchedule with default values
        public static TruckSchedule CreateDefault()
        {
            return new TruckSchedule
            {
                version = 1,
                startTime = 0f,
                endTime = 0f
            };
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(version);
            writer.Write(startTime);
            writer.Write(endTime);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out version);
            reader.Read(out startTime);
            reader.Read(out endTime);
        }
    }
}
