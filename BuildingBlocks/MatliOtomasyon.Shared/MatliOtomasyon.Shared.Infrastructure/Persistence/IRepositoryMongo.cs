using Microsoft.EntityFrameworkCore.ChangeTracking;
using MongoDB.Driver;
using System.Linq.Expressions;

namespace MatliOtomasyon.Shared.Infrastructure.Persistence
{
    public interface IRepositoryMongo<TEntity> : IDisposable where TEntity : class
    {
        Task AddAsync(TEntity obj);

        Task AddRangeAsync(List<TEntity> obj);

        Task<TEntity> GetByIdAsync(string id);
        Task<List<TEntity>> GetAllListAsync();


        Task<List<TEntity>> GetAllByPredicateAsync(Expression<Func<TEntity, bool>> predicate);
        Task<TEntity> GetAsyncByPredicate(Expression<Func<TEntity, bool>> predicate);

        Task RemoveAsync(string id);

        Task RemoveRangeAsync(List<string> ids);

        Task UpdateAsync(string id, TEntity obj);

        Task BulkUpdateAsync(IEnumerable<UpdateDefinition<TEntity>> updates);

        Task<TEntity> GetAsyncByPredicateWithNoTracking(Expression<Func<TEntity, bool>> predicate);
    }
}
