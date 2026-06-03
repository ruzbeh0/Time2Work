using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Time2Work.Components
{
    public struct SocialLeisureOpportunity : IComponentData, IQueryTypeParameter, ISerializable
    {
        public int version;
        public Entity originalTarget;
        public int originalLeisureType;
        public uint requestedFrame;

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(version);
            writer.Write(originalTarget);
            writer.Write(originalLeisureType);
            writer.Write(requestedFrame);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out version);
            reader.Read(out originalTarget);
            reader.Read(out originalLeisureType);
            reader.Read(out requestedFrame);
        }
    }
}
