using MediatR;
using PersonalAssistant.Application.Interfaces;
using PersonalAssistant.Domain.Entities;

namespace PersonalAssistant.Application.Features.Journal.Queries;

public class GetJournalEntriesByDateQueryHandler : IRequestHandler<GetJournalEntriesByDateQuery, IEnumerable<JournalEntry>>
{
    private readonly IJournalRepository _repository;

    public GetJournalEntriesByDateQueryHandler(IJournalRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<JournalEntry>> Handle(GetJournalEntriesByDateQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetByDateAsync(request.Date, cancellationToken);
    }
}