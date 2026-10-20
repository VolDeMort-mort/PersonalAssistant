using System.Globalization;
using System.Text;
using PersonalAssistant.Application.Features.Finance.Dtos;
using PersonalAssistant.Domain.Entities.Finance;
using PersonalAssistant.Presentation.Helpers;
using Telegram.Bot.Types.ReplyMarkups;

namespace PersonalAssistant.Presentation.Bot.Finance;

/// <summary>
/// Screens of the shopping list. Texts are HTML, so everything the user typed goes through <see cref="HtmlText.Encode"/>.
/// </summary>
public static class ShoppingView
{
    public const string LinkError = "⚠️ Не схоже на посилання. Надішліть адресу, що починається з <code>https://</code>";
    public const string ChooseWithButtonsError = "⚠️ Оберіть варіант кнопкою";
    public const string SavedNotice = "✅ Збережено";

    public static string AddedNotice(IReadOnlyList<string> titles) =>
        titles.Count == 1
            ? $"✅ Додано: «{HtmlText.Encode(titles[0])}»"
            : $"✅ Додано {titles.Count} {DateText.Plural(titles.Count, "товар", "товари", "товарів")}";

    public static string DeletedNotice(string title) => $"🗑 Видалено: «{HtmlText.Encode(title)}»";
    public static string RepeatedNotice(string title) => $"🔁 Знову в списку: «{HtmlText.Encode(title)}»";

    // ---------- The list ----------

    /// <param name="recorded">The expense of an item just bought: the list confirms it and offers undo.</param>
    public static BotScreen List(ShoppingListDto list, string? notice, TransactionDto? recorded)
    {
        var body = new StringBuilder();
        if (list.Items.Count == 0)
        {
            body.Append("Список порожній");
        }
        else
        {
            body.Append($"{list.Items.Count} {DateText.Plural(list.Items.Count, "товар", "товари", "товарів")}");
            if (list.Total > 0)
                body.Append($" · <b>{MoneyFormat.Amount(list.Total)}</b>");
            if (list.UnpricedCount > 0)
                body.Append($" · без ціни: {list.UnpricedCount}");

            // With one group the total already says it all
            if (list.GroupTotals.Count > 1)
                foreach (var group in list.GroupTotals)
                    body.Append('\n').Append($"{HtmlText.Encode(group.CategoryName ?? "Без категорії")} · {MoneyFormat.Amount(group.Total)}");
        }

        body.Append('\n').Append(ScreenText.Separator)
            .Append("\n<i>Надішліть назву або посилання — додам товар. Кілька рядків — кілька товарів</i>");

        notice ??= recorded is null ? null : FinanceView.RecordedNotice(recorded);

        var rows = new List<InlineKeyboardButton[]>();
        if (recorded is not null)
            rows.Add(new[] { Button($"↩️ Скасувати: {FinanceView.Label(recorded)}", ShoppingPayloads.Undo(recorded.Id)) });

        // Sorted by category already: one per row keeps the order easy to read
        rows.AddRange(list.Items.Select(i => new[] { Button(ItemButton(i), ShoppingPayloads.Item(i.Id)) }));

        rows.Add(new[] { Button("➕ Додати", ShoppingPayloads.New), Button("✅ Куплене", ShoppingPayloads.Bought(page: 1)) });
        rows.Add(new[] { Button("🔙 Назад", FinancePayloads.Templates(TransactionType.Expense)) });

        return new BotScreen(ScreenText.Compose("🛒 Список покупок", body.ToString(), notice), new InlineKeyboardMarkup(rows));
    }

    /// <param name="confirmDelete">Asks "delete it?" instead of offering the actions.</param>
    public static BotScreen Card(ShoppingItemDto item, string? notice, bool confirmDelete)
    {
        var body = string.Join('\n',
            $"Ціна: {(item.Price is { } price ? $"<b>{MoneyFormat.Amount(price)}</b>" : "невідома")}",
            $"Категорія: {(item.CategoryName is { } category ? HtmlText.Encode(category) : "без категорії")}",
            $"Лінк: {(item.Link is { } link ? LinkHtml(link) : "немає")}");

        if (confirmDelete)
        {
            var confirmKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    Button("🗑 Так, видалити", ShoppingPayloads.ConfirmDelete(item.Id)),
                    Button("✖️ Ні", ShoppingPayloads.Item(item.Id))
                }
            });
            return new BotScreen(ScreenText.Compose(item.Title, body + "\n\nВидалити товар зі списку?"), confirmKeyboard);
        }

        var rows = new List<InlineKeyboardButton[]> { new[] { Button("✅ Куплено", ShoppingPayloads.Buy(item.Id)) } };
        if (item.Link is { } url)
            rows.Add(new[] { InlineKeyboardButton.WithUrl("🔗 Відкрити", url) });
        rows.Add(new[] { Button("✏️ Назва", ShoppingPayloads.EditTitle(item.Id)), Button("💰 Ціна", ShoppingPayloads.EditPrice(item.Id)) });
        rows.Add(new[] { Button("🌐 Лінк", ShoppingPayloads.EditLink(item.Id)), Button("🏷 Категорія", ShoppingPayloads.EditCategory(item.Id)) });
        rows.Add(new[] { Button("🗑 Видалити", ShoppingPayloads.Delete(item.Id)), Button("🔙 Назад", ShoppingPayloads.List) });

        return new BotScreen(ScreenText.Compose(item.Title, body, notice), new InlineKeyboardMarkup(rows));
    }

    // ---------- Steps: the new item wizard, editing a field, buying ----------

    /// <param name="categories">Needed only on the category steps.</param>
    public static BotScreen Dialog(ShoppingDialog dialog, IReadOnlyList<CategoryDto>? categories, string? error)
    {
        var body = new StringBuilder();
        if (dialog.IsNewItem && DraftSummary(dialog) is { } draft)
            body.Append(draft).Append("\n\n");
        body.Append(Prompt(dialog));

        var title = dialog.IsNewItem ? "🛒 Новий товар" : dialog.Title ?? "";
        return new BotScreen(ScreenText.Compose(title, body.ToString(), error), DialogKeyboard(dialog, categories));
    }

    /// <summary>What the wizard has so far, e.g. "«Кросівки» · 2 500 ₴ · 🔗 rozetka.com.ua".</summary>
    private static string? DraftSummary(ShoppingDialog dialog)
    {
        var parts = new List<string>();
        if (dialog.Title is not null)
            parts.Add($"«{HtmlText.Encode(dialog.Title)}»");
        if (dialog.Price is { } price)
            parts.Add($"<b>{MoneyFormat.Amount(price)}</b>");
        if (dialog.Link is { } link)
            parts.Add($"🔗 {HtmlText.Encode(ShoppingText.HostOf(link))}");

        return parts.Count == 0 ? null : string.Join(" · ", parts);
    }

    private static string Prompt(ShoppingDialog dialog) => dialog.Step switch
    {
        ShoppingStep.Title when dialog.IsNewItem => "Крок 1 з 4 · Надішліть назву товару, напр. <code>Кросівки</code>",
        ShoppingStep.Title => "Надішліть нову назву",
        ShoppingStep.Price when dialog.IsNewItem => "Крок 2 з 4 · Скільки коштує? Надішліть суму, напр. <code>1500</code>",
        ShoppingStep.Price => "Надішліть ціну, напр. <code>1500</code>",
        ShoppingStep.Link when dialog.IsNewItem => "Крок 3 з 4 · Надішліть посилання на товар",
        ShoppingStep.Link => "Надішліть посилання на товар",
        ShoppingStep.Category when dialog.IsNewItem => "Крок 4 з 4 · Оберіть категорію або надішліть назву нової",
        ShoppingStep.Category => "Оберіть категорію або надішліть назву нової",
        ShoppingStep.BuyCategory => "У якій категорії записати витрату? Оберіть або надішліть назву нової",
        ShoppingStep.BuyConfirm => "Скільки ви заплатили?",
        ShoppingStep.BuyAmount => "Скільки ви заплатили? Надішліть суму, напр. <code>1500</code>",
        _ => "Надішліть назву нової категорії. Емодзі на початку — за бажанням, напр. <code>🎮 Ігри</code>"
    };

    private static InlineKeyboardMarkup DialogKeyboard(ShoppingDialog dialog, IReadOnlyList<CategoryDto>? categories)
    {
        var rows = new List<InlineKeyboardButton[]>();

        switch (dialog.Step)
        {
            case ShoppingStep.Category:
                rows.AddRange(KeyboardLayout.Pack((categories ?? Array.Empty<CategoryDto>())
                    .Select(c => Button(c.Name, ShoppingPayloads.Category(c.Id)))));
                rows.Add(new[] { Button("➕ Нова категорія", ShoppingPayloads.NewCategory), Button("Без категорії", ShoppingPayloads.NoCategory) });
                break;

            case ShoppingStep.BuyCategory:
                rows.AddRange(KeyboardLayout.Pack((categories ?? Array.Empty<CategoryDto>())
                    .Select(c => Button(c.Name, ShoppingPayloads.BuyCategory(c.Id)))));
                rows.Add(new[] { Button("➕ Нова категорія", ShoppingPayloads.NewCategory) });
                break;

            case ShoppingStep.BuyConfirm when dialog is { ItemId: { } itemId, ExpectedPrice: { } price }:
                rows.Add(new[]
                {
                    Button($"✅ {MoneyFormat.Amount(price)}", ShoppingPayloads.BuyExpected(itemId)),
                    Button("✏️ Інша сума", ShoppingPayloads.BuyOther(itemId))
                });
                break;
        }

        // The step's own way to skip or go back shares the last row with the way out
        var lastRow = new List<InlineKeyboardButton>();
        switch (dialog.Step)
        {
            case ShoppingStep.Price:
                lastRow.Add(Button(dialog.IsNewItem ? "⏭ Пропустити" : "🗑 Без ціни", ShoppingPayloads.Skip));
                break;
            case ShoppingStep.Link:
                lastRow.Add(Button(dialog.IsNewItem ? "⏭ Пропустити" : "🗑 Без лінка", ShoppingPayloads.Skip));
                break;
            case ShoppingStep.NewCategoryName or ShoppingStep.BuyNewCategoryName:
                lastRow.Add(Button("🔙 До категорій", ShoppingPayloads.BackToCategories));
                break;
        }
        lastRow.Add(Button("✖️ Скасувати", ShoppingPayloads.Cancel));
        rows.Add(lastRow.ToArray());

        return new InlineKeyboardMarkup(rows);
    }

    // ---------- Bought ----------

    public static BotScreen Bought(BoughtItemsPageDto page, string? notice)
    {
        var body = page.TotalCount == 0
            ? "Ще нічого не куплено"
            : $"{page.TotalCount} {DateText.Plural(page.TotalCount, "покупка", "покупки", "покупок")} · сторінка {page.Page} з {page.PageCount}";

        // The latest first, one per row like the history of operations
        var rows = page.Items
            .Select(i => new[] { Button(BoughtButton(i), ShoppingPayloads.BoughtItem(i.Id, page.Page)) })
            .ToList();

        if (page.PageCount > 1)
        {
            var pages = new List<InlineKeyboardButton>();
            if (page.Page > 1)
                pages.Add(Button("◀️", ShoppingPayloads.Bought(page.Page - 1)));
            pages.Add(Button($"{page.Page} / {page.PageCount}", ShoppingPayloads.Bought(page.Page)));
            if (page.Page < page.PageCount)
                pages.Add(Button("▶️", ShoppingPayloads.Bought(page.Page + 1)));
            rows.Add(pages.ToArray());
        }
        rows.Add(new[] { Button("🔙 До списку", ShoppingPayloads.List) });

        return new BotScreen(ScreenText.Compose("✅ Куплене", body, notice), new InlineKeyboardMarkup(rows));
    }

    /// <param name="page">The page of purchases to return to.</param>
    public static BotScreen BoughtCard(BoughtItemDto item, int page)
    {
        var lines = new List<string>
        {
            $"Куплено: {DateText.Short(DateOnly.FromDateTime(item.BoughtAtLocal))} · "
                + item.BoughtAtLocal.ToString("HH:mm", CultureInfo.InvariantCulture)
        };
        if (item.PaidAmount is { } amount)
            lines.Add($"Сума: <b>{MoneyFormat.Amount(amount)}</b>");
        if (item.CategoryName is { } category)
            lines.Add($"Категорія: {HtmlText.Encode(category)}");
        if (item.Link is { } link)
            lines.Add($"Лінк: {LinkHtml(link)}");

        var firstRow = new List<InlineKeyboardButton> { Button("🔁 Купити ще раз", ShoppingPayloads.BuyAgain(item.Id)) };
        // The expense card returns to the day of the purchase in the calendar
        if (item.TransactionId is { } transactionId)
            firstRow.Add(Button("📜 Витрата",
                FinancePayloads.Transaction(transactionId, new ReturnTo.Day(DateOnly.FromDateTime(item.BoughtAtLocal)))));

        var rows = new List<InlineKeyboardButton[]> { firstRow.ToArray() };
        if (item.Link is { } url)
            rows.Add(new[] { InlineKeyboardButton.WithUrl("🔗 Відкрити", url) });
        rows.Add(new[] { Button("🔙 Назад", ShoppingPayloads.Bought(page)) });

        return new BotScreen(ScreenText.Compose(item.Title, string.Join('\n', lines)), new InlineKeyboardMarkup(rows));
    }

    // ---------- Helpers ----------

    /// <summary>"👕 Кросівки · 2 500 ₴ 🔗": the category's emoji, the title, the price and whether there is a link.</summary>
    private static string ItemButton(ShoppingItemDto item)
    {
        var text = new StringBuilder();
        if (CategoryIcon(item.CategoryName) is { } icon)
            text.Append(icon).Append(' ');
        text.Append(item.Title);
        if (item.Price is { } price)
            text.Append(" · ").Append(MoneyFormat.Amount(price));
        if (item.Link is not null)
            text.Append(" 🔗");
        return text.ToString();
    }

    private static string BoughtButton(BoughtItemDto item)
    {
        var date = item.BoughtAtLocal.ToString("dd.MM", CultureInfo.InvariantCulture);
        return item.PaidAmount is { } amount
            ? $"{date} · {item.Title} · {MoneyFormat.Amount(amount)}"
            : $"{date} · {item.Title}";
    }

    /// <summary>The emoji a category name starts with, e.g. "👕" of "👕 Одяг"; null when it starts with a letter.</summary>
    private static string? CategoryIcon(string? categoryName)
    {
        if (string.IsNullOrWhiteSpace(categoryName))
            return null;

        // A whole emoji with its modifiers, not just its first char
        var first = StringInfo.GetNextTextElement(categoryName);
        return first.Any(char.IsLetterOrDigit) ? null : first;
    }

    /// <summary>A clickable link showing just the site's name.</summary>
    private static string LinkHtml(string link) =>
        $"<a href=\"{HtmlText.Encode(link).Replace("\"", "&quot;")}\">{HtmlText.Encode(ShoppingText.HostOf(link))}</a>";

    private static InlineKeyboardButton Button(string text, string payload) =>
        InlineKeyboardButton.WithCallbackData(text, payload);
}
