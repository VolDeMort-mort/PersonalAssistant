using MediatR;
using PersonalAssistant.Application.Interfaces;

namespace PersonalAssistant.Application.Features.Journal.Commands;

public class DeleteTelegramMessagesCommandHandler : IRequestHandler<DeleteTelegramMessagesCommand>
{
    private readonly IBotNotifService _notifService;

    public DeleteTelegramMessagesCommandHandler(IBotNotifService notificationService)
    {
        _notifService = notificationService;
    }

    public async Task Handle(DeleteTelegramMessagesCommand request, CancellationToken cancellationToken)
    {
        foreach (var messageId in request.MessageIds)
        {
            await _notifService.DeleteMessageAsync(request.ChatId, messageId, cancellationToken);
        }
    }
}