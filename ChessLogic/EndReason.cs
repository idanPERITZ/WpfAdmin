using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace ChessLogic
{
    // Enum representing all possible reasons for a chess game to end
    public enum EndReason
    {
        // Game ends when a king is in check and has no legal moves to escape
        Checkmate,
        // Game ends in a draw when a player has no legal moves but is not in check
        Stalemate,
        // Game ends in a draw after 50 moves without a pawn move or capture
        FiftyMoveRule,
        // Game ends in a draw when neither player has enough pieces to checkmate
        InsufficientMaterial,
        // Game ends in a draw when the same position occurs three times
        ThreefoldRepetition
    }
}