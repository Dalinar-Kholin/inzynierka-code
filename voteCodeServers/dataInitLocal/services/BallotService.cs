using MongoDB.Driver;
using MongoDB.Bson;

public class BallotService
{
    private readonly IMongoCollection<BallotData> _ballots;


    public BallotService(int serverId)
    {
        var client = new MongoClient("mongodb://localhost:27017");
        var database = client.GetDatabase($"server_{serverId}");
        _ballots = database.GetCollection<BallotData>("BallotData");

        var indexKeys = Builders<BallotData>.IndexKeys.Ascending(b => b.BallotId);
        _ballots.Indexes.CreateOne(new CreateIndexModel<BallotData>(indexKeys, new CreateIndexOptions { Unique = true }));
    }

    public async Task CreateBallotsBatch(List<BallotData> ballots)
    {
        if (ballots.Count > 0)
        {
            await _ballots.InsertManyAsync(ballots, new InsertManyOptions { IsOrdered = false });
        }
    }

    public async Task<bool> BallotExists(int ballotId)
    {
        return await _ballots
            .Find(b => b.BallotId == ballotId)
            .AnyAsync();
    }
}