using GrpcChain;
using Grpc.Net.Client;

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

if (args.Length < 1)
{
    Console.WriteLine("  dotnet run 5001 5002");
    return;
}

int myPort = int.Parse(args[0]);
string? nextServer = args.Length > 1 ? $"http://localhost:{args[1]}" : null;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(myPort, listenOptions =>
    {
        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2;
    });
});

builder.Services.AddGrpc();
var service = new ChainServiceImpl(nextServer, myPort);
builder.Services.AddSingleton(service);

var app = builder.Build();
app.MapGrpcService<ChainServiceImpl>();
app.MapGet("/", () => $"Chain node on port {myPort}");

_ = app.RunAsync();

Console.WriteLine("  send <message>");
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
        case "exit":
            Console.WriteLine("Shutting down...");
            Environment.Exit(0);
            break;

        case "send":
            if (parts.Length > 1)
            {
                await service.SendMessage(parts[1]);
            }
            break;
    }
}