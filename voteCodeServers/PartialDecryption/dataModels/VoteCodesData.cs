using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class VoteCodesData
{
    [BsonId]
    public ObjectId Id { get; set; }
    public string EncryptedVoteCodes { get; set; } = string.Empty;
    public bool IsUsed { get; set; } = false;
}
