using ChainCore;

public interface IExtendedTransport : ITransport
{
    Task SendReturningRecordAsync(string record, bool isSecondPass);
}