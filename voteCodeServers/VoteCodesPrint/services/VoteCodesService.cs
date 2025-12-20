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
}