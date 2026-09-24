using MediatR;
using PersonalAssistant.Application.Interfaces;
using PersonalAssistant.Domain.Entities;

namespace PersonalAssistant.Application.Features.Journal.Commands;


public class SaveJournalSessionCommandHandler : IRequestHandler<SaveJournalSessionCommand>
{
    private readonly IMediator _mediator;
    private readonly IJournalRepository _repository;
    private readonly IAudioProcessQueue _queue;

    public SaveJournalSessionCommandHandler(
        IMediator mediator,
        IJournalRepository repository, 
        IAudioProcessQueue queue)
    {
        _mediator = mediator;
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
            // FIXXX!!! Not clean architecture approach
            // Deleting text tg messages from chat
            else if (entry.Type == MessageType.Text) {
                var deleteCmd = new DeleteTelegramMessagesCommand(entry.ChatId, new List<int> { entry.MessageId });
                await _mediator.Send(deleteCmd, cancellationToken);
            }
        }
    }
}