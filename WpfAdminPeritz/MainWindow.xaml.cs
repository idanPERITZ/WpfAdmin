using System;
using System.Windows;
using WpfAdminPeritz.ServiceReferenceChess;
using System.Windows.Input;
using ChessUI;

namespace WpfAdminPeritz
{
    // Main login window for the admin panel application
    // Authenticates the admin via Firebase before granting access
    public partial class MainWindow : Window
    {
        // Field: WCF service client for admin authentication operations
        ChessServiceAdminClient ChessService;

        // Constructor: Initializes the login window and service client
        public MainWindow()
        {
            // Initialize WPF components
            InitializeComponent();
            // Create new instance of admin service client
            ChessService = new ChessServiceAdminClient();
        }

        // Event handler: Handles login button click
        // Validates input, attempts sign-in, and opens the admin panel on success
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // Get email from text box
            string email = EmailTextBox.Text;
            // Get password from password box
            string password = PasswordBox.Password;

            // Validate that both fields are filled
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Please enter both email and password.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Attempt to sign in via Firebase authentication through the WCF service
                Player admin = ChessService.SignIn(email, password);

                // If sign-in was successful and user is an admin
                if (admin != null)
                {
                    // Notify the user of successful login
                    MessageBox.Show("Login successful!", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    // Open the admin main window and close the login window
                    AdminMainWindow main = new AdminMainWindow(admin);
                    main.Show();
                    this.Close();
                }

                else
                {
                    // Sign-in failed - wrong credentials or not an admin
                    MessageBox.Show("Invalid email or password, or you are not an admin.", "Login Failed",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            catch (Exception ex)
            {
                // Handle connection errors or other unexpected exceptions
                MessageBox.Show($"An error occurred: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Event handler: Placeholder for sign up functionality (not yet implemented)
        private void SignUp_Click(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show("Opening Sign Up screen...");
        }
    }
}