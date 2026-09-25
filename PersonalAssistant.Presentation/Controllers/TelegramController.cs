using MediatR;
using Microsoft.AspNetCore.Mvc;
using PersonalAssistant.Presentation.Constants;
using PersonalAssistant.Application.Interfaces;
using PersonalAssistant.Presentation.Helpers;
using PersonalAssistant.Presentation.Services;
using PersonalAssistant.Application.Features.Journal.Commands;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using PersonalAssistant.Application.Features.Journal.Queries;


namespace PersonalAssistant.Presentation.Controllers;


[ApiController]
[Route("api/[controller]")]
public class TelegramController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IJournalSessionManager _sessionManager;
    private readonly IBotMessenger _messenger;
    public readonly IUserStateManager _stateManager;

    public TelegramController(IMediator mediator,
        IJournalSessionManager sessionManager, 
        IBotMessenger messenger, 
        IUserStateManager stateManager)
    {
        _mediator = mediator;
        _sessionManager = sessionManager;
        _messenger = messenger;
        _stateManager = stateManager;
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Post([FromBody] Update update)
    {
        if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery?.Message != null)
        {
            var chatId = update.CallbackQuery.Message.Chat.Id;
            var messageId = update.CallbackQuery.Message.MessageId;
            var payload = update.CallbackQuery.Data;

            switch (payload)
            {
                case BotConstants.Payloads.NavRoot:
                    await HandleMenuCommand(chatId, messageId);
                    break;

                case BotConstants.Payloads.NavJournal:
                    await _messenger.EditAsync(
                        chatId, messageId,
                        BotConstants.Message.MsgJournalMenu,
                        MenuBuilder.GetJournalMenu());
                    break;
                
                case BotConstants.Payloads.NavPlanner:
                    await _messenger.EditAsync(
                        chatId, messageId,
                        BotConstants.Message.MsgNotImplementedFeature,
                        MenuBuilder.GetNotImplementedFeature());
                    break;

                case BotConstants.Payloads.NavScraper:
                    await _messenger.EditAsync(
                        chatId, messageId,
                        BotConstants.Message.MsgNotImplementedFeature,
                        MenuBuilder.GetNotImplementedFeature());
                    break;

                case BotConstants.Payloads.NavJournalStart:
                    await HandleStartJournalCommand(chatId, messageId);
                    break;

                //case BotConstants.Payloads.NavJournalRecording:
                //    await _messenger.EditAsync(
                //        chatId, messageId,
                //        BotConstants.Message.MsgJournalRecording,
                //        MenuBuilder.GetJournalRecording());
                //    break;

                case BotConstants.Payloads.NavJournalRecorded:
                    await HandleSaveJournalCommand(chatId, messageId);
                    break;

                case BotConstants.Payloads.NavJournalCancelRecord:
                    await HandleCancelJournalCommand(chatId, messageId);
                    break;


            }

            return Ok();
        }

        if (update.Type == UpdateType.Message && update.Message != null)
        {
            var chatId = update.Message.Chat.Id;
            var messageId = update.Message?.MessageId ?? 0;
            var text = update.Message.Text?.ToLower() ?? "";

            
            if (text.StartsWith(BotConstants.Commands.MainMenu)) {
                await HandleMenuCommand(chatId, messageId);
            }
            else if (await _mediator.Send(new IsJournalRecordingQuery(chatId)))
            {
                await HandleIncomingMessage(chatId, update.Message);
            }
        }
        return Ok();
    }



    private async Task HandleMenuCommand(long chatId, int messageId)
    {
        _stateManager.ClearState(chatId);
        await _messenger.EditAsync(
            chatId, messageId,
            BotConstants.Message.MsgRootMenu,
            MenuBuilder.GetRootMenu());

    }


    private async Task HandleStartJournalCommand(long chatId, int messageId)
    {
        _stateManager.SetState(chatId, UserState.Journaling);
        await _mediator.Send(new StartJournalSessionCommand(chatId));
        await _messenger.EditAsync(chatId, messageId,
            BotConstants.Message.MsgJournalRecording, MenuBuilder.GetJournalRecording());
    }


    private async Task HandleCancelJournalCommand(long chatId, int messageId)
    {
        await _mediator.Send(new CancelJournalSessionCommand(chatId));
        await _messenger.EditAsync(chatId, messageId,
            BotConstants.Message.MsgJournalRecordedEmpty, MenuBuilder.GetJournalRecorded());
    }


    private async Task HandleSaveJournalCommand(long chatId, int messageId)
    {
        var savedCount = await _mediator.Send(new SaveJournalSessionCommand(chatId));

        var text = savedCount > 0
        ? BotConstants.Message.MsgJournalRecorded(savedCount)
        : BotConstants.Message.MsgJournalRecordedEmpty;

        await _messenger.EditAsync(chatId, messageId, text, MenuBuilder.GetJournalRecorded());
    }


    private async Task HandleIncomingMessage(long chatId, Message message)
    {

        if (!string.IsNullOrEmpty(message.Text))
        {
            SessionMessageDto sessionMessage = new SessionMessageDto(DtoMessageType.Text, message.Text, null, DateTime.UtcNow, message.MessageId);
            await _mediator.Send(new AddJournalSessionMessageCommand(chatId, sessionMessage)); 
        }
        else if (message.Voice != null)
        {
             SessionMessageDto sessionMessage = new SessionMessageDto(DtoMessageType.Voice, null, message.Voice.FileId, DateTime.UtcNow, message.MessageId);
            await _mediator.Send(new AddJournalSessionMessageCommand(chatId, sessionMessage));

        }
        else if (message.VideoNote != null)
        {
            SessionMessageDto sessionMessage= new SessionMessageDto(DtoMessageType.Video, null, message.VideoNote.FileId, DateTime.UtcNow, message.MessageId);
            await _mediator.Send(new AddJournalSessionMessageCommand(chatId, sessionMessage));

        }
        
        await Task.CompletedTask;
    }

}