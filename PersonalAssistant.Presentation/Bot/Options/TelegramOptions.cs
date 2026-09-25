using System.ComponentModel.DataAnnotations;

namespace PersonalAssistant.Presentation.Options;

public class TelegramOptions
{
    public const string SectionName = "TelegramBot";

    [Required] public string Token { get; set; } = "";
    [Required] public string SecretToken { get; set; } = "";   // 1-256 символів: A-Z a-z 0-9 _ -
    public string? WebhookUrl { get; set; }                    // повний URL з /api/telegram/webhook
    public long OwnerId { get; set; }                          // ваш Telegram user id
}
