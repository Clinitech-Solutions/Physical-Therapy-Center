namespace PTCenter.Tests;

/// <summary>Scenario clock: tests state their clinic date instead of relying on real future-day arrival.</summary>
public sealed class TestClinicClock(DateTimeOffset initial) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = initial;
    public override DateTimeOffset GetUtcNow() => Now;
}
