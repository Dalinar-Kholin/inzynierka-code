using System.Collections.Concurrent;
using System.Text.Json;

public class AuthSerialProcessor
{
    private readonly int _serverId;
    private readonly ConcurrentQueue<string> _authSerialQueue = new();
    private readonly HttpClient _httpClient = new();
    private readonly ChainServiceImpl _chainService;
    private readonly SemaphoreSlim _signal = new(0);

    public AuthSerialProcessor(int serverId, ChainServiceImpl chainService)
    {
        _serverId = serverId;
        _chainService = chainService;

        Task.Run(() => ProcessQueueAsync());
    }

    public void EnqueueAuthSerial(string authSerial)
    {
        _authSerialQueue.Enqueue(authSerial);
        _signal.Release();
        Console.WriteLine($"[Server {_serverId}] AuthSerial queued: {authSerial} (Queue size: {_authSerialQueue.Count})");
    }

    private async Task ProcessQueueAsync()
    {
        Console.WriteLine($"[Server {_serverId}] AuthSerial processor started");

        while (true)
        {
            try
            {
                await _signal.WaitAsync();

                if (_authSerialQueue.TryDequeue(out string? authSerial))
                {
                    await ProcessAuthSerialAsync(authSerial);
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

        Console.WriteLine($"[Server {_serverId}] AuthSerial processor stopped");
    }

    private async Task ProcessAuthSerialAsync(string authSerial)
    {
        try
        {
            Console.WriteLine($"[Server {_serverId}] Processing authSerial: {authSerial}");

            string externalServerUrl = $"http://127.0.0.1:8085/voteModel?authCode={authSerial}";

            var response = await _httpClient.GetAsync(externalServerUrl);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[Server {_serverId}] Failed to query external server for {authSerial}: {response.StatusCode}");
                return;
            }

            var voteData = await response.Content.ReadFromJsonAsync<VoteDataResponse>();

            if (voteData == null || string.IsNullOrEmpty(voteData.VoteSerial) || string.IsNullOrEmpty(voteData.VoteCode))
            {
                Console.WriteLine($"[Server {_serverId}] Invalid response from external server for {authSerial}");
                return;
            }

            Console.WriteLine($"[Server {_serverId}] Retrieved for {authSerial} -> voteSerial: {voteData.VoteSerial}, voteCode: {voteData.VoteCode}");

            // send to processing chain
            _chainService.SendData(voteData.VoteSerial, voteData.VoteCode);

            Console.WriteLine($"[Server {_serverId}] Data sent to chain for authSerial: {authSerial}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Server {_serverId}] Error processing authSerial {authSerial}: {ex.Message}");
        }
    }
    
    public int GetQueueSize()
    {
        return _authSerialQueue.Count;
    }
}

// response model from external server
record VoteDataResponse(string VoteSerial, string VoteCode);
