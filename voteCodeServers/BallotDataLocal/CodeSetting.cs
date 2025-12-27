using System.Security.Cryptography;
using System.Text;
using System.Linq;
using MongoDB.Driver;
using MongoDB.Bson;
using Org.BouncyCastle.Math;


public class CodeSetting
{
    private const int _batchSize = 1000;
    private readonly int _serverId;
    private readonly int _numberOfServers;
    private readonly int _a; // alphabet size
    private readonly int _k; // number of candidates
    private readonly ElGamalEncryption _elGamal;
    private readonly CodeSettingService _codeSettingService;
    private readonly BallotService _ballotService;
    private readonly PaillierPublicKey _paillierPublic;

    public CodeSetting(int serverId, int numberOfServers, int alphabetSize, int numberOfCandidates)
    {
        _serverId = serverId;
        _numberOfServers = numberOfServers;
        _a = alphabetSize;
        _k = numberOfCandidates;
        _codeSettingService = new CodeSettingService(serverId);
        _ballotService = new BallotService(serverId, numberOfServers);
        _elGamal = new ElGamalEncryption("../../encryption/elGamalKeys");
        _paillierPublic = new PaillierPublicKey("../../encryption/paillierKeys");
    }

    public async Task Execute(string randomValue)
    {
        byte[] r_x = Encoding.UTF8.GetBytes(randomValue);

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

            // Utrzymaj równoległość bez blokad i zachowaj kolejność przez wypełnianie tabeli indeksowanej
            // chyab wsm nie musi byc w kolejnosci - trzeba zobaczyc
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

                    long c_j_i_r = SummandDraw.GenerateSummand(r_x, j, i);

                    int c_j_i_0_p = ballotData.C0;
                    int c_j_i_1_p = ballotData.C1;

                    int c0 = (int)((c_j_i_r + (long)c_j_i_0_p) % _a);
                    int c1 = (int)((c_j_i_r + (long)c_j_i_1_p) % _a);

                    var b_m_values = new int[_k];

                    var b_p_values = ballotData.B.Select(bit => bit == '1' ? 1 : 0).ToArray();

                    for (int m = 0; m < _k; m++)
                    {
                        bool b_j_i_m_r_bool = SummandDraw.GenerateRandomBit(r_x, j, i, m + 1);
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
                        FinalC0 = c0,
                        CommC0c1 = commitments.CommC0c1,
                        CommC0c2 = commitments.CommC0c2,
                        FinalC1 = c1,
                        CommC1c1 = commitments.CommC1c1,
                        CommC1c2 = commitments.CommC1c2,
                        Z0 = commitments.Z0,
                        Z1 = commitments.Z1,
                        BindingC0 = "nie wiadomo jeszcze co",
                        BindingC1 = "nie wiadomo jeszcze co",
                        V = commitments.V,
                        R0 = commitments.Randomness,
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

        var encryptedC0 = EncryptTj(c0);
        var encryptedC1 = EncryptTj(c1);

        commitments.CommC0c1 = encryptedC0.c1.ToString();
        commitments.CommC0c2 = encryptedC0.c2.ToString();
        commitments.CommC1c1 = encryptedC1.c1.ToString();
        commitments.CommC1c2 = encryptedC1.c2.ToString();

        commitments.Z0 = new string[_k];
        commitments.Z1 = new string[_k];
        commitments.V = new string[_k];

        byte[] randomBytes = new byte[8];
        RandomNumberGenerator.Fill(randomBytes);
        long randomness = BitConverter.ToInt64(randomBytes) & long.MaxValue;

        commitments.Randomness = randomness;

        for (int m = 0; m < _k; m++)
        {
            // Z0 = ⟨Enc_EA(1-b_1), ..., Enc_EA(1-b_k)⟩
            commitments.Z0[m] = Dummy((1 - b_m_values[m]).ToString());

            // Z1 = ⟨Enc_EA(b_1), ..., Enc_EA(b_k)⟩
            commitments.Z1[m] = Dummy(b_m_values[m].ToString());

            // V = ⟨Enc_EA(H(b_1||r)), ..., Enc_EA(H(b_k||r))⟩
            using var sha256 = SHA256.Create();

            // hash(data || randomValue)
            var input = Encoding.UTF8.GetBytes($"{b_m_values[m]}{randomness}");
            byte[] hash = sha256.ComputeHash(input);

            string hashedValue = Convert.ToHexString(hash).ToLower();
            commitments.V[m] = EncryptEA(hashedValue).ToString();
        }

        return commitments;
    }

    private (BigInteger c1, BigInteger c2) EncryptTj(int message)
    {
        return _elGamal.Encrypt(message);
    }

    private BigInteger EncryptEA(string message)
    {
        return _paillierPublic.EncryptHash(message);
    }

    private string Dummy(string a)
    {
        return "nie wiadomo jeszcze co";
    }
}
