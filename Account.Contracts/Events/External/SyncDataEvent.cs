namespace Account.Contracts.Events.External;

/// <summary>
/// Sync data with an external system
/// </summary>
public class SyncDataEvent
{
    public int BatchSize { get; set; } = 100;
    public int Offset { get; set; } = 0;
}