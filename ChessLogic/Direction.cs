using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace ChessLogic
{
    // Class representing a direction of movement on the chess board
    public class Direction
    {
        // Static field: Direction moving one row up (row decreases)
        public readonly static Direction North = new Direction(-1, 0);
        // Static field: Direction moving one row down (row increases)
        public readonly static Direction South = new Direction(1, 0);
        // Static field: Direction moving one column right (column increases)
        public readonly static Direction East = new Direction(0, 1);
        // Static field: Direction moving one column left (column decreases)
        public readonly static Direction West = new Direction(0, -1);
        // Static field: Diagonal direction up and right (combines North and East)
        public readonly static Direction NorthEast = North + East;
        // Static field: Diagonal direction up and left (combines North and West)
        public readonly static Direction NorthWest = North + West;
        // Static field: Diagonal direction down and right (combines South and East)
        public readonly static Direction SouthEast = South + East;
        // Static field: Diagonal direction down and left (combines South and West)
        public readonly static Direction SouthWest = South + West;

        // Property: The change in row when moving in this direction
        public int RowChange { get; }
        // Property: The change in column when moving in this direction
        public int ColumnChange { get; }

        // Constructor: Creates a new direction with specified row and column changes
        public Direction(int rowChange, int columnChange)
        {
            // Store the row change value
            RowChange = rowChange;
            // Store the column change value
            ColumnChange = columnChange;
        }

        // Operator overload: Adds two directions together to create a combined direction
        public static Direction operator +(Direction direction1, Direction direction2)
        {
            // Return new direction with sum of row changes and sum of column changes
            return new Direction(direction1.RowChange + direction2.RowChange, direction1.ColumnChange + direction2.ColumnChange);
        }

        // Operator overload: Multiplies a direction by a scalar to extend the distance
        public static Direction operator *(int scalar, Direction direction)
        {
            // Return new direction with row and column changes multiplied by scalar
            return new Direction(scalar * direction.RowChange, scalar * direction.ColumnChange);
        }
    }
}