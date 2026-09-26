using MediatR;

namespace PersonalAssistant.Application.Features.Journal.Commands;

public record ProcessJournalMediaCommand(Guid EntryId) : IRequest;
