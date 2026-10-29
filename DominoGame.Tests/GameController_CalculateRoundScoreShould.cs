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
        _controller.StartNewGame(new List<string> { "Player A", "Player B" }, 100);
        IPlayer currentPlayer = _controller.CurrentPlayer!;
        IPlayer otherPlayer = _controller.Players.First(p => p != currentPlayer);

        _controller.PlayerHands[currentPlayer].Clear();
        _controller.PlayerHands[currentPlayer].Add(new Tile(2, 4));
        _controller.PlayerHands[otherPlayer].Clear();
        _controller.PlayerHands[otherPlayer].Add(new Tile(6, 6));

        _controller.PlayTurn(currentPlayer, new Tile(2, 4), BoardEnd.Right);

        Assert.That(_controller.RoundWinner, Is.SameAs(currentPlayer));
        Assert.That(_controller.CalculateRoundScore(), Is.EqualTo(12));
    }
}
