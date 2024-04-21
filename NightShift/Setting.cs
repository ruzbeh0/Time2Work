using Colossal;
using Colossal.IO.AssetDatabase;
using Colossal.IO.AssetDatabase.Internal;
using Game.Modding;
using Game.Settings;
using Game.UI;
using Game.UI.Widgets;
using System.Collections.Generic;
using Unity.Entities;

namespace Time2Work
{
    [FileLocation(nameof(Time2Work))]
    [SettingsUIGroupOrder(WorkPlaceShiftGroup, WorkPlaceDelayGroup, LunchBreakGroup)]
    [SettingsUIShowGroupName(WorkPlaceShiftGroup, WorkPlaceDelayGroup, LunchBreakGroup)]
    public class Setting : ModSetting
    {
        public const string WorkPlaceShiftSection = "Modify WorkPlace Shift Probability";
        public const string ResetSection = "Reset";

        public const string WorkPlaceShiftGroup = "WorkPlaceShiftGroup";
        public const string WorkPlaceDelayGroup = "WorkPlaceDelayGroup";
        public const string LunchBreakGroup = "LunchBreakGroup";

        public Setting(IMod mod) : base(mod)
        {
            if (evening_share == 0) SetDefaults();
        }
        public override void SetDefaults()
        {
            evening_share = 6;
            night_share = 4;
            delay_factor = 2;
            lunch_break_percentage = 25;
        }

        public override void Apply()
        {
            base.Apply();
            var system1 = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<WorkPlaceShiftUpdateSystem>();
            system1.Enabled = true;
            var system2 = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<WorkerShiftUpdateSystem>();
            system2.Enabled = true;
        }

        [SettingsUISlider(min = 1, max = 25, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(WorkPlaceShiftSection, WorkPlaceShiftGroup)]
        public int evening_share { get; set; }

        [SettingsUISlider(min = 1, max = 25, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(WorkPlaceShiftSection, WorkPlaceShiftGroup)]
        public int night_share { get; set; }

        [SettingsUISlider(min = 0, max = 10, step = 0.5f, scalarMultiplier = 1, unit = Unit.kFloatSingleFraction)]
        [SettingsUISection(WorkPlaceShiftSection, WorkPlaceDelayGroup)]
        public float delay_factor { get; set; }

        [SettingsUISlider(min = 0, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(WorkPlaceShiftSection, LunchBreakGroup)]
        public int lunch_break_percentage { get; set; }

        [SettingsUIButton]
        [SettingsUISection(WorkPlaceShiftSection, LunchBreakGroup)]
        public bool Button { set { SetDefaults(); } }

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
                { m_Setting.GetSettingsLocaleID(), "Time2Work" },
                { m_Setting.GetOptionTabLocaleID(Setting.WorkPlaceShiftSection), "Time2Work" },

                { m_Setting.GetOptionGroupLocaleID(Setting.WorkPlaceShiftGroup), "Modify the share of evening and night work Shifts" },
                { m_Setting.GetOptionGroupLocaleID(Setting.WorkPlaceDelayGroup), "Modify the work arrival and departure times" },
                { m_Setting.GetOptionGroupLocaleID(Setting.LunchBreakGroup), "Modify the probability of workers taking a lunch break" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.Button)), "Reset" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.Button)), $"Reset percentages to default values" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.evening_share)), "Evening" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.evening_share)), $"Percentage for evening workplaces" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.night_share)), "Night" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.night_share)), $"Percentage for night workplaces" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.delay_factor)), "Delay/Early Factor" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.delay_factor)), $"This factor will adjust the variation in arrival and departure times from work. A higher factor will increase the variation on work arrival and departure - meaning more cims will not arrive to work on time or work for longer hours. A value of zero will disable this feature. Note that the effects of this feature in the morning and evening peak hours is different: in the morning there is an equal probabilty of early or late arrival, however, in the evening the probability of leaving late is higher than of leaving early. This was implemented this way to simulate better the differences of morning and evening commute from the real world." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.lunch_break_percentage)), "Lunch Break Probability" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.lunch_break_percentage)), $"Probability of workers that will take a lunch break. During a lunch break, workers might go shopping for food or convenience food, or go for leisure. After that they will return to work." },
            };
        }

        public void Unload()
        {

        }
    }
}
