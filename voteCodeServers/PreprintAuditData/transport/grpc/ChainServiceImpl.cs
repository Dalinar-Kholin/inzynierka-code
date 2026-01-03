using Grpc.Core;
using Grpc.Net.Client;
using GrpcChain;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading;
using ChainCore;

public class ChainServiceImpl : ChainService.ChainServiceBase, ITransport
{
    private readonly int _myPort;
    private readonly string _nextServerAddress;

    private GrpcChannel? _nextChannel;
    private ChainService.ChainServiceClient? _nextClient;
    private AsyncClientStreamingCall<MessageRequest, MessageReply>? _nextStream;

    private readonly SemaphoreSlim _streamWriteLock = new SemaphoreSlim(1, 1);

    private readonly ChainEngine _chainEngine;


    public ChainServiceImpl(string nextServer, int myPort, ChainEngine chainEngine)
    {
        _myPort = myPort;
        _nextServerAddress = nextServer;

        _chainEngine = chainEngine;

        Console.WriteLine($"Starting on port {_myPort}...");

        Task.Run(ConnectToNextNode);
    }

    public async Task SendRecordAsync(string message, bool isSecondPass)
    {
        if (_nextStream == null)
        {
            Console.WriteLine($"[{_myPort}] Next stream not connected. Cannot send message.");
            return;
        }

        await _streamWriteLock.WaitAsync();
        try
        {
            await _nextStream.RequestStream.WriteAsync(
                new MessageRequest { Text = message, IsSecondPass = isSecondPass });
        }
        finally
        {
            _streamWriteLock.Release();
        }
    }

    // streaming RPC - receive messages from previous node
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

    private static DataRecord DeserializeRecord(string text)
    {
        try
        {
            var rec = JsonSerializer.Deserialize<DataRecord>(text);
            if (rec != null) return rec;
        }
        catch
        {
            Console.WriteLine("Deserialization error");
        }

        return new DataRecord { BallotId = 0 };
    }
}