using MediatR;
using PersonalAssistant.Domain.Entities;

namespace PersonalAssistant.Application.Features.Journal.Queries;

public record GetJournalEntriesByDateQuery(DateTime Date) : IRequest<IEnumerable<JournalEntry>>;