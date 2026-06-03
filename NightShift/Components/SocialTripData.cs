using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Time2Work.Components
{
    public struct SocialTripData : IComponentData, IQueryTypeParameter, ISerializable
    {
        public const int ArrivedFlag = 1;
        public const int HostLockedFlag = 2;

        public int version;
        public Entity targetBuilding;
        public Entity hostCitizen;
        public int tripType;
        public float startTime;
        public float duration;
        public int flags;

        public bool HasArrived => (flags & ArrivedFlag) != 0;
        public bool IsHostLocked => (flags & HostLockedFlag) != 0;

        public void MarkArrived(float timeOfDay)
        {
            startTime = timeOfDay;
            flags |= ArrivedFlag;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(version);
            writer.Write(targetBuilding);
            writer.Write(hostCitizen);
            writer.Write(tripType);
            writer.Write(startTime);
            writer.Write(duration);
            writer.Write(flags);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out version);
            reader.Read(out targetBuilding);
            reader.Read(out hostCitizen);
            reader.Read(out tripType);
            reader.Read(out startTime);
            reader.Read(out duration);
            reader.Read(out flags);
        }
    }
}
