using MediatR;
using PersonalAssistant.Application.Interfaces;
using PersonalAssistant.Domain.Entities;

namespace PersonalAssistant.Application.Features.Journal.Queries;

public class GetJournalEntriesByDateQueryHandler : IRequestHandler<GetJournalEntriesByDateQuery, IEnumerable<JournalEntryResponseDto>>
{
    private readonly IJournalRepository _repository;

    public GetJournalEntriesByDateQueryHandler(IJournalRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<JournalEntryResponseDto>> Handle(GetJournalEntriesByDateQuery request, CancellationToken cancellationToken)
    {
        var entries = await _repository.GetByDateAsync(request.Date, cancellationToken);

        return entries.Select(e => new JournalEntryResponseDto(
            e.Id,
            e.CreatedAt,
            e.Type.ToString(),
            e.OriginalText,
            e.TranscribedText,
            e.IsProcessed
        ));
    }
}