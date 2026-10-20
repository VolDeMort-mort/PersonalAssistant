namespace PersonalAssistant.Application.Features.Finance.Dtos;

/// <summary>
/// What happens on a day, as plain facts: how to show them (which mark wins on a small button) is up to the screen.
/// </summary>
/// <param name="HasOverdue">A planned expense was due that day and is still not paid.</param>
/// <param name="HasDue">A planned expense is due that day, today or later.</param>
/// <param name="HasPaid">A planned expense was paid that day.</param>
/// <param name="HasIncome">Any income that day; balance adjustments do not count.</param>
/// <param name="HasExpenses">Any expense that day; balance adjustments do not count.</param>
public record CalendarDayDto(DateOnly Date, bool HasOverdue, bool HasDue, bool HasPaid, bool HasIncome, bool HasExpenses);

/// <param name="Month">First day of the month.</param>
/// <param name="Days">Every day of the month, in order.</param>
public record PaymentCalendarDto(DateOnly Month, IReadOnlyList<CalendarDayDto> Days);
