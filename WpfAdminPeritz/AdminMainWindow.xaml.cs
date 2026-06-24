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
            // Notify the user service that this admin has joined so they appear in online lists
            try
            {
                var userToJoin = new WpfAdminPeritz.ServiceReferenceUserChess.Player
                {
                    Id = admin.Id,
                    UserName = admin.UserName,
                    Email = admin.Email,
                    DateJoined = admin.DateJoined,
                    GamesPlayed = admin.GamesPlayed,
                    Wins = admin.Wins,
                    Losses = admin.Losses,
                    Draws = admin.Draws,
                    GoogleId = admin.GoogleId,
                    UserType = admin.UserType
                };

                CallbackServiceManager.Instance.AddLocalOnlinePlayer(userToJoin);
            }
            catch { }
            // Load the Users page as the default view
            MainContent.Content = new Users_UserControl(admin);
        }

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                // Notify user service that this admin has left (convert to user type)
                var userToLeave = new WpfAdminPeritz.ServiceReferenceUserChess.Player
                {
                    Id = admin.Id,
                    UserName = admin.UserName,
                    Email = admin.Email,
                    DateJoined = admin.DateJoined,
                    GamesPlayed = admin.GamesPlayed,
                    Wins = admin.Wins,
                    Losses = admin.Losses,
                    Draws = admin.Draws,
                    GoogleId = admin.GoogleId,
                    UserType = admin.UserType
                };

                // Remove from local online player cache so UIs update immediately
                CallbackServiceManager.Instance.RemoveLocalOnlinePlayer(userToLeave);
                // Also attempt to inform the service if available
                try { CallbackServiceManager.Instance.UserService.PlayerLeave(userToLeave); } catch { }
            }
            catch { }

            base.OnClosed(e);
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