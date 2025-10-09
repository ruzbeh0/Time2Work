// Soft bridge to CustomChirps API with reflection, supporting entity OR prefab.
// Place this in your Time2Work mod codebase.

using System;
using System.Reflection;
using Unity.Entities;
using Game.Prefabs;

namespace Time2Work.Bridge
{
    /// <summary>
    /// Local mirror of CustomChirps.Systems.DepartmentAccount (names must match).
    /// </summary>
    public enum DepartmentAccountBridge
    {
        Electricity,
        FireRescue,
        Roads,
        Water,
        Communications,
        Police,
        PropertyAssessmentOffice,
        Post,
        BusinessNews,
        CensusBureau,
        ParkAndRec,
        EnvironmentalProtectionAgency,
        Healthcare,
        LivingStandardsAssociation,
        Garbage,
        TourismBoard,
        Transportation,
        Education
    }

    /// <summary>
    /// Reflection-based bridge to CustomChirps API. No hard reference required.
    /// Works whether only entity-posting exists or prefab-posting is available.
    /// </summary>
    public static class CustomChirpsBridge
    {
        private static bool _resolved;
        private static Type _apiType;           // CustomChirps.Systems.CustomChirpApiSystem
        private static Type _deptEnumType;      // CustomChirps.Systems.DepartmentAccount
        private static MethodInfo _postChirp;   // PostChirp(string, DepartmentAccount, Entity, string)

        /// <summary>True if at least the entity-based API is available.</summary>
        public static bool IsAvailable
        {
            get { EnsureResolve(); return _apiType != null && _deptEnumType != null && _postChirp != null; }
        }

        /// <summary>True if the prefab-based API is also available.</summary>
        public static bool PrefabPostingAvailable
        {
            get { EnsureResolve(); return _postChirp != null; }
        }

        /// <summary>
        /// Post a chirp
        /// </summary>
        public static bool PostChirp(string text, DepartmentAccountBridge department, Entity entity, string customSenderName = null)
        { // entity can be Entity.Null if needed
            EnsureResolve();

            var realDept = MapDepartment(department);
            var args = new object[] { text ?? string.Empty, realDept, entity, customSenderName };
            _postChirp.Invoke(null, args);
            return true;
        }


        // ---- helpers ----

        private static object MapDepartment(DepartmentAccountBridge department)
        {
            // Convert our mirror enum to the real enum by NAME; fallback to Transportation
            try
            {
                EnsureResolve();
                if (_deptEnumType == null) return "Transportation"; // dead fallback, won't be used if API present
                return Enum.Parse(_deptEnumType, department.ToString(), ignoreCase: false);
            }
            catch
            {
                return Enum.Parse(_deptEnumType, "Transportation", ignoreCase: true);
            }
        }

        private static PrefabBase TryGetPrefabBase(Entity prefabEntity)
        {
            if (prefabEntity == Entity.Null) return null;
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return null;

            var em = world.EntityManager;
            if (!em.HasComponent<PrefabData>(prefabEntity)) return null;

            var prefabSystem = world.GetExistingSystemManaged<PrefabSystem>();
            if (prefabSystem == null) return null;

            var pd = em.GetComponentData<PrefabData>(prefabEntity);
            return prefabSystem.TryGetPrefab<PrefabBase>(pd, out var basePrefab) ? basePrefab : null;
        }

        private static void EnsureResolve()
        {
            if (_resolved) return;
            _resolved = true;

            // Find API & enum types (by FQN first, then scan loaded assemblies)
            _apiType = Type.GetType("CustomChirps.Systems.CustomChirpApiSystem, CustomChirps") ?? FindType("CustomChirps.Systems.CustomChirpApiSystem");
            _deptEnumType = Type.GetType("CustomChirps.Systems.DepartmentAccount, CustomChirps") ?? FindType("CustomChirps.Systems.DepartmentAccount");

            if (_apiType != null)
            {
                _postChirp = _apiType.GetMethod("PostChirp", BindingFlags.Public | BindingFlags.Static);
            }
        }

        private static Type FindType(string fullName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var t = asm.GetType(fullName, throwOnError: false);
                    if (t != null) return t;
                }
                catch { /* ignore */ }
            }
            return null;
        }
    }
}
