using Ledger.Core;
using Vintagestory.API.Common;
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
        var now = NowDate();
        var snap = registry.GetOrCreate(player.PlayerUID, player.PlayerName, () => now);
        snap.Online = true;
        snap.LastJoin = now;

        Persist(snap);
    }

    public void OnPlayerLeave(IServerPlayer player)
    {
        var now = NowDate();
        var snap = registry.GetOrCreate(player.PlayerUID, player.PlayerName, () => now);
        snap.Online = false;
        snap.LastJoin = now;

        Persist(snap);
    }

    public void OnIntervalTick()
    {
        // Updates online player data
        foreach (var player in sapi.World.AllOnlinePlayers.Cast<IServerPlayer>())
        {
            var snapshot = snapshotProvider.CreateSnapshotFor(player.PlayerUID);
            Persist(snapshot);
            sapi.Logger.Log(EnumLogType.Event, "Ledger: " + snapshot.Name + " (" + snapshot.Uid + "");
        }

        // Ensures that everyone in the registry has a file (online=false if they don't).
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