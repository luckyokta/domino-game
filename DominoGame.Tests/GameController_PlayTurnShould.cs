using DominoGame.Core;
using DominoGame.Core.Enums;
using DominoGame.Core.Interfaces;
using DominoGame.Core.Models;
using DominoGame.Core.Factories;
using Microsoft.Extensions.Logging.Abstractions;

namespace DominoGame.Tests;

public class GameControllerPlayTurnTests
{
    private readonly DominoFactory _dominoFactory = new();
    private readonly List<IPlayer> _players = new();
    private readonly Dictionary<IPlayer, List<Tile>> _playerHands = new();
    private readonly Dictionary<IPlayer, int> _playerScores = new();
    private readonly DominoBoard _board = new();
    private readonly Boneyard _boneyard = new(new List<Tile>());
    private GameController _controller;

    public GameControllerPlayTurnTests()
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

    private GameController CreateController()
    {
        return new GameController(
            new DominoBoard(),
            new Boneyard(new List<Tile>()),
            new List<IPlayer>(),
            new Dictionary<IPlayer, List<Tile>>(),
            new Dictionary<IPlayer, int>(),
            new DominoFactory(),
            NullLogger<GameController>.Instance);
    }

    private GameController CreateStartedGameController()
    {
        GameController controller = CreateController();
        controller.StartNewGame(new List<string> { "Player A", "Player B" }, 100);

        return controller;
    }

    [Test]
    public void PlayTurn_RoundNotInProgress_DoesNotPlayTile()
    {
        GameController controller = CreateController();
        IPlayer player = new FakePlayer(1, "Player A");
        controller.Players.Add(player);
        controller.PlayerHands[player] = new List<Tile> { new Tile(3, 5) };

        controller.PlayTurn(player, new Tile(3, 5), BoardEnd.Right);

        Assert.That(controller.Board.PlayedTiles.Count, Is.EqualTo(0));
        Assert.That(controller.PlayerHands[player].Count, Is.EqualTo(1));
    }

    [Test]
    public void PlayTurn_PlayerIsNotCurrentPlayer_DoesNotPlayTile()
    {
        GameController controller = CreateStartedGameController();
        IPlayer currentPlayer = controller.CurrentPlayer!;
        IPlayer otherPlayer = controller.Players.First(p => p != currentPlayer);

        controller.PlayerHands[currentPlayer].Clear();
        controller.PlayerHands[currentPlayer].Add(new Tile(3, 5));
        controller.PlayerHands[otherPlayer].Clear();
        controller.PlayerHands[otherPlayer].Add(new Tile(6, 6));

        controller.PlayTurn(otherPlayer, new Tile(6, 6), BoardEnd.Right);

        Assert.That(controller.Board.PlayedTiles.Count, Is.EqualTo(0));
        Assert.That(controller.PlayerHands[otherPlayer].Count, Is.EqualTo(1));
    }

    [Test]
    public void PlayTurn_InvalidMove_DoesNotPlayTile()
    {
        GameController controller = CreateStartedGameController();
        IPlayer currentPlayer = controller.CurrentPlayer!;

        controller.PlayerHands[currentPlayer].Clear();
        controller.PlayerHands[currentPlayer].Add(new Tile(6, 6));
        controller.Board.PlayedTiles.Clear();
        controller.Board.PlayedTiles.Add(new Tile(2, 4));

        controller.PlayTurn(currentPlayer, new Tile(6, 6), BoardEnd.Right);

        Assert.That(controller.Board.PlayedTiles.Count, Is.EqualTo(1));
        Assert.That(controller.PlayerHands[currentPlayer].Count, Is.EqualTo(1));
        Assert.That(controller.CurrentPlayer, Is.SameAs(currentPlayer));
    }

    [Test]
    public void PlayTurn_ValidMove_PlaysTileAndAdvancesTurn()
    {
        GameController controller = CreateStartedGameController();
        IPlayer currentPlayer = controller.CurrentPlayer!;
        IPlayer otherPlayer = controller.Players.First(p => p != currentPlayer);

        controller.PlayerHands[currentPlayer].Clear();
        controller.PlayerHands[currentPlayer].Add(new Tile(2, 4));
        controller.PlayerHands[currentPlayer].Add(new Tile(4, 6));
        controller.PlayerHands[otherPlayer].Clear();
        controller.PlayerHands[otherPlayer].Add(new Tile(6, 6));

        Tile? playedTile = null;
        BoardEnd? playedEnd = null;
        IPlayer? playedPlayer = null;

        controller.TilePlayed += (player, tile, end) =>
        {
            playedPlayer = player;
            playedTile = tile;
            playedEnd = end;
        };

        controller.PlayTurn(currentPlayer, new Tile(2, 4), BoardEnd.Right);

        Assert.That(controller.Board.PlayedTiles.Count, Is.EqualTo(1));
        Assert.That(controller.Board.PlayedTiles[0], Is.EqualTo(new Tile(2, 4)));
        Assert.That(controller.PlayerHands[currentPlayer].Count, Is.EqualTo(1));
        Assert.That(controller.PlayerHands[currentPlayer][0], Is.EqualTo(new Tile(4, 6)));
        Assert.That(controller.CurrentPlayer, Is.SameAs(otherPlayer));
        Assert.That(controller.CanDraw, Is.True);
        Assert.That(controller.CanPass, Is.False);
        Assert.That(playedPlayer, Is.SameAs(currentPlayer));
        Assert.That(playedTile, Is.EqualTo(new Tile(2, 4)));
        Assert.That(playedEnd, Is.EqualTo(BoardEnd.Right));
    }

    [Test]
    public void PlayTurn_ValidMoveWithTileIndex_PlaysTileAndAdvancesTurn()
    {
        GameController controller = CreateStartedGameController();
        IPlayer currentPlayer = controller.CurrentPlayer!;
        IPlayer otherPlayer = controller.Players.First(p => p != currentPlayer);

        controller.PlayerHands[currentPlayer].Clear();
        controller.PlayerHands[currentPlayer].Add(new Tile(2, 4));
        controller.PlayerHands[currentPlayer].Add(new Tile(4, 6));
        controller.PlayerHands[otherPlayer].Clear();
        controller.PlayerHands[otherPlayer].Add(new Tile(6, 6));

        controller.PlayTurn(currentPlayer, 0, BoardEnd.Right);

        Assert.That(controller.Board.PlayedTiles.Count, Is.EqualTo(1));
        Assert.That(controller.Board.PlayedTiles[0], Is.EqualTo(new Tile(2, 4)));
        Assert.That(controller.CurrentPlayer, Is.SameAs(otherPlayer));
    }

    [Test]
    public void PlayTurn_PlayerEmptiesHand_CompletesRoundAndAwardsScore()
    {
        GameController controller = CreateStartedGameController();
        IPlayer currentPlayer = controller.CurrentPlayer!;
        IPlayer otherPlayer = controller.Players.First(p => p != currentPlayer);

        controller.PlayerHands[currentPlayer].Clear();
        controller.PlayerHands[currentPlayer].Add(new Tile(2, 4));
        controller.PlayerHands[otherPlayer].Clear();
        controller.PlayerHands[otherPlayer].Add(new Tile(6, 6));

        IPlayer? scoredWinner = null;
        int scoredAmount = -1;
        IPlayer? roundOverWinner = null;

        controller.PlayerScored += (winner, score) =>
        {
            scoredWinner = winner;
            scoredAmount = score;
        };

        controller.RoundOver += winner => roundOverWinner = winner;

        controller.PlayTurn(currentPlayer, new Tile(2, 4), BoardEnd.Right);

        Assert.That(controller.RoundWinner, Is.SameAs(currentPlayer));
        Assert.That(controller.RoundStatus, Is.EqualTo(RoundStatus.Completed));
        Assert.That(scoredWinner, Is.SameAs(currentPlayer));
        Assert.That(scoredAmount, Is.EqualTo(12));
        Assert.That(roundOverWinner, Is.SameAs(currentPlayer));
        Assert.That(controller.PlayerScores[currentPlayer], Is.EqualTo(12));
        Assert.That(controller.Status, Is.EqualTo(GameStatus.InProgress));
    }

    [Test]
    public void PlayTurn_GameIsBlocked_CompletesRound()
    {
        GameController controller = CreateStartedGameController();
        IPlayer currentPlayer = controller.CurrentPlayer!;

        controller.PlayerHands[currentPlayer].Clear();
        controller.PlayerHands[currentPlayer].Add(new Tile(2, 4));
        controller.PlayerHands[currentPlayer].Add(new Tile(6, 6));
        controller.PlayerHands[controller.Players.First(p => p != currentPlayer)].Clear();
        controller.PlayerHands[controller.Players.First(p => p != currentPlayer)].Add(new Tile(5, 5));
        controller.Boneyard.Tiles.Clear();

        controller.PlayTurn(currentPlayer, new Tile(2, 4), BoardEnd.Right);

        Assert.That(controller.RoundStatus, Is.EqualTo(RoundStatus.Completed));
        Assert.That(controller.RoundWinner, Is.Not.Null);
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
