using System;
using System.Windows;
using System.Windows.Media;
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
        // preserve original email for admin users so UI bypass cannot change it
        private string originalEmail;
        // Field: WCF service client for database operations
        ChessServiceAdminClient ChessService;

        // Constructor: Initializes the window with the selected player's data
        public User_UpdateWindow(Player user)
        {
            // Initialize WPF components
            InitializeComponent();
            // Store the selected player
            player = user;
            // remember original email to prevent admins' email changes from being persisted
            originalEmail = player?.Email;
            // Create WCF service client
            ChessService = new ChessServiceAdminClient();
            // Set DataContext so form fields auto-populate with current player data
            this.DataContext = player;

            // If editing an admin user, make the email field read-only and visually distinct
            try
            {
                if (player != null && !string.IsNullOrEmpty(player.UserType) &&
                    player.UserType.IndexOf("Admin", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    EmailTextBox.IsReadOnly = true;
                    // Slightly lighter background to indicate non-editable state
                    EmailTextBox.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333"));
                }
            }
            catch
            {
                // Swallow any visual-setting errors to avoid crashing the dialog
            }
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
            // Safety: if this is an admin user, prevent persisting any email change
            if (player != null && !string.IsNullOrEmpty(player.UserType) &&
                player.UserType.IndexOf("Admin", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                player.Email = originalEmail;
            }

            // Send updated player data to the database via WCF service
            ChessService.UpdatePlayer(player);
            // Notify success and close the window
            MessageBox.Show("User updated successfully.", "Success");
            this.Close();
        }
    }
}