namespace FemVed.Application.Interfaces;

/// <summary>
/// Unit of Work — commits all pending repository changes in a single database transaction.
/// Command handlers call <see cref="SaveChangesAsync"/> once at the end of the operation.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>Persists all pending changes to the database.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
