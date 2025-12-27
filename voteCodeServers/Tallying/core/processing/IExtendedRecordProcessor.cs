using ChainCore;

public interface IExtendedRecordProcessor<TRecord, ARecord> : IRecordProcessor<TRecord, ARecord>
    where TRecord : IBallotRecord
{
    // jakies dodatkowe metody
}