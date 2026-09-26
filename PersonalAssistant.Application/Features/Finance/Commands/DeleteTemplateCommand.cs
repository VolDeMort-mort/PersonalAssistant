using MediatR;
using PersonalAssistant.Application.Interfaces;

namespace PersonalAssistant.Application.Features.Finance.Commands;

/// <summary>
/// Transactions made from the template stay: they don't reference it.
/// Returns false when the template is already gone.
/// </summary>
public record DeleteTemplateCommand(long ChatId, Guid TemplateId) : IRequest<bool>;

public class DeleteTemplateCommandHandler : IRequestHandler<DeleteTemplateCommand, bool>
{
    private readonly IFinanceCatalogRepository _catalog;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteTemplateCommandHandler(IFinanceCatalogRepository catalog, IUnitOfWork unitOfWork)
    {
        _catalog = catalog;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = await _catalog.GetTemplateAsync(request.ChatId, request.TemplateId, cancellationToken);
        if (template is null)
            return false;

        _catalog.RemoveTemplate(template);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
