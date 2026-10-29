using DominoGame.Core;
using DominoGame.Core.Enums;
using DominoGame.Core.Interfaces;
using DominoGame.Core.Models;
using DominoGame.Core.Factories;
using Microsoft.Extensions.Logging.Abstractions;

namespace DominoGame.Tests;

public class GameControllerCanPlayTileTests
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
    public void CanPlayTile_BoardEmpty_ReturnsTrue()
    {
        Assert.That(_controller.CanPlayTile(new Tile(6, 6)), Is.True);
    }

    [Test]
    public void CanPlayTile_TileMatchingBoardEnd_ReturnsTrue()
    {
        _board.PlayedTiles.Clear();
        _board.PlayedTiles.Add(new Tile(3, 5));

        Assert.That(_controller.CanPlayTile(new Tile(5, 2)), Is.True);
        Assert.That(_controller.CanPlayTile(new Tile(2, 3)), Is.True);
    }

    [Test]
    public void CanPlayTile_TileNotMatchingBoardEnd_ReturnsFalse()
    {
        _board.PlayedTiles.Clear();
        _board.PlayedTiles.Add(new Tile(3, 5));

        Assert.That(_controller.CanPlayTile(new Tile(6, 6)), Is.False);
    }

    [Test]
    public void CanPlayTile_WithInvalidTileIndex_ReturnsFalse()
    {
        GameController controller = CreateStartedGameController();
        IPlayer currentPlayer = controller.CurrentPlayer!;

        controller.PlayerHands[currentPlayer].Clear();
        controller.PlayerHands[currentPlayer].Add(new Tile(3, 5));

        Assert.That(controller.CanPlayTile(-1), Is.False);
        Assert.That(controller.CanPlayTile(0), Is.True);
        Assert.That(controller.CanPlayTile(1), Is.False);
    }
}
