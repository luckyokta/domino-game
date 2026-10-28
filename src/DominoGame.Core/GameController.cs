using DominoGame.Core.Interfaces;
using DominoGame.Core.Models;
using DominoGame.Core.Enums;
using DominoGame.Core.Factories;

namespace DominoGame.Core
{
    public class GameController
    {
        private int _currentPlayerIndex;
        private readonly DominoFactory _dominoFactory;
        public Dictionary<IPlayer, List<Tile>> PlayerHands {get; private set;}
        public Dictionary<IPlayer, int> PlayerScores {get; private set;}
        public List<IPlayer> Players {get; private set;}
        public IPlayer? CurrentPlayer => Players.Count > 0 ? Players[_currentPlayerIndex] : null;
        public IBoard Board {get; private set;}
        public IBoneyard Boneyard {get; private set;}
        public bool CanDraw {get; private set;}
        public bool CanPass {get; private set;}
        public int TargetScore {get; private set;}
        public int CurrentRound {get; private set;}
        public GameStatus Status {get; private set;} = GameStatus.NotStarted;
        public RoundStatus RoundStatus {get; private set;} = RoundStatus.NotStarted;
        public IPlayer? RoundWinner {get; private set;}
        public IPlayer? MatchWinner {get; private set;}

        public event Action<List<IPlayer>>? GameStarted;
        public event Action<IPlayer, Tile, BoardEnd>? TilePlayed;
        public event Action<IPlayer>? PlayerPassed;
        public event Action<IPlayer, int>? PlayerScored;
        public event Action<IPlayer?>? RoundOver;
        public event Action<IPlayer>? MatchOver;
        public event Action<IPlayer, Tile>? TileDrawn;

        public GameController(
            IBoard board,
            IBoneyard boneyard,
            List<IPlayer> players,
            Dictionary<IPlayer, List<Tile>> playerHands,
            Dictionary<IPlayer, int> playerScores,
            DominoFactory dominoFactory)
        {
            Board = board;
            Boneyard = boneyard;
            Players = players;
            PlayerHands = playerHands;
            PlayerScores = playerScores;
            _dominoFactory = dominoFactory;
        }

        public void StartNewGame(List<string> playerNames, int targetScore = 100)
        {
            // Input validation
            if (playerNames is null)
            {
                return;
            }

            bool isPlayerCountInvalid = playerNames.Count < 2 || playerNames.Count > 4;
            bool containsInvalidPlayerName = playerNames.Any(name => string.IsNullOrWhiteSpace(name));
            bool isTargetScoreInvalid = targetScore <= 0;

            if(
                isPlayerCountInvalid ||
                containsInvalidPlayerName ||
                isTargetScoreInvalid)
            {
                return;
            }

            // clear state from previous game
            Players.Clear();
            PlayerHands.Clear();
            PlayerScores.Clear();

            _currentPlayerIndex = 0;

            CanDraw = false;
            CanPass = false;
            
            RoundWinner = null;
            MatchWinner = null;


            // Players initialization
            for(int i = 0; i < playerNames.Count; i++)
            {
                Player player = new Player(playerNames[i])
                {
                    Id = i + 1
                };

                Players.Add(player);
                PlayerHands.Add(player, new List<Tile>());
                PlayerScores.Add(player, 0);
            }

            // Prepare board, tiles, and boneyard
            Board.PlayedTiles.Clear();
            Boneyard.Tiles.Clear();
            Boneyard.Tiles.AddRange(_dominoFactory.CreateDoubleSixSet());
            ShuffleBoneyard();

            // Give tiles to each player
            int tilesPerPlayer = Players.Count == 4 ? 6 : 7;
            foreach(IPlayer player in Players)
            {
                for(int i = 0; i < tilesPerPlayer; i++)
                {
                    Tile tile = Boneyard.Tiles[0];

                    PlayerHands[player].Add(tile);
                    Boneyard.Tiles.RemoveAt(0);
                }
            }

            // Determine first player
            IPlayer firstPlayer = DetermineFirstPlayer();
            _currentPlayerIndex = Players.IndexOf(firstPlayer);

            // Set initial game state
            TargetScore = targetScore;
            CurrentRound = 1;
            Status = GameStatus.InProgress;
            RoundStatus = RoundStatus.InProgress;

            // Notify subscribers
            GameStarted?.Invoke(Players);
        }

        private void ShuffleBoneyard()
        {
            Random random = Random.Shared;

            for(int i = Boneyard.Tiles.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (Boneyard.Tiles[i], Boneyard.Tiles[j]) = (Boneyard.Tiles[j], Boneyard.Tiles[i]);
            }
        }

        private Tile? PlayerGetHighestDouble(IPlayer player)
        {
            bool isPlayerHandContainsPlayer = PlayerHands.ContainsKey(player);

            if (!isPlayerHandContainsPlayer)
            {
                return null;
            }
            
            Tile? highestDouble = null;

            foreach(Tile tile in PlayerHands[player])
            {
                bool isDoubleTile = tile.IsDouble;

                if (isDoubleTile)
                {
                    if(highestDouble == null || tile.Left > highestDouble.Value.Left)
                    {
                        highestDouble = tile;
                    }
                }
            }

            return highestDouble;
        }

        private Tile? PlayerGetHighestTile(IPlayer player)
        {
            bool isPlayerHandContainsPlayer = PlayerHands.ContainsKey(player);

            if (!isPlayerHandContainsPlayer)
            {
                return null;
            }

            Tile? highestTile = null;

            foreach(Tile tile in PlayerHands[player])
            {
                if(highestTile == null || tile.Left + tile.Right > highestTile.Value.Left + highestTile.Value.Right)
                {
                    highestTile = tile;
                }
            }

            return highestTile;
        }

        public IPlayer DetermineFirstPlayer()
        {
            IPlayer? firstPlayer = null;
            int highestDoubleValue = -1;

            // find the player with highest double
            foreach(IPlayer player in Players)
            {
                Tile? highestDouble = PlayerGetHighestDouble(player);

                if(highestDouble != null)
                {
                    int doubleLeft = highestDouble.Value.Left;

                    bool hasHigherDouble = doubleLeft > highestDoubleValue;

                    if(hasHigherDouble)
                    {
                        highestDoubleValue = doubleLeft;
                        firstPlayer = player;
                    }
                }
            }

            // find player with highest tile if no player has highest double
            if(firstPlayer == null)
            {
                int highestTileValue = -1;

                foreach(IPlayer player in Players)
                {
                    Tile? highestTile = PlayerGetHighestTile(player);

                    if(highestTile != null)
                    {
                        int tileValue = highestTile.Value.Left + highestTile.Value.Right;

                        bool hasHigherTile = tileValue > highestTileValue;

                        if(hasHigherTile)
                        {
                            highestTileValue = tileValue;
                            firstPlayer = player;
                        }
                    }
                }
            }

            if(firstPlayer == null)
            {
                throw new InvalidOperationException("Cannot determine the first player.");
            }

            IPlayer determinedFirstPlayer = firstPlayer;
            return determinedFirstPlayer;
        }

        private bool TileIsMatch(Tile tile, int targetValue)
        {
            bool isMatch = tile.Left == targetValue || tile.Right == targetValue;

            return isMatch;
        }

        public bool CanPlayTile(Tile tile)
        {
            bool canPlayTile = Board.IsEmpty;

            if(!canPlayTile)
            {
                canPlayTile = 
                    tile.Left == Board.LeftEndValue ||
                    tile.Right == Board.LeftEndValue ||
                    tile.Left == Board.RightEndValue ||
                    tile.Right == Board.RightEndValue;
            }

            return canPlayTile;
        }

        public bool CanPlayTile(int tileIndex)
        {
            IPlayer? player = CurrentPlayer;

            if(player == null || !PlayerHands.ContainsKey(player))
            {
                return false;
            }

            if(tileIndex < 0 || tileIndex >= PlayerHands[player].Count)
            {
                return false;
            }

            bool canPlayTile = CanPlayTile(PlayerHands[player][tileIndex]);

            return canPlayTile;
        }

        public bool HasValidMoves(IPlayer player)
        {
            if (Board.IsEmpty)
            {
                return true;
            }

            foreach(Tile tile in PlayerHands[player])
            {
                if (CanPlayTile(tile))
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsValidMove(IPlayer player, Tile tile, BoardEnd end)
        {
            if(
                player == null ||
                !PlayerHands.ContainsKey(player) ||
                !PlayerHasTile(player, tile))
            {
                return false;
            }

            if (Board.IsEmpty)
            {
                return true;
            }

            return IsValidMove(tile, end);
        }

        public bool IsValidMove(int tileIndex, BoardEnd end)
        {
            IPlayer? player = CurrentPlayer;

            if(player == null || !PlayerHands.ContainsKey(player))
            {
                return false;
            }

            if(tileIndex < 0 || tileIndex >= PlayerHands[player].Count)
            {
                return false;
            }

            Tile tile = PlayerHands[player][tileIndex];

            return IsValidMove(player, tile, end);
        }

        public bool IsValidMove(Tile tile, BoardEnd end)
        {
            bool isBoardEmpty = Board.IsEmpty;
            bool isValidMove = false;

            if(isBoardEmpty)
            {
                isValidMove = true;
            }
            else
            {
                int? targetValue = end == BoardEnd.Left ? Board.LeftEndValue : Board.RightEndValue;

                if(targetValue != null)
                {
                    isValidMove = TileIsMatch(tile, targetValue.Value);
                }
            }

            return isValidMove;
        }

        private Tile TileFlip(Tile tile)
        {
            return new Tile(tile.Right, tile.Left);
        }

        private Tile TryMatchAndOrientTile(Tile tile, int targetValue, BoardEnd end)
        {
            if(!TileIsMatch(tile, targetValue))
            {
                throw new InvalidOperationException("Tile does not match the target value");
            }

            if(end == BoardEnd.Left)
            {
                if(tile.Right == targetValue)
                {
                    return tile;
                }

                return TileFlip(tile);
            }

            if(tile.Left == targetValue)
            {
                return tile;
            }

            return TileFlip(tile);
        }

        private void PlaceTile(Tile tile, BoardEnd end)
        {
            if(!IsValidMove(tile, end))
            {
                throw new InvalidOperationException("Tile cannot be placed at the selected end");
            }

            if (Board.IsEmpty)
            {
                Board.PlayedTiles.Add(tile);
                return;
            }
            
            int targetValue = end == BoardEnd.Left ? Board.LeftEndValue!.Value : Board.RightEndValue!.Value;

            Tile orientedTile = TryMatchAndOrientTile(tile, targetValue, end);

            if(end == BoardEnd.Left)
            {
                Board.PlayedTiles.Insert(0, orientedTile);
            }
            else
            {
                Board.PlayedTiles.Add(orientedTile);
            }
        }

        private void AdvanceMove()
        {
            if(Players.Count == 0)
            {
                return;
            }

            _currentPlayerIndex = (_currentPlayerIndex + 1) % Players.Count;
        }

        public void PlayTurn(IPlayer player, Tile tile, BoardEnd end)
        {
            bool isRoundInProgress = Status == GameStatus.InProgress && RoundStatus == RoundStatus.InProgress;
            bool isPlayerTurn = Equals(player, CurrentPlayer);
            bool isMoveValid = IsValidMove(player, tile, end);

            if(!isRoundInProgress || !isPlayerTurn || !isMoveValid)
            {
                return;
            }

            // Place tile on the board
            PlaceTile(tile, end);

            // Remove tile from player's hand
            PlayerRemoveTile(player, tile);

            // Notify subscriber that the tile was played
            TilePlayed?.Invoke(player, tile, end);

            // Check whether the round has ended
            bool playerEmptiedHand = PlayerHands[player].Count == 0;
            bool blockedGame = Boneyard.IsEmpty && !Players.Any(currentPlayer => HasValidMoves(currentPlayer));
            bool hasRoundEnded = playerEmptiedHand || blockedGame;

            if(hasRoundEnded)
            {
                // Determine round winner
                IPlayer? winner = DetermineWinner();

                RoundWinner = winner;

                // Calculate and award points only if there is a winner
                if(winner != null)
                {
                    int roundScore = CalculateRoundScore();

                    PlayerScores[winner] += roundScore;

                    // Notify subscribers about the score
                    PlayerScored?.Invoke(winner, roundScore);
                }

                // Mark the round as complete
                RoundStatus = RoundStatus.Completed;

                // Notify subscribers that the round is over
                RoundOver?.Invoke(winner);

                // Check whether the match has a winner
                IPlayer? matchWinner = DetermineMatchWinner();

                if(matchWinner != null)
                {
                    MatchWinner = matchWinner;
                    Status = GameStatus.Completed;

                    // Notify subscribers that the match is over
                    MatchOver?.Invoke(matchWinner);
                }

                CanDraw = false;
                CanPass = false;

                return;
            }

            // No round winner yet, advance to the next player
            AdvanceMove();

            // Determine available actions for the next player
            IPlayer? nextPlayer = CurrentPlayer;

            if(nextPlayer != null)
            {
                bool hasMoves = HasValidMoves(nextPlayer);

                CanDraw = !hasMoves && !Boneyard.IsEmpty;
                CanPass = !hasMoves && Boneyard.IsEmpty;
            }
            else
            {
                CanDraw = false;
                CanPass = false;
            }
        }

        public void PlayTurn(IPlayer player, int tileIndex, BoardEnd end)
        {
            if (!PlayerHands.ContainsKey(player))
            {
                return;
            }

            List<Tile> hand = PlayerHands[player];

            if(tileIndex < 0 || tileIndex >= hand.Count)
            {
                return;
            }

            Tile tile = hand[tileIndex];

            PlayTurn(player, tile, end);
        }

        public void PassTurn()
        {
            bool isRoundInProgress = Status == GameStatus.InProgress && RoundStatus == RoundStatus.InProgress;

            if(!isRoundInProgress)
            {
                return;
            }

            IPlayer? player = CurrentPlayer;

            if(player == null)
            {
                return;
            }

            bool canPlayerPass = CanPass;
            bool playerHasValidMoves = HasValidMoves(player);

            if(!canPlayerPass || playerHasValidMoves)
            {
                return;
            }

            PlayerPassed?.Invoke(player);

            // Check whether the game is blocked
            bool blockedGame = Boneyard.IsEmpty && !Players.Any(currentPlayer => HasValidMoves(currentPlayer));

            bool hasRoundEnded = blockedGame;

            if (hasRoundEnded)
            {
                IPlayer? winner = DetermineWinner();
                
                RoundWinner = winner;

                // Calculate and award points only if there is a winner
                if(winner != null)
                {
                    int roundScore = CalculateRoundScore();

                    PlayerScores[winner] += roundScore;

                    // Notify subscribers about the score
                    PlayerScored?.Invoke(winner, roundScore);
                }

                // Mark the round as complete
                RoundStatus = RoundStatus.Completed;

                // Notify subscriber that the round is over
                RoundOver?.Invoke(winner);

                // Check whether the match has a winner
                IPlayer? matchWinner = DetermineMatchWinner();

                if(matchWinner != null)
                {
                    MatchWinner = matchWinner;
                    Status = GameStatus.Completed;

                    // Notify subscribers that the match is over
                    MatchOver?.Invoke(matchWinner);
                }

                CanDraw = false;
                CanPass = false;

                return;
            }

            // No round winner yet, advance to the next player
            AdvanceMove();

            // Set the available actions for the next player
            IPlayer? nextPlayer = CurrentPlayer;

            if(nextPlayer != null)
            {
                bool hasMoves = HasValidMoves(nextPlayer);

                CanDraw = !hasMoves && !Boneyard.IsEmpty;
                CanPass = !hasMoves && Boneyard.IsEmpty;
            }
            else
            {
                CanDraw = false;
                CanPass = false;
            }
        }

        private bool PlayerHasTile(IPlayer player, Tile tile)
        {
            bool hasTile = PlayerHands.ContainsKey(player) && PlayerHands[player].Contains(tile);

            return hasTile;
        }

        private void PlayerRemoveTile(IPlayer player, Tile tile)
        {
            if(PlayerHasTile(player, tile))
            {
                PlayerHands[player].Remove(tile);
            }
        }

        private void PlayerAddTile(IPlayer player, Tile tile)
        {
            if (PlayerHands.ContainsKey(player))
            {
                PlayerHands[player].Add(tile);
            }
        }

        private Tile DrawFromBoneyard(Boneyard boneyard)
        {
            if (Boneyard.IsEmpty)
            {
                throw new InvalidOperationException("Cannot draw a tile from an empty boneyard.");
            }

            Tile tile = boneyard.Tiles[0];
            boneyard.Tiles.RemoveAt(0);

            return tile;
        }

        public Tile? DrawTile(IPlayer player)
        {
            bool isRoundInProgress = Status == GameStatus.InProgress && RoundStatus == RoundStatus.InProgress;
            bool isPlayerTurn = Equals(CurrentPlayer, player);
            bool canPlayerDraw = CanDraw;
            bool isBoneyardEmpty = Boneyard.IsEmpty;

            if(!isRoundInProgress || !isPlayerTurn || !canPlayerDraw || isBoneyardEmpty)
            {
                return null;
            }

            Tile tile = DrawFromBoneyard((Boneyard) Boneyard);
            PlayerAddTile(player, tile);

            TileDrawn?.Invoke(player, tile);

            // Update available actions after drawing
            bool hasMoves = HasValidMoves(player);

            CanDraw = !hasMoves && !Boneyard.IsEmpty;
            CanPass = !hasMoves && Boneyard.IsEmpty;

            return tile;
        }

        private int PlayerTotalPips(IPlayer player)
        {
            if (!PlayerHands.ContainsKey(player))
            {
                return 0;
            }

            int total = 0;

            foreach(Tile tile in PlayerHands[player])
            {
                total += tile.Left + tile.Right;
            }

            return total;
        }

        public int CalculateRoundScore()
        {
            int score = 0;

            IPlayer? winner = RoundWinner;

            foreach(IPlayer player in Players)
            {
                if(winner != null && !Equals(player, winner))
                {
                    score += PlayerTotalPips(player);
                }
            }

            return score;
        }

        public void StartNewRound()
        {
            if(Status != GameStatus.InProgress || RoundStatus != RoundStatus.Completed)
            {
                return;
            }

            // Clear the previous round's state
            PlayerHands.Clear();
            Board.PlayedTiles.Clear();

            // Reset round state
            RoundWinner = null;
            CanDraw = false;
            CanPass = false;

            // Initialize a new boneyard
            Boneyard.Tiles.Clear();
            Boneyard.Tiles.AddRange(_dominoFactory.CreateDoubleSixSet());
            ShuffleBoneyard();

            // Initialize each player's hand
            foreach(IPlayer player in Players)
            {
                PlayerHands[player] = new List<Tile>();
            }

            // Give tiles to each player
            int tilesPerPlayer = Players.Count == 4 ? 6 : 7;

            foreach(IPlayer player in Players)
            {
                for(int i = 0; i < tilesPerPlayer; i++)
                {
                    Tile tile = Boneyard.Tiles[0];
                    PlayerHands[player].Add(tile);
                    Boneyard.Tiles.RemoveAt(0);
                }
            }

            // Determine the first player
            IPlayer firstPlayer = DetermineFirstPlayer();
            _currentPlayerIndex = Players.IndexOf(firstPlayer);

            // Update round state
            CurrentRound++;
            RoundStatus = RoundStatus.InProgress;
        }

        public IPlayer? DetermineWinner()
        {
            // Check whether a player had emptied their hand
            foreach(IPlayer player in Players)
            {
                if(PlayerHands.ContainsKey(player) && PlayerHands[player].Count == 0)
                {
                    IPlayer? roundWinner = player;

                    return roundWinner;
                }
            }

            // If the boneyard still has tile, the round cannot end due to blocked game
            if (!Boneyard.IsEmpty)
            {
                IPlayer? noRoundWinner = null;

                return noRoundWinner;
            }

            // Check whether any player can still play a tile
            foreach(IPlayer player in Players)
            {
                if (HasValidMoves(player))
                {
                    IPlayer? noRoundWinner = null;

                    return noRoundWinner;
                }
            }

            // If the game is blocked, calculate each player's comparison score
            Dictionary<IPlayer, int> comparisonScores = new Dictionary<IPlayer, int>();

            foreach(IPlayer player in Players)
            {
                int playerPips = PlayerTotalPips(player);
                int comparisonScore = 0;

                foreach(IPlayer opponent in Players)
                {
                    if(!Equals(player, opponent))
                    {
                        comparisonScore += PlayerTotalPips(opponent) - playerPips;
                    }
                }

                comparisonScores[player] = comparisonScore;
            }

            // Find the highest comparison score
            int highestScore = int.MinValue;
            IPlayer? highestScoringPlayer = null;
            bool isTie = false;

            foreach(KeyValuePair<IPlayer, int> entry in comparisonScores)
            {
                if(entry.Value > highestScore)
                {
                    highestScore = entry.Value;
                    highestScoringPlayer = entry.Key;
                    isTie = false;
                }
                else if(entry.Value == highestScore)
                {
                    isTie = true;
                }
            }

            IPlayer? winner = isTie ? null : highestScoringPlayer;

            return winner;
        }

        public IPlayer? DetermineMatchWinner()
        {
            foreach(IPlayer player in Players)
            {
                if(PlayerScores.ContainsKey(player) && PlayerScores[player] >= TargetScore)
                {
                    IPlayer? foundMatchWinner = player;

                    return foundMatchWinner;
                }
            }

            IPlayer? noMatchWinner = null;

            return noMatchWinner;
        }
    }
}
