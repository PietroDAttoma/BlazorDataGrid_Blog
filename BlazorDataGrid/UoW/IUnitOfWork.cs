using BlazorDataGrid.Models;
using BlazorDataGrid.Repositories;

namespace BlazorDataGrid.UoW
{
    public interface IUnitOfWork : IDisposable
    {
        IGenericRepository<Blog> Blogs { get; }
        Task<int> SaveAsync();
    }

}
