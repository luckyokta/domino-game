using DominoGame.Core;
using DominoGame.Core.Enums;
using DominoGame.Core.Interfaces;
using DominoGame.Core.Models;
using DominoGame.Core.Factories;
using Microsoft.Extensions.Logging.Abstractions;

namespace DominoGame.Tests;

public class GameControllerCalculateRoundScoreTests
{
    private readonly DominoFactory _dominoFactory = new();
    private readonly List<IPlayer> _players = new();
    private readonly Dictionary<IPlayer, List<Tile>> _playerHands = new();
    private readonly Dictionary<IPlayer, int> _playerScores = new();
    private readonly DominoBoard _board = new();
    private readonly Boneyard _boneyard = new(new List<Tile>());
    private GameController _controller;

    public GameControllerCalculateRoundScoreTests()
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
    public void CalculateRoundScore_RoundWinnerDetermined_SumsLosingPlayersPips()
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
        controller.PlayerHands[otherPlayer].Clear();
        controller.PlayerHands[otherPlayer].Add(new Tile(6, 6));

        controller.PlayTurn(currentPlayer, new Tile(2, 4), BoardEnd.Right);

        Assert.That(controller.RoundWinner, Is.SameAs(currentPlayer));
        Assert.That(controller.CalculateRoundScore(), Is.EqualTo(12));
    }
}
