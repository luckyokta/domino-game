using System.Collections.Concurrent;

namespace DominoGame.Api.Services;

/// <summary>In-memory registry of rooms, keyed by room code. Rooms are lost when the API restarts.</summary>
public class GameSessionStore
{
    // No 0/O or 1/I so codes are easy to read out loud.
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int CodeLength = 5;
    private static readonly TimeSpan IdleLimit = TimeSpan.FromHours(6);

    private readonly ConcurrentDictionary<string, GameSession> _sessions =
        new(StringComparer.OrdinalIgnoreCase);

    public GameSession Create(int targetScore)
    {
        RemoveIdleSessions();

        for (int attempt = 0; attempt < 25; attempt++)
        {
            string code = GenerateCode();
            GameSession session = new(code, targetScore);

            if (_sessions.TryAdd(code, session))
            {
                return session;
            }
        }

        throw new InvalidOperationException("Could not allocate a room code.");
    }

    public GameSession? Get(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        return _sessions.TryGetValue(code.Trim(), out GameSession? session) ? session : null;
    }

    public void Remove(string code)
    {
        _sessions.TryRemove(code, out _);
    }

    private void RemoveIdleSessions()
    {
        DateTime cutoff = DateTime.UtcNow - IdleLimit;

        foreach (KeyValuePair<string, GameSession> entry in _sessions)
        {
            if (entry.Value.LastActivityUtc < cutoff)
            {
                _sessions.TryRemove(entry.Key, out _);
            }
        }
    }

    private static string GenerateCode()
    {
        char[] chars = new char[CodeLength];

        for (int i = 0; i < chars.Length; i++)
        {
            chars[i] = Alphabet[Random.Shared.Next(Alphabet.Length)];
        }

        return new string(chars);
    }
}
