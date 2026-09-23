using MediatR;
using PersonalAssistant.Application.Interfaces;
using PersonalAssistant.Domain.Entities;

namespace PersonalAssistant.Application.Features.Journal.Commands;


public class SaveJournalSessionCommandHandler : IRequestHandler<SaveJournalSessionCommand>
{
    private readonly IJournalRepository _repository;
    private readonly IAudioProcessQueue _queue;

    public SaveJournalSessionCommandHandler(IJournalRepository repository, IAudioProcessQueue queue)
    {
        _repository = repository;
        _queue = queue;
    }

    public async Task Handle(SaveJournalSessionCommand request, CancellationToken cancellationToken)
    {
        var entries = request.Messages.Select(m => new JournalEntry
        {
            Id = Guid.NewGuid(),
            SessionId = request.SessionId,
            CreatedAt = m.CreatedAt,
            Type = (MessageType)m.Type,
            OriginalText = m.Text,
            TelegramFileId = m.FileId,
            IsProcessed = m.Type == DtoMessageType.Text
        }).ToList();

        await _repository.AddRangeAsync(entries, cancellationToken);

        foreach (var entry in entries.Where(e => e.Type == MessageType.Voice || e.Type == MessageType.Video))
        {
            await _queue.EnqueueAsync(entry.Id, cancellationToken);
        }
    }
}