using GrpcChain;
using Grpc.Net.Client;

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);

if (args.Length < 1)
{
    return;
}

int myPort = int.Parse(args[0]);
string? nextServer = args.Length > 1 ? args[1] : null;

if (!string.IsNullOrEmpty(nextServer) && !nextServer.StartsWith("http"))
    nextServer = "http://" + nextServer;


builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(myPort, listenOptions =>
    {
        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2;
    });
});

builder.Services.AddGrpc();
builder.Services.AddSingleton(new ChainServiceImpl(nextServer));

var app = builder.Build();
app.MapGrpcService<ChainServiceImpl>();
app.MapGet("/", () => $"gRPC chain node running on {myPort}");
var serverTask = app.RunAsync();

Console.WriteLine($"[NODE {myPort}] Serwer uruchomiony. Wpisz 'start'");

while (true)
{
    var input = Console.ReadLine();
    if (input?.Trim().ToLower() == "start")
    {
        using var channel = GrpcChannel.ForAddress(nextServer);
        var client = new ChainService.ChainServiceClient(channel);

        Console.WriteLine("[INIT] Wysyłam wiadomość...");
        var reply = await client.PassMessageAsync(new MessageRequest { Text = "START" });
        Console.WriteLine($"[INIT] Odpowiedź: {reply.Response}");
    }
}

