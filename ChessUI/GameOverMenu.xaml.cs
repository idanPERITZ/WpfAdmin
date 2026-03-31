using ChessLogic;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ChessUI
{
    // UserControl displayed when a chess game ends
    // Shows the winner and reason, and gives the option to exit
    public partial class GameOverMenu : UserControl
    {
        // Event: Fired when the player selects an option (Exit)
        public event Action<Option> OnOptionSelected;

        // Constructor: Initializes the menu with the final game state
        public GameOverMenu(GameState gameState)
        {
            // Initialize WPF components
            InitializeComponent();
            // Get the game result
            Result result = gameState.Result;
            // Set winner text based on who won
            WinnerText.Text = GetWinnerText(result.Winner);
            // Set reason text based on how the game ended
            ReasonText.Text = GetReasonText(result.Reason, gameState.CurrentPlayer);
        }

        // Private method: Returns the winner display text based on the winning player
        private static string GetWinnerText(Player winner)
        {
            switch (winner)
            {
                // White player won
                case Player.White:
                    return "White Wins!";
                // Black player won
                case Player.Black:
                    return "Black Wins!";
                // No winner - it's a draw
                default:
                    return "It's A Draw";
            }
        }

        // Private method: Converts a Player enum value to a display string
        private static string PlayerString(Player player)
        {
            switch (player)
            {
                // Return "White" for white player
                case Player.White:
                    return "White";
                // Return "Black" for black player
                case Player.Black:
                    return "Black";
                // Return "Draw" for no player
                default:
                    return "Draw";
            }
        }

        // Private method: Returns the end reason display text
        // currentPlayer is the player who has no moves (used for checkmate message)
        private static string GetReasonText(EndReason reason, Player currentPlayer)
        {
            switch (reason)
            {
                // currentPlayer is the player who has no moves (lost)
                // so the winner is the opposite player
                case EndReason.Checkmate:
                    return $"{PlayerString(currentPlayer == Player.White ? Player.Black : Player.White)} Won By Checkmate";
                // Game ended by stalemate
                case EndReason.Stalemate:
                    return "Stalemate";
                // Game ended by fifty move rule
                case EndReason.FiftyMoveRule:
                    return "Fifty-Move Rule";
                // Game ended due to insufficient material
                case EndReason.InsufficientMaterial:
                    return "Insufficient Material";
                // Game ended by threefold repetition
                case EndReason.ThreefoldRepetition:
                    return "Threefold Repetition";
                default:
                    return "";
            }
        }

        // Event handler: Fires OnOptionSelected with Exit when exit button is clicked
        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            OnOptionSelected?.Invoke(Option.Exit);
        }
    }
}