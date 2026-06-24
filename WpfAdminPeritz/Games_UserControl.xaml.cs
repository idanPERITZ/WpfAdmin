using ChessUI;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.ServiceModel;
using System.Windows.Threading;
using WpfAdminPeritz.ServiceReferenceChess;
using System.Linq;

namespace WpfAdminPeritz
{
    // UserControl that manages the games list, game details panel, new game creation, and live game hosting
    // Acts as the central hub for all game-related admin operations
    public partial class Games_UserControl : UserControl
    {
        // Field: WCF service client used to communicate with the chess server
        private ChessServiceAdminClient service;
        // Field: The game currently selected in the list for viewing details
        private Game selectedGame;
        // Field: The game currently being played live (null if no game is in progress)
        private Game currentlyPlayingGame;
        // Field: The logged-in admin player
        private Player player;

        // Constructor: Initializes the control with the logged-in admin and loads the games list
        public Games_UserControl(Player loggedPlayer)
        {
            // Initialize WPF components
            InitializeComponent();
            // Store the logged-in admin reference
            player = loggedPlayer;

            // Create the service client for server communication
            service = new ChessServiceAdminClient();
            // Populate the games list from the server
            LoadGames();
        }



        // Private method: Fetches all games from the server and populates the left list panel
        private void LoadGames()
        {
            GameList games;
            // Remember selected game id so we can restore selection after refresh
            int? previouslySelectedGameId = selectedGame?.Id;

            // Clear any existing game cards before reloading
            ListBoxGames.Items.Clear();
            // Fetch all games from the server
            if (player.UserType == "Admin")
            {
                games = service.GetAllGames();
            }
            else
            {
                games = service.GetGamesByPlayer(player);
                DeleteButton.Visibility = Visibility.Collapsed;
            }
            // Create a GameUC card for each game and add it to the list
            foreach (Game game in games)
            {
                GameUC gameUC = new GameUC(this, game);
                ListBoxGames.Items.Add(gameUC);
                // Restore selection if this card matches the previously selected game
                if (previouslySelectedGameId.HasValue && game.Id == previouslySelectedGameId.Value)
                {
                    ListBoxGames.SelectedItem = gameUC;
                    // update selectedGame and details
                    selectedGame = game;
                    ShowGameDetails(game);
                }
            }
        }

        private void ListBoxGames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GameUC selected = ListBoxGames.SelectedItem as GameUC;

            if (selected == null)
                return;

            selectedGame = selected.GetGame();
            ShowGameDetails(selectedGame);
        }
        // Public method: Called by GameUC when its View button is clicked
        // Selects the matching list item and shows game details; handles both first and repeated clicks
        public void SetSelectedGame(Game game)
        {
            // Find the GameUC card in the list that matches the given game ID
            foreach (object item in ListBoxGames.Items)
            {
                GameUC gameUC = item as GameUC;
                if (gameUC != null && gameUC.GetGame().Id == game.Id)
                {
                    // If this item is already selected, SelectionChanged won't fire — show details directly
                    if (ListBoxGames.SelectedItem == item)
                    {
                        ShowGameDetails(game);
                    }
                    else
                    {
                        // Otherwise select it; SelectionChanged will fire but does nothing, so show details manually
                        ListBoxGames.SelectedItem = item;
                        ShowGameDetails(game);
                    }
                    // Store the selected game for use by delete and other operations
                    selectedGame = game;
                    break;
                }
            }
        }

        // Private method: Populates the right detail panel with players, date, result, and move history
        private void ShowGameDetails(Game game)
        {
            try
            {
                // Get the White and Black player objects from the game
                Player white = game.WhitePlayer;
                Player black = game.BlackPlayer;

                // Guard: if either player is missing, show an error message and abort
                if (white == null || black == null)
                {
                    TextPlayers.Text = "Could not load player names";
                    return;
                }

                // Display both players' usernames with color indicators
                TextPlayers.Text = "⬜ " + white.UserName + "   vs   ⬛ " + black.UserName;
                // Display the game date formatted as dd/MM/yyyy HH:mm
                TextDate.Text = "📅 " + game.GameDate.Date.ToString("dd/MM/yyyy");

                // Display the result: winner's name in green, or "Draw" in orange
                if (game.Result != null)
                {
                    TextResult.Text = "Winner: " + game.Result.UserName + " 🏆";
                    TextResult.Foreground = System.Windows.Media.Brushes.DarkGreen;
                }
                else
                {
                    TextResult.Text = "Draw";
                    TextResult.Foreground = System.Windows.Media.Brushes.DarkOrange;
                }

                // Fetch the move list for this game from the server
                MoveList dbMoves = service.GetMovesByGameID(game);

                // Primary source: moves fetched from the database
                if (dbMoves != null && dbMoves.Count > 0)
                {
                    List<MoveRow> moveRows = new List<MoveRow>();
                    // Pair moves into rows: even index = White's move, odd index = Black's move
                    for (int i = 0; i < dbMoves.Count; i += 2)
                    {
                        MoveRow row = new MoveRow();
                        // Move number is 1-based
                        row.MoveNumber = (i / 2) + 1;
                        // White's move notation is stored in the 'From' field
                        row.WhiteMove = dbMoves[i].From;
                        // Black's move may not exist if White just moved on the last turn
                        if (i + 1 < dbMoves.Count)
                            row.BlackMove = dbMoves[i + 1].From;
                        else
                            row.BlackMove = "";
                        moveRows.Add(row);
                    }
                    ListViewMoves.ItemsSource = moveRows;
                }

                // No moves available from either source
                else
                {
                    ListViewMoves.ItemsSource = null;
                }
            }
            catch (Exception ex)
            {
                // Show any unexpected errors to the admin
                MessageBox.Show("Error: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Event handler: Deletes the selected game after confirmation, reverting player stats as needed
        private void BtnDeleteGame_Click(object sender, RoutedEventArgs e)
        {
            // Guard: a game must be selected before deletion can proceed
            if (selectedGame == null)
            {
                MessageBox.Show("Please select a game first.", "No Game Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Prompt the admin to confirm deletion
            System.Windows.MessageBoxResult result = MessageBox.Show(
                "Are you sure you want to delete Game #" + selectedGame.Id + "?",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                try
                {
                    // If the game had a winner, revert the winner's win and the loser's loss
                    if (selectedGame.Result != null)
                    {
                        // Revert the winner's stats (true = was a win)
                        service.RevertPlayerStats(selectedGame.Result, true);

                        // Determine the loser: whichever player is not the winner
                        Player loserId = selectedGame.Result.Id == selectedGame.WhitePlayer.Id
                            ? selectedGame.BlackPlayer
                            : selectedGame.WhitePlayer;

                        // Revert the loser's stats (false = was a loss)
                        service.RevertPlayerStats(loserId, false);
                    }
                    else
                    {
                        // If the game was a draw, revert the draw count for both players
                        service.RevertPlayerDraw(selectedGame.WhitePlayer);
                        service.RevertPlayerDraw(selectedGame.BlackPlayer);
                    }

                    // Delete all move records for this game from the database
                    service.DeleteMovesByGameID(selectedGame);
                    // Delete the game record itself from the database
                    service.DeleteGame(selectedGame);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error deleting game: " + ex.Message);
                    return;
                }

                // Reset the detail panel to its empty state
                selectedGame = null;
                ListViewMoves.ItemsSource = null;
                TextPlayers.Text = "Select a game to view details";
                TextDate.Text = "";
                TextResult.Text = "";
                // Refresh the games list to reflect the deletion
                LoadGames();
            }
        }

        // Event handler: Switches the view to the new game creation form
        private void BtnNewGame_Click(object sender, RoutedEventArgs e)
        {
            ServiceReferenceUserChess.Player user_player = new ServiceReferenceUserChess.Player()
            {
                Id = player.Id,
                UserName = player.UserName,
                Email = player.Email,
                DateJoined = player.DateJoined,
                GamesPlayed = player.GamesPlayed,
                Wins = player.Wins,
                Losses = player.Losses,
                Draws = player.Draws,
                GoogleId = player.GoogleId,
                UserType = player.UserType
            };
            CreateNewGameWindow createWindow = new CreateNewGameWindow(user_player);
            createWindow.ShowDialog();
        }

        private void BtnCopyPgn_Click(object sender, RoutedEventArgs e)
        {
            // Guard: a game must be selected before copying PGN
            if (selectedGame == null)
            {
                MessageBox.Show("Please select a game first.", "No Game Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Fetch the move list for this game from the server
                MoveList dbMoves = service.GetMovesByGameID(selectedGame);

                if (dbMoves == null || dbMoves.Count == 0)
                {
                    MessageBox.Show("No moves found for this game.", "No Moves",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Build PGN header
                System.Text.StringBuilder pgn = new System.Text.StringBuilder();
                pgn.AppendLine($"[Event \"Chess Game\"]");
                pgn.AppendLine($"[Date \"{selectedGame.GameDate:yyyy.MM.dd}\"]");
                pgn.AppendLine($"[White \"{selectedGame.WhitePlayer.UserName}\"]");
                pgn.AppendLine($"[Black \"{selectedGame.BlackPlayer.UserName}\"]");

                if (selectedGame.Result != null)
                {
                    string result = selectedGame.Result.Id == selectedGame.WhitePlayer.Id ? "1-0" : "0-1";
                    pgn.AppendLine($"[Result \"{result}\"]");
                }
                else
                {
                    pgn.AppendLine("[Result \"1/2-1/2\"]");
                }

                pgn.AppendLine();

                // Build move text
                for (int i = 0; i < dbMoves.Count; i += 2)
                {
                    int moveNumber = (i / 2) + 1;
                    pgn.Append($"{moveNumber}. {dbMoves[i].From} ");

                    if (i + 1 < dbMoves.Count)
                    {
                        pgn.Append($"{dbMoves[i + 1].From} ");
                    }
                }

                // Add game result
                if (selectedGame.Result != null)
                {
                    string result = selectedGame.Result.Id == selectedGame.WhitePlayer.Id ? "1-0" : "0-1";
                    pgn.Append(result);
                }
                else
                {
                    pgn.Append("1/2-1/2");
                }

                // Copy to clipboard
                Clipboard.SetText(pgn.ToString());
                MessageBox.Show("PGN notation copied to clipboard!", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error copying PGN: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
    // Data model used to bind move history rows to the ListView in the detail panel
    // Each row represents one full move (White's half-move and Black's half-move)
    public class MoveRow
    {
        // Property: The 1-based move number displayed in the first column
        public int MoveNumber { get; set; }
        // Property: White's move in SAN notation (e.g. "e4", "Nf3+")
        public string WhiteMove { get; set; }
        // Property: Black's move in SAN notation (empty string if game ended on White's turn)
        public string BlackMove { get; set; }
    }
}