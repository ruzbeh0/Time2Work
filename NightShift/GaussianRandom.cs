using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Time2Work
{
    internal static class GaussianRandom
    {
        public static double NextGaussianDouble(Unity.Mathematics.Random r)
        {
            double u, v, S;

            do
            {
                u = 2.0 * r.NextDouble() - 1.0;
                v = 2.0 * r.NextDouble() - 1.0;
                S = u * u + v * v;
            }
            while (S >= 1.0);

            double fac = Math.Sqrt(-2.0 * Math.Log(S) / S);
            return u * fac;
        }
    }
}
