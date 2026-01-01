using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Org.BouncyCastle.Math;
using System.Security.Cryptography;
using Microsoft.VisualBasic;
using VoteCodeServers.Helpers;

public class RecordProcessor : IExtendedRecordProcessor<VoteRecord, int>
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
    private readonly ElGamalEncryption _elGamalEncryption;
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
        _elGamalEncryption = new ElGamalEncryption(serverId, "../../encryption/elGamalKeys");

    }

    public async Task<Dictionary<int, string>> ProccesNewVotesBatch(List<int> ids)
    {
        return await _voteSerialsService.GetVoteSerialsBatch(ids);
    }

    public (List<string>, List<string>) InitVoteCodeEncryption(string voteCode)
    {
        var encryptedVoteCodeC1 = new List<string>();
        var encryptedVoteCodeC2 = new List<string>();

        for (int i = 0; i < voteCode.Length; i++)
        {
            char c = voteCode[i];

            // char to BigInteger
            var letter = E.AppendLetter(BigInteger.Zero, c);
            var encryptedLetter = _elGamalEncryption.Encrypt(letter, voteCode.Length - i);
            encryptedVoteCodeC1.Add(encryptedLetter.c1.ToString());
            encryptedVoteCodeC2.Add(encryptedLetter.c2.ToString());
        }

        return (encryptedVoteCodeC1, encryptedVoteCodeC2);
    }

    public List<string> InitVoteVector()
    {
        var voteVector = new List<string>(_numberOfCandidates);

        for (int i = 0; i < _numberOfCandidates; i++)
        {
            var one = _paillierPublic.Encrypt(BigInteger.One);
            voteVector.Add(one.ToString());
        }

        return voteVector;
    }

    public async Task<Dictionary<int, int>> ProcessReturningBatchFirstPassAsync(List<int> ids)
    {
        return await _ballotService.GetBallotIdBatch(ids, true);
    }

    public VoteCodeRecord ProcessReturningSingleFirstPass(VoteCodeRecord record, int firstPass)
    {
        // re-encryption of EncryptedVoteCode and VoteVector
        var reEncryptedVoteCodeC1 = new List<string>();
        var reEncryptedVoteCodeC2 = new List<string>();

        for (int i = 0; i < record.EncryptedVoteCodeC1.Count; i++)
        {
            var encCodeC1 = record.EncryptedVoteCodeC1[i];
            var encCodeC2 = record.EncryptedVoteCodeC2[i];

            var reEncLetter = _elGamalEncryption.ReEncrypt((new BigInteger(encCodeC1), new BigInteger(encCodeC2)), record.EncryptedVoteCodeC1.Count - i);
            reEncryptedVoteCodeC1.Add(reEncLetter.c1.ToString());
            reEncryptedVoteCodeC2.Add(reEncLetter.c2.ToString());
        }

        var reEncryptedVoteVector = new List<string>();
        foreach (var vote in record.VoteVector)
        {
            var encVote = new BigInteger(vote);
            var reEncVote = _paillierPublic.ReEncrypt(encVote);
            reEncryptedVoteVector.Add(reEncVote.ToString());
        }

        record.EncryptedVoteCodeC1 = reEncryptedVoteCodeC1;
        record.EncryptedVoteCodeC2 = reEncryptedVoteCodeC2;
        record.VoteVector = reEncryptedVoteVector;
        record.BallotId = firstPass;

        return record;
    }

    public async Task<Dictionary<int, int>> ProcessReturningBatchSecondPassAsync(List<int> ids)
    {
        return await _ballotService.GetBallotIdBatch(ids, false);
    }

    public async Task<Dictionary<int, (int, int, string)>> ProcessReturningBatchSecondPassCodeSettingAsync(List<int> ids)
    {
        return await _codeSettingService.GetFinalEncryptionBatch(ids);
    }

    public VoteCodeRecord ProcessReturningSingleSecondPass(VoteCodeRecord record, (int, int, string) codeSetting)
    {
        // removal of own letter from EncryptedVoteCode and update of VoteVector
        // the first letter from EncryptedVoteCode is the server's letter
        var encryptedLetterC1 = new BigInteger(record.EncryptedVoteCodeC1[0]);
        var encryptedLetterC2 = new BigInteger(record.EncryptedVoteCodeC2[0]);
        var decryptedLetter = _elGamalEncryption.Decrypt((encryptedLetterC1, encryptedLetterC2)).IntValue;

        record.EncryptedVoteCodeC1.RemoveAt(0);
        record.EncryptedVoteCodeC2.RemoveAt(0);

        for (int i = 0; i < codeSetting.Item3.Length; i++)
        {
            char c = codeSetting.Item3[i];
            if ((c == '0' && decryptedLetter == codeSetting.Item1) || (c == '1' && decryptedLetter == codeSetting.Item2))
            {
                record.VoteVector[i] = _paillierPublic.ReEncrypt(new BigInteger(record.VoteVector[i])).ToString();
            }
            else
            {
                record.VoteVector[i] = _paillierPublic.Encrypt(BigInteger.Zero).ToString();
            }
        }

        for (int i = 0; i < record.EncryptedVoteCodeC1.Count; i++)
        {
            var encLetterC1 = new BigInteger(record.EncryptedVoteCodeC1[i]);
            var encLetterC2 = new BigInteger(record.EncryptedVoteCodeC2[i]);
            var reEncLetter = _elGamalEncryption.ReEncrypt((encLetterC1, encLetterC2), record.EncryptedVoteCodeC1.Count - i);
            record.EncryptedVoteCodeC1[i] = reEncLetter.c1.ToString();
            record.EncryptedVoteCodeC2[i] = reEncLetter.c2.ToString();
        }


        return record;
    }

    public async Task<Dictionary<int, int>> ProcessBatchFirstPassAsync(List<int> ids)
    {
        return await _ballotService.GetShadowBatch(ids, false);
    }

    public async Task<Dictionary<int, int>> ProcessBatchSecondPassAsync(List<int> ids)
    {
        return await _ballotService.GetShadowBatch(ids, true);
    }

    public async Task<Dictionary<int, string>> ProcessBatchSecondPassLastServerAsync(List<int> ids)
    {
        return await _voteSerialsService.GetVoteSerialsBatch(ids);
    }

    public VoteRecord ProcessSingleFirstPass(VoteRecord record, int firstPass)
    {
        record.BallotId = firstPass;
        return record;
    }

    public VoteRecord ProcessSingleSecondPass(VoteRecord record, int? secondPass)
    {
        if (secondPass.HasValue)
        {
            record.BallotId = secondPass.Value;
        }

        return record;
    }

    public async Task<Dictionary<string, int>> ProccesNewVotesBatch(List<(string, string)> batch)
    {
        List<string> voteSerials = new List<string>();
        foreach (var vote in batch)
        {
            voteSerials.Add(vote.Item1);
        }

        return await _voteSerialsService.GetBallotIdsBatch(voteSerials);
    }
}

