using System;
using System.Collections.Generic;
using System.Text;
namespace ChessLogic
{
    // Class representing a double pawn move (pawn moving 2 squares forward)
    public class DoublePawn : Move
    {
        // Property: Always returns DoublePawn as the move type
        public override MoveType Type => MoveType.DoublePawn;

        // Property: The pawn's starting position
        public override Position FromPosition { get; }

        // Property: The pawn's ending position (2 squares forward)
        public override Position ToPosition { get; }

        // Field: The position the pawn skipped over (for en passant rule)
        private readonly Position skippedPosition;

        // Constructor: Initializes a double pawn move
        public DoublePawn(Position from, Position to)
        {
            // Store the starting position
            FromPosition = from;

            // Store the ending position
            ToPosition = to;

            // Calculate the middle square that was skipped
            // (average of start and end rows, same column)
            skippedPosition = new Position((from.Row + to.Row) / 2, from.Column);
        }

        // Method: Performs the double pawn move on the board
        public override bool Execute(Board board)
        {
            // Get the color of the player moving the pawn
            Player player = board[FromPosition].Color;

            // Record the skipped position (for en passant capture next turn)
            board.SetPawnSkipPosition(player, skippedPosition);

            // Move the pawn to its new position
            new NormalMove(FromPosition, ToPosition).Execute(board);

            // Return true (indicates the game state changed)
            return true;
        }
    }
}