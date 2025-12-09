using Ledger.Core;
using Newtonsoft.Json;

namespace Ledger.Storage;

public class JsonPlayerStorage(string basePath) : IPlayerStorage
{
    public void SaveSnapshot(PlayerSnapshot snapshot)
    {
        var playerDir = Path.Combine(basePath, snapshot.Uid);
        Directory.CreateDirectory(playerDir);

        var filePath = Path.Combine(playerDir, Constants.FilePlayerData);

        var json = JsonConvert.SerializeObject(snapshot, Formatting.Indented);
        File.WriteAllText(filePath, json);
    }
}