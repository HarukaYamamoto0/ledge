using Ledger.Core;
using Newtonsoft.Json;
using Vintagestory.API.Server;

namespace Ledger.Storage;

public class JsonPlayerStorage(string basePath) : IPlayerStorage
{
    public void SaveSnapshot(PlayerSnapshot snapshot)
    {
        Directory.CreateDirectory(basePath);

        var id = ToBase64Url(snapshot.Uid);

        var finalPath = Path.Combine(basePath, $"{id}.json");
        var tempPath = Path.Combine(basePath, $"._{id}.json.tmp");

        var json = JsonConvert.SerializeObject(snapshot, Formatting.Indented);

        File.WriteAllText(tempPath, json);
        File.Move(tempPath, finalPath, true);
    }

    public bool TryLoad(string uid, out PlayerSnapshot snapshot)
    {
        snapshot = null!;

        var id = ToBase64Url(uid);
        var finalPath = Path.Combine(basePath, $"{id}.json");
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

    private static string ToBase64Url(string base64)
    {
        return base64
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    // Optional helper for consumers / debugging
    private static string FromBase64Url(string base64Url)
    {
        var padded = base64Url
            .Replace('-', '+')
            .Replace('_', '/');

        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
        }

        return padded;
    }
}