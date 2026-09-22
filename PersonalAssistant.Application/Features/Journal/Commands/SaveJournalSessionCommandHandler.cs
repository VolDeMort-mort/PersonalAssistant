using MediatR;
using PersonalAssistant.Application.Interfaces;
using PersonalAssistant.Domain.Entities;

namespace PersonalAssistant.Application.Features.Journal.Commands;


public class SaveJournalSessionCommandHandler : IRequestHandler<SaveJournalSessionCommand>
{
    private readonly IJournalRepository _repository;

    public SaveJournalSessionCommandHandler(IJournalRepository repository)
    {
        _repository = repository;
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
    }
}