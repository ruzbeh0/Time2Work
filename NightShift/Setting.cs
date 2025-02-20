using Colossal;
using Colossal.IO.AssetDatabase;
using Colossal.IO.AssetDatabase.Internal;
using Game;
using Game.Modding;
using Game.Prefabs;
using Game.Settings;
using Game.UI;
using Game.UI.Widgets;
using System;
using System.Collections.Generic;
using Time2Work.Systems;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;

namespace Time2Work
{
    //[FileLocation(nameof(Time2Work))]
    [FileLocation($"ModsSettings\\{nameof(Time2Work)}\\{nameof(Time2Work)}")]
    [SettingsUIGroupOrder(SettingsGroup, DelayGroup, WorkPlaceShiftGroup, NonDayShiftByWorkTypeGroup, RemoteGroup, DayShiftGroup, ResetGroup, ShopLeisureGroup, TimeOffGroup, LeisureMealsGroup, LeisureEntertainmentGroup, LeisureShoppingGroup, LeisureParksGroup, LeisureTravelGroup, SchoolTimeOffGroup, SchoolTimeGroup, School1WeekGroup, School2WeekGroup, School34WeekGroup, TimeOffGroup, DTSimulationGroup, SlowerTimeGroup, WeekGroup, EventGroup, MinEventGroup, MaxEventGroup, OfficeGroup, CommercialGroup, IndustryGroup, CityServicesGroup, ExternalGroup, ExpensesGroup, TrucksGroup, OtherGroup)]
    [SettingsUIShowGroupName(WorkPlaceShiftGroup, NonDayShiftByWorkTypeGroup, RemoteGroup, DayShiftGroup, SchoolTimeOffGroup, SchoolTimeGroup, TimeOffGroup, LeisureMealsGroup, LeisureEntertainmentGroup, LeisureShoppingGroup, LeisureParksGroup, LeisureTravelGroup, DTSimulationGroup, SlowerTimeGroup, School1WeekGroup, School2WeekGroup, School34WeekGroup, WeekGroup, OfficeGroup, CommercialGroup, IndustryGroup, CityServicesGroup, TrucksGroup, OtherGroup, ExternalGroup, ExpensesGroup, MinEventGroup, MaxEventGroup)]
    public class Setting : ModSetting
    {
        public const string SettingsSection = "Settings";
        public const string WorkSection = "Work";
        public const string ShopLeisureSection = "Shopping and Leisure";
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

        Dictionary<int, int> countryIndexLookup = new Dictionary<int, int>();
        int[] evening_share_ = [10, 17, 13, 5, 19, 15, 31, 13, 16, 17, 8];
        int[] night_share_ = [8, 8, 7, 2, 12, 5, 8, 7, 11, 9, 4];
        int[] delay_factor_ = [2, 4, 2, 2, 2, 2, 2, 2, 2, 2, 2];
        int[] lunch_break_percentage_ = [30, 10, 30, 30, 30, 30, 30, 30, 30, 30, 30];
        float[] holidays_per_year_ = [11, 11, 13, 10, 11, 11, 7, 40, 13, 8, 11];
        float[] vacation_per_year_ = [22, 22, 30, 21, 30, 28, 25, 15, 26, 39, 22];
        bool[] disable_early_shop_leisure_ = [true, false, true, true, true, true, true, true, true, true, true];
        bool[] use_vanilla_timeoff_ = [false, true, false, false, false, false, false, false, false, false, false];
        bool[] use_school_vanilla_timeoff_ = [false, true, false, false, false, false, false, false, false, false, false];
        int[] school_start_time_ = [2, 2, 1, 2, 3, 2, 2, 0, 2, 3, 2];
        int[] school_end_time_ = [16, 16, 9, 16, 19, 12, 16, 19, 16, 16, 16];
        int[] high_school_start_time_ = [2, 2, 1, 4, 2, 2, 2, 0, 2, 3, 2];
        int[] high_school_end_time_ = [16, 16, 12, 18, 20, 16, 16, 21, 16, 16, 16];
        int[] univ_start_time_ = [2, 2, 2, 2, 4, 2, 2, 1, 2, 2, 2];
        int[] univ_end_time_ = [20, 20, 21, 22, 22, 22, 20, 26, 18, 20, 20];
        int[] work_start_time_ = [4, 4, 4, 3, 4, 4, 4, 4, 2, 3, 4];
        int[] work_end_time_ = [20, 20, 22, 20, 20, 20, 20, 22, 18, 19, 20];
        int[] dt_simulation_ = [0, 0, 4, 4, 4, 4, 4, 4, 4, 4, 4];
        float[] avg_work_hours_ft_wd_ = [8.4f, 8.4f, 8.8f, 7.5f, 7.8f, 7.6f, 8.4f, 9.2f, 8f, 7.3f, 8.4f];
        float[] avg_work_hours_pt_wd_ = [5.3f, 5.3f, 5.3f, 4f, 4.7f, 3.6f, 5.3f, 5f, 6f, 3.3f, 5.3f];
        float[] slow_time_factor_ = [1f, 3.5f, 3.5f, 3.5f, 3.5f, 3.5f, 3.5f, 3.5f, 3.5f, 3.5f, 3.5f];
        int[] part_time_percentage_ = [22, 22, 8, 18, 18, 30, 48, 60, 6, 24, 17];
        int[] remote_percentage_ = [20, 20, 10, 20, 15, 13, 32, 55, 8, 27, 14];
        bool[] night_trucks_ = [true, true, true, true, true, true, true, true, true, true, true];
        bool[] peak_spread_ = [true, true, true, true, true, true, true, true, true, true, true];
        bool[] tourism_trips_ = [true, true, true, true, true, true, true, true, true, true, true];
        bool[] commuter_trips_ = [true, true, true, true, true, true, true, true, true, true, true];
        int[] service_expenses_night_reduction_ = [30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30];
        int[] office_weekday_pct_ = [90, 90, 90, 90, 90, 90, 90, 90, 95, 90, 90];
        int[] office_avgday_pct_ = [88, 88, 88, 88, 88, 88, 88, 88, 90, 88, 88];
        int[] office_sat_pct_ = [12, 12, 12, 12, 12, 12, 12, 12, 10, 12, 12];
        int[] office_sun_pct_ = [6, 6, 6, 6, 6, 6, 6, 6, 2, 6, 6];
        int[] commercial_weekday_pct_ = [64, 64, 64, 64, 64, 64, 64, 64, 90, 64, 64];
        int[] commercial_avgday_pct_ = [68, 68, 68, 68, 68, 68, 68, 68, 85, 68, 68];
        int[] commercial_sat_pct_ = [37, 37, 37, 37, 37, 37, 37, 37, 70, 37, 37];
        int[] commercial_sun_pct_ = [26, 26, 26, 26, 26, 26, 26, 26, 15, 26, 26];
        int[] industry_weekday_pct_ = [90, 90, 90, 90, 90, 90, 90, 90, 90, 90, 90];
        int[] industry_avgday_pct_ = [86, 86, 86, 86, 86, 86, 86, 86, 80, 86, 86];
        int[] industry_sat_pct_ = [24, 24, 24, 24, 24, 24, 24, 24, 30, 24, 24];
        int[] industry_sun_pct_ = [11, 11, 11, 11, 11, 11, 11, 11, 10, 11, 11];
        int[] cityServices_weekday_pct_ = [80, 80, 80, 80, 80, 80, 80, 80, 95, 80, 80];
        int[] cityServices_avgday_pct_ = [78, 78, 78, 78, 78, 78, 78, 78, 90, 78, 78];
        int[] cityServices_sat_pct_ = [17, 17, 17, 17, 17, 17, 17, 17, 60, 17, 17];
        int[] cityServices_sun_pct_ = [12, 12, 12, 12, 12, 12, 12, 12, 40, 12, 12];
        int[] nonday_office_share_ = [7, 7, 7, 13, 7, 7, 7, 7, 5, 3, 7];
        int[] nonday_commercial_share_ = [31, 31, 31, 24, 31, 31, 31, 31, 15, 8, 31];
        int[] nonday_industry_share_ = [14, 14, 14, 14, 14, 14, 14, 14, 25, 15, 14];
        int[] nonday_cityservices_share_ = [18, 18, 18, 7, 18, 18, 18, 18, 50, 8, 18];
        int[] school_lv1_weekday_pct_ = [93, 93, 84, 98, 98, 98, 93, 98, 97, 93, 93];
        int[] school_lv1_avgday_pct_ = [92, 92, 83, 97, 97, 97, 92, 97, 95, 92, 92];
        int[] school_lv1_saturday_pct_ = [0, 0, 0, 1, 0, 0, 0, 2, 0, 0, 0];
        int[] school_lv1_sunday_pct_ = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
        int[] school_lv2_weekday_pct_ = [90, 90, 81, 93, 98, 98, 90, 97, 90, 90, 90];
        int[] school_lv2_avgday_pct_ = [88, 88, 79, 88, 97, 97, 88, 95, 85, 88, 88];
        int[] school_lv2_saturday_pct_ = [0, 0, 0, 2, 0, 0, 0, 2, 0, 0, 0];
        int[] school_lv2_sunday_pct_ = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
        int[] school_lv34_weekday_pct_ = [80, 80, 72, 85, 90, 90, 80, 90, 80, 80, 80];
        int[] school_lv34_avgday_pct_ = [70, 70, 63, 70, 80, 80, 70, 80, 70, 70, 70];
        int[] school_lv34_saturday_pct_ = [5, 5, 5, 10, 5, 5, 5, 15, 20, 5, 5];
        int[] school_lv34_sunday_pct_ = [0, 0, 0, 4, 0, 0, 0, 5, 10, 0, 0];
        int[] school_vacation_month1_ = [7, 7, 1, 7, 7, 7, 7, 4, 6, 7, 7];
        int[] school_vacation_month2_ = [8, 8, 7, 8, 8, 8, 8, 5, 7, 8, 8];
        float[] meals_weekday_ = [1.15f, 1.15f, 1.15f, 1.25f, 1.15f, 1.15f, 1.15f, 1.15f, 1.15f, 1.15f, 1.15f];
        float[] meals_avgday_ = [1.2f, 1.2f, 1.2f, 1.39f, 1.2f, 1.2f, 1.2f, 1.2f, 1.2f, 1.2f, 1.2f];
        float[] meals_saturday_ = [1.32f, 1.32f, 1.32f, 1.67f, 1.32f, 1.32f, 1.32f, 1.32f, 1.32f, 1.32f, 1.32f];
        float[] meals_sunday_ = [1.32f, 1.32f, 1.32f, 1.75f, 1.32f, 1.32f, 1.32f, 1.32f, 1.32f, 1.32f, 1.32f];
        float[] entertainment_weekday_ = [0.61f, 0.61f, 0.61f, 0.93f, 0.61f, 0.61f, 0.61f, 0.61f, 0.61f, 0.61f, 0.61f];
        float[] entertainment_avgday_ = [0.76f, 0.76f, 0.76f, 1.11f, 0.76f, 0.76f, 0.76f, 0.76f, 0.76f, 0.76f, 0.76f];
        float[] entertainment_saturday_ = [1.15f, 1.15f, 1.15f, 1.48f, 1.15f, 1.15f, 1.15f, 1.15f, 1.15f, 1.15f, 1.15f];
        float[] entertainment_sunday_ = [1.15f, 1.15f, 1.15f, 1.48f, 1.15f, 1.15f, 1.15f, 1.15f, 1.15f, 1.15f, 1.15f];
        float[] shopping_weekday_ = [0.24f, 0.24f, 0.24f, 0.3f, 0.24f, 0.24f, 0.24f, 0.24f, 0.24f, 0.24f, 0.24f];
        float[] shopping_avgday_ = [1.41f, 1.41f, 1.41f, 0.85f, 1.41f, 1.41f, 1.41f, 1.41f, 1.41f, 1.41f, 1.41f];
        float[] shopping_saturday_ = [1.68f, 1.68f, 1.68f, 1f, 1.68f, 1.68f, 1.68f, 1.68f, 1.68f, 1.68f, 1.68f];
        float[] shopping_sunday_ = [0.53f, 0.53f, 0.53f, 0.7f, 0.53f, 0.53f, 0.53f, 0.53f, 0.53f, 0.53f, 0.53f];
        float[] park_weekday_ = [0.3f, 0.3f, 0.3f, 0.31f, 0.3f, 0.3f, 0.3f, 0.3f, 0.3f, 0.3f, 0.3f];
        float[] park_avgday_ = [0.31f, 0.31f, 0.31f, 0.38f, 0.31f, 0.31f, 0.31f, 0.31f, 0.31f, 0.31f, 0.31f];
        float[] park_saturday_ = [0.35f, 0.35f, 0.35f, 0.52f, 0.35f, 0.35f, 0.35f, 0.35f, 0.35f, 0.35f, 0.35f];
        float[] park_sunday_ = [0.35f, 0.35f, 0.35f, 0.52f, 0.35f, 0.35f, 0.35f, 0.35f, 0.35f, 0.35f, 0.35f];
        float[] travel_weekday_ = [0.04f, 0.04f, 0.04f, 0.04f, 0.04f, 0.04f, 0.04f, 0.04f, 0.04f, 0.04f, 0.04f];
        float[] travel_avgday_ = [0.04f, 0.04f, 0.04f, 0.04f, 0.04f, 0.04f, 0.04f, 0.04f, 0.04f, 0.04f, 0.04f];
        float[] travel_saturday_ = [0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f];
        float[] travel_sunday_ = [0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f];
        int[] traffic_reduction_ = [5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];


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
            night_trucks = night_trucks_[index];
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
        }

        public override void Apply()
        {
            base.Apply();
            var system1 = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<WorkPlaceShiftUpdateSystem>();
            system1.Enabled = true;
            var system2 = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<WorkerShiftUpdateSystem>();
            system2.Enabled = true;
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

        [SettingsUISection(OtherSection, TrucksGroup)]
        public bool night_trucks { get; set; }

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
        public enum DTSimulationEnum
        {
            AverageDay,
            Weekday,
            Saturday,
            Sunday,
            sevendayweek
        }
        public enum SettingsEnum
        {
            Balanced = 0,
            Performance = 1,
            Brazil = 25,
            Canada = 34,
            France = 62,
            Germany = 66,
            Netherlands = 124,
            Phillipines = 140,
            Poland = 141,
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
                { m_Setting.GetOptionTabLocaleID(Setting.ShopLeisureSection), "Leisure" },
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

                //{ m_Setting.GetOptionLabelLocaleID(nameof(Setting.WeekText)), $"Percentage of Workers per Day" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DTText)), $"Changing the parameters below require restarting the game." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.MultilineText)), $"WARNING: Changing the slow time factor for existing cities will cause cim's age to change which can cause issues in the game. If the factor is changed, the population age distribution will balance itself over time." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.settings_choice)), "Mod settings" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.settings_choice)), $"Change all mod settings. Performance: will update the settings to improve performance, this is similar to the Vanilla game. Balanced: has most of the features from this mod enabled, but a few of them that have high impact on performance are disabled. Country based settings: real world data was collected for a few countries, selecting one of them will make the game more realistic but it might impact performance." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.Button)), "Confirm" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.Button)), $"Confirm new settings" },

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
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.night_trucks)), "More realistic truck traffic" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.night_trucks)), $"Reduces truck traffic during the day and increases it at night and early morning." },
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

                { m_Setting.GetEnumValueLocaleID(Setting.DTSimulationEnum.AverageDay), "Average Day" },
                { m_Setting.GetEnumValueLocaleID(Setting.DTSimulationEnum.Weekday), "Weekday" },
                { m_Setting.GetEnumValueLocaleID(Setting.DTSimulationEnum.Saturday), "Saturday" },
                { m_Setting.GetEnumValueLocaleID(Setting.DTSimulationEnum.Sunday), "Sunday" },
                { m_Setting.GetEnumValueLocaleID(Setting.DTSimulationEnum.sevendayweek), "7 Days Week (Monday to Sunday)" },

                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.Performance), "Performance" },
                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.Balanced), "Balanced" },
                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.USA), "United States of America" },
                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.UK), "United Kingdom" },
                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.Brazil), "Brazil" },
                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.Canada), "Canada" },
                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.Phillipines), "Phillipines" },
                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.Netherlands), "Netherlands" },
                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.France), "France" },
                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.Germany), "Germany" },
                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.Poland), "Poland" },

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
                { m_Setting.GetEnumValueLocaleID(Setting.dayOfWeek.Saturday), "Sat" }
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
                { m_Setting.GetOptionTabLocaleID(Setting.SettingsSection), "Configurações" },
                { m_Setting.GetOptionTabLocaleID(Setting.WorkSection), "Emprego" },
                { m_Setting.GetOptionTabLocaleID(Setting.ShopLeisureSection), "Lazer" },
                { m_Setting.GetOptionTabLocaleID(Setting.SchoolSection), "Escola" },
                { m_Setting.GetOptionTabLocaleID(Setting.Weeksection), "Semana" },
                { m_Setting.GetOptionTabLocaleID(Setting.EventSection), "Eventos Especiais" },
                { m_Setting.GetOptionTabLocaleID(Setting.OtherSection), "Outros" },

                { m_Setting.GetOptionGroupLocaleID(Setting.MinEventGroup), "Número mínimo de eventos por dia da semana" },
                { m_Setting.GetOptionGroupLocaleID(Setting.MaxEventGroup), "Número máximo de eventos por dia da semana" },
                { m_Setting.GetOptionGroupLocaleID(Setting.WorkPlaceShiftGroup), "Alterar a porcentagem de turnos vespertinos e noturnos." },
                { m_Setting.GetOptionGroupLocaleID(Setting.NonDayShiftByWorkTypeGroup), "Alterar a porcentagem de turnos vespertinos e noturnos por tipo de empregos." },
                { m_Setting.GetOptionGroupLocaleID(Setting.RemoteGroup), "Configurações de Home Office" },
                { m_Setting.GetOptionGroupLocaleID(Setting.TimeOffGroup), "Configurações de férias e feriados" },
                { m_Setting.GetOptionGroupLocaleID(Setting.DayShiftGroup), "Configurações do turno diurno."},
                { m_Setting.GetOptionGroupLocaleID(Setting.LeisureMealsGroup), "Refeições: Média de horas de lazer por dia" },
                { m_Setting.GetOptionGroupLocaleID(Setting.LeisureEntertainmentGroup), "Entretenimento: Avg. Hours for for Leisure per day" },
                { m_Setting.GetOptionGroupLocaleID(Setting.LeisureShoppingGroup), "Compras: de horas de lazer por dia" },
                { m_Setting.GetOptionGroupLocaleID(Setting.LeisureParksGroup), "Parques: Média de horas de lazer por dia" },
                { m_Setting.GetOptionGroupLocaleID(Setting.LeisureTravelGroup), "Viagens: Média de horas de lazer por dia" },
                { m_Setting.GetOptionGroupLocaleID(Setting.SchoolTimeOffGroup), "Configurações de férias escolares" },
                { m_Setting.GetOptionGroupLocaleID(Setting.SchoolTimeGroup), "Configurações de horário de início/término das aulas nas escolas" },
                { m_Setting.GetOptionGroupLocaleID(Setting.School1WeekGroup), "Frequência escolar elementar por dia" },
                { m_Setting.GetOptionGroupLocaleID(Setting.School2WeekGroup), "Frequência do ensino médio por dia" },
                { m_Setting.GetOptionGroupLocaleID(Setting.School34WeekGroup), "Frequência da faculade e universidade por dia" },
                { m_Setting.GetOptionGroupLocaleID(Setting.SlowerTimeGroup), "Configurações do Dia e do Tempo" },
                { m_Setting.GetOptionGroupLocaleID(Setting.DTSimulationGroup), "Tipo de Simulação Diária" },
                { m_Setting.GetOptionGroupLocaleID(Setting.TrucksGroup), "Caminhões" },
                { m_Setting.GetOptionGroupLocaleID(Setting.OtherGroup), "Outros" },
                { m_Setting.GetOptionGroupLocaleID(Setting.ExternalGroup), "Viagens Externas" },
                { m_Setting.GetOptionGroupLocaleID(Setting.ExpensesGroup), "Gastos com Serviços" },
                { m_Setting.GetOptionGroupLocaleID(Setting.WeekGroup), "Porcentagem de Trabalhadores por Dia" },
                { m_Setting.GetOptionGroupLocaleID(Setting.OfficeGroup), "Escritório - Porcentagem de Trabalhadores por Dia" },
                { m_Setting.GetOptionGroupLocaleID(Setting.CommercialGroup), "Comércio - Porcentagem de Trabalhadores por Dia" },
                { m_Setting.GetOptionGroupLocaleID(Setting.IndustryGroup), "Indústria - Porcentagem de Trabalhadores por Dia" },
                { m_Setting.GetOptionGroupLocaleID(Setting.CityServicesGroup), "Serviços Públicos - Porcentagem de Trabalhadores por Dia" },


                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.meals_weekday)), "Segunda a Quinta" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.meals_weekday)), $"Média de horas que uma pessoa gasta saindo para comer por dia." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.meals_avgday)), "Sexta" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.meals_avgday)), $"Média de horas que uma pessoa gasta saindo para comer por dia." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.meals_saturday)), "Sábado" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.meals_saturday)), $"Média de horas que uma pessoa gasta saindo para comer por dia." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.meals_sunday)), "Domingo" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.meals_sunday)), $"Média de horas que uma pessoa gasta saindo para comer por dia." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.entertainment_weekday)), "Segunda a Quinta" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.entertainment_weekday)), $"Média de horas que uma pessoa gasta saindo para se divertir por dia." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.entertainment_avgday)), "Sexta" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.entertainment_avgday)), $"Média de horas que uma pessoa gasta saindo para se divertir por dia." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.entertainment_saturday)), "Sábado" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.entertainment_saturday)), $"Média de horas que uma pessoa gasta saindo para se divertir por dia." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.entertainment_sunday)), "Domingo" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.entertainment_sunday)), $"Média de horas que uma pessoa gasta saindo para se divertir por dia." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.shopping_weekday)), "Segunda a Quinta" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.shopping_weekday)), $"Média de horas que uma pessoa gasta saindo para fazer compras por dia." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.shopping_avgday)), "Sexta" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.shopping_avgday)), $"Média de horas que uma pessoa gasta saindo para fazer compras por dia." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.shopping_saturday)), "Sábado" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.shopping_saturday)), $"Média de horas que uma pessoa gasta saindo para fazer compras por dia." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.shopping_sunday)), "Domingo" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.shopping_sunday)), $"Média de horas que uma pessoa gasta saindo para fazer compras por dia." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.park_weekday)), "Segunda a Quinta" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.park_weekday)), $"Média de horas que uma pessoa gasta indo a parques por dia." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.park_avgday)), "Sexta" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.park_avgday)), $"Média de horas que uma pessoa gasta indo a parques por dia." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.park_saturday)), "Sábado" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.park_saturday)), $"Média de horas que uma pessoa gasta indo a parques por dia." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.park_sunday)), "Domingo" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.park_sunday)), $"Média de horas que uma pessoa gasta indo a parques por dia." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.travel_weekday)), "Segunda a Quinta" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.travel_weekday)), $"Média de horas que uma pessoa gasta viajando por dia." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.travel_avgday)), "Sexta" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.travel_avgday)), $"Média de horas que uma pessoa gasta viajando por dia." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.travel_saturday)), "Sábado" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.travel_saturday)), $"Média de horas que uma pessoa gasta viajando por dia." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.travel_sunday)), "Sunday" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.travel_sunday)), $"Média de horas que uma pessoa gasta viajando por dia." },
                //{ m_Setting.GetOptionLabelLocaleID(nameof(Setting.WeekText)), $"Porcentagem de Trabalhadores por Dia" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DTText)), $"Alterar os parametros abaixo requer reinício do jogo." },
                //{ m_Setting.GetOptionLabelLocaleID(nameof(Setting.MultilineText)), $"AVISO: O recurso de Tempo mais Lento pode causar problemas com os mods Population Rebalance e Info Loom - em uma cidade existente. Uma nova cidade provavelmente não terá problemas com esses mods." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.settings_choice)), $"Alterar as configurações do mod" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.settings_choice)), $"Alterar todas as configurações. Desempenho: irá atualizar as configurações para melhorar o desempenho, isso é semelhante ao jogo Vanilla. Balanceado: tem a maioria dos recursos deste mod habilitados, mas alguns deles que têm alto impacto no desempenho estão desabilitadas. Configurações baseadas em um país: dados do mundo real foram coletados para alguns países. Selecionar um deles tornará o jogo mais realista, mas pode afetar o desempenho do jogo." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.Button)), "Confirmar" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.Button)), $"Confirmar novas configurações" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.school_lv1_weekday_pct)), "Segunda a Quinta" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.school_lv1_weekday_pct)), $"Porcentagem de alunos que vão à escola de segunda a quinta" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.school_lv1_avgday_pct)), "Sexta" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.school_lv1_avgday_pct)), $"Porcentagem de alunos que vão à escola na sexta" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.school_lv1_saturday_pct)), "Sábado" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.school_lv1_saturday_pct)), $"Porcentagem de alunos que vão à escola no sábado" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.school_lv1_sunday_pct)), "Domingo" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.school_lv1_sunday_pct)), $"Porcentagem de alunos que vão à escola no domingo" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.school_lv2_weekday_pct)), "Segunda a Quinta" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.school_lv2_weekday_pct)), $"Porcentagem de alunos que vão à escola de segunda a quinta" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.school_lv2_avgday_pct)), "Sexta" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.school_lv2_avgday_pct)), $"Porcentagem de alunos que vão à escola na sexta" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.school_lv2_saturday_pct)), "Sábado" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.school_lv2_saturday_pct)), $"Porcentagem de alunos que vão à escola no sábado" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.school_lv2_sunday_pct)), "Domingo" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.school_lv2_sunday_pct)), $"Porcentagem de alunos que vão à escola no domingo" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.school_lv34_weekday_pct)), "Segunda a Quinta" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.school_lv34_weekday_pct)), $"Porcentagem de alunos que vão à Faculdade ou Universidade de segunda a quinta" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.school_lv34_avgday_pct)), "Sexta" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.school_lv34_avgday_pct)), $"Porcentagem de alunos que vão à Faculdade ou Universidade  na sexta" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.school_lv34_saturday_pct)), "Sábado" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.school_lv34_saturday_pct)), $"Porcentagem de alunos que vão à Faculdade ou Universidade  no sábado" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.school_lv34_sunday_pct)), "Domingo" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.school_lv34_sunday_pct)), $"Porcentagem de alunos que vão à Faculdade ou Universidade  no domingo" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.office_weekday_pct)), "Segunda a Quinta" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.office_weekday_pct)), $"Porcentagem dos trabalhadores que trabalham de segunda a quinta" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.office_avgday_pct)), "Sexta" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.office_avgday_pct)), $"Porcentagem dos trabalhadores que trabalham na sexta" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.office_sat_pct)), "Sábado" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.office_sat_pct)), $"Porcentagem dos trabalhadores que trabalham no sábado" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.office_sun_pct)), "Domingo" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.office_sun_pct)), $"Porcentagem dos trabalhadores que trabalham no domingo" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.industry_weekday_pct)), "Segunda a Quinta" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.industry_weekday_pct)), $"Porcentagem dos trabalhadores que trabalham de segunda a quinta" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.industry_avgday_pct)), "Sexta" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.industry_avgday_pct)), $"Porcentagem dos trabalhadores que trabalham na sexta" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.industry_sat_pct)), "Sábado" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.industry_sat_pct)), $"Porcentagem dos trabalhadores que trabalham  no sábado" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.industry_sun_pct)), "Domingo" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.industry_sun_pct)), $"Porcentagem dos trabalhadores que trabalham no domingo" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.commercial_weekday_pct)), "Segunda a Quinta" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.commercial_weekday_pct)), $"Porcentagem dos trabalhadores que trabalham de segunda a quinta" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.commercial_avgday_pct)), "Sexta" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.commercial_avgday_pct)), $"Porcentagem dos trabalhadores que trabalham na sexta" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.commercial_sat_pct)), "Sábado" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.commercial_sat_pct)), $"Porcentagem dos trabalhadores que trabalham no sábado" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.commercial_sun_pct)), "Domingo" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.commercial_sun_pct)), $"Porcentagem dos trabalhadores que trabalham no domingo" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.cityServices_weekday_pct)), "Segunda a Quinta" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.cityServices_weekday_pct)), $"Porcentagem dos trabalhadores que trabalham de segunda a quinta" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.cityServices_avgday_pct)), "Sexta" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.cityServices_avgday_pct)), $"Porcentagem dos trabalhadores que trabalham na sexta" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.cityServices_sat_pct)), "Sábado" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.cityServices_sat_pct)), $"Porcentagem dos trabalhadores que trabalham no sábado" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.cityServices_sun_pct)), "Domingo" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.cityServices_sun_pct)), $"Porcentagem dos trabalhadores que trabalham no domingo" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.nonday_office_share)), "Escritório" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.nonday_office_share)), $"Porcentagem para locais de trabalho vespertinos e noturno." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.nonday_commercial_share)), "Comércio" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.nonday_commercial_share)), $"Porcentagem para locais de trabalho vespertinos e noturno." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.nonday_industry_share)), "Indústria" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.nonday_industry_share)), $"Porcentagem para locais de trabalho vespertinos e noturno." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.nonday_cityservices_share)), "Serviços Públicos" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.nonday_cityservices_share)), $"Porcentagem para locais de trabalho vespertinos e noturno." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.evening_share)), "Tarde" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.evening_share)), $"Porcentagem para locais de trabalho vespertinos" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.night_share)), "Noite" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.night_share)), $"Porcentagem para locais de trabalho noturnos" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.remote_percentage)), "Porcentagem de trabalhadores que fazem Home Office" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.remote_percentage)), $"Porcentagem de trabalhadores que trabalham em casa. Estes funcionários também tem intervalo para almoço. Apenas se aplica a trabalhadores de escritório e serviços públicos" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.part_time_percentage)), "Porcentagem de trabalhadores de meio período" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.part_time_percentage)), $"Porcentagem de trabalhadores do turno diurno que trabalham meio período. Estes funcionários trabalham ou de manhã ou de tarde. Eles não tem horário de almoço e um valor mais alto vai aumentar as viagens durante o meio do dia e diminuir os picos dos horários de rush." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.delay_factor)), "Fator de chegada/saída atrasada ou antecipada" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.delay_factor)), $"Este fator ajustará a variação dos horários de chegada e saída do trabalho. Um fator mais alto aumentará a variação na chegada e saída do trabalho - o que significa que mais cims não chegarão ao trabalho na hora certa ou trabalharão por mais horas. Um valor zero desativará essa funcionalidade. Observe que os efeitos desta opção nos horários de pico da manhã e da tarde são diferentes: de manhã há igual probabilidade de chegar cedo ou mais tarde, porém, à tarde a probabilidade de sair atrasado é maior do que de sair mais cedo. Isso foi implementado desta forma para simular melhor as diferenças entre o deslocamento matinal e vespertino em relação ao mundo real." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.peak_spread)), "Suavizar o horário de pico" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.peak_spread)), $"Se esta opção estiver habilitada, os passageiros que demoram muito tempo para chegar ao trabalho sairão mais cedo para evitar o trânsito." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.lunch_break_percentage)), "Probabilidade de intervalo para almoço" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.lunch_break_percentage)), $"Probabilidade dos trabalhadores que farão intervalo para almoço. Durante este intervalo, os trabalhadores podem ir às compras de alimentos ou alimentos de conveniência, ou ir para lazer. Depois disso eles voltarão ao trabalho." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.disable_early_shop_leisure)), "Desativar compras ou lazer de madrugada" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.disable_early_shop_leisure)), $"No jogo Vanilla, os Cims podem fazer compras ou ir para lazer já às 4 da manhã. Esta opção mudará esse comportamento para começar por volta de 8h às 10h." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.use_vanilla_timeoff)), "Desativar recurso de Férias e Feriados" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.use_vanilla_timeoff)), $"Desative o recurso de férias e feriados e use o sistema de folga do jogo padrão. No jogo Vanilla, os cims têm 60% de probabilidade de tirar uma folga todos os dias." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.use_school_vanilla_timeoff)), "Desativar recurso de férias e feriados para escolas" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.use_school_vanilla_timeoff)), $"Desativar o recurso de férias e feriados e usar o sistema de folga do jogo Vanilla para escolas. No jogo Vanilla, os cims têm 60% de probabilidade de não ir à escola." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.holidays_per_year)), "Número de feriados por ano" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.holidays_per_year)), $"A maioria dos países devem ter um valor entre 10 e 15. O valor padrão é razoável para a maioria dos países." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.vacation_per_year)), "Número de dias de férias por ano" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.vacation_per_year)), $"Número de dias de férias por ano – sem incluir finais de semana. Para países com um mês de férias, como no Brasil, utilize 22. Para os EUA, um valor de 11 é mais realista." },
                //{ m_Setting.GetOptionLabelLocaleID(nameof(Setting.school_vacation_per_year)), "Número de dias de férias por ano" },
                //{ m_Setting.GetOptionDescLocaleID(nameof(Setting.school_vacation_per_year)), $"Número de dias de férias por ano para escolas, faculdades e universidades. Não inclui finais de semana." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.school_vacation_month1)), "Mês de férias 1:" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.school_vacation_month1)), $"Mês de férias. As escolas estarão fechadas." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.school_vacation_month2)), "Mês de férias 2:" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.school_vacation_month2)), $"Mês de férias. As escolas estarão fechadas." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.dt_simulation)), "Selecione o comportamento da Simulação Diária" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.dt_simulation)), $"Esta opção altera o funcionamento da simulação durante um dia. O Dia Padrão corresponde ao comportamento Vanilla, que é uma combinação de dias da semana e finais de semana. Com as configurações padrão de férias/feriados (definidas na aba Compras e Lazer), em um Dia Padrão, cerca de 30% dos cims se comportarão como no fim de semana, realizando mais atividades de lazer e compras, enquanto o restante trabalhará ou estudará. A opção Dia de Semana aumentará as atividades de trabalho e estudo e diminuirá o lazer e as compras. O fim de semana fará o oposto. Nos fins de semana, as escolas estão fechadas. A Semana de 7 Dias irá alternar entre dias de semana e finais de semana, indo de segunda a domingo." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.night_trucks)), "Tráfego de caminhões mais realista" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.night_trucks)), $"Diminui o tráfego de caminhõess durante o dia e o aumenta a noite e de madrugada." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.school_start_time)), "Horário de início das escolas do ensino básico" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.school_start_time)), $"Hora de início para escolas do ensino básico." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.school_end_time)), "Horário de término das escolas do ensino básico" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.school_end_time)), $"Horário de término para escolas do ensino básico." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.high_school_start_time)), "Horário de início das escolas do ensino médio" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.high_school_start_time)), $"Hora de início para escolas do ensino médio." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.high_school_end_time)), "Horário de término das escolas do ensino médio" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.high_school_end_time)), $"Horário de término para escolas do ensino médio." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.univ_start_time)), "Horário de início de Universidades e Faculdades" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.univ_start_time)), $"Hora de início para Universidades e Faculdades." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.univ_end_time)), "Horário de término de Universidades e Faculdades" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.univ_end_time)), $"Horário de término para Universidades e Faculdades." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.work_start_time)), "Horário de início do turno diurno" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.work_start_time)), $"Horário de início do turno diurno de trabalho." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.work_end_time)), "Horário de término do turno diurno" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.work_end_time)), $"Horário de término do turno diurno de trabalho." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.avg_work_hours_ft_wd)), "Média de horas trabalhadas para trabalhadores em tempo integral" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.avg_work_hours_ft_wd)), $"The average number of hours that full time workers worked on a weekday" },
                //{ m_Setting.GetOptionLabelLocaleID(nameof(Setting.avg_work_hours_ft_we)), "O número médio de horas que os trabalhadores em tempo integral trabalharam em um dia de semana" },
                //{ m_Setting.GetOptionDescLocaleID(nameof(Setting.avg_work_hours_ft_we)), $"The average number of hours that full time workers worked on a weekend" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.avg_work_hours_pt_wd)), "Média de horas trabalhadas para trabalhadores em meio período" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.avg_work_hours_pt_wd)), $"O número médio de horas que os trabalhadores de meio período trabalharam em um dia de semana" },
                //{ m_Setting.GetOptionLabelLocaleID(nameof(Setting.avg_work_hours_pt_we)), "Fim de semana: Média de horas trabalhadas para trabalhadores de meio período" },
                //{ m_Setting.GetOptionDescLocaleID(nameof(Setting.avg_work_hours_pt_we)), $"O número médio de horas que os trabalhadores de meio período trabalharam em um fim de semana" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.slow_time_factor)), "Fator de redução da velocidade do tempo" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.slow_time_factor)), $"Esse fator vai reduzir a velocidade do tempo e aumentar a duração do dia. Um fator de valor 1 não vai ter efeito. Um fator de valor 2, por exemplo, vai dobrar a duração do dia. Observe que a velocidade da simulação não será alterada. Outros sistemas que não usados neste mod serão atualizados baseados na velocidade da simulação e não na duração do dia." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.daysPerMonth)), "Dias por mês" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.daysPerMonth)), $"Altera o número de dias por mês. O valor padrão é 1." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.tourism_trips)), "Variação de turistas por dia da semana." },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.tourism_trips)), $"Aumenta o número de turistas no fim de semana e reduz nos dias de semana." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.commuter_trips)), "Variação de trabalhadores externos por dia da semana." },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.commuter_trips)), $"Aumenta o número de trabalhadores de conexões externas nos dias de semana e reduz nos fim de semana. Também aumenta a probabilidade de eles chegarem de avião." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.service_expenses_night_reduction)), "Redução de Custo Noturno" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.service_expenses_night_reduction)), $"Reduz os custos de serviços das 23h ate as 6h." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.trafficReduction)), "Redução de Tráfego" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.trafficReduction)), $"Valores mais baixos aumentam o tráfego na cidade. O valor vanilla é 5. Zero terá a quantidade máxima de tráfego." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.min_attraction)), "Atração Mínima" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.min_attraction)), $"Aumentar ou diminuir esta configuração alterará o número de instalaçõesque podem hospedar eventos especiais." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.min_event_weekday)), "Segunda a Quinta" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.min_event_weekday)), $"Número mínimo de eventos de segunda a quinta" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.min_event_avg_day)), "Sexta" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.min_event_avg_day)), $"Número mínimo de eventos na sexta-feira" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.min_event_weekend)), "Fim de Semana" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.min_event_weekend)), $"Número mínimo de eventos no fim de semana" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.max_event_weekday)), "Segunda a Quinta" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.max_event_weekday)), $"Número máximo de eventos de segunda a quinta" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.max_event_avg_day)), "Sexta" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.max_event_avg_day)), $"Número máximo de eventos na sexta-feira" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.max_event_weekend)), "Fim de Semana" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.max_event_weekend)), $"Número máximo de eventos no fim de semana" },

                { m_Setting.GetEnumValueLocaleID(Setting.DTSimulationEnum.AverageDay), "Dia Padrão" },
                { m_Setting.GetEnumValueLocaleID(Setting.DTSimulationEnum.Weekday), "Dia da Semana" },
                { m_Setting.GetEnumValueLocaleID(Setting.DTSimulationEnum.Saturday), "Sábado" },
                { m_Setting.GetEnumValueLocaleID(Setting.DTSimulationEnum.Sunday), "Sunday" },
                { m_Setting.GetEnumValueLocaleID(Setting.DTSimulationEnum.sevendayweek), "Semana (Segunda a Domingo)" },

                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.Performance), "Desempenho" },
                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.Balanced), "Balanceada" },
                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.USA), "Estados Unidos da América" },
                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.UK), "Reino Unido" },
                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.Brazil), "Brasil" },
                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.Canada), "Canadá" },
                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.Phillipines), "Filipinas" },
                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.Netherlands), "Paises Baíxos" },
                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.France), "França" },
                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.Germany), "Alemanha" },
                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.Poland), "Polônia" },

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
                { m_Setting.GetEnumValueLocaleID(Setting.dayOfWeek.Saturday), "Sab" }
            };
            }

            public void Unload()
            {

            }
        }
    }
}
