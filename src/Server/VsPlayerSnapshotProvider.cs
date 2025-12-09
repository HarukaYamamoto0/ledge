using Ledger.Core;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;

namespace Ledger.Server;

public class VsPlayerSnapshotProvider(ICoreServerAPI sapi, PlayerRegistry registry) : IPlayerSnapshotProvider
{
    public PlayerSnapshot CreateSnapshotFor(string uid)
    {
        if (sapi.World.PlayerByUid(uid) is not IServerPlayer player)
        {
            // Unknown/offline player, returns a basic snapshot from the registry.
            var regSnap = registry.GetOrCreate(uid, "Unknown", NowDate);
            regSnap.Online = false;
            return regSnap;
        }

        var entity = player.Entity;

        var regSnapshot = registry.GetOrCreate(player.PlayerUID, player.PlayerName, NowDate);

        var snapshot = new PlayerSnapshot
        {
            Uid = player.PlayerUID,
            Name = player.PlayerName,
            Online = true,
            FirstJoin = regSnapshot.FirstJoin,
            LastJoin = NowDate()
        };

        FillStats(snapshot, entity);
        FillEquipment(snapshot, player);
        FillWorldInfo(snapshot, entity);

        return snapshot;
    }

    private static void FillStats(PlayerSnapshot snapshot, Entity entity)
    {
        var watched = entity.WatchedAttributes;

        // Health
        var health = watched?.GetTreeAttribute("health");
        snapshot.Stats.Health = new StatRange
        {
            Current = FiniteOrZero(health?.GetFloat("currenthealth")),
            Max = FiniteOrZero(health?.GetFloat("maxhealth"))
        };

        // Hunger (satiety)
        var hunger = watched?.GetTreeAttribute("hunger");
        snapshot.Stats.Hunger = new StatRange
        {
            Current = FiniteOrZero(hunger?.GetFloat("currentsaturation")),
            Max = FiniteOrZero(hunger?.GetFloat("maxsaturation"))
        };

        // Vanilla Stamina doesn't exist as a standalone bar,
        // so for now leave it at 0/0 to avoid misleading data.
        snapshot.Stats.Stamina = new StatRange
        {
            Current = 0,
            Max = 0
        };

        // Deaths / Playtime Hours: not currently tracked by Entity.
        // The idea is to leave this to the PlayerRegistry (events + accumulation).
    }

    private static float FiniteOrZero(float? value)
    {
        if (value == null) return 0f;
        var v = value.Value;
        return float.IsNaN(v) || float.IsInfinity(v) ? 0f : v;
    }


    private static float GetStatValue(EntityStats? stats, string code)
    {
        // EntityStats works with "shielded" values per key.
        return stats?.GetBlended(code) ?? 0f;
    }

    private static void FillEquipment(PlayerSnapshot snapshot, IServerPlayer player)
    {
        var equipment = new PlayerEquipment
        {
            Armor = [],
            Weapon = "none"
        };

        // Armor inventory
        var armorInv = player.InventoryManager.GetOwnInventory("armor");
        if (armorInv != null)
        {
            foreach (var slot in armorInv)
            {
                if (slot.Empty) continue;
                equipment.Armor.Add(slot.Itemstack.Collectible.Code?.ToShortString() ?? "unknown");
            }
        }

        // Hot bar / item in hand
        var hotbar = player.InventoryManager.GetHotbarInventory();
        var activeIndex = player.InventoryManager.ActiveHotbarSlotNumber;

        if (hotbar != null && activeIndex >= 0 && activeIndex < hotbar.Count)
        {
            var slot = hotbar[activeIndex];
            if (!slot.Empty)
            {
                equipment.Weapon = slot.Itemstack.Collectible.Code?.ToShortString() ?? "unknown";
            }
        }

        snapshot.Equipment = equipment;
    }

    private void FillWorldInfo(PlayerSnapshot snapshot, Entity entity)
    {
        var pos = entity.Pos.AsBlockPos;
        var climate = sapi.World.BlockAccessor.GetClimateAt(pos);

        snapshot.World = new PlayerWorldInfo
        {
            Temperature = climate?.Temperature ?? 0f,
            Biome = GetBiomeName(climate)
        };
    }

    private static string GetBiomeName(ClimateCondition? climate)
    {
        if (climate == null) return "unknown";

        var temp = climate.Temperature;
        var rain = climate.Rainfall;

        // Completely invented rules, just to give them cool names.
        return temp switch
        {
            >= 24 when rain >= 0.6f => "tropical",
            >= 18 when rain >= 0.4f => "temperate",
            <= 0f => "polar",
            _ => rain <= 0.2f ? "arid" : "unknown"
        };
    }

    private static string NowDate() => DateTime.UtcNow.ToString("yyyy'/'MM'/'dd HH':'mm':'ss");
}