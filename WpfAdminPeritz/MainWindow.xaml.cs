using System;
using System.Windows;
using System.Windows.Input;
using WpfAdminPeritz.ServiceReferenceChess;
using WpfAdminPeritz.ServiceReferenceUserChess;

//using WpfAdminPeritz.ServiceReferenceUser;

// Alias for Admin Player type
using AdminPlayer = WpfAdminPeritz.ServiceReferenceChess.Player;
// Alias for User Player type
using UserPlayer = WpfAdminPeritz.ServiceReferenceUserChess.Player;

namespace WpfAdminPeritz
{
    public partial class MainWindow : Window
    {
        // Field: WCF service client for admin authentication
        ChessServiceAdminClient ChessService;

        public MainWindow()
        {
            InitializeComponent();

            ChessService = new ChessServiceAdminClient();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string email = EmailTextBox.Text;
            string password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Please enter both email and password.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // First: try admin login via Admin service
                // SignIn in Admin service returns null if user is not Admin
                AdminPlayer adminPlayer = ChessService.SignIn(email, password);
                if (adminPlayer != null)
                {
                    AdminMainWindow main = new AdminMainWindow(adminPlayer);
                    main.Show();
                    this.Close();
                    return;
                }

                // Second: try regular user login via User service
                // Login in User service works for all Registered users
                UserPlayer regularPlayer = CallbackServiceManager.Instance.UserService.Login(email, password);
                if (regularPlayer != null)
                {
                    PlayerMainWindow playerMain = new PlayerMainWindow(regularPlayer);
                    playerMain.Show();
                    this.Close();
                    return;
                }

                // Neither admin nor regular user matched
                MessageBox.Show("Invalid email or password.", "Login Failed",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SignUp_Click(object sender, MouseButtonEventArgs e)
        {
            SignUpWindow signUpWindow = new SignUpWindow();
            signUpWindow.Show();
            this.Close();
        }
    }
}