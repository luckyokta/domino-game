using DominoGame.Api.Dtos;
using DominoGame.Api.Hubs;
using DominoGame.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace DominoGame.Api.Controllers;

/// <summary>
/// REST API for the domino game. A player proves who they are with the token they received
/// when creating/joining a room, sent in the X-Player-Token header.
/// Errors are returned as { "error": "message" }.
/// </summary>
[ApiController]
[Route("api/games")]
public class GamesController : ControllerBase
{
    private const string TokenHeader = "X-Player-Token";

    private readonly GameSessionStore _store;
    private readonly IHubContext<GameHub> _hub;

    public GamesController(GameSessionStore store, IHubContext<GameHub> hub)
    {
        _store = store;
        _hub = hub;
    }

    /// <summary>Create a room; the caller becomes the host.</summary>
    [HttpPost]
    public IActionResult Create([FromBody] CreateGameRequest request)
    {
        int targetScore = request.TargetScore ?? 100;

        if (targetScore < 10 || targetScore > 1000)
        {
            return BadRequest(new { error = "Target score must be between 10 and 1000." });
        }

        GameSession session = _store.Create(targetScore);
        SessionResult result = session.AddPlayer(request.PlayerName, true, out string token, out int seat);

        if (!result.Ok)
        {
            _store.Remove(session.Code);
            return BadRequest(new { error = result.Error });
        }

        return Ok(new JoinResultDto(session.Code, token, seat));
    }

    /// <summary>Join an existing room that hasn't started yet.</summary>
    [HttpPost("{code}/join")]
    public async Task<IActionResult> Join(string code, [FromBody] JoinGameRequest request)
    {
        GameSession? session = _store.Get(code);

        if (session == null)
        {
            return NotFound(new { error = "Room not found. Check the code and try again." });
        }

        SessionResult result = session.AddPlayer(request.PlayerName, false, out string token, out int seat);

        if (!result.Ok)
        {
            return BadRequest(new { error = result.Error });
        }

        await NotifyAsync(session);
        return Ok(new JoinResultDto(session.Code, token, seat));
    }

    /// <summary>The current state of the room, as seen by the calling player.</summary>
    [HttpGet("{code}")]
    public IActionResult GetState(string code, [FromHeader(Name = TokenHeader)] string? token)
    {
        GameSession? session = _store.Get(code);

        if (session == null)
        {
            return NotFound(new { error = "Room not found." });
        }

        GameStateDto? state = session.BuildState(token);

        if (state == null)
        {
            return Unauthorized(new { error = "You are not a player in this room." });
        }

        return Ok(state);
    }

    /// <summary>Host only: start the match (or start a new one after a match has finished).</summary>
    [HttpPost("{code}/start")]
    public Task<IActionResult> Start(string code, [FromHeader(Name = TokenHeader)] string? token)
    {
        return ActAsync(code, token, session => session.Start(token));
    }

    /// <summary>Play a tile from your hand on the left or right end of the board.</summary>
    [HttpPost("{code}/play")]
    public Task<IActionResult> Play(string code, [FromBody] PlayTileRequest request, [FromHeader(Name = TokenHeader)] string? token)
    {
        return ActAsync(code, token, session => session.Play(token, request.Left, request.Right, request.End));
    }

    /// <summary>Draw a tile from the boneyard (only when you have no playable tile).</summary>
    [HttpPost("{code}/draw")]
    public Task<IActionResult> Draw(string code, [FromHeader(Name = TokenHeader)] string? token)
    {
        return ActAsync(code, token, session => session.Draw(token));
    }

    /// <summary>Pass (only when you have no playable tile and the boneyard is empty).</summary>
    [HttpPost("{code}/pass")]
    public Task<IActionResult> Pass(string code, [FromHeader(Name = TokenHeader)] string? token)
    {
        return ActAsync(code, token, session => session.Pass(token));
    }

    /// <summary>Start the next round once the current one is over.</summary>
    [HttpPost("{code}/next-round")]
    public Task<IActionResult> NextRound(string code, [FromHeader(Name = TokenHeader)] string? token)
    {
        return ActAsync(code, token, session => session.NextRound(token));
    }

    // ------------------------------------------------------------------

    private async Task<IActionResult> ActAsync(string code, string? token, Func<GameSession, SessionResult> action)
    {
        GameSession? session = _store.Get(code);

        if (session == null)
        {
            return NotFound(new { error = "Room not found." });
        }

        if (session.FindSeat(token) < 0)
        {
            return Unauthorized(new { error = "You are not a player in this room." });
        }

        SessionResult result = action(session);

        if (!result.Ok)
        {
            return BadRequest(new { error = result.Error });
        }

        await NotifyAsync(session);

        return Ok(session.BuildState(token));
    }

    private Task NotifyAsync(GameSession session)
    {
        return _hub.Clients.Group(session.Code).SendAsync("GameUpdated");
    }
}
