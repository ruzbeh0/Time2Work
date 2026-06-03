using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Companies;
using Game.Simulation;
using Game.Tools;
using System;
using System.Reflection;
using Time2Work.Components;
using Unity.Entities;
using Unity.Mathematics;

namespace Time2Work.Bridge
{
    public enum SocialTripTypeBridge
    {
        Unknown = 0,
        VisitFriend = 1,
        VisitFamily = 2,
        BirthdayParty = 3,
        HouseParty = 4,
        Meetup = 5,
        GraduationParty = 6,
        WeddingParty = 7,
        NewBabyCelebration = 8,
        DateNight = 9,
        AfterWorkMeetup = 10,
        HousewarmingParty = 11,
        FirstJobCelebration = 12,
        StudyGroup = 13,
        ShoppingTrip = 14,
        Funeral = 15,
        HospitalVisit = 16,
        SchoolTroubleMeeting = 17,
        PrisonVisit = 18
    }

    public static class SocialTripsBridge
    {
        public const int ApiVersion = 1;
        private static bool s_MacroResolved;
        private static Type s_MacroBridgeType;
        private static MethodInfo s_TryConvertLeisureTrip;
        private static MethodInfo s_NotifySocialTripStarted;
        private delegate int GetLeisureTripConversionChancePercentDelegate();
        private delegate bool TryConvertLeisureTripDelegate(
            Entity citizen,
            Entity originalTarget,
            int originalLeisureType,
            out Entity targetBuilding,
            out Entity hostCitizen,
            out int tripType,
            out float durationMinutes,
            out int priority);

        private delegate void NotifySocialTripStartedDelegate(
            Entity citizen,
            Entity targetBuilding,
            Entity hostCitizen,
            int tripType);

        private static TryConvertLeisureTripDelegate s_TryConvertLeisureTripDelegate;
        private static NotifySocialTripStartedDelegate s_NotifySocialTripStartedDelegate;
        private static GetLeisureTripConversionChancePercentDelegate s_GetLeisureTripConversionChancePercentDelegate;

        public static bool IsAvailable => true;

        public static bool IsMacroProviderAvailable
        {
            get
            {
                EnsureMacroResolve();
                return s_TryConvertLeisureTrip != null;
            }
        }

        public static int GetApiVersion()
        {
            return ApiVersion;
        }

        public static int GetLeisureTripConversionChancePercent()
        {
            EnsureMacroResolve();
            if (s_TryConvertLeisureTrip == null)
                return 0;

            if (s_GetLeisureTripConversionChancePercentDelegate == null)
                return 100;

            try
            {
                return math.clamp(s_GetLeisureTripConversionChancePercentDelegate(), 0, 100);
            }
            catch (Exception ex)
            {
                Mod.log.Warn($"SocialTrips macro chance lookup failed: {ex.Message}");
                return 0;
            }
        }

        public static DateTime GetCurrentDateTime()
        {
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
                return DateTime.UtcNow;

            Time2WorkTimeSystem time2WorkTimeSystem = world.GetExistingSystemManaged<Time2WorkTimeSystem>();
            if (time2WorkTimeSystem != null)
                return time2WorkTimeSystem.GetCurrentDateTime();

            TimeSystem timeSystem = world.GetExistingSystemManaged<TimeSystem>();
            return timeSystem != null ? timeSystem.GetCurrentDateTime() : DateTime.UtcNow;
        }

        public static float GetNormalizedTime()
        {
            World world = World.DefaultGameObjectInjectionWorld;
            return world != null ? GetCurrentTimeOfDay(world) : 0f;
        }

        public static float GetNormalizedDate()
        {
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
                return 0f;

            Time2WorkTimeSystem time2WorkTimeSystem = world.GetExistingSystemManaged<Time2WorkTimeSystem>();
            if (time2WorkTimeSystem != null)
                return time2WorkTimeSystem.normalizedDate;

            TimeSystem timeSystem = world.GetExistingSystemManaged<TimeSystem>();
            return timeSystem != null ? timeSystem.normalizedDate : 0f;
        }

        public static int GetCurrentYear()
        {
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
                return GetCurrentDateTime().Year;

            Time2WorkTimeSystem time2WorkTimeSystem = world.GetExistingSystemManaged<Time2WorkTimeSystem>();
            if (time2WorkTimeSystem != null)
                return time2WorkTimeSystem.year;

            TimeSystem timeSystem = world.GetExistingSystemManaged<TimeSystem>();
            return timeSystem != null ? timeSystem.year : GetCurrentDateTime().Year;
        }

        public static int GetDaysPerYear()
        {
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
                return 365;

            Time2WorkTimeSystem time2WorkTimeSystem = world.GetExistingSystemManaged<Time2WorkTimeSystem>();
            if (time2WorkTimeSystem != null)
                return math.max(1, time2WorkTimeSystem.daysPerYear);

            TimeSystem timeSystem = world.GetExistingSystemManaged<TimeSystem>();
            return timeSystem != null ? math.max(1, timeSystem.daysPerYear) : 365;
        }

        public static int GetCurrentDayOfYearIndex()
        {
            int daysPerYear = GetDaysPerYear();
            return math.clamp((int)Math.Floor(math.saturate(GetNormalizedDate()) * daysPerYear), 0, daysPerYear - 1);
        }

        public static int GetCurrentDay()
        {
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
                return 0;

            SimulationSystem simulationSystem = world.GetExistingSystemManaged<SimulationSystem>();
            if (simulationSystem == null)
                return 0;

            EntityQuery timeDataQuery = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<TimeData>());
            try
            {
                if (timeDataQuery.IsEmptyIgnoreFilter)
                    return 0;

                int ticksPerDay = Time2WorkTimeSystem.kTicksPerDay > 0
                    ? Time2WorkTimeSystem.kTicksPerDay
                    : TimeSystem.kTicksPerDay;
                return Time2WorkTimeSystem.GetDay(simulationSystem.frameIndex, timeDataQuery.GetSingleton<TimeData>(), ticksPerDay);
            }
            finally
            {
                timeDataQuery.Dispose();
            }
        }

        public static bool CanRequestSocialTrip(Entity citizen, Entity targetBuilding)
        {
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
                return false;

            EntityManager entityManager = world.EntityManager;
            return entityManager.Exists(citizen) &&
                   entityManager.Exists(targetBuilding) &&
                   entityManager.HasComponent<Citizen>(citizen) &&
                   entityManager.HasComponent<Building>(targetBuilding) &&
                   entityManager.HasBuffer<TripNeeded>(citizen) &&
                   entityManager.HasComponent<CurrentBuilding>(citizen) &&
                   !entityManager.HasComponent<TravelPurpose>(citizen) &&
                   !entityManager.HasComponent<SocialTripData>(citizen);
        }

        public static bool IsCitizenOutsideWorkHours(Entity citizen)
        {
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
                return false;

            EntityManager entityManager = world.EntityManager;
            if (!entityManager.Exists(citizen) ||
                !entityManager.HasComponent<Citizen>(citizen) ||
                entityManager.HasComponent<Deleted>(citizen) ||
                entityManager.HasComponent<Temp>(citizen))
            {
                return false;
            }

            if (entityManager.HasComponent<TravelPurpose>(citizen))
            {
                Purpose purpose = entityManager.GetComponentData<TravelPurpose>(citizen).m_Purpose;
                if (purpose == Purpose.GoingToWork || purpose == Purpose.Working)
                    return false;
            }

            if (!entityManager.HasComponent<Worker>(citizen))
                return true;

            float timeOfDay = GetCurrentTimeOfDay(world);
            if (entityManager.HasComponent<CitizenSchedule>(citizen))
            {
                CitizenSchedule schedule = entityManager.GetComponentData<CitizenSchedule>(citizen);
                if (schedule.dayoff || schedule.go_to_work < 0f || schedule.end_work < 0f)
                    return true;

                return !IsInTimeRange(timeOfDay, schedule.go_to_work, schedule.end_work);
            }

            Worker worker = entityManager.GetComponentData<Worker>(citizen);
            switch (worker.m_Shift)
            {
                case Workshift.Evening:
                    return !IsInTimeRange(timeOfDay, 14f / 24f, 22f / 24f);
                case Workshift.Night:
                    return !IsInTimeRange(timeOfDay, 22f / 24f, 6f / 24f);
                default:
                    return !IsInTimeRange(timeOfDay, 8f / 24f, 17f / 24f);
            }
        }

        public static bool RequestSocialTrip(
            Entity citizen,
            Entity targetBuilding,
            Entity hostCitizen,
            int tripType,
            float durationMinutes,
            int priority)
        {
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
                return false;

            EntityManager entityManager = world.EntityManager;
            if (!entityManager.Exists(citizen) ||
                !entityManager.Exists(targetBuilding) ||
                !entityManager.HasComponent<Citizen>(citizen) ||
                !entityManager.HasComponent<Building>(targetBuilding) ||
                !entityManager.HasBuffer<TripNeeded>(citizen) ||
                !entityManager.HasComponent<CurrentBuilding>(citizen) ||
                entityManager.HasComponent<TravelPurpose>(citizen) ||
                entityManager.HasComponent<SocialTripData>(citizen))
            {
                return false;
            }

            if (IsHomeVisitTrip(tripType) &&
                !IsHostAvailableAtHome(entityManager, hostCitizen, targetBuilding))
            {
                return false;
            }

            float duration = Math.Max(10f, Math.Min(480f, durationMinutes)) / 1440f;
            uint frame = 0;
            SimulationSystem simulationSystem = world.GetExistingSystemManaged<SimulationSystem>();
            if (simulationSystem != null)
            {
                frame = simulationSystem.frameIndex;
            }

            SocialTripRequest request = new SocialTripRequest()
            {
                version = 1,
                targetBuilding = targetBuilding,
                hostCitizen = hostCitizen,
                tripType = tripType,
                duration = duration,
                priority = priority,
                requestedFrame = frame
            };

            if (entityManager.HasComponent<SocialTripRequest>(citizen))
            {
                SocialTripRequest existing = entityManager.GetComponentData<SocialTripRequest>(citizen);
                if (existing.priority > priority)
                    return false;

                entityManager.SetComponentData(citizen, request);
            }
            else
            {
                entityManager.AddComponentData(citizen, request);
            }

            return true;
        }

        private static bool IsHomeVisitTrip(int tripType)
        {
            return tripType == (int)SocialTripTypeBridge.VisitFriend ||
                   tripType == (int)SocialTripTypeBridge.VisitFamily;
        }

        private static bool IsHostAvailableAtHome(EntityManager entityManager, Entity hostCitizen, Entity targetBuilding)
        {
            if (hostCitizen == Entity.Null ||
                !entityManager.Exists(hostCitizen) ||
                !entityManager.HasComponent<Citizen>(hostCitizen) ||
                !entityManager.HasComponent<HouseholdMember>(hostCitizen) ||
                !entityManager.HasComponent<CurrentBuilding>(hostCitizen) ||
                !entityManager.HasBuffer<TripNeeded>(hostCitizen) ||
                entityManager.HasComponent<TravelPurpose>(hostCitizen) ||
                entityManager.HasComponent<SocialTripData>(hostCitizen) ||
                entityManager.HasComponent<Deleted>(hostCitizen) ||
                entityManager.HasComponent<Temp>(hostCitizen) ||
                entityManager.HasComponent<HealthProblem>(hostCitizen))
            {
                return false;
            }

            CurrentBuilding currentBuilding = entityManager.GetComponentData<CurrentBuilding>(hostCitizen);
            if (currentBuilding.m_CurrentBuilding != targetBuilding)
                return false;

            Entity household = entityManager.GetComponentData<HouseholdMember>(hostCitizen).m_Household;
            if (household == Entity.Null ||
                !entityManager.Exists(household) ||
                !entityManager.HasComponent<PropertyRenter>(household))
            {
                return false;
            }

            return entityManager.GetComponentData<PropertyRenter>(household).m_Property == targetBuilding;
        }

        public static bool TryConvertLeisureTrip(
            Entity citizen,
            Entity originalTarget,
            int originalLeisureType,
            out Entity targetBuilding,
            out Entity hostCitizen,
            out int tripType,
            out float durationMinutes,
            out int priority)
        {
            targetBuilding = Entity.Null;
            hostCitizen = Entity.Null;
            tripType = (int)SocialTripTypeBridge.Unknown;
            durationMinutes = 0f;
            priority = 0;

            EnsureMacroResolve();
            if (s_TryConvertLeisureTrip == null)
                return false;

            try
            {
                bool converted;
                if (s_TryConvertLeisureTripDelegate != null)
                {
                    converted = s_TryConvertLeisureTripDelegate(
                        citizen,
                        originalTarget,
                        originalLeisureType,
                        out targetBuilding,
                        out hostCitizen,
                        out tripType,
                        out durationMinutes,
                        out priority);
                }
                else
                {
                    object[] args =
                    {
                        citizen,
                        originalTarget,
                        originalLeisureType,
                        targetBuilding,
                        hostCitizen,
                        tripType,
                        durationMinutes,
                        priority
                    };

                    converted = (bool)s_TryConvertLeisureTrip.Invoke(null, args);
                    targetBuilding = (Entity)args[3];
                    hostCitizen = (Entity)args[4];
                    tripType = (int)args[5];
                    durationMinutes = (float)args[6];
                    priority = (int)args[7];
                }

                if (!converted)
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                Mod.log.Warn($"SocialTrips macro conversion failed: {ex.Message}");
                return false;
            }
        }

        public static void NotifySocialTripStarted(
            Entity citizen,
            Entity targetBuilding,
            Entity hostCitizen,
            int tripType)
        {
            EnsureMacroResolve();
            if (s_NotifySocialTripStarted == null)
                return;

            try
            {
                if (s_NotifySocialTripStartedDelegate != null)
                {
                    s_NotifySocialTripStartedDelegate(citizen, targetBuilding, hostCitizen, tripType);
                }
                else
                {
                    s_NotifySocialTripStarted.Invoke(null, new object[] { citizen, targetBuilding, hostCitizen, tripType });
                }
            }
            catch (Exception ex)
            {
                Mod.log.Warn($"SocialTrips macro notification failed: {ex.Message}");
            }
        }

        private static TDelegate CreateBridgeDelegate<TDelegate>(MethodInfo method)
            where TDelegate : class
        {
            if (method == null)
                return null;

            try
            {
                return Delegate.CreateDelegate(typeof(TDelegate), method) as TDelegate;
            }
            catch
            {
                return null;
            }
        }

        private static void EnsureMacroResolve()
        {
            if (s_MacroResolved && s_TryConvertLeisureTrip != null)
                return;

            s_MacroResolved = true;
            s_MacroBridgeType = Type.GetType("SocialTrips.Bridge.SocialTripsMacroBridge, SocialTrips") ??
                                FindType("SocialTrips.Bridge.SocialTripsMacroBridge");

            if (s_MacroBridgeType == null)
            {
                s_MacroResolved = false;
                return;
            }

            s_TryConvertLeisureTrip = s_MacroBridgeType.GetMethod(
                "TryConvertPreRolledLeisureTrip",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[]
                {
                    typeof(Entity),
                    typeof(Entity),
                    typeof(int),
                    typeof(Entity).MakeByRefType(),
                    typeof(Entity).MakeByRefType(),
                    typeof(int).MakeByRefType(),
                    typeof(float).MakeByRefType(),
                    typeof(int).MakeByRefType()
                },
                null);

            if (s_TryConvertLeisureTrip == null)
            {
                s_TryConvertLeisureTrip = s_MacroBridgeType.GetMethod(
                    "TryConvertLeisureTrip",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[]
                    {
                        typeof(Entity),
                        typeof(Entity),
                        typeof(int),
                        typeof(Entity).MakeByRefType(),
                        typeof(Entity).MakeByRefType(),
                        typeof(int).MakeByRefType(),
                        typeof(float).MakeByRefType(),
                        typeof(int).MakeByRefType()
                    },
                    null);
            }

            s_NotifySocialTripStarted = s_MacroBridgeType.GetMethod(
                "NotifySocialTripStarted",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[]
                {
                    typeof(Entity),
                    typeof(Entity),
                    typeof(Entity),
                    typeof(int)
                },
                null);

            MethodInfo getLeisureTripConversionChancePercent = s_MacroBridgeType.GetMethod(
                "GetLeisureTripConversionChancePercent",
                BindingFlags.Public | BindingFlags.Static,
                null,
                Type.EmptyTypes,
                null);

            s_TryConvertLeisureTripDelegate = CreateBridgeDelegate<TryConvertLeisureTripDelegate>(s_TryConvertLeisureTrip);
            s_NotifySocialTripStartedDelegate = CreateBridgeDelegate<NotifySocialTripStartedDelegate>(s_NotifySocialTripStarted);
            s_GetLeisureTripConversionChancePercentDelegate =
                CreateBridgeDelegate<GetLeisureTripConversionChancePercentDelegate>(getLeisureTripConversionChancePercent);

            if (s_TryConvertLeisureTrip == null)
                s_MacroResolved = false;
        }

        private static Type FindType(string fullName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    Type type = assembly.GetType(fullName, throwOnError: false);
                    if (type != null)
                        return type;
                }
                catch
                {
                }
            }

            return null;
        }

        private static float GetCurrentTimeOfDay(World world)
        {
            Time2WorkTimeSystem time2WorkTimeSystem = world.GetExistingSystemManaged<Time2WorkTimeSystem>();
            if (time2WorkTimeSystem != null)
                return time2WorkTimeSystem.normalizedTime;

            TimeSystem timeSystem = world.GetExistingSystemManaged<TimeSystem>();
            return timeSystem != null ? timeSystem.normalizedTime : 0f;
        }

        private static bool IsInTimeRange(float timeOfDay, float start, float end)
        {
            timeOfDay = math.frac(timeOfDay);
            start = math.frac(start);
            end = math.frac(end);

            return start >= end
                ? timeOfDay >= start || timeOfDay <= end
                : timeOfDay >= start && timeOfDay <= end;
        }
    }
}
