using MediatR;
using PersonalAssistant.Application.Common.Exceptions;
using PersonalAssistant.Application.Interfaces;
using PersonalAssistant.Domain.Entities.Finance;

namespace PersonalAssistant.Application.Features.Finance.Commands;

/// <param name="Amount">Null: the amount will be asked every time the template is used.</param>
public record AddTemplateCommand(long ChatId, Guid CategoryId, string Title, long? Amount) : IRequest<Guid>;

public class AddTemplateCommandHandler : IRequestHandler<AddTemplateCommand, Guid>
{
    private readonly IFinanceCatalogRepository _catalog;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _time;

    public AddTemplateCommandHandler(IFinanceCatalogRepository catalog, IUnitOfWork unitOfWork, TimeProvider time)
    {
        _catalog = catalog;
        _unitOfWork = unitOfWork;
        _time = time;
    }

    public async Task<Guid> Handle(AddTemplateCommand request, CancellationToken cancellationToken)
    {
        var category = await _catalog.GetCategoryAsync(request.ChatId, request.CategoryId, cancellationToken)
            ?? throw new NotFoundException(nameof(FinanceCategory), request.CategoryId);

        var template = FinanceTemplate.Create(category, request.Title, request.Amount, _time.GetUtcNow().UtcDateTime);
        _catalog.AddTemplate(template);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return template.Id;
    }
}
