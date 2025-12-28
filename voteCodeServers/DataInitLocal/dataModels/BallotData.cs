using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class BallotData
{
    [BsonId]
    public ObjectId Id { get; set; }
    public int BallotId { get; set; }
    public int Shadow { get; set; }
    public int ShadowPrim { get; set; } = -1;
    public int C0 { get; set; }
    public string CommC0 { get; set; }
    public long R0 { get; set; }
    public int C1 { get; set; }
    public string CommC1 { get; set; }
    public long R1 { get; set; }
    public string B { get; set; }
    public string CommB { get; set; }
    public long R2 { get; set; }
}