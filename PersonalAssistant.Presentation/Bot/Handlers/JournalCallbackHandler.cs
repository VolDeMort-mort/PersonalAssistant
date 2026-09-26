using PersonalAssistant.Presentation.Constants;
using PersonalAssistant.Presentation.Services;
using PersonalAssistant.Presentation.Helpers;
using PersonalAssistant.Application.Interfaces;
using PersonalAssistant.Application.Features.Journal.Commands;
using MediatR;


namespace PersonalAssistant.Presentation.Bot.Handlers;
public class JournalCallbackHandler : ICallbackHandler
{
    private static readonly HashSet<string> Payloads = new()
    {
        BotConstants.Payloads.NavJournalStart,
        BotConstants.Payloads.NavJournalRecorded,
        BotConstants.Payloads.NavJournalCancelRecord
    };

    private readonly IMediator _mediator;
    private readonly IBotMessenger _messenger;
    private readonly IUserStateManager _stateManager;

    public JournalCallbackHandler(IMediator mediator, IBotMessenger messenger, IUserStateManager stateManager)
    {
        _mediator = mediator;
        _messenger = messenger;
        _stateManager = stateManager;
    }

    public bool CanHandle(string payload) => Payloads.Contains(payload);

    public Task HandleAsync(CallbackContext context, CancellationToken ct) => context.Payload switch
    {
        BotConstants.Payloads.NavJournalStart => StartAsync(context, ct),
        BotConstants.Payloads.NavJournalRecorded => SaveAsync(context, ct),
        BotConstants.Payloads.NavJournalCancelRecord => CancelAsync(context, ct),
        _ => Task.CompletedTask
    };
    
    private async Task StartAsync(CallbackContext context, CancellationToken cancellationToken)
    {
        _stateManager.SetState(context.ChatId, UserState.Journaling);
        await _mediator.Send(new StartJournalSessionCommand(context.ChatId), cancellationToken);
        await _messenger.ShowScreenAsync(context.ChatId,
            BotConstants.Message.MsgJournalRecording, MenuBuilder.GetJournalRecording(), cancellationToken);
    }

    private async Task CancelAsync(CallbackContext context, CancellationToken cancellationToken)
    {
        await _mediator.Send(new CancelJournalSessionCommand(context.ChatId), cancellationToken);
        await _messenger.ShowScreenAsync(context.ChatId,
            BotConstants.Message.MsgJournalRecordedEmpty, MenuBuilder.GetJournalRecorded(), cancellationToken);
    }

    private async Task SaveAsync(CallbackContext context, CancellationToken cancellationToken)
    {
        var savedCount = await _mediator.Send(new SaveJournalSessionCommand(context.ChatId), cancellationToken);

        var text = savedCount > 0
        ? BotConstants.Message.MsgJournalRecorded(savedCount)
        : BotConstants.Message.MsgJournalRecordedEmpty;

        await _messenger.ShowScreenAsync(context.ChatId, text, MenuBuilder.GetJournalRecorded(), cancellationToken);
    }


}
