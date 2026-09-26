using MediatR;
using PersonalAssistant.Application.Interfaces;
using PersonalAssistant.Domain.Entities.Finance;

namespace PersonalAssistant.Application.Features.Finance.Commands;

/// <summary>
/// Starting balance and later corrections: records the difference between the real money
/// and what the bot knows. Returns the adjustment id, or null when nothing had to change.
/// </summary>
public record SetBalanceCommand(long ChatId, long ActualBalance) : IRequest<Guid?>;

public class SetBalanceCommandHandler : IRequestHandler<SetBalanceCommand, Guid?>
{
    private readonly IFinanceTransactionRepository _transactions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _time;

    public SetBalanceCommandHandler(IFinanceTransactionRepository transactions, IUnitOfWork unitOfWork, TimeProvider time)
    {
        _transactions = transactions;
        _unitOfWork = unitOfWork;
        _time = time;
    }

    public async Task<Guid?> Handle(SetBalanceCommand request, CancellationToken cancellationToken)
    {
        var currentBalance = await _transactions.GetBalanceAsync(request.ChatId, cancellationToken);
        var difference = request.ActualBalance - currentBalance;
        if (difference == 0)
            return null;

        var adjustment = FinanceTransaction.CreateAdjustment(request.ChatId, difference, _time.GetUtcNow().UtcDateTime);
        _transactions.Add(adjustment);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return adjustment.Id;
    }
}
