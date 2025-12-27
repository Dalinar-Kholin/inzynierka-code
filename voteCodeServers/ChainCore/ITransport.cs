namespace ChainCore
{
    public interface ITransport
    {
        Task SendRecordAsync(string record, bool isSecondPass);
    }
}
