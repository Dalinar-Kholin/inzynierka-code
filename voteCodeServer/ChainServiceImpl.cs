using Grpc.Core;
using Grpc.Net.Client;
using GrpcChain;

public class ChainServiceImpl : ChainService.ChainServiceBase
{
    private readonly string _nextServer;

    public ChainServiceImpl(string nextServer)
    {
        _nextServer = nextServer;
    }

    public override async Task<MessageReply> PassMessage(MessageRequest request, ServerCallContext context)
    {
        Console.WriteLine($"[RECEIVED] {request.Text}");
        string modified = request.Text + "X";

        if (!string.IsNullOrEmpty(_nextServer))
        {
            using var channel = GrpcChannel.ForAddress(_nextServer);
            var client = new ChainService.ChainServiceClient(channel);

            var reply = await client.PassMessageAsync(new MessageRequest { Text = modified });
            Console.WriteLine($"[] Odpowiedź: {reply.Response}");

            return new MessageReply { Code = 200, Response = "ok" };
        }
        else
        {
            Console.WriteLine($"[END] Final message: {modified}");
            return new MessageReply { Code = 200, Response = "ok" };
        }
    }
}
