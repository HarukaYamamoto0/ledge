using Ledger.Core;
using Ledger.Storage;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Ledger.Server;

// ReSharper disable once UnusedType.Global
public class LedgerModSystem : ModSystem
{
    private LedgerService _service = null!;

    public override void StartServerSide(ICoreServerAPI api)
    {
        var config = LoadConfig(api);

        var registry = new PlayerRegistry();
        var snapshotProvider = new VsPlayerSnapshotProvider(api, registry);

        var storages = new List<IPlayerStorage>();
        if (config.EnableJson)
        {
            storages.Add(new JsonPlayerStorage(config.BasePath));
        }

        // After: if (config.EnableSqlite) storages.Add(new SqlitePlayerStorage(...));

        _service = new LedgerService(api, registry, snapshotProvider, storages);

        api.Event.PlayerJoin += _service.OnPlayerJoin;
        api.Event.PlayerLeave += _service.OnPlayerLeave;

        api.World.RegisterGameTickListener(_ => _service.OnIntervalTick(), config.IntervalSeconds * 1000);

        api.Logger.Event($"{Constants.ModLogPrefix} Initialized. Interval: {0}s, BasePath: {1}", config.IntervalSeconds,
            config.BasePath);
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