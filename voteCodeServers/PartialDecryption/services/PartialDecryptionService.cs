using MongoDB.Driver;
using MongoDB.Bson;

public class PartialDecryptionService
{
    private readonly IMongoCollection<PartialDecryptionData> _partialDecryptionCollection;
    private readonly int _serverId;

    public PartialDecryptionService(int serverId)
    {
        _serverId = serverId;
        var client = new MongoClient("mongodb://localhost:27017");
        var database = client.GetDatabase($"server_{serverId}");
        _partialDecryptionCollection = database.GetCollection<PartialDecryptionData>("PartialDecryptions");
    }

    public async Task SavePartialDecryptionsBatch(List<PartialDecryptionData> batch)
    {
        if (batch.Count > 0)
        {
            await _partialDecryptionCollection.InsertManyAsync(batch, new InsertManyOptions { IsOrdered = false });
        }
    }

    public async Task<long> GetTotalCount()
    {
        return await _partialDecryptionCollection.CountDocumentsAsync(Builders<PartialDecryptionData>.Filter.Empty);
    }
}
