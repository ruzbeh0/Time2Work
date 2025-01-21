using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Time2Work.Extensions
{
    using Colossal.UI.Binding;

    public class EnumReader<T> : IReader<T>
    {
        public void Read(IJsonReader reader, out T value)
        {
            reader.Read(out int value2);
            value = (T)(object)value2;
        }
    }
}
