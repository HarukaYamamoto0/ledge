namespace Ledger.Core;

public interface IPlayerSnapshotProvider
{
    PlayerSnapshot CreateSnapshotFor(string uid);
}

public interface IPlayerStorage
{
    void SaveSnapshot(PlayerSnapshot snapshot);
}