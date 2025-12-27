using Grpc.Core;
using Grpc.Net.Client;
using GrpcChain;
using System.Text.Json;

public class ChainServiceImpl : ChainService.ChainServiceBase, IExtendedTransport
{
    private readonly int _myPort;

    private readonly string _nextServerAddress;
    private readonly string _prevServerAddress;

    private GrpcChannel? _nextChannel;
    private ChainService.ChainServiceClient? _nextClient;
    private AsyncClientStreamingCall<MessageRequest, MessageReply>? _nextStream;

    private GrpcChannel? _prevChannel;
    private ChainService.ChainServiceClient? _prevClient;
    private AsyncClientStreamingCall<MessageRequest, MessageReply>? _prevStream;

    private readonly SemaphoreSlim _streamWriteLock = new SemaphoreSlim(1, 1);
    private readonly SemaphoreSlim _returningStreamWriteLock = new SemaphoreSlim(1, 1);

    private readonly ChainEngine _chainEngine;


    public ChainServiceImpl(string nextServer, string prevServer, int myPort, ChainEngine chainEngine)
    {
        _myPort = myPort;
        _nextServerAddress = nextServer;
        _prevServerAddress = prevServer;

        _chainEngine = chainEngine;

        Console.WriteLine($"Starting on port {_myPort}...");

        Task.Run(ConnectToNextNode);
        Task.Run(ConnectToPrevNode);
    }

    // streaming RPC - odbiera od poprzedniego
    public override async Task<MessageReply> StreamMessages(IAsyncStreamReader<MessageRequest> requestStream, ServerCallContext context)
    {
        Console.WriteLine($"[{_myPort}] Forward stream connected from {context.Peer}");

        try
        {
            await foreach (var message in requestStream.ReadAllAsync())
            {
                if (message.Text == "__PING__")
                {
                    Console.WriteLine($"[{_myPort}] Received PING (forward)");
                    continue;
                }

                var record = DeserializeRecord(message.Text);

                _chainEngine.OnRecordReceived(record, message.IsSecondPass);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{_myPort}] Forward stream error: {ex.Message}");
            return new MessageReply { Response = $"Error: {ex.Message}" };
        }
        finally
        {
            Console.WriteLine($"[{_myPort}] Forward stream closed");
        }

        return new MessageReply { Response = "All messages received" };
    }

    // streaming RPC - odbiera od nastepnego
    public override async Task<MessageReply> StreamReturningMessages(IAsyncStreamReader<MessageRequest> requestStream, ServerCallContext context)
    {
        Console.WriteLine($"[{_myPort}] Returning stream connected from {context.Peer}");

        try
        {
            await foreach (var message in requestStream.ReadAllAsync())
            {
                if (message.Text == "__PING__")
                {
                    Console.WriteLine($"[{_myPort}] Received PING (returning)");
                    continue;
                }

                var record = DeserializeVoteRecord(message.Text);

                _chainEngine.OnReturningRecordReceived(record, message.IsSecondPass);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{_myPort}] Returning stream error: {ex.Message}");
            return new MessageReply { Response = $"Error: {ex.Message}" };
        }
        finally
        {
            Console.WriteLine($"[{_myPort}] Returning stream closed");
        }

        return new MessageReply { Response = "All returning messages received" };
    }


    private async Task ConnectToNextNode()
    {
        int initialDelay = 2000;
        Console.WriteLine($"Waiting {initialDelay}ms for all servers to start...");
        await Task.Delay(initialDelay);

        int retryCount = 0;
        const int maxRetries = 60;

        while (retryCount < maxRetries)
        {
            try
            {
                if (retryCount > 0)
                {
                    Console.WriteLine($"Connecting to {_nextServerAddress} (attempt {retryCount + 1})...");
                }

                var testChannel = GrpcChannel.ForAddress(_nextServerAddress);
                var testClient = new ChainService.ChainServiceClient(testChannel);

                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(3));
                var testStream = testClient.StreamMessages(cancellationToken: cts.Token);

                await testStream.RequestStream.WriteAsync(new MessageRequest { Text = "__PING__" }, cts.Token);

                _nextChannel = testChannel;
                _nextClient = testClient;
                _nextStream = testStream;

                Console.WriteLine($"Successfully connected to {_nextServerAddress}");
                return;
            }
            catch (Exception ex)
            {
                _nextChannel?.Dispose();
                _nextChannel = null;
                _nextClient = null;
                _nextStream = null;

                retryCount++;
                if (retryCount >= maxRetries)
                {
                    Console.WriteLine($"Connection failed after {maxRetries} attempts");
                    return;
                }

                await Task.Delay(1000);
            }
        }
    }

    private async Task ConnectToPrevNode()
    {
        int initialDelay = 2000;
        Console.WriteLine($"Waiting {initialDelay}ms for all servers to start...");
        await Task.Delay(initialDelay);

        int retryCount = 0;
        const int maxRetries = 60;

        while (retryCount < maxRetries)
        {
            try
            {
                if (retryCount > 0)
                {
                    Console.WriteLine($"Connecting to {_prevServerAddress} (attempt {retryCount + 1})...");
                }

                var testChannel = GrpcChannel.ForAddress(_prevServerAddress);
                var testClient = new ChainService.ChainServiceClient(testChannel);

                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(3));
                var testStream = testClient.StreamReturningMessages(cancellationToken: cts.Token);

                await testStream.RequestStream.WriteAsync(new MessageRequest { Text = "__PING__" }, cts.Token);

                _prevChannel = testChannel;
                _prevClient = testClient;
                _prevStream = testStream;

                Console.WriteLine($"Successfully connected to previous server {_prevServerAddress}");
                return;
            }
            catch (Exception ex)
            {
                _prevChannel?.Dispose();
                _prevChannel = null;
                _prevClient = null;
                _prevStream = null;

                retryCount++;
                if (retryCount >= maxRetries)
                {
                    Console.WriteLine($"Connection to prev server failed after {maxRetries} attempts");
                    return;
                }

                await Task.Delay(1000);
            }
        }
    }

    public async Task SendRecordAsync(string record, bool isSecondPass)
    {
        if (_nextStream == null)
        {
            throw new InvalidOperationException("Not connected to next server.");
        }

        await _streamWriteLock.WaitAsync();
        try
        {
            await _nextStream.RequestStream.WriteAsync(new MessageRequest
            {
                Text = record,
                IsSecondPass = isSecondPass
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{_myPort}] Error sending record to next server: {ex.Message}");
            throw;
        }
        finally
        {
            _streamWriteLock.Release();
        }
    }

    public async Task SendReturningRecordAsync(string record, bool isSecondPass)
    {
        if (_prevStream == null)
        {
            throw new InvalidOperationException("Not connected to previous server.");
        }

        await _returningStreamWriteLock.WaitAsync();
        try
        {
            await _prevStream.RequestStream.WriteAsync(new MessageRequest
            {
                Text = record,
                IsSecondPass = isSecondPass
            });
        }
        finally
        {
            _returningStreamWriteLock.Release();
        }
    }

    public void SendData(string voteSerial, string voteCode)
    {
        // narazie tak potem trzeba z bazy wziac odpowiednie
        var record = new VoteCodeRecord
        {
            BallotId = 1,
            EncryptedVoteCode = voteCode.Select(c => c.ToString()).ToList(),
            VoteVector = new List<string> { "1", "0", "1", "0", "1" } // placeholder
        };

        _chainEngine.OnReturningRecordReceived(record, isSecondPass: false);
    }

    private static VoteCodeRecord DeserializeVoteRecord(string text)
    {
        try
        {
            var rec = JsonSerializer.Deserialize<VoteCodeRecord>(text);
            if (rec != null) return rec;
        }
        catch
        {
            // fallback below
        }

        if (int.TryParse(text, out var ballotId))
        {
            return new VoteCodeRecord { BallotId = ballotId };
        }

        return new VoteCodeRecord { BallotId = 0 };
    }

    private static VoteRecord DeserializeRecord(string text)
    {
        try
        {
            var rec = JsonSerializer.Deserialize<VoteRecord>(text);
            if (rec != null) return rec;
        }
        catch
        {
            // fallback
        }

        return new VoteRecord { BallotId = 0 };
    }
}