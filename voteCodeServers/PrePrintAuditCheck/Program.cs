using VoteCodeServers.Helpers;
using Org.BouncyCastle.Math;

var cfg = Config.Load();
int numberOfCandidates = cfg.NumberOfCandidates;
int numberOfVoters = cfg.NumberOfVoters;
int numberOfServers = cfg.NumberOfServers;

const int batchSize = 1000;
var path = "../../encryption/paillierKeys/paillier_keys_private.json";

//////////////////////////////////////////////////
/// dla uproszczenia używamy tylko jednego serwera do audytu żeby tylko sprawdzić które kody są unikalne i czy da się to wgl zrobić
/// dla pełnej weryfikowalności powinno być to zrobione w rozszerzonej wersji przez wszystkie serwery
//////////////////////////////////////////////////

var sharedKeys = new List<PaillierSharedKey>
        {

            new PaillierSharedKey(serverNumber: 1, sharedKeyPath: path),
            new PaillierSharedKey(serverNumber: 2, sharedKeyPath: path),
            new PaillierSharedKey(serverNumber: 3, sharedKeyPath: path),
            new PaillierSharedKey(serverNumber: 4, sharedKeyPath: path),
            new PaillierSharedKey(serverNumber: 5, sharedKeyPath: path),
            new PaillierSharedKey(serverNumber: 6, sharedKeyPath: path),
            new PaillierSharedKey(serverNumber: 7, sharedKeyPath: path),
            new PaillierSharedKey(serverNumber: 8, sharedKeyPath: path),
            new PaillierSharedKey(serverNumber: 9, sharedKeyPath: path),
            new PaillierSharedKey(serverNumber: 10, sharedKeyPath: path)
        };

int thresholdPlayers = sharedKeys.First().degree + 1;

PaillierSharedKey decryptKey = sharedKeys.First(k => k.player_id == 1);

VoteSerialsService voteSerialsService = new VoteSerialsService(numberOfServers);
PrePrintAuditService prePrintAuditService = new PrePrintAuditService(numberOfServers);

int skip = 0;
int nonUniqueCount = 0;

var notUniqueVoteSerials = new List<string>();

while (true)
{
    var batch = await prePrintAuditService.GetPrePrintAuditBatch(batchSize, skip);
    if (batch.Count == 0)
        break;

    Parallel.ForEach(batch, ballot =>
    {
        var isUnique = AreVoteCodessUnique(ballot);
        if (!isUnique)
        {
            Interlocked.Increment(ref nonUniqueCount);
            lock (notUniqueVoteSerials)
            {
                notUniqueVoteSerials.Add(ballot.BallotVoteSerial);
            }
        }
    });
    Console.WriteLine($"Processed batch with skip {skip}, found so far {nonUniqueCount} non-unique ballots.");

    skip += batchSize;
}

Console.WriteLine($"Total non-unique ballots found: {nonUniqueCount}");
foreach (var voteSerial in notUniqueVoteSerials)
{
    Console.WriteLine($"Non-unique ballot vote serial: {voteSerial}");
}
voteSerialsService.MarkVoteSerialsAsInvalid(notUniqueVoteSerials).Wait();


bool AreVoteCodessUnique(PrePrintAuditData ballot)
{
    // decrypt vectors
    var decryptedVectors = new List<List<bool>>();
    foreach (var vector in ballot.Vectors)
    {
        var decryptedVector = new List<bool>();

        BigInteger? firstEncryptedValue = null;
        foreach (var encryptedValue in vector)
        {
            BigInteger ciphertext = new BigInteger(encryptedValue);
            var partialDecryptions = BuildPartialDecryptions(ciphertext);
            BigInteger decryptedValue = decryptKey.decrypt(partialDecryptions);
            if (firstEncryptedValue == null)
            {
                firstEncryptedValue = decryptedValue;
            }
            if (!decryptedValue.Equals(firstEncryptedValue))
            {
                decryptedVector.Add(false);
            }
            else
            {
                decryptedVector.Add(true);
            }
        }
        decryptedVectors.Add(decryptedVector);
    }

    // combining codes, code = vector[0][0], vector[1][0], ..., vector[n][0]
    var voteCodes = new List<string>();
    for (int i = 0; i < decryptedVectors[0].Count; i++)
    {
        var codeChars = new List<char>();
        for (int j = 0; j < decryptedVectors.Count; j++)
        {
            codeChars.Add(decryptedVectors[j][i] ? '1' : '0');
        }
        voteCodes.Add(new string(codeChars.ToArray()));
        Console.WriteLine($"Vote code {i} for ballot {ballot.BallotVoteSerial}: {voteCodes.Last()}");
    }

    // check code uniqueness
    var uniqueCodes = new HashSet<string>();
    foreach (var code in voteCodes)
    {
        if (!uniqueCodes.Add(code))
        {
            notUniqueVoteSerials.Add(ballot.BallotVoteSerial);
            Console.WriteLine($"Non-unique vote code found for ballot {ballot.BallotVoteSerial}: {code}");
            return false;
        }
    }
    return true;
}

Dictionary<int, BigInteger> BuildPartialDecryptions(BigInteger ciphertext)
{
    var partialDecryptions = new Dictionary<int, BigInteger>();
    foreach (var key in sharedKeys)
    {
        if (key.player_id <= thresholdPlayers)
        {
            partialDecryptions[key.player_id] = key.partial_decrypt(ciphertext);
        }
    }
    return partialDecryptions;
}