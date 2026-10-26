# Domino – online multiplayer

React (Vite, JavaScript) frontend + ASP.NET Core Web API + SignalR, built around your `GameController`.

```
DominoGame/
├─ DominoGame.sln
├─ Directory.Build.props          ← target framework (net8.0) set here, once
├─ src/
│  ├─ DominoGame.Core/            ← YOUR code, split into files, logic unchanged
│  └─ DominoGame.Api/             ← Web API + SignalR hub
│     ├─ Controllers/GamesController.cs   REST endpoints
│     ├─ Hubs/GameHub.cs                  "something changed" push notifications
│     ├─ Services/GameSession.cs          one room = one GameController + seats + log + lock
│     ├─ Services/GameSessionStore.cs     in-memory rooms by code
│     └─ Dtos/Dtos.cs                     request/response shapes
└─ client/                        ← Vite + React (JavaScript)
```

## Run it (development)

You need the .NET SDK (8 or newer) and Node.js 18+.

```bash
# terminal 1 – API on http://localhost:5080
dotnet run --project src/DominoGame.Api

# terminal 2 – React app on http://localhost:5173
cd client
npm install
npm run dev
```

Open http://localhost:5173, create a room, and open the invite link in other browsers/devices
(on the same network use your computer's IP, e.g. `http://192.168.1.20:5173`; start Vite with `npm run dev -- --host`).

Vite proxies `/api` and `/hubs` to the API, so there is no CORS setup in development.

## Run it as one app (production-style)

```bash
cd client && npm install && npm run build   # writes into src/DominoGame.Api/wwwroot
cd .. && dotnet run --project src/DominoGame.Api -c Release
```

The API now serves the React app too, at http://localhost:5080.

If you host the frontend and API on different origins: set `VITE_API_URL` when building the client and add
the frontend origin to `Cors:Origins` in `appsettings.json`.

## How it works

- **Rooms**: `POST /api/games` creates a room (5-letter code) and makes the caller host. Others join with the code.
  Each player receives a secret **token** (stored in `localStorage`, so refreshing the page keeps your seat) sent as `X-Player-Token`.
- **Hidden hands**: `GET /api/games/{code}` returns a *per-player* view: your own hand (with which tiles can go on which end),
  and only tile *counts* for opponents. Hands are revealed only when a round ends.
- **Live updates**: after every action the server sends a tiny `GameUpdated` message through SignalR to the room;
  each client then re-fetches its own view. If the socket drops, the client polls every 3 s until it reconnects.
- **Rules stay in your code**: the API never re-implements game rules. `GameSession` validates input to give readable
  errors, then calls `PlayTurn`, `DrawTile`, `PassTurn`, `StartNewRound`; your `GameController` events feed the activity log.
  One lock per room, because `GameController` is not thread-safe.

### Endpoints

| Method | Path | Notes |
|---|---|---|
| POST | `/api/games` | `{ playerName, targetScore? }` → `{ code, token, seat }` |
| POST | `/api/games/{code}/join` | `{ playerName }` → `{ code, token, seat }` |
| GET | `/api/games/{code}` | your view of the room (needs `X-Player-Token`) |
| POST | `/api/games/{code}/start` | host only; also "play again" after a finished match |
| POST | `/api/games/{code}/play` | `{ left, right, end: "left"｜"right" }` |
| POST | `/api/games/{code}/draw` | only when you have no playable tile and the boneyard isn't empty |
| POST | `/api/games/{code}/pass` | only when you have no playable tile and the boneyard is empty |
| POST | `/api/games/{code}/next-round` | any player, once a round is over |
| WS | `/hubs/game` | hub method `JoinRoom(code, token)`; server event `GameUpdated` |

Errors are `{ "error": "message" }` with 400 / 401 / 404.

## Things to know

- Rooms live in memory: restarting the API ends all games (clients are sent back to the home screen). Idle rooms (6 h) are purged.
- **Rules observed in your `GameController`** (left as-is, since you asked me to build on it): the first player is chosen by highest
  double, but the controller does not force them to *play* that double. A round has a single `RoundWinner`; when a blocked round ends in a
  tie, `RoundWinner` is `null` and nobody scores (the web and console screens say so, and the next round starts as usual).
- A player who clicks **Leave** forgets their token; their seat stays occupied for the rest of that room.
- To use another .NET version, change `TargetFramework` in `Directory.Build.props`.

## Console version (Spectre.Console)

A hot-seat console game for 2–4 players on one keyboard, using the same `DominoGame.Core` rules.

```bash
dotnet run --project src/DominoGame.ConsoleApp
```

- Set-up asks for the number of players, their names and a custom target score (10–1000, default 100).
- Each turn starts with a "pass the keyboard" screen so the previous player's hand is never left on screen.
- Moves are chosen from a menu (arrow keys + Enter): play a tile (the end is asked only when the tile fits both ends),
  draw from the boneyard, or pass. Only legal actions are offered, based on the controller's `IsValidMove`, `CanDraw` and `CanPass`.
- The board is drawn as a snake that wraps to the terminal width; yellow tiles are doubles.
- After each round all hands are revealed with pip counts and scores; at match end you can play again with the same players.
- Use a terminal with UTF-8 support (Windows Terminal, macOS Terminal, most Linux terminals).
