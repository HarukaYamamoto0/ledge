// ReSharper disable ConvertTypeCheckPatternToNullCheck

using Ledger.Core;

namespace Ledger.Server;

public class PlayerRegistry
{
    private sealed class RuntimeState
    {
        public long AccumulatedPlaytimeSeconds { get; set; }
        public DateTime? OnlineSinceUtc { get; set; }
        public int Deaths { get; set; }
    }

    private readonly Dictionary<string, PlayerSnapshot> _players = new();
    private readonly Dictionary<string, RuntimeState> _state = new();

    public IEnumerable<PlayerSnapshot> All => _players.Values;

    public PlayerSnapshot GetOrCreate(string uid, string name, Func<long> nowUnixFactory)
    {
        var snapshot = GetOrCreateSnapshot(uid, name, nowUnixFactory);
        ApplyRuntimeState(uid, snapshot, DateTime.UtcNow);
        return snapshot;
    }

    public void MarkOnline(string uid, DateTime nowUtc)
    {
        GetState(uid).OnlineSinceUtc ??= nowUtc;
    }

    public void MarkOffline(string uid, DateTime nowUtc)
    {
        var st = GetState(uid);
        if (st.OnlineSinceUtc is not DateTime since) return;

        st.AccumulatedPlaytimeSeconds += ElapsedSeconds(since, nowUtc);
        st.OnlineSinceUtc = null;
    }

    public void IncrementDeaths(string uid)
    {
        var st = GetState(uid);
        st.Deaths++;

        if (_players.TryGetValue(uid, out var snap))
        {
            snap.Stats.Deaths = st.Deaths;
        }
    }

    public int GetDeaths(string uid) => GetState(uid).Deaths;

    public long GetPlaytimeSeconds(string uid, DateTime nowUtc)
    {
        var st = GetState(uid);
        var seconds = st.AccumulatedPlaytimeSeconds;

        if (st.OnlineSinceUtc is DateTime since)
        {
            seconds += ElapsedSeconds(since, nowUtc);
        }

        return seconds;
    }

    public void SeedFromPersisted(string uid, int deaths, long playtimeSeconds, long firstJoinUnix)
    {
        var st = GetState(uid);
        st.Deaths = deaths;
        st.AccumulatedPlaytimeSeconds = playtimeSeconds;

        if (_players.TryGetValue(uid, out var snap))
        {
            snap.Stats.Deaths = deaths;
            snap.Stats.PlaytimeSeconds = playtimeSeconds;
            snap.FirstJoin = firstJoinUnix;
        }
    }

    private PlayerSnapshot GetOrCreateSnapshot(string uid, string name, Func<long> nowUnixFactory)
    {
        if (_players.TryGetValue(uid, out var existing))
        {
            existing.Name = name;
            return existing;
        }

        var nowUnix = nowUnixFactory();

        var created = new PlayerSnapshot
        {
            Uid = uid,
            Name = name,
            FirstJoin = nowUnix,
            LastJoin = nowUnix
        };

        _players[uid] = created;
        return created;
    }

    private void ApplyRuntimeState(string uid, PlayerSnapshot snapshot, DateTime nowUtc)
    {
        snapshot.Stats.Deaths = GetDeaths(uid);
        snapshot.Stats.PlaytimeSeconds = GetPlaytimeSeconds(uid, nowUtc);
    }

    private RuntimeState GetState(string uid)
    {
        if (_state.TryGetValue(uid, out var st)) return st;

        st = new RuntimeState();
        _state[uid] = st;
        return st;
    }

    public bool TryGet(string uid, out PlayerSnapshot snapshot) =>
        _players.TryGetValue(uid, out snapshot!);

    private static long ElapsedSeconds(DateTime sinceUtc, DateTime nowUtc) =>
        (long)Math.Floor((nowUtc - sinceUtc).TotalSeconds);
}