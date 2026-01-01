using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class VoteSerialData
{
    [BsonId]
    public ObjectId Id { get; set; }
    public int BallotId { get; set; }
    public string VoteSerial { get; set; }
    public string CommVoteSerial { get; set; }
    public long R0 { get; set; }
    public bool AreVoteCodesCorrect { get; set; } = true; // zmieniane przez EA po pre-print audit
}