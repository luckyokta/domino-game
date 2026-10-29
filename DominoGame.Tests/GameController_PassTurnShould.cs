using DominoGame.Core;
using DominoGame.Core.Enums;
using DominoGame.Core.Interfaces;
using DominoGame.Core.Models;
using DominoGame.Core.Factories;
using Microsoft.Extensions.Logging.Abstractions;

namespace DominoGame.Tests;

public class GameControllerPassTurnTests
{
    private readonly DominoFactory _dominoFactory = new();
    private readonly List<IPlayer> _players = new();
    private readonly Dictionary<IPlayer, List<Tile>> _playerHands = new();
    private readonly Dictionary<IPlayer, int> _playerScores = new();
    private readonly DominoBoard _board = new();
    private readonly Boneyard _boneyard = new(new List<Tile>());
    private GameController _controller;


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
    public void PassTurn_RoundNotInProgress_DoesNotPass()
    {
        GameController controller = new(
            new DominoBoard(),
            new Boneyard(new List<Tile>()),
            new List<IPlayer>(),
            new Dictionary<IPlayer, List<Tile>>(),
            new Dictionary<IPlayer, int>(),
            new DominoFactory(),
            NullLogger<GameController>.Instance);

        controller.PassTurn();

        Assert.That(controller.RoundStatus, Is.EqualTo(RoundStatus.NotStarted));
    }

    [Test]
    public void PassTurn_PlayerHasValidMove_DoesNotPass()
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
        IPlayer currentPlayer = controller.CurrentPlayer!;

        controller.PlayerHands[currentPlayer].Clear();
        controller.PlayerHands[currentPlayer].Add(new Tile(2, 4));
        controller.Boneyard.Tiles.Clear();

        Assert.That(controller.HasValidMoves(currentPlayer), Is.True);

        controller.PassTurn();

        Assert.That(controller.RoundStatus, Is.EqualTo(RoundStatus.InProgress));
    }

    [Test]
    public void PassTurn_NoValidMoveAndCanPassEnabled_PassesTurn()
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
        IPlayer currentPlayer = controller.CurrentPlayer!;
        IPlayer otherPlayer = controller.Players.First(p => p != currentPlayer);

        controller.PlayerHands[currentPlayer].Clear();
        controller.PlayerHands[currentPlayer].Add(new Tile(2, 4));
        controller.PlayerHands[currentPlayer].Add(new Tile(4, 6));
        controller.PlayerHands[otherPlayer].Clear();
        controller.PlayerHands[otherPlayer].Add(new Tile(6, 6));
        controller.Boneyard.Tiles.Clear();

        controller.PlayTurn(currentPlayer, new Tile(2, 4), BoardEnd.Right);

        Assert.That(controller.CanPass, Is.True);

        IPlayer? passedPlayer = null;
        controller.PlayerPassed += player => passedPlayer = player;

        controller.PassTurn();

        Assert.That(passedPlayer, Is.SameAs(otherPlayer));
        Assert.That(controller.RoundStatus, Is.EqualTo(RoundStatus.InProgress));
        Assert.That(controller.CurrentPlayer, Is.SameAs(currentPlayer));
        Assert.That(controller.CanPass, Is.False);
    }
}
