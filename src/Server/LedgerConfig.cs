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

    public CaptureConfig Capture { get; set; } = new();
}

public class CaptureConfig
{
    public bool Vitals { get; set; } = true;
    public bool Tiredness { get; set; } = false;
    public bool Ping { get; set; } = true;
    public bool Privileges { get; set; } = true;
    public bool Location { get; set; } = true;
    public bool World { get; set; } = true;
    public bool Equipment { get; set; } = true;
    public bool Hotbar { get; set; } = false;
}