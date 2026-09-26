using MediatR;
using PersonalAssistant.Application.Interfaces;

namespace PersonalAssistant.Application.Features.Journal.Commands;

public record StartJournalSessionCommand(long ChatId) : IRequest;

public class StartJournalSessionCommandHandler : IRequestHandler<StartJournalSessionCommand>
{
    private readonly IJournalSessionManager _sessions;
    public StartJournalSessionCommandHandler(IJournalSessionManager sessions) => _sessions = sessions;

    public Task Handle(StartJournalSessionCommand request, CancellationToken cancellationToken)
    {
        _sessions.StartSession(request.ChatId);
        return Task.CompletedTask;
    }
}
