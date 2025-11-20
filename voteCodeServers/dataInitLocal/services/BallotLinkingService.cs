using MongoDB.Driver;
using MongoDB.Bson;


public class BallotLinkingService
{
    private readonly IMongoCollection<BallotLinking> _ballotLinking;
    private readonly IMongoCollection<BallotLinking> _ballotLinkingPrim;

    public BallotLinkingService(int serverId)
    {
        var client = new MongoClient("mongodb://localhost:27017");
        var database = client.GetDatabase($"server_{serverId}");
        _ballotLinking = database.GetCollection<BallotLinking>($"BallotLinking");
        _ballotLinkingPrim = database.GetCollection<BallotLinking>($"BallotLinkingPrim");

        var indexKeys1 = Builders<BallotLinking>.IndexKeys.Ascending(b => b.BallotId);
        _ballotLinking.Indexes.CreateOne(new CreateIndexModel<BallotLinking>(indexKeys1, new CreateIndexOptions { Unique = true }));
        var indexKeys2 = Builders<BallotLinking>.IndexKeys.Ascending(b => b.PrevBallot);
        _ballotLinking.Indexes.CreateOne(new CreateIndexModel<BallotLinking>(indexKeys2, new CreateIndexOptions { Unique = true }));

        _ballotLinkingPrim.Indexes.CreateOne(new CreateIndexModel<BallotLinking>(indexKeys1, new CreateIndexOptions { Unique = true }));
        _ballotLinkingPrim.Indexes.CreateOne(new CreateIndexModel<BallotLinking>(indexKeys2, new CreateIndexOptions { Unique = true }));
    }


    public async Task SaveLinking(int id, int prevBallot, string commitment, long randomKey, bool isPrim)
    {
        var collection = isPrim ? _ballotLinkingPrim : _ballotLinking;

        var newData = new BallotLinking
        {
            Id = ObjectId.GenerateNewId(),
            BallotId = id,
            PrevBallot = prevBallot,
            CommPrevBallot = commitment,
            R0 = randomKey
        };
        await collection.InsertOneAsync(newData);

    }
}
