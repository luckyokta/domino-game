using DominoGame.Core;
using DominoGame.Core.Enums;
using DominoGame.Core.Interfaces;
using DominoGame.Core.Models;
using DominoGame.Core.Factories;
using Microsoft.Extensions.Logging.Abstractions;

namespace DominoGame.Tests;

public class GameControllerDetermineFirstPlayerTests
{
    private readonly DominoFactory _dominoFactory = new();
    private readonly List<IPlayer> _players = new();
    private readonly Dictionary<IPlayer, List<Tile>> _playerHands = new();
    private readonly Dictionary<IPlayer, int> _playerScores = new();
    private readonly DominoBoard _board = new();
    private readonly Boneyard _boneyard = new(new List<Tile>());
    private GameController _controller;

    public GameControllerDetermineFirstPlayerTests()
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

    [Test]
    public void DetermineFirstPlayer_HighestDouble_ReturnsPlayerWithHighestDouble()
    {
        IPlayer playerA = new FakePlayer(1, "Player A");
        IPlayer playerB = new FakePlayer(2, "Player B");
        _players.Add(playerA);
        _players.Add(playerB);
        _playerHands[playerA] = new List<Tile> { new Tile(6, 6), new Tile(5, 3) };
        _playerHands[playerB] = new List<Tile> { new Tile(6, 5) };

        IPlayer? firstPlayer = _controller.DetermineFirstPlayer();

        Assert.That(firstPlayer, Is.SameAs(playerA));
    }

    [Test]
    public void DetermineFirstPlayer_NoDoubles_ReturnsPlayerWithHighestTotalPipsTile()
    {
        IPlayer playerA = new FakePlayer(1, "Player A");
        IPlayer playerB = new FakePlayer(2, "Player B");
        _players.Add(playerA);
        _players.Add(playerB);
        _playerHands[playerA] = new List<Tile> { new Tile(3, 4) };
        _playerHands[playerB] = new List<Tile> { new Tile(6, 5) };

        IPlayer? firstPlayer = _controller.DetermineFirstPlayer();

        Assert.That(firstPlayer, Is.SameAs(playerB));
    }

    [Test]
    public void DetermineFirstPlayer_NoPlayers_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() => _controller.DetermineFirstPlayer());
    }
}
