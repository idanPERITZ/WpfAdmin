using System;
using System.Collections.Generic;
using System.Text;
namespace ChessLogic
{
    // Class representing the castling move in chess
    public class Castle : Move
    {
        // Property: The type of move (CastleKingside or CastleQueenside)
        public override MoveType Type { get; }

        // Property: The king's starting position
        public override Position FromPosition { get; }

        // Property: The king's ending position after castling
        public override Position ToPosition { get; }

        // Field: Direction the king moves (East for kingside, West for queenside)
        private readonly Direction kingMoveDirection;

        // Field: The rook's starting position
        private readonly Position rookFromPosition;

        // Field: The rook's ending position after castling
        private readonly Position rookToPosition;

        // Constructor: Initializes a castling move
        public Castle(MoveType type, Position kingPosition)
        {
            // Store the type of castling
            Type = type;

            // Store the king's current position
            FromPosition = kingPosition;

            // If castling kingside (short castle, king moves right)
            if (type == MoveType.CastleKingside)
            {
                // King moves east (right)
                kingMoveDirection = Direction.East;

                // King ends at column 6
                ToPosition = new Position(kingPosition.Row, 6);

                // Rook starts at column 7 (rightmost)
                rookFromPosition = new Position(kingPosition.Row, 7);

                // Rook ends at column 5 (next to king)
                rookToPosition = new Position(kingPosition.Row, 5);
            }
            // If castling queenside (long castle, king moves left)
            else if (type == MoveType.CastleQueenside)
            {
                // King moves west (left)
                kingMoveDirection = Direction.West;

                // King ends at column 2
                ToPosition = new Position(kingPosition.Row, 2);

                // Rook starts at column 0 (leftmost)
                rookFromPosition = new Position(kingPosition.Row, 0);

                // Rook ends at column 3 (next to king)
                rookToPosition = new Position(kingPosition.Row, 3);
            }
        }

        // Method: Performs the castling move on the board
        public override bool Execute(Board board)
        {
            // Move the king to its new position
            new NormalMove(FromPosition, ToPosition).Execute(board);

            // Move the rook to its new position
            new NormalMove(rookFromPosition, rookToPosition).Execute(board);

            // Return false (castling doesn't capture pieces)
            return false;
        }

        // Method: Checks if the castling move is legal
        public override bool IsLegal(Board board)
        {
            // Get the color of the player trying to castle
            Player player = board[FromPosition].Color;

            // If the king is currently in check, castling is illegal
            if (board.IsInCheck(player))
            {
                return false;
            }

            // Create a copy of the board to test the move
            Board copy = board.Copy();

            // Track the king's position on the copy board
            Position kingPositionInCopy = FromPosition;

            // Loop 2 times (king moves 2 squares when castling)
            for (int i = 1; i <= 2; i++)
            {
                // Move the king one square in the castling direction
                new NormalMove(kingPositionInCopy, kingPositionInCopy + kingMoveDirection).Execute(copy);

                // Update the king's position
                kingPositionInCopy += kingMoveDirection;

                // If the king is in check after this move, castling is illegal
                if (copy.IsInCheck(player))
                {
                    return false;
                }
            }

            // If all checks passed, castling is legal
            return true;
        }
    }
}