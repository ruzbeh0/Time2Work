using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;
using Time2Work;

namespace Time2Work
{
    public class Mod : IMod
    {
        public static ILog log = LogManager.GetLogger($"{nameof(Time2Work)}.{nameof(Mod)}").SetShowsErrorsInUI(false);
        public static Setting m_Setting;

        public void OnLoad(UpdateSystem updateSystem)
        {
            log.Info(nameof(OnLoad));

            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
                log.Info($"Current mod asset at {asset.path}");

            m_Setting = new Setting(this);
            m_Setting.RegisterInOptionsUI();
            GameManager.instance.localizationManager.AddSource("en-US", new LocaleEN(m_Setting));

            AssetDatabase.global.LoadSettings(nameof(Time2Work), m_Setting, new Setting(this));

            updateSystem.UpdateAfter<WorkPlaceShiftUpdateSystem>(SystemUpdatePhase.PrefabUpdate);
            updateSystem.UpdateBefore<WorkPlaceShiftUpdateSystem>(SystemUpdatePhase.PrefabReferences);
            updateSystem.UpdateAfter<WorkerShiftUpdateSystem>(SystemUpdatePhase.PrefabUpdate);
            updateSystem.UpdateBefore<WorkerShiftUpdateSystem>(SystemUpdatePhase.PrefabReferences);
        }

        public void OnDispose()
        {
            log.Info(nameof(OnDispose));
            if (m_Setting != null)
            {
                m_Setting.UnregisterInOptionsUI();
                m_Setting = null;
            }
        }
    }
}