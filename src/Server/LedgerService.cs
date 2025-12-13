using Ledger.Core;
using Ledger.Storage;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Ledger.Server;

public class LedgerService(
    ICoreServerAPI sapi,
    PlayerRegistry registry,
    IPlayerSnapshotProvider snapshotProvider,
    IReadOnlyList<IPlayerStorage> storages,
    JsonPlayerStorage? jsonStorage = null)
{
    public void OnPlayerJoin(IServerPlayer player)
    {
        var nowUtc = DateTime.UtcNow;
        var snap = registry.GetOrCreate(player.PlayerUID, player.PlayerName, DateUtil.NowUnix);

        if (jsonStorage != null && jsonStorage.TryLoad(player.PlayerUID, out var persisted))
        {
            registry.SeedFromPersisted(
                player.PlayerUID,
                persisted.Stats.Deaths,
                persisted.Stats.PlaytimeSeconds,
                persisted.FirstJoin
            );

            snap.FirstJoin = persisted.FirstJoin;
            snap.Stats.Deaths = persisted.Stats.Deaths;
            snap.Stats.PlaytimeSeconds = persisted.Stats.PlaytimeSeconds;
        }

        snap.Online = true;

        registry.MarkOnline(player.PlayerUID, nowUtc);

        SyncRuntimeStats(player.PlayerUID, snap, nowUtc);
        Persist(snap);
    }

    public void OnPlayerLeave(IServerPlayer player)
    {
        var nowUtc = DateTime.UtcNow;
        var nowUnix = DateUtil.NowUnix();

        var snap = registry.GetOrCreate(player.PlayerUID, player.PlayerName, DateUtil.NowUnix);

        snap.Online = false;
        snap.LastJoin = nowUnix;

        registry.MarkOffline(player.PlayerUID, nowUtc);
        SyncRuntimeStats(player.PlayerUID, snap, nowUtc);

        Persist(snap);
    }

    public void OnPlayerDeath(IServerPlayer byPlayer, DamageSource damageSource)
    {
        var nowUtc = DateTime.UtcNow;
        var nowUnix = DateUtil.NowUnix();

        registry.IncrementDeaths(byPlayer.PlayerUID);

        var snap = registry.GetOrCreate(byPlayer.PlayerUID, byPlayer.PlayerName, DateUtil.NowUnix);
        snap.LastJoin = nowUnix;

        SyncRuntimeStats(byPlayer.PlayerUID, snap, nowUtc);
        Persist(snap);
    }

    public void OnIntervalTick()
    {
        var nowUtc = DateTime.UtcNow;

        foreach (var player in sapi.World.AllOnlinePlayers.Cast<IServerPlayer>())
        {
            registry.MarkOnline(player.PlayerUID, nowUtc);

            var snapshot = snapshotProvider.CreateSnapshotFor(player.PlayerUID);

            SyncRuntimeStats(player.PlayerUID, snapshot, nowUtc);
            Persist(snapshot);
        }

        var onlineUids = new HashSet<string>(
            sapi.World.AllOnlinePlayers.Select(p => p.PlayerUID));

        foreach (var regSnap in registry.All)
        {
            if (onlineUids.Contains(regSnap.Uid)) continue;

            regSnap.Online = false;
            Persist(regSnap);
        }
    }

    private void SyncRuntimeStats(string uid, PlayerSnapshot snapshot, DateTime nowUtc)
    {
        snapshot.Stats.Deaths = registry.GetDeaths(uid);
        snapshot.Stats.PlaytimeSeconds = registry.GetPlaytimeSeconds(uid, nowUtc);
    }

    private void Persist(PlayerSnapshot snapshot)
    {
        if (string.IsNullOrWhiteSpace(snapshot.Name))
            snapshot.Name = "Unknown";

        foreach (var storage in storages)
            storage.SaveSnapshot(snapshot);
    }
}