namespace ChessLogic
{
    // Class representing a pawn promotion move in chess
    public class PawnPromotion : Move
    {
        // Property: Always returns PawnPromotion as the move type
        public override MoveType Type => MoveType.PawnPromotion;

        // Property: The pawn's starting position
        public override Position FromPosition { get; }

        // Property: The pawn's ending position (last row)
        public override Position ToPosition { get; }

        // Field: The type of piece the pawn will be promoted to
        private readonly PieceType newType;

        // Constructor: Initializes a pawn promotion move
        public PawnPromotion(Position fromPosition, Position toPosition, PieceType newType)
        {
            // Store the starting position
            FromPosition = fromPosition;

            // Store the ending position
            ToPosition = toPosition;

            // Store the piece type to promote to
            this.newType = newType;
        }

        // Private method: Creates the new promoted piece based on the chosen type
        private Piece CreatePromotedPiece(Player color)
        {
            // Use a switch statement to create the appropriate piece
            switch (newType)
            {
                case PieceType.Queen:
                    // If Queen was chosen, create a Queen
                    return new Queen(color);

                case PieceType.Rook:
                    // If Rook was chosen, create a Rook
                    return new Rook(color);

                case PieceType.Bishop:
                    // If Bishop was chosen, create a Bishop
                    return new Bishop(color);

                default:
                    // Default (Knight or anything else), create a Knight
                    return new Knight(color);
            }
        }

        // Method: Performs the pawn promotion on the board
        public override bool Execute(Board board)
        {
            // Get the pawn that is being promoted
            Piece pawn = board[FromPosition];

            // Remove the pawn from its starting position
            board[FromPosition] = null;

            // Create the new promoted piece with the same color as the pawn
            Piece promotionPiece = CreatePromotedPiece(pawn.Color);

            // Mark the new piece as having moved (standard for all pieces)
            promotionPiece.HasMoved = true;

            // Place the promoted piece at the destination position
            board[ToPosition] = promotionPiece;

            // Return true (indicates the game state changed)
            return true;
        }
    }
}