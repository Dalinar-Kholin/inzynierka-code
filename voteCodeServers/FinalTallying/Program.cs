using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;

var cfg = VoteCodeServers.Helpers.Config.Load();
int numberOfCandidates = cfg.NumberOfCandidates;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5000);
});

builder.Services.AddSingleton<AuthCodeQueueService>(_ => new AuthCodeQueueService(numberOfCandidates));

var app = builder.Build();

// curl -X POST http://localhost:5000/api/forCounting -d 'authCodeValue'
app.MapPost("/api/forCounting", async (HttpRequest request, AuthCodeQueueService queueService) =>
{
    // try to read authCode from body as plain text
    string authCode;
    using (var reader = new StreamReader(request.Body))
    {
        authCode = await reader.ReadToEndAsync();
    }

    if (string.IsNullOrEmpty(authCode))
    {
        return Results.BadRequest(new { error = "AuthCode is required" });
    }

    // add to queue
    queueService.Enqueue(authCode);
    return Results.Accepted(null, new
    {
        message = "AuthCode queued for final tallying",
        authCode = authCode,
    });
});

app.Run();


