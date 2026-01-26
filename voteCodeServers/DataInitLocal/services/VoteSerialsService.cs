using MongoDB.Driver;
using MongoDB.Bson;

public class VoteSerialsService
{
    private readonly IMongoCollection<VoteSerialData> _voteSerials;

    public VoteSerialsService(int serverId)
    {
        var client = new MongoClient("mongodb://localhost:27017");
        var database = client.GetDatabase($"server_{serverId}");
        _voteSerials = database.GetCollection<VoteSerialData>("VoteSerials");

        var indexKeys1 = Builders<VoteSerialData>.IndexKeys.Ascending(b => b.BallotId);
        _voteSerials.Indexes.CreateOne(new CreateIndexModel<VoteSerialData>(indexKeys1, new CreateIndexOptions { Unique = true }));
        var indexKeys2 = Builders<VoteSerialData>.IndexKeys.Ascending(b => b.VoteSerial);
        _voteSerials.Indexes.CreateOne(new CreateIndexModel<VoteSerialData>(indexKeys2, new CreateIndexOptions { Unique = true }));
    }

    public async Task SaveVoteSerialsBatch(List<VoteSerialData> voteSerials)
    {
        if (voteSerials.Count > 0)
        {
            await _voteSerials.InsertManyAsync(voteSerials, new InsertManyOptions { IsOrdered = false });
        }
    }

    public async Task<Dictionary<int, string>> GetVoteSerialsBatch(List<int> ballotIds)
    {
        if (ballotIds == null || ballotIds.Count == 0)
        {
            return new Dictionary<int, string>();
        }

        var filter = Builders<VoteSerialData>.Filter.In(x => x.BallotId, ballotIds);
        var projection = Builders<VoteSerialData>.Projection
            .Include(x => x.BallotId)
            .Include(x => x.VoteSerial);

        var results = await _voteSerials
            .Find(filter)
            .Project<VoteSerialData>(projection)
            .ToListAsync();

        return results.ToDictionary(r => r.BallotId, r => r.VoteSerial);
    }

    public async Task<Dictionary<string, int>> GetBallotIdsBatch(List<string> voteSerials)
    {
        var filter = Builders<VoteSerialData>.Filter.In(x => x.VoteSerial, voteSerials);
        var projection = Builders<VoteSerialData>.Projection
            .Include(x => x.BallotId)
            .Include(x => x.VoteSerial);

        var results = await _voteSerials
            .Find(filter)
            .Project<VoteSerialData>(projection)
            .ToListAsync();
        return results.ToDictionary(r => r.VoteSerial, r => r.BallotId);
    }

    public async Task MarkVoteSerialsAsInvalid(List<string> voteSerials)
    {
        var filter = Builders<VoteSerialData>.Filter.In(x => x.VoteSerial, voteSerials);
        var update = Builders<VoteSerialData>.Update.Set(x => x.AreVoteCodesCorrect, false);
        await _voteSerials.UpdateManyAsync(filter, update);
    }

    public async Task<List<string>> GetInvalidVoteSerials()
    {
        var filter = Builders<VoteSerialData>.Filter.Eq(x => x.AreVoteCodesCorrect, false);
        var projection = Builders<VoteSerialData>.Projection.Include(x => x.VoteSerial);
        var results = await _voteSerials
            .Find(filter)
            .Project<VoteSerialData>(projection)
            .ToListAsync();
        return results.Select(r => r.VoteSerial).ToList();
    }
}