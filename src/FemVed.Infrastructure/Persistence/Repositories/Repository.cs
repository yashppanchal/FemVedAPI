using System.Linq.Expressions;
using FemVed.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FemVed.Infrastructure.Persistence.Repositories;

/// <summary>
/// Generic EF Core repository implementation.
/// Provides standard CRUD and query operations for any domain entity.
/// </summary>
/// <typeparam name="T">Domain entity type.</typeparam>
public class Repository<T> : IRepository<T> where T : class
{
    /// <summary>The underlying EF Core DbContext.</summary>
    protected readonly DbContext Context;
    private readonly DbSet<T> _dbSet;

    /// <summary>Initializes the repository with the given DbContext.</summary>
    public Repository(DbContext context)
    {
        Context = context;
        _dbSet = context.Set<T>();
    }

    /// <inheritdoc />
    public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dbSet.FindAsync(new object[] { id }, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<T>> GetAllAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        IQueryable<T> query = _dbSet;
        if (predicate != null)
            query = query.Where(predicate);
        return await query.ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => await _dbSet.FirstOrDefaultAsync(predicate, cancellationToken);

    /// <inheritdoc />
    public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => await _dbSet.AnyAsync(predicate, cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(T entity)
        => await _dbSet.AddAsync(entity);

    /// <inheritdoc />
    public void Update(T entity)
        => _dbSet.Update(entity);

    /// <inheritdoc />
    public void Remove(T entity)
        => _dbSet.Remove(entity);

    /// <inheritdoc />
    public IQueryable<T> Query()
        => _dbSet.AsQueryable();
}
