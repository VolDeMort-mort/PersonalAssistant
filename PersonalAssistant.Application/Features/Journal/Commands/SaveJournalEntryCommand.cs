using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MediatR;
using PersonalAssistant.Application.Interfaces;
using PersonalAssistant.Domain.Entities;

namespace PersonalAssistant.Application.Features.Journal.Commands;

public record SaveJournalEntryCommand(string Text, string? EmotionTag = null) : IRequest<Guid>;


public class SaveJournalEntryCommandHandler : IRequestHandler<SaveJournalEntryCommand, Guid>
{
    private readonly IJournalRepository _repository;

    public SaveJournalEntryCommandHandler(IJournalRepository repository)
    {
        _repository = repository;
    }

    public async Task<Guid> Handle(SaveJournalEntryCommand request, CancellationToken cancellationToken)
    {
        // Створюємо доменну сутність
        var entry = new JournalEntry(request.Text, request.EmotionTag);

        // Зберігаємо через абстракцію            
        await _repository.AddAsync(entry, cancellationToken);

        return entry.Id;
    }
}
