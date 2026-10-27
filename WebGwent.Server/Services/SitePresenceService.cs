using Microsoft.AspNetCore.Components.Server.Circuits;

namespace WebGwent.Server.Services;

/// <summary>Counts active Blazor Server circuits (= browsers currently on the site).</summary>
public sealed class SitePresenceService
{
    private int _count;

    public int OnlineCount => Math.Max(0, Volatile.Read(ref _count));

    public event Action? Changed;

    internal void CircuitOpened()
    {
        Interlocked.Increment(ref _count);
        Changed?.Invoke();
    }

    internal void CircuitClosed()
    {
        Interlocked.Decrement(ref _count);
        Changed?.Invoke();
    }
}

public sealed class SitePresenceCircuitHandler(SitePresenceService presence) : CircuitHandler
{
    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        presence.CircuitOpened();
        return Task.CompletedTask;
    }

    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        presence.CircuitClosed();
        return Task.CompletedTask;
    }
}
