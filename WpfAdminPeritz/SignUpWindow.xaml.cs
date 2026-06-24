using System;
using System.Windows;
using WpfAdminPeritz.ServiceReferenceUserChess;

namespace WpfAdminPeritz
{
    // Window: Allows a new user to register via Firebase and be saved to the database
    public partial class SignUpWindow : Window
    {
        // Constructor: Initializes the sign-up window
        public SignUpWindow()
        {
            InitializeComponent();
        }

        // Event handler: Registers a new user in Firebase and the database, then returns to Login
        private void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            // Read input fields
            string username = UsernameTextBox.Text;
            string email = EmailTextBox.Text;
            string password = PasswordBox.Password;
            string confirmPassword = ConfirmPasswordBox.Password;

            // Validate: all fields must be filled
            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(confirmPassword))
            {
                MessageBox.Show("Please fill in all fields.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validate: passwords must match
            if (password != confirmPassword)
            {
                MessageBox.Show("Passwords do not match.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validate: password must be at least 6 characters (Firebase minimum)
            if (password.Length < 8)
            {
                MessageBox.Show("Password must be at least 8 characters.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Build the new player object to send to the service
                Player newPlayer = new Player()
                {
                    // Set the username entered by the user
                    UserName = username,
                    // Set the email entered by the user
                    Email = email,
                    // New users are of type Registered (not Admin)
                    UserType = "Registered",
                    // New users start with zero games played
                    GamesPlayed = 0,
                    // New users start with zero wins
                    Wins = 0,
                    // New users start with zero losses
                    Losses = 0,
                    // Set the join date to today
                    Draws = 0,
                    DateJoined = DateTime.Now
                };

                // InsertPlayer handles Firebase registration and DB insert in one call (server-side)
                CallbackServiceManager.Instance.UserService.InsertPlayer(newPlayer, password);

                // Notify success and navigate back to Login
                MessageBox.Show("Registration successful! You can now log in.", "Welcome",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                GoToLogin();
            }

            catch (Exception ex)
            {
                MessageBox.Show("An error occurred: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Event handler: Navigates back to the Login window without registering
        private void BackToLogin_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            GoToLogin();
        }

        // Method: Opens the Login window and closes this window
        private void GoToLogin()
        {
            MainWindow loginWindow = new MainWindow();
            loginWindow.Show();
            this.Close();
        }
    }
}