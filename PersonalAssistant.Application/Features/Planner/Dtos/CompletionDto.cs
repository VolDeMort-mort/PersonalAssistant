namespace PersonalAssistant.Application.Features.Planner.Dtos;

/// <param name="CompletedDate">The date that was completed; undo needs it back. Null for a task without a date.</param>
/// <param name="NextDueDate">Where a repeated task moved on to; null when the task is done.</param>
public record CompletionDto(Guid TaskId, string Title, DateOnly? CompletedDate, DateOnly? NextDueDate)
{
    public bool IsRepeated => NextDueDate is not null;
}
