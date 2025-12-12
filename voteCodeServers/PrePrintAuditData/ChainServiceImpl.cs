using Grpc.Core;
using Grpc.Net.Client;
using GrpcChain;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading;

public class ChainServiceImpl : ChainService.ChainServiceBase
{
    private readonly string _nextServerAddress;
    private readonly int _myPort;
    private readonly int _serverId;
    private readonly bool _isLastServer;
    private readonly int _totalServers;
    private readonly int _ballotNumber;

    private readonly BlockingCollection<DataRecord> _queue1 = new();
    private readonly BlockingCollection<DataRecord> _queue2 = new();
    private readonly List<PrePrintAuditData> _auditList = new();
    private readonly object _auditListLock = new();
    private readonly SemaphoreSlim _streamWriteLock = new SemaphoreSlim(1, 1);

    private List<int> _shadowPermutation;
    private List<int> _shadowPrimPermutation;

    private GrpcChannel? _nextChannel;
    private ChainService.ChainServiceClient? _nextClient;
    private AsyncClientStreamingCall<MessageRequest, MessageReply>? _nextStream;

    private bool _isProcessing = false;
    private DateTime _lastProcessingTime = DateTime.Now;
    private DateTime _lastAuditFlushTime = DateTime.Now;
    private readonly object _processingLock = new();

    private readonly DataProcessingService _processor;
    private readonly BallotLinkingService _ballotLinkingService;
    private readonly PrePrintAuditService _auditService;

    private long _processedQ1 = 0;
    private long _processedQ2 = 0;

    private const int _batchSize = 1000;
    private const int _timeoutSeconds = 1;
    private const int _auditBatchSize = 1000;
    private const int _auditTimeoutSeconds = 30;

    public ChainServiceImpl(int serverId, int totalServers, string nextServer, int myPort, int ballotNumber)
    {
        _serverId = serverId;
        _isLastServer = (serverId == totalServers);
        _totalServers = totalServers;
        _ballotNumber = ballotNumber;

        _nextServerAddress = nextServer;
        _myPort = myPort;
        _processor = new DataProcessingService(_serverId, _totalServers);
        _auditService = new PrePrintAuditService(serverId);
        _ballotLinkingService = new BallotLinkingService(serverId);

        _shadowPermutation = _ballotLinkingService.GetPermutationListAsync(false).Result;
        _shadowPrimPermutation = _ballotLinkingService.GetPermutationListAsync(true).Result;

        Console.WriteLine($"Starting on port {_myPort}...");

        Task.Run(MonitorTimeout);
        Task.Run(ConnectToNextNode);
        Task.Run(PrintStatsLoop);
        if (_isLastServer) Task.Run(MonitorAuditTimeout);
    }

    // streaming RPC
    public override async Task<MessageReply> StreamMessages(IAsyncStreamReader<MessageRequest> requestStream, ServerCallContext context)
    {
        Console.WriteLine($"[{_myPort}] Stream connected from {context.Peer}");

        try
        {
            int count1 = 0, count2 = 0;
            await foreach (var message in requestStream.ReadAllAsync())
            {
                if (message.Text == "__PING__")
                {
                    Console.WriteLine($"[{_myPort}] Received PING");
                    continue;
                }

                var record = DeserializeRecord(message.Text);

                if (message.IsSecondPass)
                {
                    _queue2.Add(record);
                    count2++;
                    Console.WriteLine($"[{_myPort}] Queue 2 - Added: {record.BallotId}");
                }
                else
                {
                    _queue1.Add(record);
                    count1++;
                    Console.WriteLine($"[{_myPort}] Queue 1 - Added: {record.BallotId}");
                }

                CheckAndStartProcessing();
            }
            Console.WriteLine($"[{_myPort}] Stream finished. Q1: {count1}, Q2: {count2}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{_myPort}] Stream error: {ex.Message}");
            return new MessageReply { Response = $"Error: {ex.Message}" };
        }
        finally
        {
            Console.WriteLine($"[{_myPort}] Stream closed");
        }

        // wyjebac odpowiedzi chyba
        return new MessageReply { Response = "All messages received" };
    }

    private void MonitorAuditTimeout()
    {
        while (true)
        {
            try
            {
                Task.Delay(1000).Wait();
                CheckAndProcessAuditQueue();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{_myPort}] Audit monitor error: {ex.Message}");
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
                    if (_queue1.Count > 0 || _queue2.Count > 0)
                        CheckAndStartProcessing();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{_myPort}] Monitor timeout error: {ex.Message}");
            }
        }
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

    private async Task PrintStatsLoop()
    {
        while (true)
        {
            try
            {
                await Task.Delay(2000);
                var q1 = Interlocked.Read(ref _processedQ1);
                var q2 = Interlocked.Read(ref _processedQ2);
                var q1Pending = _queue1.Count;
                var q2Pending = _queue2.Count;

                // update tmux pane
                var title = $"S{_serverId} Q1={q1}({q1Pending}) Q2={q2}({q2Pending})";
                Console.Write($"\u001b]2;{title}\u0007");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{_myPort}] Stats loop error: {ex.Message}");
            }
        }
    }

    private void CheckAndStartProcessing()
    {
        lock (_processingLock)
        {
            bool q2HasEnoughRecords = _queue2.Count >= _batchSize;
            bool q1HasEnoughRecords = _queue1.Count >= _batchSize;

            bool TimeoutExpired = DateTime.Now.Subtract(_lastProcessingTime).TotalSeconds >= _timeoutSeconds;

            // Queue 2 has priority
            if (q2HasEnoughRecords && !_isProcessing)
            {
                _isProcessing = true;
                Task.Run(() => ProcessQueue(2));
            }
            else if (q1HasEnoughRecords && !_isProcessing)
            {
                _isProcessing = true;
                Task.Run(() => ProcessQueue(1));
            }

            else if (TimeoutExpired && !_isProcessing)
            {
                if (_queue2.Count >= _queue1.Count && _queue2.Count > 0)
                {
                    _isProcessing = true;
                    Task.Run(() => ProcessQueue(2));
                }
                else if (_queue1.Count > 0)
                {
                    _isProcessing = true;
                    Task.Run(() => ProcessQueue(1));
                }
            }
        }
    }

    public async Task InitializeData()
    {
        if (_serverId != 1)
        {
            Console.WriteLine($"Initialize only available on Server 1");
            return;
        }

        Console.WriteLine($"Initializing Queue 1 with {_ballotNumber} records...");
        for (int i = 1; i <= _ballotNumber; i++)
        {
            _queue1.Add(new DataRecord { BallotId = i });
            if (i % 100 == 0 || i == 1)
            {
                Console.WriteLine($"Added {i}/{_ballotNumber} records");
            }
        }
    }

    private async Task ProcessQueue(int queueNumber)
    {
        var batch = new List<DataRecord>();
        var queue = queueNumber == 2 ? _queue2 : _queue1;

        for (int i = 0; i < _batchSize && queue.Count > 0; i++)
        {
            if (queue.TryTake(out var record, 100))
            {
                batch.Add(record);
            }
        }

        Console.WriteLine($"[{_myPort}] Processing Queue {queueNumber} of {batch.Count} records");

        if (queueNumber == 1)
        {
            await ProcessQueueFirstPass(batch);
        }
        else
        {
            await ProcessQueueSecondPass(batch);
        }

        lock (_processingLock)
        {
            _isProcessing = false;
            _lastProcessingTime = DateTime.Now;
            Console.WriteLine($"[{_myPort}] [Q{queueNumber}] Finished. Q1={_queue1.Count}, Q2={_queue2.Count}");
        }

        CheckAndStartProcessing();
    }

    private async Task ProcessQueueFirstPass(List<DataRecord> batch)
    {
        var ids = new List<int>(batch.Count);

        // applying shadow permutation
        if (_serverId != 1)
        {
            foreach (var record in batch)
            {
                record.BallotId = _shadowPermutation[record.BallotId - 1];
                ids.Add(record.BallotId);
            }
        }
        else
        {
            foreach (var record in batch)
            {
                ids.Add(record.BallotId);
            }
        }

        // fetching data from DB
        var firstPass = await _processor.ProcessBatchFirstPassAsync(ids);

        // processing per-record in parallel
        var tasks = new List<Task>();
        for (int i = 0; i < batch.Count; i++)
        {
            var index = i;
            var task = Task.Run(async () =>
            {
                try
                {
                    var record = batch[index];
                    var processedRecord = _processor.ProcessSingleFirstPass(record, firstPass[record.BallotId]);
                    Interlocked.Increment(ref _processedQ1);

                    var payload = SerializeRecord(processedRecord);
                    if (_nextStream != null)
                    {
                        await _streamWriteLock.WaitAsync();
                        try
                        {
                            await _nextStream.RequestStream.WriteAsync(
                                new MessageRequest { Text = payload, IsSecondPass = _isLastServer });
                            Console.WriteLine($"[{_myPort}] [Q1] Forwarded: {processedRecord.BallotId}");
                        }
                        finally
                        {
                            _streamWriteLock.Release();
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[{_myPort}] [Q1] CHAIN DISCONNECTED");
                    }

                    // saving commitments - chyba bo w printegrity nic nie ma o commitmentach
                    // await _processor.PersistRecordAsync(processedRecord);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{_myPort}] [Q1] Processing error: {ex.Message}");
                }
            });
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
    }

    private async Task ProcessQueueSecondPass(List<DataRecord> batch)
    {
        var ids = new List<int>(batch.Count);

        // applying shadow prim permutation
        foreach (var record in batch)
        {
            record.BallotId = _shadowPrimPermutation[record.BallotId - 1];
            ids.Add(record.BallotId);
        }

        // zoraganizowac lepiej chyba sie by dalo

        // fetching data from DB
        Dictionary<int, string?> voteSerials = null;
        Dictionary<int, int?> secondPass = null;

        if (_isLastServer)
        {
            voteSerials = await _processor.ProcessBatchSecondPassLastServerAsync(ids);
        }
        else
        {
            secondPass = await _processor.ProcessBatchSecondPassAsync(ids);
        }

        // processing per-record in parallel (mirrors first pass)
        var tasks = new List<Task>();
        for (int i = 0; i < batch.Count; i++)
        {
            var index = i;
            var task = Task.Run(async () =>
            {
                try
                {
                    var record = batch[index];
                    var secondPassData = secondPass != null && secondPass.ContainsKey(record.BallotId)
                        ? secondPass[record.BallotId]
                        : null;
                    var voteSerialData = voteSerials != null && voteSerials.ContainsKey(record.BallotId)
                        ? voteSerials[record.BallotId]
                        : null;

                    var processedRecord = _processor.ProcessSingleSecondPass(record, secondPassData);
                    Interlocked.Increment(ref _processedQ2);

                    if (_isLastServer)
                    {
                        OnDataCompleted(processedRecord, voteSerialData);
                    }
                    else if (_nextStream != null)
                    {
                        var payload = SerializeRecord(processedRecord);
                        await _streamWriteLock.WaitAsync();
                        try
                        {
                            await _nextStream.RequestStream.WriteAsync(
                                new MessageRequest { Text = payload, IsSecondPass = true });
                            Console.WriteLine($"[{_myPort}] [Q2] Forwarded: {processedRecord.BallotId}");
                        }
                        finally
                        {
                            _streamWriteLock.Release();
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[{_myPort}] [Q2] CHAIN DISCONNECTED");
                    }

                    // saving commitments - chyba
                    // await _processor.PersistRecordAsync(processedRecord);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{_myPort}] [Q2] Processing error: {ex.Message}");
                }
            });
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
    }

    private void CheckAndProcessAuditQueue()
    {
        lock (_auditListLock)
        {
            bool hasEnoughRecords = _auditList.Count >= _auditBatchSize;
            bool timeoutExpired = DateTime.Now.Subtract(_lastAuditFlushTime).TotalSeconds >= _auditTimeoutSeconds
                                  && _auditList.Count > 0;

            if (hasEnoughRecords || timeoutExpired)
            {
                Console.WriteLine($"Processing audit list batch of {_auditList.Count} records");
                Task.Run(() => ProcessAuditQueueBatch());
            }
        }
    }

    private async Task ProcessAuditQueueBatch()
    {
        List<PrePrintAuditData> batch;

        lock (_auditListLock)
        {
            batch = new List<PrePrintAuditData>(_auditList);
            _auditList.Clear();
            _lastAuditFlushTime = DateTime.Now;
        }

        if (batch.Count > 0)
        {
            try
            {
                Console.WriteLine($"Creating PrePrintAudit batch of {batch.Count} records");

                await _auditService.CreatePrePrintAuditBatch(batch);
                Console.WriteLine($"Audit batch saved successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing audit batch: {ex.Message}");
            }
        }
    }

    private void OnDataCompleted(DataRecord data, string voteSerial)
    {
        lock (_auditListLock)
        {
            var auditData = new PrePrintAuditData
            {
                BallotVoteSerial = voteSerial,
                Vectors = data.Vectors
            };
            _auditList.Add(auditData);
            Console.WriteLine($"Added to audit list. Count: {_auditList.Count}");
        }

        CheckAndProcessAuditQueue();
    }

    private static string SerializeRecord(DataRecord record)
    {
        return JsonSerializer.Serialize(record);
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
            // fallback below
        }

        if (int.TryParse(text, out var ballotId))
        {
            return new DataRecord { BallotId = ballotId };
        }

        return new DataRecord { BallotId = 0 };
    }
}