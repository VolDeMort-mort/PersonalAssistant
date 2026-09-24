using MediatR;

namespace PersonalAssistant.Application.Features.Journal.Commands;

public enum DtoMessageType
{
    Text,
    Voice,
    Video
}

public record SaveJournalSessionCommand(Guid SessionId, long ChatId, List<SessionMessageDto> Messages) : IRequest;