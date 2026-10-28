using DominoGame.Api.Dtos;
using DominoGame.Core;
using DominoGame.Core.Enums;
using DominoGame.Core.Factories;
using DominoGame.Core.Interfaces;
using DominoGame.Core.Models;

namespace DominoGame.Api.Services;

public record SessionResult(bool Ok, string? Error)
{
    public static SessionResult Success() => new(true, null);
    public static SessionResult Fail(string error) => new(false, error);
}

public class Seat
{
    public Seat(string name, string token, bool isHost)
    {
        Name = name;
        Token = token;
        IsHost = isHost;
    }

    public string Name { get; }
    public string Token { get; }
    public bool IsHost { get; }
}

/// <summary>
/// One online room. Wraps a single <see cref="GameController"/> (your game rules, unchanged)
/// and adds: seats/tokens (who is who), per-player views of the state, validation with
/// readable error messages, an activity log, and locking (GameController is not thread-safe).
/// Seat index == index of the player inside GameController.Players.
/// </summary>
public class GameSession
{
    public const int MinPlayers = 2;
    public const int MaxPlayers = 4;

    private const int MaxNameLength = 20;
    private const int MaxLogEntries = 60;
    private const int LogEntriesSent = 40;

    private readonly object _sync = new();
    private readonly List<Seat> _seats = new();
    private readonly List<string> _log = new();
    private readonly GameController _game = new GameController(
        new DominoBoard(),
        new Boneyard(new List<Tile>()),
        new List<IPlayer>(),
        new Dictionary<IPlayer, List<Tile>>(),
        new Dictionary<IPlayer, int>(),
        new DominoFactory());
    private bool _started;
    private int _lastRoundPoints;

    public GameSession(string code, int targetScore)
    {
        Code = code;
        TargetScore = targetScore;
        LastActivityUtc = DateTime.UtcNow;

        // Turn GameController events into a human-readable activity log.
        _game.GameStarted += _ => AddLog("The game has started. Good luck!");

        _game.TilePlayed += (player, tile, end) =>
        {
            if (_game.Board.PlayedTiles.Count == 1)
            {
                AddLog($"{player.Name} opened with {tile}.");
            }
            else
            {
                AddLog($"{player.Name} played {tile} on the {end.ToString().ToLowerInvariant()} end.");
            }
        };

        _game.TileDrawn += (player, _) => AddLog($"{player.Name} drew a tile from the boneyard.");
        _game.PlayerPassed += player => AddLog($"{player.Name} passed.");

        _game.PlayerScored += (player, points) =>
        {
            _lastRoundPoints = points;
            AddLog($"{player.Name} scored {points} points.");
        };

        _game.RoundOver += winner =>
        {
            if (winner == null)
            {
                AddLog($"Round {_game.CurrentRound} is over. It's a tie, so nobody scores.");
            }
            else
            {
                AddLog($"Round {_game.CurrentRound} is over. Winner: {winner.Name}.");
            }
        };

        _game.MatchOver += winner => AddLog($"{winner.Name} wins the match!");
    }

    public string Code { get; }
    public int TargetScore { get; }
    public DateTime LastActivityUtc { get; private set; }

    // ------------------------------------------------------------------
    // Lobby
    // ------------------------------------------------------------------

    public SessionResult AddPlayer(string? rawName, bool isHost, out string token, out int seatIndex)
    {
        token = string.Empty;
        seatIndex = -1;

        string name = (rawName ?? string.Empty).Trim();

        if (name.Length == 0)
        {
            return SessionResult.Fail("Please enter your name.");
        }

        if (name.Length > MaxNameLength)
        {
            return SessionResult.Fail($"Names can be at most {MaxNameLength} characters long.");
        }

        lock (_sync)
        {
            if (_started)
            {
                return SessionResult.Fail("This game has already started.");
            }

            if (_seats.Count >= MaxPlayers)
            {
                return SessionResult.Fail("This room is full (4 players maximum).");
            }

            if (_seats.Any(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                return SessionResult.Fail("Someone in this room already uses that name.");
            }

            token = Guid.NewGuid().ToString("N");
            _seats.Add(new Seat(name, token, isHost));
            seatIndex = _seats.Count - 1;

            Touch();
            return SessionResult.Success();
        }
    }

    public int FindSeat(string? token)
    {
        lock (_sync)
        {
            return FindSeatUnsafe(token);
        }
    }

    public SessionResult Start(string? token)
    {
        lock (_sync)
        {
            int me = FindSeatUnsafe(token);

            if (me < 0)
            {
                return SessionResult.Fail("You are not a player in this room.");
            }

            if (!_seats[me].IsHost)
            {
                return SessionResult.Fail("Only the host can start the game.");
            }

            if (_started && _game.Status != GameStatus.Completed)
            {
                return SessionResult.Fail("The game is already in progress.");
            }

            if (_seats.Count < MinPlayers)
            {
                return SessionResult.Fail("At least 2 players are needed to start.");
            }

            // New match (also used for "play again" after a match has finished).
            _log.Clear();
            _lastRoundPoints = 0;

            try
            {
                _game.StartNewGame(_seats.Select(s => s.Name).ToList(), TargetScore);
            }
            catch (InvalidOperationException)
            {
                return SessionResult.Fail("The game could not be started.");
            }

            if (_game.Status != GameStatus.InProgress || _game.Players.Count != _seats.Count)
            {
                return SessionResult.Fail("The game could not be started.");
            }

            _started = true;
            Touch();
            return SessionResult.Success();
        }
    }

    // ------------------------------------------------------------------
    // Game actions
    // ------------------------------------------------------------------

    public SessionResult Play(string? token, int left, int right, string? endText)
    {
        if (!Enum.TryParse(endText, true, out BoardEnd end) || !Enum.IsDefined(end))
        {
            return SessionResult.Fail("End must be 'left' or 'right'.");
        }

        lock (_sync)
        {
            SessionResult? failure = RequireTurn(token, out IPlayer player);

            if (failure != null)
            {
                return failure;
            }

            List<Tile> hand = _game.PlayerHands[player];
            Tile tile = default;
            bool found = false;

            foreach (Tile candidate in hand)
            {
                bool sameWay = candidate.Left == left && candidate.Right == right;
                bool flipped = candidate.Left == right && candidate.Right == left;

                if (sameWay || flipped)
                {
                    tile = candidate;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                return SessionResult.Fail("You don't have that tile.");
            }

            if (!_game.IsValidMove(player, tile, end))
            {
                return SessionResult.Fail($"That tile doesn't match the {end.ToString().ToLowerInvariant()} end of the board.");
            }

            int handSizeBefore = hand.Count;

            try
            {
                _game.PlayTurn(player, tile, end);
            }
            catch (InvalidOperationException)
            {
                return SessionResult.Fail("The move was rejected.");
            }

            if (hand.Count >= handSizeBefore)
            {
                return SessionResult.Fail("The move was rejected.");
            }

            Touch();
            return SessionResult.Success();
        }
    }

    public SessionResult Draw(string? token)
    {
        lock (_sync)
        {
            SessionResult? failure = RequireTurn(token, out IPlayer player);

            if (failure != null)
            {
                return failure;
            }

            if (!_game.CanDraw)
            {
                return SessionResult.Fail("You can only draw when you have no playable tile and the boneyard isn't empty.");
            }

            Tile? drawn;
            try
            {
                drawn = _game.DrawTile(player);
            }
            catch (InvalidOperationException)
            {
                return SessionResult.Fail("Could not draw a tile.");
            }

            if (drawn == null)
            {
                return SessionResult.Fail("Could not draw a tile.");
            }

            Touch();
            return SessionResult.Success();
        }
    }

    public SessionResult Pass(string? token)
    {
        lock (_sync)
        {
            SessionResult? failure = RequireTurn(token, out IPlayer _);

            if (failure != null)
            {
                return failure;
            }

            if (!_game.CanPass)
            {
                return SessionResult.Fail("You can only pass when you have no playable tile and the boneyard is empty.");
            }

            _game.PassTurn();

            Touch();
            return SessionResult.Success();
        }
    }

    public SessionResult NextRound(string? token)
    {
        lock (_sync)
        {
            if (FindSeatUnsafe(token) < 0)
            {
                return SessionResult.Fail("You are not a player in this room.");
            }

            if (!_started || _game.Status != GameStatus.InProgress || _game.RoundStatus != RoundStatus.Completed)
            {
                return SessionResult.Fail("The round isn't over yet.");
            }

            _lastRoundPoints = 0;
            try
            {
                _game.StartNewRound();
            }
            catch (InvalidOperationException)
            {
                return SessionResult.Fail("The round could not be started.");
            }
            AddLog($"Round {_game.CurrentRound} has started.");

            Touch();
            return SessionResult.Success();
        }
    }

    // ------------------------------------------------------------------
    // State (what a particular player is allowed to see)
    // ------------------------------------------------------------------

    public GameStateDto? BuildState(string? token)
    {
        lock (_sync)
        {
            int me = FindSeatUnsafe(token);

            if (me < 0)
            {
                return null;
            }

            Touch();

            if (!_started)
            {
                List<PlayerDto> lobbyPlayers = new();

                for (int i = 0; i < _seats.Count; i++)
                {
                    lobbyPlayers.Add(new PlayerDto(i, _seats[i].Name, _seats[i].IsHost, i == me, false, 0, 0, null));
                }

                bool canStartLobby = _seats[me].IsHost && _seats.Count >= MinPlayers;

                return new GameStateDto(
                    Code, "lobby", TargetScore, 0, me,
                    lobbyPlayers, new List<HandTileDto>(), new List<TileDto>(),
                    null, null, 0, null,
                    false, false, false, canStartLobby,
                    null, null, 0, LogSnapshot());
            }

            bool roundOver = _game.RoundStatus == RoundStatus.Completed;
            bool matchOver = _game.Status == GameStatus.Completed;
            bool reveal = roundOver || matchOver;

            string phase = matchOver ? "matchOver" : roundOver ? "roundOver" : "playing";

            IPlayer? current = _game.CurrentPlayer;
            int currentSeat = current == null ? -1 : _game.Players.IndexOf(current);

            bool yourTurn = phase == "playing" && currentSeat == me;

            List<PlayerDto> players = new();

            for (int i = 0; i < _game.Players.Count; i++)
            {
                IPlayer p = _game.Players[i];
                List<Tile> tiles = _game.PlayerHands.TryGetValue(p, out List<Tile>? found) ? found : new List<Tile>();

                List<TileDto>? revealed = reveal
                    ? tiles.Select(t => new TileDto(t.Left, t.Right)).ToList()
                    : null;

                players.Add(new PlayerDto(
                    i,
                    _seats[i].Name,
                    _seats[i].IsHost,
                    i == me,
                    i == currentSeat && phase == "playing",
                    tiles.Count,
                    _game.PlayerScores[p],
                    revealed));
            }

            List<Tile> myTiles = _game.PlayerHands[_game.Players[me]];

            List<HandTileDto> hand = myTiles
                .Select(t => new HandTileDto(
                    t.Left,
                    t.Right,
                    yourTurn && _game.IsValidMove(t, BoardEnd.Left),
                    yourTurn && _game.IsValidMove(t, BoardEnd.Right)))
                .ToList();

            List<TileDto> board = _game.Board.PlayedTiles
                .Select(t => new TileDto(t.Left, t.Right))
                .ToList();

            // null when the round is not over, or when it ended in a tie (nobody scores)
            int? roundWinner = reveal && _game.RoundWinner != null
                ? _game.Players.IndexOf(_game.RoundWinner)
                : (int?)null;

            int? matchWinner = _game.MatchWinner == null ? (int?)null : _game.Players.IndexOf(_game.MatchWinner);
            int? currentSeatOrNull = currentSeat >= 0 ? currentSeat : (int?)null;

            return new GameStateDto(
                Code,
                phase,
                TargetScore,
                _game.CurrentRound,
                me,
                players,
                hand,
                board,
                _game.Board.LeftEndValue,
                _game.Board.RightEndValue,
                _game.Boneyard.RemainingCount,
                currentSeatOrNull,
                yourTurn,
                yourTurn && _game.CanDraw,
                yourTurn && _game.CanPass,
                matchOver && _seats[me].IsHost,
                roundWinner,
                matchWinner,
                reveal ? _lastRoundPoints : 0,
                LogSnapshot());
        }
    }

    // ------------------------------------------------------------------
    // Helpers (call only while holding _sync)
    // ------------------------------------------------------------------

    private int FindSeatUnsafe(string? token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return -1;
        }

        return _seats.FindIndex(s => s.Token == token);
    }

    /// <summary>Returns null when the caller may act now; otherwise the reason they may not.</summary>
    private SessionResult? RequireTurn(string? token, out IPlayer player)
    {
        player = null!;

        int me = FindSeatUnsafe(token);

        if (me < 0)
        {
            return SessionResult.Fail("You are not a player in this room.");
        }

        if (!_started || _game.Status != GameStatus.InProgress || _game.RoundStatus != RoundStatus.InProgress)
        {
            return SessionResult.Fail("There is no round in progress.");
        }

        IPlayer? current = _game.CurrentPlayer;

        if (current == null || _game.Players.IndexOf(current) != me)
        {
            return SessionResult.Fail("It isn't your turn.");
        }

        player = current;
        return null;
    }

    private void AddLog(string message)
    {
        _log.Add(message);

        if (_log.Count > MaxLogEntries)
        {
            _log.RemoveAt(0);
        }
    }

    private List<string> LogSnapshot()
    {
        return _log.Skip(Math.Max(0, _log.Count - LogEntriesSent)).ToList();
    }

    private void Touch()
    {
        LastActivityUtc = DateTime.UtcNow;
    }
}
