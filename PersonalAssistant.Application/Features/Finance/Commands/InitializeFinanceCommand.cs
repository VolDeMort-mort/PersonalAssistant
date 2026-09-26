using MediatR;
using PersonalAssistant.Application.Interfaces;
using PersonalAssistant.Domain.Entities.Finance;

namespace PersonalAssistant.Application.Features.Finance.Commands;

/// <summary>
/// Prepares finances on the first visit: default categories and the starter template.
/// Returns true only on that first visit, so the caller can ask for the starting balance.
/// </summary>
public record InitializeFinanceCommand(long ChatId) : IRequest<bool>;

public class InitializeFinanceCommandHandler : IRequestHandler<InitializeFinanceCommand, bool>
{
    private const string TransportCategory = "🚌 Транспорт";

    private static readonly string[] ExpenseCategories =
    {
        "🍔 Їжа", TransportCategory, "🏠 Житло", "📱 Зв'язок і підписки",
        "💊 Здоров'я", "🎉 Розваги", "👕 Одяг", "📦 Інше"
    };

    private static readonly string[] IncomeCategories =
    {
        "💼 Зарплата", "💻 Фріланс", "🎁 Подарунок", "📦 Інше"
    };

    private readonly IFinanceCatalogRepository _catalog;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _time;

    public InitializeFinanceCommandHandler(IFinanceCatalogRepository catalog, IUnitOfWork unitOfWork, TimeProvider time)
    {
        _catalog = catalog;
        _unitOfWork = unitOfWork;
        _time = time;
    }

    public async Task<bool> Handle(InitializeFinanceCommand request, CancellationToken cancellationToken)
    {
        if (await _catalog.HasCategoriesAsync(request.ChatId, cancellationToken))
            return false;

        var categories = CreateCategories(request.ChatId, TransactionType.Expense, ExpenseCategories)
            .Concat(CreateCategories(request.ChatId, TransactionType.Income, IncomeCategories))
            .ToList();

        foreach (var category in categories)
            _catalog.AddCategory(category);

        var transport = categories.First(c => c.Name == TransportCategory);
        _catalog.AddTemplate(FinanceTemplate.Create(transport, "🚌 Проїзд", 30, _time.GetUtcNow().UtcDateTime));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static IEnumerable<FinanceCategory> CreateCategories(long chatId, TransactionType type, string[] names) =>
        names.Select((name, index) => FinanceCategory.Create(chatId, type, name, sortOrder: index));
}
