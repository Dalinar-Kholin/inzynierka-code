using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

public class MerkleTreeBuilder
{
    private readonly BallotService _ballotService;
    private readonly int _serverId;

    public MerkleTreeBuilder(int serverId, int numberOfServers)
    {
        _serverId = serverId;
        _ballotService = new BallotService(serverId, numberOfServers);
    }

    private string HashCommitment(string commitment)
    {
        using (var sha512 = SHA512.Create())
        {
            byte[] hashBytes = sha512.ComputeHash(Encoding.UTF8.GetBytes(commitment));
            return Convert.ToHexString(hashBytes).ToLower();
        }
    }

    private static string HashPair(string left, string right)
    {
        using (var sha512 = SHA512.Create())
        {
            string combined = left + right;
            byte[] hashBytes = sha512.ComputeHash(Encoding.UTF8.GetBytes(combined));
            return Convert.ToHexString(hashBytes).ToLower();
        }
    }

    public async Task<string> BuildMerkleTree()
    {
        Console.WriteLine($"Building Merkle Tree for Server {_serverId}...");

        long totalCount = await _ballotService.GetTotalCount();
        Console.WriteLine($"Total ballots: {totalCount}");

        if (totalCount == 0)
        {
            Console.WriteLine("No ballots found.");
            return string.Empty;
        }

        // get all leaves
        var leaves = new List<string>();
        int skip = 0;
        const int batchSize = 1000;

        while (skip < totalCount)
        {
            var ballots = await _ballotService.GetBallotsBatch(skip, batchSize);

            foreach (var ballot in ballots)
            {
                // add all 3 commitments to the tree
                leaves.Add(HashCommitment(ballot.CommC0));
                leaves.Add(HashCommitment(ballot.CommC1));
                leaves.Add(HashCommitment(ballot.CommB));
            }
            skip += batchSize;
        }

        Console.WriteLine($"Loaded {leaves.Count} leaves (CommC0, CommC1, CommB for each ballot)");

        int level = 0;
        var currentLevel = leaves;

        // build higher levels
        while (currentLevel.Count > 1)
        {
            var nextLevel = new List<string>();
            level++;

            for (int i = 0; i < currentLevel.Count; i += 2)
            {
                string left = currentLevel[i];
                string right = (i + 1 < currentLevel.Count) ? currentLevel[i + 1] : currentLevel[i];
                string parentHash = HashPair(left, right);
                nextLevel.Add(parentHash);
            }

            currentLevel = nextLevel;
        }

        string rootHash = currentLevel[0];
        Console.WriteLine($"Root hash: {rootHash}");

        // save root to JSON file
        var rootData = new
        {
            ServerId = _serverId,
            Phase = 1,
            CommitmentType = "CommC0+CommC1+CommB",
            RootHash = rootHash,
            TotalLeaves = leaves.Count,
            TotalBallots = totalCount,
            TreeLevels = level + 1,
            Timestamp = DateTime.UtcNow
        };

        string jsonFile = $"merkle_root_server{_serverId}.json";
        string json = JsonSerializer.Serialize(rootData, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(jsonFile, json);

        Console.WriteLine($"Merkle tree complete. Levels: {level + 1}");
        Console.WriteLine($"Root saved to: {jsonFile}");
        Console.WriteLine($"Root hash to send to BB: {rootHash}");

        return rootHash;
    }

    public static string BuildMerkleRootFromLeaves(List<string> leaves)
    {
        if (leaves == null || leaves.Count == 0)
            throw new ArgumentException("Leaves list must not be empty");

        var currentLevel = new List<string>(leaves);
        while (currentLevel.Count > 1)
        {
            var nextLevel = new List<string>();
            for (int i = 0; i < currentLevel.Count; i += 2)
            {
                string left = currentLevel[i];
                string right = (i + 1 < currentLevel.Count) ? currentLevel[i + 1] : currentLevel[i];
                string parentHash = HashPair(left, right);
                nextLevel.Add(parentHash);
            }
            currentLevel = nextLevel;
        }
        return currentLevel[0];
    }
}
