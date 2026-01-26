using ChainCore;

public class ChainEngine : ChainEngineBase<DataRecord, (int, int, string), RecordProcessor, ITransport>
{
    // additional structures for VoteCodes processing
    private readonly object _voteCodesListLock = new();

    private DateTime _lastVoteCodesFlushTime = DateTime.Now;
    private readonly List<VoteCodesData> _voteCodesList = new();

    private readonly VoteCodesService _voteCodesService;

    private const int _voteCodesBatchSize = 1000;
    private const int _voteCodesTimeoutSeconds = 30;

    public ChainEngine(int serverId, int totalServers, int myPort, RecordProcessor processor)
        : base(serverId, totalServers, myPort, processor)
    {
        _voteCodesService = new VoteCodesService(serverId);
    }

    public override void SetTransport(ITransport transport)
    {
        _transport = transport;

        // when transport is set start monitoring tasks
        Task.Run(MonitorTimeout);
        Task.Run(PrintStatsLoop);
        if (_isLastServer)
        {
            Task.Run(MonitorVoteCodesTimeout);
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
        // add encrypted vote codes to list
        lock (_voteCodesListLock)
        {
            var encryptedVoteCodes = _processor.FinnalizeEncryptedVoteCodes(data.EncryptedVoteCodes, voteSerial);

            // skip if null
            if (encryptedVoteCodes == null)
            {
                Console.WriteLine($"Encrypted vote codes is null for vote serial {voteSerial}, skipping...");
                return;
            }

            var voteCodesData = new VoteCodesData
            {
                EncryptedVoteCodes = encryptedVoteCodes,
            };
            _voteCodesList.Add(voteCodesData);
            Console.WriteLine($"Added to vote codes list. Count: {_voteCodesList.Count}");
        }

        CheckAndProcessVoteCodesQueue();
    }

    private void CheckAndProcessVoteCodesQueue()
    {
        lock (_voteCodesListLock)
        {
            bool hasEnoughRecords = _voteCodesList.Count >= _voteCodesBatchSize;
            bool timeoutExpired = DateTime.Now.Subtract(_lastVoteCodesFlushTime).TotalSeconds >= _voteCodesTimeoutSeconds
                                  && _voteCodesList.Count > 0;

            if (hasEnoughRecords || timeoutExpired)
            {
                Console.WriteLine($"Processing vote codes list batch of {_voteCodesList.Count} records");
                Task.Run(() => ProcessVoteCodesQueueBatch());
            }
        }
    }

    private async Task ProcessVoteCodesQueueBatch()
    {
        List<VoteCodesData> batch;

        lock (_voteCodesListLock)
        {
            batch = new List<VoteCodesData>(_voteCodesList);
            _voteCodesList.Clear();
            _lastVoteCodesFlushTime = DateTime.Now;
        }

        if (batch.Count > 0)
        {
            try
            {
                Console.WriteLine($"Creating VoteCodes batch of {batch.Count} records");

                await _voteCodesService.CreateVoteCodesBatch(batch);
                Console.WriteLine($"VoteCodes batch saved successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing vote codes batch: {ex.Message}");
            }
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

    private void MonitorVoteCodesTimeout()
    {
        while (true)
        {
            try
            {
                Task.Delay(1000).Wait();
                CheckAndProcessVoteCodesQueue();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{_myPort}] VoteCodes monitor error: {ex.Message}");
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