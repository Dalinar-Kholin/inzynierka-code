using System.Security.Cryptography;
using System.Text;
using System.Linq;
using MongoDB.Driver;
using MongoDB.Bson;
using Org.BouncyCastle.Math;

public class CodeSetting
{
    private readonly int _serverId;
    private readonly int _numberOfServers;
    private readonly int _alphabetSize;
    private readonly int _numberOfCandidates;
    private readonly CodeSettingService _codeSettingService;
    private readonly BallotService _ballotService;
    private readonly PaillierPublicKey _paillierPublic;
    private const int _batchSize = 1000;

    public CodeSetting(int serverId, int numberOfServers, int alphabetSize, int numberOfCandidates)
    {
        _serverId = serverId;
        _numberOfServers = numberOfServers;
        _alphabetSize = alphabetSize;
        _numberOfCandidates = numberOfCandidates;
        _codeSettingService = new CodeSettingService(serverId);
        _ballotService = new BallotService(serverId, numberOfServers);
        _paillierPublic = new PaillierPublicKey("../../encryption/paillierKeys");
    }

    public async Task Execute(string randomValue)
    {
        int currentBatch = 0;
        bool hasMoreData = true;

        Console.WriteLine($"CodeSetting for server {_serverId}...");

        while (hasMoreData)
        {
            var ballotDataBatch = await _ballotService.GetProtocol5DataBatch(currentBatch * _batchSize, _batchSize);

            if (!ballotDataBatch.Any())
            {
                hasMoreData = false;
                Console.WriteLine("No more data");
                break;
            }

            Console.WriteLine($"Batch {currentBatch + 1}");

            var codeSettingsArray = new CodeSettingData[ballotDataBatch.Count];

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount - 2
            };

            Parallel.For(0, ballotDataBatch.Count, parallelOptions, idx =>
            {
                try
                {
                    var ballotData = ballotDataBatch[idx];
                    int i = ballotData.BallotId;
                    int j = _serverId;

                    long c_j_i_r = SummandDraw.ComputeSummand(randomValue, j, i);

                    int c_j_i_0_p = ballotData.C0;
                    int c_j_i_1_p = ballotData.C1;

                    int c0 = (int)((c_j_i_r + (long)c_j_i_0_p) % _alphabetSize);
                    int c1 = (int)((c_j_i_r + (long)c_j_i_1_p) % _alphabetSize);

                    var b_m_values = new int[_numberOfCandidates];

                    var b_p_values = ballotData.B.Select(bit => bit == '1' ? 1 : 0).ToArray();

                    for (int m = 0; m < _numberOfCandidates; m++)
                    {
                        bool b_j_i_m_r_bool = SummandDraw.ComputeSummandBit(randomValue, j, i, m + 1);
                        int b_j_i_m_r = b_j_i_m_r_bool ? 1 : 0;

                        int b_m = (b_j_i_m_r + b_p_values[m]) % 2;
                        b_m_values[m] = b_m;
                    }

                    var commitments = GenerateCommitments(c0, c1, b_m_values);

                    var codeSetting = new CodeSettingData
                    {
                        Id = ObjectId.GenerateNewId(),
                        BallotId = ballotData.BallotId,
                        FinalB = string.Join("", b_m_values),
                        CommB = commitments.CommB,
                        R0 = commitments.R0,
                        FinalC0 = c0,
                        CommC0 = commitments.CommC0,
                        R1 = commitments.R1,
                        FinalC1 = c1,
                        CommC1 = commitments.CommC1,
                        R2 = commitments.R2,
                        V = commitments.V,
                    };

                    codeSettingsArray[idx] = codeSetting;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error i: {ballotDataBatch[idx].BallotId}: {ex.Message}");
                }
            });


            var codeSettingsBatch = codeSettingsArray.ToList();
            if (codeSettingsBatch.Count > 0)
            {
                await _codeSettingService.SaveCodeSettingsBatch(codeSettingsBatch);
                Console.WriteLine($"Batch {currentBatch + 1} saved");
            }

            currentBatch++;
        }

        Console.WriteLine($"CodeSetting completed server {_serverId}");
    }


    private Commitments GenerateCommitments(int c0, int c1, int[] b_m_values)
    {
        var commitments = new Commitments();
        using var sha256 = SHA256.Create();

        byte[] randomBytesB = new byte[8];
        RandomNumberGenerator.Fill(randomBytesB);
        long r0 = BitConverter.ToInt64(randomBytesB) & long.MaxValue;

        var inputB = Encoding.UTF8.GetBytes($"{string.Join("", b_m_values)}{r0}");
        byte[] hashB = sha256.ComputeHash(inputB);
        string hashedValueB = Convert.ToHexString(hashB).ToLower();

        commitments.CommB = hashedValueB;
        commitments.R0 = r0;

        byte[] randomBytesC0 = new byte[8];
        RandomNumberGenerator.Fill(randomBytesC0);
        long r1 = BitConverter.ToInt64(randomBytesC0) & long.MaxValue;

        var inputC0 = Encoding.UTF8.GetBytes($"{c0}{r1}");
        byte[] hashC0 = sha256.ComputeHash(inputC0);
        string hashedValueC0 = Convert.ToHexString(hashC0).ToLower();

        commitments.CommC0 = hashedValueC0;
        commitments.R1 = r1;

        byte[] randomBytesC1 = new byte[8];
        RandomNumberGenerator.Fill(randomBytesC1);
        long r2 = BitConverter.ToInt64(randomBytesC1) & long.MaxValue;

        var inputC1 = Encoding.UTF8.GetBytes($"{c1}{r2}");
        byte[] hashC1 = sha256.ComputeHash(inputC1);
        string hashedValueC1 = Convert.ToHexString(hashC1).ToLower();

        commitments.CommC1 = hashedValueC1;
        commitments.R2 = r2;

        commitments.V = new string[_numberOfCandidates];

        byte[] randomBytes = new byte[8];
        RandomNumberGenerator.Fill(randomBytes);
        long randomness = BitConverter.ToInt64(randomBytes) & long.MaxValue;

        for (int m = 0; m < _numberOfCandidates; m++)
        {
            // V = ⟨Enc_EA(H(b_1||r)), ..., Enc_EA(H(b_k||r))⟩
            // hash(data || randomValue)
            var input = Encoding.UTF8.GetBytes($"{b_m_values[m]}{randomness}");
            byte[] hash = sha256.ComputeHash(input);

            string hashedValue = Convert.ToHexString(hash).ToLower();
            commitments.V[m] = EncryptEA(hashedValue).ToString();
        }

        return commitments;
    }

    private BigInteger EncryptEA(string message)
    {
        return _paillierPublic.EncryptHash(message);
    }
}
