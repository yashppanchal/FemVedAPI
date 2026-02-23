using System.Linq.Expressions;

namespace FemVed.Application.Interfaces;

/// <summary>
/// Generic repository providing standard data access operations for an entity.
/// All implementations live in Infrastructure. Application code depends only on this interface.
/// </summary>
/// <typeparam name="T">Domain entity type.</typeparam>
public interface IRepository<T> where T : class
{
    /// <summary>Returns the entity with the specified primary key, or null if not found.</summary>
    /// <param name="id">Entity primary key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns all entities matching the optional predicate.</summary>
    /// <param name="predicate">Optional filter expression.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<T>> GetAllAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);

    /// <summary>Returns the first entity matching the predicate, or null.</summary>
    /// <param name="predicate">Filter expression.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>Returns true if any entity matches the predicate.</summary>
    /// <param name="predicate">Filter expression.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>Adds a new entity to the context (not yet persisted — call SaveChangesAsync via UoW).</summary>
    /// <param name="entity">Entity to add.</param>
    Task AddAsync(T entity);

    /// <summary>Marks an entity as modified in the context.</summary>
    /// <param name="entity">Entity to update.</param>
    void Update(T entity);

    /// <summary>Marks an entity for removal from the context.</summary>
    /// <param name="entity">Entity to remove.</param>
    void Remove(T entity);

    /// <summary>Returns an IQueryable for building complex queries with EF includes/projections.</summary>
    IQueryable<T> Query();
}
