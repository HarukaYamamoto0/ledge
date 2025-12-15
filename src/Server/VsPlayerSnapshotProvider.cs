using Ledger.Core;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;

namespace Ledger.Server;

public class VsPlayerSnapshotProvider(ICoreServerAPI sapi, PlayerRegistry registry, LedgerConfig config)
    : IPlayerSnapshotProvider
{
    private static readonly int[] ArmorSlots = [12, 13, 14];

    private CaptureConfig _config = config.Capture;

    public PlayerSnapshot CreateSnapshotFor(string uid)
    {
        var snap = registry.GetOrCreate(uid, "", DateUtil.NowUnix);

        if (sapi.World.PlayerByUid(uid) is not IServerPlayer player)
        {
            snap.Online = false;
            return snap;
        }

        snap.Uid = player.PlayerUID;
        snap.Name = player.PlayerName;
        snap.Online = true;

        var entity = player.Entity;
        var cap = _config;

        if (cap.Vitals) FillVitalsOrKeepPrevious(snap, entity);
        if (cap.Tiredness) FillTirednessOrKeepPrevious(snap, entity);

        if (cap.Ping) FillPingOrKeepPrevious(snap, player);
        if (cap.Privileges) FillPrivilegesOrKeepPrevious(snap, player);

        if (cap.Equipment) FillEquipmentOrKeepPrevious(snap, player, cap.Hotbar);
        if (cap.Location) FillLocationOrKeepPrevious(snap, entity);
        if (cap.World) FillWorldOrKeepPrevious(snap, entity);

        return snap;
    }

    public void UpdateCapture(CaptureConfig capture)
    {
        _config = capture;
    }

    private static void FillVitalsOrKeepPrevious(PlayerSnapshot snapshot, Entity entity)
    {
        var watched = entity.WatchedAttributes;
        if (watched == null) return;

        var health = watched.GetTreeAttribute("health");
        if (health != null)
        {
            snapshot.Stats.Health = new StatRange
            {
                Current = FiniteOrZero(health.GetFloat("currenthealth")),
                Max = FiniteOrZero(health.GetFloat("maxhealth"))
            };
        }

        var hunger = watched.GetTreeAttribute("hunger");
        if (hunger != null)
        {
            snapshot.Stats.Hunger = new StatRange
            {
                Current = FiniteOrZero(hunger.GetFloat("currentsaturation")),
                Max = FiniteOrZero(hunger.GetFloat("maxsaturation"))
            };
        }
    }

    private static void FillTirednessOrKeepPrevious(PlayerSnapshot snapshot, Entity entity)
    {
        var watched = entity.WatchedAttributes;
        if (watched == null) return;

        try
        {
            var t = watched.GetFloat("tiredness", float.NaN);
            if (float.IsNaN(t) || float.IsInfinity(t)) return;
            snapshot.Stats.Tiredness = t;
        }
        catch
        {
            // ignored
        }
    }

    private static void FillPingOrKeepPrevious(PlayerSnapshot snapshot, IServerPlayer player)
    {
        try
        {
            var pingSeconds = player.Ping;
            if (float.IsNaN(pingSeconds) || float.IsInfinity(pingSeconds) || pingSeconds < 0) return;
            snapshot.Stats.PingMs = (int)Math.Round(pingSeconds * 1000f);
        }
        catch
        {
            // ignored
        }
    }

    private static void FillPrivilegesOrKeepPrevious(PlayerSnapshot snapshot, IPlayer player)
    {
        try
        {
            var privileges = player.Privileges;
            if (privileges == null || privileges.Length == 0) return;

            snapshot.Stats.Privileges = privileges
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch
        {
            // ignored
        }
    }

    private static void FillEquipmentOrKeepPrevious(PlayerSnapshot snapshot, IServerPlayer player, bool includeHotbar)
    {
        var invManager = player.InventoryManager;
        if (invManager == null) return;

        var equipment = snapshot.Equipment;

        try
        {
            var inv = invManager.GetOwnInventory("character");
            if (inv is { Count: > 0 })
            {
                var armor = (from slot in inv
                    where slot?.Empty == false
                    let code = slot.Itemstack?.Collectible?.Code?.ToShortString()
                    where !string.IsNullOrWhiteSpace(code)
                    where LooksWearable(code, slot.Itemstack?.Collectible)
                    select code).ToList();

                if (armor.Count > 0)
                    equipment.Armor = armor;
            }
        }
        catch
        {
            // ignored
        }

        try
        {
            var hotbar = invManager.GetHotbarInventory();
            if (hotbar is { Count: > 0 })
            {
                var activeIndex = invManager.ActiveHotbarSlotNumber;

                if (activeIndex >= 0 && activeIndex < hotbar.Count && hotbar[activeIndex]?.Empty == false)
                {
                    var held = hotbar[activeIndex].Itemstack?.Collectible?.Code?.ToShortString();
                    if (!string.IsNullOrWhiteSpace(held))
                        equipment.HeldItem = held;
                }

                if (includeHotbar)
                    equipment.Hotbar = ReadHotbarFixed10(hotbar);
            }
        }
        catch
        {
            // ignored
        }

        snapshot.Equipment = equipment;
    }

    private static List<string> ReadHotbarFixed10(IInventory hotbar)
    {
        var list = new List<string>(10);
        var count = Math.Min(10, hotbar.Count);

        for (var i = 0; i < count; i++)
        {
            var slot = hotbar[i];
            var code = slot?.Empty != false
                ? "none"
                : slot.Itemstack?.Collectible?.Code?.ToShortString() ?? "unknown";

            list.Add(code);
        }

        while (list.Count < 10) list.Add("none");
        return list;
    }

    private void FillLocationOrKeepPrevious(PlayerSnapshot snapshot, Entity entity)
    {
        var pos = entity.Pos?.AsBlockPos;
        if (pos == null) return;

        var halfX = sapi.WorldManager.MapSizeX / 2;
        var halfZ = sapi.WorldManager.MapSizeZ / 2;

        snapshot.Location = new PlayerLocation
        {
            X = pos.X - halfX,
            Y = pos.Y,
            Z = pos.Z - halfZ
        };
    }

    private void FillWorldOrKeepPrevious(PlayerSnapshot snapshot, Entity entity)
    {
        var pos = entity.Pos?.AsBlockPos;
        if (pos == null) return;

        try
        {
            var climate = sapi.World.BlockAccessor.GetClimateAt(pos);
            if (climate == null) return;

            snapshot.World = new PlayerWorldInfo
            {
                AmbientTemperature = climate.Temperature,
                ClimateTag = GetClimateTag(climate)
            };
        }
        catch
        {
            // ignored
        }
    }

    private static double FiniteOrZero(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value)) return 0d;
        return value;
    }

    private static string GetClimateTag(ClimateCondition climate)
    {
        var temp = climate.Temperature;
        var rain = climate.Rainfall;

        return temp switch
        {
            >= 24 when rain >= 0.6f => "tropical",
            >= 18 when rain >= 0.4f => "temperate",
            <= 0f => "winter",
            _ => rain <= 0.2f ? "arid" : "unknown"
        };
    }

    private static bool LooksWearable(string code, CollectibleObject? collectible)
    {
        if (code.Contains("armor", StringComparison.OrdinalIgnoreCase)) return true;
        if (code.Contains("clothes", StringComparison.OrdinalIgnoreCase)) return true;

        var tn = collectible?.GetType().Name;
        return !string.IsNullOrWhiteSpace(tn) && tn.Contains("Wearable", StringComparison.OrdinalIgnoreCase);
    }
}