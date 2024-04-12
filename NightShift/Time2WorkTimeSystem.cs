using Game.Prefabs;
using Game.Simulation;
using Game.Common;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using UnityEngine;

namespace Time2Work
{
    internal partial class Time2WorkTimeSystem : TimeSystem
    {

        public new const int kTicksPerDay = 262144;

    }
}
