using ChainCore;

public class ChainEngine : ChainEngineBase<DataRecord, (int, string[]), RecordProcessor, ITransport>
{
    private readonly List<PrePrintAuditData> _auditList = new();
    private readonly object _auditListLock = new();
    private DateTime _lastAuditFlushTime = DateTime.Now;
    private readonly PrePrintAuditService _auditService;

    private const int _auditBatchSize = 1000;
    private const int _auditTimeoutSeconds = 30;

    public ChainEngine(int serverId, int totalServers, int myPort, RecordProcessor processor)
        : base(serverId, totalServers, myPort, processor)
    {
        _auditService = new PrePrintAuditService(serverId);
    }

    public override void SetTransport(ITransport transport)
    {
        _transport = transport;

        // when transport is set start monitoring tasks
        Task.Run(MonitorTimeout);
        Task.Run(PrintStatsLoop);
        if (_isLastServer)
        {
            Task.Run(MonitorAuditTimeout);
        }
    }

    public async Task InitializeData(int ballotNumber)
    {
        if (_serverId != 1)
        {
            Console.WriteLine($"Initialize only available on Server 1");
            return;
        }

        Console.WriteLine($"Initializing Queue 1 with {ballotNumber} records...");
        for (int i = 1; i <= ballotNumber; i++)
        {
            _queue1.Add(new DataRecord { BallotId = i });
            if (i % 100 == 0 || i == 1)
            {
                Console.WriteLine($"Added {i}/{ballotNumber} records");
            }
        }
    }

    protected override void CheckAndStartProcessing()
    {
        lock (_processingLock)
        {
            bool q2HasEnoughRecords = _queue2.Count >= _batchSize;
            bool q1HasEnoughRecords = _queue1.Count >= _batchSize;

            bool TimeoutExpired = DateTime.Now.Subtract(_lastProcessingTime).TotalSeconds >= _timeoutSeconds;

            // queue 2 has priority
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

    protected override void OnDataCompleted(DataRecord data, string voteSerial)
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
}