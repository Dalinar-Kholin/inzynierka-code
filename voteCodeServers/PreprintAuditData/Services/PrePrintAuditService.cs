using MongoDB.Driver;
using MongoDB.Bson;

public class PrePrintAuditService
{
    private readonly IMongoCollection<PrePrintAuditData> _ballots;


    public PrePrintAuditService(int serverId)
    {
        var client = new MongoClient("mongodb://localhost:27017");
        var database = client.GetDatabase($"GlobalDatabase");
        _ballots = database.GetCollection<PrePrintAuditData>("PrePrintAudit");

        var indexKeys = Builders<PrePrintAuditData>.IndexKeys.Ascending(b => b.BallotVoteSerial);
        _ballots.Indexes.CreateOne(new CreateIndexModel<PrePrintAuditData>(indexKeys, new CreateIndexOptions { Unique = true }));
    }

    public async Task CreatePrePrintAuditBatch(List<PrePrintAuditData> ballots)
    {
        if (ballots.Count > 0)
        {
            await _ballots.InsertManyAsync(ballots, new InsertManyOptions { IsOrdered = false });
        }
    }
}