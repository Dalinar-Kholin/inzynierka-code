using MongoDB.Driver;
using MongoDB.Bson;

public class VoteCodesService
{
    private readonly IMongoCollection<VoteCodesData> _ballots;


    public VoteCodesService(int serverId)
    {
        var client = new MongoClient("mongodb://localhost:27017");
        var database = client.GetDatabase($"VoteCodesDatabase");
        _ballots = database.GetCollection<VoteCodesData>("EncryptedVoteCodes");
    }

    public async Task CreateVoteCodesBatch(List<VoteCodesData> ballots)
    {
        if (ballots.Count > 0)
        {
            await _ballots.InsertManyAsync(ballots, new InsertManyOptions { IsOrdered = false });
        }
    }

    public async Task<List<VoteCodesData>> GetVoteCodesBatch(int skip, int limit)
    {
        return await _ballots.Find(Builders<VoteCodesData>.Filter.Empty)
            .Skip(skip)
            .Limit(limit)
            .ToListAsync();
    }

    public async Task<long> GetTotalCount()
    {
        return await _ballots.CountDocumentsAsync(Builders<VoteCodesData>.Filter.Empty);
    }
}