using PersonalAssistant.Application.Features.Finance.Dtos;
using PersonalAssistant.Domain.Entities.Finance;

namespace PersonalAssistant.Presentation.Bot.Finance;

public enum PaymentStep
{
    Title,
    Amount,
    Category,
    NewCategoryName,
    Recurrence,

    // One of these, depending on the recurrence: weekly, monthly, or month then day for once/yearly
    Weekday,
    MonthDay,
    Month,
    Day,

    RemindDays,
    RemindHour,

    /// <summary>Paying a payment with another amount than expected.</summary>
    PaidAmount
}

/// <summary>
/// Unfinished input around scheduled payments: the new payment wizard,
/// or the amount actually paid when it differs from the expected one.
/// </summary>
public class PaymentDialog : BotDialog
{
    private PaymentDialog(PaymentStep step) => Step = step;

    public PaymentStep Step { get; set; }

    public string? Title { get; set; }
    public long? Amount { get; set; }
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public Recurrence? Recurrence { get; set; }

    /// <summary>Month picked for a one-time or yearly payment, as its first day.</summary>
    public DateOnly? Month { get; set; }

    public DateOnly? FirstDueDate { get; set; }
    public int? AnchorDay { get; set; }
    public int? RemindDaysBefore { get; set; }

    /// <summary>The payment being paid with another amount.</summary>
    public Guid? PaymentId { get; private init; }

    public bool IsPaying => PaymentId is not null;

    public static PaymentDialog NewPayment() => new(PaymentStep.Title);

    public static PaymentDialog OtherAmount(ScheduledPaymentDto payment) =>
        new(PaymentStep.PaidAmount) { PaymentId = payment.Id, Title = payment.Title };
}
