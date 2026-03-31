using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WpfAdminPeritz.ServiceReferenceChess;
using ChessUI;

namespace WpfAdminPeritz
{
    // Window for adding a new registered user to the system
    // Allows the admin to enter a username and email for the new user
    public partial class User_AddUserWindow : Window
    {
        // Field: WCF service client for database operations
        ChessServiceAdminClient ChessService;
        // Field: The new player object bound to the form fields
        Player newUser;

        // Constructor: Initializes the window and creates a new player with default values
        public User_AddUserWindow()
        {
            // Initialize WPF components
            InitializeComponent();
            // Create WCF service client
            ChessService = new ChessServiceAdminClient();
            // Create new player with default statistics and set as DataContext for bindings
            this.DataContext = newUser = new Player()
            {
                // New users start with zero games played
                GamesPlayed = 0,
                // New users start with zero losses
                Losses = 0,
                // New users start with zero wins
                Wins = 0,
                // All admin-created users are registered users
                UserType = "Registered",
                // Set join date to today
                DateJoined = DateTime.Now
            };
        }

        // Event handler: Closes the window without saving
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // Event handler: Saves the new user to the database and closes the window
        // TODO: Add validation to check that username and email fields are filled and valid
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Insert the new user into the database
            ChessService.InsertUser(newUser);
            // Notify success and close the window
            MessageBox.Show("User added successfully.", "Success");
            this.Close();
        }
    }
}