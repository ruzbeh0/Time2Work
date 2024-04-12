using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;
using Game.Serialization;
using Game.Simulation;
using Time2Work;
using Unity.Entities;
using HarmonyLib;
using System.Linq;
using System.IO;

namespace Time2Work
{
    public class Mod : IMod
    {
        public static readonly string harmonyID = "Time2Work";
        public static ILog log = LogManager.GetLogger($"{nameof(Time2Work)}.{nameof(Mod)}").SetShowsErrorsInUI(false);
        //public static string output = "C:\\Users\\ruzbe\\AppData\\LocalLow\\Colossal Order\\Cities Skylines II\\ModsData\\DataOutput.csv";
        public static string output = "DataOutput.csv";
        public static ILog log2 = LogManager.GetLogger($"DataOutput").SetShowsErrorsInUI(false);
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

            // Disable original systems
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Simulation.CitizenBehaviorSystem>().Enabled = false;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Simulation.CitizenTravelPurposeSystem>().Enabled = false;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Simulation.WorkerSystem>().Enabled = false;

            updateSystem.UpdateAt<WorkPlaceShiftUpdateSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<WorkerShiftUpdateSystem>(SystemUpdatePhase.GameSimulation);
            //updateSystem.UpdateAt<CitizenStatistics>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<Time2WorkCitizenBehaviorSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<Time2WorkCitizenTravelPurposeSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<Time2WorkWorkerSystem>(SystemUpdatePhase.GameSimulation);
            //updateSystem.UpdateAt<Time2WorkTimeSystem>(SystemUpdatePhase.GameSimulation);
            //updateSystem.UpdateAt<Time2WorkTimeSystem>(SystemUpdatePhase.EditorSimulation);
            //updateSystem.UpdateAfter<PostDeserialize<Time2WorkTimeSystem>>(SystemUpdatePhase.Deserialize);

            //Harmony
            var harmony = new Harmony(harmonyID);
            Harmony.DEBUG = true;
            harmony.PatchAll(typeof(Mod).Assembly);
            var patchedMethods = harmony.GetPatchedMethods().ToArray();
            log.Info($"Plugin {harmonyID} made patches! Patched methods: " + patchedMethods);
            foreach (var patchedMethod in patchedMethods)
            {
                log.Info($"Patched method: {patchedMethod.Module.Name}:{patchedMethod.Name}");
            }

        }

        public void OnDispose()
        {
            log.Info(nameof(OnDispose));

            var harmony = new Harmony(harmonyID);
            harmony.UnpatchAll(harmonyID);

            if (m_Setting != null)
            {
                m_Setting.UnregisterInOptionsUI();
                m_Setting = null;
            }
        }
    }
}