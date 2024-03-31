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
    [SettingsUIGroupOrder(WorkPlaceShiftGroup)]
    [SettingsUIShowGroupName(WorkPlaceShiftGroup)]
    public class Setting : ModSetting
    {
        public const string WorkPlaceShiftSection = "Modify WorkPlace Shift Probability";
        public const string ResetSection = "Reset";

        public const string WorkPlaceShiftGroup = "WorkPlaceShiftGroup";

        public Setting(IMod mod) : base(mod)
        {
            if (evening_share == 0) SetDefaults();
        }
        public override void SetDefaults()
        {
            evening_share = 6;
            night_share = 4;
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

        [SettingsUIButton]
        [SettingsUISection(WorkPlaceShiftSection, WorkPlaceShiftGroup)]
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

                { m_Setting.GetOptionGroupLocaleID(Setting.WorkPlaceShiftGroup), "Modify the Share of Evening and Night Work Shifts" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.Button)), "Reset" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.Button)), $"Reset percentages to default values" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.evening_share)), "Evening" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.evening_share)), $"Percentage for evening workplaces" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.night_share)), "Night" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.night_share)), $"Percentage for evening workplaces" },
            };
        }

        public void Unload()
        {

        }
    }
}
