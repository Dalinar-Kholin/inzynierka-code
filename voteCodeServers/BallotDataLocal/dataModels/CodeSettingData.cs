using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class CodeSettingData
{
    [BsonId]
    public ObjectId Id { get; set; }
    public int BallotId { get; set; }
    public string FinalB { get; set; }
    public string CommB { get; set; }
    public long R0 { get; set; }
    public int FinalC0 { get; set; }
    public string CommC0 { get; set; }
    public long R1 { get; set; }
    public int FinalC1 { get; set; }
    public string CommC1 { get; set; }
    public long R2 { get; set; }
    public string[] V { get; set; } // globalnie - zaszyfrowane EA
}