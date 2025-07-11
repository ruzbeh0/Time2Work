﻿using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Colossal.PSI.Environment;
using Colossal.UI;
using Game;
using Game.Modding;
using Game.SceneFlow;
using Game.Serialization;
using Game.Settings;
using Game.Simulation;
using Game.UI.InGame;
using HarmonyLib;
using System;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using Time2Work.Systems;
using Unity.Entities;
using UnityEngine;
using static Colossal.Json.DiffUtility;
using static Time2Work.Setting;

namespace Time2Work
{
    public class Mod : IMod
    {
        public static readonly string harmonyID = "RealisticTrips";
        public static readonly string Id = "Time2Work";
        public static ILog log = LogManager.GetLogger($"RealisticTrips").SetShowsErrorsInUI(false);
        public static Setting m_Setting;
        public static int numCurrentEvents = 999;
        public static string version = "1.8.4";
        public static string modPath;
        public static Mod Instance { get; private set; }
        internal ILog Log { get; private set; }

        // Mods Settings Folder
        public static string SettingsFolder = Path.Combine(EnvPath.kUserDataPath, "ModsSettings", nameof(Time2Work));
        // Mods Data Folder
        public static string DataFolder = Path.Combine(EnvPath.kUserDataPath, "ModsData", nameof(Time2Work));

        public void OnLoad(UpdateSystem updateSystem)
        {
            log.Info($"Realistic Trips - Version:{version}");

            if(!Directory.Exists(SettingsFolder))
            {
                Directory.CreateDirectory(SettingsFolder);
            }
            
            if (!Directory.Exists(DataFolder))
            {
                Directory.CreateDirectory(DataFolder);
            }

            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
            {
                modPath = Path.GetDirectoryName(asset.path);
                log.Info($"Current mod asset at {asset.path}");
            }

            m_Setting = new Setting(this);
            //m_ModData = new ModData();

            foreach (var modInfo in GameManager.instance.modManager)
            {
                if (modInfo.asset.name.Equals("RealPop"))
                {
                    Mod.log.Info($"Loaded mod with conflict: {modInfo.asset.name}");
                }
            }

            m_Setting.RegisterInOptionsUI();
            GameManager.instance.localizationManager.AddSource("en-US", new LocaleEN(m_Setting));
            GameManager.instance.localizationManager.AddSource("pt-BR", new LocalePT(m_Setting));

            AssetDatabase.global.LoadSettings(nameof(Time2Work), m_Setting, new Setting(this));
            //AssetDatabase.global.LoadSettings("data_" + nameof(Time2Work), m_ModData, new ModData(this));

            // Disable original systems
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Simulation.CitizenBehaviorSystem>().Enabled = false;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Simulation.CitizenTravelPurposeSystem>().Enabled = false;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Simulation.WorkerSystem>().Enabled = false;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Simulation.LeisureSystem>().Enabled = false;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Simulation.StudentSystem>().Enabled = false;
            if(m_Setting.better_trucks)
            {
                World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Simulation.TripNeededSystem>().Enabled = false;
            }
            else
            {
                World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Simulation.TripNeededSystem>().Enabled = true;
            }

            World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Simulation.TourismSystem>().Enabled = false;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Simulation.TouristSpawnSystem>().Enabled = false;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Simulation.AttractionSystem>().Enabled = false;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.UI.InGame.TimeUISystem>().Enabled = false;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.UI.InGame.StatisticsUISystem>().Enabled = false;

            updateSystem.UpdateAt<Time2WorkTimeSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<Time2WorkTimeSystem>(SystemUpdatePhase.EditorSimulation);
            updateSystem.UpdateAfter<Time2WorkTimeSystem>(SystemUpdatePhase.Deserialize);
            updateSystem.UpdateAt<WorkPlaceShiftUpdateSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<WorkerShiftUpdateSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<Time2WorkCitizenBehaviorSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<Time2WorkCitizenTravelPurposeSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<Time2WorkWorkerSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<Time2WorkLeisureSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<Time2WorkStudentSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAfter<WeekSystem>(SystemUpdatePhase.GameSimulation);
            if (m_Setting.better_trucks)
            {
                updateSystem.UpdateBefore<Time2WorkCitizenTravelPurposeSystem, Time2Work.Systems.Time2WorkTripNeededSystem>(SystemUpdatePhase.GameSimulation);
                updateSystem.UpdateAt<Time2Work.Systems.Time2WorkTripNeededSystem>(SystemUpdatePhase.GameSimulation);
                updateSystem.UpdateAt<Time2Work.Systems.TruckScheduleSystem>(SystemUpdatePhase.GameSimulation);
            }
            
            //updateSystem.UpdateAt<Time2WorkStorageTransferSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<CompanyDisableNightNotificationSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<Time2WorkTourismSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<Time2WorkTouristSpawnSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<Time2WorkAttractionSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<EconomyParameterUpdaterSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<CitizenScheduleSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAfter<SpecialEventSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<SpecialEventsUISystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<Time2WorkTimeUISystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<Time2WorkUISystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<Time2WorkStatisticsUISystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAfter<TimeSettingsMultiplierSystem>(SystemUpdatePhase.PrefabUpdate);
            updateSystem.UpdateBefore<TimeSettingsMultiplierSystem>(SystemUpdatePhase.PrefabReferences);
            updateSystem.UpdateAfter<DemandParameterUpdaterSystem>(SystemUpdatePhase.PrefabUpdate);
            updateSystem.UpdateBefore<DemandParameterUpdaterSystem>(SystemUpdatePhase.PrefabReferences);
            CitizenScheduleSection citizenScheduleSection = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<CitizenScheduleSection>();
            

            //Harmony
            var harmony = new Harmony(harmonyID);
            //Harmony.DEBUG = true;
            harmony.PatchAll(typeof(Mod).Assembly);
            var patchedMethods = harmony.GetPatchedMethods().ToArray();
            log.Info($"Plugin {harmonyID} made patches! Patched methods: " + patchedMethods);
            foreach (var patchedMethod in patchedMethods)
            {
                log.Info($"Patched method: {patchedMethod.Module.Name}:{patchedMethod.Name}");
            }

            //Unity.Collections.NativeLeakDetection.Mode = Unity.Collections.NativeLeakDetectionMode.EnabledWithStackTrace;
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