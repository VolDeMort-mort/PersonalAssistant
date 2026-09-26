namespace PersonalAssistant.Domain.Common;

/// <summary>
/// Shared checks that keep entities valid: factories call them before an object is created.
/// </summary>
internal static class Guard
{
    public static long Positive(long value, string paramName) =>
        value > 0 ? value : throw new ArgumentOutOfRangeException(paramName, value, "Must be greater than zero.");

    public static string RequiredText(string? value, int maxLength, string paramName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, paramName);

        var text = value.Trim();
        return text.Length <= maxLength
            ? text
            : throw new ArgumentException($"Must be at most {maxLength} characters.", paramName);
    }

    public static string? OptionalText(string? value, int maxLength, string paramName) =>
        string.IsNullOrWhiteSpace(value) ? null : RequiredText(value, maxLength, paramName);

    public static DateTime Utc(DateTime value, string paramName) =>
        value.Kind == DateTimeKind.Utc ? value : throw new ArgumentException("Must be a UTC time.", paramName);
}
