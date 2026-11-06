using EnergyMonitoring.Infrastructure.Mongo;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using System.Linq.Expressions;
using MatliOtomasyon.Shared.Infrastructure.Persistence;

namespace EnergyMonitoring.Infrastructure.Repositories.Core
{
    public class RepositoryForMongo<TEntity> : IRepositoryMongo<TEntity>
    where TEntity : class
    {
        protected readonly IMongoCollection<TEntity> _collection;

        public RepositoryForMongo(IMongoClient mongoClient, MongoDbSettings settings)
        {
            var database = mongoClient.GetDatabase(settings.DatabaseName);
            _collection = database.GetCollection<TEntity>(typeof(TEntity).Name); // Koleksiyon adını TEntity'den alıyoruz
        }

        // Add Async
        public async Task AddAsync(TEntity obj)
        {
            await _collection.InsertOneAsync(obj);
        }

        // Add Range Async
        public async Task AddRangeAsync(List<TEntity> obj)
        {
            await _collection.InsertManyAsync(obj);
        }

        // Get By Id (MongoDB'de genellikle ObjectId kullanılır)
        public async Task<TEntity> GetByIdAsync(string id)
        {
            var filter = Builders<TEntity>.Filter.Eq("_id", id);
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }

        // Get All Async
        public async Task<List<TEntity>> GetAllListAsync()
        {
            return await _collection.Find(Builders<TEntity>.Filter.Empty).ToListAsync();
        }

        // Get All Asynchronously by Predicate (expression)
        public async Task<List<TEntity>> GetAllByPredicateAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return await _collection.Find(predicate).ToListAsync();
        }

        // Get By Predicate Async
        public async Task<TEntity> GetAsyncByPredicate(Expression<Func<TEntity, bool>> predicate)
        {
            return await _collection.Find(predicate).FirstOrDefaultAsync();
        }

        // Remove Async
        public async Task RemoveAsync(string id)
        {
            var filter = Builders<TEntity>.Filter.Eq("_id", id);
            await _collection.DeleteOneAsync(filter);
        }

        // Remove Range Async
        public async Task RemoveRangeAsync(List<string> ids)
        {
            var filter = Builders<TEntity>.Filter.In("_id", ids);
            await _collection.DeleteManyAsync(filter);
        }

        // Update Async
        public async Task UpdateAsync(string id, TEntity obj)
        {
            var filter = Builders<TEntity>.Filter.Eq("_id", id);
            await _collection.ReplaceOneAsync(filter, obj);
        }

        // Bulk Insert
        public async Task BulkInsertAsync(IEnumerable<TEntity> list)
        {
            await _collection.InsertManyAsync(list);
        }

        // Bulk Update (MongoDB'de bulk işlemleri daha karmaşıktır)
        public async Task BulkUpdateAsync(IEnumerable<UpdateDefinition<TEntity>> updates)
        {
            var bulkOps = updates.Select(update => new UpdateOneModel<TEntity>(Builders<TEntity>.Filter.Eq("_id", update), update));
            await _collection.BulkWriteAsync(bulkOps);
        }

        // Find One with No Tracking
        public async Task<TEntity> GetAsyncByPredicateWithNoTracking(Expression<Func<TEntity, bool>> predicate)
        {
            return await _collection.Find(predicate).FirstOrDefaultAsync();
        }

        // Dispose method
        public void Dispose()
        {
            // MongoDB client'ı genellikle singleton olarak yönetildiğinden, burada dispose etmeye gerek yok.
            GC.SuppressFinalize(this);
        }
    }
}
