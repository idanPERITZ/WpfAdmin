using System;
using System.Windows;
using WpfAdminPeritz.ServiceReferenceUserChess;

namespace WpfAdminPeritz
{
    public partial class PlayerMainWindow : Window
    {
        private Player loggedInPlayer;

        public PlayerMainWindow(Player player)
        {
            InitializeComponent();

            loggedInPlayer = player;

            Title = $"Welcome, {loggedInPlayer.UserName}!";

            MainContent.Content = new PlayerGames_UserControl(loggedInPlayer);

            CallbackServiceManager.Instance.OnInvitationReceived += OnInvitationReceived;
        }

        private void BtnPlayers_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new PlayerPlayers_UserControl(loggedInPlayer);
        }

        private void BtnMyGames_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new PlayerGames_UserControl(loggedInPlayer);
        }

        private void BtnFriends_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new PlayerFriends_UserControl(loggedInPlayer);
        }

        private void OnInvitationReceived(Player inviter, bool inviterIsWhite)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                CreateNewGameWindow createWindow =
                    new CreateNewGameWindow(loggedInPlayer);

                createWindow.Show();

                createWindow.HandleIncomingInvitation(
                    inviter,
                    inviterIsWhite);
            }));
        }

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                CallbackServiceManager.Instance.UserService.PlayerLeave(loggedInPlayer);
            }
            catch { }

            CallbackServiceManager.Instance.OnInvitationReceived -= OnInvitationReceived;

            base.OnClosed(e);
        }
    }
}