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

    public async Task<BallotData> GetOrCreateBallot(int ballotId)
    {
        var ballot = await _ballots
            .Find(b => b.BallotId == ballotId)
            .FirstOrDefaultAsync();

        if (ballot == null)
        {
            ballot = new BallotData
            {
                BallotId = ballotId,
            };
            await _ballots.InsertOneAsync(ballot);
            Console.WriteLine($"Created new ballot {ballotId}");
        }

        return ballot;
    }

    public async Task SaveBallot(BallotData ballot)
    {
        await _ballots.ReplaceOneAsync(b => b.BallotId == ballot.BallotId, ballot);
    }
}