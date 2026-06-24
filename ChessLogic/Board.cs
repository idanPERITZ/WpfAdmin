using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace ChessLogic
{
    // Class representing the chess board
    public class Board
    {
        // Field: 2D array holding all pieces on the 8x8 board
        private readonly Piece[,] pieces = new Piece[8, 8];

        // Field: Dictionary tracking positions where pawns skipped a square (for en passant)
        private readonly Dictionary<Player, Position> pawnSkipPositions = new Dictionary<Player, Position>
        {
            // Initialize with null for both players
            {Player.White, null },
            {Player.Black, null },
        };

        // Indexer: Access pieces by row and column numbers
        public Piece this[int row, int col]
        {
            // Get the piece at the specified position
            get { return pieces[row, col]; }
            // Set a piece at the specified position
            set { pieces[row, col] = value; }
        }

        // Indexer: Access pieces by Position object
        public Piece this[Position position]
        {
            // Get the piece at the position
            get { return this[position.Row, position.Column]; }
            // Set a piece at the position
            set { this[position.Row, position.Column] = value; }
        }

        // Method: Get the position where a player's pawn skipped a square
        public Position GetPawnSkipPosition(Player player)
        {
            // Return the pawn skip position for the player if present.
            if (pawnSkipPositions.TryGetValue(player, out Position pos))
                return pos;

            return null;
        }

        // Method: Set the position where a player's pawn skipped a square
        public void SetPawnSkipPosition(Player player, Position position)
        {
            // If the player key exists, update it. If an invalid player is
            // provided (e.g. Player.None) safely ignore the request.
            if (pawnSkipPositions.ContainsKey(player))
                pawnSkipPositions[player] = position;
        }

        // Static method: Create a new board with starting chess position
        public static Board Initial()
        {
            // Create a new empty board
            Board board = new Board();
            // Add all pieces to their starting positions
            board.AddStartingPieces();
            // Return the initialized board
            return board;
        }

        // Method: Check if queenside castling is possible for a player
        public bool CastleRightQueenSide(Player player)
        {
            switch (player)
            {
                case Player.White:
                    // White: king at (7,4), rook at (7,0)
                    return IsUnmovedKingAndRook(new Position(7, 4), new Position(7, 0));
                case Player.Black:
                    // Black: king at (0,4), rook at (0,0)
                    return IsUnmovedKingAndRook(new Position(0, 4), new Position(0, 0));
                default:
                    return false;
            }
        }

        // Private method: Place all pieces in their starting positions
        private void AddStartingPieces()
        {
            // Black's back row (row 0)
            this[0, 0] = new Rook(Player.Black);
            this[0, 1] = new Knight(Player.Black);
            this[0, 2] = new Bishop(Player.Black);
            this[0, 3] = new Queen(Player.Black);
            this[0, 4] = new King(Player.Black);
            this[0, 5] = new Bishop(Player.Black);
            this[0, 6] = new Knight(Player.Black);
            this[0, 7] = new Rook(Player.Black);

            // White's back row (row 7)
            this[7, 0] = new Rook(Player.White);
            this[7, 1] = new Knight(Player.White);
            this[7, 2] = new Bishop(Player.White);
            this[7, 3] = new Queen(Player.White);
            this[7, 4] = new King(Player.White);
            this[7, 5] = new Bishop(Player.White);
            this[7, 6] = new Knight(Player.White);
            this[7, 7] = new Rook(Player.White);

            // Add pawns for both players
            for (int col = 0; col < 8; col++)
            {
                // Black pawns on row 1
                this[1, col] = new Pawn(Player.Black);
                // White pawns on row 6
                this[6, col] = new Pawn(Player.White);
            }
        }

        // Static method: Check if a position is inside the board boundaries
        public static bool IsInside(Position position)
        {
            // Position must be between 0-7 for both row and column
            return position.Row >= 0 && position.Row < 8 &&
                   position.Column >= 0 && position.Column < 8;
        }

        // Method: Check if a position is empty (no piece there)
        public bool IsEmpty(Position position)
        {
            return this[position] == null;
        }

        // Method: Get all positions that have pieces on them
        public IEnumerable<Position> PiecePositions()
        {
            // Loop through all rows
            for (int row = 0; row < 8; row++)
            {
                // Loop through all columns
                for (int col = 0; col < 8; col++)
                {
                    // Create position object
                    Position position = new Position(row, col);

                    // If there's a piece at this position
                    if (!IsEmpty(position))
                    {
                        // Return this position
                        yield return position;
                    }
                }
            }
        }

        // Method: Get all positions that have pieces of a specific player
        public IEnumerable<Position> PiecePositionsFor(Player player)
        {
            // Filter piece positions to only those matching the player's color
            return PiecePositions().Where(pos => this[pos].Color == player);
        }

        // Method: Check if a player's king is in check
        public bool IsInCheck(Player player)
        {
            // Check if any opponent piece can capture the king
            return PiecePositionsFor(player.Opponent()).Any(pos =>
            {
                // Get the opponent's piece
                Piece piece = this[pos];
                // Check if this piece can capture the king
                return piece.CanCaptureOpponentKing(pos, this);
            });
        }

        // Method: Create a deep copy of the board
        public Board Copy()
        {
            // Create a new empty board
            Board copy = new Board();

            // Loop through all piece positions
            foreach (Position pos in PiecePositions())
            {
                // Copy each piece to the new board
                copy[pos] = this[pos].Copy();
            }

            // Return the copied board
            return copy;
        }

        // Method: Count all pieces on the board by type and color
        public Counting CountPieces()
        {
            // Create a new counting object
            Counting counting = new Counting();
            // Loop through all pieces
            foreach (Position pos in PiecePositions())
            {
                // Get the piece
                Piece piece = this[pos];
                // Increment the count for this piece type and color
                counting.Increment(piece.Color, piece.Type);
            }

            // Return the counting results
            return counting;
        }

        // Method: Check if there's insufficient material for checkmate
        public bool InsufficialMatterial()
        {
            // Count all pieces
            Counting counting = CountPieces();
            // Check various insufficient material scenarios
            return IsKing_VS_King(counting) ||
                   IsKingKnight_VS_King(counting) ||
                   IsKingBishop_VS_King(counting) ||
                   IsKingBishop_VS_KingBishop(counting);
        }

        // Static method: Check if only kings remain (King vs King)
        private static bool IsKing_VS_King(Counting counting)
        {
            // Only 2 pieces total means only the two kings
            return counting.TotalCount == 2;
        }

        // Static method: Check if it's King+Bishop vs King
        private static bool IsKingBishop_VS_King(Counting counting)
        {
            // Total of 3 pieces
            return counting.TotalCount == 3 &&
                   // One side has a bishop (the other only has king)
                   (counting.White(PieceType.Bishop) == 1 || counting.Black(PieceType.Bishop) == 1);
        }

        // Static method: Check if it's King+Knight vs King
        private static bool IsKingKnight_VS_King(Counting counting)
        {
            // Total of 3 pieces
            return counting.TotalCount == 3 &&
                   // One side has a knight (the other only has king)
                   (counting.White(PieceType.Knight) == 1 || counting.Black(PieceType.Knight) == 1);
        }

        // Method: Check if it's King+Bishop vs King+Bishop with same color bishops
        private bool IsKingBishop_VS_KingBishop(Counting counting)
        {
            // Must be exactly 4 pieces total
            if (counting.TotalCount != 4) return false;
            // Each side must have exactly 1 bishop
            if (counting.White(PieceType.Bishop) != 1 || counting.Black(PieceType.Bishop) != 1) return false;

            // Find the white bishop's position
            Position whiteBishopPosition = FindPiece(Player.White, PieceType.Bishop);
            // Find the black bishop's position
            Position blackBishopPosition = FindPiece(Player.Black, PieceType.Bishop);

            // Check if both bishops are on same colored squares (can't checkmate)
            return whiteBishopPosition.SquareColor() == blackBishopPosition.SquareColor();
        }

        // Private method: Find the position of a specific piece type for a player
        private Position FindPiece(Player color, PieceType type)
        {
            // Return the first position that matches the color and type
            return PiecePositionsFor(color).First(pos => this[pos].Type == type);
        }

        // Private method: Check if king and rook haven't moved (for castling)
        private bool IsUnmovedKingAndRook(Position kingPosition, Position rookPosition)
        {
            // If either position is empty, return false
            if (IsEmpty(kingPosition) || IsEmpty(rookPosition))
            {
                return false;
            }

            // Get the pieces at both positions
            Piece king = this[kingPosition];
            Piece rook = this[rookPosition];

            // Check: correct piece types and neither has moved
            return king.Type == PieceType.King && rook.Type == PieceType.Rook && !rook.HasMoved && !king.HasMoved;
        }

        // Method: Check if kingside castling is possible for a player
        public bool CastleRightKingSide(Player player)
        {
            // Check based on player color
            switch (player)
            {
                case Player.White:
                    // White: king at (7,4), rook at (7,7)
                    return IsUnmovedKingAndRook(new Position(7, 4), new Position(7, 7));
                case Player.Black:
                    // Black: king at (0,4), rook at (0,7)
                    return IsUnmovedKingAndRook(new Position(0, 4), new Position(0, 7));
                default:
                    // Invalid player
                    return false;
            }
        }

        // Private method: Check if there's a pawn that can capture en passant
        private bool HasPawnInPosition(Player player, Position[] pawnPositions, Position skipPos)
        {
            // Loop through potential pawn positions
            foreach (Position pos in pawnPositions.Where(IsInside))
            {
                // Get the piece at this position
                Piece piece = this[pos];
                // Skip if empty, wrong color, or not a pawn
                if (piece == null || piece.Color != player || piece.Type != PieceType.Pawn)
                {
                    continue;
                }

                // Create en passant move
                EnPassant move = new EnPassant(pos, skipPos);
                // If the move is legal, return true
                if (move.IsLegal(this))
                {
                    return true;
                }
            }

            // No valid en passant found
            return false;
        }

        // Method: Check if a player can capture en passant
        public bool CanCaptureEnPassant(Player player)
        {
            // Get the position the opponent's pawn skipped
            Position skipPos = GetPawnSkipPosition(player.Opponent());

            // If no pawn skipped recently, return false
            if (skipPos == null)
            {
                return false;
            }

            // Determine positions where attacking pawns could be
            Position[] pawnPositions;
            switch (player)
            {
                case Player.White:
                    // White pawns attack diagonally upward (from their perspective)
                    pawnPositions = new Position[] { skipPos + Direction.SouthWest, skipPos + Direction.SouthEast };
                    break;
                case Player.Black:
                    // Black pawns attack diagonally downward (from their perspective)
                    pawnPositions = new Position[] { skipPos + Direction.NorthWest, skipPos + Direction.NorthEast };
                    break;
                default:
                    // Invalid player
                    pawnPositions = new Position[0];
                    break;
            }

            // Check if there's a pawn that can perform en passant
            return HasPawnInPosition(player, pawnPositions, skipPos);
        }
    }
}