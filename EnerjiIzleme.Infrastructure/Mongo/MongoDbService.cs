using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace EnerjiIzleme.Infrastructure.Mongo
{
    public class MongoDbService
    {
        private readonly IMongoDatabase _database;

        public MongoDbService(IConfiguration configuration)
        {
            var mongoSettings = configuration.GetSection("MongoDbSettings").Get<MongoDbSettings>();
            MongoClient client = new(mongoSettings.ConnectionString);
            _database = client.GetDatabase(mongoSettings.DatabaseName);
        }

        public IMongoCollection<T> GetCollection<T>() =>
            _database.GetCollection<T>(typeof(T).Name.ToLowerInvariant());
    }
}
