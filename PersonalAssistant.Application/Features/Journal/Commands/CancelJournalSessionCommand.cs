using MediatR;
using PersonalAssistant.Application.Interfaces;

namespace PersonalAssistant.Application.Features.Journal.Commands;

public record CancelJournalSessionCommand(long ChatId) : IRequest;

public class CancelJournalSessionCommandHandler : IRequestHandler<CancelJournalSessionCommand>
{
    private readonly IJournalSessionManager _sessions;
    public CancelJournalSessionCommandHandler(IJournalSessionManager sessions) => _sessions = sessions;

    public Task Handle(CancelJournalSessionCommand request, CancellationToken cancellationToken)
    {
        _sessions.EndSession(request.ChatId);
        return Task.CompletedTask;
    }
}
