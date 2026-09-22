using MediatR;
using Microsoft.AspNetCore.Mvc;
using PersonalAssistant.Application.Constants;
using PersonalAssistant.Application.Interfaces;
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

    public TelegramController(IMediator mediator, IJournalSessionManager sessionManager, IBotNotifService botNotifService)
    {
        _mediator = mediator;
        _sessionManager = sessionManager;
        _notifService = botNotifService;
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Post([FromBody] Update update)
    {
        if (update.Message == null)
            return Ok();

        var chatId = update.Message.Chat.Id;
        var text = update.Message.Text?.ToLower() ?? "";

        if (text.StartsWith(BotConstants.Commands.StartJournal))
        {
            await HandleStartJournalCommand(chatId);
        }
        else if (text.StartsWith(BotConstants.Commands.StopJournal))
        {
            await HandleStopJournalCommand(chatId);
        }
        else if (_sessionManager.IsRecording(chatId))
        {
            await HandleIncomingMessage(chatId, update.Message);
        }
        return Ok();
    }


    /// <summary>
    /// Launches a listening session
    /// </summary>
    private async Task HandleStartJournalCommand(long chatId)
    {
        _sessionManager.StartSession(chatId);
        await _notifService.SendMessageAsync(chatId, BotConstants.Messages.JournalOpened);
    }

    /// <summary>
    /// Stops listening session 
    /// </summary>
    private async Task HandleStopJournalCommand(long chatId)
    {
        if (!_sessionManager.IsRecording(chatId))
        {
            await _notifService.SendMessageAsync(chatId, BotConstants.Messages.JournalAlreadyClosed);
            return;
        }

        var sessionData = _sessionManager.EndSession(chatId);

        if (sessionData != null && sessionData.Value.Messages.Any())
        {
            var command = new SaveJournalSessionCommand(sessionData.Value.SessionId, sessionData.Value.Messages);
            await _mediator.Send(command);

            await _notifService.SendMessageAsync(chatId, BotConstants.Messages.JournalSaved(sessionData.Value.Messages.Count));
        }
        else
        {
            await _notifService.SendMessageAsync(chatId, BotConstants.Messages.JournalClosedEmpty);
        }
    }

    /// <summary>
    /// Listens and records messages from user
    /// </summary>
    private Task HandleIncomingMessage(long chatId, Message message)
    {
        System.Diagnostics.Debug.WriteLine($"[DEBUG] Message type received. Text: {message.Text != null}, Voice: {message.Voice != null}, Audio: {message.Audio != null}, Video: {message.Video != null}, VideoNote: {message.VideoNote != null}");
        
        if (!string.IsNullOrEmpty(message.Text))
        {
            _sessionManager.AddMessage(chatId, new SessionMessageDto(DtoMessageType.Text, message.Text, null, DateTime.UtcNow));
        }
        else if (message.Voice != null)
        {
            _sessionManager.AddMessage(chatId, new SessionMessageDto(DtoMessageType.Voice, null, message.Voice.FileId, DateTime.UtcNow));
        }
        else if (message.VideoNote != null)
        {
            _sessionManager.AddMessage(chatId, new SessionMessageDto(DtoMessageType.Video, null, message.VideoNote.FileId, DateTime.UtcNow));
        }

        return Task.CompletedTask;
    }

}