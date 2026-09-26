namespace PersonalAssistant.Application.Features.Finance.Dtos;

/// <param name="DaysLeft">Days until <paramref name="DueDate"/>: 0 today, negative when overdue.</param>
public record PaymentReminderDto(long ChatId, Guid PaymentId, string Title, long Amount, DateOnly DueDate, int DaysLeft);
