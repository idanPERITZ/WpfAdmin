using System;
using System.Collections.Generic;
using System.Text;
namespace ChessLogic
{
    // Abstract base class for all chess moves
    public abstract class Move
    {
        // Abstract property: The type of move (must be implemented by child classes)
        public abstract MoveType Type { get; }

        // Abstract property: The starting position of the piece
        public abstract Position FromPosition { get; }

        // Abstract property: The ending position of the piece
        public abstract Position ToPosition { get; }

        // Abstract method: Executes the move on the board (must be implemented by child classes)
        public abstract bool Execute(Board board);

        // Virtual method: Checks if the move is legal (can be overridden by child classes)
        public virtual bool IsLegal(Board board)
        {
            // Get the color of the player making the move
            Player player = board[FromPosition].Color;

            // Create a copy of the board to test the move
            Board boardCopy = board.Copy();

            // Execute the move on the copy board
            Execute(boardCopy);

            // Check if the player's king is in check after the move
            // Return true only if the king is NOT in check (move is legal)
            return !boardCopy.IsInCheck(player);
        }
    }
}