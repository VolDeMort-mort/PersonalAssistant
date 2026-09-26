namespace PersonalAssistant.Presentation.Helpers;

/// <summary>Shared look of the bot's screens: a centered title over a line, then the content.</summary>
public static class ScreenText
{
    // Under every title; as the widest line it also keeps windows the same width
    public const string Separator = "────────────────────────";

    /// <param name="title">Plain text: it is encoded here, after its width is measured.</param>
    /// <param name="body">HTML.</param>
    /// <param name="notice">HTML shown at the bottom, e.g. a confirmation or an error.</param>
    public static string Compose(string title, string body, string? notice = null)
    {
        var padding = TextLayout.CenterPadding(title, Separator, bold: true);
        var text = $"{padding}<b>{HtmlText.Encode(title)}</b>\n{Separator}\n{body}";
        return notice is null ? text : $"{text}\n\n{notice}";
    }
}
