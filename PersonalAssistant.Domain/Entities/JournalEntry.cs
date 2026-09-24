namespace PersonalAssistant.Domain.Entities;

public enum MessageType
{
    Text,
    Voice,
    Video
}

public class JournalEntry
{
    public Guid Id { get; set; }

    public Guid SessionId { get; set; }

    public long ChatId { get; set; }

    public int MessageId { get; set; }

    public DateTime CreatedAt { get; set; }
    
    public MessageType Type { get; set; }

    public string? OriginalText { get; set; }

    public string? TelegramFileId { get; set; }

    public string? LocalFilePath { get; set; }

    public bool IsProcessed { get; set; }

    public string? TranscribedText { get; set; }
}