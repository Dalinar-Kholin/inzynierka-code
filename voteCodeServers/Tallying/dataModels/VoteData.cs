using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class VoteData
{
    [BsonId]
    public ObjectId Id { get; set; }
    public string AuthCode { get; set; }
    public string VoteSerial { get; set; }
    public List<string> VoteVector { get; set; } = new List<string>();
}
