using System;
using System.Windows;
using WpfAdminPeritz.ServiceReferenceChess;
using ChessUI;

namespace WpfAdminPeritz
{
    // Window for updating an existing user's information
    // Pre-fills the form with the selected user's current data
    public partial class User_UpdateWindow : Window
    {
        // Field: The player being edited
        Player player;
        // Field: WCF service client for database operations
        ChessServiceAdminClient ChessService;

        // Constructor: Initializes the window with the selected player's data
        public User_UpdateWindow(Player user)
        {
            // Initialize WPF components
            InitializeComponent();
            // Store the selected player
            player = user;
            // Create WCF service client
            ChessService = new ChessServiceAdminClient();
            // Set DataContext so form fields auto-populate with current player data
            this.DataContext = player;
        }

        // Event handler: Closes the window without saving any changes
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // Event handler: Saves the updated user data to the database and closes the window
        // TODO: Add validation to check that username and email fields are filled and valid
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Send updated player data to the database via WCF service
            ChessService.UpdateUser(player);
            // Notify success and close the window
            MessageBox.Show("User updated successfully.", "Success");
            this.Close();
        }
    }
}