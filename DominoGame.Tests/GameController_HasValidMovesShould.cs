using DominoGame.Core;
using DominoGame.Core.Enums;
using DominoGame.Core.Interfaces;
using DominoGame.Core.Models;
using DominoGame.Core.Factories;
using Microsoft.Extensions.Logging.Abstractions;

namespace DominoGame.Tests;

public class GameControllerHasValidMovesTests
{
    private readonly DominoFactory _dominoFactory = new();
    private readonly List<IPlayer> _players = new();
    private readonly Dictionary<IPlayer, List<Tile>> _playerHands = new();
    private readonly Dictionary<IPlayer, int> _playerScores = new();
    private readonly DominoBoard _board = new();
    private readonly Boneyard _boneyard = new(new List<Tile>());
    private GameController _controller;

    public GameControllerHasValidMovesTests()
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
    public void HasValidMoves_BoardEmpty_ReturnsTrue()
    {
        IPlayer player = new FakePlayer(1, "Player A");
        _players.Add(player);
        _playerHands[player] = new List<Tile> { new Tile(6, 6) };

        Assert.That(_controller.HasValidMoves(player), Is.True);
    }

    [Test]
    public void HasValidMoves_PlayerWithoutValidMove_ReturnsFalse()
    {
        IPlayer player = new FakePlayer(1, "Player A");
        _players.Add(player);
        _playerHands[player] = new List<Tile> { new Tile(6, 6) };
        _board.PlayedTiles.Clear();
        _board.PlayedTiles.Add(new Tile(2, 4));

        Assert.That(_controller.HasValidMoves(player), Is.False);
    }

    [Test]
    public void HasValidMoves_PlayerWithValidMove_ReturnsTrue()
    {
        IPlayer player = new FakePlayer(1, "Player A");
        _players.Add(player);
        _playerHands[player] = new List<Tile> { new Tile(5, 2) };
        _board.PlayedTiles.Clear();
        _board.PlayedTiles.Add(new Tile(2, 4));

        Assert.That(_controller.HasValidMoves(player), Is.True);
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
