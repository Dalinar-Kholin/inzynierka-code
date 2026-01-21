using Org.BouncyCastle.Math;

Console.WriteLine("=== Partial Decryption Server ===");

if (args.Length < 1)
{
    Console.WriteLine("Usage: dotnet run <serverId>");
    return;
}

int serverId = int.Parse(args[0]);
Console.WriteLine($"Starting Partial Decryption Server {serverId}");

const int batchSize = 1000;

// services
var voteCodesService = new VoteCodesService(serverId);
var partialDecryptionService = new PartialDecryptionService(serverId);

var paillierSharedKey = new PaillierSharedKey(serverId, "../../encryption/paillierKeys/paillier_keys_private.json");

Console.WriteLine("Fetching vote codes from VoteCodesDatabase...");
long totalCount = await voteCodesService.GetTotalCount();
Console.WriteLine($"Total vote codes to process: {totalCount}");

if (totalCount == 0)
{
    Console.WriteLine("No vote codes found. Exiting.");
    return;
}

int processedCount = 0;
int skip = 0;
int ballotIdCounter = 1;  // Licznik BallotId od 1

while (skip < totalCount)
{
    Console.WriteLine($"Processing batch starting at {skip}...");

    var voteCodes = await voteCodesService.GetVoteCodesBatch(skip, batchSize);
    if (voteCodes.Count == 0)
        break;

    var partialDecryptionBatch = new List<PartialDecryptionData>();

    foreach (var voteCode in voteCodes)
    {
        try
        {
            var encryptedValue = new BigInteger(voteCode.EncryptedVoteCodes);
            var partialDecryption = paillierSharedKey.partial_decrypt(encryptedValue);

            // save partial decryption
            var partialDecryptionData = new PartialDecryptionData
            {
                Id = voteCode.Id,  // same Id as in VoteCodesData
                BallotId = ballotIdCounter,  // BallotId od 1 do końca
                EncryptedVoteCodes = voteCode.EncryptedVoteCodes,
                PartialDecryption = partialDecryption.ToString()
            };

            partialDecryptionBatch.Add(partialDecryptionData);
            processedCount++;
            ballotIdCounter++;  // Inkrementuj licznik BallotId
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing vote code: {ex.Message}");
        }
    }

    if (partialDecryptionBatch.Count > 0)
    {
        await partialDecryptionService.SavePartialDecryptionsBatch(partialDecryptionBatch);
        Console.WriteLine($"Saved {partialDecryptionBatch.Count} partial decryptions. Total processed: {processedCount}/{totalCount}");
    }

    skip += batchSize;
}

Console.WriteLine($"=== Partial Decryption Complete ===");

