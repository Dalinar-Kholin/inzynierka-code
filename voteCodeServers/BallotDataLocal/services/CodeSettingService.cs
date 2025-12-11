using MongoDB.Driver;
using MongoDB.Bson;

public class CodeSettingService
{
    private readonly IMongoCollection<CodeSettingData> _codeSettingsCollection;

    public CodeSettingService(int serverId)
    {
        var client = new MongoClient("mongodb://localhost:27017");
        var database = client.GetDatabase($"server_{serverId}");
        _codeSettingsCollection = database.GetCollection<CodeSettingData>("CodeSettings");

        var indexKeys = Builders<CodeSettingData>.IndexKeys.Ascending(b => b.BallotId);
        _codeSettingsCollection.Indexes.CreateOne(new CreateIndexModel<CodeSettingData>(indexKeys, new CreateIndexOptions { Unique = true }));
    }


    public async Task SaveCodeSettingsBatch(List<CodeSettingData> codeSettings)
    {
        if (codeSettings.Count > 0)
        {
            await _codeSettingsCollection.InsertManyAsync(codeSettings, new InsertManyOptions { IsOrdered = false });
        }
    }

    public async Task<Dictionary<int, string[]>> GetVBatch(List<int> ballotIds)
    {
        var filter = Builders<CodeSettingData>.Filter.In(b => b.BallotId, ballotIds);
        var projection = Builders<CodeSettingData>.Projection
            .Include(b => b.BallotId)
            .Include(b => b.V);

        var results = await _codeSettingsCollection
            .Find(filter)
            .Project<CodeSettingData>(projection)
            .ToListAsync();

        return results.ToDictionary(
            b => b.BallotId,
            b => b.V
        );
    }
}