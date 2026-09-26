using PersonalAssistant.Domain.Entities.Finance;

namespace PersonalAssistant.Application.Features.Finance.Dtos;

/// <param name="CategoryName">Null for balance adjustments.</param>
/// <param name="CreatedAtLocal">In the user's time zone.</param>
public record TransactionDto(
    Guid Id,
    TransactionType Type,
    long Amount,
    string? CategoryName,
    string? Comment,
    bool IsAdjustment,
    DateTime CreatedAtLocal);
