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
    }

    public async Task CreateBallotsBatch(List<BallotData> ballots)
    {
        if (ballots.Count > 0)
        {
            await _ballots.InsertManyAsync(ballots, new InsertManyOptions { IsOrdered = false });
        }
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
}