using Sip3CX.Abstractions;
using SIPSorcery.SIP;
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

    public Task HoldAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[{CallId}] Placing on hold.", CallId);
        _ua.PutOnHold();
        State = SipCallState.OnHold;
        return Task.CompletedTask;
    }

    public Task ResumeAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[{CallId}] Resuming from hold.", CallId);
        _ua.TakeOffHold();
        State = SipCallState.Connected;
        return Task.CompletedTask;
    }

    public Task HangUpAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[{CallId}] Hanging up.", CallId);
        _ua.Hangup();
        State = SipCallState.Ended;
        return Task.CompletedTask;
    }

    public Task SendDtmfAsync(char digit, CancellationToken ct = default)
    {
        _logger.LogInformation("[{CallId}] Sending DTMF: {Digit}", CallId, digit);
        _ua.SendDtmf((byte)(digit - '0'));
        return Task.CompletedTask;
    }

    public Task TransferAsync(string target, CancellationToken ct = default)
    {
        _logger.LogInformation("[{CallId}] Transferring to {Target}", CallId, target);
        var uri = SIPURI.ParseSIPURI(target);
        return _ua.BlindTransfer(uri, TimeSpan.FromSeconds(5), ct);
    }

    public ValueTask DisposeAsync()
    {
        if (State == SipCallState.Connected || State == SipCallState.OnHold)
            _ua.Hangup();
        return ValueTask.CompletedTask;
    }
}
