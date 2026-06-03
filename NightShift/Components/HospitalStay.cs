using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Time2Work.Components
{
    public struct HospitalStay : IComponentData, IQueryTypeParameter, ISerializable
    {
        public int version;
        public uint startFrame;
        public uint endFrame;
        public float durationHours;

        public HospitalStay(uint startFrame, uint endFrame, float durationHours)
        {
            version = 1;
            this.startFrame = startFrame;
            this.endFrame = endFrame;
            this.durationHours = durationHours;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(version);
            writer.Write(startFrame);
            writer.Write(endFrame);
            writer.Write(durationHours);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out version);
            reader.Read(out startFrame);
            reader.Read(out endFrame);
            reader.Read(out durationHours);
        }
    }
}
