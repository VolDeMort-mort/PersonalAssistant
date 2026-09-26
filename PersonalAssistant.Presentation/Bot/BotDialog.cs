namespace PersonalAssistant.Presentation.Bot;

/// <summary>
/// Unfinished input of any feature. It is UI state, so it lives in Presentation and only in memory:
/// a restart drops the unfinished input, while everything already saved stays.
/// </summary>
public abstract class BotDialog
{
    /// <summary>
    /// The window the dialog is drawn in. After /menu opens a new window,
    /// typed text is no longer meant for this dialog.
    /// </summary>
    public int? ScreenId { get; set; }
}
