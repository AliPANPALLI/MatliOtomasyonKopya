using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using SharedKernel.Core;
using System.Linq.Expressions;

namespace SuHatScada.Infrastructure.Repositories.Core
{
    public class Repository<TEntity> : IRepository<TEntity>
     where TEntity : class
    {
        protected readonly SuHatScadaDbContext Db;
        protected readonly DbSet<TEntity> DbSet;

        public Repository(SuHatScadaDbContext context)
        {
            Db = context;
            DbSet = Db.Set<TEntity>();
        }

        public async Task AddAsync(TEntity obj)
        {
            await DbSet.AddAsync(obj);
        }

        public async Task AddRangeAsync(List<TEntity> obj)
        {
            await DbSet.AddRangeAsync(obj);
        }

        public void BulkInsert(IEnumerable<TEntity> list)
        {
            DbSet.AddRange(list);
        }

        public async Task<TEntity> GetByIdAsync(int id)
        {
            return await DbSet.FindAsync(id);
        }

        public async Task<IQueryable<TEntity>> GetAllAsync()
        {
            return await Task.Factory.StartNew(() => DbSet);
        }

        public async Task<List<TEntity>> GetAllListAsync()
        {
            return await DbSet.ToListAsync();
        }
        public void Remove(TEntity obj)
        {
            DbSet.Remove(obj);
        }
        public async Task RemoveAsync(int id)
        {
            DbSet.Remove(await DbSet.FindAsync(id));
        }

        public void RemoveRange(IEnumerable<TEntity> list)
        {
            DbSet.RemoveRange(list);
        }

        public void Update(TEntity obj)
        {
            DbSet.Update(obj);
        }

        public async Task UpdateAsync(TEntity obj)
        {
            await Task.Factory.StartNew(() => { DbSet.Update(obj); });
        }

        // TODO this method could be renamed to Where
        public IQueryable<TEntity> GetAllAsyncByPredicate(Expression<Func<TEntity, bool>> predicate)
        {
            return DbSet.Where(predicate);
        }

        public async Task<TEntity> GetAsyncByPredicate(Expression<Func<TEntity, bool>> predicate)
        {
            return await DbSet.FirstOrDefaultAsync(predicate);
        }

        public async Task<TEntity> GetAsyncByPredicateWithAsNoTracking(Expression<Func<TEntity, bool>> predicate)
        {
            return await DbSet.AsNoTracking().FirstOrDefaultAsync(predicate);
        }

        public async Task<List<TEntity>> GetAllAsyncByPredicateWithAsNoTracking(Expression<Func<TEntity, bool>> predicate)
        {
            return await DbSet.AsNoTracking().Where(predicate).ToListAsync();
        }

        public void Dispose()
        {
            Db.Dispose();
            GC.SuppressFinalize(this);
        }

        public int SaveChanges()
        {
            return Db.SaveChanges();
        }

        public async Task<int> SaveChangesAsync()
        {
            try
            {
                return await Db.SaveChangesAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public async Task<EntityEntry<TEntity>> AddEntityEntryAsync(TEntity obj)
        {
            return await DbSet.AddAsync(obj);
        }

        public void Attach(TEntity obj)
        {
            DbSet.Attach(obj);
        }

        public void AttachRange(List<TEntity> obj)
        {
            DbSet.AttachRange(obj);
        }

        public void Detach(TEntity obj)
        {
            Db.Entry(obj).State = EntityState.Detached;
        }
    }
}
