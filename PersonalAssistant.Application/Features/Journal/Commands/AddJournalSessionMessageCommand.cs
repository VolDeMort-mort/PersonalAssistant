using MediatR;
using PersonalAssistant.Application.Interfaces;

namespace PersonalAssistant.Application.Features.Journal.Commands;

public record AddJournalSessionMessageCommand(long ChatId, SessionMessageDto Message) : IRequest;

public class AddJournalSessionMessageCommandHandler : IRequestHandler<AddJournalSessionMessageCommand>
{
    private readonly IJournalSessionManager _sessions;
    public AddJournalSessionMessageCommandHandler(IJournalSessionManager sessions) => _sessions = sessions;

    public Task Handle(AddJournalSessionMessageCommand request, CancellationToken cancellationToken)
    {
        _sessions.AddMessage(request.ChatId, request.Message);
        return Task.CompletedTask;
    }
}
