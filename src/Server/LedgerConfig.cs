// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
namespace Ledger.Server;

public class LedgerConfig
{
    public int IntervalSeconds { get; set; } = 60;
    public string BasePath { get; set; } = "./Mods/LedgerData";
    public bool EnableJson { get; set; } = true;
    public bool EnableSqlite { get; set; } = false; // TODO: implement
}