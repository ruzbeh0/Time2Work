using Colossal.Serialization.Entities;
using Game.Economy;
using Unity.Entities;

namespace Time2Work.Components
{
    public struct ShoppingPurchaseData : IComponentData, IQueryTypeParameter, ISerializable
    {
        public const int SourceUnknown = 0;
        public const int SourcePlanned = 1;
        public const int SourceActual = 2;
        public const int SourceFallback = 3;

        public int version;
        public Resource resource;
        public int amount;
        public int cost;
        public float distance;
        public uint frame;
        public int source;
        public bool estimatedCost;

        public ShoppingPurchaseData(Resource resource, int amount, int cost, float distance, uint frame, int source = SourceActual, bool estimatedCost = false)
        {
            this.version = 2;
            this.resource = resource;
            this.amount = amount;
            this.cost = cost;
            this.distance = distance;
            this.frame = frame;
            this.source = source;
            this.estimatedCost = estimatedCost;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(version);
            writer.Write((int)resource);
            writer.Write(amount);
            writer.Write(cost);
            writer.Write(distance);
            writer.Write(frame);
            writer.Write(source);
            writer.Write(estimatedCost);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out version);
            int resourceValue;
            reader.Read(out resourceValue);
            resource = (Resource)resourceValue;
            reader.Read(out amount);
            reader.Read(out cost);
            reader.Read(out distance);
            reader.Read(out frame);
            if (version >= 2)
            {
                reader.Read(out source);
                reader.Read(out estimatedCost);
            }
            else
            {
                source = SourceUnknown;
                estimatedCost = true;
            }
        }
    }
}
