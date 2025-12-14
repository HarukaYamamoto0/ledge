using Ledger.Core;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;

namespace Ledger.Server;

public class VsPlayerSnapshotProvider(ICoreServerAPI sapi, PlayerRegistry registry) : IPlayerSnapshotProvider
{
    private static readonly int[] ArmorSlots = [12, 13, 14];

    public PlayerSnapshot CreateSnapshotFor(string uid)
    {
        // Always start from a registry snapshot to avoid accidental resets
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

        FillVitalsOrKeepPrevious(snap, entity);
        FillTirednessOrKeepPrevious(snap, entity);

        FillPingOrKeepPrevious(snap, player);
        FillPrivilegesOrKeepPrevious(snap, player);

        FillEquipmentOrKeepPrevious(snap, player);
        FillLocationOrKeepPrevious(snap, entity);
        FillWorldOrKeepPrevious(snap, entity);

        return snap;
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
            Nothing();
        }
    }

    private static void FillPingOrKeepPrevious(PlayerSnapshot snapshot, IServerPlayer player)
    {
        try
        {
            // Ping is in seconds; NaN is not connected.
            var pingSeconds = player.Ping;
            if (float.IsNaN(pingSeconds) || float.IsInfinity(pingSeconds) || pingSeconds < 0) return;

            snapshot.Stats.PingMs = (int)Math.Round(pingSeconds * 1000f);
        }
        catch
        {
            Nothing();
        }
    }

    private static void FillPrivilegesOrKeepPrevious(PlayerSnapshot snapshot, IPlayer player)
    {
        try
        {
            // On server this is generally available. If it comes empty/null, we keep previous.
            var privileges = player.Privileges;
            if (privileges == null || privileges.Length == 0) return;

            // Normalize + remove duplicates
            snapshot.Stats.Privileges = privileges
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch
        {
            Nothing();
        }
    }

    private static void FillEquipmentOrKeepPrevious(PlayerSnapshot snapshot, IServerPlayer player)
    {
        var equipment = snapshot.Equipment;

        // Armor
        var inv = player.InventoryManager.GetOwnInventory("character");
        if (inv != null)
        {
            var armor = new List<string>(3);

            foreach (var idx in ArmorSlots)
            {
                if (idx < 0 || idx >= inv.Count || inv[idx].Empty)
                {
                    armor.Add("none");
                    continue;
                }

                armor.Add(inv[idx].Itemstack?.Collectible?.Code?.ToShortString() ?? "unknown");
            }

            equipment.Armor = armor;
        }

        // Hotbar (fixed 10)
        var hotbar = player.InventoryManager.GetHotbarInventory();
        if (hotbar != null)
        {
            var list = new List<string>(10);

            var count = Math.Min(10, hotbar.Count);
            for (var i = 0; i < count; i++)
            {
                var slot = hotbar[i];
                list.Add(slot.Empty
                    ? "none"
                    : (slot.Itemstack?.Collectible?.Code?.ToShortString() ?? "unknown"));
            }

            while (list.Count < 10) list.Add("none");

            equipment.Hotbar = list;

            // HeldItem: active slot
            var activeIndex = player.InventoryManager.ActiveHotbarSlotNumber;
            if (activeIndex >= 0 &&
                activeIndex < hotbar.Count &&
                !hotbar[activeIndex].Empty)
            {
                equipment.HeldItem =
                    hotbar[activeIndex].Itemstack?.Collectible?.Code?.ToShortString() ?? "unknown";
            }
            // If active slot invalid/empty, keep the previous HeldItem
        }

        snapshot.Equipment = equipment;
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

        ClimateCondition? climate = null;

        try
        {
            climate = sapi.World.BlockAccessor.GetClimateAt(pos);
        }
        catch
        {
            Nothing();
        }

        if (climate == null) return;

        snapshot.World = new PlayerWorldInfo
        {
            AmbientTemperature = climate.Temperature,
            ClimateTag = GetClimateTag(climate)
        };
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

    private static void Nothing()
    {
        // Nothing to do here
    }
}