using System.Diagnostics.CodeAnalysis;
using Ledger.Core;
using Ledger.Storage;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Ledger.Server;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class LedgerModSystem : ModSystem
{
    private LedgerService _service = null!;
    private JsonPlayerStorage? _jsonStorage;

    public override void StartServerSide(ICoreServerAPI api)
    {
        var config = LoadConfig(api);

        var basePath = string.IsNullOrWhiteSpace(config.BasePath)
            ? api.GetOrCreateDataPath(Constants.DefaultBasePath)
            : config.BasePath;

        var registry = new PlayerRegistry();
        var snapshotProvider = new VsPlayerSnapshotProvider(api, registry);

        var storages = new List<IPlayerStorage>();
        if (config.EnableJson)
        {
            _jsonStorage = new JsonPlayerStorage(basePath);
            JsonPlayerStorage.CleanupTempFiles(basePath, api);
            storages.Add(_jsonStorage);
        }

        // TODO: After: if (config.EnableSqlite) storages.Add(new SqlitePlayerStorage(...));

        _service = new LedgerService(api, registry, snapshotProvider, storages, _jsonStorage);

        api.Event.PlayerJoin += _service.OnPlayerJoin;
        api.Event.PlayerLeave += _service.OnPlayerLeave;
        api.Event.PlayerDeath += _service.OnPlayerDeath;

        var interval = config.IntervalSeconds;

        if (interval < Constants.MinIntervalSeconds)
        {
            api.Logger.Warning(
                $"{Constants.ModLogPrefix} IntervalSeconds too low, clamping to {Constants.MinIntervalSeconds}s");
            interval = Constants.MinIntervalSeconds;
        }

        api.World.RegisterGameTickListener(_ => _service.OnIntervalTick(), interval * 1000);

        api.Logger.Event(
            $"{Constants.ModLogPrefix} Initialized. Interval: {interval}s, BasePath: {basePath}");
    }

    private static LedgerConfig LoadConfig(ICoreServerAPI api)
    {
        var config = api.LoadModConfig<LedgerConfig>(Constants.FileConfig);
        if (config == null)
        {
            config = new LedgerConfig();
            api.StoreModConfig(config, Constants.FileConfig);
            api.Logger.Event($"{Constants.ModLogPrefix} Created default {Constants.FileConfig}");
        }
        else
        {
            api.Logger.Event($"{Constants.ModLogPrefix} Loaded {Constants.FileConfig}");
        }

        return config;
    }
}