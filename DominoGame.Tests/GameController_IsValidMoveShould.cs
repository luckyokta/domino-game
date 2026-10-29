using DominoGame.Core;
using DominoGame.Core.Enums;
using DominoGame.Core.Interfaces;
using DominoGame.Core.Models;
using DominoGame.Core.Factories;
using Microsoft.Extensions.Logging.Abstractions;

namespace DominoGame.Tests;

public class GameControllerIsValidMoveTests
{
    private readonly DominoFactory _dominoFactory = new();
    private readonly List<IPlayer> _players = new();
    private readonly Dictionary<IPlayer, List<Tile>> _playerHands = new();
    private readonly Dictionary<IPlayer, int> _playerScores = new();
    private readonly DominoBoard _board = new();
    private readonly Boneyard _boneyard = new(new List<Tile>());
    private GameController _controller;

    public GameControllerIsValidMoveTests()
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
    public void IsValidMove_BoardEmpty_ReturnsTrue()
    {
        IPlayer player = new FakePlayer(1, "Player A");
        _players.Add(player);
        _playerHands[player] = new List<Tile> { new Tile(6, 6) };

        Assert.That(_controller.IsValidMove(player, new Tile(6, 6), BoardEnd.Right), Is.True);
    }

    [Test]
    public void IsValidMove_TileMatchesBoardEnd_ReturnsTrue()
    {
        IPlayer player = new FakePlayer(1, "Player A");
        _players.Add(player);
        _playerHands[player] = new List<Tile> { new Tile(4, 6) };
        _board.PlayedTiles.Clear();
        _board.PlayedTiles.Add(new Tile(2, 4));

        Assert.That(_controller.IsValidMove(player, new Tile(4, 6), BoardEnd.Right), Is.True);
    }

    [Test]
    public void IsValidMove_TileDoesNotMatchBoardEnd_ReturnsFalse()
    {
        IPlayer player = new FakePlayer(1, "Player A");
        _players.Add(player);
        _playerHands[player] = new List<Tile> { new Tile(6, 6) };
        _board.PlayedTiles.Clear();
        _board.PlayedTiles.Add(new Tile(2, 4));

        Assert.That(_controller.IsValidMove(player, new Tile(6, 6), BoardEnd.Right), Is.False);
    }

    [Test]
    public void IsValidMove_PlayerNotHoldingTile_ReturnsFalse()
    {
        IPlayer player = new FakePlayer(1, "Player A");
        _players.Add(player);
        _playerHands[player] = new List<Tile> { new Tile(4, 6) };
        _board.PlayedTiles.Clear();
        _board.PlayedTiles.Add(new Tile(2, 4));

        IPlayer opponent = new FakePlayer(2, "Player B");

        Assert.That(_controller.IsValidMove(opponent, new Tile(4, 6), BoardEnd.Right), Is.False);
    }

    [Test]
    public void IsValidMove_WithInvalidTileIndex_ReturnsFalse()
    {
        GameController controller = CreateStartedGameController();

        Assert.That(controller.IsValidMove(-1, BoardEnd.Right), Is.False);
        Assert.That(controller.IsValidMove(999, BoardEnd.Right), Is.False);
    }

    private sealed class FakePlayer : IPlayer
    {
        public FakePlayer(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public int Id { get; }
        public string Name { get; }
    }
}
