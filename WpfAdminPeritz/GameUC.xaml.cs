using WpfAdminPeritz.ServiceReferenceChess;
using System.Windows;
using System.Windows.Controls;

namespace WpfAdminPeritz
{
    // UserControl representing a single game row in the games list panel
    // Displays the game's ID and date, and provides a View button to load its full details
    public partial class GameUC : UserControl
    {
        // Field: Reference to the parent Games_UserControl used to trigger game detail display
        private Games_UserControl adminListGames;
        // Field: The game this row represents
        private Game game;

        // Constructor: Initializes the row with the parent control reference and game data
        public GameUC(Games_UserControl adminList, Game g)
        {
            // Initialize WPF components
            InitializeComponent();
            // Store the parent control for callback use
            this.adminListGames = adminList;
            // Store the game this row represents
            this.game = g;
            // Set DataContext to the game object so XAML bindings (GameID, GameDate) resolve automatically
            this.DataContext = g;
        }

        // Event handler: Tells the parent control to display the full details for this row's game
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // Notify the parent control that this game has been selected for viewing
            adminListGames.SetSelectedGame(game);
        }

        // Public method: Returns the game object associated with this row
        // Used by Games_UserControl to identify which game corresponds to a selected list item
        public Game GetGame()
        {
            return game;
        }
    }
}