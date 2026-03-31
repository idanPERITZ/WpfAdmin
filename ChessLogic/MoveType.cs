using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace ChessLogic
{
    // Enum representing all possible types of moves in chess
    public enum MoveType
    {
        // Standard move where a piece moves to an empty square or captures
        Normal,
        // Castling move on the kingside (short castle)
        CastleKingside,
        // Castling move on the queenside (long castle)
        CastleQueenside,
        // Pawn moving two squares forward from starting position
        DoublePawn,
        // Special pawn capture move (en passant)
        EnPassant,
        // Pawn reaching the last rank and promoting to another piece
        PawnPromotion
    }
}