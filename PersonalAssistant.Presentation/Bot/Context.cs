using Telegram.Bot.Types;

namespace PersonalAssistant.Presentation.Bot;

public record CallbackContext(long ChatId, int MessageId, string Payload);
public record MessageContext(long ChatId, int MessageId, Message Message);
