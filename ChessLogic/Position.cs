using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace ChessLogic
{
    // Class representing a position on the chess board
    public class Position
    {
        // Property: The row index (0-7, where 0 is top)
        public int Row { get; }
        // Property: The column index (0-7, where 0 is left)
        public int Column { get; }

        // Constructor: Creates a new position with row and column
        public Position(int row, int column)
        {
            // Store the row value
            Row = row;
            // Store the column value
            Column = column;
        }

        // Method: Determines the color of the square at this position
        public Player SquareColor()
        {
            // If sum of row and column is even
            if ((Row + Column) % 2 == 0)
            {
                // Square is white (like a1, c1, e1, etc.)
                return Player.White;
            }
            // If sum is odd, square is black
            return Player.Black;
        }

        // Override: Generates hash code for position (used in collections)
        public override int GetHashCode()
        {
            // Combine row and column into a single hash code
            int hash = 17;
            hash = hash * 31 + Row;
            hash = hash * 31 + Column;
            return hash;
        }

        // Override: Checks if two positions are equal
        public override bool Equals(object obj)
        {
            // Check if object is a Position and has same row and column
            return obj is Position position &&
                   Row == position.Row &&
                   Column == position.Column;
        }


        // Operator overload: Checks if two positions are equal using ==
        public static bool operator ==(Position left, Position right)
        {
            // Use default equality comparer to check equality
            return EqualityComparer<Position>.Default.Equals(left, right);
        }

        // Operator overload: Checks if two positions are not equal using !=
        public static bool operator !=(Position left, Position right)
        {
            // Return opposite of equality check
            return !(left == right);
        }

        // Operator overload: Adds a direction to a position to get new position
        public static Position operator +(Position position, Direction direction)
        {
            // Return new position by adding direction's changes to current position
            return new Position(position.Row + direction.RowChange, position.Column + direction.ColumnChange);
        }
    }
}