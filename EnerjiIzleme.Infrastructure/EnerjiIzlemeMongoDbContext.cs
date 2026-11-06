using EnerjiIzleme.Domain.Models;
using EnerjiIzleme.Infrastructure.Mongo;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace EnerjiIzleme.Infrastructure
{
    public class EnerjiIzlemeMongoDbContext
    {
        private readonly IMongoDatabase _database;

        public EnerjiIzlemeMongoDbContext(IOptions<MongoDbSettings> settings)
        {
            var client = new MongoClient(settings.Value.ConnectionString);
            _database = client.GetDatabase(settings.Value.DatabaseName);
        }

        public IMongoCollection<Logs> Logs => _database.GetCollection<Logs>("Logs");
        public IMongoCollection<LogsActual> LogsActual => _database.GetCollection<LogsActual>("LogsActual");
        // diğer Mongo koleksiyonları
    }
}