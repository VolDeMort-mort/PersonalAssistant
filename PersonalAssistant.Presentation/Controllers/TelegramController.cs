using MediatR;
using Microsoft.AspNetCore.Mvc;
using PersonalAssistant.Application.Constants;
using PersonalAssistant.Application.Interfaces;
using PersonalAssistant.Presentation.Helpers;
using PersonalAssistant.Application.Features.Journal.Commands;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;


namespace PersonalAssistant.Presentation.Controllers;


[ApiController]
[Route("api/[controller]")]
public class TelegramController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IJournalSessionManager _sessionManager;
    private readonly IBotNotifService _notifService;
    public readonly IUserStateManager _stateManager;

    public TelegramController(IMediator mediator,
        IJournalSessionManager sessionManager, 
        IBotNotifService botNotifService, 
        IUserStateManager stateManager)
    {
        _mediator = mediator;
        _sessionManager = sessionManager;
        _notifService = botNotifService;
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
                    await _notifService.EditMessageAsync(
                        chatId, messageId,
                        BotConstants.Message.MsgJournalMenu,
                        MenuBuilder.GetJournalMenu());
                    break;
                
                case BotConstants.Payloads.NavPlanner:
                    await _notifService.EditMessageAsync(
                        chatId, messageId,
                        BotConstants.Message.MsgNotImplementedFeature,
                        MenuBuilder.GetNotImplementedFeature());
                    break;

                case BotConstants.Payloads.NavScraper:
                    await _notifService.EditMessageAsync(
                        chatId, messageId,
                        BotConstants.Message.MsgNotImplementedFeature,
                        MenuBuilder.GetNotImplementedFeature());
                    break;

                case BotConstants.Payloads.NavJournalStart:
                    await HandleStartJournalCommand(chatId, messageId);
                    break;

                //case BotConstants.Payloads.NavJournalRecording:
                //    await _notifService.EditMessageAsync(
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
            else if (_sessionManager.IsRecording(chatId))
            {
                await HandleIncomingMessage(chatId, update.Message);
            }
        }
        return Ok();
    }



    private async Task HandleMenuCommand(long chatId, int messageId)
    {
        _stateManager.ClearState(chatId);
        await _notifService.EditMessageAsync(
            chatId, messageId,
            BotConstants.Message.MsgRootMenu,
            MenuBuilder.GetRootMenu());

    }


    private async Task HandleStartJournalCommand(long chatId, int messageId)
    {
        _stateManager.SetState(chatId, UserState.Journaling);
        _sessionManager.StartSession(chatId);
        await _notifService.EditMessageAsync(
                        chatId, messageId,
                        BotConstants.Message.MsgJournalRecording,
                        MenuBuilder.GetJournalRecording());
    }


    private async Task HandleCancelJournalCommand(long chatId, int messageId)
    {
        if (!_sessionManager.IsRecording(chatId))
        {
            await _notifService.EditMessageAsync(
                chatId, messageId,
                BotConstants.Message.MsgJournalRecordedEmpty,
                MenuBuilder.GetJournalRecorded());
            return;
        }

        var sessionData = _sessionManager.EndSession(chatId);

        await _notifService.EditMessageAsync(
            chatId, messageId,
            BotConstants.Message.MsgJournalRecordedEmpty,
            MenuBuilder.GetJournalRecorded());
    }


    private async Task HandleSaveJournalCommand(long chatId, int messageId)
    {
        if (!_sessionManager.IsRecording(chatId))
        {
            await _notifService.EditMessageAsync(
                chatId, messageId,
                BotConstants.Message.MsgJournalRecordedEmpty,
                MenuBuilder.GetJournalRecorded());
            return;
        }

        var sessionData = _sessionManager.EndSession(chatId);

        if (sessionData != null && sessionData.Value.Messages.Any())
        {
            var command = new SaveJournalSessionCommand(sessionData.Value.SessionId, chatId, sessionData.Value.Messages);
            await _mediator.Send(command);

            await _notifService.EditMessageAsync(
                chatId, messageId,
                BotConstants.Message.MsgJournalRecorded(sessionData.Value.Messages.Count),
                MenuBuilder.GetJournalRecorded());
        }
        else
        {
            await _notifService.EditMessageAsync(
                chatId, messageId,
                BotConstants.Message.MsgJournalRecordedEmpty,
                MenuBuilder.GetJournalRecorded());
        }
    }


    private Task HandleIncomingMessage(long chatId, Message message)
    {

        if (!string.IsNullOrEmpty(message.Text))
        {
            _sessionManager.AddMessage(chatId, new SessionMessageDto(DtoMessageType.Text, message.Text, null, DateTime.UtcNow, message.MessageId));
        }
        else if (message.Voice != null)
        {
            _sessionManager.AddMessage(chatId, new SessionMessageDto(DtoMessageType.Voice, null, message.Voice.FileId, DateTime.UtcNow, message.MessageId));
        }
        else if (message.VideoNote != null)
        {
            _sessionManager.AddMessage(chatId, new SessionMessageDto(DtoMessageType.Video, null, message.VideoNote.FileId, DateTime.UtcNow, message.MessageId));
        }

        return Task.CompletedTask;
    }

}