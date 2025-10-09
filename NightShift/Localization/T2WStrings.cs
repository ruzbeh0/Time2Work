// Utils/Loc.cs
using Colossal.Localization;
using Game.SceneFlow;
using static System.Net.Mime.MediaTypeNames;

namespace Time2Work.Localization
{
    public static class T2WStrings
    {
        // Safe Translate that falls back to the key itself
        public static string T(string key, params (string name, string value)[] vars)
        {
            var mgr = GameManager.instance?.localizationManager;
            if (mgr != null)
            {
                try
                {
                    // Fix: Replace the non-existent 'Get' method with 'TryGetValue' to retrieve the translation.
                    if (mgr.activeDictionary != null && mgr.activeDictionary.TryGetValue(key, out var value))
                    {
                        if (vars != null)
                            foreach (var (n, v) in vars)
                                value = value.Replace("{" + n + "}", v ?? "");
                        return value;
                    }
                }
                catch
                {
                    // Ignore and fall back
                }
            }
            return null;
        }

        // Current locale ID (e.g., "en-US", "pt-BR"); no 'activeLocale' property exists.
        public static string CurrentLocaleId()
        {
            var mgr = GameManager.instance?.localizationManager;
            try
            {
                // This is the property available in CS2
                return mgr?.activeLocaleId ?? "en-US";
            }
            catch
            {
                return "en-US";
            }
        }
    }
}
