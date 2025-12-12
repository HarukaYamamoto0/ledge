using Ledger.Core;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;

namespace Ledger.Server;

public class LedgerService(
    ICoreServerAPI sapi,
    PlayerRegistry registry,
    IPlayerSnapshotProvider snapshotProvider,
    IReadOnlyList<IPlayerStorage> storages)
{
    public void OnPlayerJoin(IServerPlayer player)
    {
        var nowDate = NowDate();
        var nowUtc = DateTime.UtcNow;

        var snap = registry.GetOrCreate(player.PlayerUID, player.PlayerName, () => nowDate);
        snap.Online = true;
        snap.LastJoin = nowDate;

        registry.MarkOnline(player.PlayerUID, nowUtc);

        Persist(snap);
    }

    public void OnPlayerLeave(IServerPlayer player)
    {
        var nowDate = NowDate();
        var nowUtc = DateTime.UtcNow;

        var snap = registry.GetOrCreate(player.PlayerUID, player.PlayerName, () => nowDate);
        snap.Online = false;
        snap.LastJoin = nowDate;

        registry.MarkOffline(player.PlayerUID, nowUtc);
        snap.Stats.PlaytimeHours = registry.GetPlaytimeHours(player.PlayerUID, nowUtc);

        Persist(snap);
    }

    public void OnPlayerDeath(IServerPlayer byPlayer, DamageSource damageSource)
    {
        registry.IncrementDeaths(byPlayer.PlayerUID);

        var snap = registry.GetOrCreate(byPlayer.PlayerUID, byPlayer.PlayerName, NowDate);
        snap.Stats.Deaths = registry.GetDeaths(byPlayer.PlayerUID);
        snap.Stats.PlaytimeHours = registry.GetPlaytimeHours(byPlayer.PlayerUID, DateTime.UtcNow);

        Persist(snap);
    }

    public void OnIntervalTick()
    {
        var nowUtc = DateTime.UtcNow;

        foreach (var player in sapi.World.AllOnlinePlayers.Cast<IServerPlayer>())
        {
            registry.MarkOnline(player.PlayerUID, nowUtc);

            var snapshot = snapshotProvider.CreateSnapshotFor(player.PlayerUID);
            snapshot.Stats.PlaytimeHours = registry.GetPlaytimeHours(player.PlayerUID, nowUtc);

            Persist(snapshot);
        }

        foreach (var regSnap in registry.All)
        {
            if (sapi.World.AllOnlinePlayers.Any(p => p.PlayerUID == regSnap.Uid)) continue;
            regSnap.Online = false;
            Persist(regSnap);
        }
    }

    private void Persist(PlayerSnapshot snapshot)
    {
        foreach (var storage in storages)
        {
            storage.SaveSnapshot(snapshot);
        }
    }

    private static string NowDate() => DateTime.UtcNow.ToString("yyyy-MM-dd");
}