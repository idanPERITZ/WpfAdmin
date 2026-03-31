using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace ChessLogic
{
    // Enum representing the players in a chess game
    public enum Player
    {
        // No player (used for empty squares)
        None,
        // White player (moves first)
        White,
        // Black player (moves second)
        Black,
    }

    // Static class providing extension methods for the Player enum
    public static class PlayerExtentions
    {
        // Extension method: Returns the opponent of the current player
        public static Player Opponent(this Player player)
        {
            // Use switch to determine opponent
            switch (player)
            {
                // If current player is White
                case Player.White:
                    // Return Black as opponent
                    return Player.Black;
                // If current player is Black
                case Player.Black:
                    // Return White as opponent
                    return Player.White;
                // If no player (default case)
                default:
                    // Return None
                    return Player.None;
            }
        }
    }
}