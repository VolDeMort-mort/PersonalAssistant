using MediatR;
using PersonalAssistant.Domain.Entities;

public enum DtoMessageType
{
    Text,
    Voice,
    Video
}

public record SessionMessageDto(DtoMessageType Type, string? Text, string? FileId, DateTime CreatedAt, int MessageId);

public record SaveJournalSessionCommand(Guid SessionId, long ChatId, List<SessionMessageDto> Messages) : IRequest;