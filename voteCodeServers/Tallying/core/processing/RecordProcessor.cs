using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Org.BouncyCastle.Math;
using System.Security.Cryptography;
using Microsoft.VisualBasic;
using VoteCodeServers.Helpers;

public class RecordProcessor : IExtendedRecordProcessor<VoteRecord, (int?, int, int, string)>
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


    public async Task<Dictionary<int, int>> ProcessReturningBatchFirstPassAsync(List<int> ids)
    {
        return await _ballotService.GetBallotIdBatch(ids, true);
    }


    public VoteRecord ProcessReturningSingleFirstPass(VoteCodeRecord record, int firstPass)
    {
        // re-encrypcja EncryptedVoteCode oraz VoteVector


        // zamiana BallotId na ShadowSerial/Prim
        record.BallotId = firstPass;

        return record;
    }

    public async Task<Dictionary<int, int>> ProcessReturningBatchSecondPassAsync(List<int> ids)
    {
        return await _ballotService.GetBallotIdBatch(ids, false);
    }

    public async Task<Dictionary<int, CodeSetting>> ProcessReturningBatchSecondPassCodeSettingAsync(List<int> ids)
    {
        return await _codeSettingService.GetFinalEncryptionBatch(ids);
    }

    public VoteCodeRecord ProcessReturningSingleSecondPass(VoteCodeRecord record, CodeSetting codeSetting)
    {
        // usuniecie swojej litery z EncryptedVoteCode oraz aktualizacja VoteVector

        return record;
    }


    public async Task<Dictionary<int, (int?, int, int, string)>> ProcessBatchFirstPassAsync(List<int> ids)
    {
        // narazie tylko zeby zwracalo cokolwiek dla testowania
        // czyli zwroc mi slownik z przykaldowymi danymi dla id (bo w ids bedzei tylko lsita 1 elementowa)
        Console.WriteLine(ids[0]);
        return new Dictionary<int, (int?, int, int, string)>()
        {
            { ids[0], (123, 456, 789, "exampleVoteCode") }
        };
    }

    public async Task<Dictionary<int, int?>> ProcessBatchSecondPassAsync(List<int> ids)
    {
        return await _ballotService.GetShadowBatch(ids, true);
    }

    public async Task<Dictionary<int, string?>> ProcessBatchSecondPassLastServerAsync(List<int> ids)
    {
        return await _voteSerialsService.GetVoteSerialsBatch(ids);
    }

    public VoteRecord ProcessSingleFirstPass(VoteRecord record, (int?, int, int, string) firstPass)
    {
        return record;
    }

    public VoteRecord ProcessSingleSecondPass(VoteRecord record, int? secondPass)
    {
        return record;
    }
}

