using MediatR;

namespace PersonalAssistant.Application.Features.Journal.Events;

/// <summary>
/// Content of a journal entry is safely persisted,
/// so the original chat message is no longer needed.
/// </summary>
public record JournalEntryStored(Guid EntryId, long ChatId, int MessageId) : INotification;
