using PersonalAssistant.Application.Features.Finance.Dtos;
using PersonalAssistant.Domain.Entities.Finance;

namespace PersonalAssistant.Presentation.Bot.Finance;

public enum FinanceDialogKind
{
    /// <summary>Amount → category → comment.</summary>
    ManualEntry,

    /// <summary>Title → category → amount or "ask every time".</summary>
    NewTemplate,

    /// <summary>Amount for a template without a fixed one.</summary>
    TemplateUse,

    /// <summary>Starting balance or a correction.</summary>
    Balance
}

public enum FinanceStep
{
    Amount,
    Title,
    Category,
    NewCategoryName,
    Comment,
    TemplateAmount,
    Balance
}

/// <summary>
/// Unfinished input: which step the user is on and what was entered so far.
/// It is UI state, so it lives in Presentation and only in memory: a restart drops
/// the unfinished input, while everything already saved stays.
/// </summary>
public class FinanceDialog
{
    private FinanceDialog(FinanceDialogKind kind, TransactionType type, FinanceStep step)
    {
        Kind = kind;
        Type = type;
        Step = step;
    }

    public FinanceDialogKind Kind { get; }

    /// <summary>Income or expense being entered; not used by <see cref="FinanceDialogKind.Balance"/>.</summary>
    public TransactionType Type { get; }

    public FinanceStep Step { get; set; }

    /// <summary>
    /// The window the dialog is drawn in. After /menu opens a new window,
    /// typed text is no longer meant for this dialog.
    /// </summary>
    public int? ScreenId { get; set; }

    public long? Amount { get; set; }
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? Title { get; set; }
    public Guid? TemplateId { get; private init; }
    public bool IsFirstVisit { get; private init; }
    public long? KnownBalance { get; private init; }

    public static FinanceDialog ManualEntry(TransactionType type) =>
        new(FinanceDialogKind.ManualEntry, type, FinanceStep.Amount);

    public static FinanceDialog NewTemplate(TransactionType type) =>
        new(FinanceDialogKind.NewTemplate, type, FinanceStep.Title);

    public static FinanceDialog TemplateUse(TemplateDetailsDto template) =>
        new(FinanceDialogKind.TemplateUse, template.Type, FinanceStep.Amount)
        {
            TemplateId = template.Id,
            Title = template.Title
        };

    public static FinanceDialog Balance(bool isFirstVisit, long knownBalance) =>
        new(FinanceDialogKind.Balance, TransactionType.Income, FinanceStep.Balance)
        {
            IsFirstVisit = isFirstVisit,
            KnownBalance = knownBalance
        };
}
