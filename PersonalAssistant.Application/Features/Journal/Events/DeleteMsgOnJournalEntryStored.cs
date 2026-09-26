using MediatR;
using PersonalAssistant.Application.Interfaces;

namespace PersonalAssistant.Application.Features.Journal.Events;

public class DeleteMsgOnJournalEntryStoredHandler : INotificationHandler<JournalEntryStored>
{
    private readonly IBotNotifService _notifService;

    public DeleteMsgOnJournalEntryStoredHandler(IBotNotifService notifService)
    {
        _notifService = notifService;
    }

    public Task Handle(JournalEntryStored notification, CancellationToken cancellationToken)
        => _notifService.DeleteMessageAsync(notification.ChatId, notification.MessageId, cancellationToken);
}
