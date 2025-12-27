public interface IRecordProcessor<TRecord, ARecord>
{
    Task<Dictionary<int, ARecord>> ProcessBatchFirstPassAsync(List<int> ids);
    Task<Dictionary<int, int?>> ProcessBatchSecondPassAsync(List<int> ids);
    Task<Dictionary<int, string?>> ProcessBatchSecondPassLastServerAsync(List<int> ids);
    TRecord ProcessSingleFirstPass(TRecord record, ARecord firstPass);
    TRecord ProcessSingleSecondPass(TRecord record, int? secondPass);
}