namespace PersonalAssistant.Presentation.Constants;

public static class BotConstants
{
    public static class Commands
    {
        public const string MainMenu = "/menu";
        public const string StartJournal = "/startjournal";
        public const string StopJournal = "/stopjournal";
    }

    public static class Payloads
    {
        public const string NavRoot = "nav_root";
        public const string NavJournal = "nav_journal";
        public const string NavPlanner = "nav_planner";
        public const string NavScraper = "nav_scraper";

        public const string NavJournalStart = "journal_start";
        public const string NavJournalRecording = "journal_recording";
        public const string NavJournalRecorded = "journal_recorded";
        public const string NavJournalCancelRecord = "journal_cancel_record";
    }

    public static class Message
    {
        public const string MsgRootMenu = "👋 Головне меню. Оберіть модуль:";
        public const string MsgJournalMenu = "📓 Журнал. Режим очікування.";
        //public const string MsgPlannerMenu = "📅 Планувальник. У вас 2 активні задачі.";
        //public const string MsgScraperMenu = "🌐 Парсер. Надішліть посилання для автоматичного збору даних.";

        public const string MsgJournalRecording = "🔴 Запис іде... Надішліть повідомлення";
        public const string MsgJournalRecordedEmpty = "Запис зупинено. Нічого не записано(";
        public static string MsgJournalRecorded(int count) =>
            $"✅ Збережено повідомлень: {count}";

        public const string MsgNotImplementedFeature = "На даний момент ця фІча в розробці";

        public const string MsgJournalLocked = "🔴 Спершу збережіть або відмініть запис журналу";
    }

}