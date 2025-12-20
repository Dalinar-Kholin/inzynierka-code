using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Org.BouncyCastle.Math;
using System.Security.Cryptography;
using Microsoft.VisualBasic;
using VoteCodeServers.Helpers;


public class DataProcessingService
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

    public DataProcessingService(int serverId, int totalServers, int numberOfCandidates)
    {
        _serverId = serverId;
        _totalServers = totalServers;
        _isLastServer = _serverId == _totalServers;
        _numberOfCandidates = numberOfCandidates;

        _ballotService = new BallotService(serverId);
        _ballotLinkingService = new BallotLinkingService(serverId);
        _voteSerialsService = new VoteSerialsService(serverId);
        _codeSettingService = new CodeSettingService(serverId);
        _paillierPublic = new PaillierPublicKey("../../encryption/paillierKeys");
    }

    public async Task<Dictionary<int, (int?, int, int, string)>> ProcessBatchFirstPassAsync(List<int> ids)
    {
        Dictionary<int, int?> shadowBatch = await _ballotService.GetShadowBatch(ids, false);
        Dictionary<int, (int, int, string)> codeSettingBatch = await _codeSettingService.GetFinalEncryptionBatch(ids);

        var batchData = new Dictionary<int, (int?, int, int, string)>();
        foreach (var ballotId in ids)
        {
            shadowBatch.TryGetValue(ballotId, out var shadow);
            codeSettingBatch.TryGetValue(ballotId, out var codeSetting);
            batchData[ballotId] = (shadow, codeSetting.Item1, codeSetting.Item2, codeSetting.Item3);
        }

        return batchData;
    }

    public async Task<Dictionary<int, int?>> ProcessBatchSecondPassAsync(List<int> ids)
    {
        return await _ballotService.GetShadowBatch(ids, true);
    }

    public async Task<Dictionary<int, string?>> ProcessBatchSecondPassLastServerAsync(List<int> ids)
    {
        return await _voteSerialsService.GetVoteSerialsBatch(ids);
    }

    public DataRecord ProcessSingleFirstPass(DataRecord record, (int?, int, int, string) firstPass)
    {
        // zaszyfrowanie swoich voteCodów i dodanie do rekordu

        if (record.EncryptedVoteCodes == null)
        {
            record.EncryptedVoteCodes = _paillierPublic.Encrypt(new BigInteger("0")).ToString();
        }

        BigInteger codes = BigInteger.Zero;
        foreach (char c in firstPass.Item4)
        {
            if (c == '0')
            {
                codes = E.AppendDigit(codes, firstPass.Item2);
            }
            else
            {
                codes = E.AppendDigit(codes, firstPass.Item3);
            }
        }
        codes = E.ShiftLeft(codes, (_serverId - 1) * _numberOfCandidates);

        codes = _paillierPublic.Encrypt(codes);
        record.EncryptedVoteCodes = (new BigInteger(record.EncryptedVoteCodes).Multiply(codes).Mod(_paillierPublic.n_squared)).ToString();

        if (firstPass.Item1.HasValue)
        {
            record.BallotId = firstPass.Item1.Value;
        }

        return record;
    }

    public DataRecord ProcessSingleSecondPass(DataRecord record, int? secondPass)
    {
        record.EncryptedVoteCodes = _paillierPublic.ReEncrypt(new BigInteger(record.EncryptedVoteCodes)).ToString();

        if (secondPass.HasValue)
        {
            record.BallotId = secondPass.Value;
        }

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

