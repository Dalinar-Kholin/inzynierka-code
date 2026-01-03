using GrpcChain;
using Grpc.Net.Client;

var cfg = VoteCodeServers.Helpers.Config.Load();
int ballotNumber = cfg.NumberOfVoters * 4 + cfg.SafetyParameter * 2;
int numberOfCandidates = cfg.NumberOfCandidates;
var batchSettings = cfg.BatchSettings;

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

if (args.Length < 2)
{
    Console.WriteLine("Usage: dotnet run <serverId> <totalServers>");
    Console.WriteLine("Example for 3 servers in loop:");
    Console.WriteLine("  Terminal 1: dotnet run 1 3");
    Console.WriteLine("  Terminal 2: dotnet run 2 3");
    Console.WriteLine("  Terminal 3: dotnet run 3 3");
    return;
}

int serverId = int.Parse(args[0]);
int totalServers = int.Parse(args[1]);

int myPort = 5000 + serverId;

int nextServerId = (serverId % totalServers) + 1;
int nextPort = 5000 + nextServerId;
int prevServerId = ((serverId - 2 + totalServers) % totalServers) + 1;
int prevPort = 5000 + prevServerId;
string nextServer = $"http://localhost:{nextPort}";
string prevServer = $"http://localhost:{prevPort}";

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(myPort, listenOptions =>
    {
        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2;
    });
});

builder.Services.AddGrpc();

var processor = new RecordProcessor(serverId, totalServers, numberOfCandidates);
var engine = new ChainEngine(serverId, totalServers, myPort, processor, batchSettings.VotesBatchSize, batchSettings.VotesTriggerSize);
var service = new ChainServiceImpl(nextServer, prevServer, myPort, engine);

engine.SetTransport(service);

builder.Services.AddSingleton(service);

var app = builder.Build();
app.MapGrpcService<ChainServiceImpl>();
app.MapGet("/", () => $"Chain node on port {myPort}");

_ = app.RunAsync();

Console.WriteLine("  send <message>");
if (serverId == 1)
{
    Console.WriteLine("  init <count>    - Initialize Queue 1 with <count> records (Server 1 only)");
}
Console.WriteLine("  exit");

while (true)
{
    Console.Write($"[{myPort}]> ");
    var input = Console.ReadLine();

    if (string.IsNullOrEmpty(input)) continue;

    var parts = input.Split(' ', 3);
    var command = parts[0].ToLower();
    var message1 = parts.Length > 1 ? parts[1] : null;
    var message2 = parts.Length > 2 ? parts[2] : null;

    switch (command)
    {
        case "e":
            Console.WriteLine("Shutting down...");
            Environment.Exit(0);
            break;

        case "send":
            if (serverId == totalServers)
            {
                service.SendData(message1, message2);
                Console.WriteLine("Data sent.");
            }
            else
            {
                Console.WriteLine("'send' only available on the last server.");
            }
            break;
    }
}