using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Linq;
using System.Collections.Generic;
using ChessLogic;
using System;


namespace ChessUI
{
    // Static class for loading and managing chess piece images
    public static class Images
    {
        // Static field: Dictionary mapping white piece types to their images
        private static readonly Dictionary<PieceType, ImageSource> whitePieceImages = new Dictionary<PieceType, ImageSource>()
        {
            // White pawn image
            { PieceType.Pawn, LoadImage("Assets/White Pawn.png") },
            // White rook image
            { PieceType.Rook, LoadImage("Assets/White Rook.png") },
            // White knight image
            { PieceType.Knight, LoadImage("Assets/White Knight.png") },
            // White bishop image
            { PieceType.Bishop, LoadImage("Assets/White Bishop.png") },
            // White queen image
            { PieceType.Queen, LoadImage("Assets/White Queen.png") },
            // White king image
            { PieceType.King, LoadImage("Assets/White King.png") }
        };

        // Static field: Dictionary mapping black piece types to their images
        private static readonly Dictionary<PieceType, ImageSource> blackPieceImages = new Dictionary<PieceType, ImageSource>()
        {
            // Black pawn image
            { PieceType.Pawn, LoadImage("Assets/Black Pawn.png") },
            // Black rook image
            { PieceType.Rook, LoadImage("Assets/Black Rook.png") },
            // Black knight image
            { PieceType.Knight, LoadImage("Assets/Black Knight.png") },
            // Black bishop image
            { PieceType.Bishop, LoadImage("Assets/Black Bishop.png") },
            // Black queen image
            { PieceType.Queen, LoadImage("Assets/Black Queen.png") },
            // Black king image
            { PieceType.King, LoadImage("Assets/Black King.png") }
        };

        // Private static method: Loads an image from a file path
        private static ImageSource LoadImage(string filePath)
        {
            // Create and return a BitmapImage from the relative file path
            return new BitmapImage(new Uri(filePath, UriKind.Relative));
        }

        // Public static method: Gets the image for a piece by color and type
        public static ImageSource GetImage(Player color, PieceType type)
        {
            // Use switch statement to select correct dictionary
            switch (color)
            {
                case Player.White:
                    // If white, get from white piece images
                    return whitePieceImages[type];
                case Player.Black:
                    // If black, get from black piece images
                    return blackPieceImages[type];
                default:
                    // If no player (empty square), return null
                    return null;
            }
        }

        // Public static method: Gets the image for a piece object
        public static ImageSource GetImage(Piece piece)
        {
            // If piece is null (empty square)
            if (piece == null)
                // Return null (no image)
                return null;

            // Get image using piece's color and type
            return GetImage(piece.Color, piece.Type);
        }
    }
}