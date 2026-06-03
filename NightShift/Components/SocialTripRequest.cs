using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Time2Work.Components
{
    public struct SocialTripRequest : IComponentData, IQueryTypeParameter, ISerializable
    {
        public int version;
        public Entity targetBuilding;
        public Entity hostCitizen;
        public int tripType;
        public float duration;
        public int priority;
        public uint requestedFrame;

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(version);
            writer.Write(targetBuilding);
            writer.Write(hostCitizen);
            writer.Write(tripType);
            writer.Write(duration);
            writer.Write(priority);
            writer.Write(requestedFrame);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out version);
            reader.Read(out targetBuilding);
            reader.Read(out hostCitizen);
            reader.Read(out tripType);
            reader.Read(out duration);
            reader.Read(out priority);
            reader.Read(out requestedFrame);
        }
    }
}
