using DominoGame.Api.Services;
using Microsoft.AspNetCore.SignalR;

namespace DominoGame.Api.Hubs;

/// <summary>
/// Used only to push "something changed" notifications. Clients connect, call JoinRoom,
/// and then receive "GameUpdated" whenever anything happens in their room. They then
/// fetch their own personalised state from GET /api/games/{code}.
/// All game actions go through the REST controller.
/// </summary>
public class GameHub : Hub
{
    private readonly GameSessionStore _store;

    public GameHub(GameSessionStore store)
    {
        _store = store;
    }

    public async Task JoinRoom(string code, string token)
    {
        GameSession? session = _store.Get(code);

        if (session == null || session.FindSeat(token) < 0)
        {
            throw new HubException("Invalid room or player token.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, session.Code);
    }
}
