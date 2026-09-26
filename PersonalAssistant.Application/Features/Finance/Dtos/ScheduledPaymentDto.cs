using PersonalAssistant.Domain.Entities.Finance;

namespace PersonalAssistant.Application.Features.Finance.Dtos;

/// <param name="Amount">Expected amount; the real one is chosen when paying.</param>
/// <param name="DaysLeft">Days until <paramref name="NextDueDate"/>: 0 today, negative when overdue.</param>
/// <param name="RemindDaysBefore">Null when there is no reminder.</param>
public record ScheduledPaymentDto(
    Guid Id,
    string Title,
    long Amount,
    string CategoryName,
    Recurrence Recurrence,
    DateOnly NextDueDate,
    int DaysLeft,
    int? RemindDaysBefore,
    TimeOnly? RemindAt)
{
    public bool IsOverdue => DaysLeft < 0;

    internal static ScheduledPaymentDto From(ScheduledPayment payment, string categoryName, DateOnly today) => new(
        payment.Id,
        payment.Title,
        payment.Amount,
        categoryName,
        payment.Recurrence,
        payment.NextDueDate,
        payment.NextDueDate.DayNumber - today.DayNumber,
        payment.RemindDaysBefore,
        payment.RemindAt);
}
