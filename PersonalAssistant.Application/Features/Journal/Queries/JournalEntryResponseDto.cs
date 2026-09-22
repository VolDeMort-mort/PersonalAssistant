namespace PersonalAssistant.Application.Features.Journal.Queries;

public record JournalEntryResponseDto(
    Guid Id,
    DateTime CreatedAt,
    string Type,
    string? Text,
    string? TranscribedText,
    bool IsProcessed
);