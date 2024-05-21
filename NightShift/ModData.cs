using Colossal.IO.AssetDatabase;
using Game.Modding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Time2Work
{
    [FileLocation($"ModsData\\{nameof(Time2Work)}\\data")]
    public class ModData
    {
        public float average_commute { get; set; } = 0f;
        public float commute_top10per { get; set; } = 0f;
    }
}
