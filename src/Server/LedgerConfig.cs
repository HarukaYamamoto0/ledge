// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace Ledger.Server;

public class LedgerConfig
{
    // Interval in seconds between snapshots
    public int IntervalSeconds { get; set; } = Constants.DefaultIntervalSeconds;

    // Optional custom output directory.
    // If empty, Ledger will use the game's ModData folder.
    public string BasePath { get; set; } = "";

    // Enable JSON file output
    public bool EnableJson { get; set; } = true;
}