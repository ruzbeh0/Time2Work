using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Burst;

namespace Time2Work.Utils
{

    //[BurstCompile]
    public class LeisureProbabilityCalculator
    {
        // Base hourly profiles for a Weekday (hours 0-23) for each leisure type.
        // These values roughly sum to 100 when taken as percentages.
        private static readonly double[] MealsBase = {
    1.11, 1.11, 1.11, 1.11, 1.11, 2.22, 4.44, 8.88, 6.66, 5.55, 4.44, 3.33,
    16.65, 8.88, 4.44, 3.33, 4.44, 7.77, 22.20, 11.10, 6.66, 4.44, 2.22, 1.11
    };

        private static readonly double[] EntertainmentBase = {
    1.06, 1.06, 1.06, 1.06, 1.06, 1.06, 2.13, 2.13, 2.13, 3.19, 3.19, 4.26,
    5.32, 6.39, 6.39, 7.45, 7.45, 8.52, 13.83, 17.02, 17.02, 10.64, 6.39, 1.06
    };

        private static readonly double[] ShoppingBase = {
    1.04, 1.04, 1.04, 1.04, 1.04, 1.04, 2.08, 3.13, 5.21, 9.38, 11.46, 11.46,
    11.46, 9.38, 8.33, 9.38, 9.38, 9.38, 7.29, 5.21, 3.13, 3.13, 3.13, 1.04
    };

        private static readonly double[] ParkBase = {
    1.09, 1.09, 1.09, 1.09, 1.09, 1.09, 2.17, 3.26, 4.35, 7.61, 11.96, 14.13,
    11.96, 11.96, 11.96, 11.96, 10.87, 7.61, 4.35, 3.26, 1.09, 1.09, 1.09, 1.09
    };

        private static readonly double[] TravelBase = {
    3.23, 2.15, 2.15, 2.15, 3.23, 4.31, 6.46, 9.69, 8.61, 7.54, 6.46, 6.46,
    7.54, 7.54, 7.54, 7.54, 7.54, 7.54, 5.38, 4.31, 3.23, 2.15, 2.15, 2.15
    };


        // Day type IDs: Weekday = 1, Friday = 0, Saturday = 2, Sunday = 3
        // Country IDs (example): Brazil=25, Canada=34, France=62, Germany=66, Netherlands=124,
        //                        Philippines=140, Poland=141, UK=187, USA=188

        /// <summary>
        /// Returns the probability (%) for Meals at the given hour.
        /// </summary>
        public static float GetMealsProbability(int countryId, int dayType, int hour)
        {
            double[] profile = (double[])MealsBase.Clone();

            // Apply day type modifications for Meals:
            if (dayType == 0) // Friday
            {
                // Boost evening hours (hour 17 and later)
                for (int h = 17; h < 24; h++)
                    profile[h] *= 1.2;
            }
            else if (dayType == 2) // Saturday
            {
                // Increase overall activity, but reduce early hours
                MultiplyRange(profile, 0, 8, 0.8);
                MultiplyAll(profile, 1.2);
            }
            else if (dayType == 3) // Sunday
            {
                MultiplyAll(profile, 1.1);
                MultiplyRange(profile, 0, 6, 0.7);
                MultiplyRange(profile, 12, 18, 1.2);
            }
            // (For Meals, no special country adjustment in our model.)

            Normalize(profile);
            return (float)profile[hour] / 100f;
        }

        /// <summary>
        /// Returns the probability (%) for Entertainment at the given hour.
        /// </summary>
        public static float GetEntertainmentProbability(int countryId, int dayType, int hour)
        {
            double[] profile = (double[])EntertainmentBase.Clone();

            // Apply day type modifications for Entertainment:
            if (dayType == 0) // Friday
            {
                for (int h = 17; h < 24; h++)
                    profile[h] *= 1.2;
            }
            else if (dayType == 2) // Saturday
            {
                MultiplyRange(profile, 0, 8, 0.8);
                MultiplyAll(profile, 1.2);
            }
            else if (dayType == 3) // Sunday
            {
                MultiplyAll(profile, 1.1);
                MultiplyRange(profile, 0, 6, 0.7);
                MultiplyRange(profile, 12, 18, 1.2);
            }

            // Country adjustments for Entertainment:
            if (countryId == 25 || countryId == 140) // Brazil or Philippines
            {
                // Boost later hours (20-22)
                MultiplyRange(profile, 20, 23, 1.3);
            }
            else if (countryId == 187) // UK
            {
                // Boost early evening (17-20)
                MultiplyRange(profile, 17, 21, 1.2);
            }

            Normalize(profile);
            return (float)profile[hour] / 100f;
        }

        /// <summary>
        /// Returns the probability (%) for Shopping at the given hour.
        /// </summary>
        public static float GetShoppingProbability(int countryId, int dayType, int hour)
        {
            double[] profile = (double[])ShoppingBase.Clone();

            // Apply day type modifications for Shopping:
            if (dayType == 0) // Friday
            {
                // (No extra adjustment for Friday for shopping in this model)
            }
            else if (dayType == 2) // Saturday
            {
                MultiplyRange(profile, 0, 8, 0.8);
                MultiplyAll(profile, 1.2);
            }
            else if (dayType == 3) // Sunday
            {
                MultiplyAll(profile, 1.1);
                MultiplyRange(profile, 0, 6, 0.7);
                MultiplyRange(profile, 12, 18, 1.2);
            }

            // Country adjustments for Shopping:
            if (countryId == 62 || countryId == 66) // France or Germany
            {
                // Early closing: reduce late hours
                MultiplyRange(profile, 18, 24, 0.6);
                if (dayType == 3) // On Sunday, further reduce shopping activity
                {
                    MultiplyAll(profile, 0.4);
                }
            }

            Normalize(profile);
            return (float)profile[hour] / 100f;
        }

        /// <summary>
        /// Returns the probability (%) for Park at the given hour.
        /// </summary>
        public static float GetParkProbability(int countryId, int dayType, int hour)
        {
            double[] profile = (double[])ParkBase.Clone();

            // Apply day type modifications for Park:
            if (dayType == 0) // Friday
            {
                // No special day modifier for Park on Friday
            }
            else if (dayType == 2) // Saturday
            {
                MultiplyRange(profile, 0, 8, 0.8);
                MultiplyAll(profile, 1.2);
            }
            else if (dayType == 3) // Sunday
            {
                MultiplyAll(profile, 1.1);
                MultiplyRange(profile, 0, 6, 0.7);
                MultiplyRange(profile, 12, 18, 1.2);
            }

            // Country adjustments for Park:
            if (countryId == 25 || countryId == 140) // Brazil or Philippines
            {
                MultiplyRange(profile, 11, 17, 1.2);
                MultiplyRange(profile, 0, 8, 0.8);
            }

            Normalize(profile);
            return (float)profile[hour] / 100f;
        }

        /// <summary>
        /// Returns the probability (%) for Travel at the given hour.
        /// </summary>
        public static float GetTravelProbability(int countryId, int dayType, int hour)
        {
            double[] profile = (double[])TravelBase.Clone();

            // Apply day type modifications for Travel:
            if (dayType == 0) // Friday
            {
                // Boost evening travel on Friday (if needed)
                for (int h = 17; h < 24; h++)
                    profile[h] *= 1.2;
            }
            else if (dayType == 2) // Saturday
            {
                MultiplyRange(profile, 0, 8, 0.8);
                MultiplyAll(profile, 1.2);
            }
            else if (dayType == 3) // Sunday
            {
                MultiplyAll(profile, 1.1);
                MultiplyRange(profile, 0, 6, 0.7);
                MultiplyRange(profile, 12, 18, 1.2);
            }

            // Country adjustment for Travel:
            if (countryId == 188) // USA
            {
                // Boost morning commute (hour 7-8)
                MultiplyRange(profile, 7, 9, 1.2);
            }

            Normalize(profile);
            return (float)profile[hour] / 100f;
        }

        /// <summary>
        /// Multiplies each element in profile from index 'start' (inclusive) to 'end' (exclusive) by a factor.
        /// </summary>
        private static void MultiplyRange(double[] profile, int start, int end, double factor)
        {
            for (int i = start; i < Math.Min(end, profile.Length); i++)
            {
                profile[i] *= factor;
            }
        }

        /// <summary>
        /// Multiplies every element in the profile by the given factor.
        /// </summary>
        private static void MultiplyAll(double[] profile, double factor)
        {
            for (int i = 0; i < profile.Length; i++)
            {
                profile[i] *= factor;
            }
        }

        /// <summary>
        /// Normalizes the profile array so that the sum of its elements equals 100.
        /// </summary>
        private static void Normalize(double[] profile)
        {
            double sum = profile.Sum();
            if (sum > 0)
            {
                for (int i = 0; i < profile.Length; i++)
                {
                    profile[i] = (profile[i] / sum) * 100;
                }
            }
        }
    }
}

