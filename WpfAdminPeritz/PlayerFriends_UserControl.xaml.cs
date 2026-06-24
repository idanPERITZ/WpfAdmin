using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfAdminPeritz.ServiceReferenceUserChess;

namespace WpfAdminPeritz
{
    public partial class PlayerFriends_UserControl : UserControl
    {
        // Service to contact server for player and friendship data
        private ChessServiceUserClient service;
        // The player who is currently using this view
        private Player loggedInPlayer;

        // Constructor: prepare the friends list and subscribe to online events
        public PlayerFriends_UserControl(Player player)
        {
            InitializeComponent();

            loggedInPlayer = player;
            service = CallbackServiceManager.Instance.UserService;

            CallbackServiceManager.Instance.OnPlayerJoined += OnOnlineStatusChanged;
            CallbackServiceManager.Instance.OnPlayerLeft += OnOnlineStatusChanged;

            LoadUsers();

            Unloaded += (s, e) =>
            {
                CallbackServiceManager.Instance.OnPlayerJoined -= OnOnlineStatusChanged;
                CallbackServiceManager.Instance.OnPlayerLeft -= OnOnlineStatusChanged;
            };
        }

        // Called when someone joins or leaves online; refresh the friends list
        private void OnOnlineStatusChanged(Player changedPlayer)
        {
            Dispatcher.BeginInvoke(new Action(() => LoadUsers()));
        }

        // Load accepted friends and mark which of them are online
        private void LoadUsers()
        {
            // preserve selection
            int? previouslySelectedId = null;
            var selected = ListBoxPlayers.SelectedItem as Border;
            if (selected != null && selected.Tag is Player selPlayer)
                previouslySelectedId = selPlayer.Id;

            ListBoxPlayers.Items.Clear();

            FriendshipList friendships = service.GetAcceptedFriendsByPlayer(loggedInPlayer);
            PlayerList onlinePlayers = service.GetOnlinePlayers();

            foreach (Friendship friendship in friendships)
            {
                Player friend;

                if (friendship.RequesterID.Id == loggedInPlayer.Id)
                    friend = friendship.ReceiverID;
                else
                    friend = friendship.RequesterID;

                bool isOnline = false;

                foreach (Player onlinePlayer in onlinePlayers)
                {
                    if (onlinePlayer.Id == friend.Id)
                    {
                        isOnline = true;
                        break;
                    }
                }

                var card = CreatePlayerCard(friend, isOnline);
                ListBoxPlayers.Items.Add(card);

                if (previouslySelectedId.HasValue && friend != null && friend.Id == previouslySelectedId.Value)
                {
                    ListBoxPlayers.SelectedItem = card;
                }
            }
        }

        // Make a small UI card representing a friend (name, email, online dot)
        private Border CreatePlayerCard(Player player, bool isOnline)
        {
            Border card = new Border
            {
                CornerRadius = new CornerRadius(12),
                Margin = new Thickness(0, 5, 0, 5),
                Padding = new Thickness(14, 10, 14, 10),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F9F9F9")),
                Tag = player
            };

            Grid row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(44) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // יצירת האייקון עם החייל
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
                Text = player.UserName,
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.Black
            });

            namePanel.Children.Add(new TextBlock
            {
                Text = player.Email,
                FontSize = 11,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#888888"))
            });

            namePanel.Children.Add(new TextBlock
            {
                Text = isOnline ? "● Online" : "● Offline",
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 4, 0, 0),
                Foreground = isOnline
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60"))
                    : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C0392B"))
            });

            Grid.SetColumn(namePanel, 1);

            row.Children.Add(avatar);
            row.Children.Add(namePanel);

            card.Child = row;

            return card;
        }

        // When a friend card is clicked, show that friend's details on the right
        private void ListBoxPlayers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Border selectedCard = ListBoxPlayers.SelectedItem as Border;
            if (selectedCard == null) return;

            Player selectedPlayer = selectedCard.Tag as Player;
            if (selectedPlayer == null) return;

            ShowFriendDetails(selectedPlayer);
        }

        // Populate the right-side panel with the selected friend's stats and games
        private void ShowFriendDetails(Player player)
        {
            SelectedPlayerName.Text = player.UserName + "'s Profile";
            TxtWins.Text = player.Wins.ToString();
            TxtDraws.Text = player.Draws.ToString();
            TxtLosses.Text = player.Losses.ToString();

            ListBoxFriends.Items.Clear();

            GameList games = service.GetGamesByPlayer(player);

            if (games == null || games.Count == 0)
            {
                ListBoxFriends.Items.Add(new TextBlock
                {
                    Text = "No games yet.",
                    Foreground = Brushes.Gray,
                    Padding = new Thickness(8),
                    FontSize = 14
                });
                return;
            }

            foreach (Game game in games)
            {
                // Show who played and who won (or indicate a draw)
                string resultText = game.Result != null ? $" ({game.Result.UserName} Won)" : " (Draw)";
                ListBoxFriends.Items.Add(new TextBlock
                {
                    Text = $"{game.WhitePlayer.UserName} vs {game.BlackPlayer.UserName}{resultText}",
                    FontSize = 14,
                    Padding = new Thickness(8),
                    Foreground = Brushes.Black
                });
            }
        }
    }
}