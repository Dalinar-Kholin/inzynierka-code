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
    }

    public async Task SaveCodeSettingsBatch(List<CodeSettingData> codeSettings)
    {
        if (codeSettings.Count > 0)
        {
            await _codeSettingsCollection.InsertManyAsync(codeSettings, new InsertManyOptions { IsOrdered = false });
        }
    }
}