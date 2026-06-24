using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using WpfAdminPeritz.ServiceReferenceUserChess;

namespace WpfAdminPeritz
{
    public partial class CreateNewGameWindow : Window
    {
        private readonly Player player;
        private readonly ChessServiceUserClient service;
        private Game currentlyPlayingGame;
        private readonly Dictionary<int, List<string[]>> gameMoves = new Dictionary<int, List<string[]>>();

        private Player currentInviter;
        private bool currentInviterIsWhite;
        private DispatcherTimer onlineTimer;
        private bool isAdmin;

        public CreateNewGameWindow(Player player)
        {
            InitializeComponent();
            this.player = player;

            // Detect admin - admins have UserType containing "Admin" (case-insensitive)
            isAdmin = player != null &&
                      !string.IsNullOrEmpty(player.UserType) &&
                      player.UserType.IndexOf("Admin", StringComparison.OrdinalIgnoreCase) >= 0;

            if (isAdmin)
            {
                // Show admin block screen immediately
                SetupGrid.Visibility = Visibility.Collapsed;
                InvitationGrid.Visibility = Visibility.Collapsed;
                AdminBlockGrid.Visibility = Visibility.Visible;
                return;
            }

            service = CallbackServiceManager.Instance.UserService;
            CallbackServiceManager.Instance.OnInvitationResponseReceived += OnInvitationResponseReceived;
            CallbackServiceManager.Instance.OnOpponentLeftGame += OnOpponentLeftGame;
            LoadOnlinePlayers();

            onlineTimer = new DispatcherTimer();
            onlineTimer.Interval = TimeSpan.FromSeconds(2);
            onlineTimer.Tick += (s, e) => LoadOnlinePlayers();
            onlineTimer.Start();
        }

        private void LoadOnlinePlayers()
        {
            PlayerList onlinePlayers = CallbackServiceManager.Instance.GetOnlinePlayers();

            if (onlinePlayers == null)
            {
                ComboOpponent.ItemsSource = null;
                return;
            }

            // Only show non-admin players in the opponent list
            ComboOpponent.ItemsSource = onlinePlayers
                .Where(onlinePlayer => onlinePlayer != null &&
                                       onlinePlayer.Id > 0 &&
                                       onlinePlayer.Id != player.Id &&
                                       (string.IsNullOrEmpty(onlinePlayer.UserType) ||
                                        onlinePlayer.UserType.IndexOf("Admin", StringComparison.OrdinalIgnoreCase) < 0))
                .ToList();

            ComboOpponent.DisplayMemberPath = "UserName";
        }

        private void OnInvitationResponseReceived(Player otherPlayer, bool accepted, Game game)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                BtnCreateGame.IsEnabled = true;

                if (!accepted || game == null)
                {
                    MessageBox.Show(
                        otherPlayer.UserName + " has declined the game invitation.",
                        "Invitation Declined",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    Dispatcher.BeginInvoke(new Action(() => { this.Close(); }));
                    return;
                }

                currentlyPlayingGame = game;

                Player opponent = game.WhitePlayer.Id == player.Id
                    ? game.BlackPlayer
                    : game.WhitePlayer;
                this.Hide();
                PlayGame(game, opponent);
            }));
        }

        private void BtnCreateGame_Click(object sender, RoutedEventArgs e)
        {
            Player opponent = ComboOpponent.SelectedItem as Player;

            if (opponent == null)
            {
                MessageBox.Show(
                    "Please select an online player.",
                    "Missing Information",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            bool isWhite = RadioWhite.IsChecked == true;

            BtnCreateGame.IsEnabled = false;
            BtnCreateGame.Content = "Sending invitation...";
            try
            {
                service.InvitePlayer(player, opponent, isWhite);
            }
            catch (Exception ex)
            {
                BtnCreateGame.IsEnabled = true;
                BtnCreateGame.Content = "Send Invitation";
                MessageBox.Show(
                    "Failed to send invitation: " + ex.Message,
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void BtnAdminClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void PlayGame(Game localGame, Player opponent)
        {
            ChessMainWindowUserControl gameWindow = new ChessMainWindowUserControl();

            ChessLogic.Player color =
                localGame.WhitePlayer.Id == player.Id
                    ? ChessLogic.Player.White
                    : ChessLogic.Player.Black;

            gameWindow.SetPlayerColor(
                color,
                localGame,
                service,
                opponent.UserName,
                player,
                opponent);

            gameWindow.SetMoveCallback(
                (fromString, toString, index, notation) =>
                {
                    try
                    {
                        MoveRecord moveRecord = new MoveRecord
                        {
                            Game = localGame,
                            From = notation,
                            To = fromString + toString,
                            MoveIndex = index,
                            MoveType = "Normal"
                        };

                        service.SendMove(moveRecord);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            "Move save error: " + ex.Message,
                            "Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                });

            Window playerWindow = new Window
            {
                Title = color == ChessLogic.Player.White
                    ? "White - " + player.UserName
                    : "Black - " + player.UserName,
                Content = gameWindow,
                Width = 950,
                Height = 700,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            bool closingHandled = false;
            bool gameDeleted = false;

            Action deleteUnfinishedGame = () =>
            {
                if (localGame == null) return;
                try
                {
                    gameDeleted = true;
                    service.DeleteOpen(localGame);
                    currentlyPlayingGame = null;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "Error deleting unfinished game: " + ex.Message,
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
                localGame = null;
            };

            playerWindow.Closing += (windowSender, args) =>
            {
                if (closingHandled || localGame == null) return;

                MessageBoxResult confirm = MessageBox.Show(
                    "The game has not finished.\nIf you close this window, the game and its moves will be deleted.\nAre you sure you want to exit?",
                    "Exit Game",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirm == MessageBoxResult.No)
                {
                    args.Cancel = true;
                    return;
                }

                closingHandled = true;
                gameWindow.StopPolling();
                deleteUnfinishedGame();
            };

            gameWindow.GameOver += (winner, reason, moves) =>
            {
                if (gameDeleted || localGame == null) return;

                gameMoves[localGame.Id] = moves;
                Player winnerPlayer = null;
                if (winner == ChessLogic.Player.White)
                    winnerPlayer = localGame.WhitePlayer;
                else if (winner == ChessLogic.Player.Black)
                    winnerPlayer = localGame.BlackPlayer;

                Game completedGame = localGame;
                localGame = null;
                currentlyPlayingGame = null;

                DispatcherTimer closeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
                closeTimer.Tick += (timerSender, timerArgs) =>
                {
                    closeTimer.Stop();
                    gameWindow.StopPolling();
                    SaveResultAndClose(winnerPlayer, completedGame);
                    closingHandled = true;
                    playerWindow.Close();
                };
                closeTimer.Start();
            };

            gameWindow.GameExited += () =>
            {
                Dispatcher.Invoke(() =>
                {
                    closingHandled = true;
                    gameWindow.StopPolling();
                    playerWindow.Close();
                });
            };

            playerWindow.Show();
        }

        private void SaveResultAndClose(Player winnerPlayer, Game gameToSave)
        {
            try
            {
                gameToSave.Result = winnerPlayer;
                service.UpdateGameResult(gameToSave);

                if (winnerPlayer != null)
                {
                    service.UpdatePlayerStats(winnerPlayer, true);
                    Player loser = winnerPlayer.Id == gameToSave.WhitePlayer.Id
                        ? gameToSave.BlackPlayer
                        : gameToSave.WhitePlayer;
                    service.UpdatePlayerStats(loser, false);
                }
                else
                {
                    service.UpdatePlayerDraw(gameToSave.WhitePlayer);
                    service.UpdatePlayerDraw(gameToSave.BlackPlayer);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Failed to save result: " + ex.Message,
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                Close();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            if (!isAdmin)
            {
                CallbackServiceManager.Instance.OnInvitationResponseReceived -= OnInvitationResponseReceived;
                CallbackServiceManager.Instance.OnOpponentLeftGame -= OnOpponentLeftGame;
                if (onlineTimer != null)
                    onlineTimer.Stop();
            }
            base.OnClosed(e);
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (!isAdmin && currentlyPlayingGame != null)
            {
                var gameToSend = currentlyPlayingGame;
                var playerLeaving = this.player;
                System.Threading.Tasks.Task.Run(() =>
                {
                    try { service.LeaveGame(gameToSend, playerLeaving); }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Error sending LeaveGame: " + ex.Message); }
                });
            }
            base.OnClosing(e);
        }

        public void HandleIncomingInvitation(Player inviter, bool inviterIsWhite)
        {
            this.currentInviter = inviter;
            this.currentInviterIsWhite = inviterIsWhite;
            string color = inviterIsWhite ? "Black" : "White";
            TxtInvitationMessage.Text = $"{inviter.UserName} invites you to a chess game!\nYou will play as {color}\nDo you want to accept?";
            SetupGrid.Visibility = Visibility.Collapsed;
            InvitationGrid.Visibility = Visibility.Visible;
        }

        private void BtnAccept_Click(object sender, RoutedEventArgs e)
        {
            bool invitedIsWhite = !currentInviterIsWhite;
            System.Threading.Tasks.Task.Run(() =>
            {
                try { CallbackServiceManager.Instance.RespondToInvitation(currentInviter, player, true, invitedIsWhite); }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine("WCF Error on accept: " + ex.Message); }
            });
        }

        private void BtnDecline_Click(object sender, RoutedEventArgs e)
        {
            bool invitedIsWhite = !currentInviterIsWhite;
            System.Threading.Tasks.Task.Run(() =>
            {
                try { CallbackServiceManager.Instance.RespondToInvitation(currentInviter, player, false, invitedIsWhite); }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine("WCF Error on decline: " + ex.Message); }
            });
            this.Close();
        }

        private void OnOpponentLeftGame()
        {
            this.Close();
        }
    }
}
