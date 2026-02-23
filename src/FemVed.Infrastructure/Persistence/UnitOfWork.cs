using FemVed.Application.Interfaces;

namespace FemVed.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of the Unit of Work pattern.
/// Wraps <see cref="AppDbContext.SaveChangesAsync"/> to commit all pending changes in a single transaction.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private bool _disposed;

    /// <summary>Initializes the UnitOfWork with the injected <see cref="AppDbContext"/>.</summary>
    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _context.SaveChangesAsync(cancellationToken);

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            _context.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
