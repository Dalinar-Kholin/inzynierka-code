using MongoDB.Driver;
using MongoDB.Bson;

public class VoteService
{
    private readonly IMongoCollection<VoteData> _votes;


    public VoteService()
    {
        var client = new MongoClient("mongodb://localhost:27017");
        var database = client.GetDatabase($"VoteCodesDatabase");
        _votes = database.GetCollection<VoteData>("Votes");

        _votes.Indexes.CreateOne(new CreateIndexModel<VoteData>(
            Builders<VoteData>.IndexKeys.Ascending(b => b.AuthCode),
            new CreateIndexOptions { Unique = true }));
    }

    public async Task CreateVotesBatch(List<VoteData> votes)
    {
        if (votes.Count > 0)
        {
            await _votes.InsertManyAsync(votes, new InsertManyOptions { IsOrdered = false });
        }
    }
}