using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Linq.Expressions;

namespace BlazorDataGrid.Repositories
{
    public interface IGenericRepository<T> where T : class
    {
        // 🔍 Query base (collezioni)
        Task<IEnumerable<T>> GetAllAsNoTrackingOrderedAsync<TKey>(
            Expression<Func<T, TKey>> orderBy,
            bool descending = false,
            params Expression<Func<T, object>>[] includeExpressions);

        // 🔍 Query singola entità
        Task<T?> GetByIdAsNoTrackingAsync(object id);
        Task<T?> GetByIdWithIncludesAsNoTrackingAsync(object id, params Expression<Func<T, object>>[] includes);
        Task<T?> GetByFilterAsNoTrackingAsync(Expression<Func<T, bool>> filter, params Expression<Func<T, object>>[] includes);

        // 🔍 Esistenza
        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);

        // ✏️ Mutazioni (CRUD)
        Task AddAsync(T entity);
        void Update(T entity);
        void Delete(T entity);
        void SoftDelete(T entity);

        // 🔄 Tracciamento e stato EF
        EntityEntry<T> GetEntry(T entity);
        void Detach(T entity);
        void DetachWhere(Func<T, bool> predicate);
        void ApplyValues(T target, T source);
        void ClearRowVersion(T entity);
        void SetOriginalRowVersion(T entity, byte[] rowVersion);
    }
}
