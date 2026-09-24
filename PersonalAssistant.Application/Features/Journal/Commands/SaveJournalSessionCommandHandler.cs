using MediatR;
using PersonalAssistant.Application.Features.Journal.Events;
using PersonalAssistant.Application.Interfaces;
using PersonalAssistant.Domain.Entities;

namespace PersonalAssistant.Application.Features.Journal.Commands;


public class SaveJournalSessionCommandHandler : IRequestHandler<SaveJournalSessionCommand>
{
    private readonly IPublisher _publisher;
    private readonly IJournalRepository _repository;
    private readonly IAudioProcessQueue _queue;

    public SaveJournalSessionCommandHandler(
        IPublisher publisher,
        IJournalRepository repository, 
        IAudioProcessQueue queue)
    {
        _publisher = publisher;
        _repository = repository;
        _queue = queue;
    }

    public async Task Handle(SaveJournalSessionCommand request, CancellationToken cancellationToken)
    {
        var entries = request.Messages.Select(m => new JournalEntry
        {
            Id = Guid.NewGuid(),
            ChatId = request.ChatId,
            SessionId = request.SessionId,
            MessageId = m.MessageId,
            CreatedAt = m.CreatedAt,
            Type = (MessageType)m.Type,
            OriginalText = m.Text,
            TelegramFileId = m.FileId,
            IsProcessed = m.Type == DtoMessageType.Text
        }).ToList();

        await _repository.AddRangeAsync(entries, cancellationToken);

        foreach (var entry in entries)
        {
            if (entry.Type == MessageType.Voice || entry.Type == MessageType.Video)
                await _queue.EnqueueAsync(entry.Id, cancellationToken);
            else if (entry.Type == MessageType.Text) {
                await _publisher.Publish(new JournalEntryStored(entry.Id, entry.ChatId, entry.MessageId), cancellationToken);
            }
        }
    }
}