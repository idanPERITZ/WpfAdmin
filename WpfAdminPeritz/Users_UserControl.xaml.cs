using System.Windows;
using System.Windows.Controls;
using WpfAdminPeritz.ServiceReferenceChess;
using ChessUI;

namespace WpfAdminPeritz
{
    // UserControl for managing registered players in the admin panel
    // Displays a list of all users and allows adding, updating, and deleting them
    public partial class Users_UserControl : UserControl
    {
        // Field: WCF service client for database operations
        private ChessServiceAdminClient ChessService;
        // Field: Full list of players loaded from the database
        private PlayerList players;
        // Field: The currently selected player in the list
        private Player selectedPlayer;
        // Field: The logged-in admin player
        private Player admin;

        // Constructor: Initializes the UserControl and loads all users
        public Users_UserControl(Player loggedInAdmin)
        {
            // Initialize WPF components
            InitializeComponent();
            // Store the logged-in admin
            admin = loggedInAdmin;
            // Create WCF service client
            ChessService = new ChessServiceAdminClient();
            // Load all users into the list
            LoadUsers();
        }

        // Private method: Loads all users from the database and displays them as UserUC cards
        private void LoadUsers()
        {
            // Clear existing items
            ListBoxUsers.Items.Clear();
            // Get all users from database
            players = ChessService.GetAllUsers();
            // Create a UserUC card for each player and add to the list
            foreach (Player player in players)
            {
                UserUC userUC = new UserUC(this, player);
                ListBoxUsers.Items.Add(userUC);
            }
        }

        // Event handler: Updates the details panel when a user is selected in the list
        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListBoxUsers.SelectedItem is UserUC selected)
            {
                selectedPlayer = selected.GetPlayer();
                StackPanelUserView.DataContext = selectedPlayer;
            }
        }

        // Event handler: Deletes the selected player from the database
        private void ButtonDelete_Click(object sender, RoutedEventArgs e)
        {
            // Do nothing if no player is selected
            if (selectedPlayer == null)
                return;

            // Prevent deletion of admin users
            if (selectedPlayer.UserType == "Admin")
            {
                MessageBox.Show("Cannot delete an admin user.");
                return;
            }

            // Ask for confirmation before deleting
            MessageBoxResult result = MessageBox.Show(
                $"Are you sure you want to delete '{selectedPlayer.UserName}'?",
                "Confirm",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                // Delete the user from the database
                ChessService.DeleteUser(selectedPlayer);
                // Refresh the users list
                LoadUsers();
            }
        }

        // Event handler: Opens the update window for the selected player
        private void ButtonUpdate_Click(object sender, RoutedEventArgs e)
        {
            // Do nothing if no player is selected
            if (selectedPlayer == null)
                return;

            // Open the update window as a dialog and wait for it to close
            User_UpdateWindow window = new User_UpdateWindow(selectedPlayer);
            window.ShowDialog();
            // Refresh the users list to show updated data
            LoadUsers();
        }

        // Event handler: Opens the add user window to create a new player
        private void ButtonNew_Click(object sender, RoutedEventArgs e)
        {
            // Open the add user window as a dialog and wait for it to close
            User_AddUserWindow window = new User_AddUserWindow();
            window.ShowDialog();
            // Refresh the users list to show the new player
            LoadUsers();
        }

        // Public method: Sets the selected player and displays their details
        // Called by UserUC when the View button is clicked
        public void Set(Player player)
        {
            this.selectedPlayer = player;
            StackPanelUserView.DataContext = selectedPlayer;

            // Find and select the matching UserUC in the list
            foreach (object item in ListBoxUsers.Items)
            {
                UserUC userUC = item as UserUC;
                if (userUC != null && userUC.GetPlayer().Id == player.Id)
                {
                    ListBoxUsers.SelectedItem = item;
                    break;
                }
            }
        }
    }
}