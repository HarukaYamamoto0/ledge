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

        var interval = config.IntervalSeconds < Constants.MinIntervalSeconds
            ? Constants.MinIntervalSeconds
            : config.IntervalSeconds;

        if (interval != config.IntervalSeconds)
        {
            api.Logger.Warning(
                $"{Constants.ModLogPrefix} IntervalSeconds too low, clamping to {Constants.MinIntervalSeconds}s");
        }

        var registry = new PlayerRegistry();
        var snapshotProvider = new VsPlayerSnapshotProvider(api, registry, config);

        var storages = new List<IPlayerStorage>();

        if (config.EnableJson)
        {
            _jsonStorage = new JsonPlayerStorage(basePath);
            JsonPlayerStorage.CleanupTempFiles(basePath, api);
            storages.Add(_jsonStorage);
        }

        _service = new LedgerService(api, registry, snapshotProvider, storages, _jsonStorage);

        api.Event.PlayerJoin += _service.OnPlayerJoin;
        api.Event.PlayerLeave += _service.OnPlayerLeave;
        api.Event.PlayerDeath += _service.OnPlayerDeath;

        api.World.RegisterGameTickListener(_ => _service.OnIntervalTick(), interval * 1000);

        api.ChatCommands
            .Create("ledger")
            .WithDescription("Ledger commands")
            .RequiresPrivilege(Privilege.controlserver)
            .BeginSubCommand("reload")
            .WithDescription("Reload Ledger configuration")
            .HandleWith(args =>
            {
                if (args.Caller.Player is not IServerPlayer)
                {
                    return TextCommandResult.Error("This command can only be used server-side.");
                }

                ReloadConfig(api);
                return TextCommandResult.Success("Ledger config reloaded.");
            })
            .EndSubCommand();

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

    private void ReloadConfig(ICoreServerAPI api)
    {
        try
        {
            var config = LoadConfig(api);

            _service.UpdateCaptureConfig(config.Capture);

            api.Logger.Event($"{Constants.ModLogPrefix} Config reloaded via command.");
        }
        catch (Exception ex)
        {
            api.Logger.Error($"{Constants.ModLogPrefix} Reload failed: {ex}");
            throw;
        }
    }
}