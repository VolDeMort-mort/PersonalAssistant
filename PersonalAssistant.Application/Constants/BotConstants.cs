namespace PersonalAssistant.Application.Constants;

public static class BotConstants
{
    public static class Commands
    {
        public const string StartJournal = "/startjournal";
        public const string StopJournal = "/stopjournal";
    }

    public static class Messages
    {
        public const string JournalOpened = "📖 Журнал відкрито. Я слухаю... (відправ текст, аудіо чи відео. Коли закінчиш - напиши /stopjournal)";
        public const string JournalAlreadyClosed = "Журнал і так був закритий.";
        public const string JournalClosedEmpty = "Журнал зачинено. Ти нічого не записав 🤷‍♂️";

        public static string JournalSaved(int count) =>
            $"✅ Збережено повідомлень: {count}. Аудіо/відео відправлені на обробку Whisper у фоні!";
    }
}