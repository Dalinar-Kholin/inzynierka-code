using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Org.BouncyCastle.Math;
using System.Security.Cryptography;
using Microsoft.VisualBasic;
using VoteCodeServers.Helpers;

public class RecordProcessor : IRecordProcessor<DataRecord, (int, int, string)>
{
    private readonly int _serverId;
    private readonly int _totalServers;
    private readonly bool _isLastServer;
    private readonly int _numberOfCandidates;

    private readonly BallotService _ballotService;
    private readonly BallotLinkingService _ballotLinkingService;
    private readonly VoteSerialsService _voteSerialsService;
    private readonly CodeSettingService _codeSettingService;
    private readonly PaillierPublicKey _paillierPublic;
    private AlphabetEncoder E = AlphabetEncoder.Instance;

    public RecordProcessor(int serverId, int totalServers, int numberOfCandidates)
    {
        _serverId = serverId;
        _totalServers = totalServers;
        _isLastServer = _serverId == _totalServers;
        _numberOfCandidates = numberOfCandidates;

        _ballotService = new BallotService(serverId, totalServers);
        _ballotLinkingService = new BallotLinkingService(serverId);
        _voteSerialsService = new VoteSerialsService(serverId);
        _codeSettingService = new CodeSettingService(serverId);
        _paillierPublic = new PaillierPublicKey("../../encryption/paillierKeys");
    }

    public async Task<Dictionary<int, (int, int, string)>> ProcessBatchFirstPassAsync(List<int> ballotIds)
    {
        Dictionary<int, (int, int, string)> codeSettingBatch = await _codeSettingService.GetFinalEncryptionBatch(ballotIds);

        var batchData = new Dictionary<int, (int, int, string)>();
        foreach (var ballotId in ballotIds)
        {
            if (!codeSettingBatch.TryGetValue(ballotId, out var codeSetting) || codeSetting.Item3 == null)
            {
                Console.WriteLine($"[RecordProcessor] Missing code setting for ballot {ballotId} (skipping)");
                continue;
            }

            batchData[ballotId] = (codeSetting.Item1, codeSetting.Item2, codeSetting.Item3);
        }

        return batchData;
    }

    public async Task<Dictionary<int, string>> ProcessBatchSecondPassLastServerAsync(List<int> shadowPrimSerials)
    {
        return await _voteSerialsService.GetVoteSerialsBatch(shadowPrimSerials);
    }

    public DataRecord ProcessSingleFirstPass(DataRecord record, (int, int, string) firstPass)
    {
        if (record.EncryptedVoteCodes == null)
        {
            record.EncryptedVoteCodes = _paillierPublic.Encrypt(new BigInteger("0")).ToString();
        }

        BigInteger codes = BigInteger.Zero;
        foreach (char c in firstPass.Item3)
        {
            if (c == '0')
            {
                codes = E.AppendDigit(codes, firstPass.Item1);
            }
            else
            {
                codes = E.AppendDigit(codes, firstPass.Item2);
            }
        }
        codes = E.ShiftLeft(codes, (_serverId - 1) * _numberOfCandidates);

        codes = _paillierPublic.Encrypt(codes);
        record.EncryptedVoteCodes = (new BigInteger(record.EncryptedVoteCodes).Multiply(codes).Mod(_paillierPublic.n_squared)).ToString();

        return record;
    }

    public DataRecord ProcessSingleSecondPass(DataRecord record)
    {
        record.EncryptedVoteCodes = _paillierPublic.ReEncrypt(new BigInteger(record.EncryptedVoteCodes)).ToString();

        return record;
    }

    public string FinnalizeEncryptedVoteCodes(string encryptedVoteCodes, string voteSerial)
    {

        ///////////////////////////////////////////////////////////////////
        // dodac sprawdzenie czy dany voteSerial jest AreVoteCodeCorrect //
        ///////////////////////////////////////////////////////////////////

        BigInteger encodedVoteSerial = BigInteger.Zero;
        foreach (char c in voteSerial)
        {
            encodedVoteSerial = E.AppendLetter(encodedVoteSerial, c);
        }
        encodedVoteSerial = E.ShiftLeft(encodedVoteSerial, _totalServers * _numberOfCandidates);
        encodedVoteSerial = _paillierPublic.Encrypt(encodedVoteSerial);

        return (new BigInteger(encryptedVoteCodes).Multiply(encodedVoteSerial).Mod(_paillierPublic.n_squared)).ToString();
    }

    public async Task PersistRecordAsync(DataRecord record)
    {
        // zapisac commitmenty do DB chyba
        await Task.CompletedTask;
    }
}

