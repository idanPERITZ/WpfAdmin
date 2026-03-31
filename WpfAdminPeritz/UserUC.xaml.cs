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
using System.Windows.Navigation;
using System.Windows.Shapes;
using WpfAdminPeritz.ServiceReferenceChess;
using ChessUI;

namespace WpfAdminPeritz
{
    // UserControl representing a single user card in the users list
    // Displays the username, email, and a View button to show user details
    public partial class UserUC : UserControl
    {
        // Field: Reference to the parent Users_UserControl for callback on View click
        Users_UserControl adminListUsers;
        // Field: The player this card represents
        Player user;

        // Constructor: Initializes the card with the parent control and player data
        public UserUC(Users_UserControl adminList, Player player)
        {
            // Initialize WPF components
            InitializeComponent();
            // Store reference to parent control
            this.adminListUsers = adminList;
            // Store the player data
            this.user = player;
            // Set DataContext for XAML bindings (UserName, Email)
            this.DataContext = player;
        }

        // Event handler: Notifies the parent control when View button is clicked
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // Tell the parent to display this user's details
            adminListUsers.Set(user);
        }

        // Method: Returns the player this card represents
        // Used by Users_UserControl to find the selected item in the list
        public Player GetPlayer()
        {
            return user;
        }
    }
}