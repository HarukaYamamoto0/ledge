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
        var baseSnap = registry.GetOrCreate(uid, "Unknown", DateUtil.NowUnix);

        if (sapi.World.PlayerByUid(uid) is not IServerPlayer player)
        {
            baseSnap.Online = false;
            return baseSnap;
        }

        baseSnap.Uid = player.PlayerUID;
        baseSnap.Name = player.PlayerName;
        baseSnap.Online = true;

        var entity = player.Entity;

        // Stats we can safely read (health/hunger) overwrite; others stay as-is (deaths/playtime from registry)
        FillVitalsOrKeepPrevious(baseSnap, entity);

        // Equipment overwrite (safe)
        FillEquipment(baseSnap, player);

        // World overwrite only if available; otherwise keep previous
        FillWorldOrKeepPrevious(baseSnap, entity);

        return baseSnap;
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

        // Do NOT touch Deaths / PlaytimeSeconds here, registry is the source
    }

    private static double FiniteOrZero(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value)) return 0d;
        return value;
    }

    private static void FillEquipment(PlayerSnapshot snapshot, IServerPlayer player)
    {
        var armor = new List<string>(3);

        var inv = player.InventoryManager.GetOwnInventory("character");
        if (inv != null)
        {
            foreach (var idx in ArmorSlots)
            {
                if (idx < 0 || idx >= inv.Count)
                {
                    armor.Add("none");
                    continue;
                }

                var slot = inv[idx];
                if (slot.Empty)
                {
                    armor.Add("none");
                    continue;
                }

                armor.Add(slot.Itemstack?.Collectible?.Code?.ToShortString() ?? "unknown");
            }
        }
        else
        {
            armor.AddRange(["none", "none", "none"]);
        }

        var weapon = "none";

        var hotbar = player.InventoryManager.GetHotbarInventory();
        var activeIndex = player.InventoryManager.ActiveHotbarSlotNumber;

        if (hotbar != null &&
            activeIndex >= 0 &&
            activeIndex < hotbar.Count &&
            !hotbar[activeIndex].Empty)
        {
            weapon = hotbar[activeIndex].Itemstack.Collectible.Code?.ToShortString() ?? "unknown";
        }

        snapshot.Equipment = new PlayerEquipment
        {
            Armor = armor,
            HeldItem = weapon
        };
    }

    private void FillWorldOrKeepPrevious(PlayerSnapshot snapshot, Entity entity)
    {
        var pos = entity.Pos?.AsBlockPos;
        if (pos == null) return; // keep existing

        ClimateCondition? climate = null;

        try
        {
            climate = sapi.World.BlockAccessor.GetClimateAt(pos);
        }
        catch
        {
            // keep existing
        }

        if (climate == null) return; // keep existing

        snapshot.World = new PlayerWorldInfo
        {
            AmbientTemperature = climate.Temperature,
            ClimateTag = GetClimateTag(climate)
        };
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
}