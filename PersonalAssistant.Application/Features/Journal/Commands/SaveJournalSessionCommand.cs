using MediatR;
using PersonalAssistant.Domain.Entities;

public enum DtoMessageType
{
    Text,
    Voice,
    Video
}

public record SessionMessageDto(DtoMessageType Type, string? Text, string? FileId, DateTime CreatedAt);

public record SaveJournalSessionCommand(Guid SessionId, List<SessionMessageDto> Messages) : IRequest;