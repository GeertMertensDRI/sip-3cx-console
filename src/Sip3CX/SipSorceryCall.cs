using Sip3CX.Abstractions;
using SIPSorcery.SIP.App;
using Microsoft.Extensions.Logging;

namespace Sip3CX;

internal sealed class SipSorceryCall : ISipCall
{
    private readonly SIPUserAgent _ua;
    private readonly ILogger<SipSorceryCall> _logger;

    public string       CallId      { get; }
    public SipCallState State       { get; private set; } = SipCallState.Initiating;
    public string       RemoteParty { get; }
    public DateTimeOffset StartedAt { get; } = DateTimeOffset.UtcNow;

    public SipSorceryCall(SIPUserAgent ua, string callId, string remoteParty, ILogger<SipSorceryCall> logger)
    {
        _ua         = ua;
        CallId      = callId;
        RemoteParty = remoteParty;
        _logger     = logger;

        _ua.OnCallHungup += _ =>
        {
            State = SipCallState.Ended;
            _logger.LogInformation("[{CallId}] Call hung up.", CallId);
        };
    }

    public void MarkConnected() => State = SipCallState.Connected;
    public void MarkFailed()    => State = SipCallState.Failed;

    public async Task HoldAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[{CallId}] Placing on hold.", CallId);
        await _ua.PutOnHold();
        State = SipCallState.OnHold;
    }

    public async Task ResumeAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[{CallId}] Resuming from hold.", CallId);
        await _ua.TakeOffHold();
        State = SipCallState.Connected;
    }

    public async Task HangUpAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[{CallId}] Hanging up.", CallId);
        _ua.Hangup();
        State = SipCallState.Ended;
        await Task.CompletedTask;
    }

    public Task SendDtmfAsync(char digit, CancellationToken ct = default)
    {
        _logger.LogInformation("[{CallId}] Sending DTMF: {Digit}", CallId, digit);
        // SIPSorcery supports DTMF via RTP or INFO
        return _ua.SendDtmf((byte)(digit - '0'));
    }

    public Task TransferAsync(string target, CancellationToken ct = default)
    {
        _logger.LogInformation("[{CallId}] Transferring to {Target}", CallId, target);
        return _ua.BlindTransfer(target, TimeSpan.FromSeconds(5), ct);
    }

    public ValueTask DisposeAsync()
    {
        if (State == SipCallState.Connected || State == SipCallState.OnHold)
            _ua.Hangup();
        return ValueTask.CompletedTask;
    }
}
