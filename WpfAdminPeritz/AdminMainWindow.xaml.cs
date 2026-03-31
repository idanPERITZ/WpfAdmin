using System;
using System.Windows;
using WpfAdminPeritz.ServiceReferenceChess;

namespace WpfAdminPeritz
{
    // Main window for the admin panel application
    // Contains a sidebar for navigation and a content area that loads different UserControls
    public partial class AdminMainWindow : Window
    {
        // Field: The currently logged-in admin player
        private Player admin;

        // Constructor: Initializes the window with the logged-in admin
        public AdminMainWindow(Player loggedInAdmin)
        {
            // Initialize WPF components
            InitializeComponent();
            // Store the logged-in admin
            admin = loggedInAdmin;
            // Load the Users page as the default view
            MainContent.Content = new Users_UserControl(admin);
        }

        // Event handler: Loads the Users management page when Users button is clicked
        private void BtnAdminList_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new Users_UserControl(admin);
        }

        // Event handler: Loads the Games management page when Games button is clicked
        private void BtnGames_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new Games_UserControl(admin);
        }

        // Event handler: Placeholder for Friends page (not yet implemented)
        private void BtnFriends_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new Friendship_UserControl(admin);
        }
    }
}