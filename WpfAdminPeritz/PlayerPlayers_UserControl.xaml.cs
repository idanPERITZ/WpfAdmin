using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfAdminPeritz.ServiceReferenceUserChess;

namespace WpfAdminPeritz
{
    public partial class PlayerPlayers_UserControl : UserControl
    {
        private ChessServiceUserClient service;
        private Player loggedInPlayer;

        public PlayerPlayers_UserControl(Player player)
        {
            InitializeComponent();

            loggedInPlayer = player;
            service = CallbackServiceManager.Instance.UserService;

            // Subscribe to real-time online status events
            CallbackServiceManager.Instance.OnPlayerJoined += OnOnlineStatusChanged;
            CallbackServiceManager.Instance.OnPlayerLeft += OnOnlineStatusChanged;

            LoadPlayers();

            Unloaded += (s, e) =>
            {
                CallbackServiceManager.Instance.OnPlayerJoined -= OnOnlineStatusChanged;
                CallbackServiceManager.Instance.OnPlayerLeft -= OnOnlineStatusChanged;
            };
        }

        private void OnOnlineStatusChanged(Player changedPlayer)
        {
            Dispatcher.BeginInvoke(new Action(() => LoadPlayers()));
        }

        private void LoadPlayers()
        {
            ListBoxPlayers.Items.Clear();

            PlayerList players = service.GetAllplayers();
            PlayerList onlinePlayers = service.GetOnlinePlayers();

            foreach (Player player in players)
            {
                if (player.Id == loggedInPlayer.Id)
                    continue;

                if (!string.IsNullOrEmpty(player.UserType) &&
                    player.UserType.IndexOf("Admin", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;

                bool isOnline = false;
                foreach (Player onlinePlayer in onlinePlayers)
                {
                    if (onlinePlayer.Id == player.Id)
                    {
                        isOnline = true;
                        break;
                    }
                }

                ListBoxPlayers.Items.Add(CreatePlayerCard(player, isOnline));
            }
        }

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

        private void ListBoxPlayers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Border selectedCard = ListBoxPlayers.SelectedItem as Border;
            if (selectedCard == null) return;

            Player selectedPlayer = selectedCard.Tag as Player;
            if (selectedPlayer == null) return;

            ShowPlayerDetails(selectedPlayer);
        }

        private void ShowPlayerDetails(Player player)
        {
            SelectedPlayerName.Text = player.UserName + "'s Profile";
            TxtWins.Text = player.Wins.ToString();
            TxtDraws.Text = player.Draws.ToString();
            TxtLosses.Text = player.Losses.ToString();

            ListBoxGames.Items.Clear();

            GameList games = service.GetGamesByPlayer(player);

            if (games == null || games.Count == 0)
            {
                ListBoxGames.Items.Add(new TextBlock
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
                ListBoxGames.Items.Add(new TextBlock
                {
                    Text = $"{game.WhitePlayer.UserName} vs {game.BlackPlayer.UserName}",
                    FontSize = 14,
                    Padding = new Thickness(8),
                    Foreground = Brushes.Black
                });
            }
        }

        private void ListBoxGames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Optional: show game preview or details when a game is selected.
            // This method exists to match the XAML event handler and avoid build errors.
        }
    }
}
