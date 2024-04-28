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
    [SettingsUIGroupOrder(WorkPlaceShiftGroup, WorkPlaceDelayGroup, LunchBreakGroup, WorkTimeGroup, ResetGroup, ShopLeisureGroup, SchoolTimeOffGroup, SchoolTimeGroup, TimeOffGroup, DTSimulationGroup)]
    [SettingsUIShowGroupName(WorkPlaceShiftGroup, WorkPlaceDelayGroup, LunchBreakGroup, WorkTimeGroup, SchoolTimeOffGroup, SchoolTimeGroup, TimeOffGroup)]
    public class Setting : ModSetting
    {
        public const string WorkSection = "Work";
        public const string ShopLeisureSection = "Shopping and Leisure";
        public const string SchoolSection = "School";
        public const string DayTypeSimulationSection = "Day Type Simulation";
        public const string ResetGroup = "Reset";
        public const string ShopLeisureGroup = "ShopLeisureGroup";

        public const string WorkPlaceShiftGroup = "WorkPlaceShiftGroup";
        public const string WorkPlaceDelayGroup = "WorkPlaceDelayGroup";
        public const string LunchBreakGroup = "LunchBreakGroup";
        public const string WorkTimeGroup = "WorkTimeGroup";
        public const string TimeOffGroup = "TimeOffGroup";
        public const string SchoolTimeOffGroup = "SchoolTimeOffGroup";
        public const string SchoolTimeGroup = "SchoolTimeGroup";
        public const string DTSimulationGroup = "DTSimulationGroup";

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
            dt_simulation = DTSimulationEnum.AverageDay;
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
        [SettingsUISection(WorkSection, WorkPlaceShiftGroup)]
        public int evening_share { get; set; }

        [SettingsUISlider(min = 1, max = 25, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(WorkSection, WorkPlaceShiftGroup)]
        public int night_share { get; set; }

        [SettingsUISlider(min = 0, max = 10, step = 0.5f, scalarMultiplier = 1, unit = Unit.kFloatSingleFraction)]
        [SettingsUISection(WorkSection, WorkPlaceDelayGroup)]
        public float delay_factor { get; set; }

        [SettingsUISlider(min = 0, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(WorkSection, LunchBreakGroup)]
        public int lunch_break_percentage { get; set; }

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

        [SettingsUISection(WorkSection, WorkTimeGroup)]
        public timeEnum work_start_time { get; set; } = timeEnum.t900;

        [SettingsUISection(WorkSection, WorkTimeGroup)]
        public timeEnum work_end_time { get; set; } = timeEnum.t1700;

        [SettingsUISection(DayTypeSimulationSection, DTSimulationGroup)]
        public DTSimulationEnum dt_simulation { get; set; } = DTSimulationEnum.AverageDay;

        public enum DTSimulationEnum 
        {
            AverageDay,
            Weekday,
            Weekend,
            sevendayweek
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

        [SettingsUIButton]
        [SettingsUISection(WorkSection, ResetGroup)]
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
                { m_Setting.GetOptionTabLocaleID(Setting.WorkSection), "Work" },
                { m_Setting.GetOptionTabLocaleID(Setting.ShopLeisureSection), "Shopping and Leisure" },
                { m_Setting.GetOptionTabLocaleID(Setting.SchoolSection), "School" },
                { m_Setting.GetOptionTabLocaleID(Setting.DayTypeSimulationSection), "Day Type Simulation" },

                { m_Setting.GetOptionGroupLocaleID(Setting.WorkPlaceShiftGroup), "Modify the share of evening and night work shifts" },
                { m_Setting.GetOptionGroupLocaleID(Setting.WorkPlaceDelayGroup), "Modify the work arrival and departure times" },
                { m_Setting.GetOptionGroupLocaleID(Setting.LunchBreakGroup), "Modify the probability of workers taking a lunch break" },
                { m_Setting.GetOptionGroupLocaleID(Setting.TimeOffGroup), "Vacation and Holiday Settings" },
                { m_Setting.GetOptionGroupLocaleID(Setting.WorkTimeGroup), "Work Start/End Time Settings" },
                { m_Setting.GetOptionGroupLocaleID(Setting.SchoolTimeOffGroup), "School Vacation Settings" },
                { m_Setting.GetOptionGroupLocaleID(Setting.SchoolTimeGroup), "School Start/End Time Settings" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.Button)), "Reset" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.Button)), $"Reset all settings to default values" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.evening_share)), "Evening" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.evening_share)), $"Percentage for evening workplaces" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.night_share)), "Night" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.night_share)), $"Percentage for night workplaces" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.delay_factor)), "Delay/Early Factor" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.delay_factor)), $"This factor will adjust the variation in arrival and departure times from work. A higher factor will increase the variation on work arrival and departure - meaning more cims will not arrive to work on time or work for longer hours. A value of zero will disable this feature. Note that the effects of this feature in the morning and evening peak hours is different: in the morning there is an equal probabilty of early or late arrival, however, in the evening the probability of leaving late is higher than of leaving early. This was implemented this way to simulate better the differences of morning and evening commute from the real world." },
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
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.school_start_time)), "School Start Time" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.school_start_time)), $"Start time for schools, colleges, and universities." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.school_end_time)), "School End Time" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.school_end_time)), $"End time for schools, colleges, and universities." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.work_start_time)), "Work Day Shift Start Time" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.work_start_time)), $"Start time for work day shift." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.work_end_time)), "Work Day End Time" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.work_end_time)), $"End time for work day shift." },

                { m_Setting.GetEnumValueLocaleID(Setting.DTSimulationEnum.AverageDay), "Average Day" },
                { m_Setting.GetEnumValueLocaleID(Setting.DTSimulationEnum.Weekday), "Weekday" },
                { m_Setting.GetEnumValueLocaleID(Setting.DTSimulationEnum.Weekend), "Weekend" },
                { m_Setting.GetEnumValueLocaleID(Setting.DTSimulationEnum.sevendayweek), "7 Days Week (Monday to Sunday)" },

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
                { m_Setting.GetOptionTabLocaleID(Setting.WorkSection), "Emprego" },
                { m_Setting.GetOptionTabLocaleID(Setting.ShopLeisureSection), "Compras e Lazer" },
                { m_Setting.GetOptionTabLocaleID(Setting.SchoolSection), "Escola" },
                { m_Setting.GetOptionTabLocaleID(Setting.DayTypeSimulationSection), "Tipo de Simulação Diária" },

                { m_Setting.GetOptionGroupLocaleID(Setting.WorkPlaceShiftGroup), "Alterar a porcentagem de turnos vespertinos e noturnos." },
                { m_Setting.GetOptionGroupLocaleID(Setting.WorkPlaceDelayGroup), "Alterar o horário de chegada e saida do trabalho." },
                { m_Setting.GetOptionGroupLocaleID(Setting.LunchBreakGroup), "mMdificar a probabilidade dos trabalhadores saírem para um intervalo de almoço" },
                { m_Setting.GetOptionGroupLocaleID(Setting.TimeOffGroup), "Configurações de férias e feriados" },
                { m_Setting.GetOptionGroupLocaleID(Setting.WorkTimeGroup), "Configurações de horário de início/término do trabalho" },
                { m_Setting.GetOptionGroupLocaleID(Setting.SchoolTimeOffGroup), "Configurações de férias escolares" },
                { m_Setting.GetOptionGroupLocaleID(Setting.SchoolTimeGroup), "Configurações de horário de início/término das aulas nas escolas" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.Button)), "Restaurar Configurações" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.Button)), $"Redefinir todas as configurações para os valores padrões." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.evening_share)), "Tarde" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.evening_share)), $"Porcentagem para locais de trabalho vespertinos" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.night_share)), "Noite" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.night_share)), $"Porcentagem para locais de trabalho noturnos" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.delay_factor)), "Fator de chegada/saída atrasada ou antecipada" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.delay_factor)), $"Este fator ajustará a variação dos horários de chegada e saída do trabalho. Um fator mais alto aumentará a variação na chegada e saída do trabalho - o que significa que mais cims não chegarão ao trabalho na hora certa ou trabalharão por mais horas. Um valor zero desativará essa funcionalidade. Observe que os efeitos desta opção nos horários de pico da manhã e da tarde são diferentes: de manhã há igual probabilidade de chegar cedo ou mais tarde, porém, à tarde a probabilidade de sair atrasado é maior do que de sair mais cedo. Isso foi implementado desta forma para simular melhor as diferenças entre o deslocamento matinal e vespertino em relação ao mundo real." },
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
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.school_start_time)), "Horário de início das escolas" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.school_start_time)), $"Hora de início para escolas, faculdades e universidades." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.school_end_time)), "Horário de término das escolas" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.school_end_time)), $"Horário de término para escolas, faculdades e universidades." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.work_start_time)), "Horário de início do turno diurno" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.work_start_time)), $"Horário de início do turno diurno de trabalho." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.work_end_time)), "Horário de término do turno diurno" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.work_end_time)), $"Horário de término do turno diurno de trabalho." },

                { m_Setting.GetEnumValueLocaleID(Setting.DTSimulationEnum.AverageDay), "Dia Padrão" },
                { m_Setting.GetEnumValueLocaleID(Setting.DTSimulationEnum.Weekday), "Dia da Semana" },
                { m_Setting.GetEnumValueLocaleID(Setting.DTSimulationEnum.Weekend), "Fim de Semana" },
                { m_Setting.GetEnumValueLocaleID(Setting.DTSimulationEnum.sevendayweek), "Semana (Segunda a Domingo)" },

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
