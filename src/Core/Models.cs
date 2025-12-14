// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Global

namespace Ledger.Core;

using System.Collections.Generic;

public class PlayerSnapshot
{
    public int SchemaVersion { get; set; } = 2;

    public string Uid { get; set; } = "";
    public string Name { get; set; } = "";
    public bool Online { get; set; }

    public PlayerMeta Meta { get; set; } = new();
    public PlayerLocation Location { get; set; } = new();

    public PlayerStats Stats { get; set; } = new();
    public PlayerEquipment Equipment { get; set; } = new();
    public PlayerWorldInfo World { get; set; } = new();
}

public class PlayerStats
{
    public StatRange Health { get; set; } = new();
    public StatRange Hunger { get; set; } = new();

    public int Deaths { get; set; }
    public long PlaytimeSeconds { get; set; }
    public int? PingMs { get; set; }
    public List<string> Privileges { get; set; } = [];
    public double? Tiredness { get; set; }
}

public class StatRange
{
    public double Current { get; set; }
    public double Max { get; set; }
}

public class PlayerEquipment
{
    public List<string> Armor { get; set; } = new(3);
    public string HeldItem { get; set; } = "none";
    public List<string> Hotbar { get; set; } = new(10);
}

public class PlayerWorldInfo
{
    public double AmbientTemperature { get; set; }
    public string ClimateTag { get; set; } = "unknown";
}

public class PlayerMeta
{
    public long FirstJoin { get; set; }
    public long LastJoin { get; set; }
    public long LastSeen { get; set; }
}

public class PlayerLocation
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Z { get; set; }
}