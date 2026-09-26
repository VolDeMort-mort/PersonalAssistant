namespace PersonalAssistant.Application.Common.Exceptions;

/// <summary>
/// A request refers to something that does not exist (any more), e.g. a button of a deleted template.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string entityName, object key)
        : base($"{entityName} '{key}' was not found.")
    {
    }
}
