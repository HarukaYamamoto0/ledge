// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

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

    public string FirstJoin { get; set; } = "";
    public string LastJoin { get; set; } = "";
}

public class PlayerStats
{
    public StatRange Health { get; set; } = new();
    public StatRange Hunger { get; set; } = new();
    public StatRange Stamina { get; set; } = new();

    public int Deaths { get; set; } = 0;
    public double PlaytimeHours { get; set; } = 0;
}

public class StatRange
{
    public double Current { get; set; }
    public double Max { get; set; }
}

public class PlayerEquipment
{
    public List<string> Armor { get; set; } = [];
    public string Weapon { get; set; } = "none";
}

public class PlayerWorldInfo
{
    public string Biome { get; set; } = "unknown";
    public double Temperature { get; set; }
}