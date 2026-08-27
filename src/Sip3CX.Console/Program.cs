using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sip3CX;
using Sip3CX.Abstractions;
using Sip3CX.ConsoleApp;

// ── Configuration ─────────────────────────────────────────────────────────────
var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables()
    .Build();

var settings = config.GetSection("Sip").Get<SipSettings>()
               ?? throw new InvalidOperationException("Missing 'Sip' config section.");

var transport = Enum.Parse<SipTransport>(settings.Transport, ignoreCase: true);

// ���─ Dependency Injection ───────────────────────────────────────────────────────
var services = new ServiceCollection()
    .AddLogging(b => b
        .AddConsole()
        .SetMinimumLevel(LogLevel.Information))
    .AddSip3CxServices(transport)
    .BuildServiceProvider();

var logger      = services.GetRequiredService<ILogger<Program>>();
var sipClient   = services.GetRequiredService<ISipClient>();
var callManager = services.GetRequiredService<ISipCallManager>();

// ── Wire up global events ──────────────────────────────────────────────────────
sipClient.RegistrationStateChanged += (_, e) =>
    Console.WriteLine($"[Registration] {e.State}{(e.Reason != null ? $" — {e.Reason}" : "")}");

sipClient.IncomingCall += (_, e) =>
    Console.WriteLine($"[Incoming Call] From: {e.CallerUri}  CallId: {e.CallId}");

callManager.CallsChanged += (_, _) =>
    Console.WriteLine($"[Calls] Active: {callManager.ActiveCalls.Count}");

// ── Register with 3CX ─────────────────────────────────────────────────────────
var credentials = new SipCredentials(
    settings.Username,
    settings.Password,
    settings.Domain,
    settings.Port,
    transport);

await sipClient.RegisterAsync(credentials);

// Wait for registration
Console.WriteLine("Waiting for registration (up to 10 s)…");
using var regCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
try
{
    await Task.Run(async () =>
    {
        while (sipClient.State != SipClientState.Registered)
        {
            regCts.Token.ThrowIfCancellationRequested();
            await Task.Delay(200, regCts.Token);
        }
    }, regCts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Registration timed out. Check your 3CX credentials/network.");
    return 1;
}

// ── Interactive menu ───────────────────────────────────────────────────────────
PrintHelp();

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

while (!cts.IsCancellationRequested)
{
    Console.Write("> ");
    var line = Console.ReadLine();
    if (line is null) break;

    var parts = line.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
    if (parts.Length == 0) continue;

    switch (parts[0].ToLower())
    {
        case "call" when parts.Length == 2:
            await Dial(parts[1]);
            break;

        case "hangup" when parts.Length == 2:
            await callManager.HangUpAsync(parts[1]);
            Console.WriteLine($"Hung up {parts[1]}");
            break;

        case "hangall":
            await callManager.HangUpAllAsync();
            break;

        case "hold" when parts.Length == 2:
            await WithCall(parts[1], c => c.HoldAsync());
            break;

        case "resume" when parts.Length == 2:
            await WithCall(parts[1], c => c.ResumeAsync());
            break;

        case "dtmf" when parts.Length == 2 && parts[1].Length == 1:
            var lastCall = callManager.ActiveCalls.Values.FirstOrDefault();
            if (lastCall != null) await lastCall.SendDtmfAsync(parts[1][0]);
            else Console.WriteLine("No active call.");
            break;

        case "transfer" when parts.Length == 2:
            var active = callManager.ActiveCalls.Values.FirstOrDefault();
            if (active != null) await active.TransferAsync(parts[1]);
            else Console.WriteLine("No active call.");
            break;

        case "list":
            if (callManager.ActiveCalls.Count == 0)
                Console.WriteLine("No active calls.");
            foreach (var (id, c) in callManager.ActiveCalls)
                Console.WriteLine($"  {id}  {c.RemoteParty}  {c.State}  {c.StartedAt:HH:mm:ss}");
            break;

        case "status":
            Console.WriteLine($"SIP state: {sipClient.State}");
            break;

        case "quit":
        case "exit":
            cts.Cancel();
            break;

        default:
            PrintHelp();
            break;
    }
}

// ── Cleanup ────────────────────────────────────────────────────────────────────
await sipClient.UnregisterAsync();
await callManager.DisposeAsync();
await sipClient.DisposeAsync();
Console.WriteLine("Goodbye.");
return 0;

// ── Helpers ────────────────────────────────────────────────────────────────────
async Task Dial(string target)
{
    try
    {
        var call = await callManager.DialAsync(target);
        Console.WriteLine($"Call started. CallId={call.CallId}  State={call.State}");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to place call to {Target}", target);
    }
}

async Task WithCall(string callId, Func<ISipCall, Task> action)
{
    if (callManager.ActiveCalls.TryGetValue(callId, out var call))
        await action(call);
    else
        Console.WriteLine($"Call {callId} not found.");
}

void PrintHelp()
{
    Console.WriteLine("""
    ┌─ 3CX SIP Console ────────────────────────────────────────────┐
    │  call <sip:ext@host>   Place a call                          │
    │  hangup <callId>       Hang up a specific call               │
    │  hangall               Hang up all calls                     │
    │  hold <callId>         Put call on hold                      │
    │  resume <callId>       Resume held call                      │
    │  dtmf <digit>          Send DTMF on the first active call    │
    │  transfer <sip:...>    Blind transfer first active call      │
    │  list                  List active calls                     │
    │  status                Show SIP registration status         │
    │  quit / exit           Unregister and exit                   │
    └──────────────────────────────────────────────────────────────┘
    """);
}
