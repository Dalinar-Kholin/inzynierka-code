using GrpcChain;
using Grpc.Net.Client;

int ballotNumber = 40400;

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
string nextServer = $"http://localhost:{nextPort}";

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(myPort, listenOptions =>
    {
        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2;
    });
});

builder.Services.AddGrpc();

var service = new ChainServiceImpl(serverId, totalServers, nextServer, myPort, ballotNumber);
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

    var parts = input.Split(' ', 2);
    var command = parts[0].ToLower();

    switch (command)
    {
        case "e":
            Console.WriteLine("Shutting down...");
            Environment.Exit(0);
            break;

        case "init":
            if (serverId == 1)
            {
                await service.InitializeData();
            }
            else
            {
                Console.WriteLine("'init' only available on Server 1");
            }
            break;
    }
}