using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class PartialDecryptionData
{
    [BsonId]
    public ObjectId Id { get; set; }
    public string EncryptedVoteCodes { get; set; } = string.Empty;
    public string PartialDecryption { get; set; } = string.Empty;
}
