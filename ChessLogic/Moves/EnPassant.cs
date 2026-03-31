namespace ChessLogic
{
    // Class representing an en passant capture move in chess
    public class EnPassant : Move
    {
        // Property: Always returns EnPassant as the move type
        public override MoveType Type => MoveType.EnPassant;

        // Property: The attacking pawn's starting position
        public override Position FromPosition { get; }

        // Property: The attacking pawn's ending position (diagonal move)
        public override Position ToPosition { get; }

        // Field: The position of the enemy pawn that will be captured
        private readonly Position capturePosition;

        // Constructor: Initializes an en passant move
        public EnPassant(Position from, Position to)
        {
            // Store the starting position
            FromPosition = from;

            // Store the ending position (diagonal square)
            ToPosition = to;

            // Calculate the captured pawn's position
            // (same row as attacker, same column as destination)
            capturePosition = new Position(from.Row, to.Column);
        }

        // Method: Performs the en passant capture on the board
        public override bool Execute(Board board)
        {
            // Move the attacking pawn diagonally to the empty square
            new NormalMove(FromPosition, ToPosition).Execute(board);

            // Remove the captured enemy pawn from the board
            board[capturePosition] = null;

            // Return true (indicates a capture occurred)
            return true;
        }
    }
}