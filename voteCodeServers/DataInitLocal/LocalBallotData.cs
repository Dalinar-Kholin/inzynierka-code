using System.Security.Cryptography;
using System.Text;
using MongoDB.Driver;
using MongoDB.Bson;

public class LocalBallotData
{
    // najlepiej zrobic plik z ustawieniami
    private const int _batchSize = 1000;
    private const int _serialLenght = 10;
    private const string _serialAlphabet = "qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM1234567890";
    private readonly int _serverId;
    private readonly int _a; // alphabet length
    private readonly int _n; // total number of ballots
    private readonly int _numberOfServers;
    private readonly int _numberOfCandidates;
    private readonly BallotService _ballotService;
    private readonly VoteSerialsService _voteSerialService;
    private readonly BallotLinkingService _ballotLinkingService;


    public LocalBallotData(int serverId, string A, int numberOfVoters, int safetyParameter, int numberOfServers, int numberOfCandidates)
    {
        var n = 4 * numberOfVoters + 2 * safetyParameter;
        if (n > 100_000_000) throw new ArgumentException("Too big."); ;

        _serverId = serverId;
        _a = A.Length;
        _n = n;
        _numberOfServers = numberOfServers;
        _numberOfCandidates = numberOfCandidates;
        _ballotService = new BallotService(serverId);
        _voteSerialService = new VoteSerialsService(serverId);
        _ballotLinkingService = new BallotLinkingService(serverId);
    }

    public async Task DataInit()
    {
        var ballotsBatch = new List<BallotData>();
        var voteSerialsBatch = new List<VoteSerialData>();

        var permutation = new PermutationGenerator(_n);
        var permutationPrim = new PermutationGenerator(_n);

        for (int i = 1; i <= _n; i++)
        {
            Console.WriteLine(i);

            var ballot = new BallotData
            {
                BallotId = i,
                Shadow = permutation.GetValue(i - 1)
            };

            int first = RandomNumberGenerator.GetInt32(1, _a + 1);
            int second;
            do
            {
                second = RandomNumberGenerator.GetInt32(1, _a + 1);
            } while (second == first);

            ballot.C0 = first;
            var commitment = Comm(ballot.C0.ToString());
            ballot.CommC0 = commitment.Item1;
            ballot.R0 = commitment.Item2;

            ballot.C1 = second;
            commitment = Comm(ballot.C1.ToString());
            ballot.CommC1 = commitment.Item1;
            ballot.R1 = commitment.Item2;

            var sb = new StringBuilder();
            for (int j = 0; j < _numberOfCandidates; j++)
            {
                sb.Append(RandomNumberGenerator.GetInt32(0, 2));
            }

            ballot.B = sb.ToString();
            commitment = Comm(ballot.B);
            ballot.CommB = commitment.Item1;
            ballot.R2 = commitment.Item2;

            if (_serverId != _numberOfServers)
            {
                ballot.ShadowPrim = permutationPrim.GetValue(i - 1);
            }

            ballotsBatch.Add(ballot);

            if (_serverId == _numberOfServers)
            {
                string voteSerial = GenerateSerialNumber(_serialLenght, _serialAlphabet);
                var serialCommitment = Comm(voteSerial);

                var voteSerialData = new VoteSerialData
                {
                    Id = ObjectId.GenerateNewId(),
                    BallotId = i,
                    VoteSerial = voteSerial,
                    CommVoteSerial = serialCommitment.Item1,
                    R0 = serialCommitment.Item2
                };
                voteSerialsBatch.Add(voteSerialData);
            }

            if (ballotsBatch.Count >= _batchSize || i == _n)
            {
                await _ballotService.CreateBallotsBatch(ballotsBatch);
                ballotsBatch.Clear();
                Console.WriteLine("Ballots batch saved.");

                if (_serverId == _numberOfServers && voteSerialsBatch.Count > 0)
                {
                    await _voteSerialService.SaveVoteSerialsBatch(voteSerialsBatch);
                    voteSerialsBatch.Clear();
                    Console.WriteLine("VoteSerials batch saved.");
                }
            }
        }
    }

    public async Task DataLinking()
    {
        PermutationGenerator permutation;

        if (_serverId != 1)
        {
            permutation = new PermutationGenerator(_n);
            var batchRecords = new List<BallotLinking>();

            for (int i = 1; i <= _n; i++)
            {
                Console.WriteLine($"{i} {permutation.GetValue(i - 1)}");

                var commitment = Comm(permutation.GetValue(i - 1).ToString());

                var record = new BallotLinking
                {
                    Id = ObjectId.GenerateNewId(),
                    BallotId = i,
                    PrevBallot = permutation.GetValue(i - 1),
                    CommPrevBallot = commitment.Item1,
                    R0 = commitment.Item2
                };
                batchRecords.Add(record);

                if (batchRecords.Count >= _batchSize || i == _n)
                {
                    await _ballotLinkingService.SaveLinkingBatch(batchRecords, false);
                    batchRecords.Clear();
                    Console.WriteLine("Links batch saved.");
                }
            }
        }

        permutation = new PermutationGenerator(_n);
        var batchRecordsPrim = new List<BallotLinking>();

        for (int i = 1; i <= _n; i++)
        {
            Console.WriteLine($"{i} {permutation.GetValue(i - 1)} Prim");
            var commitment = Comm(permutation.GetValue(i - 1).ToString());

            var record = new BallotLinking
            {
                Id = ObjectId.GenerateNewId(),
                BallotId = i,
                PrevBallot = permutation.GetValue(i - 1),
                CommPrevBallot = commitment.Item1,
                R0 = commitment.Item2
            };
            batchRecordsPrim.Add(record);

            if (batchRecordsPrim.Count >= _batchSize || i == _n)
            {
                await _ballotLinkingService.SaveLinkingBatch(batchRecordsPrim, true);
                batchRecordsPrim.Clear();
                Console.WriteLine("LinksPrim batch saved.");
            }
        }
    }

    private string GenerateSerialNumber(int length, string alphabet)
    {
        var random = Random.Shared;
        var serial = new StringBuilder();
        for (int i = 0; i < length; i++)
        {
            serial.Append(alphabet[RandomNumberGenerator.GetInt32(1, alphabet.Length)]);
        }
        return serial.ToString();
    }

    // zamienic na perfect hiding
    public (string commitment, long randomValue) Comm(string data)
    {
        // blinding factor
        byte[] randomBytes = new byte[8];
        RandomNumberGenerator.Fill(randomBytes);
        long randomValue = BitConverter.ToInt64(randomBytes) & long.MaxValue;

        using var sha256 = SHA256.Create();

        // hash(data || randomValue)
        var input = Encoding.UTF8.GetBytes(data + randomValue.ToString());
        byte[] hash = sha256.ComputeHash(input);

        string commitment = Convert.ToHexString(hash).ToLower();

        return (commitment, randomValue);
    }

    public bool Reveal(string data, long randomValue, string commitment)
    {
        using var sha256 = SHA256.Create();

        byte[] input = Encoding.UTF8.GetBytes(data + randomValue.ToString());
        byte[] hash = sha256.ComputeHash(input);

        string computed = Convert.ToHexString(hash).ToLower();

        return computed == commitment;
    }
}