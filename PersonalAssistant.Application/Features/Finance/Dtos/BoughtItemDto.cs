namespace PersonalAssistant.Application.Features.Finance.Dtos;

/// <summary>A bought item: what was paid and when comes from the expense it was recorded with.</summary>
/// <param name="CategoryName">The category it was bought in.</param>
/// <param name="BoughtAtLocal">In the user's time zone.</param>
/// <param name="PaidAmount">Null when the expense can't be found.</param>
/// <param name="TransactionId">The expense; null when it can't be found.</param>
public record BoughtItemDto(
    Guid Id,
    string Title,
    string? Link,
    string? CategoryName,
    DateTime BoughtAtLocal,
    long? PaidAmount,
    Guid? TransactionId);

/// <param name="Items">The latest purchase first.</param>
/// <param name="Page">1-based; already within 1..<paramref name="PageCount"/>.</param>
public record BoughtItemsPageDto(IReadOnlyList<BoughtItemDto> Items, int Page, int PageCount, int TotalCount);
