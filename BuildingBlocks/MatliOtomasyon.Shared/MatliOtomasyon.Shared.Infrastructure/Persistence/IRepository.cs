using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Linq.Expressions;

namespace MatliOtomasyon.Shared.Infrastructure.Persistence
{
    public interface IRepository<TEntity> : IDisposable where TEntity : class
    {
        Task AddAsync(TEntity obj);

        Task<EntityEntry<TEntity>> AddEntityEntryAsync(TEntity obj);

        Task AddRangeAsync(List<TEntity> obj);

        Task UpdateAsync(TEntity obj);

        void Update(TEntity obj);

        Task RemoveAsync(int id);

        Task<TEntity> GetByIdAsync(int id);

        Task<IQueryable<TEntity>> GetAllAsync();

        Task<List<TEntity>> GetAllListAsync();

        IQueryable<TEntity> GetAllAsyncByPredicate(Expression<Func<TEntity, bool>> predicate);

        Task<TEntity> GetAsyncByPredicate(Expression<Func<TEntity, bool>> predicate);

        Task<TEntity> GetAsyncByPredicateWithAsNoTracking(Expression<Func<TEntity, bool>> predicate);

        Task<List<TEntity>> GetAllAsyncByPredicateWithAsNoTracking(Expression<Func<TEntity, bool>> predicate);

        void BulkInsert(IEnumerable<TEntity> list);

        void Remove(TEntity entity);

        void RemoveRange(IEnumerable<TEntity> list);

        int SaveChanges();

        Task<int> SaveChangesAsync();

        void Attach(TEntity obj);

        void AttachRange(List<TEntity> obj);
        void Detach(TEntity obj);
    }
}
