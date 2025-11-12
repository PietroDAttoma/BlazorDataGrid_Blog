using BlazorDataGrid.Data;
using BlazorDataGrid.Models;
using BlazorDataGrid.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BlazorDataGrid.UoW
{
    // Implementazione del pattern Unit of Work con DbContext isolato
    public class UnitOfWork : IUnitOfWork, IDisposable, IAsyncDisposable
    {
        private readonly MyContext _context;

        private IGenericRepository<Blog>? _blogs;

        public UnitOfWork(IDbContextFactory<MyContext> contextFactory)
        {
            // ✅ Crea un nuovo DbContext isolato per questo UnitOfWork
            _context = contextFactory.CreateDbContext();
        }

        public IGenericRepository<Blog> Blogs
            => _blogs ??= new GenericRepository<Blog>(_context);

        public async Task<int> SaveAsync() => await _context.SaveChangesAsync();

        // ✅ Salva tutte le modifiche tracciate dal DbContext in modo asincrono
        public async ValueTask DisposeAsync() => await _context.DisposeAsync();

        public void Dispose() => _context.Dispose();
    }
}