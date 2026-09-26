using System.Globalization;

namespace PersonalAssistant.Presentation.Bot;

/// <summary>
/// Parsed callback data "prefix:action" or "prefix:action:arg".
/// Telegram allows 64 bytes, a Guid in "N" format takes 32.
/// </summary>
/// <param name="Name">The action.</param>
/// <param name="Arg">Id, number or code, depending on the action.</param>
public record CallbackAction(string Name, string? Arg)
{
    public Guid? Id => Guid.TryParseExact(Arg, "N", out var id) ? id : null;

    public int? Number => int.TryParse(Arg, NumberStyles.None, CultureInfo.InvariantCulture, out var number) ? number : null;

    public static (string Name, string? Arg) Split(string payload, string prefix)
    {
        var parts = payload[prefix.Length..].Split(':', 2);
        return (parts[0], parts.Length > 1 ? parts[1] : null);
    }
}
