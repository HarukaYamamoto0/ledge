// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace Ledger.Server;

public class LedgerConfig
{
    // Interval in seconds between snapshots
    public int IntervalSeconds { get; set; } = 60;

    // Optional custom output directory.
    // If empty, Ledger will use the game's ModData folder.
    public string BasePath { get; set; } = "";

    // Enable JSON file output
    public bool EnableJson { get; set; } = true;

    // Enable SQLite storage (not implemented yet)
    public bool EnableSqlite { get; set; } = false; // TODO: Implement later...
}