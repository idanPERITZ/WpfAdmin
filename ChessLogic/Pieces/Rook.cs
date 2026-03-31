using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace ChessLogic
{
    public class Rook : Piece
    {
        public override PieceType Type => PieceType.Rook;
        public override Player Color { get; }

        private static readonly Direction[] moveDirections = new Direction[]
        {
            Direction.North,
            Direction.East,
            Direction.South,
            Direction.West,
        };

        public Rook(Player color)
        {
            Color = color;
        }
        public override Piece Copy()
        {
            Rook copy = new Rook(Color);
            copy.HasMoved = HasMoved;
            return copy;
        }

        public override IEnumerable<Move> GetMoves(Position from, Board board)
        {
            return MovePositionInDirections(from, board, moveDirections).Select(to => new NormalMove(from, to));
        }
    }
}
