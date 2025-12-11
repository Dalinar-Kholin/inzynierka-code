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

    public async Task SaveVoteSerial(int id, string trueVoteSerial, string commitment, long randomKey)
    {
        var record = await _voteSerials
            .Find(b => b.BallotId == id)
            .FirstOrDefaultAsync();

        if (record == null)
        {
            var newData = new VoteSerialData
            {
                Id = ObjectId.GenerateNewId(),
                BallotId = id,
                VoteSerial = trueVoteSerial,
                CommVoteSerial = commitment,
                R0 = randomKey
            };
            await _voteSerials.InsertOneAsync(newData);
        }
        else
        {
            record.VoteSerial = trueVoteSerial;
            record.CommVoteSerial = commitment;
            record.R0 = randomKey;
            await _voteSerials.ReplaceOneAsync(b => b.Id == record.Id, record);
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
}
