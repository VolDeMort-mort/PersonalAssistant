using MediatR;
using Microsoft.AspNetCore.Mvc;
using PersonalAssistant.Application.Features.Journal.Commands;
using PersonalAssistant.Presentation.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
namespace PersonalAssistant.Presentation.Controllers;


[ApiController]
[Route("api/[controller]")]
public class TelegramController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly JournalSessionManager _sessionManager;
    private readonly ITelegramBotClient _botClient;

    public TelegramController(IMediator mediator, JournalSessionManager sessionManager, ITelegramBotClient botClient)
    {
        _mediator = mediator;
        _sessionManager = sessionManager;
        _botClient = botClient;
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Post([FromBody] Update update)
    {
        if (update.Type != UpdateType.Message || update.Message?.Text == null)
            return Ok();

        var chatId = update.Message.Chat.Id;
        var text = update.Message.Text.ToLower() ?? "";

        if (text.StartsWith("/startjournal"))
        {
            await HandleJournalCommand(chatId);
        }
        else if (text.StartsWith("/stopjournal"))
        {
            await HandleStopCommand(chatId);
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
    private async Task HandleJournalCommand(long chatId)
    {
        _sessionManager.StartSession(chatId);
        await _botClient.SendMessage(chatId, "📖 Журнал відкрито. Я слухаю... (відправ текст, аудіо чи відео. Коли закінчиш - напиши /stop)");
    }
    

    /// <summary>
    /// Stops listening session 
    /// </summary>
    private async Task HandleStopCommand(long chatId)
    {
        if (!_sessionManager.IsRecording(chatId))
        {
            await _botClient.SendMessage(chatId, "Журнал і так був закритий.");
            return;
        }

        var fullJournalText = _sessionManager.EndSessionAndGetText(chatId);

        if (!string.IsNullOrWhiteSpace(fullJournalText))
        {
            var command = new SaveJournalEntryCommand(fullJournalText, "Telegram_Session");
            await _mediator.Send(command);
            await _botClient.SendMessage(chatId, "✅ Запис успішно збережено у щоденник!");
        }
        else
        {
            await _botClient.SendMessage(chatId, "Журнал зачинено. Ти нічого не записав 🤷‍♂️");
        }
    }


    /// <summary>
    /// Listens and records messages from user
    /// </summary>
    private Task HandleIncomingMessage(long chatId, Message message)
    {
        if (!string.IsNullOrEmpty(message.Text))
        {
            _sessionManager.AddMessage(chatId, message.Text);
        }
        else if (message.Voice != null || message.Audio != null)
        {
            _sessionManager.AddMessage(chatId, "[Аудіоповідомлення - очікує підключення Whisper]");
        }
        else if (message.Video != null || message.VideoNote != null)
        {
            _sessionManager.AddMessage(chatId, "[Відеоповідомлення - очікує підключення Whisper]");
        }

        return Task.CompletedTask;
    }
}