using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using WpfAdminPeritz.ServiceReferenceChess;

namespace WpfAdminPeritz
{
    public partial class Users_UserControl : UserControl
    {
        // This control shows the list of users on the left and details on the right.
        // Event handlers below respond to buttons and list events.
        private ChessServiceAdminClient ChessService;
        private Player admin;
        private Player selectedPlayer;
        private DispatcherTimer onlineTimer;

        // Constructor: create control for the given logged-in admin user
        // It starts a timer to refresh user list and subscribes to online events.
        public Users_UserControl(Player loggedInAdmin)
        {
            InitializeComponent();

            admin = loggedInAdmin;

            ChessService = new ChessServiceAdminClient();

            LoadUsers();

            onlineTimer = new DispatcherTimer();

            onlineTimer.Interval = TimeSpan.FromSeconds(2);

            onlineTimer.Tick += OnlineTimer_Tick;

            onlineTimer.Start();

            // Fix 4: Subscribe to real-time player online/offline events
            CallbackServiceManager.Instance.OnPlayerJoined += OnOnlineStatusChanged;
            CallbackServiceManager.Instance.OnPlayerLeft += OnOnlineStatusChanged;

            Unloaded += Users_UserControl_Unloaded;
        }

        // Timer tick: refresh the users list periodically
        private void OnlineTimer_Tick(object sender, EventArgs e)
        {
            LoadUsers();
        }

        // Load all players from the server and rebuild the left list
        // Marks which players are currently online
        private void LoadUsers()
        {
            Player currentSelected = selectedPlayer;

            ListBoxUsers.Items.Clear();

            PlayerList players = ChessService.GetAllplayers();

            var onlinePlayers = CallbackServiceManager.Instance.GetOnlinePlayers();

            foreach (Player player in players)
            {
                // Only consider a player online if they have a valid Id and appear in the online list
                bool isOnline = player != null &&
                                player.Id > 0 &&
                                onlinePlayers != null &&
                                onlinePlayers.Any(x => x.Id == player.Id);

                UserUC userUC = new UserUC(this, player, isOnline);

                ListBoxUsers.Items.Add(userUC);

                if (currentSelected != null &&
                    currentSelected.Id == player.Id)
                {
                    ListBoxUsers.SelectedItem = userUC;
                }
            }
        }

        // Select a player in the list and show its details on the right
        public void Set(Player player)
        {
            selectedPlayer = player;

            StackPanelUserView.DataContext = player;

            // Immediately select the corresponding item in the list so the selection
            // visual (CardListBoxItemStyle IsSelected trigger) appears without delay.
            foreach (object item in ListBoxUsers.Items)
            {
                UserUC userUC = item as UserUC;
                if (userUC != null && userUC.GetPlayer() != null && userUC.GetPlayer().Id == player.Id)
                {
                    ListBoxUsers.SelectedItem = userUC;
                    ListBoxUsers.ScrollIntoView(userUC);
                    break;
                }
            }
        }

        // Delete the currently selected player after confirmation
        private void ButtonDelete_Click(object sender, RoutedEventArgs e)
        {
            if (selectedPlayer == null)
                return;

            // Prevent deleting admin users
            if (!string.IsNullOrEmpty(selectedPlayer.UserType) &&
                selectedPlayer.UserType.IndexOf("Admin", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                MessageBox.Show(
                    "You cannot delete an admin user.",
                    "Delete User",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Prevent deleting the currently logged-in admin (self-delete)
            if (admin != null && selectedPlayer.Id == admin.Id)
            {
                MessageBox.Show(
                    "You cannot delete your own admin account.",
                    "Delete User",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            MessageBoxResult result = MessageBox.Show(
                "Are you sure you want to delete this user?",
                "Delete User",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            ChessService.DeletePlayer(selectedPlayer);

            MessageBox.Show(
                "User deleted successfully.",
                "Success",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            selectedPlayer = null;

            StackPanelUserView.DataContext = null;

            LoadUsers();
        }

        // Open the update dialog for the selected player
        private void ButtonUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (selectedPlayer == null)
                return;

            User_UpdateWindow updateWindow =
                new User_UpdateWindow(selectedPlayer);

            updateWindow.ShowDialog();

            LoadUsers();
        }

        // Open the Add New User dialog
        private void ButtonNew_Click(object sender, RoutedEventArgs e)
        {
            User_AddUserWindow addWindow =
                new User_AddUserWindow();

            addWindow.ShowDialog();

            LoadUsers();
        }

        // Force offline action removed per user request. Server-side PlayerLeave will be
        // invoked automatically for the current logged-in users when the application closes
        // (see App.xaml.cs or window OnClosed implementations where appropriate).

        // When selection changes in the left list, update the right detail view
        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UserUC selected =
                ListBoxUsers.SelectedItem as UserUC;

            if (selected == null)
                return;

            Set(selected.GetPlayer());
        }

        // Prevent the ListBox from automatically scrolling items into view
        // because selection and programmatic scrolling are handled explicitly
        // in the Set(...) method (ScrollIntoView is used there).
        private void ListBoxUsers_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            // Mark the event handled to avoid default scrolling behavior
            e.Handled = true;
        }

        // Called when another client reports a player joined or left
        // We refresh the UI on the UI thread.
        private void OnOnlineStatusChanged(WpfAdminPeritz.ServiceReferenceUserChess.Player changedPlayer)
        {
            Dispatcher.BeginInvoke(new Action(() => LoadUsers()));
        }

        // Unsubscribe from events and stop timers when the control is unloaded
        private void Users_UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (onlineTimer != null)
                onlineTimer.Stop();
            CallbackServiceManager.Instance.OnPlayerJoined -= OnOnlineStatusChanged;
            CallbackServiceManager.Instance.OnPlayerLeft -= OnOnlineStatusChanged;
        }
    }
}