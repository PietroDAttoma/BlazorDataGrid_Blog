using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Linq.Expressions;

namespace BlazorDataGrid.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        private readonly DbContext _context; // Contesto EF Core
        private readonly DbSet<T> _dbSet;    // DbSet associato al tipo T

        public GenericRepository(DbContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>(); // Ottiene il DbSet per l'entità T
        }

        // 🔄 Restituisce tutte le entità ordinate in modalità "no tracking", con ordinamento dinamico
        public async Task<IEnumerable<T>> GetAllAsNoTrackingOrderedAsync<TKey>(
            Expression<Func<T, TKey>> orderBy,
            bool descending = false,
            params Expression<Func<T, object>>[] includeExpressions)
        {
            IQueryable<T> query = _dbSet.AsNoTracking();

            // 🔗 Applica gli include
            foreach (var include in includeExpressions)
            {
                query = query.Include(include);
            }

            // 🔽 Applica ordinamento
            query = descending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);

            return await query.ToListAsync();
        }

        public async Task<T?> GetByIdAsNoTrackingAsync(object id)
        {
            var keyName = _context.Model.FindEntityType(typeof(T))!
                                        .FindPrimaryKey()!
                                        .Properties
                                        .Select(x => x.Name)
                                        .Single(); // gestisce solo chiavi semplici

            return await _dbSet.AsNoTracking()
                               .FirstOrDefaultAsync(e => EF.Property<object>(e, keyName) == id);
        }

        // 🔄 Restituisce un'entità per chiave con include specificati, senza tracciamento EF.
        public async Task<T?> GetByIdWithIncludesAsNoTrackingAsync(
            object id,
            params Expression<Func<T, object>>[] includes)
        {
            var keyName = _context.Model.FindEntityType(typeof(T))!
                                        .FindPrimaryKey()!
                                        .Properties
                                        .Select(x => x.Name)
                                        .Single();

            IQueryable<T> query = _dbSet.AsNoTracking(); // 👈 No tracking

            foreach (var include in includes)
            {
                query = query.Include(include);
            }

            return await query.FirstOrDefaultAsync(e => EF.Property<object>(e, keyName) == id);
        }
        public async Task<T?> GetByFilterAsNoTrackingAsync(
            Expression<Func<T, bool>> filter,
            params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _dbSet.AsNoTracking();

            // ✅ Applica filtro obbligatorio passato dal chiamante
            query = query.Where(filter);

            // ➕ Include proprietà di navigazione
            foreach (var include in includes)
            {
                query = query.Include(include);
            }

            // 🔄 Restituisce il primo risultato corrispondente
            return await query.FirstOrDefaultAsync();
        }

        // 🔎 Verifica se esiste almeno un'entità che soddisfa il predicato
        public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.AnyAsync(predicate);
        }

        // 🔄 Imposta manualmente il valore originale della RowVersion per la concorrenza ottimistica
        public void SetOriginalRowVersion(T entity, byte[] rowVersion)
        {
            // Variante hardcoded: _context.Entry(entity).Property("RowVersion").OriginalValue = rowVersion;

            var entityType = _context.Model.FindEntityType(entity.GetType());
            var timestampProperty = entityType?
                .GetProperties()
                .FirstOrDefault(p => p.IsConcurrencyToken &&   // proprietà marcata come token di concorrenza
                                     p.ClrType == typeof(byte[]) &&
                                     p.ValueGenerated == ValueGenerated.OnAddOrUpdate);

            if (timestampProperty != null)
            {
                _context.Entry(entity).Property(timestampProperty.Name).OriginalValue = rowVersion;
            }
        }

         // 🔄 Copia i valori correnti da un'entità sorgente a una target (utile per update parziali)
        public void ApplyValues(T target, T source)
        {
            var entry = _context.Entry(target);
            entry.CurrentValues.SetValues(source);
        }

        // 🔎 Restituisce l'EntityEntry associato a un'entità (per accedere a stato, proprietà, ecc.)
        public EntityEntry<T> GetEntry(T entity)
        {
            return _context.Entry(entity);
        }

        // ➕ Aggiunge una nuova entità in modo asincrono
        public async Task AddAsync(T entity) => await _dbSet.AddAsync(entity);

        // ✏️ Aggiorna un'entità esistente
        public void Update(T entity) => _dbSet.Update(entity);

        // ❌ Elimina un'entità dal DbSet
        public void Delete(T entity) => _dbSet.Remove(entity);

        // 🔄 Scollega un'entità dal ChangeTracker (non sarà più monitorata)
        public void Detach(T entity)
        {
            _context.Entry(entity).State = EntityState.Detached;
        }
        // 🔄 Scollega tutte le entità che soddisfano un predicato → utile per pulizie mirate
        public void DetachWhere(Func<T, bool> predicate)
        {
            var entries = _context.ChangeTracker.Entries<T>()
                                  .Where(e => predicate(e.Entity))
                                  .ToList();

            foreach (var entry in entries)
            {
                entry.State = EntityState.Detached;
            }
        }

        // 🧼 Pulisce la RowVersion corrente e originale di un'entità dopo il detach.
        // ⚠️ Utile in scenari di rollback o entità eliminate.
        public void ClearRowVersion(T entity)
        {
            var entry = _context.Entry(entity);

            var timestampProperty = entry.Metadata.GetProperties()
                .FirstOrDefault(p => p.IsConcurrencyToken &&
                                     p.ClrType == typeof(byte[]) &&
                                     p.ValueGenerated == ValueGenerated.OnAddOrUpdate);

            if (timestampProperty != null)
            {
                entry.Property(timestampProperty.Name).CurrentValue = null;
                entry.Property(timestampProperty.Name).OriginalValue = null;
            }
        }

        // 🗑️ Soft delete: marca l'entità come eliminata impostando la proprietà "IsDeleted"
        // ⚠️ Funziona solo se l'entità ha una proprietà "IsDeleted"
        public void SoftDelete(T entity)
        {
            var entry = _context.Entry(entity);

            // 🟢 Imposta IsDeleted = true
            var isDeletedProp = entry.Metadata.FindProperty("IsDeleted");
            if (isDeletedProp != null)
            {
                entry.Property("IsDeleted").CurrentValue = true;
                entry.State = EntityState.Modified;
            }

            // 🕒 Imposta DeletedAt = DateTime.UtcNow (se la proprietà esiste)
            var deletedAtProp = entry.Metadata.FindProperty("DeletedAt");
            if (deletedAtProp != null)
            {
                entry.Property("DeletedAt").CurrentValue = DateTime.UtcNow;
            }
        }
    }
}