using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class BallotData
{
    [BsonId]
    public ObjectId Id { get; set; }
    public int BallotId { get; set; }
    public int? Shadow { get; set; } = null;
    public int? ShadowPrim { get; set; } = null;
    public string? C0 { get; set; } = null;
    public string? CommC0 { get; set; } = null;
    public long? R0 { get; set; } = null;
    public string? C1 { get; set; } = null;
    public string? CommC1 { get; set; } = null;
    public long? R1 { get; set; } = null;
    public string? B { get; set; } = null;
    public string? CommB { get; set; } = null;
    public long? R2 { get; set; } = null;
}