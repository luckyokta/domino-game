namespace DominoGame.Api.Dtos;

// ---------- Requests ----------

public class CreateGameRequest
{
    public string? PlayerName { get; set; }
    public int? TargetScore { get; set; }
}

public class JoinGameRequest
{
    public string? PlayerName { get; set; }
}

public class PlayTileRequest
{
    public int Left { get; set; }
    public int Right { get; set; }

    /// <summary>"left" or "right" (case-insensitive).</summary>
    public string? End { get; set; }
}

// ---------- Responses ----------

public record JoinResultDto(string Code, string Token, int Seat);

public record TileDto(int Left, int Right);

public record HandTileDto(int Left, int Right, bool CanPlayLeft, bool CanPlayRight);

public record PlayerDto(
    int Seat,
    string Name,
    bool IsHost,
    bool IsYou,
    bool IsCurrent,
    int TileCount,
    int Score,
    List<TileDto>? RevealedHand);

/// <summary>
/// Everything one player is allowed to see. Opponents' hands are only revealed
/// (via PlayerDto.RevealedHand) once the round is over.
/// Phase is one of: "lobby", "playing", "roundOver", "matchOver".
/// </summary>
public record GameStateDto(
    string Code,
    string Phase,
    int TargetScore,
    int Round,
    int YourSeat,
    List<PlayerDto> Players,
    List<HandTileDto> Hand,
    List<TileDto> Board,
    int? LeftEnd,
    int? RightEnd,
    int BoneyardCount,
    int? CurrentSeat,
    bool IsYourTurn,
    bool CanDraw,
    bool CanPass,
    bool CanStart,
    int? RoundWinner,
    int? MatchWinner,
    int LastRoundPoints,
    List<string> Log);
