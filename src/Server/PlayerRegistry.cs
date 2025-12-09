using Ledger.Core;

namespace Ledger.Server;

public class PlayerRegistry
{
    private readonly Dictionary<string, PlayerSnapshot> _players = new();

    public PlayerSnapshot GetOrCreate(string uid, string name, Func<string> nowDateFactory)
    {
        if (!_players.TryGetValue(uid, out var snapshot))
        {
            snapshot = new PlayerSnapshot
            {
                Uid = uid,
                Name = name,
                FirstJoin = nowDateFactory(),
                LastJoin = nowDateFactory()
            };

            _players[uid] = snapshot;
        }
        else
        {
            snapshot.Name = name;
        }

        return snapshot;
    }

    public IEnumerable<PlayerSnapshot> All => _players.Values;
}