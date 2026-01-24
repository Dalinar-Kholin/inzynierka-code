using System.Collections.Concurrent;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using static MerkleTreeBuilder;

public class AuthCodeProcessor
{
    private readonly int _serverId;
    private readonly ConcurrentQueue<string> _authCodeQueue = new();
    private readonly HttpClient _httpClient = new();
    private readonly ChainEngine _chainEngine;
    private readonly VoteService _voteService = new();
    const int BatchSize = 1000;
    private readonly ConcurrentQueue<VoteData> _voteDataBatchQueue = new();

    private readonly SemaphoreSlim _signal = new(0);

    private readonly ConcurrentDictionary<string, string> _voteSerialToAuthCode = new();

    public AuthCodeProcessor(int serverId, ChainEngine chainEngine)
    {
        _serverId = serverId;
        _chainEngine = chainEngine;

        Task.Run(() => ProcessQueueAsync());
        Task.Run(async () =>
        {
            while (true)
            {
                await Task.Delay(10000);

                var batchToSave = new List<VoteData>();

                while (batchToSave.Count < BatchSize && _voteDataBatchQueue.TryDequeue(out VoteData? voteData))
                {
                    batchToSave.Add(voteData);
                }

                if (batchToSave.Count > 0)
                {
                    try
                    {
                        await _voteService.CreateVotesBatch(batchToSave);
                        Console.WriteLine($"[Server {_serverId}] Saved batch of {batchToSave.Count} VoteData to database");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Server {_serverId}] Error saving VoteData batch to database: {ex.Message}");
                    }
                }
            }
        });
    }

    public void EnqueueAuthCode(string authCode)
    {
        _authCodeQueue.Enqueue(authCode);
        _signal.Release();
        Console.WriteLine($"[Server {_serverId}] AuthCode queued: {authCode} (Queue size: {_authCodeQueue.Count})");
    }

    private async Task ProcessQueueAsync()
    {
        Console.WriteLine($"[Server {_serverId}] AuthCode processor started");

        while (true)
        {
            try
            {
                await _signal.WaitAsync();

                if (_authCodeQueue.TryDequeue(out string? authCode))
                {
                    await ProcessAuthCodeAsync(authCode);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Server {_serverId}] Error in queue processor: {ex.Message}");
            }
        }

        Console.WriteLine($"[Server {_serverId}] AuthCode processor stopped");
    }

    private async Task ProcessAuthCodeAsync(string authCode)
    {
        try
        {
            Console.WriteLine($"[Server {_serverId}] Processing authCode: {authCode}");
            string externalServerUrl = $"http://127.0.0.1:8085/voteModel?authCode={authCode}";

            var response = await _httpClient.GetAsync(externalServerUrl);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[Server {_serverId}] Failed to query external server for {authCode}: {response.StatusCode}");
                return;
            }

            var voteData = await response.Content.ReadFromJsonAsync<VoteDataResponse>();

            var voteCode = Encoding.UTF8.GetString(voteData?.VoteCode.ToArray() ?? []);
            var voteSerial = Encoding.UTF8.GetString(voteData?.VoteSerial.ToArray() ?? []);
            
            if (voteData == null || string.IsNullOrEmpty(voteSerial) || string.IsNullOrEmpty(voteCode))
            {
                Console.WriteLine($"[Server {_serverId}] Invalid response from external server for {authCode}");
                return;
            }

            Console.WriteLine($"[Server {_serverId}] Retrieved for {authCode} -> voteSerial: {voteData.VoteSerial}, voteCode: {voteData.VoteCode}");

            // send to processing chain
            _chainEngine.OnNewVoteReceived(voteSerial, voteCode);
            _voteSerialToAuthCode[voteSerial] = authCode;

            Console.WriteLine($"[Server {_serverId}] Data sent to chain for authCode: {authCode}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Server {_serverId}] Error processing authCode {authCode}: {ex.Message}");
        }
    }

    public async Task NotifyVoteTallied(string voteSerial, List<string> voteVectors)
    {
        // Hzoh5Sxx3m
        // HeYy7XwzAd HlYyGXTzAd rlY67KTDAV HeYy7XwDSV reYyGXTDAV

        //send Hzoh5Sxx3m rlY67KTDAV
        //send lSDKTgJlAw rCjVn1yJEG
        //send AEFEcAnFsw UZmVm6iifd

        // lSDKTgJlAw
        // rCjVL1yu4G r2y8LJyu4l rCjVn1yJEG qCy8nJnu4G q2j8n1nJEl

        // AEFEcAnFsw
        // lZmVmYiEFd lZmem6iifd UZjVuYiifQ UZmemYlEfQ UZmVm6iifd


        if (_voteSerialToAuthCode.TryGetValue(voteSerial, out string? authCode))
        {
            // zmiana: na BB wysyłamy korzeń wektora głosów
            // wektor bedzie trzymany w publicznej bazie danych
            // _id, authCode, VS, voteVector

            // w wersji prostej dostajemy authCode do zliczania.
            // zliczanie to dostanie authCoda, pobranie z bazy publicznej danych, 
            // zrobienie z nich merkla i porownanie z rootem na BB
            // gdy sie zgadza to licz głos itd.

            // w wersji prawidłowej
            // przeszukujemy BB, sprawdzamy czy dany glos liczyc czy nie
            // jeżeli tak to pobieramy z BB: authCode, VS, comm(voteVector)
            // na podstawie authCode pobieramy prawdziwy voteVector z bazy publicznej danych,
            // zrobienie z nich merkla i porownanie z rootem z BB
            // gdy sie zgadza to licz głos itd.

            Console.WriteLine($"[Server {_serverId}] Vote tallied for voteSerial: {voteSerial}, authCode: {authCode}, voteVectors: {string.Join(", ", voteVectors)}");
            // merkle tree root from voteVector
            var leaves = new List<string>(voteVectors);
            var rootHash = MerkleTreeBuilder.BuildMerkleRootFromLeaves(leaves);
            Console.WriteLine($"[Server {_serverId}] VoteVector root hash: {rootHash}");

            // _id, authCode, VS, voteVector do publciznej bazy danych
            var voteData = new VoteData
            {
                AuthCode = authCode,
                VoteSerial = voteSerial,
                VoteVector = voteVectors
            };
            // enqueue to batch queue
            _voteDataBatchQueue.Enqueue(voteData);

            // a na BB wysyłamy authCode i root(voteVector)
            string externalServerUrl = "http://127.0.0.1:8080/updateVoteVector";

            var payload = new { authCode = authCode, voteVector = rootHash };
            await _httpClient.PostAsJsonAsync(externalServerUrl, payload);
        }
        else
        {
            Console.WriteLine($"[Server {_serverId}] Vote tallied for voteSerial: {voteSerial}, but authCode not found");
        }
    }

    public int GetQueueSize()
    {
        return _authCodeQueue.Count;
    }
}

public class VoteDataResponse
{
    public List<byte> VoteSerial { get; set; } = default!;
    public List<byte> VoteCode   { get; set; } = default!;
}