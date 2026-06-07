using System;

namespace Time2Work.Bridge
{
    public static class ElectionsBridge
    {
        public const int ApiVersion = 3;

        private static int s_EffectId;
        private static float s_ResourceConsumptionMultiplier = 1f;
        private static bool s_ElectionDaySundayOverrideEnabled;
        private static int s_ElectionDaySundayOverrideYear;
        private static int s_ElectionDaySundayOverrideDayOfYear;
        private static int s_ElectionDaySundayOverrideRevision;
        private static bool s_ElectionDaySpecialEventsSuppressedEnabled;
        private static int s_ElectionDaySpecialEventsSuppressedYear;
        private static int s_ElectionDaySpecialEventsSuppressedDayOfYear;

        public static int GetApiVersion()
        {
            return ApiVersion;
        }

        public static void SetMayorResourceConsumptionMultiplier(int effectId, float multiplier)
        {
            s_EffectId = effectId;
            s_ResourceConsumptionMultiplier = Clamp(multiplier, 0.75f, 1.25f);
        }

        public static void ClearMayorResourceConsumptionMultiplier(int effectId)
        {
            if (effectId == 0 || effectId == s_EffectId)
            {
                s_EffectId = 0;
                s_ResourceConsumptionMultiplier = 1f;
            }
        }

        public static float GetResourceConsumptionMultiplier()
        {
            return s_ResourceConsumptionMultiplier;
        }

        public static float GetEffectiveResourceConsumption(float baseValue)
        {
            return Math.Max(1f, baseValue * s_ResourceConsumptionMultiplier);
        }

        public static void SetElectionDaySundayOverride(int year, int dayOfYear, bool enabled)
        {
            if (!enabled || year <= 0 || dayOfYear <= 0)
            {
                ClearElectionDaySundayOverride();
                return;
            }

            if (s_ElectionDaySundayOverrideEnabled &&
                s_ElectionDaySundayOverrideYear == year &&
                s_ElectionDaySundayOverrideDayOfYear == dayOfYear)
            {
                return;
            }

            s_ElectionDaySundayOverrideEnabled = true;
            s_ElectionDaySundayOverrideYear = year;
            s_ElectionDaySundayOverrideDayOfYear = dayOfYear;
            s_ElectionDaySundayOverrideRevision++;
        }

        public static void ClearElectionDaySundayOverride()
        {
            if (!s_ElectionDaySundayOverrideEnabled &&
                s_ElectionDaySundayOverrideYear == 0 &&
                s_ElectionDaySundayOverrideDayOfYear == 0)
            {
                return;
            }

            s_ElectionDaySundayOverrideEnabled = false;
            s_ElectionDaySundayOverrideYear = 0;
            s_ElectionDaySundayOverrideDayOfYear = 0;
            s_ElectionDaySundayOverrideRevision++;
        }

        public static bool IsElectionDaySundayOverrideActive(int year, int dayOfYear)
        {
            return s_ElectionDaySundayOverrideEnabled &&
                   s_ElectionDaySundayOverrideYear == year &&
                   s_ElectionDaySundayOverrideDayOfYear == dayOfYear;
        }

        public static int GetElectionDaySundayOverrideRevision()
        {
            return s_ElectionDaySundayOverrideRevision;
        }

        public static void SetElectionDaySpecialEventsSuppressed(int year, int dayOfYear, bool enabled)
        {
            if (!enabled || year <= 0 || dayOfYear <= 0)
            {
                ClearElectionDaySpecialEventsSuppressed();
                return;
            }

            s_ElectionDaySpecialEventsSuppressedEnabled = true;
            s_ElectionDaySpecialEventsSuppressedYear = year;
            s_ElectionDaySpecialEventsSuppressedDayOfYear = dayOfYear;
        }

        public static void ClearElectionDaySpecialEventsSuppressed()
        {
            s_ElectionDaySpecialEventsSuppressedEnabled = false;
            s_ElectionDaySpecialEventsSuppressedYear = 0;
            s_ElectionDaySpecialEventsSuppressedDayOfYear = 0;
        }

        public static bool AreElectionDaySpecialEventsSuppressed(int year, int dayOfYear)
        {
            return s_ElectionDaySpecialEventsSuppressedEnabled &&
                   s_ElectionDaySpecialEventsSuppressedYear == year &&
                   s_ElectionDaySpecialEventsSuppressedDayOfYear == dayOfYear;
        }

        private static float Clamp(float value, float min, float max)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                return 1f;

            return Math.Max(min, Math.Min(max, value));
        }
    }
}
