using System.Text.RegularExpressions;
using PersonalAssistant.Application.Features.Finance.Dtos;
using PersonalAssistant.Domain.Entities.Finance;

namespace PersonalAssistant.Presentation.Bot.Finance;

/// <summary>
/// Reads shopping items out of a typed or shared message. This is the bot's input format, so it lives here:
/// Application only gets ready titles and links.
/// </summary>
public static partial class ShoppingText
{
    // Punctuation that ends a sentence rather than a link: "ось тут https://shop.ua/item."
    private static readonly char[] TrailingPunctuation = { '.', ',', ';', ':', '!', '?', ')', ']', '»', '"', '\'' };

    // Left between the title and a removed link: "Кросівки — https://…"
    private static readonly char[] Separators = { ' ', '-', '—', '–', ':', '|', '·' };

    [GeneratedRegex(@"https?://\S+", RegexOptions.IgnoreCase)]
    private static partial Regex LinkPattern();

    [GeneratedRegex(@"\s{2,}")]
    private static partial Regex Spaces();

    /// <summary>
    /// One item per line. A link in a line becomes the item's link and the rest its title.
    /// A line with only a link belongs to the line above it, which is how stores share a product:
    /// its name, then the link under it.
    /// </summary>
    public static IReadOnlyList<NewShoppingItem> ReadItems(string text)
    {
        var items = new List<NewShoppingItem>();

        foreach (var line in text.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0))
        {
            var (title, link) = Split(line);

            if (title is null && link is not null && items is [.., { Link: null } previous])
            {
                items[^1] = previous with { Link = link };
                continue;
            }

            title ??= link is not null ? HostOf(link) : null;
            if (title is not null)
                items.Add(new NewShoppingItem(Shorten(title), Link: link));
        }

        return items;
    }

    /// <summary>The first link in the text, for the link step of the wizard; null when there is none.</summary>
    public static string? FindLink(string text) => Split(text).Link;

    private static (string? Title, string? Link) Split(string line)
    {
        foreach (Match match in LinkPattern().Matches(line))
        {
            var link = match.Value.TrimEnd(TrailingPunctuation);
            if (!ShoppingItem.IsValidLink(link))
                continue;

            var rest = Spaces().Replace(line.Remove(match.Index, link.Length), " ").Trim(Separators);
            return (rest.Length > 0 ? rest : null, link);
        }

        return (line, null);
    }

    /// <summary>"rozetka.com.ua": the name of a link shared without one, and the text of links on screens.</summary>
    public static string HostOf(string link)
    {
        var host = new Uri(link).Host;
        return host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? host[4..] : host;
    }

    /// <summary>A shared description can be long: it is cut to fit rather than refused.</summary>
    private static string Shorten(string title) =>
        title.Length <= ShoppingItem.MaxTitleLength ? title : title[..(ShoppingItem.MaxTitleLength - 1)] + "…";
}
