using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using ChainCore;

public class ChainEngine : ChainEngineBase<VoteRecord, int, RecordProcessor, IExtendedTransport>
{
    private readonly bool _isFirstServer;

    private readonly BlockingCollection<VoteCodeRecord> _returningQueue1 = new();
    private readonly BlockingCollection<VoteCodeRecord> _returningQueue2 = new();

    private List<int> _reversedShadowPermutation;
    private List<int> _reversedShadowPrimPermutation;

    private long _processedReturningQ1 = 0;
    private long _processedReturningQ2 = 0;

    private readonly BlockingCollection<(string, string)> _newVotesQueue = new();
    private DateTime _votingEndDate = DateTime.Now.AddMinutes(60); // ustawic na koniec glosowania

    private readonly int _newVotesBatchSize;
    private readonly int _newVotesTriggerSize;

    private AuthCodeProcessor _authCodeProcessor;

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Dodac losowe kolejki typu: jak bedzie wiecej niz 2k rekordów to wybieramy z nich 1k losowo i przetwarzamy
    // zeby nie pozwolic na korelacje czasowe
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public ChainEngine(int serverId, int totalServers, int myPort, RecordProcessor processor,
                       int newVotesBatchSize = 1000, int newVotesTriggerSize = 2000)
        : base(serverId, totalServers, myPort, processor)
    {
        _isFirstServer = _serverId == 1;
        _reversedShadowPermutation = _ballotLinkingService.GetReversedPermutationListAsync(false).Result;
        _reversedShadowPrimPermutation = _ballotLinkingService.GetReversedPermutationListAsync(true).Result;

        _newVotesBatchSize = newVotesBatchSize;
        _newVotesTriggerSize = newVotesTriggerSize;

        Console.WriteLine($"[ChainEngine] BatchSize={_newVotesBatchSize}, TriggerSize={_newVotesTriggerSize}");
    }

    public override void SetTransport(IExtendedTransport transport)
    {
        _transport = transport;

        // when transport is set start monitoring tasks
        Task.Run(MonitorTimeout);
        Task.Run(PrintStatsLoop);
        Task.Run(MonitorInitialQueue);
    }

    public void SetAuthCodeProcessor(AuthCodeProcessor authCodeProcessor)
    {
        _authCodeProcessor = authCodeProcessor;
    }

    public void OnNewVoteReceived(string voteSerial, string voteCode)
    {
        if (DateTime.Now > _votingEndDate)
        {
            Console.WriteLine($"[{_myPort}] Voting period has ended. New votes are not accepted.");
            return;
        }

        _newVotesQueue.Add((voteSerial, voteCode));
    }

    public void OnReturningRecordReceived(VoteCodeRecord record, bool isSecondPass)
    {
        if (isSecondPass)
            _returningQueue2.Add(record);
        else
            _returningQueue1.Add(record);
    }

    protected override void CheckAndStartProcessing()
    {
        var elapsed = DateTime.Now - _lastProcessingTime;
        var isTimeoutExpired = elapsed.TotalSeconds >= _timeoutSeconds;

        lock (_processingLock)
        {
            if (_isProcessing)
                return;

            if (_queue2.Count >= _batchSize)
            {
                Task.Run(() => ProcessQueue(2));
                return;
            }

            if (_queue1.Count >= _batchSize)
            {
                Task.Run(() => ProcessQueue(1));
                return;
            }

            if (_returningQueue2.Count >= _batchSize)
            {
                Task.Run(() => ProcessReturningQueue(2));
                return;
            }

            if (_returningQueue1.Count >= _batchSize)
            {
                Task.Run(() => ProcessReturningQueue(1));
                return;
            }

            if (!isTimeoutExpired)
                return;

            StartTimeoutFallback();
        }
    }

    private void StartTimeoutFallback()
    {
        if (_queue2.Count > 0 && _queue2.Count >= _queue1.Count)
            Task.Run(() => ProcessQueue(2));
        else if (_queue1.Count > 0 && _queue1.Count >= _returningQueue2.Count)
            Task.Run(() => ProcessQueue(1));
        else if (_returningQueue2.Count > 0 && _returningQueue2.Count >= _returningQueue1.Count)
            Task.Run(() => ProcessReturningQueue(2));
        else if (_returningQueue1.Count > 0)
            Task.Run(() => ProcessReturningQueue(1));
    }

    private async Task ProcessReturningQueue(int queueNumber)
    {
        var batch = new List<VoteCodeRecord>();
        var queue = queueNumber == 2 ? _returningQueue2 : _returningQueue1;

        for (int i = 0; i < _batchSize && queue.Count > 0; i++)
        {
            if (queue.TryTake(out var record, 100))
            {
                batch.Add(record);
            }
        }

        Console.WriteLine($"[{_myPort}] Processing Returning Queue {queueNumber} of {batch.Count} records");

        if (queueNumber == 1)
        {
            await ProcessReturningQueueFirstPass(batch);
        }
        else
        {
            await ProcessReturningQueueSecondPass(batch);
        }

        lock (_processingLock)
        {
            _isProcessing = false;
            _lastProcessingTime = DateTime.Now;
            Console.WriteLine($"[{_myPort}] [RQ{queueNumber}] Finished. RQ1={_returningQueue1.Count}, RQ2={_returningQueue2.Count}");
        }

        CheckAndStartProcessing();
    }

    private async Task ProcessReturningQueueFirstPass(List<VoteCodeRecord> batch)
    {
        var ids = new List<int>(batch.Count);
        foreach (var record in batch)
            ids.Add(record.BallotId);

        await Task.WhenAll(batch.Select(record => ProcessReturningSingleFirstPass(record)));
    }

    private async Task ProcessReturningSingleFirstPass(VoteCodeRecord record)
    {
        try
        {
            var processedRecord = _processor.ProcessReturningSingleFirstPass(record);

            processedRecord.BallotId = _reversedShadowPrimPermutation[processedRecord.BallotId - 1];

            Interlocked.Increment(ref _processedQ1);

            var payload = SerializeRecord(processedRecord);
            await _transport.SendReturningRecordAsync(payload, isSecondPass: _isFirstServer);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{_myPort}] [RQ1] Processing error: {ex.Message}");
        }
    }


    private async Task ProcessReturningQueueSecondPass(List<VoteCodeRecord> batch)
    {
        var ids = new List<int>(batch.Count);
        foreach (var record in batch)
        {
            ids.Add(record.BallotId);
        }

        // get codeSetting data based on BallotId
        var codeSettingData = await _processor.ProcessReturningBatchSecondPassCodeSettingAsync(ids);

        await Task.WhenAll(
            batch.Select(record =>
                ProcessReturningSingleSecondPass(
                    record,
                    codeSettingData
                )
            )
        );
    }

    private async Task ProcessReturningSingleSecondPass(
        VoteCodeRecord record,
        Dictionary<int, (int, int, string)> codeSettingData)
    {
        try
        {
            if (!codeSettingData.TryGetValue(record.BallotId, out var codeSetting))
                throw new InvalidOperationException($"Missing code-setting data for ballot {record.BallotId}");

            var processedRecord = _processor.ProcessReturningSingleSecondPass(record, codeSetting);

            Interlocked.Increment(ref _processedQ2);

            // bounce to next server instead of the previous one
            if (_isFirstServer)
            {
                // VoteCodeRecord -> VoteRecord + old BallotId restoration
                var newProcessedRecord = new VoteRecord
                {
                    BallotId = record.BallotId,
                    VoteVector = processedRecord.VoteVector,
                };

                var payload = SerializeRecord(newProcessedRecord);
                await _transport.SendRecordAsync(payload, isSecondPass: false);
            }
            else
            {
                processedRecord.BallotId = _reversedShadowPermutation[processedRecord.BallotId - 1];

                var payload = SerializeRecord(processedRecord);
                await _transport.SendReturningRecordAsync(payload, isSecondPass: true);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{_myPort}] [RQ2] Processing error: {ex.Message}");
        }
    }

    protected override void OnDataCompleted(VoteRecord data, string? voteSerial)
    {
        var filename = $"tallied_votes_server{_serverId}.txt";
        using (var writer = new StreamWriter(filename, append: true))
        {
            writer.WriteLine(voteSerial);
            foreach (var vote in data.VoteVector)
            {
                writer.WriteLine(vote);
            }
            writer.WriteLine();
        }

        // send VoteVector to BB via AuthCodeProcessor
        if (_authCodeProcessor != null)
        {
            _authCodeProcessor.NotifyVoteTallied(voteSerial, data.VoteVector);
        }
    }

    // create VoteCodeRecord from new votes batch and send to previous server
    private async Task ProcessNewVotesBatch(List<(string, string)> batch)
    {
        Dictionary<string, int> ballotIds = await _processor.ProccesNewVotesBatch(batch);

        foreach (var vote in batch)
        {
            var encryptedVoteCodes = _processor.InitVoteCodeEncryption(vote.Item2);
            var voteVector = _processor.InitVoteVector();

            var record = new VoteCodeRecord
            {
                BallotId = _reversedShadowPrimPermutation[ballotIds[vote.Item1] - 1],
                EncryptedVoteCodeC1 = encryptedVoteCodes.Item1,
                EncryptedVoteCodeC2 = encryptedVoteCodes.Item2,
                VoteVector = voteVector
            };

            var payload = SerializeRecord(record);

            await _transport.SendReturningRecordAsync(payload, isSecondPass: false);
        }
    }

    private static string SerializeRecord(VoteCodeRecord record)
    {
        return JsonSerializer.Serialize(record);
    }

    private void MonitorInitialQueue()
    {
        while (true)
        {
            try
            {
                Task.Delay(1000).Wait();

                lock (_processingLock)
                {
                    if (_newVotesQueue.Count >= _newVotesTriggerSize)
                    {
                        var batch = new List<(string, string)>();

                        //////////////////////////////////////////////
                        // dodac losowe wybieranie glosow z kolejki //
                        //////////////////////////////////////////////

                        for (int i = 0; i < _newVotesBatchSize && _newVotesQueue.Count > 0; i++)
                        {
                            if (_newVotesQueue.TryTake(out var vote, 100))
                            {
                                batch.Add(vote);
                            }
                        }

                        ProcessNewVotesBatch(batch);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{_myPort}] Monitor initial queue error: {ex.Message}");
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
                    if (_queue1.Count > 0 || _queue2.Count > 0 || _returningQueue1.Count > 0 || _returningQueue2.Count > 0)
                        CheckAndStartProcessing();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{_myPort}] Monitor timeout error: {ex.Message}");
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