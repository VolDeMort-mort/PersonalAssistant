using System.Reflection.Metadata;
using MediatR;
using PersonalAssistant.Application.Features.Journal.Events;
using PersonalAssistant.Application.Interfaces;
using PersonalAssistant.Domain.Entities;

namespace PersonalAssistant.Application.Features.Journal.Commands;

public class SaveJournalSessionCommandHandler : IRequestHandler<SaveJournalSessionCommand, int>
{
    private readonly IPublisher _publisher;
    private readonly IJournalRepository _repository;
    private readonly IAudioProcessQueue _queue;
    private readonly IJournalSessionManager _sessions;

    public SaveJournalSessionCommandHandler(
        IPublisher publisher,
        IJournalRepository repository, 
        IAudioProcessQueue queue,
        IJournalSessionManager sessions)
    {
        _publisher = publisher;
        _repository = repository;
        _queue = queue;
        _sessions = sessions;
    }

    public async Task<int> Handle(SaveJournalSessionCommand request, CancellationToken cancellationToken)
    {
        var session = _sessions.EndSession(request.ChatId);
        if (session is null || session.Value.Messages.Count == 0)
        return 0;

        var (sessionId, messages) = session.Value;


        var entries = messages.Select(m => m.Type switch
        {
            DtoMessageType.Text => JournalEntry.CreateText(sessionId, request.ChatId, m.MessageId, m.Text, m.CreatedAt),
            DtoMessageType.Voice => JournalEntry.CreateMedia(sessionId, request.ChatId, m.MessageId, MessageType.Voice, m.FileId, m.CreatedAt),
            DtoMessageType.Video => JournalEntry.CreateMedia(sessionId, request.ChatId, m.MessageId, MessageType.Video, m.FileId, m.CreatedAt),
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

        return entries.Count;
    }
}