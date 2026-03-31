using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace ChessLogic
{
    // Enum representing all chess piece types
    public enum PieceType
    {
        // Pawn piece
        Pawn,
        // Knight piece (moves in L-shape)
        Knight,
        // Bishop piece (moves diagonally)
        Bishop,
        // Rook piece (moves horizontally/vertically)
        Rook,
        // Queen piece (moves in all directions)
        Queen,
        // King piece (most important, game ends if checkmated. Moves one square in any direction.)
        King
    }
}