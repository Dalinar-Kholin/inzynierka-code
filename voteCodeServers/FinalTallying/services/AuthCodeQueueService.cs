using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using System.Collections.Concurrent;

public record AuthCodeRequest(string AuthCode);

public class AuthCodeQueueService
{
    private readonly int _numberOfCandidates;
    private readonly ConcurrentQueue<string> _queue = new();
    private readonly Timer _timer;
    private const int _batchSize = 1000;
    private const int _intervalMs = 5000;
    private readonly VoteService _voteService = new VoteService();
    private List<BigInteger> final_ciphertext;
    private readonly PaillierPublicKey _paillierPublic = new PaillierPublicKey("../../encryption/paillierKeys");

    public AuthCodeQueueService(int numberOfCandidates)
    {
        _numberOfCandidates = numberOfCandidates;
        final_ciphertext = new List<BigInteger>();
        for (int i = 0; i < numberOfCandidates; i++)
        {
            final_ciphertext.Add(_paillierPublic.Encrypt(BigInteger.Zero));
        }

        _timer = new Timer(ProcessQueue, null, _intervalMs, _intervalMs);
    }

    public void Enqueue(string authCode)
    {
        _queue.Enqueue(authCode);
        Console.WriteLine($"Enqueued: {authCode}");
        if (_queue.Count >= _batchSize)
            ProcessQueue(null);
    }

    private void ProcessQueue(object? state)
    {
        var batch = new List<string>();
        while (batch.Count < _batchSize && _queue.TryDequeue(out var code))
            batch.Add(code);
        if (batch.Count > 0)
        {
            var votes = _voteService.QueryVotes(batch);

            ////////////////////////////////////////////////////////////////////////////////
            // powinna byc dodana jeszcze weryfikacja czy votes jest poprawne z BB:
            // liczymy merkla od votes, pobieramy z BB korzen i porownujemy
            ////////////////////////////////////////////////////////////////////////////////

            final_ciphertext = AddCiphertexts(final_ciphertext, votes);
            Console.WriteLine($"Processing batch: {string.Join(", ", batch)}");
        }

        Console.WriteLine("Current final ciphertext:");
        for (int i = 0; i < final_ciphertext.Count; i++)
        {
            Console.WriteLine($"Candidate {i + 1}: {final_ciphertext[i]}");
        }
    }

    private List<BigInteger> AddCiphertexts(List<BigInteger> currentTotal, List<VoteData> votes)
    {
        Console.WriteLine("Adding ciphertexts from votes to current total...");
        foreach (var vote in votes)
        {
            for (int i = 0; i < _numberOfCandidates; i++)
            {
                var part = vote.VoteVector[i];
                Console.WriteLine($"Adding part for candidate {i + 1}: {part}");
                currentTotal[i] = currentTotal[i].Multiply(new BigInteger(part)).Mod(_paillierPublic.n_squared);
            }
        }
        return currentTotal;
    }

    public List<BigInteger> GetFinalCiphertext()
    {
        return final_ciphertext;
    }
}