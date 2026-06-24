using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using WpfAdminPeritz.ServiceReferenceChess;

namespace WpfAdminPeritz
{
    public partial class Friendship_UserControl : UserControl
    {
        private ChessServiceAdminClient service;
        private Player LoggedInPlayer;
        private DispatcherTimer onlineTimer;
        private Player currentSelectedPlayer;

        public Friendship_UserControl(Player loggedInAdmin)
        {
            InitializeComponent();

            service = new ChessServiceAdminClient();

            // remember logged-in user so we can exclude them from lists
            LoggedInPlayer = loggedInAdmin;

            // Subscribe to join/leave events to keep online statuses accurate
            CallbackServiceManager.Instance.OnPlayerJoined += Instance_OnPlayerJoined;
            CallbackServiceManager.Instance.OnPlayerLeft += Instance_OnPlayerLeft;

            // Periodically refresh online status in case server missed a disconnect
            onlineTimer = new DispatcherTimer();
            onlineTimer.Interval = TimeSpan.FromSeconds(2);
            onlineTimer.Tick += OnlineTimer_Tick;
            onlineTimer.Start();

            LoadUsers();

            Unloaded += Friendship_UserControl_Unloaded;
        }

        private void Friendship_UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            CallbackServiceManager.Instance.OnPlayerJoined -= Instance_OnPlayerJoined;
            CallbackServiceManager.Instance.OnPlayerLeft -= Instance_OnPlayerLeft;
            if (onlineTimer != null)
                onlineTimer.Stop();
        }

        private void Instance_OnPlayerJoined(ServiceReferenceUserChess.Player player)
        {
            Dispatcher.BeginInvoke(new Action(() => LoadUsers()));
        }

        private void Instance_OnPlayerLeft(ServiceReferenceUserChess.Player player)
        {
            Dispatcher.BeginInvoke(new Action(() => LoadUsers()));
        }

        private void OnlineTimer_Tick(object sender, EventArgs e)
        {
            LoadUsers();
        }

        private void LoadUsers()
        {
            // Preserve the currently selected player id so selection can be restored after refresh
            int? previouslySelectedId = currentSelectedPlayer?.Id;

            ListBoxPlayers.Items.Clear();

            PlayerList players = service.GetAllplayers();

            var onlinePlayers = CallbackServiceManager.Instance.GetOnlinePlayers();

            foreach (Player player in players)
            {
                // Do not show the logged-in player in the list
                if (LoggedInPlayer != null && player != null && player.Id == LoggedInPlayer.Id)
                    continue;

                // Allow all user types to be shown so any user can interact with any other.
                // Consider them "online" when they appear in the online user list from the user service.
                bool isOnline = player != null &&
                                player.Id > 0 &&
                                onlinePlayers != null &&
                                onlinePlayers.Any(x => x.Id == player.Id);

                var uc = new FriendshipUC(this, player, isOnline);
                ListBoxPlayers.Items.Add(uc);

                // If this item was previously selected, restore selection and view state
                if (previouslySelectedId.HasValue && player.Id == previouslySelectedId.Value)
                {
                    ListBoxPlayers.SelectedItem = uc;
                    uc.SetViewEnabled(true);
                    currentSelectedPlayer = player;
                    ShowFriendsOf(player);
                }
            }

            // If there was a previously selected player id but we couldn't find it in the
            // refreshed list, clear the friends view.
            if (previouslySelectedId.HasValue && (currentSelectedPlayer == null || currentSelectedPlayer.Id != previouslySelectedId.Value))
            {
                ListBoxFriends.Items.Clear();
                SelectedPlayerName.Text = "Select a Player";
                TxtWins.Text = "0";
                TxtDraws.Text = "0";
                TxtLosses.Text = "0";
            }
        }

        private void ListBoxPlayers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (object item in ListBoxPlayers.Items)
            {
                FriendshipUC userControl = item as FriendshipUC;

                if (userControl != null)
                    userControl.SetViewEnabled(false);
            }

            FriendshipUC selectedControl = ListBoxPlayers.SelectedItem as FriendshipUC;

            if (selectedControl == null)
                return;

            selectedControl.SetViewEnabled(true);

            ShowFriendsOf(selectedControl.GetPlayer());
            currentSelectedPlayer = selectedControl.GetPlayer();
        }

        public void ShowFriendsOf(Player player)
        {
            SelectedPlayerName.Text = player.UserName + "'s Friends";

            TxtWins.Text = player.Wins.ToString();
            TxtDraws.Text = player.Draws.ToString();
            TxtLosses.Text = player.Losses.ToString();

            ListBoxFriends.Items.Clear();

            FriendshipList friendships = service.GetAcceptedFriendsByPlayer(player);

            var onlinePlayers = CallbackServiceManager.Instance.GetOnlinePlayers();

            if (friendships == null || friendships.Count == 0)
            {
                ListBoxFriends.Items.Add(new TextBlock
                {
                    Text = "No friends yet.",
                    Foreground = Brushes.Gray,
                    Padding = new Thickness(8),
                    FontSize = 14
                });

                return;
            }

            foreach (Friendship friendship in friendships)
            {
                Player friend;

                if (friendship.RequesterID.Id == player.Id)
                    friend = friendship.ReceiverID;
                else
                    friend = friendship.RequesterID;

                // Skip if the friend is the player him/herself (defensive)
                if (friend == null || friend.Id == player.Id)
                    continue;

                if (friend != null)
                {
                    // Mark friends as online when they appear in the online list returned by the user service
                    bool isOnline = friend != null &&
                                    friend.Id > 0 &&
                                    onlinePlayers != null &&
                                    onlinePlayers.Any(x => x.Id == friend.Id);

                    ListBoxFriends.Items.Add(CreateFriendCard(friend, isOnline));
                }
            }
        }

        private Border CreateFriendCard(Player friend, bool isOnline)
        {
            Border card = new Border
            {
                CornerRadius = new CornerRadius(12),
                Margin = new Thickness(0, 5, 0, 5),
                Padding = new Thickness(14, 10, 14, 10),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F9F9F9"))
            };

            Grid row = new Grid();

            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(44) });

            row.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(1, GridUnitType.Star)
            });

            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Border avatar = new Border
            {
                Width = 34,
                Height = 34,
                CornerRadius = new CornerRadius(17),
                Background = Brushes.Black,
                VerticalAlignment = VerticalAlignment.Center
            };

            avatar.Child = new TextBlock
            {
                Text = "♟",
                FontSize = 16,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            Grid.SetColumn(avatar, 0);

            StackPanel namePanel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 0, 0)
            };

            namePanel.Children.Add(new TextBlock
            {
                Text = friend.UserName,
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.Black
            });

            TextBlock status = new TextBlock
            {
                FontSize = 11,
                FontWeight = FontWeights.SemiBold
            };

            if (isOnline)
            {
                status.Text = "● Online";
                status.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60"));
            }
            else
            {
                status.Text = "● Offline";
                status.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C0392B"));
            }

            namePanel.Children.Add(status);

            Grid.SetColumn(namePanel, 1);

            StackPanel statsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            statsPanel.Children.Add(MakeBadge("W: " + friend.Wins, "#E8F5E9", "#2E7D32"));
            statsPanel.Children.Add(MakeBadge("D: " + friend.Draws, "#F5F5F5", "#616161"));
            statsPanel.Children.Add(MakeBadge("L: " + friend.Losses, "#FFEBEE", "#C62828"));

            Grid.SetColumn(statsPanel, 2);

            row.Children.Add(avatar);
            row.Children.Add(namePanel);
            row.Children.Add(statsPanel);

            card.Child = row;

            return card;
        }

        private Border MakeBadge(string text, string bgColor, string fgColor)
        {
            return new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bgColor)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 3, 8, 3),
                Margin = new Thickness(0, 0, 6, 0),

                Child = new TextBlock
                {
                    Text = text,
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(fgColor))
                }
            };
        }
    }
}