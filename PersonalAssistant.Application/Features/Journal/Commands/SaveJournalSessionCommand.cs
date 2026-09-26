using MediatR;

namespace PersonalAssistant.Application.Features.Journal.Commands;

public enum DtoMessageType
{
    Text,
    Voice,
    Video
}

public record SaveJournalSessionCommand(long ChatId) : IRequest<int>;
