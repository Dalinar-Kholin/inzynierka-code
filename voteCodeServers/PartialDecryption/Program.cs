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

// Inicjalizacja serwisów
var voteCodesService = new VoteCodesService(serverId);
var partialDecryptionService = new PartialDecryptionService(serverId);

// Załaduj klucz prywatny Pailliera
var paillierSharedKey = new PaillierSharedKey(serverId, "../../encryption/paillierKeys/paillier_keys_private.json");

Console.WriteLine("Fetching vote codes from VoteCodesDatabase...");
long totalCount = await voteCodesService.GetTotalCount();
Console.WriteLine($"Total vote codes to process: {totalCount}");

if (totalCount == 0)
{
    Console.WriteLine("No vote codes found. Exiting.");
    return;
}

// Wyczyść poprzednie dane (opcjonalnie)
Console.WriteLine("Clearing previous partial decryptions...");
await partialDecryptionService.ClearCollection();

int processedCount = 0;
int skip = 0;

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
            // Konwertuj zaszyfrowany kod na BigInteger
            var encryptedValue = new BigInteger(voteCode.EncryptedVoteCodes);

            // Wykonaj częściową deszyfrację
            var partialDecryption = paillierSharedKey.partial_decrypt(encryptedValue);

            // Zapisz wynik
            var partialDecryptionData = new PartialDecryptionData
            {
                Id = voteCode.Id,  // Zachowaj to samo _id
                EncryptedVoteCodes = voteCode.EncryptedVoteCodes,
                PartialDecryption = partialDecryption.ToString()
            };

            partialDecryptionBatch.Add(partialDecryptionData);
            processedCount++;
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
Console.WriteLine($"Total processed: {processedCount}");
Console.WriteLine($"Results saved in PartialDecryptionDatabase_Server{serverId}");

