using MongoDB.Driver;
using MongoDB.Bson;

public class BallotService
{
    private readonly IMongoCollection<BallotData> _ballots;


    public BallotService(int serverId, int totalServers)
    {
        var client = new MongoClient("mongodb://localhost:27017");
        var database = client.GetDatabase($"server_{serverId}");
        _ballots = database.GetCollection<BallotData>("BallotData");

        _ballots.Indexes.CreateOne(new CreateIndexModel<BallotData>(
            Builders<BallotData>.IndexKeys.Ascending(b => b.BallotId),
            new CreateIndexOptions { Unique = true }));

        _ballots.Indexes.CreateOne(new CreateIndexModel<BallotData>(
            Builders<BallotData>.IndexKeys.Ascending(b => b.Shadow),
            new CreateIndexOptions { Unique = true }));

        // ShadowPrim is null on the last server
        if (serverId != totalServers)
        {
            _ballots.Indexes.CreateOne(new CreateIndexModel<BallotData>(
                Builders<BallotData>.IndexKeys.Ascending(b => b.ShadowPrim),
                new CreateIndexOptions { Unique = true }));
        }
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

    // pobieranie wszystkich potrzebnych danych do CodeSetting (Protocol 5) w jednym batchu
    public async Task<List<DataForCodeSetting>> GetProtocol5DataBatch(int skip, int take)
    {
        var projection = Builders<BallotData>.Projection
            .Include(b => b.BallotId)
            .Include(b => b.C0)
            .Include(b => b.C1)
            .Include(b => b.B);

        return await _ballots
            .Find(FilterDefinition<BallotData>.Empty)
            .Project<DataForCodeSetting>(projection)
            .Skip(skip)
            .Limit(take)
            .ToListAsync();
    }

    public async Task<Dictionary<int, int?>> GetShadowBatch(List<int> ballotIds, bool isPrim = false)
    {
        var filter = Builders<BallotData>.Filter.In(b => b.BallotId, ballotIds);
        var projection = isPrim
            ? Builders<BallotData>.Projection
                .Include(b => b.BallotId)
                .Include(b => b.ShadowPrim)
            : Builders<BallotData>.Projection
                .Include(b => b.BallotId)
                .Include(b => b.Shadow);

        var results = await _ballots
            .Find(filter)
            .Project<BallotData>(projection)
            .ToListAsync();

        return results.ToDictionary(
            b => b.BallotId,
            b => isPrim ? b.ShadowPrim : b.Shadow
        );
    }

}