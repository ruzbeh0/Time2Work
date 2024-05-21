using Colossal;
using Colossal.IO.AssetDatabase;
using Colossal.IO.AssetDatabase.Internal;
using Game;
using Game.Modding;
using Game.Settings;
using Game.UI;
using Game.UI.Widgets;
using System.Collections.Generic;
using Time2Work.Systems;
using Unity.Entities;

namespace Time2Work
{
    //[FileLocation(nameof(Time2Work))]
    [FileLocation($"ModsSettings\\{nameof(Time2Work)}\\settings")]
    [SettingsUIGroupOrder(SettingsGroup, DelayGroup, WorkPlaceShiftGroup, RemoteGroup, DayShiftGroup, ResetGroup, ShopLeisureGroup, SchoolTimeOffGroup, SchoolTimeGroup, TimeOffGroup, TrucksGroup, DTSimulationGroup, SlowerTimeGroup)]
    [SettingsUIShowGroupName(WorkPlaceShiftGroup, RemoteGroup, DayShiftGroup, SchoolTimeOffGroup, SchoolTimeGroup, TimeOffGroup, DTSimulationGroup, SlowerTimeGroup, TrucksGroup)]
    public class Setting : ModSetting
    {
        public const string SettingsSection = "Settings";
        public const string WorkSection = "Work";
        public const string ShopLeisureSection = "Shopping and Leisure";
        public const string SchoolSection = "School";
        public const string OtherSection = "Other";
        public const string ResetGroup = "Reset";
        public const string ShopLeisureGroup = "ShopLeisureGroup";

        public const string SettingsGroup = "SettingsGroup";
        public const string WorkPlaceShiftGroup = "WorkPlaceShiftGroup";
        public const string DelayGroup = "DelayGroup";
        public const string RemoteGroup = "RemoteGroup";
        public const string DayShiftGroup = "DayShiftGroup";
        public const string TimeOffGroup = "TimeOffGroup";
        public const string SchoolTimeOffGroup = "SchoolTimeOffGroup";
        public const string SchoolTimeGroup = "SchoolTimeGroup";
        public const string TrucksGroup = "TrucksGroup";
        public const string DTSimulationGroup = "DTSimulationGroup";
        public const string SlowerTimeGroup = "SlowerTimeGroup";

        public Setting(IMod mod) : base(mod)
        {
            if (evening_share == 0) SetDefaults();
        }
        public override void SetDefaults()
        {
            evening_share = 6;
            night_share = 4;
            delay_factor = 2;
            lunch_break_percentage = 20;
            holidays_per_year = 11;
            vacation_per_year = 22;
            school_vacation_per_year = 55;
            disable_early_shop_leisure = true;
            use_vanilla_timeoff = true;
            use_school_vanilla_timeoff = true;
            school_start_time = timeEnum.t900;
            school_end_time = timeEnum.t1700;
            work_start_time = timeEnum.t900;
            work_end_time = timeEnum.t1700;
            dt_simulation = DTSimulationEnum.AverageDay;
            slow_time_factor = 3.5f;
            enable_slower_time = false;
            part_time_percentage = 22;
            remote_percentage = 14;
            night_trucks = true;
            peak_spread = true;
        }

        private void setPerformance()
        {
            evening_share = 17;
            night_share = 8;
            delay_factor = 4;
            lunch_break_percentage = 10;
            holidays_per_year = 11;
            vacation_per_year = 22;
            school_vacation_per_year = 55;
            disable_early_shop_leisure = false;
            use_vanilla_timeoff = true;
            use_school_vanilla_timeoff = true;
            school_start_time = timeEnum.t900;
            school_end_time = timeEnum.t1700;
            work_start_time = timeEnum.t900;
            work_end_time = timeEnum.t1700;
            dt_simulation = DTSimulationEnum.AverageDay;
            slow_time_factor = 3.5f;
            enable_slower_time = false;
            part_time_percentage = 22;
            remote_percentage = 20;
            night_trucks = true;
        }

        private void setRealistic()
        {
            evening_share = 6;
            night_share = 4;
            delay_factor = 2;
            lunch_break_percentage = 30;
            holidays_per_year = 11;
            vacation_per_year = 22;
            school_vacation_per_year = 55;
            disable_early_shop_leisure = true;
            use_vanilla_timeoff = false;
            use_school_vanilla_timeoff = false;
            school_start_time = timeEnum.t900;
            school_end_time = timeEnum.t1700;
            work_start_time = timeEnum.t900;
            work_end_time = timeEnum.t1700;
            dt_simulation = DTSimulationEnum.sevendayweek;
            slow_time_factor = 3.5f;
            enable_slower_time = true;
            part_time_percentage = 22;
            remote_percentage = 14;
            night_trucks = true;
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
                if (settings_choice.Equals(SettingsEnum.Performance))
                {
                    setPerformance();
                }
                else
                {
                    if (settings_choice.Equals(SettingsEnum.Realistic))
                    {
                        setRealistic();
                    }
                    else
                    {
                        SetDefaults();
                    }
                }
            }

        }

        [SettingsUISection(SettingsSection, SettingsGroup)]
        [SettingsUIMultilineText]
        public string MultilineText => string.Empty;

        [SettingsUISlider(min = 1, max = 25, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(WorkSection, WorkPlaceShiftGroup)]
        public int evening_share { get; set; }

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

        [SettingsUISection(SchoolSection, SchoolTimeOffGroup)]
        public bool use_school_vanilla_timeoff { get; set; }

        [SettingsUISlider(min = 0, max = 120, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(SchoolSection, SchoolTimeOffGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(use_school_vanilla_timeoff))]
        public float school_vacation_per_year { get; set; }

        [SettingsUISection(SchoolSection, SchoolTimeGroup)]
        public timeEnum school_start_time { get; set; } = timeEnum.t900;

        [SettingsUISection(SchoolSection, SchoolTimeGroup)]
        public timeEnum school_end_time { get; set; } = timeEnum.t1700;

        [SettingsUISection(WorkSection, DayShiftGroup)]
        public timeEnum work_start_time { get; set; } = timeEnum.t900;

        [SettingsUISection(WorkSection, DayShiftGroup)]
        public timeEnum work_end_time { get; set; } = timeEnum.t1700;

        [SettingsUISlider(min = 0, max = 40, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(WorkSection, DayShiftGroup)]
        public int part_time_percentage { get; set; }

        [SettingsUISection(OtherSection, TrucksGroup)]
        public bool night_trucks { get; set; }

        [SettingsUISection(OtherSection, DTSimulationGroup)]
        public DTSimulationEnum dt_simulation { get; set; } = DTSimulationEnum.AverageDay;

        [SettingsUISection(OtherSection, SlowerTimeGroup)]
        public bool enable_slower_time { get; set; }

        private bool disableSlowerTime()
        {
            return !enable_slower_time;
        }

        [SettingsUISlider(min = 1.1f, max = 5, step = 0.1f, scalarMultiplier = 1, unit = Unit.kFloatSingleFraction)]
        [SettingsUISection(OtherSection, SlowerTimeGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(disableSlowerTime))]
        public float slow_time_factor { get; set; }

        public enum DTSimulationEnum
        {
            AverageDay,
            Weekday,
            Weekend,
            sevendayweek
        }
        public enum SettingsEnum
        {
            Balanced,
            Performance,
            Realistic
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
                { m_Setting.GetSettingsLocaleID(), "Realistic Trips (Time2Work)" },
                { m_Setting.GetOptionTabLocaleID(Setting.SettingsSection), "Settings" },
                { m_Setting.GetOptionTabLocaleID(Setting.WorkSection), "Work" },
                { m_Setting.GetOptionTabLocaleID(Setting.ShopLeisureSection), "Shopping and Leisure" },
                { m_Setting.GetOptionTabLocaleID(Setting.SchoolSection), "School" },
                { m_Setting.GetOptionTabLocaleID(Setting.OtherSection), "Other" },

                { m_Setting.GetOptionGroupLocaleID(Setting.WorkPlaceShiftGroup), "Modify the share of evening and night work shifts" },
                { m_Setting.GetOptionGroupLocaleID(Setting.RemoteGroup), "Remote Work Settings" },
                { m_Setting.GetOptionGroupLocaleID(Setting.TimeOffGroup), "Vacation and Holiday Settings" },
                { m_Setting.GetOptionGroupLocaleID(Setting.DayShiftGroup), "Day Shift Settings" },
                { m_Setting.GetOptionGroupLocaleID(Setting.SchoolTimeOffGroup), "School Vacation Settings" },
                { m_Setting.GetOptionGroupLocaleID(Setting.SchoolTimeGroup), "School Start/End Time Settings" },
                { m_Setting.GetOptionGroupLocaleID(Setting.SlowerTimeGroup), "Slow Time and Increase Day Length" },
                { m_Setting.GetOptionGroupLocaleID(Setting.DTSimulationGroup), "Day Type Simulation" },
                { m_Setting.GetOptionGroupLocaleID(Setting.TrucksGroup), "Trucks" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.MultilineText)), $"WARNING: Slower Time feature can cause issues with Population Rebalance and Info Loom mods - in an existing city. A new city will probably not have problems with those mods." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.settings_choice)), "Mod settings" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.settings_choice)), $"Change all mod settings. Performance: will update the settings to improve performance, this is similar to the Vanilla game. Balanced: has most of the features from this mod enabled, but a few of them that have high impact on performance are disabled. Realistic: all features are enabled and set to values that would make the simulation more realistic" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.Button)), "Confirm" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.Button)), $"Confirm new settings" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.evening_share)), "Evening" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.evening_share)), $"Percentage for evening workplaces" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.night_share)), "Night" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.night_share)), $"Percentage for night workplaces" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.part_time_percentage)), "Part Time Percentage" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.part_time_percentage)), $"Percentage of day shift workers that work part time. These workers will work either in the morning or in the afternoon. They do not take lunch break, and a higher value will increase trips in the middle of the day and reduce the rush hour peaks." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.remote_percentage)), "Percentage of Remote Workers" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.remote_percentage)), $"Percentage of workers that work from home. These workers can still go out for a lunch break." },
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
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.school_vacation_per_year)), "Number of vacation days per year" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.school_vacation_per_year)), $"Number of vacation days per year for schools, colleges and universities. Does not include weekends." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.dt_simulation)), "Select day type simulation behaviour" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.dt_simulation)), $"This option changes how the simulation works during a day. Average Day corresponds to the Vanilla behavior, which is a combination of weekday and weekend behaviors. With the default vacation/holiday settings (defined in the Shopping and Leisure tab), in an Average Day, around 30% of cims will behave as on the weekend, doing more leisure and shopping activities, while the rest will work or study. The Weekday option will increase work and study activities and lower leisure and shopping. Weekend will do the opposite. On Weekends, schools are closed. The 7 Days Week will rotate through weekdays and weekends, going from Monday to Sunday." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.night_trucks)), "More realist truck traffic" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.night_trucks)), $"Reduces truck traffic during the day and increases it at night and early morning." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.school_start_time)), "School Start Time" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.school_start_time)), $"Start time for schools, colleges, and universities." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.school_end_time)), "School End Time" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.school_end_time)), $"End time for schools, colleges, and universities." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.work_start_time)), "Work Day Shift Start Time" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.work_start_time)), $"Start time for work day shift." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.work_end_time)), "Work Day Shift End Time" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.work_end_time)), $"End time for work day shift." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.enable_slower_time)), "Enable Slower Time" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.enable_slower_time)), $"Slower time without changing the simulation speed." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.slow_time_factor)), "Slower Time Factor" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.slow_time_factor)), $"This factor will slow down time and increase the length of the day. A factor of 1 will have no effect. A factor of 2, for example, will make the day last twice as long. Note that the simulation speed does not change, and other systems not affected by this mod will update based on the simulation speed and not on the length of the day." },

                { m_Setting.GetEnumValueLocaleID(Setting.DTSimulationEnum.AverageDay), "Average Day" },
                { m_Setting.GetEnumValueLocaleID(Setting.DTSimulationEnum.Weekday), "Weekday" },
                { m_Setting.GetEnumValueLocaleID(Setting.DTSimulationEnum.Weekend), "Weekend" },
                { m_Setting.GetEnumValueLocaleID(Setting.DTSimulationEnum.sevendayweek), "7 Days Week (Monday to Sunday)" },

                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.Performance), "Performance" },
                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.Balanced), "Balanced" },
                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.Realistic), "Realistic" },

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
                { m_Setting.GetSettingsLocaleID(), "Time2Work" },
                { m_Setting.GetOptionTabLocaleID(Setting.SettingsSection), "Configurações" },
                { m_Setting.GetOptionTabLocaleID(Setting.WorkSection), "Emprego" },
                { m_Setting.GetOptionTabLocaleID(Setting.ShopLeisureSection), "Compras e Lazer" },
                { m_Setting.GetOptionTabLocaleID(Setting.SchoolSection), "Escola" },
                { m_Setting.GetOptionTabLocaleID(Setting.OtherSection), "Outros" },

                { m_Setting.GetOptionGroupLocaleID(Setting.WorkPlaceShiftGroup), "Alterar a porcentagem de turnos vespertinos e noturnos." },
                { m_Setting.GetOptionGroupLocaleID(Setting.RemoteGroup), "Configurações de Home Office" },
                { m_Setting.GetOptionGroupLocaleID(Setting.TimeOffGroup), "Configurações de férias e feriados" },
                { m_Setting.GetOptionGroupLocaleID(Setting.DayShiftGroup), "Configurações do turno diurno."},
                { m_Setting.GetOptionGroupLocaleID(Setting.SchoolTimeOffGroup), "Configurações de férias escolares" },
                { m_Setting.GetOptionGroupLocaleID(Setting.SchoolTimeGroup), "Configurações de horário de início/término das aulas nas escolas" },
                { m_Setting.GetOptionGroupLocaleID(Setting.SlowerTimeGroup), "Reduzir a velocidade do tempo e aumentar a duração do dia" },
                { m_Setting.GetOptionGroupLocaleID(Setting.DTSimulationGroup), "Tipo de Simulação Diária" },
                { m_Setting.GetOptionGroupLocaleID(Setting.TrucksGroup), "Caminhões" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.MultilineText)), $"AVISO: O recurso de Tempo mais Lento pode causar problemas com os mods Population Rebalance e Info Loom - em uma cidade existente. Uma nova cidade provavelmente não terá problemas com esses mods." },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.settings_choice)), $"Alterar as configurações do mod" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.settings_choice)), $"Alterar todas as configurações. Desempenho: irá atualizar as configurações para melhorar o desempenho, isso é semelhante ao jogo Vanilla. Balanceado: tem a maioria dos recursos deste mod habilitados, mas alguns deles que têm alto impacto no desempenho estão desabilitadas. Realista: todos os recursos estão habilitados e definidos com valores que tornarão a simulação mais realista" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.Button)), "Confirmar" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.Button)), $"Confirmar novas configurações" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.evening_share)), "Tarde" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.evening_share)), $"Porcentagem para locais de trabalho vespertinos" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.night_share)), "Noite" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.night_share)), $"Porcentagem para locais de trabalho noturnos" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.remote_percentage)), "Porcentagem de trabalhadores que fazem Home Office" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.remote_percentage)), $"Porcentagem de trabalhadores que trabalham em casa. Estes funcionários também tem intervalo para almoço." },
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
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.school_vacation_per_year)), "Número de dias de férias por ano" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.school_vacation_per_year)), $"Número de dias de férias por ano para escolas, faculdades e universidades. Não inclui finais de semana." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.dt_simulation)), "Selecione o comportamento da Simulação Diária" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.dt_simulation)), $"Esta opção altera o funcionamento da simulação durante um dia. O Dia Padrão corresponde ao comportamento Vanilla, que é uma combinação de dias da semana e finais de semana. Com as configurações padrão de férias/feriados (definidas na aba Compras e Lazer), em um Dia Padrão, cerca de 30% dos cims se comportarão como no fim de semana, realizando mais atividades de lazer e compras, enquanto o restante trabalhará ou estudará. A opção Dia de Semana aumentará as atividades de trabalho e estudo e diminuirá o lazer e as compras. O fim de semana fará o oposto. Nos fins de semana, as escolas estão fechadas. A Semana de 7 Dias irá alternar entre dias de semana e finais de semana, indo de segunda a domingo." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.night_trucks)), "Tráfego de caminhões mais realista" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.night_trucks)), $"Diminui o tráfego de caminhõess durante o dia e o aumenta a noite e de madrugada." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.school_start_time)), "Horário de início das escolas" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.school_start_time)), $"Hora de início para escolas, faculdades e universidades." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.school_end_time)), "Horário de término das escolas" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.school_end_time)), $"Horário de término para escolas, faculdades e universidades." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.work_start_time)), "Horário de início do turno diurno" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.work_start_time)), $"Horário de início do turno diurno de trabalho." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.work_end_time)), "Horário de término do turno diurno" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.work_end_time)), $"Horário de término do turno diurno de trabalho." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.enable_slower_time)), "Ativer tempo mais lento" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.enable_slower_time)), $"Reduz a velocidade do tempo sem mudar a velocidade da simulação." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.slow_time_factor)), "Fator de redução da velocidade do tempo" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.slow_time_factor)), $"Esse fator vai reduzir a velocidade do tempo e aumentar a duração do dia. Um fator de valor 1 não vai ter efeito. Um fator de valor 2, por exemplo, vai dobrar a duração do dia. Observe que a velocidade da simulação não será alterada. Outros sistemas que não usados neste mod serão atualizados baseados na velocidade da simulação e não na duração do dia." },

                { m_Setting.GetEnumValueLocaleID(Setting.DTSimulationEnum.AverageDay), "Dia Padrão" },
                { m_Setting.GetEnumValueLocaleID(Setting.DTSimulationEnum.Weekday), "Dia da Semana" },
                { m_Setting.GetEnumValueLocaleID(Setting.DTSimulationEnum.Weekend), "Fim de Semana" },
                { m_Setting.GetEnumValueLocaleID(Setting.DTSimulationEnum.sevendayweek), "Semana (Segunda a Domingo)" },

                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.Performance), "Desempenho" },
                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.Balanced), "Balanceada" },
                { m_Setting.GetEnumValueLocaleID(Setting.SettingsEnum.Realistic), "Realista" },

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
            };
            }

            public void Unload()
            {

            }
        }
    }
}
