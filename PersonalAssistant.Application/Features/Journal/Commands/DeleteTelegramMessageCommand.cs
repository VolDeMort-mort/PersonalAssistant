using MediatR;

namespace PersonalAssistant.Application.Features.Journal.Commands;

public record DeleteTelegramMessagesCommand(long ChatId, List<int> MessageIds) : IRequest;