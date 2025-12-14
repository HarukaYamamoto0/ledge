using Ledger.Server;

namespace Ledger.Core;

public interface IPlayerSnapshotProvider
{
    PlayerSnapshot CreateSnapshotFor(string uid);
    void UpdateCapture(CaptureConfig capture);
}

public interface IPlayerStorage
{
    void SaveSnapshot(PlayerSnapshot snapshot);
}