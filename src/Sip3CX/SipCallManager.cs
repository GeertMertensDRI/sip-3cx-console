using System.Collections.Concurrent;
using Sip3CX.Abstractions;
using Microsoft.Extensions.Logging;

namespace Sip3CX;

public sealed class SipCallManager : ISipCallManager
{
    private readonly ISipClient _client;
    private readonly ILogger<SipCallManager> _logger;
    private readonly ConcurrentDictionary<string, ISipCall> _calls = new();

    public IReadOnlyDictionary<string, ISipCall> ActiveCalls => _calls;
    public event EventHandler? CallsChanged;

    public SipCallManager(ISipClient client, ILogger<SipCallManager> logger)
    {
        _client = client;
        _logger = logger;
        _client.CallStateChanged += OnCallStateChanged;
    }

    public async Task<SipCallResult> DialAsync(string target, string browserSdpOffer, CancellationToken ct = default)
    {
        _logger.LogInformation("Dialling {Target}", target);
        var result = await _client.PlaceCallAsync(target, browserSdpOffer, ct);
        _calls[result.Call.CallId] = result.Call;
        CallsChanged?.Invoke(this, EventArgs.Empty);
        return result;
    }

    public async Task HangUpAsync(string callId, CancellationToken ct = default)
    {
        await _client.HangUpAsync(callId, ct);
        _calls.TryRemove(callId, out _);
        CallsChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task HangUpAllAsync(CancellationToken ct = default)
    {
        foreach (var id in _calls.Keys.ToList())
            await HangUpAsync(id, ct);
    }

    private void OnCallStateChanged(object? sender, CallStateChangedEventArgs e)
    {
        if (e.State is SipCallState.Ended or SipCallState.Failed)
        {
            _calls.TryRemove(e.CallId, out _);
            CallsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await HangUpAllAsync();
        foreach (var call in _calls.Values)
            await call.DisposeAsync();
    }
}
