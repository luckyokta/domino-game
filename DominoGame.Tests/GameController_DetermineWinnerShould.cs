using DominoGame.Core;
using DominoGame.Core.Enums;
using DominoGame.Core.Interfaces;
using DominoGame.Core.Models;
using DominoGame.Core.Factories;
using Microsoft.Extensions.Logging.Abstractions;

namespace DominoGame.Tests;

public class GameControllerDetermineWinnerTests
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
    public void DetermineWinner_PlayerEmptiedHand_ReturnsThatPlayer()
    {
        IPlayer playerA = new FakePlayer(1, "Player A");
        IPlayer playerB = new FakePlayer(2, "Player B");
        _players.Add(playerA);
        _players.Add(playerB);
        _playerHands[playerA] = new List<Tile>();
        _playerHands[playerB] = new List<Tile> { new Tile(6, 6) };
        _boneyard.Tiles.Add(new Tile(5, 5));

        IPlayer? winner = _controller.DetermineWinner();

        Assert.That(winner, Is.SameAs(playerA));
    }

    [Test]
    public void DetermineWinner_BlockedGame_ReturnsLowestPipsPlayer()
    {
        IPlayer playerA = new FakePlayer(1, "Player A");
        IPlayer playerB = new FakePlayer(2, "Player B");
        _players.Add(playerA);
        _players.Add(playerB);
        _playerHands[playerA] = new List<Tile> { new Tile(5, 5) };
        _playerHands[playerB] = new List<Tile> { new Tile(6, 6) };
        _board.PlayedTiles.Clear();
        _board.PlayedTiles.Add(new Tile(2, 4));
        _boneyard.Tiles.Clear();

        IPlayer? winner = _controller.DetermineWinner();

        Assert.That(winner, Is.SameAs(playerA));
    }

    [Test]
    public void DetermineWinner_Tie_ReturnsNull()
    {
        IPlayer playerA = new FakePlayer(1, "Player A");
        IPlayer playerB = new FakePlayer(2, "Player B");
        _players.Add(playerA);
        _players.Add(playerB);
        _playerHands[playerA] = new List<Tile> { new Tile(3, 3) };
        _playerHands[playerB] = new List<Tile> { new Tile(2, 4) };
        _board.PlayedTiles.Clear();
        _board.PlayedTiles.Add(new Tile(6, 6));
        _boneyard.Tiles.Clear();

        IPlayer? winner = _controller.DetermineWinner();

        Assert.That(winner, Is.Null);
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
