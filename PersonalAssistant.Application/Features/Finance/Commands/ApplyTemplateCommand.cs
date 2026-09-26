using MediatR;
using PersonalAssistant.Application.Common.Exceptions;
using PersonalAssistant.Application.Interfaces;
using PersonalAssistant.Domain.Entities.Finance;

namespace PersonalAssistant.Application.Features.Finance.Commands;

/// <summary>
/// Records a transaction from a template. Returns the new transaction id.
/// </summary>
/// <param name="Amount">Required when the template has no fixed amount; otherwise overrides it.</param>
public record ApplyTemplateCommand(long ChatId, Guid TemplateId, long? Amount = null) : IRequest<Guid>;

public class ApplyTemplateCommandHandler : IRequestHandler<ApplyTemplateCommand, Guid>
{
    private readonly IFinanceCatalogRepository _catalog;
    private readonly IFinanceTransactionRepository _transactions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _time;

    public ApplyTemplateCommandHandler(IFinanceCatalogRepository catalog, IFinanceTransactionRepository transactions,
        IUnitOfWork unitOfWork, TimeProvider time)
    {
        _catalog = catalog;
        _transactions = transactions;
        _unitOfWork = unitOfWork;
        _time = time;
    }

    public async Task<Guid> Handle(ApplyTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = await _catalog.GetTemplateAsync(request.ChatId, request.TemplateId, cancellationToken)
            ?? throw new NotFoundException(nameof(FinanceTemplate), request.TemplateId);

        var category = await _catalog.GetCategoryAsync(request.ChatId, template.CategoryId, cancellationToken)
            ?? throw new NotFoundException(nameof(FinanceCategory), template.CategoryId);

        var transaction = template.CreateTransaction(category, request.Amount, _time.GetUtcNow().UtcDateTime);
        _transactions.Add(transaction);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return transaction.Id;
    }
}
