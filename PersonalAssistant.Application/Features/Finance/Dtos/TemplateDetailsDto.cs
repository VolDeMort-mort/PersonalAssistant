using PersonalAssistant.Domain.Entities.Finance;

namespace PersonalAssistant.Application.Features.Finance.Dtos;

/// <param name="Amount">Null: the amount is asked every time.</param>
public record TemplateDetailsDto(Guid Id, TransactionType Type, string Title, string CategoryName, long? Amount);
