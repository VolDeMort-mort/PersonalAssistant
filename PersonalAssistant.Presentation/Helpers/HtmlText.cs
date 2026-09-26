namespace PersonalAssistant.Presentation.Helpers;

public static class HtmlText
{
    /// <summary>
    /// User text inside an HTML message: Telegram rejects the whole message if &lt;, &gt; or &amp; are left as is.
    /// Button texts are plain text and must not be encoded.
    /// </summary>
    public static string Encode(string text) =>
        text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
}
