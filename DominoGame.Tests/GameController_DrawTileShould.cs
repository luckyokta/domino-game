using DominoGame.Core;
using DominoGame.Core.Enums;
using DominoGame.Core.Interfaces;
using DominoGame.Core.Models;
using DominoGame.Core.Factories;
using Microsoft.Extensions.Logging.Abstractions;

namespace DominoGame.Tests;

public class GameControllerDrawTileTests
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
    public void DrawTile_NoValidMoveAndCanDrawEnabled_DrawsTileFromBoneyard()
    {
        GameController controller = CreateStartedGameController();
        IPlayer currentPlayer = controller.CurrentPlayer!;
        IPlayer otherPlayer = controller.Players.First(p => p != currentPlayer);

        controller.PlayerHands[currentPlayer].Clear();
        controller.PlayerHands[currentPlayer].Add(new Tile(2, 4));
        controller.PlayerHands[currentPlayer].Add(new Tile(4, 6));
        controller.PlayerHands[otherPlayer].Clear();
        controller.PlayerHands[otherPlayer].Add(new Tile(6, 6));

        controller.PlayTurn(currentPlayer, new Tile(2, 4), BoardEnd.Right);

        Assert.That(controller.CanDraw, Is.True);

        Tile? drawnTile = null;
        IPlayer? drawnBy = null;
        controller.TileDrawn += (player, tile) =>
        {
            drawnBy = player;
            drawnTile = tile;
        };

        Tile? drawn = controller.DrawTile(otherPlayer);

        Assert.That(drawn, Is.Not.Null);
        Assert.That(drawnBy, Is.SameAs(otherPlayer));
        Assert.That(drawnTile, Is.Not.Null);
        Assert.That(controller.Boneyard.RemainingCount, Is.EqualTo(13));
        Assert.That(controller.PlayerHands[otherPlayer].Count, Is.EqualTo(2));
    }

    [Test]
    public void DrawTile_BoneyardEmpty_ReturnsNull()
    {
        GameController controller = CreateStartedGameController();
        IPlayer currentPlayer = controller.CurrentPlayer!;
        IPlayer otherPlayer = controller.Players.First(p => p != currentPlayer);

        controller.PlayerHands[currentPlayer].Clear();
        controller.PlayerHands[currentPlayer].Add(new Tile(2, 4));
        controller.PlayerHands[currentPlayer].Add(new Tile(4, 6));
        controller.PlayerHands[otherPlayer].Clear();
        controller.PlayerHands[otherPlayer].Add(new Tile(6, 6));

        controller.PlayTurn(currentPlayer, new Tile(2, 4), BoardEnd.Right);

        Assert.That(controller.CanDraw, Is.True);
        controller.Boneyard.Tiles.Clear();

        Assert.That(controller.DrawTile(otherPlayer), Is.Null);
    }

    [Test]
    public void DrawTile_PlayerIsNotCurrentPlayer_ReturnsNull()
    {
        GameController controller = CreateStartedGameController();
        IPlayer currentPlayer = controller.CurrentPlayer!;
        IPlayer otherPlayer = controller.Players.First(p => p != currentPlayer);

        controller.PlayerHands[currentPlayer].Clear();
        controller.PlayerHands[currentPlayer].Add(new Tile(2, 4));
        controller.PlayerHands[currentPlayer].Add(new Tile(4, 6));
        controller.PlayerHands[otherPlayer].Clear();
        controller.PlayerHands[otherPlayer].Add(new Tile(6, 6));

        controller.PlayTurn(currentPlayer, new Tile(2, 4), BoardEnd.Right);

        Assert.That(controller.CanDraw, Is.True);

        // The turn has already advanced to otherPlayer, so drawing with
        // currentPlayer's identity must be rejected because it is not their turn.
        Assert.That(controller.DrawTile(currentPlayer), Is.Null);
    }
}
