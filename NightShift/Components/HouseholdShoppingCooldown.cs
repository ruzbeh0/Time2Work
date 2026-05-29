using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Time2Work.Components
{
    public struct HouseholdShoppingCooldown : IComponentData, IQueryTypeParameter, ISerializable
    {
        public int version;
        public uint shoppingAllowedFrame;

        public HouseholdShoppingCooldown(uint shoppingAllowedFrame)
        {
            this.version = 1;
            this.shoppingAllowedFrame = shoppingAllowedFrame;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(version);
            writer.Write(shoppingAllowedFrame);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out version);
            reader.Read(out shoppingAllowedFrame);
        }
    }
}
