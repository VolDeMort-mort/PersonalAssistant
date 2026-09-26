using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using PersonalAssistant.Presentation.Bot;
using PersonalAssistant.Presentation.Bot.Options;
using Telegram.Bot.Types;
using Microsoft.Extensions.Options;

namespace PersonalAssistant.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TelegramController : ControllerBase
{
    private const string SecretHeader = "X-Telegram-Bot-Api-Secret-Token";

    private readonly IUpdateQueue _queue;
    private readonly TelegramOptions _options;

    public TelegramController(IUpdateQueue queue, IOptions<TelegramOptions> options)
    {
        _queue = queue;
        _options = options.Value;
    }

    [HttpPost("webhook")]
    public IActionResult Post([FromBody] Update update)
    {
        if (!IsValidSecret(Request.Headers[SecretHeader].ToString()))
            return Unauthorized();

        return _queue.TryEnqueue(update)
            ? Ok()
            : StatusCode(StatusCodes.Status503ServiceUnavailable);
    }

    private bool IsValidSecret(string received) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(received),
            Encoding.UTF8.GetBytes(_options.SecretToken));
}
