using ChessLogic;
using System.Windows.Controls;
using System.Windows.Input;
using System;

namespace ChessUI
{
    // UserControl displayed when a pawn reaches the last rank and needs to be promoted
    // Shows four piece options (Queen, Rook, Bishop, Knight) for the player to choose from
    public partial class PromotionMenu : UserControl
    {
        // Event: Fired when the player clicks on a piece to promote to
        // Returns the selected PieceType to the caller
        public event Action<PieceType> PieceSelected;

        // Constructor: Initializes the menu with the correct piece images for the promoting player
        public PromotionMenu(Player player)
        {
            // Initialize WPF components
            InitializeComponent();

            // Set piece images based on the promoting player's color
            QueenImage.Source = Images.GetImage(player, PieceType.Queen);
            BishopImage.Source = Images.GetImage(player, PieceType.Bishop);
            RookImage.Source = Images.GetImage(player, PieceType.Rook);
            KnightImage.Source = Images.GetImage(player, PieceType.Knight);
        }

        // Event handler: Fires PieceSelected with Queen when queen image is clicked
        private void QueenImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            PieceSelected?.Invoke(PieceType.Queen);
        }

        // Event handler: Fires PieceSelected with Rook when rook image is clicked
        private void RookImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            PieceSelected?.Invoke(PieceType.Rook);
        }

        // Event handler: Fires PieceSelected with Bishop when bishop image is clicked
        private void BishopImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            PieceSelected?.Invoke(PieceType.Bishop);
        }

        // Event handler: Fires PieceSelected with Knight when knight image is clicked
        private void KnightImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            PieceSelected?.Invoke(PieceType.Knight);
        }
    }
}