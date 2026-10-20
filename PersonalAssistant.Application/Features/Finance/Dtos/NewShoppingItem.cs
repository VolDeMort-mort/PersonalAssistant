namespace PersonalAssistant.Application.Features.Finance.Dtos;

/// <summary>An item to put on the shopping list; everything but the title is optional.</summary>
/// <param name="Price">Whole hryvnias; null when not known.</param>
/// <param name="Link">An absolute http(s) link; null without one.</param>
/// <param name="CategoryId">An expense category; null for none yet.</param>
public record NewShoppingItem(string Title, long? Price = null, string? Link = null, Guid? CategoryId = null);
