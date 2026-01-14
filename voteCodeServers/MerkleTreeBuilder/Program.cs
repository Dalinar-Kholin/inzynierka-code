using VoteCodeServers.Helpers;
using System.Text.Json;
using System.Text;

Console.WriteLine("=== Merkle Tree Builder ===");

// Dictionary mapping collection numbers to collection names
var collectionNames = new Dictionary<int, string>
{
    { 0, "BallotData" },
    { 1, "PreprintAuditData" },
    { 2, "PartialDecryptionData" }
};

if (args.Length < 1)
{
    Console.WriteLine("Usage: dotnet run <serverId> [collectionNumber]");
    Console.WriteLine("  serverId: Server ID (required)");
    Console.WriteLine("  collectionNumber: Collection number (default: 0)");
    Console.WriteLine("\nAvailable collections:");
    foreach (var kvp in collectionNames)
    {
        Console.WriteLine($"    {kvp.Key} - {kvp.Value}");
    }
    return;
}

int serverId = int.Parse(args[0]);
int collectionNumber = args.Length > 1 ? int.Parse(args[1]) : 0;

if (!collectionNames.ContainsKey(collectionNumber))
{
    Console.WriteLine($"Error: Invalid collection number {collectionNumber}");
    Console.WriteLine("\nAvailable collections:");
    foreach (var kvp in collectionNames)
    {
        Console.WriteLine($"    {kvp.Key} - {kvp.Value}");
    }
    return;
}

string collectionName = collectionNames[collectionNumber];
Console.WriteLine($"Building Merkle Tree for Server {serverId}");
Console.WriteLine($"Collection: {collectionName} (#{collectionNumber})");

var cfg = Config.Load();
int numberOfVoters = cfg.NumberOfVoters;
int safetyParameter = cfg.SafetyParameter;
int numberOfServers = cfg.NumberOfServers;

int totalBallots = 4 * numberOfVoters + 2 * safetyParameter;
Console.WriteLine($"Total ballots expected: {totalBallots}");

var builder = new MerkleTreeBuilder(serverId, numberOfServers);

Console.WriteLine("\nBuilding Merkle tree...");
string rootHash = await builder.BuildMerkleTree();

Console.WriteLine("\n=== Merkle Tree Building Complete ===");
Console.WriteLine($"Root saved to merkle_root_server{serverId}.json");

// Send root hash to committer
Console.WriteLine("\nSending Merkle root to committer...");
try
{
    using (var client = new HttpClient())
    {
        // Convert hex string to bytes
        byte[] rootBytes = Convert.FromHexString(rootHash);

        // CommitmentType 128 = MerkleRoot (> 127, fits in uint8)
        var payload = new
        {
            commitmentType = 128,
            id = serverId,
            data = rootBytes
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("http://127.0.0.1:8080/commitSingleValue", content);

        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine($"✓ Root hash sent successfully to committer");
        }
        else
        {
            Console.WriteLine($"✗ Failed to send root hash: {response.StatusCode}");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"✗ Error sending root hash: {ex.Message}");
}
