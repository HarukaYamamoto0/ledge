using Ledger.Core;
using Newtonsoft.Json;
using Vintagestory.API.Server;

namespace Ledger.Storage;

public class JsonPlayerStorage(string basePath) : IPlayerStorage
{
    public void SaveSnapshot(PlayerSnapshot snapshot)
    {
        Directory.CreateDirectory(basePath);

        var finalPath = Path.Combine(basePath, $"{snapshot.Uid}.json");

        // Temp file kept in the same directory for atomic replacement
        // Prefixed with ._ so watchdog scripts can easily ignore it
        var tempPath = Path.Combine(basePath, $"._{snapshot.Uid}.json.tmp");

        var json = JsonConvert.SerializeObject(snapshot, Formatting.Indented);

        File.WriteAllText(tempPath, json);

        // Replace a final file atomically-ish (same directory, cross-platform safe)
        File.Move(tempPath, finalPath, true);
    }

    public static void CleanupTempFiles(string basePath, ICoreServerAPI api)
    {
        try
        {
            if (!Directory.Exists(basePath)) return;

            foreach (var tmp in Directory.EnumerateFiles(basePath, "._*.tmp"))
            {
                File.Delete(tmp);
            }
        }
        catch (Exception ex)
        {
            api.Logger.Warning(
                $"{Constants.ModLogPrefix} Failed to cleanup temp files: {ex.Message}");
        }
    }

    public bool TryLoad(string uid, out PlayerSnapshot snapshot)
    {
        snapshot = null!;

        var finalPath = Path.Combine(basePath, $"{uid}.json");
        if (!File.Exists(finalPath)) return false;

        try
        {
            var json = File.ReadAllText(finalPath);
            var obj = JsonConvert.DeserializeObject<PlayerSnapshot>(json);
            if (obj == null) return false;

            snapshot = obj;
            return true;
        }
        catch
        {
            return false;
        }
    }
}