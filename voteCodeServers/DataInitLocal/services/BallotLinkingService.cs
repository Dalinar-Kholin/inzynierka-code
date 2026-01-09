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

        var indexKeys1 = Builders<BallotLinking>.IndexKeys.Ascending(b => b.PrevBallotId);
        _ballotLinking.Indexes.CreateOne(new CreateIndexModel<BallotLinking>(indexKeys1, new CreateIndexOptions { Unique = true }));
        var indexKeys2 = Builders<BallotLinking>.IndexKeys.Ascending(b => b.BallotId);
        _ballotLinking.Indexes.CreateOne(new CreateIndexModel<BallotLinking>(indexKeys2, new CreateIndexOptions { Unique = true }));

        _ballotLinkingPrim.Indexes.CreateOne(new CreateIndexModel<BallotLinking>(indexKeys1, new CreateIndexOptions { Unique = true }));
        _ballotLinkingPrim.Indexes.CreateOne(new CreateIndexModel<BallotLinking>(indexKeys2, new CreateIndexOptions { Unique = true }));
    }

    public async Task SaveLinkingBatch(List<BallotLinking> records, bool isPrim)
    {
        var collection = isPrim ? _ballotLinkingPrim : _ballotLinking;
        await collection.InsertManyAsync(records);
    }

    public async Task<List<int>> GetPermutationListAsync(bool isPrim)
    {
        var collection = isPrim ? _ballotLinkingPrim : _ballotLinking;
        var allRecords = await collection.Find(FilterDefinition<BallotLinking>.Empty)
            .Sort(Builders<BallotLinking>.Sort.Ascending(b => b.PrevBallotId))
            .ToListAsync();

        var permutationList = new List<int>();
        foreach (var record in allRecords)
        {
            permutationList.Add(record.BallotId);
        }

        return permutationList;
    }

    public async Task<List<int>> GetReversedPermutationListAsync(bool isPrim)
    {
        var collection = isPrim ? _ballotLinkingPrim : _ballotLinking;
        var allRecords = await collection.Find(FilterDefinition<BallotLinking>.Empty)
            .Sort(Builders<BallotLinking>.Sort.Ascending(b => b.BallotId))
            .ToListAsync();

        var reversedPermutationList = new List<int>();
        foreach (var record in allRecords)
        {
            reversedPermutationList.Add(record.PrevBallotId);
        }

        return reversedPermutationList;
    }
}
