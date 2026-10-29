using DominoGame.Core;
using DominoGame.Core.Enums;
using DominoGame.Core.Interfaces;
using DominoGame.Core.Models;
using DominoGame.Core.Factories;
using Microsoft.Extensions.Logging.Abstractions;

namespace DominoGame.Tests;

public class GameControllerStartNewRoundTests
{
    private readonly DominoFactory _dominoFactory = new();
    private readonly List<IPlayer> _players = new();
    private readonly Dictionary<IPlayer, List<Tile>> _playerHands = new();
    private readonly Dictionary<IPlayer, int> _playerScores = new();
    private readonly DominoBoard _board = new();
    private readonly Boneyard _boneyard = new(new List<Tile>());
    private GameController _controller;

    public GameControllerStartNewRoundTests()
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

    private GameController CreateStartedGameController()
    {
        GameController controller = new(
            new DominoBoard(),
            new Boneyard(new List<Tile>()),
            new List<IPlayer>(),
            new Dictionary<IPlayer, List<Tile>>(),
            new Dictionary<IPlayer, int>(),
            new DominoFactory(),
            NullLogger<GameController>.Instance);

        controller.StartNewGame(new List<string> { "Player A", "Player B" }, 100);

        return controller;
    }

    [Test]
    public void StartNewRound_RoundNotCompleted_DoesNotStartNewRound()
    {
        GameController controller = CreateStartedGameController();

        controller.StartNewRound();

        Assert.That(controller.CurrentRound, Is.EqualTo(1));
        Assert.That(controller.RoundStatus, Is.EqualTo(RoundStatus.InProgress));
    }

    [Test]
    public void StartNewRound_RoundCompleted_StartsNewRound()
    {
        GameController controller = CreateStartedGameController();
        IPlayer currentPlayer = controller.CurrentPlayer!;

        controller.PlayerHands[currentPlayer].Clear();
        controller.PlayerHands[currentPlayer].Add(new Tile(2, 4));

        controller.PlayTurn(currentPlayer, new Tile(2, 4), BoardEnd.Right);

        Assert.That(controller.RoundStatus, Is.EqualTo(RoundStatus.Completed));

        controller.StartNewRound();

        Assert.That(controller.Status, Is.EqualTo(GameStatus.InProgress));
        Assert.That(controller.RoundStatus, Is.EqualTo(RoundStatus.InProgress));
        Assert.That(controller.CurrentRound, Is.EqualTo(2));
        Assert.That(controller.Board.IsEmpty, Is.True);
        Assert.That(controller.Boneyard.RemainingCount, Is.EqualTo(14));

        foreach (IPlayer player in controller.Players)
        {
            Assert.That(controller.PlayerHands[player].Count, Is.EqualTo(7));
        }
    }
}
