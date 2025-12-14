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
        var nowUnix = DateUtil.NowUnix();

        var snap = registry.GetOrCreate(player.PlayerUID, player.PlayerName, DateUtil.NowUnix);

        // Seed persisted stats/meta (only once, on join)
        if (jsonStorage != null && jsonStorage.TryLoad(player.PlayerUID, out var persisted))
        {
            registry.SeedFromPersisted(
                player.PlayerUID,
                persisted.Stats.Deaths,
                persisted.Stats.PlaytimeSeconds,
                persisted.Meta.FirstJoin
            );

            // Keep canonical meta from a persisted file
            snap.Meta.FirstJoin = persisted.Meta.FirstJoin;
            snap.Meta.LastJoin = persisted.Meta.LastJoin; // will be overwritten just below
        }

        snap.Online = true;
        snap.Meta.LastJoin = nowUnix;
        snap.Meta.LastSeen = nowUnix;

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
        snap.Meta.LastSeen = nowUnix;

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

        // Death is a "seen" event
        snap.Meta.LastSeen = nowUnix;

        SyncRuntimeStats(byPlayer.PlayerUID, snap, nowUtc);
        Persist(snap);
    }

    public void OnIntervalTick()
    {
        var nowUtc = DateTime.UtcNow;
        var nowUnix = DateUtil.NowUnix();

        // Online players: create fresh snapshots via provider
        foreach (var player in sapi.World.AllOnlinePlayers.Cast<IServerPlayer>())
        {
            registry.MarkOnline(player.PlayerUID, nowUtc);

            var snapshot = snapshotProvider.CreateSnapshotFor(player.PlayerUID);
            snapshot.Online = true;
            snapshot.Meta.LastSeen = nowUnix;

            SyncRuntimeStats(player.PlayerUID, snapshot, nowUtc);
            Persist(snapshot);
        }

        // Offline players: persist registry snapshots without wiping fields
        var onlineUids = new HashSet<string>(sapi.World.AllOnlinePlayers.Select(p => p.PlayerUID));

        foreach (var regSnap in registry.All)
        {
            if (onlineUids.Contains(regSnap.Uid)) continue;

            regSnap.Online = false;

            // Do NOT bump LastSeen for offline players here,
            // otherwise consumers can't tell how old the data is.
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
        // Sanitization guards (prevents null poisoning and keeps JSON stable)
        if (string.IsNullOrWhiteSpace(snapshot.Name))
            snapshot.Name = "Unknown";

        // Ensure meta exists and has sane values

        // If FirstJoin is missing (e.g., legacy file), set it at first persist
        if (snapshot.Meta.FirstJoin <= 0)
            snapshot.Meta.FirstJoin = DateUtil.NowUnix();

        foreach (var storage in storages)
            storage.SaveSnapshot(snapshot);
    }

    public void UpdateCaptureConfig(CaptureConfig capture)
    {
        snapshotProvider.UpdateCapture(capture);
    }
}