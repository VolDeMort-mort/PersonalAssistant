using MediatR;
using PersonalAssistant.Application.Interfaces;

namespace PersonalAssistant.Application.Features.Journal.Queries;

public record IsJournalRecordingQuery(long ChatId) : IRequest<bool>;

public class IsJournalRecordingQueryHandler : IRequestHandler<IsJournalRecordingQuery, bool>
{
    private readonly IJournalSessionManager _sessions;
    public IsJournalRecordingQueryHandler(IJournalSessionManager sessions) => _sessions = sessions;

    public Task<bool> Handle(IsJournalRecordingQuery request, CancellationToken cancellationToken)
        => Task.FromResult(_sessions.IsRecording(request.ChatId));
}
