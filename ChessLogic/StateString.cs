using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace ChessLogic
{
    // Class that creates a string representation of the game state (similar to PNG notation)
    public class StateString
    {
        // Field: StringBuilder for efficiently building the state string
        private readonly StringBuilder sb = new StringBuilder();

        // Constructor: Builds the complete state string from current game state
        public StateString(Player currentPlayer, Board board)
        {
            // Add piece positions on the board
            AddPiecePlacement(board);
            // Add separator space
            sb.Append(' ');
            // Add which player's turn it is
            AddCurrentPlayer(currentPlayer);
            // Add separator space
            sb.Append(' ');
            // Add castling availability for both players
            AddCastlingRights(board);
            // Add separator space
            sb.Append(' ');
            // Add en passant target square if available
            AddEnPassant(board, currentPlayer);
        }

        // Override: Returns the complete state string
        public override string ToString()
        {
            // Return the built string
            return sb.ToString();
        }

        // Static method: Converts a piece to its character representation
        private static char PieceChar(Piece piece)
        {
            // Determine character based on piece type
            switch (piece.Type)
            {
                case PieceType.Pawn:
                    return 'p';    // Pawn = 'p'
                case PieceType.Knight:
                    return 'n';    // Knight = 'n'
                case PieceType.Rook:
                    return 'r';    // Rook = 'r'
                case PieceType.Bishop:
                    return 'b';    // Bishop = 'b'
                case PieceType.Queen:
                    return 'q';    // Queen = 'q'
                case PieceType.King:
                    return 'k';    // King = 'k'
                default:
                    return ' ';    // Default = space
            }
        }

        // Private method: Adds data for a single row to the string
        private void AddRowData(Board board, int row)
        {
            // Counter for consecutive empty squares
            int empty = 0;

            // Loop through all columns in this row
            for (int c = 0; c < 8; c++)
            {
                // If square is empty
                if (board[row, c] == null)
                {
                    // Increment empty counter
                    empty++;
                    // Continue to next square
                    continue;
                }

                // If there were empty squares before this piece
                if (empty > 0)
                {
                    // Add the number of empty squares
                    sb.Append(empty);
                    // Reset counter
                    empty = 0;
                }

                // Add the piece character
                sb.Append(PieceChar(board[row, c]));
            }

            // If row ends with empty squares
            if (empty > 0)
            {
                // Add the count of trailing empty squares
                sb.Append(empty);
            }
        }

        // Private method: Adds all piece positions to the string
        private void AddPiecePlacement(Board board)
        {
            // Loop through all rows
            for (int r = 0; r < 8; r++)
            {
                // Add separator between rows (except before first row)
                if (r != 0)
                {
                    sb.Append('/');
                }
                // Add data for this row
                AddRowData(board, r);
            }
        }

        // Private method: Adds the current player to the string
        private void AddCurrentPlayer(Player currentPlayer)
        {
            // If white's turn
            if (currentPlayer == Player.White)
            {
                // Add 'w'
                sb.Append('w');
            }
            // If black's turn
            if (currentPlayer == Player.Black)
            {
                // Add 'b'
                sb.Append('b');
            }
        }

        // Private method: Adds castling rights for both players
        private void AddCastlingRights(Board board)
        {
            // Check if white can castle kingside
            bool castleWKS = board.CastleRightKingSide(Player.White);
            // Check if white can castle queenside
            bool castleWQS = board.CastleRightKingSide(Player.White);
            // Check if black can castle kingside
            bool castleBKS = board.CastleRightKingSide(Player.Black);
            // Check if black can castle queenside
            bool castleBQS = board.CastleRightKingSide(Player.Black);

            // If no castling is possible for either player
            if (!(castleWKS || castleWQS || castleBKS || castleBQS))
            {
                // Add '-' to indicate no castling rights
                sb.Append('-');
                return;
            }

            // If white can castle kingside
            if (castleWKS)
            {
                // Add 'K'
                sb.Append('K');
            }
            // If white can castle queenside
            if (castleWQS)
            {
                // Add 'Q'
                sb.Append('Q');
            }
            // If black can castle kingside
            if (castleBKS)
            {
                // Add 'k'
                sb.Append('k');
            }
            // If black can castle queenside
            if (castleBQS)
            {
                // Add 'q'
                sb.Append('q');
            }
        }

        // Private method: Adds en passant target square if available
        private void AddEnPassant(Board board, Player currentPlayer)
        {
            // If en passant is not possible
            if (!board.CanCaptureEnPassant(currentPlayer))
            {
                // Add '-' to indicate no en passant
                sb.Append('-');
                return;
            }

            // Get the position where opponent's pawn skipped
            Position pos = board.GetPawnSkipPosition(currentPlayer.Opponent());
            // Convert column number to file letter (0='a', 1='b', etc.)
            char file = (char)('a' + pos.Column);
            // Convert row number to rank number (chess notation: 8-row)
            int rank = 8 - pos.Row;
            // Add the file letter (a-h)
            sb.Append(file);
            // Add the rank number (1-8)
            sb.Append(rank);
        }
    }
}