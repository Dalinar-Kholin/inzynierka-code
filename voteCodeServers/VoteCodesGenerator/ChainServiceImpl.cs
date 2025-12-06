using Grpc.Core;
using Grpc.Net.Client;
using GrpcChain;
using System.Collections.Concurrent;

public class ChainServiceImpl : ChainService.ChainServiceBase
{
    private readonly string? _nextServerAddress;
    private readonly int _myPort;
    private readonly BlockingCollection<string> _messageQueue = new();
    private GrpcChannel? _nextChannel;
    private ChainService.ChainServiceClient? _nextClient;
    private AsyncClientStreamingCall<MessageRequest, MessageReply>? _nextStream;

    private bool _isProcessing = false;
    private readonly object _processingLock = new();
    private DateTime _lastProcessingTime = DateTime.Now;
    private const int _batchSize = 1000;
    private const int _timeoutSeconds = 5;

    public ChainServiceImpl(string? nextServer, int myPort)
    {
        _nextServerAddress = nextServer;
        _myPort = myPort;

        Console.WriteLine($"[NODE {_myPort}] Starting...");
        Console.WriteLine($"[NODE {_myPort}] Batch size: {_batchSize}");
        Console.WriteLine($"[NODE {_myPort}] Next server: {nextServer ?? "NONE (last node)"}");

        // sprawdzanie timeoutu
        Task.Run(MonitorTimeout);

        if (!string.IsNullOrEmpty(_nextServerAddress))
        {
            Task.Run(ConnectToNextNode);
        }
    }

    // stream od poprzedniego węzła
    public override async Task<MessageReply> StreamMessages(IAsyncStreamReader<MessageRequest> requestStream, ServerCallContext context)
    {
        Console.WriteLine($"[NODE {_myPort}] Stream connected from {context.Peer}");

        try
        {
            int count = 0;
            await foreach (var message in requestStream.ReadAllAsync())
            {
                Console.WriteLine($"[NODE {_myPort}] Received: {message.Text}");

                _messageQueue.Add(message.Text);
                count++;

                CheckAndStartProcessing();
            }
            Console.WriteLine($"[NODE {_myPort}] Stream finished. Received {count} messages");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NODE {_myPort}] Stream error: {ex.Message}");
            return new MessageReply { Response = $"Error: {ex.Message}" };
        }
        finally
        {
            Console.WriteLine($"[NODE {_myPort}] Stream closed");
        }

        return new MessageReply { Response = "All messages received" };
    }


    private void CheckAndStartProcessing()
    {
        lock (_processingLock)
        {
            bool hasEnoughRecords = _messageQueue.Count >= _batchSize;
            bool timeoutExpired = DateTime.Now.Subtract(_lastProcessingTime).TotalSeconds >= _timeoutSeconds && _messageQueue.Count > 0;

            if ((hasEnoughRecords || timeoutExpired) && !_isProcessing)
            {
                _isProcessing = true;
                Task.Run(ProcessQueue);
            }
        }
    }

    private void MonitorTimeout()
    {
        while (true)
        {
            try
            {
                Task.Delay(1000).Wait();

                lock (_processingLock)
                {
                    // sprawdzanie timeoutu
                    if (!_isProcessing && _messageQueue.Count > 0)
                    {
                        if (DateTime.Now.Subtract(_lastProcessingTime).TotalSeconds >= _timeoutSeconds)
                        {
                            Console.WriteLine($"[NODE {_myPort}] Timeout triggered. Processing {_messageQueue.Count} records");
                            _isProcessing = true;
                            _lastProcessingTime = DateTime.Now;
                            Task.Run(ProcessQueue);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NODE {_myPort}] Monitor timeout error: {ex.Message}");
            }
        }
    }

    private async Task ProcessQueue()
    {
        var batch = new List<string>();

        // pobierz dokładnie _batchSize rekordów lub mniej
        for (int i = 0; i < _batchSize && _messageQueue.Count > 0; i++)
        {
            if (_messageQueue.TryTake(out var message, 100))
            {
                batch.Add(message);
            }
        }

        Console.WriteLine($"[NODE {_myPort}] Processing batch of {batch.Count} records");

        // przetwórz batch
        foreach (var message in batch)
        {
            try
            {
                var processed = ProcessSingleRecord(message);
                Console.WriteLine($"[NODE {_myPort}] Processed: {message} -> {processed}");

                if (_nextStream != null)
                {
                    await _nextStream.RequestStream.WriteAsync(
                        new MessageRequest { Text = processed });
                    Console.WriteLine($"[NODE {_myPort}] Forwarded: {processed}");
                }
                else
                {
                    Console.WriteLine($"[NODE {_myPort}] END OF CHAIN: {processed}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NODE {_myPort}] Processing error: {ex.Message}");
            }
        }

        lock (_processingLock)
        {
            _isProcessing = false;
            _lastProcessingTime = DateTime.Now;
            Console.WriteLine($"[NODE {_myPort}] Batch processing finished. Queue size now: {_messageQueue.Count}");
        }

        // sprawdzenie czy już można przetwarzać następny batch
        if (_messageQueue.Count >= _batchSize)
        {
            CheckAndStartProcessing();
        }
    }

    private string ProcessSingleRecord(string record)
    {
        return record + "X";
    }

    private async Task ConnectToNextNode()
    {
        try
        {
            Console.WriteLine($"[NODE {_myPort}] Connecting to next node: {_nextServerAddress}");

            _nextChannel = GrpcChannel.ForAddress(_nextServerAddress!);
            _nextClient = new ChainService.ChainServiceClient(_nextChannel);
            _nextStream = _nextClient.StreamMessages();

            Console.WriteLine($"[NODE {_myPort}] Connected to {_nextServerAddress}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NODE {_myPort}] Connection failed: {ex.Message}");
        }
    }

    public async Task SendMessage(string message)
    {
        if (_nextStream != null)
        {
            Console.WriteLine($"[NODE {_myPort}] Sending manually: {message}");
            await _nextStream.RequestStream.WriteAsync(new MessageRequest { Text = message });
        }
        else
        {
            _messageQueue.Add(message);
            Console.WriteLine($"[NODE {_myPort}] Added to queue: {message}");

            CheckAndStartProcessing();
        }
    }

    public async Task FinishAndGetResponse()
    {
        if (_nextStream != null)
        {
            try
            {
                await _nextStream.RequestStream.CompleteAsync();

                var response = await _nextStream.ResponseAsync;
                Console.WriteLine($"[NODE {_myPort}] Final response from next: {response.Response}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NODE {_myPort}] Error getting response: {ex.Message}");
            }
        }
    }

    public void PrintStatus()
    {
        Console.WriteLine($"[NODE {_myPort}] Queue size: {_messageQueue.Count}");
    }
}