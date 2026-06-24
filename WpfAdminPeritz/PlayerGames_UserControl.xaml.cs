using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using WpfAdminPeritz.ServiceReferenceUserChess;

namespace WpfAdminPeritz
{
    public partial class PlayerGames_UserControl : UserControl
    {
        private ChessServiceUserClient service;
        private Game selectedGame;
        private Player loggedInPlayer;

        public PlayerGames_UserControl(Player player)
        {
            InitializeComponent();

            loggedInPlayer = player;
            service = CallbackServiceManager.Instance.UserService;

            LoadGames();
        }

        private void LoadGames()
        {
            ListBoxGames.Items.Clear();

            GameList games = service.GetGamesByPlayer(loggedInPlayer);

            foreach (Game game in games)
            {
                GameUC_Player gameUC = new GameUC_Player(this, game);

                ListBoxGames.Items.Add(gameUC);
            }
        }

        private void ListBoxGames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GameUC_Player selected = ListBoxGames.SelectedItem as GameUC_Player;

            if (selected == null)
                return;

            selectedGame = selected.GetGame();

            ShowGameDetails(selectedGame);
        }

        public void SetSelectedGame(Game game)
        {
            foreach (object item in ListBoxGames.Items)
            {
                GameUC_Player gameUC = item as GameUC_Player;

                if (gameUC != null && gameUC.GetGame().Id == game.Id)
                {
                    if (ListBoxGames.SelectedItem == item)
                    {
                        ShowGameDetails(game);
                    }
                    else
                    {
                        ListBoxGames.SelectedItem = item;
                        ShowGameDetails(game);
                    }

                    selectedGame = game;

                    break;
                }
            }
        }

        private void ShowGameDetails(Game game)
        {
            Player white = game.WhitePlayer;
            Player black = game.BlackPlayer;

            TextPlayers.Text =
                "⬜ " + white.UserName +
                "   vs   ⬛ " + black.UserName;

            TextDate.Text =
                "📅 " +
                game.GameDate.Date.ToString("dd/MM/yyyy");

            if (game.Result != null)
            {
                TextResult.Text =
                    "Winner: " +
                    game.Result.UserName +
                    " 🏆";

                TextResult.Foreground =
                    System.Windows.Media.Brushes.DarkGreen;
            }
            else
            {
                TextResult.Text = "Draw";

                TextResult.Foreground =
                    System.Windows.Media.Brushes.DarkOrange;
            }

            MoveList dbMoves = service.GetMovesByGameID(game);

            if (dbMoves != null && dbMoves.Count > 0)
            {
                List<MoveRow> moveRows = new List<MoveRow>();

                for (int i = 0; i < dbMoves.Count; i += 2)
                {
                    MoveRow row = new MoveRow();

                    row.MoveNumber = (i / 2) + 1;
                    row.WhiteMove = dbMoves[i].From;

                    if (i + 1 < dbMoves.Count)
                        row.BlackMove = dbMoves[i + 1].From;
                    else
                        row.BlackMove = "";

                    moveRows.Add(row);
                }

                ListViewMoves.ItemsSource = moveRows;
            }
            else
            {
                ListViewMoves.ItemsSource = null;
            }
        }

        private void BtnNewGame_Click(object sender, RoutedEventArgs e)
        {
            CreateNewGameWindow createWindow =
                new CreateNewGameWindow(loggedInPlayer);

            createWindow.ShowDialog();
        }
    }
}