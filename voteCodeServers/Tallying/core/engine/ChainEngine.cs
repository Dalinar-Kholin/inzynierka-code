using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading;
using ChainCore;

public class ChainEngine : ChainEngineBase<VoteRecord, (int?, int, int, string), RecordProcessor, IExtendedTransport>
{
    private readonly bool _isFirstServer;

    private readonly BlockingCollection<VoteCodeRecord> _returningQueue1 = new();
    private readonly BlockingCollection<VoteCodeRecord> _returningQueue2 = new();

    private List<int> _reversedShadowPermutation;
    private List<int> _reversedShadowPrimPermutation;

    private long _processedReturningQ1 = 0;
    private long _processedReturningQ2 = 0;


    public ChainEngine(int serverId, int totalServers, int myPort, RecordProcessor processor)
        : base(serverId, totalServers, myPort, processor)
    {
        _isFirstServer = _serverId == 1;
        _reversedShadowPermutation = _ballotLinkingService.GetReversedPermutationListAsync(false).Result;
        _reversedShadowPrimPermutation = _ballotLinkingService.GetReversedPermutationListAsync(true).Result;
    }

    public override void SetTransport(IExtendedTransport transport)
    {
        _transport = transport;

        // when transport is set start monitoring tasks
        Task.Run(MonitorTimeout);
        Task.Run(PrintStatsLoop);
    }

    protected override void CheckAndStartProcessing()
    {
        lock (_processingLock)
        {
            if (_isProcessing)
                return;

            if (_queue2.Count >= _batchSize)
            {
                Start(() => ProcessQueue(2));
                return;
            }

            if (_queue1.Count >= _batchSize)
            {
                Start(() => ProcessQueue(1));
                return;
            }

            if (_returningQueue2.Count >= _batchSize)
            {
                Start(() => ProcessReturningQueue(2));
                return;
            }

            if (_returningQueue1.Count >= _batchSize)
            {
                Start(() => ProcessReturningQueue(1));
                return;
            }

            if (!IsTimeoutExpired())
                return;
            StartTimeoutFallback();
        }
    }

    private void StartTimeoutFallback()
    {
        if (_queue2.Count > 0 && _queue2.Count >= _queue1.Count)
            Start(() => ProcessQueue(2));
        else if (_queue1.Count > 0 && _queue1.Count >= _returningQueue2.Count)
            Start(() => ProcessQueue(1));
        else if (_returningQueue2.Count > 0 && _returningQueue2.Count >= _returningQueue1.Count)
            Start(() => ProcessReturningQueue(2));
        else if (_returningQueue1.Count > 0)
            Start(() => ProcessReturningQueue(1));
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
        {
            ids.Add(record.BallotId);
        }

        // get BallotId from ShadowSerialPrim
        var firstPass = await _processor.ProcessReturningBatchFirstPassAsync(ids);

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

                    var processedRecord = _processor.ProcessReturningSingleFirstPass(record, firstPass[record.BallotId]);

                    processedRecord.BallotId = _reversedShadowPrimPermutation[processedRecord.BallotId - 1];

                    Interlocked.Increment(ref _processedQ1);

                    var payload = SerializeRecord(processedRecord);
                    await _transport.SendReturningRecordAsync(payload, isSecondPass: _isFirstServer);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{_myPort}] [RQ1] Processing error: {ex.Message}");
                }
            });
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
    }

    private async Task ProcessReturningQueueSecondPass(List<VoteCodeRecord> batch)
    {
        var ids = new List<int>(batch.Count);

        foreach (var record in batch)
        {
            ids.Add(record.BallotId);
        }

        // get BallotId from ShadowSerial
        var secondPass = await _processor.ProcessReturningBatchSecondPassAsync(ids);

        // get codeSetting data based on BallotId
        var codeSettingData = await _processor.ProcessReturningBatchSecondPassCodeSettingAsync(secondPass.Values.ToList());

        if (_isFirstServer)
        {
            // build reversed second pass mapping
            var reversedSecondPass = new Dictionary<int, int>();
            foreach (var kvp in secondPass)
            {
                reversedSecondPass[kvp.Value] = kvp.Key;
            }
        }

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

                    record.BallotId = secondPass[record.BallotId];

                    var processedRecord = _processor.ProcessReturningSingleSecondPass(record, codeSettingData[record.BallotId]);

                    Interlocked.Increment(ref _processedQ2);

                    // bounce to next server instead of the previous one
                    if (_isFirstServer)
                    {
                        // VoteCodeRecord to VoteRecord conversion and old BallotId restoration
                        var newProcessedRecord = new VoteRecord
                        {
                            BallotId = reversedSecondPass[processedRecord.BallotId],
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

                    // saving commitments - chyba
                    // await _processor.PersistRecordAsync(processedRecord);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{_myPort}] [RQ2] Processing error: {ex.Message}");
                }
            });
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
    }

    protected override void OnDataCompleted(VoteRecord data, string voteSerial)
    {
        Console.Write("Vote Vector: ");
        foreach (var vote in data.VoteVector)
        {
            Console.Write(vote + " ");
        }
        Console.Write("\n");
    }

    public void OnReturningRecordReceived(VoteCodeRecord record, bool isSecondPass)
    {
        if (isSecondPass)
            _returningQueue2.Add(record);
        else
            _returningQueue1.Add(record);
    }

    private static string SerializeRecord(VoteCodeRecord record)
    {
        return JsonSerializer.Serialize(record);
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