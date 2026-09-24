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
        var entries = request.Messages.Select(m => m.Type switch
        {
            DtoMessageType.Text => JournalEntry.CreateText(request.SessionId, request.ChatId, m.MessageId, m.Text, m.CreatedAt),
            DtoMessageType.Voice => JournalEntry.CreateMedia(request.SessionId, request.ChatId, m.MessageId, MessageType.Voice, m.FileId, m.CreatedAt),
            DtoMessageType.Video => JournalEntry.CreateMedia(request.SessionId, request.ChatId, m.MessageId, MessageType.Video, m.FileId, m.CreatedAt),
            _ => throw new ArgumentOutOfRangeException(nameof(m.Type), m.Type, null)
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