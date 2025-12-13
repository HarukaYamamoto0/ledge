// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Global

namespace Ledger.Core;

using System.Collections.Generic;

public class PlayerSnapshot
{
    public string Uid { get; set; } = "";
    public string Name { get; set; } = "";
    public bool Online { get; set; }

    public PlayerStats Stats { get; set; } = new();
    public PlayerEquipment Equipment { get; set; } = new();
    public PlayerWorldInfo World { get; set; } = new();

    public long FirstJoin { get; set; }
    public long LastJoin { get; set; }
}

public class PlayerStats
{
    public StatRange Health { get; set; } = new();
    public StatRange Hunger { get; set; } = new();

    public int Deaths { get; set; }
    public long PlaytimeSeconds { get; set; }
}

public class StatRange
{
    public double Current { get; set; }
    public double Max { get; set; }
}

public class PlayerEquipment
{
    // ReSharper disable once CollectionNeverQueried.Global
    public List<string> Armor { get; set; } = [];
    public string Weapon { get; set; } = "none";
}

public class PlayerWorldInfo
{
    public double AmbientTemperature { get; set; }
    public string ClimateTag { get; set; } = "unknown";
}