using PersonalAssistant.Domain.Entities.Finance;

namespace PersonalAssistant.Application.Interfaces;

/// <summary>
/// Changes are saved by <see cref="IUnitOfWork"/>.
/// </summary>
public interface IScheduledPaymentRepository
{
    void Add(ScheduledPayment payment);
    void Remove(ScheduledPayment payment);

    Task<ScheduledPayment?> GetByIdAsync(long chatId, Guid id, CancellationToken cancellationToken);

    /// <summary>Payments still to be paid, nearest first.</summary>
    Task<IReadOnlyList<ScheduledPayment>> GetActiveAsync(long chatId, CancellationToken cancellationToken);

    /// <summary>
    /// All chats: active payments with a reminder not yet sent, due no later than <paramref name="dueUntil"/>.
    /// The exact moment is checked by <see cref="ScheduledPayment.IsReminderDue"/>.
    /// </summary>
    Task<IReadOnlyList<ScheduledPayment>> GetReminderCandidatesAsync(DateOnly dueUntil, CancellationToken cancellationToken);
}
