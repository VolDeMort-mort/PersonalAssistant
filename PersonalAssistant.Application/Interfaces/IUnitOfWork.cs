namespace PersonalAssistant.Application.Interfaces;

/// <summary>
/// Saves every change collected by repositories in one database transaction: all of them or none.
/// Repositories that work with it only collect changes and never save by themselves.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
