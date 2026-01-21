using System.Collections.Concurrent;
using System.Text.Json;

public class AuthCodeProcessor
{
    private readonly int _serverId;
    private readonly ConcurrentQueue<string> _authCodeQueue = new();
    private readonly HttpClient _httpClient = new();
    private readonly ChainServiceImpl _chainService;
    private readonly SemaphoreSlim _signal = new(0);

    public AuthCodeProcessor(int serverId, ChainServiceImpl chainService)
    {
        _serverId = serverId;
        _chainService = chainService;

        Task.Run(() => ProcessQueueAsync());
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

            if (voteData == null || string.IsNullOrEmpty(voteData.VoteSerial) || string.IsNullOrEmpty(voteData.VoteCode))
            {
                Console.WriteLine($"[Server {_serverId}] Invalid response from external server for {authCode}");
                return;
            }

            Console.WriteLine($"[Server {_serverId}] Retrieved for {authCode} -> voteSerial: {voteData.VoteSerial}, voteCode: {voteData.VoteCode}");
            // send to processing chain
            _chainService.SendData(voteData.VoteSerial, voteData.VoteCode);

            Console.WriteLine($"[Server {_serverId}] Data sent to chain for authCode: {authCode}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Server {_serverId}] Error processing authCode {authCode}: {ex.Message}");
        }
    }

    public int GetQueueSize()
    {
        return _authCodeQueue.Count;
    }
}

// response model from external server
record VoteDataResponse(string VoteSerial, string VoteCode);
