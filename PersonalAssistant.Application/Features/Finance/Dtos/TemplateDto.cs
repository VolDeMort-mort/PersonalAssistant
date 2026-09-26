namespace PersonalAssistant.Application.Features.Finance.Dtos;

/// <param name="Amount">Null: the amount is asked every time.</param>
public record TemplateDto(Guid Id, string Title, long? Amount);
