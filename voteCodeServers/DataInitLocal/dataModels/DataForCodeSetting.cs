using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class DataForCodeSetting
{
    [BsonId]
    public ObjectId Id { get; set; }
    public int BallotId { get; set; }
    public int C0 { get; set; }
    public int C1 { get; set; }
    public string B { get; set; }

}