using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class BallotLinking
{
    [BsonId]
    public ObjectId Id { get; set; }
    public int PrevBallotId { get; set; }
    public int BallotId { get; set; }
    public string CommLinking { get; set; }
    public long R0 { get; set; }
}