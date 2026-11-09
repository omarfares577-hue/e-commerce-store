using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Store.Domain.Common;
using Store.Application.Specification;
namespace Store.Application.Contracts
{
    public interface IGenericRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(int id);
        Task<IReadOnlyList<T>> GetAllAsync();
        Task<IReadOnlyList<T>> GetAllWithSpecAsync(ISpecification<T> spec);
        Task<T?> GetByIdWithSpecAsync(ISpecification<T> spec);
        Task<int> GetAllCountAsync(ISpecification<T> spec);
        Task AddAsync(T entity);
        void Update(T entity); 
        void Delete(T entity);
    }
}
