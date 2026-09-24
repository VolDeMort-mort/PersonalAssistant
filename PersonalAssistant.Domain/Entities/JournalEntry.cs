namespace PersonalAssistant.Domain.Entities;

public enum MessageType
{
    Text = 0,
    Voice = 1,
    Video = 2
}

public enum ProcessingStatus
{
    NotRequired = 0,
    Pending = 1,
    Downloaded = 2,
    Transcribed = 3,
    Failed = 4
}

public class JournalEntry
{
    private JournalEntry() { }

    public Guid Id { get; private set; }
    public Guid SessionId { get; private set; }
    public long ChatId { get; private set; }
    public int MessageId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public MessageType Type { get; private set; }
    public string? OriginalText { get; private set; }
    public string? TelegramFileId { get; private set; }
    public string? LocalFilePath { get; private set; }
    public string? TranscribedText { get; private set; }
    public ProcessingStatus Status { get; private set; }
    public string? FailureReason { get; private set; }

    public static JournalEntry CreateText(Guid sessionId, long chatId, int messageId, string? text, DateTime createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        return new JournalEntry
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            ChatId = chatId,
            MessageId = messageId,
            CreatedAt = createdAt,
            Type = MessageType.Text,
            OriginalText = text,
            Status = ProcessingStatus.NotRequired
        };
    }

    public static JournalEntry CreateMedia(Guid sessionId, long chatId, int messageId, MessageType type, string? telegramFileId, DateTime createdAt)
    {
        if (type == MessageType.Text)
            throw new ArgumentException("Use CreateText for text entries.", nameof(type));
        ArgumentException.ThrowIfNullOrWhiteSpace(telegramFileId);

        return new JournalEntry
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            ChatId = chatId,
            MessageId = messageId,
            CreatedAt = createdAt,
            Type = type,
            TelegramFileId = telegramFileId,
            Status = ProcessingStatus.Pending
        };
    }

    public void MarkDownloaded(string localFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(localFilePath);
        if (Status != ProcessingStatus.Pending)
            throw new InvalidOperationException($"Cannot mark entry as downloaded from status {Status}.");

        LocalFilePath = localFilePath;
        Status = ProcessingStatus.Downloaded;
    }

    public void MarkTranscribed(string transcribedText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transcribedText);
        if (Status != ProcessingStatus.Downloaded)
            throw new InvalidOperationException($"Cannot mark entry as transcribed from status {Status}.");

        TranscribedText = transcribedText;
        FailureReason = null;
        Status = ProcessingStatus.Transcribed;
    }

    public void MarkFailed(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (Status is not (ProcessingStatus.Pending or ProcessingStatus.Downloaded))
            throw new InvalidOperationException($"Cannot mark entry as failed from status {Status}.");

        FailureReason = reason;
        Status = ProcessingStatus.Failed;
    }
}
