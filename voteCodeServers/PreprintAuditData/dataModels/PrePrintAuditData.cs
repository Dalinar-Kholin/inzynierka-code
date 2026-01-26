using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class PrePrintAuditData
{
    [BsonId]
    public ObjectId Id { get; set; }
    public string BallotVoteSerial { get; set; } = string.Empty;
    public List<string[]> Vectors { get; set; } = new List<string[]>();
}
