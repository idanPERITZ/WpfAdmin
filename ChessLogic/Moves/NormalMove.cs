using System;
using System.Collections.Generic;
using System.Text;
namespace ChessLogic
{
    // Class representing a normal/regular chess move
    public class NormalMove : Move
    {
        // Property: Always returns Normal as the move type
        public override MoveType Type => MoveType.Normal;

        // Property: The piece's starting position
        public override Position FromPosition { get; }

        // Property: The piece's ending position
        public override Position ToPosition { get; }

        // Constructor: Initializes a normal move
        public NormalMove(Position from, Position to)
        {
            // Store the starting position
            FromPosition = from;

            // Store the ending position
            ToPosition = to;
        }

        // Method: Performs the normal move on the board
        public override bool Execute(Board board)
        {
            // Get the piece that is moving
            Piece piece = board[FromPosition];

            // If there is no piece at the source, nothing to do.
            if (piece == null)
                return false;

            // Check if there's a piece at the destination (capture)
            bool capture = !board.IsEmpty(ToPosition);

            // Place the piece at the destination position
            board[ToPosition] = piece;

            // Remove the piece from the starting position
            board[FromPosition] = null;

            // Mark that this piece has moved (important for castling/pawn rules)
            piece.HasMoved = true;

            // Return true if a piece was captured OR if a pawn moved
            // (this resets the 50-move draw counter)
            return capture || piece.Type == PieceType.Pawn;
        }
    }
}