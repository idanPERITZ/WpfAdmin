using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace ChessLogic
{
    // Class representing the complete state of a chess game
    public class GameState
    {
        // Property: The chess board with all pieces
        public Board Board { get; }
        // Property: The player whose turn it is to move
        public Player CurrentPlayer { get; private set; }
        // Property: The result of the game (null if game is ongoing)
        public Result Result { get; private set; } = null;
        // Field: Counter for moves without capture or pawn movement (for fifty-move rule)
        private int noCaptureOrPawnMoves = 0;
        // Field: String representation of the current board state
        private string stateString;
        // Field: Dictionary tracking how many times each board state has occurred
        private readonly Dictionary<string, int> stateHistory = new Dictionary<string, int>();

        // Constructor: Initializes a new game state with a player and board
        public GameState(Player player, Board board)
        {
            // Set the starting player
            CurrentPlayer = player;
            // Set the board
            Board = board;
            // Create string representation of initial state
            stateString = new StateString(CurrentPlayer, board).ToString();
            // Record this state as occurring once
            stateHistory[stateString] = 1;
        }

        // Method: Returns all legal moves for the piece at a given position
        public IEnumerable<Move> LegalMovesForPiece(Position position)
        {
            // If position is empty or piece doesn't belong to current player
            if (Board.IsEmpty(position) || Board[position].Color != CurrentPlayer)
            {
                // Return empty collection (no legal moves)
                return Enumerable.Empty<Move>();
            }

            // Get the piece at this position
            Piece piece = Board[position];
            // Get all possible moves for this piece
            IEnumerable<Move> possibleMoves = piece.GetMoves(position, Board);
            // Filter to only moves that are legal (don't leave king in check)
            return possibleMoves.Where(move => move.IsLegal(Board));
        }

        // Method: Executes a move and updates the game state
        public void MakeMove(Move move)
        {
            // Clear any en passant opportunity from previous turn
            Board.SetPawnSkipPosition(CurrentPlayer, null);
            // Execute the move and check if it was a capture or pawn move
            bool captureOrPawn = move.Execute(Board);

            // If move was a capture or pawn move
            if (captureOrPawn)
            {
                // Reset the fifty-move rule counter
                noCaptureOrPawnMoves = 0;
                // Clear state history (threefold repetition no longer possible)
                stateHistory.Clear();
            }
            // Otherwise (normal piece move)
            else
            {
                // Increment the fifty-move rule counter
                noCaptureOrPawnMoves++;
            }

            // Switch to the opponent's turn
            CurrentPlayer = CurrentPlayer.Opponent();
            // Update the state string for the new board position
            UpdateStateString();
            // Check if the game has ended
            CheckForGameOver();
        }

        // Method: Returns all legal moves available to a specific player
        public IEnumerable<Move> AllLegalMovesFor(Player player)
        {
            // Get all piece positions for the player and their possible moves
            IEnumerable<Move> movePosibility = Board.PiecePositionsFor(player).SelectMany(pos =>
            {
                // Get the piece at this position
                Piece piece = Board[pos];
                // Return all moves this piece can make
                return piece.GetMoves(pos, Board);
            });

            // Filter to only legal moves (don't leave king in check)
            return movePosibility.Where(move => move.IsLegal(Board));
        }

        // Private method: Checks if the game has ended and sets the result
        private void CheckForGameOver()
        {
            // If current player has no legal moves
            if (!AllLegalMovesFor(CurrentPlayer).Any())
            {
                // If king is in check, it's checkmate
                if (Board.IsInCheck(CurrentPlayer))
                {
                    // Opponent wins
                    Result = Result.Win(CurrentPlayer.Opponent());
                }
                // If king is not in check, it's stalemate
                else
                {
                    // Game is a draw
                    Result = Result.Draw(EndReason.Stalemate);
                }
            }
            // Check if there's insufficient material to checkmate
            else if (Board.InsufficialMatterial())
            {
                // Game is a draw
                Result = Result.Draw(EndReason.InsufficientMaterial);
            }
            // Check if fifty-move rule is triggered
            else if (FiftyMovesRule())
            {
                // Game is a draw
                Result = Result.Draw(EndReason.FiftyMoveRule);
            }
            // Check if same position occurred three times
            else if (ThreefoldRepetition())
            {
                // Game is a draw
                Result = Result.Draw(EndReason.ThreefoldRepetition);
            }
        }

        // Method: Checks if the game is over
        public bool IsGameOver()
        {
            // Game is over if Result is not null
            return Result != null;
        }

        // Private method: Checks if fifty-move rule should trigger
        private bool FiftyMovesRule()
        {
            // Calculate full moves (each player's move counts as half)
            int fullMoves = noCaptureOrPawnMoves / 2;
            // Rule triggers after exactly 50 full moves
            return fullMoves == 50;
        }

        // Private method: Updates the state string and history after a move
        private void UpdateStateString()
        {
            // Create new state string for current position
            stateString = new StateString(CurrentPlayer, Board).ToString();

            // If this state hasn't been seen before
            if (!stateHistory.ContainsKey(stateString))
            {
                // Record it as occurring once
                stateHistory[stateString] = 1;
            }
            // If this state has been seen before
            else
            {
                // Increment the occurrence counter
                stateHistory[stateString]++;
            }
        }

        // Private method: Checks if current position occurred three times
        private bool ThreefoldRepetition()
        {
            // Return true if current state has occurred exactly 3 times
            return stateHistory[stateString] == 3;
        }
    }
}