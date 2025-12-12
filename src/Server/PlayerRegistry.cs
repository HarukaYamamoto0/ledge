using Ledger.Core;

namespace Ledger.Server;

public class PlayerRegistry
{
    private sealed class RuntimeState
    {
        public double AccumulatedPlaytimeHours { get; set; }
        public DateTime? OnlineSinceUtc { get; set; }
        public int Deaths { get; set; }
    }

    private readonly Dictionary<string, PlayerSnapshot> _players = new();
    private readonly Dictionary<string, RuntimeState> _state = new();

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

        // Mantém stats “persistentes” na snapshot (via state)
        var st = GetState(uid);
        snapshot.Stats.Deaths = st.Deaths;
        snapshot.Stats.PlaytimeHours = GetPlaytimeHours(uid, DateTime.UtcNow);

        return snapshot;
    }

    public IEnumerable<PlayerSnapshot> All => _players.Values;

    public void MarkOnline(string uid, DateTime nowUtc)
    {
        var st = GetState(uid);
        st.OnlineSinceUtc ??= nowUtc;
    }

    public void MarkOffline(string uid, DateTime nowUtc)
    {
        var st = GetState(uid);

        if (st.OnlineSinceUtc is DateTime since)
        {
            st.AccumulatedPlaytimeHours += (nowUtc - since).TotalHours;
            st.OnlineSinceUtc = null;
        }
    }

    public void IncrementDeaths(string uid)
    {
        var st = GetState(uid);
        st.Deaths += 1;

        if (_players.TryGetValue(uid, out var snap))
        {
            snap.Stats.Deaths = st.Deaths;
        }
    }

    public int GetDeaths(string uid) => GetState(uid).Deaths;

    public double GetPlaytimeHours(string uid, DateTime nowUtc)
    {
        var st = GetState(uid);
        var hours = st.AccumulatedPlaytimeHours;

        if (st.OnlineSinceUtc is DateTime since)
        {
            hours += (nowUtc - since).TotalHours;
        }

        return Math.Round(hours, 2);
    }

    private RuntimeState GetState(string uid)
    {
        if (!_state.TryGetValue(uid, out var st))
        {
            st = new RuntimeState();
            _state[uid] = st;
        }

        return st;
    }
}