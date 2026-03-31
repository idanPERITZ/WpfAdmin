using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace ChessLogic
{
    // Class for counting chess pieces by type and color on the board
    public class Counting
    {
        // Field: Dictionary storing count of each piece type for white player
        private readonly Dictionary<PieceType, int> whiteCount = new Dictionary<PieceType, int>();
        // Field: Dictionary storing count of each piece type for black player
        private readonly Dictionary<PieceType, int> blackCount = new Dictionary<PieceType, int>();

        // Property: Total number of all pieces on the board
        public int TotalCount { get; private set; }

        // Constructor: Initializes all piece type counters to zero
        public Counting()
        {
            // Loop through all piece types in the PieceType enum
            foreach (PieceType type in Enum.GetValues(typeof(PieceType)))
            {
                // Initialize white's count for this piece type to 0
                whiteCount[type] = 0;
                // Initialize black's count for this piece type to 0
                blackCount[type] = 0;
            }
        }

        // Method: Increments the count for a specific piece type and color
        public void Increment(Player color, PieceType type)
        {
            // If the piece belongs to white player
            if (color == Player.White)
            {
                // Increase white's count for this piece type
                whiteCount[type]++;
            }
            // If the piece belongs to black player
            else if (color == Player.Black)
            {
                // Increase black's count for this piece type
                blackCount[type]++;
            }

            // Increment the total count of all pieces
            TotalCount++;
        }

        // Method: Returns the count of a specific piece type for white
        public int White(PieceType type)
        {
            // Return white's count for the requested piece type
            return whiteCount[type];
        }

        // Method: Returns the count of a specific piece type for black
        public int Black(PieceType type)
        {
            // Return black's count for the requested piece type
            return blackCount[type];
        }
    }
}