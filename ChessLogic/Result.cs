using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace ChessLogic
{
    // Class representing the result of a chess game
    public class Result
    {
        // Property: The player who won the game (None if draw)
        public Player Winner { get; }
        // Property: The reason why the game ended
        public EndReason Reason { get; }

        // Constructor: Creates a new game result with winner and reason
        public Result(Player winner, EndReason reason)
        {
            // Store the winning player
            Winner = winner;
            // Store the reason for game ending
            Reason = reason;
        }

        // Static method: Creates a result for a win by checkmate
        public static Result Win(Player winner)
        {
            // Return new result with the winner and checkmate reason
            return new Result(winner, EndReason.Checkmate);
        }

        // Static method: Creates a result for a draw
        public static Result Draw(EndReason reason)
        {
            // Return new result with no winner (Player.None) and draw reason
            return new Result(Player.None, reason);
        }
    }
}