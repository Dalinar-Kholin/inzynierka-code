using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Org.BouncyCastle.Math;
using System.Security.Cryptography;
using Microsoft.VisualBasic;
using VoteCodeServers.Helpers;

public class RecordProcessor : IRecordProcessor<DataRecord, (int, string[])>
{
    private readonly int _serverId;
    private readonly int _totalServers;
    private readonly bool _isLastServer;

    private readonly BallotService _ballotService;
    private readonly BallotLinkingService _ballotLinkingService;
    private readonly VoteSerialsService _voteSerialsService;
    private readonly CodeSettingService _codeSettingService;
    private readonly PaillierPublicKey _paillierPublic;

    public RecordProcessor(int serverId, int totalServers)
    {
        _serverId = serverId;
        _totalServers = totalServers;
        _isLastServer = _serverId == _totalServers;

        _ballotService = new BallotService(serverId, totalServers);
        _ballotLinkingService = new BallotLinkingService(serverId);
        _voteSerialsService = new VoteSerialsService(serverId);
        _codeSettingService = new CodeSettingService(serverId);
        _paillierPublic = new PaillierPublicKey("../../encryption/paillierKeys");
    }

    public async Task<Dictionary<int, (int, string[])>> ProcessBatchFirstPassAsync(List<int> ids)
    {
        Dictionary<int, int> shadowBatch = await _ballotService.GetShadowBatch(ids, false);
        Dictionary<int, string[]> vBatch = await _codeSettingService.GetVBatch(ids);

        var batchData = new Dictionary<int, (int shadowSerial, string[] v)>();
        foreach (var ballotId in ids)
        {
            shadowBatch.TryGetValue(ballotId, out var shadow);
            vBatch.TryGetValue(ballotId, out var v);
            batchData[ballotId] = (shadow, v);
        }

        return batchData;
    }

    public async Task<Dictionary<int, int>> ProcessBatchSecondPassAsync(List<int> ids)
    {
        return await _ballotService.GetShadowBatch(ids, true);
    }

    public async Task<Dictionary<int, string>> ProcessBatchSecondPassLastServerAsync(List<int> ids)
    {
        return await _voteSerialsService.GetVoteSerialsBatch(ids);
    }

    public DataRecord ProcessSingleFirstPass(DataRecord record, (int, string[]) firstPass)
    {
        // re-encrypt each vector V which is in the record.Vectors array
        for (int m = 0; m < record.Vectors.Count; m++)
        {
            for (int i = 0; i < record.Vectors[m].Length; i++)
            {
                BigInteger vElementBigInt = new BigInteger(record.Vectors[m][i]);
                BigInteger reEncryptedElement = _paillierPublic.ReEncrypt(vElementBigInt);
                record.Vectors[m][i] = reEncryptedElement.ToString();
            }
        }

        record.BallotId = firstPass.Item1;

        if (firstPass.Item2 != null)
        {
            record.Vectors.Add(firstPass.Item2);
        }

        // Fisher–Yates vector shuffle
        if (record.Vectors != null && record.Vectors.Count > 1)
        {
            for (int i = record.Vectors.Count - 1; i > 0; i--)
            {
                int j = RandomNumberGenerator.GetInt32(i + 1);
                if (j != i)
                {
                    var tmp = record.Vectors[i];
                    record.Vectors[i] = record.Vectors[j];
                    record.Vectors[j] = tmp;
                }
            }
        }

        return record;
    }

    public DataRecord ProcessSingleSecondPass(DataRecord record, int? secondPass)
    {
        // re-encrypt each vector V which is in the record.Vectors array
        for (int m = 0; m < record.Vectors.Count; m++)
        {
            for (int i = 0; i < record.Vectors[m].Length; i++)
            {
                BigInteger vElementBigInt = new BigInteger(record.Vectors[m][i]);
                BigInteger reEncryptedElement = _paillierPublic.ReEncrypt(vElementBigInt);
                record.Vectors[m][i] = reEncryptedElement.ToString();
            }
        }

        if (secondPass.HasValue)
        {
            record.BallotId = secondPass.Value;
        }

        return record;
    }

    public async Task PersistRecordAsync(DataRecord record)
    {
        // zapisac commitmenty do DB chyba
        await Task.CompletedTask;
    }
}

