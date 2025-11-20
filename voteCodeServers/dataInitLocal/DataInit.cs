using System.Security.Cryptography;
using System.Text;

public class DataInit
{
    // najlepiej zrobic plik z ustawieniami
    private readonly int _serialLenght = 10;
    private readonly string _serialAlphabet = "qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM1234567890";
    private readonly int _serverId;
    private readonly int _a; // alphabet length
    private readonly int _n; // total number of ballots
    private readonly int _numberOfServers;
    private readonly int _numberOfCandidates;
    private readonly BallotService _ballotService;
    private readonly VoteSerialsService _voteSerialService;
    private readonly BallotLinkingService _ballotLinkingService;


    public DataInit(int serverId, string A, int numberOfVoters, int safetyParameter, int numberOfServers, int numberOfCandidates)
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

    public async Task PartI()
    {
        var permutation = new PermutationGenerator(_n);

        for (int i = 1; i <= _n; i++)
        {
            Console.WriteLine(i);
            var ballot = await _ballotService.GetOrCreateBallot(i);

            ballot.Shadow = permutation.GetValue(i - 1);

            int first = Random.Shared.Next(1, _a + 1);
            int second;
            do
            {
                second = Random.Shared.Next(1, _a + 1);
            } while (second == first);

            ballot.C0 = first.ToString();
            var commitment = Comm(ballot.C0);
            ballot.CommC0 = commitment.Item1;
            ballot.R0 = commitment.Item2;

            ballot.C1 = second.ToString();
            commitment = Comm(ballot.C1);
            ballot.CommC1 = commitment.Item1;
            ballot.R1 = commitment.Item2;

            var sb = new StringBuilder();
            for (int j = 0; j < _numberOfCandidates; j++)
            {
                int bit = Random.Shared.Next(0, 2);
                sb.Append(bit);
            }

            ballot.B = sb.ToString();
            commitment = Comm(ballot.B);
            ballot.CommB = commitment.Item1;
            ballot.R2 = commitment.Item2;

            Console.WriteLine("Saved.");
            await _ballotService.SaveBallot(ballot);
        }
    }

    public async Task PartII()
    {
        if (_serverId != _numberOfServers)
        {
            var permutation = new PermutationGenerator(_n);

            for (int i = 1; i <= _n; i++)
            {
                Console.WriteLine(i);

                var ballot = await _ballotService.GetOrCreateBallot(i);
                ballot.ShadowPrim = permutation.GetValue(i - 1);

                Console.WriteLine("Saved.");
                await _ballotService.SaveBallot(ballot);
            }
        }
        else
        {
            for (int i = 1; i <= _n; i++)
            {
                string voteSerial = GenerateSerialNumber(_serialLenght, _serialAlphabet);
                var commitment = Comm(voteSerial);

                Console.WriteLine(voteSerial);
                await _voteSerialService.SaveVoteSerial(i, voteSerial, commitment.Item1, commitment.Item2);
            }

        }
    }

    public async Task DataLinking()
    {
        PermutationGenerator permutation;
        if (_serverId != 1)
        {
            permutation = new PermutationGenerator(_n);
            for (int i = 1; i <= _n; i++)
            {
                Console.WriteLine($"{i} {permutation.GetValue(i - 1)}");

                var commitment = Comm(permutation.GetValue(i - 1).ToString());
                await _ballotLinkingService.SaveLinking(i, permutation.GetValue(i - 1), commitment.Item1, commitment.Item2, false);
            }
        }

        permutation = new PermutationGenerator(_n);
        for (int i = 1; i <= _n; i++)
        {
            Console.WriteLine($"{i} {permutation.GetValue(i - 1)} Prim");
            var commitment = Comm(permutation.GetValue(i - 1).ToString());

            await _ballotLinkingService.SaveLinking(i, permutation.GetValue(i - 1), commitment.Item1, commitment.Item2, true);
        }
    }

    private string GenerateSerialNumber(int length, string alphabet)
    {
        var random = Random.Shared;
        var serial = new StringBuilder();
        for (int i = 0; i < length; i++)
        {
            serial.Append(alphabet[random.Next(alphabet.Length)]);
        }
        return serial.ToString();
    }

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