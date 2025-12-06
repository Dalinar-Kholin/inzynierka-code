using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class CodeSettingData
{
    [BsonId]
    public ObjectId Id { get; set; }
    public int BallotId { get; set; }
    public string FinalB { get; set; } // lokalnie
    public int FinalC0 { get; set; } // lokalnie
    public string CommC0c1 { get; set; } //globalnie - zaszyfrowane elgamalem
    public string CommC0c2 { get; set; } //globalnie - zaszyfrowane elgamalem
    public int FinalC1 { get; set; } // lokalnie
    public string CommC1c1 { get; set; } // globalnie - zaszyfrowane elgamalem
    public string CommC1c2 { get; set; } // globalnie - zaszyfrowane elgamalem
    public string[] Z0 { get; set; } // doprecyzować jak ma wygladac
    public string[] Z1 { get; set; } // doprecyzować jak ma wygladac
    public string BindingC0 { get; set; } // globalnie - wymyślić jak ma wygladac
    public string BindingC1 { get; set; } // globalnie - wymyślić jak ma wygladac
    public string[] V { get; set; } // globalnie - zaszyfrowane EA
    public long R0 { get; set; } // lokalnie - nie wiem czy potrzebne, chyba że V nie służy tylko do sprawdzenia unikalności kodów
}