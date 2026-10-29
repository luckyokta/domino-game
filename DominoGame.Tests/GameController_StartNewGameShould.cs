using DominoGame.Core;
using DominoGame.Core.Enums;
using DominoGame.Core.Interfaces;
using DominoGame.Core.Models;
using DominoGame.Core.Factories;
using Microsoft.Extensions.Logging.Abstractions;

namespace DominoGame.Tests;

public class GameControllerStartNewGameTests
{
    private readonly DominoFactory _dominoFactory = new();
    private readonly List<IPlayer> _players = new();
    private readonly Dictionary<IPlayer, List<Tile>> _playerHands = new();
    private readonly Dictionary<IPlayer, int> _playerScores = new();
    private readonly DominoBoard _board = new();
    private readonly Boneyard _boneyard = new(new List<Tile>());
    private GameController _controller;

    public GameControllerStartNewGameTests()
    {
        _controller = new GameController(_board, _boneyard, _players, _playerHands, _playerScores, _dominoFactory, NullLogger<GameController>.Instance);
    }

    [SetUp]
    public void Setup()
    {
        _players.Clear();
        _playerHands.Clear();
        _playerScores.Clear();
        _board.PlayedTiles.Clear();
        _boneyard.Tiles.Clear();
        _controller = new GameController(_board, _boneyard, _players, _playerHands, _playerScores, _dominoFactory, NullLogger<GameController>.Instance);
    }

    [Test]
    public void StartNewGame_ValidPlayerNames_StartsGameInProgress()
    {
        _controller.StartNewGame(new List<string> { "Player A", "Player B" }, 100);

        Assert.That(_controller.Status, Is.EqualTo(GameStatus.InProgress));
        Assert.That(_controller.RoundStatus, Is.EqualTo(RoundStatus.InProgress));
        Assert.That(_controller.CurrentRound, Is.EqualTo(1));
        Assert.That(_controller.TargetScore, Is.EqualTo(100));
        Assert.That(_controller.Players.Count, Is.EqualTo(2));
        Assert.That(_controller.PlayerHands.Count, Is.EqualTo(2));
        Assert.That(_controller.PlayerScores.Count, Is.EqualTo(2));
        Assert.That(_controller.Board.IsEmpty, Is.True);
        Assert.That(_controller.Boneyard.RemainingCount, Is.EqualTo(14));

        foreach (IPlayer player in _controller.Players)
        {
            Assert.That(_controller.PlayerHands[player].Count, Is.EqualTo(7));
        }
    }

    [Test]
    public void StartNewGame_ValidPlayerNames_RaisesGameStartedEvent()
    {
        List<IPlayer>? startedPlayers = null;

        _controller.GameStarted += players => startedPlayers = players;

        _controller.StartNewGame(new List<string> { "Player A", "Player B" }, 100);

        Assert.That(startedPlayers, Is.Not.Null);
        Assert.That(startedPlayers!.Count, Is.EqualTo(2));
    }

    [Test]
    public void StartNewGame_NullPlayerNames_DoesNotStartGame()
    {
        _controller.StartNewGame(null!);

        Assert.That(_controller.Status, Is.EqualTo(GameStatus.NotStarted));
        Assert.That(_controller.Players.Count, Is.EqualTo(0));
    }

    [Test]
    public void StartNewGame_SinglePlayer_DoesNotStartGame()
    {
        _controller.StartNewGame(new List<string> { "Player A" }, 100);

        Assert.That(_controller.Status, Is.EqualTo(GameStatus.NotStarted));
        Assert.That(_controller.Players.Count, Is.EqualTo(0));
    }

    [Test]
    public void StartNewGame_FivePlayers_DoesNotStartGame()
    {
        _controller.StartNewGame(
            new List<string> { "Player A", "Player B", "Player C", "Player D", "Player E" },
            100);

        Assert.That(_controller.Status, Is.EqualTo(GameStatus.NotStarted));
        Assert.That(_controller.Players.Count, Is.EqualTo(0));
    }

    [Test]
    public void StartNewGame_WhitespacePlayerName_DoesNotStartGame()
    {
        _controller.StartNewGame(new List<string> { "Player A", "   " }, 100);

        Assert.That(_controller.Status, Is.EqualTo(GameStatus.NotStarted));
        Assert.That(_controller.Players.Count, Is.EqualTo(0));
    }

    [Test]
    public void StartNewGame_ZeroTargetScore_DoesNotStartGame()
    {
        _controller.StartNewGame(new List<string> { "Player A", "Player B" }, 0);

        Assert.That(_controller.Status, Is.EqualTo(GameStatus.NotStarted));
        Assert.That(_controller.Players.Count, Is.EqualTo(0));
    }

    [Test]
    public void StartNewGame_ValidFourPlayerGame_DealsSixTilesPerPlayer()
    {
        _controller.StartNewGame(
            new List<string> { "Player A", "Player B", "Player C", "Player D" },
            100);

        Assert.That(_controller.Status, Is.EqualTo(GameStatus.InProgress));
        Assert.That(_controller.Players.Count, Is.EqualTo(4));
        Assert.That(_controller.Boneyard.RemainingCount, Is.EqualTo(4));

        foreach (IPlayer player in _controller.Players)
        {
            Assert.That(_controller.PlayerHands[player].Count, Is.EqualTo(6));
        }
    }
}
