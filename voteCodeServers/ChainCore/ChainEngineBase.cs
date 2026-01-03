using System.Collections.Concurrent;
using System.Text.Json;

namespace ChainCore
{
    // zmienic nazwe ARecord na cos innego
    public abstract class ChainEngineBase<TRecord, ARecord, TProcessor, TTransport>
        where TRecord : IBallotRecord
        where TProcessor : IRecordProcessor<TRecord, ARecord>
        where TTransport : ITransport
    {
        protected readonly int _myPort;
        protected readonly int _serverId;
        protected readonly int _totalServers;
        protected readonly bool _isLastServer;

        protected readonly BlockingCollection<TRecord> _queue1 = new();
        protected readonly BlockingCollection<TRecord> _queue2 = new();

        protected List<int> _shadowPermutation;
        protected List<int> _shadowPrimPermutation;

        // services
        protected readonly BallotLinkingService _ballotLinkingService;
        protected readonly TProcessor _processor;
        protected TTransport? _transport;

        protected bool _isProcessing = false;
        protected DateTime _lastProcessingTime = DateTime.Now;
        protected readonly object _processingLock = new();

        // stats
        protected long _processedQ1 = 0;
        protected long _processedQ2 = 0;

        protected const int _batchSize = 1000;
        protected const int _timeoutSeconds = 1;

        public ChainEngineBase(int serverId, int totalServers, int myPort, TProcessor processor)
        {
            _serverId = serverId;
            _totalServers = totalServers;
            _isLastServer = _serverId == _totalServers;
            _myPort = myPort;

            _processor = processor;
            _ballotLinkingService = new BallotLinkingService(serverId);

            _shadowPermutation = _ballotLinkingService.GetPermutationListAsync(false).Result;
            _shadowPrimPermutation = _ballotLinkingService.GetPermutationListAsync(true).Result;
        }
        protected abstract void CheckAndStartProcessing();
        protected abstract void OnDataCompleted(TRecord record, string? voteSerial);


        public virtual void SetTransport(TTransport transport)
        {
            _transport = transport;
        }

        public void OnRecordReceived(TRecord record, bool secondPass)
        {
            if (secondPass)
                _queue2.Add(record);
            else
                _queue1.Add(record);
        }

        public async Task ProcessQueue(int queueNumber)
        {
            var batch = new List<TRecord>();
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

        protected async Task ProcessQueueFirstPass(List<TRecord> batch)
        {
            // shadow permutation + ids
            var ids = new List<int>(batch.Count);
            foreach (var record in batch)
            {
                if (_serverId != 1)
                    record.BallotId = _shadowPermutation[record.BallotId - 1];
                ids.Add(record.BallotId);
            }

            var firstPass = await _processor.ProcessBatchFirstPassAsync(ids);

            await Task.WhenAll(
                batch.Select(record => Task.Run(() => ProcessSingleFirstPass(record, firstPass)))
            );
        }

        private async Task ProcessSingleFirstPass(
            TRecord record,
            Dictionary<int, ARecord> firstPass)
        {
            try
            {
                if (!firstPass.TryGetValue(record.BallotId, out var firstPassData))
                    throw new InvalidOperationException($"Missing first-pass data for ballot {record.BallotId}");

                var processed = _processor.ProcessSingleFirstPass(record, firstPassData);
                Interlocked.Increment(ref _processedQ1);

                var payload = SerializeRecord(processed);
                await _transport.SendRecordAsync(payload, _isLastServer);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{_myPort}] [Q1] Processing error: {ex.Message}");
            }
        }


        protected async Task ProcessQueueSecondPass(List<TRecord> batch)
        {
            // shadow permutation + ids
            var ids = new List<int>(batch.Count);
            foreach (var record in batch)
            {
                record.BallotId = _shadowPrimPermutation[record.BallotId - 1];
                ids.Add(record.BallotId);
            }

            Dictionary<int, int>? secondPass = null;
            Dictionary<int, string>? voteSerials = null;

            if (_isLastServer)
                voteSerials = await _processor.ProcessBatchSecondPassLastServerAsync(ids);
            else
                secondPass = await _processor.ProcessBatchSecondPassAsync(ids);

            await Task.WhenAll(batch.Select(record => Task.Run(() => ProcessSingle(record, secondPass, voteSerials))));
        }

        private async Task ProcessSingle(
            TRecord record,
            Dictionary<int, int>? secondPass,
            Dictionary<int, string>? voteSerials)
        {
            try
            {
                int? secondPassData = null;
                string? voteSerialData = null;

                if (secondPass != null)
                {
                    secondPass.TryGetValue(record.BallotId, out var sp);
                    secondPassData = sp;
                }
                if (voteSerials != null)
                {
                    voteSerials.TryGetValue(record.BallotId, out var vs);
                    voteSerialData = vs;
                }

                var processed = _processor.ProcessSingleSecondPass(record, secondPassData);
                Interlocked.Increment(ref _processedQ2);

                if (_isLastServer)
                    OnDataCompleted(processed, voteSerialData);
                else
                    await _transport.SendRecordAsync(SerializeRecord(processed), true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{_myPort}] [Q2] Processing error: {ex.Message}");
            }
        }

        protected string SerializeRecord(TRecord record)
        {
            return JsonSerializer.Serialize(record);
        }
    }
}
