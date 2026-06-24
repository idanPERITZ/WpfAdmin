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
        private ChessServiceAdminClient ChessService;
        private Player admin;
        private Player selectedPlayer;
        private DispatcherTimer onlineTimer;

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

        private void OnlineTimer_Tick(object sender, EventArgs e)
        {
            LoadUsers();
        }

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

        private void ButtonUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (selectedPlayer == null)
                return;

            User_UpdateWindow updateWindow =
                new User_UpdateWindow(selectedPlayer);

            updateWindow.ShowDialog();

            LoadUsers();
        }

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

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UserUC selected =
                ListBoxUsers.SelectedItem as UserUC;

            if (selected == null)
                return;

            Set(selected.GetPlayer());
        }

        private void OnOnlineStatusChanged(WpfAdminPeritz.ServiceReferenceUserChess.Player changedPlayer)
        {
            Dispatcher.BeginInvoke(new Action(() => LoadUsers()));
        }

        private void Users_UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (onlineTimer != null)
                onlineTimer.Stop();
            CallbackServiceManager.Instance.OnPlayerJoined -= OnOnlineStatusChanged;
            CallbackServiceManager.Instance.OnPlayerLeft -= OnOnlineStatusChanged;
        }

        private void ListBoxUsers_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            // Prevent auto-scroll behavior when items are selected programmatically
            e.Handled = true;
        }
    }
}