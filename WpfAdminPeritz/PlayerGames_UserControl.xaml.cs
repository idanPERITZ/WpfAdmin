using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using WpfAdminPeritz.ServiceReferenceUserChess;

namespace WpfAdminPeritz
{
    public partial class PlayerGames_UserControl : UserControl
    {
        // Service used to fetch games and moves
        private ChessServiceUserClient service;
        // The currently selected game in the UI
        private Game selectedGame;
        // The player who is logged in (owning this view)
        private Player loggedInPlayer;

        // Constructor: show the logged-in player's games
        public PlayerGames_UserControl(Player player)
        {
            InitializeComponent();

            loggedInPlayer = player;
            service = CallbackServiceManager.Instance.UserService;

            LoadGames();
        }

        // Load all games for the logged-in player and populate the list
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

        // When the user selects a game from the left list, show its details
        private void ListBoxGames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GameUC_Player selected = ListBoxGames.SelectedItem as GameUC_Player;

            if (selected == null)
                return;

            selectedGame = selected.GetGame();

            ShowGameDetails(selectedGame);
        }

        // Select the matching GameUC_Player in the list and show details
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

        // Fill the right details panel with players, date, result and moves
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

        // Open the New Game window to challenge another player
        private void BtnNewGame_Click(object sender, RoutedEventArgs e)
        {
            CreateNewGameWindow createWindow =
                new CreateNewGameWindow(loggedInPlayer);

            createWindow.ShowDialog();
        }
    }
}