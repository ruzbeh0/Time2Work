using Colossal;
using Colossal.IO.AssetDatabase;
using Colossal.IO.AssetDatabase.Internal;
using Colossal.PSI.Environment;
using Game;
using Game.Modding;
using Game.Prefabs;
using Game.Settings;
using Game.UI;
using Game.UI.Widgets;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Time2Work.Systems;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;

namespace Time2Work
{
    [FileLocation("ModsSettings\\" + nameof(Time2Work) + "\\" + nameof(Time2Work))]
    [SettingsUIGroupOrder(SettingsGroup, DelayGroup, WorkPlaceShiftGroup, NonDayShiftByWorkTypeGroup, RemoteGroup, DayShiftGroup, ResetGroup, ShopLeisureGroup, TimeOffGroup, LeisureMealsGroup, LeisureEntertainmentGroup, LeisureShoppingGroup, LeisureParksGroup, LeisureTravelGroup, ShoppingTimeGroup, ShoppingTripGateGroup, ShoppingCooldownGroup, HospitalStayGroup, SchoolTimeOffGroup, SchoolTimeGroup, School1WeekGroup, School2WeekGroup, School34WeekGroup, TimeOffGroup, DTSimulationGroup, SlowerTimeGroup, WeekGroup, EventGroup, MinEventGroup, MaxEventGroup, OfficeGroup, CommercialGroup, IndustryGroup, CityServicesGroup, ExternalGroup, ExpensesGroup, TrucksGroup, OtherGroup, VisitTimeGroup, HolidayGroup)]
    [SettingsUIShowGroupName(WorkPlaceShiftGroup, NonDayShiftByWorkTypeGroup, RemoteGroup, DayShiftGroup, SchoolTimeOffGroup, SchoolTimeGroup, TimeOffGroup, LeisureMealsGroup, LeisureEntertainmentGroup, LeisureShoppingGroup, LeisureParksGroup, LeisureTravelGroup, ShoppingTimeGroup, ShoppingTripGateGroup, ShoppingCooldownGroup, HospitalStayGroup, DTSimulationGroup, SlowerTimeGroup, School1WeekGroup, School2WeekGroup, School34WeekGroup, WeekGroup, OfficeGroup, CommercialGroup, IndustryGroup, CityServicesGroup, TrucksGroup, OtherGroup, ExternalGroup, ExpensesGroup, MinEventGroup, MaxEventGroup, VisitTimeGroup, HolidayGroup)]
    public class Setting : ModSetting
    {
        public const string SettingsSection = "Settings";
        public const string WorkSection = "Work";
        public const string ShopLeisureSection = "Shopping and Leisure";
        public const string HealthSection = "Health";
        public const string SchoolSection = "School";
        public const string Weeksection = "Week";
        public const string OtherSection = "Other";
        public const string EventSection = "Special Events";
        public const string ResetGroup = "Reset";
        public const string ShopLeisureGroup = "ShopLeisureGroup";
        public const string EventGroup = "EventGroup";

        public const string MinEventGroup = "MinEventGroup";
        public const string MaxEventGroup = "MaxEventGroup";
        public const string SettingsGroup = "SettingsGroup";
        public const string WorkPlaceShiftGroup = "WorkPlaceShiftGroup";
        public const string DelayGroup = "DelayGroup";
        public const string RemoteGroup = "RemoteGroup";
        public const string DayShiftGroup = "DayShiftGroup";
        public const string NonDayShiftByWorkTypeGroup = "NonDayShiftByWorkTypeGroup";
        public const string TimeOffGroup = "TimeOffGroup";
        public const string LeisureMealsGroup = "LeisureMealsGroup";
        public const string LeisureEntertainmentGroup = "LeisureEntertainmentGroup";
        public const string LeisureShoppingGroup = "LeisureShoppingGroup";
        public const string LeisureParksGroup = "LeisureParksGroup";
        public const string LeisureTravelGroup = "LeisureTravelGroup";
        public const string ShoppingTimeGroup = "ShoppingTimeGroup";
        public const string ShoppingTripGateGroup = "ShoppingTripGateGroup";
        public const string ShoppingCooldownGroup = "ShoppingCooldownGroup";
        public const string HospitalStayGroup = "HospitalStayGroup";
        public const string SchoolTimeOffGroup = "SchoolTimeOffGroup";
        public const string SchoolTimeGroup = "SchoolTimeGroup";
        public const string School1WeekGroup = "School1WeekGroup";
        public const string School2WeekGroup = "School2WeekGroup";
        public const string School34WeekGroup = "School34WeekGroup";
        public const string TrucksGroup = "TrucksGroup";
        public const string OtherGroup = "OtherGroup";
        public const string DTSimulationGroup = "DTSimulationGroup";
        public const string SlowerTimeGroup = "SlowerTimeGroup";
        public const string ExternalGroup = "ExternalGroup";
        public const string ExpensesGroup = "ExpensesGroup";
        public const string WeekGroup = "WeekGroup";
        public const string OfficeGroup = "OfficeGroup";
        public const string CommercialGroup = "CommercialGroup";
        public const string IndustryGroup = "IndustryGroup";
        public const string CityServicesGroup = "CityServicesGroup";
        public const string VisitTimeGroup = "VisitTimeGroup";
        public const string HolidayGroup = "HolidayGroup";

        Dictionary<int, int> countryIndexLookup = new Dictionary<int, int>();
        // Preset order follows SettingsEnum numeric order; keep every array length in sync.
        int[] evening_share_ = new int[] { 10, 17, 15, 13, 13, 5, 19, 15, 22, 14, 14, 19, 31, 13, 16, 32, 18, 15, 17, 8 };
        int[] night_share_ = new int[] { 8, 8, 7, 5, 7, 2, 12, 5, 10, 6, 8, 8, 8, 7, 11, 12, 10, 7, 9, 4 };
        int[] delay_factor_ = new int[] { 2, 4, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2 };
        int[] lunch_break_percentage_ = new int[] { 30, 10, 30, 30, 30, 30, 30, 30, 30, 30, 20, 30, 30, 30, 30, 30, 30, 30, 30, 30 };
        float[] holidays_per_year_ = new float[] { 11, 11, 15, 11, 13, 10, 11, 11, 17, 12, 16, 7, 7, 40, 13, 11, 12, 14, 8, 11 };
        float[] vacation_per_year_ = new float[] { 22, 22, 10, 20, 30, 21, 30, 28, 15, 20, 11, 12, 25, 15, 26, 15, 15, 22, 39, 22 };
        bool[] disable_early_shop_leisure_ = new bool[] { true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true };
        bool[] use_vanilla_timeoff_ = new bool[] { false, true, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false };
        bool[] use_school_vanilla_timeoff_ = new bool[] { false, true, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false };
        int[] school_start_time_ = new int[] { 2, 2, 1, 4, 1, 2, 3, 2, 2, 2, 3, 2, 2, 0, 2, 1, 1, 4, 3, 2 };
        int[] school_end_time_ = new int[] { 16, 16, 14, 16, 9, 16, 19, 12, 15, 14, 18, 14, 16, 17, 16, 15, 14, 18, 16, 16 };
        int[] high_school_start_time_ = new int[] { 2, 2, 1, 3, 1, 4, 2, 2, 2, 2, 3, 1, 2, 0, 2, 1, 1, 3, 3, 2 };
        int[] high_school_end_time_ = new int[] { 16, 16, 15, 17, 12, 18, 20, 16, 17, 16, 20, 15, 16, 21, 16, 18, 15, 16, 16, 16 };
        int[] univ_start_time_ = new int[] { 2, 2, 3, 4, 2, 2, 4, 2, 4, 4, 4, 2, 2, 1, 2, 3, 2, 4, 2, 2 };
        int[] univ_end_time_ = new int[] { 20, 20, 22, 20, 21, 22, 22, 22, 20, 22, 22, 22, 20, 26, 18, 20, 20, 24, 20, 20 };
        int[] work_start_time_ = new int[] { 4, 4, 3, 4, 4, 3, 4, 4, 4, 4, 4, 2, 4, 1, 2, 3, 2, 4, 3, 4 };
        int[] work_end_time_ = new int[] { 20, 20, 21, 20, 22, 20, 20, 20, 22, 22, 22, 22, 20, 22, 18, 22, 20, 23, 19, 20 };
        int[] dt_simulation_ = new int[] { 0, 0, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4 };
        float[] avg_work_hours_ft_wd_ = new float[] { 8.4f, 8.4f, 8.5f, 7.8f, 8.8f, 7.5f, 7.8f, 7.6f, 9f, 7.8f, 8.3f, 9.2f, 8.4f, 9.2f, 8f, 9f, 8.7f, 8.1f, 7.3f, 8.4f };
        float[] avg_work_hours_pt_wd_ = new float[] { 5.3f, 5.3f, 5f, 4.5f, 5.3f, 4f, 4.7f, 3.6f, 5.5f, 4.6f, 5f, 5.5f, 5.3f, 5f, 6f, 4.2f, 5f, 4.8f, 3.3f, 5.3f };
        float[] slow_time_factor_ = new float[] { 1f, 3.5f, 3.5f, 3.5f, 3.5f, 3.5f, 3.5f, 3.5f, 3.5f, 3.5f, 3.5f, 3.5f, 3.5f, 3.5f, 3.5f, 3.5f, 3.5f, 3.5f, 3.5f, 3.5f };
        int[] part_time_percentage_ = new int[] { 22, 22, 14, 30, 8, 18, 18, 30, 10, 18, 25, 15, 48, 60, 6, 7, 15, 14, 24, 17 };
        int[] remote_percentage_ = new int[] { 20, 20, 12, 35, 10, 20, 15, 13, 12, 15, 15, 10, 32, 55, 8, 20, 15, 18, 27, 14 };
        bool[] peak_spread_ = new bool[] { true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true };
        bool[] tourism_trips_ = new bool[] { true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true };
        bool[] commuter_trips_ = new bool[] { true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true };
        int[] service_expenses_night_reduction_ = new int[] { 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30 };
        int[] office_weekday_pct_ = new int[] { 90, 90, 90, 90, 90, 90, 90, 90, 92, 90, 92, 92, 90, 92, 95, 99, 90, 90, 90, 90 };
        int[] office_avgday_pct_ = new int[] { 88, 88, 88, 88, 88, 88, 88, 88, 90, 88, 90, 90, 88, 88, 90, 98, 88, 88, 88, 88 };
        int[] office_sat_pct_ = new int[] { 12, 12, 15, 12, 12, 12, 12, 12, 35, 12, 15, 25, 12, 12, 10, 30, 18, 12, 12, 12 };
        int[] office_sun_pct_ = new int[] { 6, 6, 5, 6, 6, 6, 6, 6, 8, 6, 5, 8, 6, 6, 2, 0, 8, 6, 6, 6 };
        int[] commercial_weekday_pct_ = new int[] { 64, 64, 75, 64, 64, 64, 64, 64, 85, 70, 70, 80, 64, 92, 90, 99, 70, 75, 64, 64 };
        int[] commercial_avgday_pct_ = new int[] { 68, 68, 75, 68, 68, 68, 68, 68, 85, 72, 75, 80, 68, 87, 85, 98, 72, 78, 68, 68 };
        int[] commercial_sat_pct_ = new int[] { 37, 37, 55, 45, 37, 37, 37, 37, 70, 50, 55, 60, 37, 50, 70, 80, 45, 55, 37, 37 };
        int[] commercial_sun_pct_ = new int[] { 26, 26, 30, 30, 26, 26, 26, 26, 45, 20, 40, 35, 26, 15, 15, 50, 30, 25, 26, 26 };
        int[] industry_weekday_pct_ = new int[] { 90, 90, 90, 90, 90, 90, 90, 90, 92, 90, 92, 92, 90, 92, 90, 99, 90, 90, 90, 90 };
        int[] industry_avgday_pct_ = new int[] { 86, 86, 88, 86, 86, 86, 86, 86, 90, 86, 90, 90, 86, 87, 80, 99, 86, 86, 86, 86 };
        int[] industry_sat_pct_ = new int[] { 24, 24, 30, 24, 24, 24, 24, 24, 50, 24, 35, 45, 24, 50, 30, 50, 25, 24, 24, 24 };
        int[] industry_sun_pct_ = new int[] { 11, 11, 10, 11, 11, 11, 11, 11, 20, 11, 15, 15, 11, 15, 10, 20, 12, 11, 11, 11 };
        int[] cityServices_weekday_pct_ = new int[] { 80, 80, 82, 80, 80, 80, 80, 80, 85, 80, 85, 85, 80, 92, 95, 90, 82, 80, 80, 80 };
        int[] cityServices_avgday_pct_ = new int[] { 78, 78, 80, 78, 78, 78, 78, 78, 82, 78, 85, 84, 78, 92, 90, 90, 80, 78, 78, 78 };
        int[] cityServices_sat_pct_ = new int[] { 17, 17, 20, 17, 17, 17, 17, 17, 40, 17, 35, 35, 17, 15, 60, 90, 25, 17, 17, 17 };
        int[] cityServices_sun_pct_ = new int[] { 12, 12, 12, 12, 12, 12, 12, 12, 25, 12, 25, 20, 12, 7, 40, 90, 15, 12, 12, 12 };
        int[] nonday_office_share_ = new int[] { 7, 7, 8, 7, 7, 13, 7, 7, 12, 7, 8, 10, 7, 20, 5, 1, 10, 7, 3, 7 };
        int[] nonday_commercial_share_ = new int[] { 31, 31, 28, 31, 31, 24, 31, 31, 30, 28, 25, 28, 31, 12, 15, 30, 28, 28, 8, 31 };
        int[] nonday_industry_share_ = new int[] { 14, 14, 18, 14, 14, 14, 14, 14, 25, 14, 18, 22, 14, 12, 25, 37, 16, 14, 15, 14 };
        int[] nonday_cityservices_share_ = new int[] { 18, 18, 20, 18, 18, 7, 18, 18, 28, 18, 22, 25, 18, 25, 50, 50, 22, 18, 8, 18 };
        int[] school_lv1_weekday_pct_ = new int[] { 93, 93, 96, 98, 84, 98, 98, 98, 95, 95, 98, 96, 93, 98, 97, 100, 96, 98, 93, 93 };
        int[] school_lv1_avgday_pct_ = new int[] { 92, 92, 95, 97, 83, 97, 97, 97, 95, 94, 98, 95, 92, 97, 95, 25, 95, 97, 92, 92 };
        int[] school_lv1_saturday_pct_ = new int[] { 0, 0, 0, 0, 0, 1, 0, 0, 20, 5, 1, 0, 0, 2, 0, 1, 0, 0, 0, 0 };
        int[] school_lv1_sunday_pct_ = new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        int[] school_lv2_weekday_pct_ = new int[] { 90, 90, 93, 95, 81, 93, 98, 98, 95, 95, 98, 93, 90, 97, 90, 100, 93, 95, 90, 90 };
        int[] school_lv2_avgday_pct_ = new int[] { 88, 88, 90, 94, 79, 88, 97, 97, 95, 94, 98, 90, 88, 95, 85, 25, 90, 94, 88, 88 };
        int[] school_lv2_saturday_pct_ = new int[] { 0, 0, 0, 0, 0, 2, 0, 0, 30, 5, 5, 5, 0, 2, 0, 1, 0, 0, 0, 0 };
        int[] school_lv2_sunday_pct_ = new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        int[] school_lv34_weekday_pct_ = new int[] { 80, 80, 80, 80, 72, 85, 90, 90, 85, 80, 90, 80, 80, 90, 80, 100, 80, 80, 80, 80 };
        int[] school_lv34_avgday_pct_ = new int[] { 70, 70, 70, 70, 63, 70, 80, 80, 80, 70, 85, 70, 70, 80, 70, 100, 70, 70, 70, 70 };
        int[] school_lv34_saturday_pct_ = new int[] { 5, 5, 5, 5, 5, 10, 5, 5, 20, 10, 10, 10, 5, 15, 20, 0, 5, 5, 5, 5 };
        int[] school_lv34_sunday_pct_ = new int[] { 0, 0, 0, 0, 0, 4, 0, 0, 0, 0, 0, 0, 0, 5, 10, 0, 0, 0, 0, 0 };
        int[] school_vacation_month1_ = new int[] { 7, 7, 1, 12, 1, 7, 7, 7, 5, 7, 8, 7, 7, 4, 6, 6, 12, 7, 7, 7 };
        int[] school_vacation_month2_ = new int[] { 8, 8, 7, 1, 7, 8, 8, 8, 6, 8, 3, 12, 8, 5, 7, 12, 7, 8, 8, 8 };
        float[] meals_weekday_ = new float[] { 1.15f, 1.15f, 1.25f, 1.16f, 1.21f, 1.25f, 1.27f, 1.17f, 1.18f, 1.30f, 1.14f, 1.25f, 1.17f, 1.21f, 1.16f, 1.20f, 1.17f, 1.35f, 1.18f, 1.15f };
        float[] meals_avgday_ = new float[] { 1.20f, 1.20f, 1.35f, 1.22f, 1.26f, 1.39f, 1.32f, 1.22f, 1.25f, 1.45f, 1.20f, 1.35f, 1.22f, 1.26f, 1.21f, 1.25f, 1.23f, 1.50f, 1.24f, 1.20f };
        float[] meals_saturday_ = new float[] { 1.32f, 1.32f, 1.60f, 1.36f, 1.43f, 1.67f, 1.45f, 1.36f, 1.42f, 1.60f, 1.30f, 1.60f, 1.35f, 1.41f, 1.36f, 1.40f, 1.38f, 1.70f, 1.39f, 1.32f };
        float[] meals_sunday_ = new float[] { 1.32f, 1.32f, 1.55f, 1.35f, 1.45f, 1.75f, 1.48f, 1.36f, 1.40f, 1.55f, 1.30f, 1.55f, 1.35f, 1.41f, 1.36f, 1.40f, 1.37f, 1.65f, 1.39f, 1.32f };

        float[] entertainment_weekday_ = new float[] { 0.61f, 0.61f, 0.80f, 0.64f, 0.64f, 0.93f, 0.63f, 0.63f, 0.62f, 0.70f, 0.58f, 0.80f, 0.63f, 0.64f, 0.62f, 0.64f, 0.62f, 0.85f, 0.65f, 0.61f };
        float[] entertainment_avgday_ = new float[] { 0.76f, 0.76f, 1.00f, 0.80f, 0.80f, 1.11f, 0.78f, 0.78f, 0.78f, 0.85f, 0.72f, 1.00f, 0.78f, 0.80f, 0.78f, 0.80f, 0.78f, 1.05f, 0.81f, 0.76f };
        float[] entertainment_saturday_ = new float[] { 1.15f, 1.15f, 1.45f, 1.24f, 1.27f, 1.48f, 1.21f, 1.21f, 1.20f, 1.30f, 1.05f, 1.40f, 1.22f, 1.24f, 1.20f, 1.21f, 1.18f, 1.50f, 1.24f, 1.15f };
        float[] entertainment_sunday_ = new float[] { 1.15f, 1.15f, 1.35f, 1.20f, 1.27f, 1.48f, 1.21f, 1.21f, 1.18f, 1.25f, 1.00f, 1.30f, 1.22f, 1.24f, 1.20f, 1.21f, 1.15f, 1.45f, 1.24f, 1.15f };

        float[] shopping_weekday_ = new float[] { 0.24f, 0.24f, 0.27f, 0.24f, 0.24f, 0.30f, 0.24f, 0.25f, 0.27f, 0.24f, 0.26f, 0.28f, 0.25f, 0.26f, 0.25f, 0.27f, 0.25f, 0.25f, 0.25f, 0.24f };
        float[] shopping_avgday_ = new float[] { 1.41f, 1.41f, 1.48f, 1.40f, 1.41f, 0.85f, 1.44f, 1.38f, 1.55f, 1.40f, 1.45f, 1.50f, 1.45f, 1.55f, 1.38f, 1.58f, 1.40f, 1.45f, 1.44f, 1.41f };
        float[] shopping_saturday_ = new float[] { 1.68f, 1.68f, 1.85f, 1.76f, 1.81f, 1.00f, 1.76f, 1.85f, 1.90f, 1.75f, 1.75f, 1.90f, 1.76f, 1.93f, 1.93f, 1.85f, 1.75f, 1.80f, 1.85f, 1.68f };
        float[] shopping_sunday_ = new float[] { 0.53f, 0.53f, 0.75f, 0.50f, 0.54f, 0.70f, 0.48f, 0.37f, 1.00f, 0.45f, 1.10f, 0.75f, 0.56f, 0.61f, 0.34f, 0.58f, 0.65f, 0.50f, 0.48f, 0.53f };

        float[] park_weekday_ = new float[] { 0.30f, 0.30f, 0.31f, 0.32f, 0.32f, 0.31f, 0.32f, 0.31f, 0.30f, 0.32f, 0.30f, 0.31f, 0.32f, 0.29f, 0.31f, 0.32f, 0.31f, 0.32f, 0.31f, 0.30f };
        float[] park_avgday_ = new float[] { 0.31f, 0.31f, 0.35f, 0.34f, 0.33f, 0.38f, 0.33f, 0.32f, 0.32f, 0.34f, 0.32f, 0.35f, 0.33f, 0.29f, 0.32f, 0.33f, 0.33f, 0.36f, 0.32f, 0.31f };
        float[] park_saturday_ = new float[] { 0.35f, 0.35f, 0.50f, 0.40f, 0.39f, 0.52f, 0.37f, 0.38f, 0.38f, 0.40f, 0.35f, 0.48f, 0.39f, 0.32f, 0.37f, 0.38f, 0.40f, 0.45f, 0.37f, 0.35f };
        float[] park_sunday_ = new float[] { 0.35f, 0.35f, 0.52f, 0.38f, 0.39f, 0.52f, 0.37f, 0.38f, 0.38f, 0.40f, 0.35f, 0.50f, 0.39f, 0.32f, 0.37f, 0.38f, 0.40f, 0.45f, 0.37f, 0.35f };

        float[] travel_weekday_ = new float[] { 0.04f, 0.04f, 0.045f, 0.04f, 0.035f, 0.04f, 0.045f, 0.045f, 0.045f, 0.04f, 0.035f, 0.045f, 0.045f, 0.035f, 0.035f, 0.05f, 0.045f, 0.04f, 0.045f, 0.04f };
        float[] travel_avgday_ = new float[] { 0.04f, 0.04f, 0.045f, 0.04f, 0.035f, 0.04f, 0.045f, 0.045f, 0.045f, 0.04f, 0.035f, 0.045f, 0.045f, 0.035f, 0.035f, 0.05f, 0.045f, 0.04f, 0.045f, 0.04f };
        float[] travel_saturday_ = new float[] { 0.05f, 0.05f, 0.06f, 0.055f, 0.045f, 0.055f, 0.06f, 0.06f, 0.065f, 0.055f, 0.045f, 0.06f, 0.06f, 0.045f, 0.045f, 0.065f, 0.06f, 0.055f, 0.06f, 0.05f };
        float[] travel_sunday_ = new float[] { 0.05f, 0.05f, 0.055f, 0.05f, 0.045f, 0.055f, 0.06f, 0.06f, 0.06f, 0.05f, 0.045f, 0.055f, 0.06f, 0.045f, 0.045f, 0.065f, 0.055f, 0.05f, 0.06f, 0.05f };

        int[] traffic_reduction_ = new int[] { 5, 5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        public Setting(IMod mod) : base(mod)
        {

            int i = 0;
            foreach (var value in Enum.GetValues(typeof(SettingsEnum)))
            {
                SettingsEnum e = (SettingsEnum)value;
                countryIndexLookup.Add((int)e, i);
                i++;
            }

            if (evening_share == 0) SetDefaults();
        }

        public override void SetDefaults()
        {
            SetParameters(0);
        }
        public void SetParameters(int index)
        {
            evening_share = evening_share_[index];
            night_share = night_share_[index];
            delay_factor = delay_factor_[index];
            lunch_break_percentage = lunch_break_percentage_[index];
            holidays_per_year = holidays_per_year_[index];
            vacation_per_year = vacation_per_year_[index];
            disable_early_shop_leisure = disable_early_shop_leisure_[index];
            use_vanilla_timeoff = use_vanilla_timeoff_[index];
            use_school_vanilla_timeoff = use_school_vanilla_timeoff_[index];
            school_start_time = (timeEnum)school_start_time_[index];
            school_end_time = (timeEnum)school_end_time_[index];
            high_school_start_time = (timeEnum)high_school_start_time_[index];
            high_school_end_time = (timeEnum)high_school_end_time_[index];
            univ_start_time = (timeEnum)univ_start_time_[index];
            univ_end_time = (timeEnum)univ_end_time_[index];
            work_start_time = (timeEnum)work_start_time_[index];
            work_end_time = (timeEnum)work_end_time_[index];
            avg_work_hours_ft_wd = avg_work_hours_ft_wd_[index];
            avg_work_hours_pt_wd = avg_work_hours_pt_wd_[index];
            dt_simulation = (DTSimulationEnum)dt_simulation_[index];
            slow_time_factor = slow_time_factor_[index];
            part_time_percentage = part_time_percentage_[index];
            remote_percentage = remote_percentage_[index];
            //better_trucks = better_trucks_[index];
            peak_spread = peak_spread_[index];
            tourism_trips = tourism_trips_[index];
            commuter_trips = commuter_trips_[index];
            service_expenses_night_reduction = service_expenses_night_reduction_[index];
            office_weekday_pct = office_weekday_pct_[index];
            office_avgday_pct = office_avgday_pct_[index];
            office_sat_pct = office_sat_pct_[index];
            office_sun_pct = office_sun_pct_[index];
            commercial_weekday_pct = commercial_weekday_pct_[index];
            commercial_avgday_pct = commercial_avgday_pct_[index];
            commercial_sat_pct = commercial_sat_pct_[index];
            commercial_sun_pct = commercial_sun_pct_[index];
            industry_weekday_pct = industry_weekday_pct_[index];
            industry_avgday_pct = industry_avgday_pct_[index];
            industry_sat_pct = industry_sat_pct_[index];
            industry_sun_pct = industry_sun_pct_[index];
            cityServices_weekday_pct = cityServices_weekday_pct_[index];
            cityServices_avgday_pct = cityServices_avgday_pct_[index];
            cityServices_sat_pct = cityServices_sat_pct_[index];
            cityServices_sun_pct = cityServices_sun_pct_[index];
            nonday_office_share = nonday_office_share_[index];
            nonday_commercial_share = nonday_commercial_share_[index];
            nonday_industry_share = nonday_industry_share_[index];
            nonday_cityservices_share = nonday_cityservices_share_[index];
            school_lv1_weekday_pct = school_lv1_weekday_pct_[index];
            school_lv1_avgday_pct = school_lv1_avgday_pct_[index];
            school_lv1_saturday_pct = school_lv1_saturday_pct_[index];
            school_lv1_sunday_pct = school_lv1_sunday_pct_[index];
            school_lv2_weekday_pct = school_lv2_weekday_pct_[index];
            school_lv2_avgday_pct = school_lv2_avgday_pct_[index];
            school_lv2_saturday_pct = school_lv2_saturday_pct_[index];
            school_lv2_sunday_pct = school_lv2_sunday_pct_[index];
            school_lv34_weekday_pct = school_lv34_weekday_pct_[index];
            school_lv34_avgday_pct = school_lv34_avgday_pct_[index];
            school_lv34_saturday_pct = school_lv34_saturday_pct_[index];
            school_lv34_sunday_pct = school_lv34_sunday_pct_[index];
            school_vacation_month1 = (months)school_vacation_month1_[index];
            school_vacation_month2 = (months)school_vacation_month2_[index];
            meals_weekday = meals_weekday_[index];
            meals_avgday = meals_avgday_[index];
            meals_saturday = meals_saturday_[index];
            meals_sunday = meals_sunday_[index];
            entertainment_weekday = entertainment_weekday_[index];
            entertainment_avgday = entertainment_avgday_[index];
            entertainment_saturday = entertainment_saturday_[index];
            entertainment_sunday = entertainment_sunday_[index];
            shopping_weekday = shopping_weekday_[index];
            shopping_avgday = shopping_avgday_[index];
            shopping_saturday = shopping_saturday_[index];
            shopping_sunday = shopping_sunday_[index];
            park_weekday = park_weekday_[index];
            park_avgday = park_avgday_[index];
            park_saturday = park_saturday_[index];
            park_sunday = park_sunday_[index];
            travel_weekday = travel_weekday_[index];
            travel_avgday = travel_avgday_[index];
            travel_saturday = travel_saturday_[index];
            travel_sunday = travel_sunday_[index];
            trafficReduction = traffic_reduction_[index];
            resourceConsumption = 20;
            hospital_stay_duration_enabled = false;
            hospital_stay_inpatient_chance_pct = 15;
            hospital_short_stay_average_hours = 4;
            hospital_short_stay_stddev_hours = 2;
            hospital_short_stay_minimum_hours = 2;
            hospital_short_stay_maximum_hours = 12;
            hospital_inpatient_average_hours = 72;
            hospital_inpatient_stddev_hours = 24;
            hospital_inpatient_minimum_hours = 12;
            hospital_inpatient_maximum_hours = 168;
        }

        public override void Apply()
        {
            base.Apply();

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
                return;

            var system1 = world.GetExistingSystemManaged<WorkPlaceShiftUpdateSystem>();
            if (system1 != null)
                system1.Enabled = true;

            var system2 = world.GetExistingSystemManaged<WorkerShiftUpdateSystem>();
            if (system2 != null)
                system2.Enabled = true;

            var timeSettingsMultiplierSystem = world.GetExistingSystemManaged<Time2Work.Systems.TimeSettingsMultiplierSystem>();
            if (timeSettingsMultiplierSystem != null)
                timeSettingsMultiplierSystem.Enabled = true;

            var healthEventProbabilityScalerSystem = world.GetExistingSystemManaged<Time2Work.Systems.HealthEventProbabilityScalerSystem>();
            if (healthEventProbabilityScalerSystem != null)
                healthEventProbabilityScalerSystem.Enabled = true;
        }

        [SettingsUISection(SettingsSection, SettingsGroup)]
        public SettingsEnum settings_choice { get; set; } = SettingsEnum.Balanced;

        [SettingsUIButton]
        [SettingsUISection(SettingsSection, SettingsGroup)]
        public bool Button
        {
            set
            {
                countryIndexLookup.TryGetValue((int)settings_choice, out int selectedSetting);
                SetParameters(selectedSetting);
            }

        }

        //[SettingsUISection(SettingsSection, SettingsGroup)]
        //[SettingsUIMultilineText]
        //public string MultilineText => string.Empty;

        public float average_commute { get; set; } = 0f;
        public float commute_top10per { get; set; } = 0f;

        [SettingsUISlider(min = 1, max = 25, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(WorkSection, WorkPlaceShiftGroup)]
        public int evening_share { get; set; }

        [SettingsUISlider(min = 1, max = 50, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(WorkSection, NonDayShiftByWorkTypeGroup)]
        public int nonday_office_share { get; set; }

        [SettingsUISlider(min = 1, max = 50, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(WorkSection, NonDayShiftByWorkTypeGroup)]
        public int nonday_commercial_share { get; set; }

        [SettingsUISlider(min = 1, max = 50, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(WorkSection, NonDayShiftByWorkTypeGroup)]
        public int nonday_industry_share { get; set; }

        [SettingsUISlider(min = 1, max = 50, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(WorkSection, NonDayShiftByWorkTypeGroup)]
        public int nonday_cityservices_share { get; set; }

        [SettingsUISlider(min = 1, max = 25, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(WorkSection, WorkPlaceShiftGroup)]
        public int night_share { get; set; }

        [SettingsUISlider(min = 0, max = 10, step = 0.5f, scalarMultiplier = 1, unit = Unit.kFloatSingleFraction)]
        [SettingsUISection(WorkSection, DelayGroup)]
        public float delay_factor { get; set; }

        [SettingsUISection(WorkSection, DelayGroup)]
        public bool peak_spread { get; set; }

        [SettingsUISlider(min = 0, max = 50, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(WorkSection, DayShiftGroup)]
        public int lunch_break_percentage { get; set; }

        [SettingsUISlider(min = 0, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(WorkSection, RemoteGroup)]
        public int remote_percentage { get; set; }

        [SettingsUISection(ShopLeisureSection, ShopLeisureGroup)]
        public bool disable_early_shop_leisure { get; set; }


        [SettingsUISection(ShopLeisureSection, TimeOffGroup)]
        public bool use_vanilla_timeoff { get; set; }

        [SettingsUISlider(min = 0, max = 30, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(ShopLeisureSection, TimeOffGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(use_vanilla_timeoff))]
        public float holidays_per_year { get; set; }

        [SettingsUISlider(min = 0, max = 60, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(ShopLeisureSection, TimeOffGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(use_vanilla_timeoff))]
        public float vacation_per_year { get; set; }

        [SettingsUISlider(min = 0, max = 3, step = 0.01f, scalarMultiplier = 1, unit = Unit.kFloatTwoFractions)]
        [SettingsUISection(ShopLeisureSection, LeisureMealsGroup)]
        public float meals_weekday { get; set; }

        [SettingsUISlider(min = 0, max = 3, step = 0.01f, scalarMultiplier = 1, unit = Unit.kFloatTwoFractions)]
        [SettingsUISection(ShopLeisureSection, LeisureMealsGroup)]
        public float meals_avgday { get; set; }

        [SettingsUISlider(min = 0, max = 3, step = 0.01f, scalarMultiplier = 1, unit = Unit.kFloatTwoFractions)]
        [SettingsUISection(ShopLeisureSection, LeisureMealsGroup)]
        public float meals_saturday { get; set; }

        [SettingsUISlider(min = 0, max = 3, step = 0.01f, scalarMultiplier = 1, unit = Unit.kFloatTwoFractions)]
        [SettingsUISection(ShopLeisureSection, LeisureMealsGroup)]
        public float meals_sunday { get; set; }

        [SettingsUISlider(min = 0, max = 3, step = 0.01f, scalarMultiplier = 1, unit = Unit.kFloatTwoFractions)]
        [SettingsUISection(ShopLeisureSection, LeisureEntertainmentGroup)]
        public float entertainment_weekday { get; set; }

        [SettingsUISlider(min = 0, max = 3, step = 0.01f, scalarMultiplier = 1, unit = Unit.kFloatTwoFractions)]
        [SettingsUISection(ShopLeisureSection, LeisureEntertainmentGroup)]
        public float entertainment_avgday { get; set; }

        [SettingsUISlider(min = 0, max = 3, step = 0.01f, scalarMultiplier = 1, unit = Unit.kFloatTwoFractions)]
        [SettingsUISection(ShopLeisureSection, LeisureEntertainmentGroup)]
        public float entertainment_saturday { get; set; }

        [SettingsUISlider(min = 0, max = 3, step = 0.01f, scalarMultiplier = 1, unit = Unit.kFloatTwoFractions)]
        [SettingsUISection(ShopLeisureSection, LeisureEntertainmentGroup)]
        public float entertainment_sunday { get; set; }

        [SettingsUISlider(min = 0, max = 3, step = 0.01f, scalarMultiplier = 1, unit = Unit.kFloatTwoFractions)]
        [SettingsUISection(ShopLeisureSection, LeisureShoppingGroup)]
        public float shopping_weekday { get; set; }

        [SettingsUISlider(min = 0, max = 3, step = 0.01f, scalarMultiplier = 1, unit = Unit.kFloatTwoFractions)]
        [SettingsUISection(ShopLeisureSection, LeisureShoppingGroup)]
        public float shopping_avgday { get; set; }

        [SettingsUISlider(min = 0, max = 3, step = 0.01f, scalarMultiplier = 1, unit = Unit.kFloatTwoFractions)]
        [SettingsUISection(ShopLeisureSection, LeisureShoppingGroup)]
        public float shopping_saturday { get; set; }

        [SettingsUISlider(min = 0, max = 3, step = 0.01f, scalarMultiplier = 1, unit = Unit.kFloatTwoFractions)]
        [SettingsUISection(ShopLeisureSection, LeisureShoppingGroup)]
        public float shopping_sunday { get; set; }

        [SettingsUISection(ShopLeisureSection, ShoppingTripGateGroup)]
        public bool shopping_trip_gates_enabled { get; set; } = true;

        [SettingsUISlider(min = 0, max = 95, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(ShopLeisureSection, ShoppingTripGateGroup)]
        public int shopping_gate_meals_pct { get; set; } = 5;

        [SettingsUISlider(min = 0, max = 95, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(ShopLeisureSection, ShoppingTripGateGroup)]
        public int shopping_gate_groceries_pct { get; set; } = 18;

        [SettingsUISlider(min = 0, max = 95, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(ShopLeisureSection, ShoppingTripGateGroup)]
        public int shopping_gate_health_fuel_pct { get; set; } = 15;

        [SettingsUISlider(min = 0, max = 95, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(ShopLeisureSection, ShoppingTripGateGroup)]
        public int shopping_gate_household_goods_pct { get; set; } = 35;

        [SettingsUISlider(min = 0, max = 95, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(ShopLeisureSection, ShoppingTripGateGroup)]
        public int shopping_gate_consumer_goods_pct { get; set; } = 45;

        [SettingsUISlider(min = 0, max = 95, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(ShopLeisureSection, ShoppingTripGateGroup)]
        public int shopping_gate_large_purchases_pct { get; set; } = 65;

        [SettingsUISection(ShopLeisureSection, ShoppingCooldownGroup)]
        public bool household_shopping_cooldown_enabled { get; set; } = true;

        [SettingsUISlider(min = 0, max = 95, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(ShopLeisureSection, ShoppingCooldownGroup)]
        public int household_cooldown_groceries_pct { get; set; } = 75;

        [SettingsUISlider(min = 0, max = 95, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(ShopLeisureSection, ShoppingCooldownGroup)]
        public int household_cooldown_health_fuel_pct { get; set; } = 40;

        [SettingsUISlider(min = 0, max = 95, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(ShopLeisureSection, ShoppingCooldownGroup)]
        public int household_cooldown_household_goods_pct { get; set; } = 85;

        [SettingsUISlider(min = 0, max = 95, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(ShopLeisureSection, ShoppingCooldownGroup)]
        public int household_cooldown_large_purchases_pct { get; set; } = 90;

        [SettingsUISlider(min = 0, max = 95, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(ShopLeisureSection, ShoppingCooldownGroup)]
        public int household_cooldown_other_pct { get; set; } = 80;

        [SettingsUISlider(min = 0.25f, max = 8f, step = 0.25f, scalarMultiplier = 1, unit = Unit.kFloatSingleFraction)]
        [SettingsUISection(ShopLeisureSection, ShoppingCooldownGroup)]
        public float household_cooldown_regular_hours { get; set; } = 2.5f;

        [SettingsUISlider(min = 0.25f, max = 12f, step = 0.25f, scalarMultiplier = 1, unit = Unit.kFloatSingleFraction)]
        [SettingsUISection(ShopLeisureSection, ShoppingCooldownGroup)]
        public float household_cooldown_large_purchase_hours { get; set; } = 4f;

        [SettingsUISlider(min = 0, max = 3, step = 0.01f, scalarMultiplier = 1, unit = Unit.kFloatTwoFractions)]
        [SettingsUISection(ShopLeisureSection, LeisureParksGroup)]
        public float park_weekday { get; set; }

        [SettingsUISlider(min = 0, max = 3, step = 0.01f, scalarMultiplier = 1, unit = Unit.kFloatTwoFractions)]
        [SettingsUISection(ShopLeisureSection, LeisureParksGroup)]
        public float park_avgday { get; set; }

        [SettingsUISlider(min = 0, max = 3, step = 0.01f, scalarMultiplier = 1, unit = Unit.kFloatTwoFractions)]
        [SettingsUISection(ShopLeisureSection, LeisureParksGroup)]
        public float park_saturday { get; set; }

        [SettingsUISlider(min = 0, max = 3, step = 0.01f, scalarMultiplier = 1, unit = Unit.kFloatTwoFractions)]
        [SettingsUISection(ShopLeisureSection, LeisureParksGroup)]
        public float park_sunday { get; set; }

        [SettingsUISlider(min = 0, max = 3, step = 0.01f, scalarMultiplier = 1, unit = Unit.kFloatTwoFractions)]
        [SettingsUISection(ShopLeisureSection, LeisureTravelGroup)]
        public float travel_weekday { get; set; }

        [SettingsUISlider(min = 0, max = 3, step = 0.01f, scalarMultiplier = 1, unit = Unit.kFloatTwoFractions)]
        [SettingsUISection(ShopLeisureSection, LeisureTravelGroup)]
        public float travel_avgday { get; set; }

        [SettingsUISlider(min = 0, max = 3, step = 0.01f, scalarMultiplier = 1, unit = Unit.kFloatTwoFractions)]
        [SettingsUISection(ShopLeisureSection, LeisureTravelGroup)]
        public float travel_saturday { get; set; }

        [SettingsUISlider(min = 0, max = 3, step = 0.01f, scalarMultiplier = 1, unit = Unit.kFloatTwoFractions)]
        [SettingsUISection(ShopLeisureSection, LeisureTravelGroup)]
        public float travel_sunday { get; set; }

        [SettingsUISlider(min = 5, max = 180, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(ShopLeisureSection, ShoppingTimeGroup)]
        public int avg_time_beverages { get; set; } = 15;

        [SettingsUISlider(min = 5, max = 180, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(ShopLeisureSection, ShoppingTimeGroup)]
        public int avg_time_chemicals { get; set; } = 20;

        [SettingsUISlider(min = 5, max = 180, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(ShopLeisureSection, ShoppingTimeGroup)]
        public int avg_time_convenienceFood { get; set; } = 5;

        [SettingsUISlider(min = 5, max = 180, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(ShopLeisureSection, ShoppingTimeGroup)]
        public int avg_time_electronics { get; set; } = 45;

        [SettingsUISlider(min = 5, max = 180, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(ShopLeisureSection, ShoppingTimeGroup)]
        public int avg_time_software { get; set; } = 30;

        [SettingsUISlider(min = 5, max = 180, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(ShopLeisureSection, ShoppingTimeGroup)]
        public int avg_time_financial { get; set; } = 25;

        [SettingsUISlider(min = 5, max = 180, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(ShopLeisureSection, ShoppingTimeGroup)]
        public int avg_time_food { get; set; } = 40;

        [SettingsUISlider(min = 5, max = 180, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(ShopLeisureSection, ShoppingTimeGroup)]
        public int avg_time_furniture { get; set; } = 90;

        [SettingsUISlider(min = 5, max = 180, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(ShopLeisureSection, ShoppingTimeGroup)]
        public int avg_time_meals { get; set; } = 60;

        [SettingsUISlider(min = 5, max = 180, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(ShopLeisureSection, ShoppingTimeGroup)]
        public int avg_time_media { get; set; } = 20;

        [SettingsUISlider(min = 5, max = 180, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(ShopLeisureSection, ShoppingTimeGroup)]
        public int avg_time_paper { get; set; } = 15;

        [SettingsUISlider(min = 5, max = 180, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(ShopLeisureSection, ShoppingTimeGroup)]
        public int avg_time_petrochemicals { get; set; } = 5;

        [SettingsUISlider(min = 5, max = 180, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(ShopLeisureSection, ShoppingTimeGroup)]
        public int avg_time_pharmaceuticals { get; set; } = 10;

        [SettingsUISlider(min = 5, max = 180, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(ShopLeisureSection, ShoppingTimeGroup)]
        public int avg_time_plastics { get; set; } = 20;

        [SettingsUISlider(min = 5, max = 180, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(ShopLeisureSection, ShoppingTimeGroup)]
        public int avg_time_telecom { get; set; } = 30;

        [SettingsUISlider(min = 5, max = 180, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(ShopLeisureSection, ShoppingTimeGroup)]
        public int avg_time_textiles { get; set; } = 30;

        [SettingsUISlider(min = 5, max = 180, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(ShopLeisureSection, ShoppingTimeGroup)]
        public int avg_time_recreation { get; set; } = 120;

        [SettingsUISlider(min = 5, max = 180, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(ShopLeisureSection, ShoppingTimeGroup)]
        public int avg_time_entertainment { get; set; } = 90;

        [SettingsUISlider(min = 5, max = 180, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(ShopLeisureSection, ShoppingTimeGroup)]
        public int avg_time_vehicles { get; set; } = 120;

        [SettingsUISection(HealthSection, HospitalStayGroup)]
        public bool hospital_stay_duration_enabled { get; set; } = false;

        [SettingsUISlider(min = 0, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(HealthSection, HospitalStayGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(HideHospitalStayDurationSettings))]
        public int hospital_stay_inpatient_chance_pct { get; set; } = 15;

        [SettingsUISlider(min = 2, max = 24, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(HealthSection, HospitalStayGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(HideHospitalStayDurationSettings))]
        public int hospital_short_stay_average_hours { get; set; } = 4;

        [SettingsUISlider(min = 0, max = 12, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(HealthSection, HospitalStayGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(HideHospitalStayDurationSettings))]
        public int hospital_short_stay_stddev_hours { get; set; } = 2;

        [SettingsUISlider(min = 2, max = 12, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(HealthSection, HospitalStayGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(HideHospitalStayDurationSettings))]
        public int hospital_short_stay_minimum_hours { get; set; } = 2;

        [SettingsUISlider(min = 2, max = 48, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(HealthSection, HospitalStayGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(HideHospitalStayDurationSettings))]
        public int hospital_short_stay_maximum_hours { get; set; } = 12;

        [SettingsUISlider(min = 12, max = 168, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(HealthSection, HospitalStayGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(HideHospitalStayDurationSettings))]
        public int hospital_inpatient_average_hours { get; set; } = 72;

        [SettingsUISlider(min = 0, max = 72, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(HealthSection, HospitalStayGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(HideHospitalStayDurationSettings))]
        public int hospital_inpatient_stddev_hours { get; set; } = 24;

        [SettingsUISlider(min = 2, max = 72, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(HealthSection, HospitalStayGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(HideHospitalStayDurationSettings))]
        public int hospital_inpatient_minimum_hours { get; set; } = 12;

        [SettingsUISlider(min = 12, max = 336, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(HealthSection, HospitalStayGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(HideHospitalStayDurationSettings))]
        public int hospital_inpatient_maximum_hours { get; set; } = 168;

        public bool HideHospitalStayDurationSettings
        {
            get { return !hospital_stay_duration_enabled; }
        }

        //[SettingsUISlider(min = 400, max = 1600, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        //[SettingsUISection(OtherSection, VisitTimeGroup)]
        //public int avg_time_prison { get; set; } = 12*60;


        [SettingsUISection(SchoolSection, SchoolTimeOffGroup)]
        public bool use_school_vanilla_timeoff { get; set; }

        [SettingsUISection(SchoolSection, SchoolTimeOffGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(use_school_vanilla_timeoff))]
        //public float school_vacation_per_year { get; set; }
        public months school_vacation_month1 { get; set; } = months.July;

        [SettingsUISection(SchoolSection, SchoolTimeOffGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(use_school_vanilla_timeoff))]
        public months school_vacation_month2 { get; set; } = months.January;

        [SettingsUISection(SchoolSection, SchoolTimeGroup)]
        public timeEnum school_start_time { get; set; } = timeEnum.t900;

        [SettingsUISection(SchoolSection, SchoolTimeGroup)]
        public timeEnum school_end_time { get; set; } = timeEnum.t1700;

        [SettingsUISection(SchoolSection, SchoolTimeGroup)]
        public timeEnum high_school_start_time { get; set; } = timeEnum.t900;

        [SettingsUISection(SchoolSection, SchoolTimeGroup)]
        public timeEnum high_school_end_time { get; set; } = timeEnum.t1700;

        [SettingsUISection(SchoolSection, SchoolTimeGroup)]
        public timeEnum univ_start_time { get; set; } = timeEnum.t900;

        [SettingsUISection(SchoolSection, SchoolTimeGroup)]
        public timeEnum univ_end_time { get; set; } = timeEnum.t1700;

        [SettingsUISlider(min = 1, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(SchoolSection, School1WeekGroup)]
        public int school_lv1_weekday_pct { get; set; }

        [SettingsUISlider(min = 1, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(SchoolSection, School1WeekGroup)]
        public int school_lv1_avgday_pct { get; set; }

        [SettingsUISlider(min = 1, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(SchoolSection, School1WeekGroup)]
        public int school_lv1_saturday_pct { get; set; }

        [SettingsUISlider(min = 1, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(SchoolSection, School1WeekGroup)]
        public int school_lv1_sunday_pct { get; set; }

        [SettingsUISlider(min = 1, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(SchoolSection, School2WeekGroup)]
        public int school_lv2_weekday_pct { get; set; }

        [SettingsUISlider(min = 1, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(SchoolSection, School2WeekGroup)]
        public int school_lv2_avgday_pct { get; set; }

        [SettingsUISlider(min = 1, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(SchoolSection, School2WeekGroup)]
        public int school_lv2_saturday_pct { get; set; }

        [SettingsUISlider(min = 1, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(SchoolSection, School2WeekGroup)]
        public int school_lv2_sunday_pct { get; set; }

        [SettingsUISlider(min = 1, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(SchoolSection, School34WeekGroup)]
        public int school_lv34_weekday_pct { get; set; }

        [SettingsUISlider(min = 1, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(SchoolSection, School34WeekGroup)]
        public int school_lv34_avgday_pct { get; set; }

        [SettingsUISlider(min = 1, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(SchoolSection, School34WeekGroup)]
        public int school_lv34_saturday_pct { get; set; }

        [SettingsUISlider(min = 1, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(SchoolSection, School34WeekGroup)]
        public int school_lv34_sunday_pct { get; set; }

        [SettingsUISection(WorkSection, DayShiftGroup)]
        public timeEnum work_start_time { get; set; } = timeEnum.t900;

        [SettingsUISection(WorkSection, DayShiftGroup)]
        public timeEnum work_end_time { get; set; } = timeEnum.t1700;

        [SettingsUISlider(min = 4, max = 10, step = 1, scalarMultiplier = 1, unit = Unit.kFloatSingleFraction)]
        [SettingsUISection(WorkSection, DayShiftGroup)]
        public float avg_work_hours_ft_wd { get; set; }

        //[SettingsUISlider(min = 4, max = 10, step = 1, scalarMultiplier = 1, unit = Unit.kFloatSingleFraction)]
        //[SettingsUISection(WorkSection, DayShiftGroup)]
        //public float avg_work_hours_ft_we { get; set; }

        [SettingsUISlider(min = 0, max = 40, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(WorkSection, DayShiftGroup)]
        public int part_time_percentage { get; set; }

        [SettingsUISlider(min = 2, max = 6, step = 1, scalarMultiplier = 1, unit = Unit.kFloatSingleFraction)]
        [SettingsUISection(WorkSection, DayShiftGroup)]
        public float avg_work_hours_pt_wd { get; set; }

        //[SettingsUISlider(min = 2, max = 6, step = 1, scalarMultiplier = 1, unit = Unit.kFloatSingleFraction)]
        //[SettingsUISection(WorkSection, DayShiftGroup)]
        //public float avg_work_hours_pt_we { get; set; }

        [SettingsUISection(Weeksection, SlowerTimeGroup)]
        [SettingsUIMultilineText]
        public string DTText => string.Empty;

        [SettingsUISection(Weeksection, SlowerTimeGroup)]
        public DTSimulationEnum dt_simulation { get; set; } = DTSimulationEnum.AverageDay;

        [SettingsUISection(Weeksection, SlowerTimeGroup)]
        [SettingsUIMultilineText]
        public string MultilineText => string.Empty;

        [SettingsUISlider(min = 1f, max = 10, step = 0.1f, scalarMultiplier = 1, unit = Unit.kFloatSingleFraction)]
        [SettingsUISection(Weeksection, SlowerTimeGroup)]
        public float slow_time_factor { get; set; }

        [SettingsUISlider(min = 1, max = 30, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(Weeksection, SlowerTimeGroup)]
        public int daysPerMonth { get; set; } = 1;

        [SettingsUISection(Weeksection, SlowerTimeGroup)]
        public DateFormatEnum date_format { get; set; } = DateFormatEnum.DayOfWeek_Month_Year;

        //[SettingsUISection(Weeksection, WeekGroup)]
        //[SettingsUIMultilineText]
        //public string WeekText => string.Empty;

        [SettingsUISlider(min = 1, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(Weeksection, OfficeGroup)]
        public int office_weekday_pct { get; set; }

        [SettingsUISlider(min = 1, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(Weeksection, OfficeGroup)]
        public int office_avgday_pct { get; set; }

        [SettingsUISlider(min = 1, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(Weeksection, OfficeGroup)]
        public int office_sat_pct { get; set; }

        [SettingsUISlider(min = 1, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(Weeksection, OfficeGroup)]
        public int office_sun_pct { get; set; }

        [SettingsUISlider(min = 1, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(Weeksection, CommercialGroup)]
        public int commercial_weekday_pct { get; set; }

        [SettingsUISlider(min = 1, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(Weeksection, CommercialGroup)]
        public int commercial_avgday_pct { get; set; }

        [SettingsUISlider(min = 1, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(Weeksection, CommercialGroup)]
        public int commercial_sat_pct { get; set; }

        [SettingsUISlider(min = 1, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(Weeksection, CommercialGroup)]
        public int commercial_sun_pct { get; set; }

        [SettingsUISlider(min = 1, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(Weeksection, IndustryGroup)]
        public int industry_weekday_pct { get; set; }

        [SettingsUISlider(min = 1, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(Weeksection, IndustryGroup)]
        public int industry_avgday_pct { get; set; }

        [SettingsUISlider(min = 1, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(Weeksection, IndustryGroup)]
        public int industry_sat_pct { get; set; }

        [SettingsUISlider(min = 1, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(Weeksection, IndustryGroup)]
        public int industry_sun_pct { get; set; }

        [SettingsUISlider(min = 1, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(Weeksection, CityServicesGroup)]
        public int cityServices_weekday_pct { get; set; }

        [SettingsUISlider(min = 1, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(Weeksection, CityServicesGroup)]
        public int cityServices_avgday_pct { get; set; }

        [SettingsUISlider(min = 1, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(Weeksection, CityServicesGroup)]
        public int cityServices_sat_pct { get; set; }

        [SettingsUISlider(min = 1, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(Weeksection, CityServicesGroup)]
        public int cityServices_sun_pct { get; set; }

        //[SettingsUISection(OtherSection, TrucksGroup)]
        //public bool better_trucks { get; set; }

        [SettingsUISection(OtherSection, ExternalGroup)]
        public bool tourism_trips { get; set; }

        [SettingsUISection(OtherSection, ExternalGroup)]
        public bool commuter_trips { get; set; }

        [SettingsUISlider(min = 1, max = 50, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(OtherSection, ExpensesGroup)]
        public int service_expenses_night_reduction { get; set; }

        [SettingsUISlider(min = 0, max = 5, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(OtherSection, OtherGroup)]
        public int trafficReduction { get; set; }

        [SettingsUISlider(min = 1, max = 30, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(OtherSection, OtherGroup)]
        public int resourceConsumption { get; set; }

        [SettingsUISection(OtherSection, OtherGroup)]
        public bool shopping_log_enabled { get; set; } = false;

        [SettingsUISection(OtherSection, OtherGroup)]
        [SettingsUISetter(typeof(Setting), nameof(SetUseUniversalModMenu))]
        public bool use_universal_mod_menu { get; set; } = false;

        [SettingsUISlider(min = 1, max = 500, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(EventSection, EventGroup)]
        public int min_attraction { get; set; } = 25;

        [SettingsUISlider(min = 0, max = 3, step = 1f, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(EventSection, MinEventGroup)]
        public int min_event_weekday { get; set; } = 1;

        [SettingsUISlider(min = 0, max = 3, step = 1f, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(EventSection, MinEventGroup)]
        public int min_event_avg_day { get; set; } = 1;

        [SettingsUISlider(min = 0, max = 3, step = 1f, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(EventSection, MinEventGroup)]
        public int min_event_weekend { get; set; } = 1;

        [SettingsUISlider(min = 0, max = 3, step = 1f, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(EventSection, MaxEventGroup)]
        public int max_event_weekday { get; set; } = 1;

        [SettingsUISlider(min = 0, max = 3, step = 1f, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(EventSection, MaxEventGroup)]
        public int max_event_avg_day { get; set; } = 2;

        [SettingsUISlider(min = 0, max = 3, step = 1f, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(EventSection, MaxEventGroup)]
        public int max_event_weekend { get; set; } = 3;

        [SettingsUISlider(min = 0, max = 7, step = 1f, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(EventSection, HolidayGroup)]
        public int new_years_num_events { get; set; } = 5;
        public enum DTSimulationEnum
        {
            AverageDay,
            Weekday,
            Saturday,
            Sunday,
            sevendayweek
        }

        public enum DateFormatEnum
        {
            DayOfWeek_Month_Year,
            DayOfWeek_DDMMYYYY,
            DayOfWeek_MMDDYYYY
        }

        public enum SettingsEnum
        {
            Balanced = 0,
            Performance = 1,
            Argentina = 8,
            Australia = 10,
            Brazil = 25,
            Canada = 34,
            France = 62,
            Germany = 66,
            India = 79,
            Italy = 85,
            Japan = 87,
            Mexico = 112,
            Netherlands = 124,
            Phillipines = 140,
            Poland = 141,
            Singapore = 160,
            SouthAfrica = 163,
            Spain = 166,
            UK = 187,
            USA = 188
        }


        public enum timeEnum
        {
            t700,
            t730,
            t800,
            t830,
            t900,
            t930,
            t1000,
            t1030,
            t1100,
            t1130,
            t1200,
            t1230,
            t1300,
            t1330,
            t1400,
            t1430,
            t1500,
            t1530,
            t1600,
            t1630,
            t1700,
            t1730,
            t1800,
            t1830,
            t1900,
            t1930,
            t2000
        }

        public enum months
        {
            January = 1,
            February = 2,
            March = 3,
            April = 4,
            May = 5,
            June = 6,
            July = 7,
            August = 8,
            September = 9,
            October = 10,
            November = 11,
            December = 12
        }

        public enum dayOfWeek
        {
            Sunday,
            Monday,
            Tuesday,
            Wednesday,
            Thursday,
            Friday,
            Saturday
        }

        public void SetUseUniversalModMenu(bool value)
        {
            World.DefaultGameObjectInjectionWorld
                ?.GetExistingSystemManaged<Time2WorkUISystem>()
                ?.UpdateUseUniversalModMenu(value);
        }

        public class LocaleEN : IDictionarySource
        {
            private readonly Setting m_Setting;
            public LocaleEN(Setting setting)
            {
                m_Setting = setting;
            }
            public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
            {
                return new Dictionary<string, string>
            {
                { m_Setting.GetSettingsLocaleID(), "Realistic Trips" },
                { m_Setting.GetOptionTabLocaleID(Setting.SettingsSection), "Settings" },
                { m_Setting.GetOptionTabLocaleID(Setting.WorkSection), "Work" },
                { m_Setting.GetOptionTabLocaleID(Setting.ShopLeisureSection), "Shopping and Leisure" },
                { m_Setting.GetOptionTabLocaleID(Setting.HealthSection), "Health" },
                { m_Setting.GetOptionTabLocaleID(Setting.SchoolSection), "School" },
                { m_Setting.GetOptionTabLocaleID(Setting.Weeksection), "Week" },
                { m_Setting.GetOptionTabLocaleID(Setting.EventSection), "Special Events" },
                { m_Setting.GetOptionTabLocaleID(Setting.OtherSection), "Other" },

                { m_Setting.GetOptionGroupLocaleID(Setting.MinEventGroup), "Min Number of Events per Day of the Week" },
                { m_Setting.GetOptionGroupLocaleID(Setting.MaxEventGroup), "Max Number of Events per Day of the Week" },
                { m_Setting.GetOptionGroupLocaleID(Setting.WorkPlaceShiftGroup), "Modify the share of evening and night work shifts" },
                { m_Setting.GetOptionGroupLocaleID(Setting.NonDayShiftByWorkTypeGroup), "Modify the share of non-day shifts by work type" },
                { m_Setting.GetOptionGroupLocaleID(Setting.RemoteGroup), "Remote Work Settings" },
                { m_Setting.GetOptionGroupLocaleID(Setting.TimeOffGroup), "Vacation and Holiday Settings" },
                { m_Setting.GetOptionGroupLocaleID(Setting.DayShiftGroup), "Day Shift Settings" },
                { m_Setting.GetOptionGroupLocaleID(Setting.LeisureMealsGroup), "Meals: Avg. Hours for for Leisure per day" },
                { m_Setting.GetOptionGroupLocaleID(Setting.LeisureEntertainmentGroup), "Entertainment: Avg. Hours for for Leisure per day" },
                { m_Setting.GetOptionGroupLocaleID(Setting.LeisureShoppingGroup), "Shopping: Avg. Hours for for Leisure per day" },
                { m_Setting.GetOptionGroupLocaleID(Setting.LeisureParksGroup), "Parks: Avg. Hours for for Leisure per day" },
                { m_Setting.GetOptionGroupLocaleID(Setting.LeisureTravelGroup), "Travel: Avg. Hours for for Leisure per day" },
                { m_Setting.GetOptionGroupLocaleID(Setting.ShoppingTimeGroup), "Avg. Shopping Time (minutes) for each Resource" },
                { m_Setting.GetOptionGroupLocaleID(Setting.ShoppingTripGateGroup), "Shopping trip frequency gates" },
                { m_Setting.GetOptionGroupLocaleID(Setting.ShoppingCooldownGroup), "Household shopping cooldown" },
                { m_Setting.GetOptionGroupLocaleID(Setting.HospitalStayGroup), "Hospital encounters" },
                { m_Setting.GetOptionGroupLocaleID(Setting.SchoolTimeOffGroup), "School Vacation Settings" },
                { m_Setting.GetOptionGroupLocaleID(Setting.SchoolTimeGroup), "School Start/End Time Settings" },
                { m_Setting.GetOptionGroupLocaleID(Setting.School1WeekGroup), "Elementary School Attendance by Day" },
                { m_Setting.GetOptionGroupLocaleID(Setting.School2WeekGroup), "High School Attendance by Day" },
                { m_Setting.GetOptionGroupLocaleID(Setting.School34WeekGroup), "College and University Attendance by Day" },
                { m_Setting.GetOptionGroupLocaleID(Setting.SlowerTimeGroup), "Day and Time Settings" },
                { m_Setting.GetOptionGroupLocaleID(Setting.DTSimulationGroup), "Day Type Simulation" },
                { m_Setting.GetOptionGroupLocaleID(Setting.TrucksGroup), "Trucks" },
                { m_Setting.GetOptionGroupLocaleID(Setting.OtherGroup), "OTher" },
                { m_Setting.GetOptionGroupLocaleID(Setting.ExternalGroup), "External Trips" },
                { m_Setting.GetOptionGroupLocaleID(Setting.ExpensesGroup), "Service Expenses" },
                { m_Setting.GetOptionGroupLocaleID(Setting.WeekGroup), "Percentage of Workers per Day" },
                { m_Setting.GetOptionGroupLocaleID(Setting.OfficeGroup), "Office - Percentage of Workers per Day" },
                { m_Setting.GetOptionGroupLocaleID(Setting.CommercialGroup), "Commercial - Percentage of Workers per Day" },
                { m_Setting.GetOptionGroupLocaleID(Setting.IndustryGroup), "Industry - Percentage of Workers per Day" },
                { m_Setting.GetOptionGroupLocaleID(Setting.CityServicesGroup), "City Services - Percentage of Workers per Day" },
                { m_Setting.GetOptionGroupLocaleID(Setting.VisitTimeGroup), "Time Spent Visiting certain Buildings" },
                { m_Setting.GetOptionGroupLocaleID(Setting.HolidayGroup), "Holidays" },

                //{ m_Setting.GetOptionLabelLocaleID(nameof(Setting.WeekText)), $"Percentage of Workers per Day" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DTText)), $"Changing the parameters below require restarting the game." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.MultilineText)), $"WARNING: Changing the slow time factor for existing cities will cause cim's age to change which can cause issues in the game. If the factor is changed, the population age distribution will balance itself over time." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.settings_choice)), "Mod settings" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.settings_choice)), $"Change all mod settings. Performance: will update the settings to improve performance, this is similar to the Vanilla game. Balanced: has most of the features from this mod enabled, but a few of them that have high impact on performance are disabled. Country based settings: real world data was collected for a few countries, selecting one of them will make the game more realistic but it might impact performance." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.Button)), "Confirm" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.Button)), $"Confirm new settings" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.new_years_num_events)), "Number of New Years Events" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.new_years_num_events)), $"The number of events that will happen in the last day of the year. They all end at midnight." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.meals_weekday)), "Monday to Thursday" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.meals_weekday)), $"Average hours that a person spends going out for a meal per day." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.meals_avgday)), "Friday" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.meals_avgday)), $"Average hours that a person spends going out for a meal per day." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.meals_saturday)), "Saturday" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.meals_saturday)), $"Average hours that a person spends going out for a meal per day." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.meals_sunday)), "Sunday" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.meals_sunday)), $"Average hours that a person spends going out for a meal per day." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.entertainment_weekday)), "Monday to Thursday" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.entertainment_weekday)), $"Average hours that a person spends going out for entertainment per day." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.entertainment_avgday)), "Friday" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.entertainment_avgday)), $"Average hours that a person spends going out for entertainment per day." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.entertainment_saturday)), "Saturday" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.entertainment_saturday)), $"Average hours that a person spends going out for entertainment per day." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.entertainment_sunday)), "Sunday" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.entertainment_sunday)), $"Average hours that a person spends going out for entertainment per day." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.shopping_weekday)), "Monday to Thursday" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.shopping_weekday)), $"Average hours that a person spends going out for shopping per day." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.shopping_avgday)), "Friday" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.shopping_avgday)), $"Average hours that a person spends going out for shopping per day." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.shopping_saturday)), "Saturday" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.shopping_saturday)), $"Average hours that a person spends going out for shopping per day." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.shopping_sunday)), "Sunday" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.shopping_sunday)), $"Average hours that a person spends going out for shopping per day." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.shopping_trip_gates_enabled)), "Enable shopping trip gates" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.shopping_trip_gates_enabled)), $"When enabled, some low-need shopping errands are probabilistically skipped so households combine purchases instead of making every possible trip." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.shopping_gate_meals_pct)), "Meals" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.shopping_gate_meals_pct)), $"Chance that an eligible meal shopping trip is skipped." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.shopping_gate_groceries_pct)), "Groceries and drinks" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.shopping_gate_groceries_pct)), $"Chance that eligible food, convenience food, or beverage errands are skipped." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.shopping_gate_health_fuel_pct)), "Medicine and fuel" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.shopping_gate_health_fuel_pct)), $"Chance that eligible pharmaceutical or petrochemical errands are skipped." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.shopping_gate_household_goods_pct)), "Small household goods" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.shopping_gate_household_goods_pct)), $"Chance that eligible paper or textile errands are skipped." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.shopping_gate_consumer_goods_pct)), "Consumer goods" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.shopping_gate_consumer_goods_pct)), $"Chance that eligible plastics, chemicals, or electronics errands are skipped." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.shopping_gate_large_purchases_pct)), "Large purchases" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.shopping_gate_large_purchases_pct)), $"Chance that eligible furniture or vehicle errands are skipped." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.household_shopping_cooldown_enabled)), "Enable household cooldown" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.household_shopping_cooldown_enabled)), $"When enabled, a recent household shopping trip temporarily suppresses additional non-meal shopping errands from the same household." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.household_cooldown_groceries_pct)), "Groceries and drinks" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.household_cooldown_groceries_pct)), $"Chance that a grocery or drink errand is suppressed while the household cooldown is active." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.household_cooldown_health_fuel_pct)), "Medicine and fuel" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.household_cooldown_health_fuel_pct)), $"Chance that a medicine or fuel errand is suppressed while the household cooldown is active." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.household_cooldown_household_goods_pct)), "Small household goods" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.household_cooldown_household_goods_pct)), $"Chance that a paper or textile errand is suppressed while the household cooldown is active." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.household_cooldown_large_purchases_pct)), "Large purchases" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.household_cooldown_large_purchases_pct)), $"Chance that a furniture, electronics, or vehicle errand is suppressed while the household cooldown is active." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.household_cooldown_other_pct)), "Other goods" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.household_cooldown_other_pct)), $"Chance that other non-meal shopping errands are suppressed while the household cooldown is active." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.household_cooldown_regular_hours)), "Regular cooldown hours" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.household_cooldown_regular_hours)), $"Cooldown duration after most non-meal shopping trips." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.household_cooldown_large_purchase_hours)), "Large purchase cooldown hours" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.household_cooldown_large_purchase_hours)), $"Cooldown duration after furniture, electronics, or vehicle shopping trips." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.park_weekday)), "Monday to Thursday" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.park_weekday)), $"Average hours that a person spends going to parks per day." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.park_avgday)), "Friday" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.park_avgday)), $"Average hours that a person spends going to parks per day." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.park_saturday)), "Saturday" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.park_saturday)), $"Average hours that a person spends going to parks per day." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.park_sunday)), "Sunday" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.park_sunday)), $"Average hours that a person spends going to parks per day." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.travel_weekday)), "Monday to Thursday" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.travel_weekday)), $"Average hours that a person spends travelling per day." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.travel_avgday)), "Friday" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.travel_avgday)), $"Average hours that a person spends travelling per day." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.travel_saturday)), "Saturday" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.travel_saturday)), $"Average hours that a person spends travelling per day." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.travel_sunday)), "Sunday" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.travel_sunday)), $"Average hours that a person spends travelling per day." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.school_lv1_weekday_pct)), "Monday to Thursday" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.school_lv1_weekday_pct)), $"Percentage of students that go to School on Monday to Thursday" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.school_lv1_avgday_pct)), "Friday" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.school_lv1_avgday_pct)), $"Percentage of students that go to School on Friday" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.school_lv1_saturday_pct)), "Saturday" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.school_lv1_saturday_pct)), $"Percentage of students that go to School on Saturday" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.school_lv1_sunday_pct)), "Sunday" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.school_lv1_sunday_pct)), $"Percentage of students that go to School on Sunday" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.school_lv2_weekday_pct)), "Monday to Thursday" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.school_lv2_weekday_pct)), $"Percentage of students that go to School on Monday to Thursday" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.school_lv2_avgday_pct)), "Friday" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.school_lv2_avgday_pct)), $"Percentage of students that go to School on Friday" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.school_lv2_saturday_pct)), "Saturday" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.school_lv2_saturday_pct)), $"Percentage of students that go to School on Saturday" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.school_lv2_sunday_pct)), "Sunday" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.school_lv2_sunday_pct)), $"Percentage of students that go to School on Sunday" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.school_lv34_weekday_pct)), "Monday to Thursday" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.school_lv34_weekday_pct)), $"Percentage of students that go to College or University on Monday to Thursday" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.school_lv34_avgday_pct)), "Friday" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.school_lv34_avgday_pct)), $"Percentage of students that go to College or University on Friday" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.school_lv34_saturday_pct)), "Saturday" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.school_lv34_saturday_pct)), $"Percentage of students that go to College or University on Saturday" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.school_lv34_sunday_pct)), "Sunday" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.school_lv34_sunday_pct)), $"Percentage of students that go to College or University on Sunday" },
                 { m_Setting.GetOptionLabelLocaleID(nameof(Setting.office_weekday_pct)), "Monday to Thursday" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.office_weekday_pct)), $"Percentage of workers that work on Monday to Thursday" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.office_avgday_pct)), "Friday" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.office_avgday_pct)), $"Percentage of workers that work on Friday" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.office_sat_pct)), "Saturday" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.office_sat_pct)), $"Percentage of workers that work on Saturday" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.office_sun_pct)), "Sunday" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.office_sun_pct)), $"Percentage of workers that work on Sunday" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.industry_weekday_pct)), "Monday to Thursday" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.industry_weekday_pct)), $"Percentage of workers that work on Monday to Thursday" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.industry_avgday_pct)), "Friday" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.industry_avgday_pct)), $"Percentage of workers that work on Friday" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.industry_sat_pct)), "Saturday" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.industry_sat_pct)), $"Percentage of workers that work on Saturday" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.industry_sun_pct)), "Sunday" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.industry_sun_pct)), $"Percentage of workers that work on Sunday" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.commercial_weekday_pct)), "Monday to Thursday" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.commercial_weekday_pct)), $"Percentage of workers that work on Monday to Thursday" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.commercial_avgday_pct)), "Friday" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.commercial_avgday_pct)), $"Percentage of workers that work on Friday" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.commercial_sat_pct)), "Saturday" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.commercial_sat_pct)), $"Percentage of workers that work on Saturday" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.commercial_sun_pct)), "Sunday" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.commercial_sun_pct)), $"Percentage of workers that work on Sunday" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.cityServices_weekday_pct)), "Monday to Thursday" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.cityServices_weekday_pct)), $"Percentage of workers that work on Monday to Thursday" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.cityServices_avgday_pct)), "Friday" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.cityServices_avgday_pct)), $"Percentage of workers that work on Friday" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.cityServices_sat_pct)), "Saturday" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.cityServices_sat_pct)), $"Percentage of workers that work on Saturday" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.cityServices_sun_pct)), "Sunday" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.cityServices_sun_pct)), $"Percentage of workers that work on Sunday" },


                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.evening_share)), "Evening" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.evening_share)), $"Percentage for evening workplaces" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.night_share)), "Night" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.night_share)), $"Percentage for night workplaces" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.nonday_office_share)), "Office" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.nonday_office_share)), $"Percentage for evening and night workplaces" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.nonday_commercial_share)), "Commercial" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.nonday_commercial_share)), $"Percentage for evening and night workplaces" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.nonday_industry_share)), "Industry" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.nonday_industry_share)), $"Percentage for evening and night workplaces" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.nonday_cityservices_share)), "City Services" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.nonday_cityservices_share)), $"Percentage for evening and night workplaces" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.part_time_percentage)), "Part Time Percentage" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.part_time_percentage)), $"Percentage of day shift workers that work part time. These workers will work either in the morning or in the afternoon. They do not take lunch break, and a higher value will increase trips in the middle of the day and reduce the rush hour peaks." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.remote_percentage)), "Percentage of Remote Workers" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.remote_percentage)), $"Percentage of workers that work from home. These workers can still go out for a lunch break. Remote work only apply to offices and city services." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.delay_factor)), "Delay/Early Factor" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.delay_factor)), $"This factor will adjust the variation in arrival and departure times from work. A higher factor will increase the variation on work arrival and departure - meaning more cims will not arrive to work on time or work for longer hours. A value of zero will disable this feature. Note that the effects of this feature in the morning and evening peak hours is different: in the morning there is an equal probabilty of early or late arrival, however, in the evening the probability of leaving late is higher than of leaving early. This was implemented this way to simulate better the differences of morning and evening commute from the real world." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.peak_spread)), "Peak Spreading" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.peak_spread)), $"If this option is enabled, commuters that have a very long trip to go to work will leave earlier to avoid traffic." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.lunch_break_percentage)), "Lunch Break Probability" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.lunch_break_percentage)), $"Probability of workers that will take a lunch break. During a lunch break, workers might go shopping for food or convenience food, or go for leisure. After that they will return to work." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.disable_early_shop_leisure)), "Disable Early Shopping or Leisure" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.disable_early_shop_leisure)), $"In the Vanilla game, Cims can go for shopping or leisure as early as 4 AM. This option will change that behavior to start around 8 to 10 AM." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.use_vanilla_timeoff)), "Disable Vacation and Holiday feature" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.use_vanilla_timeoff)), $"Disable vacation and holiday feature and use Vanilla time off system. In the Vanilla game, cims have a 60% probability of taking time off each day." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.use_school_vanilla_timeoff)), "Disable Vacation and Holiday feature for schools" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.use_school_vanilla_timeoff)), $"Disable vacation and holiday feature and use Vanilla time off system for schools. In the Vanilla game, cims have a 60% probability of not going to school." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.holidays_per_year)), "Number of holidays per year" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.holidays_per_year)), $"Most countries should have a value between 10 and 15. The default value is reasonable for most countries." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.vacation_per_year)), "Number of vacation days per year" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.vacation_per_year)), $"Number of vacation days per year - not including weekends. For countries with a month of vacation, use 22. For the US, a value of 11 is more realistic." },
                //{ m_Setting.GetOptionLabelLocaleID(nameof(Setting.school_vacation_per_year)), "Number of vacation days per year" },
                //{ m_Setting.GetOptionDescLocaleID(nameof(Setting.school_vacation_per_year)), $"Number of vacation days per year for schools, colleges and universities. Does not include weekends." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.school_vacation_month1)), "Vacation Month 1:" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.school_vacation_month1)), $"Month in which schools will be closed for vacation." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.school_vacation_month2)), "Vacation Month 2:" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.school_vacation_month2)), $"Month in which schools will be closed for vacation." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.dt_simulation)), "Select day type simulation behaviour" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.dt_simulation)), $"This option changes how the simulation works during a day. Average Day corresponds to the Vanilla behavior, which is a combination of weekday and weekend behaviors. With the default vacation/holiday settings (defined in the Shopping and Leisure tab), in an Average Day, around 30% of cims will behave as on the weekend, doing more leisure and shopping activities, while the rest will work or study. The Weekday option will increase work and study activities and lower leisure and shopping. Weekend will do the opposite. On Weekends, schools are closed. The 7 Days Week will rotate through weekdays and weekends, going from Monday to Sunday." },
                //{ m_Setting.GetOptionLabelLocaleID(nameof(Setting.better_trucks)), "More realistic truck traffic" },
                //{ m_Setting.GetOptionDescLocaleID(nameof(Setting.better_trucks)), $"Truck traffic will increase in the early and mid parts of the day." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.school_start_time)), "Elementary School Start Time" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.school_start_time)), $"Start time for elementary schools." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.school_end_time)), "Elementary School End Time" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.school_end_time)), $"End time for elementary schools." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.high_school_start_time)), "High School Start Time" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.high_school_start_time)), $"Start time for high schools." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.high_school_end_time)), "High School End Time" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.high_school_end_time)), $"End time for high schools." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.univ_start_time)), "College/University Start Time" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.univ_start_time)), $"Start time for colleges and universities." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.univ_end_time)), "College/University End Time" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.univ_end_time)), $"End time for college and universities." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.work_start_time)), "Work Day Shift Start Time" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.work_start_time)), $"Start time for work day shift." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.work_end_time)), "Work Day Shift End Time" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.work_end_time)), $"End time for work day shift." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.avg_work_hours_ft_wd)), "Avg. Hours Worked for Full Time Workers" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.avg_work_hours_ft_wd)), $"The average number of hours that full time workers worked on a weekday" },
                //{ m_Setting.GetOptionLabelLocaleID(nameof(Setting.avg_work_hours_ft_we)), "Weekend: Avg. Hours Worked for Full Time Workers" },
                //{ m_Setting.GetOptionDescLocaleID(nameof(Setting.avg_work_hours_ft_we)), $"The average number of hours that full time workers worked on a weekend" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.avg_work_hours_pt_wd)), "Avg. Hours Worked for Part Time Workers" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.avg_work_hours_pt_wd)), $"The average number of hours that part time workers worked on a weekday" },
                //{ m_Setting.GetOptionLabelLocaleID(nameof(Setting.avg_work_hours_pt_we)), "Weekend: Avg. Hours Worked for Part Time Workers" },
                //{ m_Setting.GetOptionDescLocaleID(nameof(Setting.avg_work_hours_pt_we)), $"The average number of hours that part time workers worked on a weekend" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.slow_time_factor)), "Slower Time Factor" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.slow_time_factor)), $"This factor will slow down time and increase the length of the day. A factor of 1 will have no effect. A factor of 2, for example, will make the day last twice as long. Note that the simulation speed does not change, and other systems not affected by this mod will update based on the simulation speed and not on the length of the day." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.daysPerMonth)), "Days per Month" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.daysPerMonth)), $"Changes the number of days per month. Default is 1" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.tourism_trips)), "Tourism variation by day of the week" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.tourism_trips)), $"Increases tourists on weekends and reduces them on weekdays." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.commuter_trips)), "Commuter  variation by day of the week" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.commuter_trips)), $"Increases outside connection commuters on weekdays and reduces them on weekends. Also increases the probability of them arriving by plane." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.service_expenses_night_reduction)), "Night Cost Reduction" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.service_expenses_night_reduction)), $"Reduce the cost of services from 11 PM to 6 AM." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.trafficReduction)), "Traffic Reduction" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.trafficReduction)), $"Lower values increase traffic in the city. Vanilla value is 5. Zero will have the maximun amout of traffic." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.resourceConsumption)), "Resource Consumption" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.resourceConsumption)), $"Set the amount of resources needed to be consumed by each citizen. Easy game mode value is 1, normal mode is 20." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.shopping_log_enabled)), "Shopping diagnostics log" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.shopping_log_enabled)), $"Logs hourly shopping totals by resource, including trips, amount, spend, average stay duration, and average distance. This is intended for calibration and can reduce performance while enabled." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.use_universal_mod_menu)), "Show button in Universal Mod Menu" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.use_universal_mod_menu)), $"Adds a Realistic Trips button to the Universal Mod Menu. Disabled by default. Restarting the game after changing this option is recommended." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.hospital_stay_duration_enabled)), "Enable hospital encounter duration" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.hospital_stay_duration_enabled)), $"When enabled, cims who arrive at a hospital stay for either a short visit or an occasional inpatient stay. Disabled by default." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.hospital_stay_inpatient_chance_pct)), "Inpatient chance" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.hospital_stay_inpatient_chance_pct)), $"Chance that a hospital encounter becomes a longer inpatient stay instead of a short visit." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.hospital_short_stay_average_hours)), "Short visit average hours" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.hospital_short_stay_average_hours)), $"Mean duration for short hospital visits, in in-game hours." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.hospital_short_stay_stddev_hours)), "Short visit variation hours" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.hospital_short_stay_stddev_hours)), $"Standard deviation for short hospital visit duration, in in-game hours." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.hospital_short_stay_minimum_hours)), "Short visit minimum hours" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.hospital_short_stay_minimum_hours)), $"Shortest sampled short hospital visit, in in-game hours." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.hospital_short_stay_maximum_hours)), "Short visit maximum hours" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.hospital_short_stay_maximum_hours)), $"Longest sampled short hospital visit, in in-game hours." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.hospital_inpatient_average_hours)), "Inpatient average hours" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.hospital_inpatient_average_hours)), $"Mean duration for longer inpatient stays, in in-game hours." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.hospital_inpatient_stddev_hours)), "Inpatient variation hours" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.hospital_inpatient_stddev_hours)), $"Standard deviation for inpatient stay duration, in in-game hours." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.hospital_inpatient_minimum_hours)), "Inpatient minimum hours" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.hospital_inpatient_minimum_hours)), $"Shortest sampled inpatient stay, in in-game hours." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.hospital_inpatient_maximum_hours)), "Inpatient maximum hours" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.hospital_inpatient_maximum_hours)), $"Longest sampled inpatient stay, in in-game hours." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.min_attraction)), "Min. Attraction" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.min_attraction)), $"Increasing or decreasing this setting will change the number of park facilities that can host special events." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.min_event_weekday)), "Monday to Thursday" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.min_event_weekday)), $"Minimum number of events on Monday to Thursday" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.min_event_avg_day)), "Friday" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.min_event_avg_day)), $"Minimum number of events on Friday" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.min_event_weekend)), "Weekend" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.min_event_weekend)), $"Minimum number of events on the Wekend" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.max_event_weekday)), "Monday to Thursday" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.max_event_weekday)), $"Maximum number of events on Monday to Thursday" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.max_event_avg_day)), "Friday" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.max_event_avg_day)), $"Maximum number of events on Friday" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.max_event_weekend)), "Weekend" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.max_event_weekend)), $"Maximum number of events on the Wekend" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.avg_time_beverages)), "Beverages" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.avg_time_beverages)), $"Average time in minutes to shop for beverages." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.avg_time_chemicals)), "Chemicals" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.avg_time_chemicals)), $"Average time in minutes to shop for chemicals." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.avg_time_convenienceFood)), "Convenience Food" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.avg_time_convenienceFood)), $"Average time in minutes to shop for convenience food." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.avg_time_electronics)), "Electronics" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.avg_time_electronics)), $"Average time in minutes to shop for electronics." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.avg_time_software)), "Software" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.avg_time_software)), $"Average time in minutes to shop for software." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.avg_time_financial)), "Financial" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.avg_time_financial)), $"Average time in minutes to shop for financial services." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.avg_time_food)), "Food" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.avg_time_food)), $"Average time in minutes to shop for food." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.avg_time_furniture)), "Furniture" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.avg_time_furniture)), $"Average time in minutes to shop for furniture." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.avg_time_meals)), "Meals" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.avg_time_meals)), $"Average time in minutes to shop for meals." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.avg_time_media)), "Media" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.avg_time_media)), $"Average time in minutes to shop for media." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.avg_time_paper)), "Paper" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.avg_time_paper)), $"Average time in minutes to shop for paper products." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.avg_time_petrochemicals)), "Petrochemicals" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.avg_time_petrochemicals)), $"Average time in minutes to shop for petrochemicals." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.avg_time_pharmaceuticals)), "Pharmaceuticals" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.avg_time_pharmaceuticals)), $"Average time in minutes to shop for pharmaceuticals." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.avg_time_plastics)), "Plastics" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.avg_time_plastics)), $"Average time in minutes to shop for plastics." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.avg_time_telecom)), "Telecom" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.avg_time_telecom)), $"Average time in minutes to shop for telecom services." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.avg_time_textiles)), "Textiles" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.avg_time_textiles)), $"Average time in minutes to shop for textiles." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.avg_time_recreation)), "Recreation" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.avg_time_recreation)), $"Average time in minutes to shop for recreation items." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.avg_time_entertainment)), "Entertainment" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.avg_time_entertainment)), $"Average time in minutes to shop for entertainment." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.avg_time_vehicles)), "Vehicles" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.avg_time_vehicles)), $"Average time in minutes to shop for vehicles." },
                //{ m_Setting.GetOptionLabelLocaleID(nameof(Setting.avg_time_hospital)), "Hospitals/Clinics" },
                //{ m_Setting.GetOptionDescLocaleID(nameof(Setting.avg_time_hospital)), $"Average time in minutes spent in hospitals or clinics." },
                //{ m_Setting.GetOptionLabelLocaleID(nameof(Setting.avg_time_prison)), "Prisons/Jails" },
                //{ m_Setting.GetOptionDescLocaleID(nameof(Setting.avg_time_prison)), $"Average time in minutes spent in prisons or jails." },


                { m_Setting.GetEnumValueLocaleID(Setting.DTSimulationEnum.AverageDay), "Average Day" },
                { m_Setting.GetEnumValueLocaleID(Setting.DTSimulationEnum.Weekday), "Weekday" },
                { m_Setting.GetEnumValueLocaleID(Setting.DTSimulationEnum.Saturday), "Saturday" },
                { m_Setting.GetEnumValueLocaleID(Setting.DTSimulationEnum.Sunday), "Sunday" },
                { m_Setting.GetEnumValueLocaleID(Setting.DTSimulationEnum.sevendayweek), "7 Days Week (Monday to Sunday)" },

                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.Performance), "Performance" },
                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.Balanced), "Balanced" },
                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.USA), "United States of America" },
                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.Singapore), "Singapore" },
                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.UK), "United Kingdom" },
                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.Brazil), "Brazil" },
                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.Canada), "Canada" },
                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.Phillipines), "Philippines" },
                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.Netherlands), "Netherlands" },
                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.France), "France" },
                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.Germany), "Germany" },
                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.Poland), "Poland" },
                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.Argentina), "Argentina" },
                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.Australia), "Australia" },
                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.India), "India" },
                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.Italy), "Italy" },
                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.Japan), "Japan" },
                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.Mexico), "Mexico" },
                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.SouthAfrica), "South Africa" },
                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.Spain), "Spain" },

                { m_Setting.GetEnumValueLocaleID(Setting.timeEnum.t700), "7:00 AM" },
                { m_Setting.GetEnumValueLocaleID(Setting.timeEnum.t730), "7:30 AM" },
                { m_Setting.GetEnumValueLocaleID(Setting.timeEnum.t800), "8:00 AM" },
                { m_Setting.GetEnumValueLocaleID(Setting.timeEnum.t830), "8:30 AM" },
                { m_Setting.GetEnumValueLocaleID(Setting.timeEnum.t900), "9:00 AM" },
                { m_Setting.GetEnumValueLocaleID(Setting.timeEnum.t930), "9:30 AM" },
                { m_Setting.GetEnumValueLocaleID(Setting.timeEnum.t1000), "10:00 AM" },
                { m_Setting.GetEnumValueLocaleID(Setting.timeEnum.t1030), "10:30 AM" },
                { m_Setting.GetEnumValueLocaleID(Setting.timeEnum.t1100), "11:00 AM" },
                { m_Setting.GetEnumValueLocaleID(Setting.timeEnum.t1130), "11:30 AM" },
                { m_Setting.GetEnumValueLocaleID(Setting.timeEnum.t1200), "12:00 AM" },
                { m_Setting.GetEnumValueLocaleID(Setting.timeEnum.t1230), "12:30 AM" },
                { m_Setting.GetEnumValueLocaleID(Setting.timeEnum.t1300), "1:00 PM" },
                { m_Setting.GetEnumValueLocaleID(Setting.timeEnum.t1330), "1:30 PM" },
                { m_Setting.GetEnumValueLocaleID(Setting.timeEnum.t1400), "2:00 PM" },
                { m_Setting.GetEnumValueLocaleID(Setting.timeEnum.t1430), "2:30 PM" },
                { m_Setting.GetEnumValueLocaleID(Setting.timeEnum.t1500), "3:00 PM" },
                { m_Setting.GetEnumValueLocaleID(Setting.timeEnum.t1530), "3:30 PM" },
                { m_Setting.GetEnumValueLocaleID(Setting.timeEnum.t1600), "4:00 PM" },
                { m_Setting.GetEnumValueLocaleID(Setting.timeEnum.t1630), "4:30 PM" },
                { m_Setting.GetEnumValueLocaleID(Setting.timeEnum.t1700), "5:00 PM" },
                { m_Setting.GetEnumValueLocaleID(Setting.timeEnum.t1730), "5:30 PM" },
                { m_Setting.GetEnumValueLocaleID(Setting.timeEnum.t1800), "6:00 PM" },
                { m_Setting.GetEnumValueLocaleID(Setting.timeEnum.t1830), "6:30 PM" },
                { m_Setting.GetEnumValueLocaleID(Setting.timeEnum.t1900), "7:00 PM" },
                { m_Setting.GetEnumValueLocaleID(Setting.timeEnum.t1930), "7:30 PM" },
                { m_Setting.GetEnumValueLocaleID(Setting.timeEnum.t2000), "8:00 PM" },

                { m_Setting.GetEnumValueLocaleID(Setting.months.January), "Jan" },
                { m_Setting.GetEnumValueLocaleID(Setting.months.February), "Feb" },
                { m_Setting.GetEnumValueLocaleID(Setting.months.March), "Mar" },
                { m_Setting.GetEnumValueLocaleID(Setting.months.April), "Apr" },
                { m_Setting.GetEnumValueLocaleID(Setting.months.May), "May" },
                { m_Setting.GetEnumValueLocaleID(Setting.months.June), "Jun" },
                { m_Setting.GetEnumValueLocaleID(Setting.months.July), "Jul" },
                { m_Setting.GetEnumValueLocaleID(Setting.months.August), "Aug" },
                { m_Setting.GetEnumValueLocaleID(Setting.months.September), "Sep" },
                { m_Setting.GetEnumValueLocaleID(Setting.months.October), "Oct" },
                { m_Setting.GetEnumValueLocaleID(Setting.months.November), "Nov" },
                { m_Setting.GetEnumValueLocaleID(Setting.months.December), "Dec" },

                { m_Setting.GetEnumValueLocaleID(Setting.dayOfWeek.Sunday), "Sun" },
                { m_Setting.GetEnumValueLocaleID(Setting.dayOfWeek.Monday), "Mon" },
                { m_Setting.GetEnumValueLocaleID(Setting.dayOfWeek.Tuesday), "Tue" },
                { m_Setting.GetEnumValueLocaleID(Setting.dayOfWeek.Wednesday), "Wed" },
                { m_Setting.GetEnumValueLocaleID(Setting.dayOfWeek.Thursday), "Thu" },
                { m_Setting.GetEnumValueLocaleID(Setting.dayOfWeek.Friday), "Fri" },
                { m_Setting.GetEnumValueLocaleID(Setting.dayOfWeek.Saturday), "Sat" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.date_format)), "Date Format in UI" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.date_format)), "Choose how the day and date are shown in the Game UI." },

                { m_Setting.GetEnumValueLocaleID(Setting.DateFormatEnum.DayOfWeek_Month_Year), "Day of Week, Month Year" },
                { m_Setting.GetEnumValueLocaleID(Setting.DateFormatEnum.DayOfWeek_DDMMYYYY), "Day of Week, DD/MM/YYYY" },
                { m_Setting.GetEnumValueLocaleID(Setting.DateFormatEnum.DayOfWeek_MMDDYYYY), "Day of Week, MM/DD/YYYY" },

                { "t2w.chirp.special_event.today",   "Special event today from {start} to {end} at" },
                { "t2w.chirp.special_event.attendees",   "{attendees} citizens attended the special event at" },
                { "t2w.chirp.special_event.starting","Special event starting soon at" },
                { "t2w.chirp.special_event.ending",  "Special event ending soon at" },
                { "t2w.chirp.holiday.new_year",      "Happy New Year!" },
                { "t2w.chirp.mod_name",      "Realistic Trips Mod" },
            };
            }

            public void Unload()
            {

            }
        }

        public class LocalePT : IDictionarySource
        {
            private readonly Setting m_Setting;
            public LocalePT(Setting setting)
            {
                m_Setting = setting;
            }
            public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
            {
                return new Dictionary<string, string>
            {
                { m_Setting.GetSettingsLocaleID(), "Realistic Trips" },
                { m_Setting.GetOptionTabLocaleID(Setting.SettingsSection), "ConfiguraÃ§Ãµes" },
                { m_Setting.GetOptionTabLocaleID(Setting.WorkSection), "Emprego" },
                { m_Setting.GetOptionTabLocaleID(Setting.ShopLeisureSection), "Compras e Lazer" },
                { m_Setting.GetOptionTabLocaleID(Setting.HealthSection), "Saude" },
                { m_Setting.GetOptionTabLocaleID(Setting.SchoolSection), "Escola" },
                { m_Setting.GetOptionTabLocaleID(Setting.Weeksection), "Semana" },
                { m_Setting.GetOptionTabLocaleID(Setting.EventSection), "Eventos Especiais" },
                { m_Setting.GetOptionTabLocaleID(Setting.OtherSection), "Outros" },

                { m_Setting.GetOptionGroupLocaleID(Setting.MinEventGroup), "NÃºmero mÃ­nimo de eventos por dia da semana" },
                { m_Setting.GetOptionGroupLocaleID(Setting.MaxEventGroup), "NÃºmero mÃ¡ximo de eventos por dia da semana" },
                { m_Setting.GetOptionGroupLocaleID(Setting.WorkPlaceShiftGroup), "Alterar a porcentagem de turnos vespertinos e noturnos." },
                { m_Setting.GetOptionGroupLocaleID(Setting.NonDayShiftByWorkTypeGroup), "Alterar a porcentagem de turnos vespertinos e noturnos por tipo de empregos." },
                { m_Setting.GetOptionGroupLocaleID(Setting.RemoteGroup), "ConfiguraÃ§Ãµes de Home Office" },
                { m_Setting.GetOptionGroupLocaleID(Setting.TimeOffGroup), "ConfiguraÃ§Ãµes de fÃ©rias e feriados" },
                { m_Setting.GetOptionGroupLocaleID(Setting.DayShiftGroup), "ConfiguraÃ§Ãµes do turno diurno."},
                { m_Setting.GetOptionGroupLocaleID(Setting.LeisureMealsGroup), "RefeiÃ§Ãµes: MÃ©dia de horas de lazer por dia" },
                { m_Setting.GetOptionGroupLocaleID(Setting.LeisureEntertainmentGroup), "Entretenimento: Avg. Hours for for Leisure per day" },
                { m_Setting.GetOptionGroupLocaleID(Setting.LeisureShoppingGroup), "Compras: de horas de lazer por dia" },
                { m_Setting.GetOptionGroupLocaleID(Setting.LeisureParksGroup), "Parques: MÃ©dia de horas de lazer por dia" },
                { m_Setting.GetOptionGroupLocaleID(Setting.LeisureTravelGroup), "Viagens: MÃ©dia de horas de lazer por dia" },
                { m_Setting.GetOptionGroupLocaleID(Setting.ShoppingTimeGroup), "Temp medio de compras (minutos) para cada recurso" },
                { m_Setting.GetOptionGroupLocaleID(Setting.ShoppingTripGateGroup), "Filtros de frequÃªncia de compras" },
                { m_Setting.GetOptionGroupLocaleID(Setting.ShoppingCooldownGroup), "Intervalo de compras por domicÃ­lio" },
                { m_Setting.GetOptionGroupLocaleID(Setting.HospitalStayGroup), "Atendimentos hospitalares" },
                { m_Setting.GetOptionGroupLocaleID(Setting.SchoolTimeOffGroup), "ConfiguraÃ§Ãµes de fÃ©rias escolares" },
                { m_Setting.GetOptionGroupLocaleID(Setting.SchoolTimeGroup), "ConfiguraÃ§Ãµes de horÃ¡rio de inÃ­cio/tÃ©rmino das aulas nas escolas" },
                { m_Setting.GetOptionGroupLocaleID(Setting.School1WeekGroup), "FrequÃªncia escolar elementar por dia" },
                { m_Setting.GetOptionGroupLocaleID(Setting.School2WeekGroup), "FrequÃªncia do ensino mÃ©dio por dia" },
                { m_Setting.GetOptionGroupLocaleID(Setting.School34WeekGroup), "FrequÃªncia da faculade e universidade por dia" },
                { m_Setting.GetOptionGroupLocaleID(Setting.SlowerTimeGroup), "ConfiguraÃ§Ãµes do Dia e do Tempo" },
                { m_Setting.GetOptionGroupLocaleID(Setting.DTSimulationGroup), "Tipo de SimulaÃ§Ã£o DiÃ¡ria" },
                { m_Setting.GetOptionGroupLocaleID(Setting.TrucksGroup), "CaminhÃµes" },
                { m_Setting.GetOptionGroupLocaleID(Setting.OtherGroup), "Outros" },
                { m_Setting.GetOptionGroupLocaleID(Setting.ExternalGroup), "Viagens Externas" },
                { m_Setting.GetOptionGroupLocaleID(Setting.ExpensesGroup), "Gastos com ServiÃ§os" },
                { m_Setting.GetOptionGroupLocaleID(Setting.WeekGroup), "Porcentagem de Trabalhadores por Dia" },
                { m_Setting.GetOptionGroupLocaleID(Setting.OfficeGroup), "EscritÃ³rio - Porcentagem de Trabalhadores por Dia" },
                { m_Setting.GetOptionGroupLocaleID(Setting.CommercialGroup), "ComÃ©rcio - Porcentagem de Trabalhadores por Dia" },
                { m_Setting.GetOptionGroupLocaleID(Setting.IndustryGroup), "IndÃºstria - Porcentagem de Trabalhadores por Dia" },
                { m_Setting.GetOptionGroupLocaleID(Setting.CityServicesGroup), "ServiÃ§os PÃºblicos - Porcentagem de Trabalhadores por Dia" },
                { m_Setting.GetOptionGroupLocaleID(Setting.VisitTimeGroup), "Tempo gasto visitando certos edifÃ­cios" },
                { m_Setting.GetOptionGroupLocaleID(Setting.HolidayGroup), "Feriados" },


                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.new_years_num_events)), "NÃºmero de eventos de Ano Novo" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.new_years_num_events)), $"O nÃºmero de eventos que ocorrerÃ£o no Ãºltimo dia do ano. Todos eles terminam Ã  meia-noite." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.meals_weekday)), "Segunda a Quinta" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.meals_weekday)), $"MÃ©dia de horas que uma pessoa gasta saindo para comer por dia." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.meals_avgday)), "Sexta" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.meals_avgday)), $"MÃ©dia de horas que uma pessoa gasta saindo para comer por dia." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.meals_saturday)), "SÃ¡bado" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.meals_saturday)), $"MÃ©dia de horas que uma pessoa gasta saindo para comer por dia." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.meals_sunday)), "Domingo" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.meals_sunday)), $"MÃ©dia de horas que uma pessoa gasta saindo para comer por dia." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.entertainment_weekday)), "Segunda a Quinta" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.entertainment_weekday)), $"MÃ©dia de horas que uma pessoa gasta saindo para se divertir por dia." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.entertainment_avgday)), "Sexta" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.entertainment_avgday)), $"MÃ©dia de horas que uma pessoa gasta saindo para se divertir por dia." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.entertainment_saturday)), "SÃ¡bado" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.entertainment_saturday)), $"MÃ©dia de horas que uma pessoa gasta saindo para se divertir por dia." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.entertainment_sunday)), "Domingo" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.entertainment_sunday)), $"MÃ©dia de horas que uma pessoa gasta saindo para se divertir por dia." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.shopping_weekday)), "Segunda a Quinta" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.shopping_weekday)), $"MÃ©dia de horas que uma pessoa gasta saindo para fazer compras por dia." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.shopping_avgday)), "Sexta" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.shopping_avgday)), $"MÃ©dia de horas que uma pessoa gasta saindo para fazer compras por dia." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.shopping_saturday)), "SÃ¡bado" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.shopping_saturday)), $"MÃ©dia de horas que uma pessoa gasta saindo para fazer compras por dia." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.shopping_sunday)), "Domingo" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.shopping_sunday)), $"MÃ©dia de horas que uma pessoa gasta saindo para fazer compras por dia." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.shopping_trip_gates_enabled)), "Ativar filtros de compras" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.shopping_trip_gates_enabled)), $"Quando ativado, algumas pequenas compras sÃ£o ignoradas de forma probabilÃ­stica para simular compras combinadas." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.shopping_gate_meals_pct)), "RefeiÃ§Ãµes" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.shopping_gate_meals_pct)), $"Chance de ignorar uma viagem elegÃ­vel para refeiÃ§Ã£o." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.shopping_gate_groceries_pct)), "Mercado e bebidas" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.shopping_gate_groceries_pct)), $"Chance de ignorar compras elegÃ­veis de comida, comida pronta ou bebidas." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.shopping_gate_health_fuel_pct)), "Medicamentos e combustÃ­vel" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.shopping_gate_health_fuel_pct)), $"Chance de ignorar compras elegÃ­veis de medicamentos ou petroquÃ­micos." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.shopping_gate_household_goods_pct)), "Itens domÃ©sticos pequenos" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.shopping_gate_household_goods_pct)), $"Chance de ignorar compras elegÃ­veis de papel ou tÃªxteis." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.shopping_gate_consumer_goods_pct)), "Bens de consumo" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.shopping_gate_consumer_goods_pct)), $"Chance de ignorar compras elegÃ­veis de plÃ¡sticos, quÃ­micos ou eletrÃ´nicos." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.shopping_gate_large_purchases_pct)), "Compras grandes" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.shopping_gate_large_purchases_pct)), $"Chance de ignorar compras elegÃ­veis de mÃ³veis ou veÃ­culos." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.household_shopping_cooldown_enabled)), "Ativar intervalo por domicÃ­lio" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.household_shopping_cooldown_enabled)), $"Quando ativado, uma compra recente do domicÃ­lio reduz temporariamente novas compras que nÃ£o sejam refeiÃ§Ãµes." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.household_cooldown_groceries_pct)), "Mercado e bebidas" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.household_cooldown_groceries_pct)), $"Chance de suprimir compras de comida ou bebidas enquanto o intervalo do domicÃ­lio estiver ativo." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.household_cooldown_health_fuel_pct)), "Medicamentos e combustÃ­vel" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.household_cooldown_health_fuel_pct)), $"Chance de suprimir compras de medicamentos ou combustÃ­vel enquanto o intervalo do domicÃ­lio estiver ativo." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.household_cooldown_household_goods_pct)), "Itens domÃ©sticos pequenos" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.household_cooldown_household_goods_pct)), $"Chance de suprimir compras de papel ou tÃªxteis enquanto o intervalo do domicÃ­lio estiver ativo." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.household_cooldown_large_purchases_pct)), "Compras grandes" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.household_cooldown_large_purchases_pct)), $"Chance de suprimir compras de mÃ³veis, eletrÃ´nicos ou veÃ­culos enquanto o intervalo do domicÃ­lio estiver ativo." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.household_cooldown_other_pct)), "Outros bens" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.household_cooldown_other_pct)), $"Chance de suprimir outras compras que nÃ£o sejam refeiÃ§Ãµes enquanto o intervalo do domicÃ­lio estiver ativo." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.household_cooldown_regular_hours)), "Horas de intervalo regular" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.household_cooldown_regular_hours)), $"DuraÃ§Ã£o do intervalo depois da maioria das compras que nÃ£o sejam refeiÃ§Ãµes." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.household_cooldown_large_purchase_hours)), "Horas de intervalo para compras grandes" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.household_cooldown_large_purchase_hours)), $"DuraÃ§Ã£o do intervalo depois de compras de mÃ³veis, eletrÃ´nicos ou veÃ­culos." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.park_weekday)), "Segunda a Quinta" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.park_weekday)), $"MÃ©dia de horas que uma pessoa gasta indo a parques por dia." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.park_avgday)), "Sexta" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.park_avgday)), $"MÃ©dia de horas que uma pessoa gasta indo a parques por dia." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.park_saturday)), "SÃ¡bado" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.park_saturday)), $"MÃ©dia de horas que uma pessoa gasta indo a parques por dia." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.park_sunday)), "Domingo" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.park_sunday)), $"MÃ©dia de horas que uma pessoa gasta indo a parques por dia." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.travel_weekday)), "Segunda a Quinta" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.travel_weekday)), $"MÃ©dia de horas que uma pessoa gasta viajando por dia." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.travel_avgday)), "Sexta" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.travel_avgday)), $"MÃ©dia de horas que uma pessoa gasta viajando por dia." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.travel_saturday)), "SÃ¡bado" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.travel_saturday)), $"MÃ©dia de horas que uma pessoa gasta viajando por dia." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.travel_sunday)), "Sunday" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.travel_sunday)), $"MÃ©dia de horas que uma pessoa gasta viajando por dia." },
                //{ m_Setting.GetOptionLabelLocaleID(nameof(Setting.WeekText)), $"Porcentagem de Trabalhadores por Dia" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DTText)), $"Alterar os parametros abaixo requer reinÃ­cio do jogo." },
                //{ m_Setting.GetOptionLabelLocaleID(nameof(Setting.MultilineText)), $"AVISO: O recurso de Tempo mais Lento pode causar problemas com os mods Population Rebalance e Info Loom - em uma cidade existente. Uma nova cidade provavelmente nÃ£o terÃ¡ problemas com esses mods." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.settings_choice)), $"Alterar as configuraÃ§Ãµes do mod" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.settings_choice)), $"Alterar todas as configuraÃ§Ãµes. Desempenho: irÃ¡ atualizar as configuraÃ§Ãµes para melhorar o desempenho, isso Ã© semelhante ao jogo Vanilla. Balanceado: tem a maioria dos recursos deste mod habilitados, mas alguns deles que tÃªm alto impacto no desempenho estÃ£o desabilitadas. ConfiguraÃ§Ãµes baseadas em um paÃ­s: dados do mundo real foram coletados para alguns paÃ­ses. Selecionar um deles tornarÃ¡ o jogo mais realista, mas pode afetar o desempenho do jogo." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.Button)), "Confirmar" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.Button)), $"Confirmar novas configuraÃ§Ãµes" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.school_lv1_weekday_pct)), "Segunda a Quinta" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.school_lv1_weekday_pct)), $"Porcentagem de alunos que vÃ£o Ã  escola de segunda a quinta" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.school_lv1_avgday_pct)), "Sexta" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.school_lv1_avgday_pct)), $"Porcentagem de alunos que vÃ£o Ã  escola na sexta" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.school_lv1_saturday_pct)), "SÃ¡bado" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.school_lv1_saturday_pct)), $"Porcentagem de alunos que vÃ£o Ã  escola no sÃ¡bado" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.school_lv1_sunday_pct)), "Domingo" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.school_lv1_sunday_pct)), $"Porcentagem de alunos que vÃ£o Ã  escola no domingo" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.school_lv2_weekday_pct)), "Segunda a Quinta" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.school_lv2_weekday_pct)), $"Porcentagem de alunos que vÃ£o Ã  escola de segunda a quinta" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.school_lv2_avgday_pct)), "Sexta" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.school_lv2_avgday_pct)), $"Porcentagem de alunos que vÃ£o Ã  escola na sexta" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.school_lv2_saturday_pct)), "SÃ¡bado" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.school_lv2_saturday_pct)), $"Porcentagem de alunos que vÃ£o Ã  escola no sÃ¡bado" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.school_lv2_sunday_pct)), "Domingo" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.school_lv2_sunday_pct)), $"Porcentagem de alunos que vÃ£o Ã  escola no domingo" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.school_lv34_weekday_pct)), "Segunda a Quinta" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.school_lv34_weekday_pct)), $"Porcentagem de alunos que vÃ£o Ã  Faculdade ou Universidade de segunda a quinta" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.school_lv34_avgday_pct)), "Sexta" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.school_lv34_avgday_pct)), $"Porcentagem de alunos que vÃ£o Ã  Faculdade ou Universidade  na sexta" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.school_lv34_saturday_pct)), "SÃ¡bado" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.school_lv34_saturday_pct)), $"Porcentagem de alunos que vÃ£o Ã  Faculdade ou Universidade  no sÃ¡bado" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.school_lv34_sunday_pct)), "Domingo" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.school_lv34_sunday_pct)), $"Porcentagem de alunos que vÃ£o Ã  Faculdade ou Universidade  no domingo" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.office_weekday_pct)), "Segunda a Quinta" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.office_weekday_pct)), $"Porcentagem dos trabalhadores que trabalham de segunda a quinta" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.office_avgday_pct)), "Sexta" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.office_avgday_pct)), $"Porcentagem dos trabalhadores que trabalham na sexta" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.office_sat_pct)), "SÃ¡bado" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.office_sat_pct)), $"Porcentagem dos trabalhadores que trabalham no sÃ¡bado" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.office_sun_pct)), "Domingo" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.office_sun_pct)), $"Porcentagem dos trabalhadores que trabalham no domingo" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.industry_weekday_pct)), "Segunda a Quinta" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.industry_weekday_pct)), $"Porcentagem dos trabalhadores que trabalham de segunda a quinta" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.industry_avgday_pct)), "Sexta" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.industry_avgday_pct)), $"Porcentagem dos trabalhadores que trabalham na sexta" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.industry_sat_pct)), "SÃ¡bado" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.industry_sat_pct)), $"Porcentagem dos trabalhadores que trabalham  no sÃ¡bado" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.industry_sun_pct)), "Domingo" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.industry_sun_pct)), $"Porcentagem dos trabalhadores que trabalham no domingo" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.commercial_weekday_pct)), "Segunda a Quinta" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.commercial_weekday_pct)), $"Porcentagem dos trabalhadores que trabalham de segunda a quinta" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.commercial_avgday_pct)), "Sexta" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.commercial_avgday_pct)), $"Porcentagem dos trabalhadores que trabalham na sexta" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.commercial_sat_pct)), "SÃ¡bado" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.commercial_sat_pct)), $"Porcentagem dos trabalhadores que trabalham no sÃ¡bado" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.commercial_sun_pct)), "Domingo" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.commercial_sun_pct)), $"Porcentagem dos trabalhadores que trabalham no domingo" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.cityServices_weekday_pct)), "Segunda a Quinta" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.cityServices_weekday_pct)), $"Porcentagem dos trabalhadores que trabalham de segunda a quinta" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.cityServices_avgday_pct)), "Sexta" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.cityServices_avgday_pct)), $"Porcentagem dos trabalhadores que trabalham na sexta" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.cityServices_sat_pct)), "SÃ¡bado" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.cityServices_sat_pct)), $"Porcentagem dos trabalhadores que trabalham no sÃ¡bado" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.cityServices_sun_pct)), "Domingo" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.cityServices_sun_pct)), $"Porcentagem dos trabalhadores que trabalham no domingo" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.nonday_office_share)), "EscritÃ³rio" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.nonday_office_share)), $"Porcentagem para locais de trabalho vespertinos e noturno." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.nonday_commercial_share)), "ComÃ©rcio" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.nonday_commercial_share)), $"Porcentagem para locais de trabalho vespertinos e noturno." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.nonday_industry_share)), "IndÃºstria" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.nonday_industry_share)), $"Porcentagem para locais de trabalho vespertinos e noturno." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.nonday_cityservices_share)), "ServiÃ§os PÃºblicos" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.nonday_cityservices_share)), $"Porcentagem para locais de trabalho vespertinos e noturno." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.evening_share)), "Tarde" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.evening_share)), $"Porcentagem para locais de trabalho vespertinos" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.night_share)), "Noite" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.night_share)), $"Porcentagem para locais de trabalho noturnos" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.remote_percentage)), "Porcentagem de trabalhadores que fazem Home Office" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.remote_percentage)), $"Porcentagem de trabalhadores que trabalham em casa. Estes funcionÃ¡rios tambÃ©m tem intervalo para almoÃ§o. Apenas se aplica a trabalhadores de escritÃ³rio e serviÃ§os pÃºblicos" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.part_time_percentage)), "Porcentagem de trabalhadores de meio perÃ­odo" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.part_time_percentage)), $"Porcentagem de trabalhadores do turno diurno que trabalham meio perÃ­odo. Estes funcionÃ¡rios trabalham ou de manhÃ£ ou de tarde. Eles nÃ£o tem horÃ¡rio de almoÃ§o e um valor mais alto vai aumentar as viagens durante o meio do dia e diminuir os picos dos horÃ¡rios de rush." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.delay_factor)), "Fator de chegada/saÃ­da atrasada ou antecipada" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.delay_factor)), $"Este fator ajustarÃ¡ a variaÃ§Ã£o dos horÃ¡rios de chegada e saÃ­da do trabalho. Um fator mais alto aumentarÃ¡ a variaÃ§Ã£o na chegada e saÃ­da do trabalho - o que significa que mais cims nÃ£o chegarÃ£o ao trabalho na hora certa ou trabalharÃ£o por mais horas. Um valor zero desativarÃ¡ essa funcionalidade. Observe que os efeitos desta opÃ§Ã£o nos horÃ¡rios de pico da manhÃ£ e da tarde sÃ£o diferentes: de manhÃ£ hÃ¡ igual probabilidade de chegar cedo ou mais tarde, porÃ©m, Ã  tarde a probabilidade de sair atrasado Ã© maior do que de sair mais cedo. Isso foi implementado desta forma para simular melhor as diferenÃ§as entre o deslocamento matinal e vespertino em relaÃ§Ã£o ao mundo real." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.peak_spread)), "Suavizar o horÃ¡rio de pico" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.peak_spread)), $"Se esta opÃ§Ã£o estiver habilitada, os passageiros que demoram muito tempo para chegar ao trabalho sairÃ£o mais cedo para evitar o trÃ¢nsito." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.lunch_break_percentage)), "Probabilidade de intervalo para almoÃ§o" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.lunch_break_percentage)), $"Probabilidade dos trabalhadores que farÃ£o intervalo para almoÃ§o. Durante este intervalo, os trabalhadores podem ir Ã s compras de alimentos ou alimentos de conveniÃªncia, ou ir para lazer. Depois disso eles voltarÃ£o ao trabalho." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.disable_early_shop_leisure)), "Desativar compras ou lazer de madrugada" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.disable_early_shop_leisure)), $"No jogo Vanilla, os Cims podem fazer compras ou ir para lazer jÃ¡ Ã s 4 da manhÃ£. Esta opÃ§Ã£o mudarÃ¡ esse comportamento para comeÃ§ar por volta de 8h Ã s 10h." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.use_vanilla_timeoff)), "Desativar recurso de FÃ©rias e Feriados" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.use_vanilla_timeoff)), $"Desative o recurso de fÃ©rias e feriados e use o sistema de folga do jogo padrÃ£o. No jogo Vanilla, os cims tÃªm 60% de probabilidade de tirar uma folga todos os dias." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.use_school_vanilla_timeoff)), "Desativar recurso de fÃ©rias e feriados para escolas" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.use_school_vanilla_timeoff)), $"Desativar o recurso de fÃ©rias e feriados e usar o sistema de folga do jogo Vanilla para escolas. No jogo Vanilla, os cims tÃªm 60% de probabilidade de nÃ£o ir Ã  escola." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.holidays_per_year)), "NÃºmero de feriados por ano" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.holidays_per_year)), $"A maioria dos paÃ­ses devem ter um valor entre 10 e 15. O valor padrÃ£o Ã© razoÃ¡vel para a maioria dos paÃ­ses." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.vacation_per_year)), "NÃºmero de dias de fÃ©rias por ano" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.vacation_per_year)), $"NÃºmero de dias de fÃ©rias por ano â€“ sem incluir finais de semana. Para paÃ­ses com um mÃªs de fÃ©rias, como no Brasil, utilize 22. Para os EUA, um valor de 11 Ã© mais realista." },
                //{ m_Setting.GetOptionLabelLocaleID(nameof(Setting.school_vacation_per_year)), "NÃºmero de dias de fÃ©rias por ano" },
                //{ m_Setting.GetOptionDescLocaleID(nameof(Setting.school_vacation_per_year)), $"NÃºmero de dias de fÃ©rias por ano para escolas, faculdades e universidades. NÃ£o inclui finais de semana." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.school_vacation_month1)), "MÃªs de fÃ©rias 1:" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.school_vacation_month1)), $"MÃªs de fÃ©rias. As escolas estarÃ£o fechadas." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.school_vacation_month2)), "MÃªs de fÃ©rias 2:" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.school_vacation_month2)), $"MÃªs de fÃ©rias. As escolas estarÃ£o fechadas." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.dt_simulation)), "Selecione o comportamento da SimulaÃ§Ã£o DiÃ¡ria" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.dt_simulation)), $"Esta opÃ§Ã£o altera o funcionamento da simulaÃ§Ã£o durante um dia. O Dia PadrÃ£o corresponde ao comportamento Vanilla, que Ã© uma combinaÃ§Ã£o de dias da semana e finais de semana. Com as configuraÃ§Ãµes padrÃ£o de fÃ©rias/feriados (definidas na aba Compras e Lazer), em um Dia PadrÃ£o, cerca de 30% dos cims se comportarÃ£o como no fim de semana, realizando mais atividades de lazer e compras, enquanto o restante trabalharÃ¡ ou estudarÃ¡. A opÃ§Ã£o Dia de Semana aumentarÃ¡ as atividades de trabalho e estudo e diminuirÃ¡ o lazer e as compras. O fim de semana farÃ¡ o oposto. Nos fins de semana, as escolas estÃ£o fechadas. A Semana de 7 Dias irÃ¡ alternar entre dias de semana e finais de semana, indo de segunda a domingo." },
                //{ m_Setting.GetOptionLabelLocaleID(nameof(Setting.better_trucks)), "TrÃ¡fego de caminhÃµes mais realista" },
                //{ m_Setting.GetOptionDescLocaleID(nameof(Setting.better_trucks)), $"Auemnta o trÃ¡fego de caminhÃµess durante o inicio e metade do dia." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.school_start_time)), "HorÃ¡rio de inÃ­cio das escolas do ensino bÃ¡sico" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.school_start_time)), $"Hora de inÃ­cio para escolas do ensino bÃ¡sico." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.school_end_time)), "HorÃ¡rio de tÃ©rmino das escolas do ensino bÃ¡sico" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.school_end_time)), $"HorÃ¡rio de tÃ©rmino para escolas do ensino bÃ¡sico." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.high_school_start_time)), "HorÃ¡rio de inÃ­cio das escolas do ensino mÃ©dio" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.high_school_start_time)), $"Hora de inÃ­cio para escolas do ensino mÃ©dio." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.high_school_end_time)), "HorÃ¡rio de tÃ©rmino das escolas do ensino mÃ©dio" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.high_school_end_time)), $"HorÃ¡rio de tÃ©rmino para escolas do ensino mÃ©dio." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.univ_start_time)), "HorÃ¡rio de inÃ­cio de Universidades e Faculdades" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.univ_start_time)), $"Hora de inÃ­cio para Universidades e Faculdades." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.univ_end_time)), "HorÃ¡rio de tÃ©rmino de Universidades e Faculdades" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.univ_end_time)), $"HorÃ¡rio de tÃ©rmino para Universidades e Faculdades." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.work_start_time)), "HorÃ¡rio de inÃ­cio do turno diurno" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.work_start_time)), $"HorÃ¡rio de inÃ­cio do turno diurno de trabalho." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.work_end_time)), "HorÃ¡rio de tÃ©rmino do turno diurno" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.work_end_time)), $"HorÃ¡rio de tÃ©rmino do turno diurno de trabalho." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.avg_work_hours_ft_wd)), "MÃ©dia de horas trabalhadas para trabalhadores em tempo integral" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.avg_work_hours_ft_wd)), $"The average number of hours that full time workers worked on a weekday" },
                //{ m_Setting.GetOptionLabelLocaleID(nameof(Setting.avg_work_hours_ft_we)), "O nÃºmero mÃ©dio de horas que os trabalhadores em tempo integral trabalharam em um dia de semana" },
                //{ m_Setting.GetOptionDescLocaleID(nameof(Setting.avg_work_hours_ft_we)), $"The average number of hours that full time workers worked on a weekend" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.avg_work_hours_pt_wd)), "MÃ©dia de horas trabalhadas para trabalhadores em meio perÃ­odo" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.avg_work_hours_pt_wd)), $"O nÃºmero mÃ©dio de horas que os trabalhadores de meio perÃ­odo trabalharam em um dia de semana" },
                //{ m_Setting.GetOptionLabelLocaleID(nameof(Setting.avg_work_hours_pt_we)), "Fim de semana: MÃ©dia de horas trabalhadas para trabalhadores de meio perÃ­odo" },
                //{ m_Setting.GetOptionDescLocaleID(nameof(Setting.avg_work_hours_pt_we)), $"O nÃºmero mÃ©dio de horas que os trabalhadores de meio perÃ­odo trabalharam em um fim de semana" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.slow_time_factor)), "Fator de reduÃ§Ã£o da velocidade do tempo" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.slow_time_factor)), $"Esse fator vai reduzir a velocidade do tempo e aumentar a duraÃ§Ã£o do dia. Um fator de valor 1 nÃ£o vai ter efeito. Um fator de valor 2, por exemplo, vai dobrar a duraÃ§Ã£o do dia. Observe que a velocidade da simulaÃ§Ã£o nÃ£o serÃ¡ alterada. Outros sistemas que nÃ£o usados neste mod serÃ£o atualizados baseados na velocidade da simulaÃ§Ã£o e nÃ£o na duraÃ§Ã£o do dia." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.daysPerMonth)), "Dias por mÃªs" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.daysPerMonth)), $"Altera o nÃºmero de dias por mÃªs. O valor padrÃ£o Ã© 1." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.tourism_trips)), "VariaÃ§Ã£o de turistas por dia da semana." },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.tourism_trips)), $"Aumenta o nÃºmero de turistas no fim de semana e reduz nos dias de semana." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.commuter_trips)), "VariaÃ§Ã£o de trabalhadores externos por dia da semana." },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.commuter_trips)), $"Aumenta o nÃºmero de trabalhadores de conexÃµes externas nos dias de semana e reduz nos fim de semana. TambÃ©m aumenta a probabilidade de eles chegarem de aviÃ£o." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.service_expenses_night_reduction)), "ReduÃ§Ã£o de Custo Noturno" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.service_expenses_night_reduction)), $"Reduz os custos de serviÃ§os das 23h ate as 6h." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.trafficReduction)), "ReduÃ§Ã£o de TrÃ¡fego" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.trafficReduction)), $"Valores mais baixos aumentam o trÃ¡fego na cidade. O valor vanilla Ã© 5. Zero terÃ¡ a quantidade mÃ¡xima de trÃ¡fego." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.resourceConsumption)), "Consumo de Recursos" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.resourceConsumption)), $"Defina a quantidade de recursos que cada cidadÃ£o precisa consumir. O valor no modo fÃ¡cil Ã© 1 e no modo normal Ã© 20." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.shopping_log_enabled)), "Log de diagnÃ³stico de compras" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.shopping_log_enabled)), $"Registra totais horÃ¡rios de compras por recurso, incluindo viagens, quantidade, gasto, duraÃ§Ã£o mÃ©dia e distÃ¢ncia mÃ©dia. Use para calibraÃ§Ã£o; pode reduzir o desempenho enquanto estiver ativado." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.use_universal_mod_menu)), "Mostrar botao no Universal Mod Menu" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.use_universal_mod_menu)), $"Adiciona um botao do Realistic Trips ao Universal Mod Menu. Desativado por padrao. Recomenda-se reiniciar o jogo depois de alterar esta opcao." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.hospital_stay_duration_enabled)), "Ativar duracao de atendimento hospitalar" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.hospital_stay_duration_enabled)), $"Quando ativado, cims que chegam ao hospital ficam por uma visita curta ou, as vezes, uma internacao mais longa. Desativado por padrao." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.hospital_stay_inpatient_chance_pct)), "Chance de internacao" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.hospital_stay_inpatient_chance_pct)), $"Chance de um atendimento hospitalar virar uma internacao longa em vez de uma visita curta." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.hospital_short_stay_average_hours)), "Media da visita curta" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.hospital_short_stay_average_hours)), $"Duracao media de visitas curtas ao hospital, em horas do jogo." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.hospital_short_stay_stddev_hours)), "Variacao da visita curta" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.hospital_short_stay_stddev_hours)), $"Desvio padrao da duracao de visitas curtas, em horas do jogo." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.hospital_short_stay_minimum_hours)), "Minimo da visita curta" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.hospital_short_stay_minimum_hours)), $"Menor duracao sorteada para visita curta, em horas do jogo." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.hospital_short_stay_maximum_hours)), "Maximo da visita curta" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.hospital_short_stay_maximum_hours)), $"Maior duracao sorteada para visita curta, em horas do jogo." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.hospital_inpatient_average_hours)), "Media da internacao" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.hospital_inpatient_average_hours)), $"Duracao media de internacoes mais longas, em horas do jogo." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.hospital_inpatient_stddev_hours)), "Variacao da internacao" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.hospital_inpatient_stddev_hours)), $"Desvio padrao da duracao de internacoes, em horas do jogo." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.hospital_inpatient_minimum_hours)), "Internacao minima" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.hospital_inpatient_minimum_hours)), $"Menor duracao sorteada para internacao, em horas do jogo." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.hospital_inpatient_maximum_hours)), "Internacao maxima" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.hospital_inpatient_maximum_hours)), $"Maior duracao sorteada para internacao, em horas do jogo." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.min_attraction)), "AtraÃ§Ã£o MÃ­nima" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.min_attraction)), $"Aumentar ou diminuir esta configuraÃ§Ã£o alterarÃ¡ o nÃºmero de instalaÃ§Ãµesque podem hospedar eventos especiais." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.min_event_weekday)), "Segunda a Quinta" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.min_event_weekday)), $"NÃºmero mÃ­nimo de eventos de segunda a quinta" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.min_event_avg_day)), "Sexta" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.min_event_avg_day)), $"NÃºmero mÃ­nimo de eventos na sexta-feira" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.min_event_weekend)), "Fim de Semana" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.min_event_weekend)), $"NÃºmero mÃ­nimo de eventos no fim de semana" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.max_event_weekday)), "Segunda a Quinta" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.max_event_weekday)), $"NÃºmero mÃ¡ximo de eventos de segunda a quinta" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.max_event_avg_day)), "Sexta" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.max_event_avg_day)), $"NÃºmero mÃ¡ximo de eventos na sexta-feira" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.max_event_weekend)), "Fim de Semana" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.max_event_weekend)), $"NÃºmero mÃ¡ximo de eventos no fim de semana" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.avg_time_beverages)), "Bebidas" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.avg_time_beverages)), $"Tempo mÃ©dio em minutos para comprar bebidas." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.avg_time_chemicals)), "Produtos QuÃ­micos" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.avg_time_chemicals)), $"Tempo mÃ©dio em minutos para comprar produtos quÃ­micos." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.avg_time_convenienceFood)), "Comida Pronta" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.avg_time_convenienceFood)), $"Tempo mÃ©dio em minutos para comprar comida pronta." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.avg_time_electronics)), "EletrÃ´nicos" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.avg_time_electronics)), $"Tempo mÃ©dio em minutos para comprar eletrÃ´nicos." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.avg_time_software)), "Software" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.avg_time_software)), $"Tempo mÃ©dio em minutos para comprar software." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.avg_time_financial)), "ServiÃ§os Financeiros" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.avg_time_financial)), $"Tempo mÃ©dio em minutos para comprar serviÃ§os financeiros." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.avg_time_food)), "Alimentos" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.avg_time_food)), $"Tempo mÃ©dio em minutos para comprar alimentos." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.avg_time_furniture)), "MÃ³veis" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.avg_time_furniture)), $"Tempo mÃ©dio em minutos para comprar mÃ³veis." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.avg_time_meals)), "RefeiÃ§Ãµes" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.avg_time_meals)), $"Tempo mÃ©dio em minutos para comprar refeiÃ§Ãµes." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.avg_time_media)), "MÃ­dia" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.avg_time_media)), $"Tempo mÃ©dio em minutos para comprar mÃ­dia." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.avg_time_paper)), "Papel" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.avg_time_paper)), $"Tempo mÃ©dio em minutos para comprar produtos de papel." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.avg_time_petrochemicals)), "PetroquÃ­micos" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.avg_time_petrochemicals)), $"Tempo mÃ©dio em minutos para comprar petroquÃ­micos." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.avg_time_pharmaceuticals)), "Medicamentos" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.avg_time_pharmaceuticals)), $"Tempo mÃ©dio em minutos para comprar medicamentos." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.avg_time_plastics)), "PlÃ¡sticos" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.avg_time_plastics)), $"Tempo mÃ©dio em minutos para comprar plÃ¡sticos." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.avg_time_telecom)), "TelecomunicaÃ§Ãµes" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.avg_time_telecom)), $"Tempo mÃ©dio em minutos para comprar serviÃ§os de telecomunicaÃ§Ãµes." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.avg_time_textiles)), "TÃªxteis" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.avg_time_textiles)), $"Tempo mÃ©dio em minutos para comprar tÃªxteis." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.avg_time_recreation)), "RecreaÃ§Ã£o" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.avg_time_recreation)), $"Tempo mÃ©dio em minutos para comprar itens de recreaÃ§Ã£o." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.avg_time_entertainment)), "Entretenimento" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.avg_time_entertainment)), $"Tempo mÃ©dio em minutos para comprar entretenimento." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.avg_time_vehicles)), "VeÃ­culos" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.avg_time_vehicles)), $"Tempo mÃ©dio em minutos para comprar veÃ­culos." },
                //{ m_Setting.GetOptionLabelLocaleID(nameof(Setting.avg_time_hospital)), "Hospitais/ClÃ­nicas" },
                //{ m_Setting.GetOptionDescLocaleID(nameof(Setting.avg_time_hospital)), $"Tempo mÃ©dio em minutos gasto em hospitais ou clÃ­nicas." },
                //{ m_Setting.GetOptionLabelLocaleID(nameof(Setting.avg_time_prison)), "PrisÃµes/Cadeias" },
                //{ m_Setting.GetOptionDescLocaleID(nameof(Setting.avg_time_prison)), $"Tempo mÃ©dio em minutos gasto em prisÃµes ou cadeias." },



                { m_Setting.GetEnumValueLocaleID(Setting.DTSimulationEnum.AverageDay), "Dia PadrÃ£o" },
                { m_Setting.GetEnumValueLocaleID(Setting.DTSimulationEnum.Weekday), "Dia da Semana" },
                { m_Setting.GetEnumValueLocaleID(Setting.DTSimulationEnum.Saturday), "SÃ¡bado" },
                { m_Setting.GetEnumValueLocaleID(Setting.DTSimulationEnum.Sunday), "Sunday" },
                { m_Setting.GetEnumValueLocaleID(Setting.DTSimulationEnum.sevendayweek), "Semana (Segunda a Domingo)" },

                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.Performance), "Desempenho" },
                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.Balanced), "Balanceada" },
                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.USA), "Estados Unidos da AmÃ©rica" },
                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.UK), "Reino Unido" },
                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.Brazil), "Brasil" },
                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.Singapore), "Singapura" },
                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.Canada), "CanadÃ¡" },
                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.Phillipines), "Filipinas" },
                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.Netherlands), "Paises BaÃ­xos" },
                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.France), "FranÃ§a" },
                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.Germany), "Alemanha" },
                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.Poland), "PolÃ´nia" },
                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.Argentina), "Argentina" },
                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.Australia), "AustrÃ¡lia" },
                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.India), "Ãndia" },
                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.Italy), "ItÃ¡lia" },
                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.Japan), "JapÃ£o" },
                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.Mexico), "MÃ©xico" },
                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.SouthAfrica), "Ãfrica do Sul" },
                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.Spain), "Espanha" },

                { m_Setting.GetEnumValueLocaleID(Setting.timeEnum.t700), "7:00" },
                { m_Setting.GetEnumValueLocaleID(Setting.timeEnum.t730), "7:30" },
                { m_Setting.GetEnumValueLocaleID(Setting.timeEnum.t800), "8:00" },
                { m_Setting.GetEnumValueLocaleID(Setting.timeEnum.t830), "8:30" },
                { m_Setting.GetEnumValueLocaleID(Setting.timeEnum.t900), "9:00" },
                { m_Setting.GetEnumValueLocaleID(Setting.timeEnum.t930), "9:30" },
                { m_Setting.GetEnumValueLocaleID(Setting.timeEnum.t1000), "10:00" },
                { m_Setting.GetEnumValueLocaleID(Setting.timeEnum.t1030), "10:30" },
                { m_Setting.GetEnumValueLocaleID(Setting.timeEnum.t1100), "11:00" },
                { m_Setting.GetEnumValueLocaleID(Setting.timeEnum.t1130), "11:30" },
                { m_Setting.GetEnumValueLocaleID(Setting.timeEnum.t1200), "12:00" },
                { m_Setting.GetEnumValueLocaleID(Setting.timeEnum.t1230), "12:30" },
                { m_Setting.GetEnumValueLocaleID(Setting.timeEnum.t1300), "13:00" },
                { m_Setting.GetEnumValueLocaleID(Setting.timeEnum.t1330), "13:30" },
                { m_Setting.GetEnumValueLocaleID(Setting.timeEnum.t1400), "14:00" },
                { m_Setting.GetEnumValueLocaleID(Setting.timeEnum.t1430), "14:30" },
                { m_Setting.GetEnumValueLocaleID(Setting.timeEnum.t1500), "15:00" },
                { m_Setting.GetEnumValueLocaleID(Setting.timeEnum.t1530), "15:30" },
                { m_Setting.GetEnumValueLocaleID(Setting.timeEnum.t1600), "16:00" },
                { m_Setting.GetEnumValueLocaleID(Setting.timeEnum.t1630), "16:30" },
                { m_Setting.GetEnumValueLocaleID(Setting.timeEnum.t1700), "17:00" },
                { m_Setting.GetEnumValueLocaleID(Setting.timeEnum.t1730), "17:30" },
                { m_Setting.GetEnumValueLocaleID(Setting.timeEnum.t1800), "18:00" },
                { m_Setting.GetEnumValueLocaleID(Setting.timeEnum.t1830), "18:30" },
                { m_Setting.GetEnumValueLocaleID(Setting.timeEnum.t1900), "19:00" },
                { m_Setting.GetEnumValueLocaleID(Setting.timeEnum.t1930), "19:30" },
                { m_Setting.GetEnumValueLocaleID(Setting.timeEnum.t2000), "20:00" },

                { m_Setting.GetEnumValueLocaleID(Setting.months.January), "Jan" },
                { m_Setting.GetEnumValueLocaleID(Setting.months.February), "Fev" },
                { m_Setting.GetEnumValueLocaleID(Setting.months.March), "Mar" },
                { m_Setting.GetEnumValueLocaleID(Setting.months.April), "Abr" },
                { m_Setting.GetEnumValueLocaleID(Setting.months.May), "Mai" },
                { m_Setting.GetEnumValueLocaleID(Setting.months.June), "Jun" },
                { m_Setting.GetEnumValueLocaleID(Setting.months.July), "Jul" },
                { m_Setting.GetEnumValueLocaleID(Setting.months.August), "Ago" },
                { m_Setting.GetEnumValueLocaleID(Setting.months.September), "Set" },
                { m_Setting.GetEnumValueLocaleID(Setting.months.October), "Out" },
                { m_Setting.GetEnumValueLocaleID(Setting.months.November), "Nov" },
                { m_Setting.GetEnumValueLocaleID(Setting.months.December), "Dez" },

                { m_Setting.GetEnumValueLocaleID(Setting.dayOfWeek.Sunday), "Dom" },
                { m_Setting.GetEnumValueLocaleID(Setting.dayOfWeek.Monday), "Seg" },
                { m_Setting.GetEnumValueLocaleID(Setting.dayOfWeek.Tuesday), "Ter" },
                { m_Setting.GetEnumValueLocaleID(Setting.dayOfWeek.Wednesday), "Qua" },
                { m_Setting.GetEnumValueLocaleID(Setting.dayOfWeek.Thursday), "Qui" },
                { m_Setting.GetEnumValueLocaleID(Setting.dayOfWeek.Friday), "Sex" },
                { m_Setting.GetEnumValueLocaleID(Setting.dayOfWeek.Saturday), "Sab" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.date_format)), "Formato da data na interface" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.date_format)), "Escolha como o dia da semana e a data serÃ£o exibidos na interface superior." },

                { m_Setting.GetEnumValueLocaleID(Setting.DateFormatEnum.DayOfWeek_Month_Year), "Dia da Semana, MÃªs Ano" },
                { m_Setting.GetEnumValueLocaleID(Setting.DateFormatEnum.DayOfWeek_DDMMYYYY), "Dia da Semana, DD/MM/AAAA" },
                { m_Setting.GetEnumValueLocaleID(Setting.DateFormatEnum.DayOfWeek_MMDDYYYY), "Dia da Semana, MM/DD/AAAA" },

                { "t2w.chirp.special_event.today",   "Evento especial hoje das {start} Ã s {end} em" },
                { "t2w.chirp.special_event.attendees",   "{attendees} cidadÃµes compareceram ao evento especial em" },
                { "t2w.chirp.special_event.starting","Evento especial comeÃ§ando em breve em" },
                { "t2w.chirp.special_event.ending",  "Evento especial terminando em breve em" },
                { "t2w.chirp.holiday.new_year",      "Feliz Ano Novo!" },
                { "t2w.chirp.mod_name",      "Mod Realistic Trips" },
            };
            }

            public void Unload()
            {

            }
        }
    }
}
