using ChessUI;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using WpfAdminPeritz.ServiceReferenceChess;

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
        private Player admin;
        // Field: In-memory cache of move lists keyed by GameID (used when DB moves are unavailable)
        private Dictionary<int, List<string[]>> gameMoves = new Dictionary<int, List<string[]>>();

        // Constructor: Initializes the control with the logged-in admin and loads the games list
        public Games_UserControl(Player loggedInAdmin)
        {
            // Initialize WPF components
            InitializeComponent();
            // Store the logged-in admin reference
            admin = loggedInAdmin;
            // Create the service client for server communication
            service = new ChessServiceAdminClient();
            // Populate the games list from the server
            LoadGames();
        }

        // Private method: Fetches all games from the server and populates the left list panel
        private void LoadGames()
        {
            // Clear any existing game cards before reloading
            ListBoxGames.Items.Clear();
            // Fetch all games from the server
            GameList games = service.GetAllGames();
            // Create a GameUC card for each game and add it to the list
            foreach (Game game in games)
            {
                GameUC gameUC = new GameUC(this, game);
                ListBoxGames.Items.Add(gameUC);
            }
        }

        // Event handler: Fires when the list selection changes
        // Details are intentionally shown only via SetSelectedGame (View button click), not on selection change
        private void ListBoxGames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Do nothing - details are only shown on explicit View button click via SetSelectedGame
        }

        // Public method: Called by GameUC when its View button is clicked
        // Selects the matching list item and shows game details; handles both first and repeated clicks
        public void SetSelectedGame(Game game)
        {
            // Find the GameUC card in the list that matches the given game ID
            foreach (object item in ListBoxGames.Items)
            {
                GameUC gameUC = item as GameUC;
                if (gameUC != null && gameUC.GetGame().GameID == game.GameID)
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
                Player white = game.WhitePlayerUserID;
                Player black = game.BlackPlayerUserID;

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
                MoveList dbMoves = service.GetMovesByGameID(game.GameID);

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
                // Fallback source: in-memory move cache (used for games just played this session)
                else if (gameMoves.ContainsKey(game.GameID))
                {
                    List<string[]> moves = gameMoves[game.GameID];
                    List<MoveRow> moveRows = new List<MoveRow>();
                    // Each entry in gameMoves is already a [WhiteMove, BlackMove] pair
                    for (int i = 0; i < moves.Count; i++)
                    {
                        MoveRow row = new MoveRow();
                        row.MoveNumber = i + 1;
                        row.WhiteMove = moves[i][0];
                        // Black's move may be null if the game ended on White's turn
                        row.BlackMove = moves[i][1] ?? "";
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
                "Are you sure you want to delete Game #" + selectedGame.GameID + "?",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                try
                {
                    // If the game had a winner, revert the winner's win and the loser's loss
                    if (selectedGame.Result != null)
                    {
                        // Revert the winner's stats (true = was a win)
                        service.RevertPlayerStats(selectedGame.Result.Id, true);

                        // Determine the loser: whichever player is not the winner
                        int loserId = selectedGame.Result.Id == selectedGame.WhitePlayerUserID.Id
                            ? selectedGame.BlackPlayerUserID.Id
                            : selectedGame.WhitePlayerUserID.Id;

                        // Revert the loser's stats (false = was a loss)
                        service.RevertPlayerStats(loserId, false);
                    }
                    else
                    {
                        // If the game was a draw, revert the draw count for both players
                        service.RevertPlayerDraw(selectedGame.WhitePlayerUserID.Id);
                        service.RevertPlayerDraw(selectedGame.BlackPlayerUserID.Id);
                    }

                    // Delete all move records for this game from the database
                    service.DeleteMovesByGameID(selectedGame.GameID);
                    // Delete the game record itself from the database
                    service.DeleteGame(selectedGame);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error deleting game: " + ex.Message);
                    return;
                }

                // Remove the game's move cache entry if it exists in memory
                if (gameMoves.ContainsKey(selectedGame.GameID))
                    gameMoves.Remove(selectedGame.GameID);

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
            // Fetch all users and filter to only Registered players for the player dropdowns
            PlayerList allUsers = service.GetAllUsers();
            List<Player> users = new List<Player>();
            foreach (Player p in allUsers)
            {
                if (p.UserType == "Registered")
                    users.Add(p);
            }

            // Populate the White and Black player combo boxes
            ComboWhite.ItemsSource = users;
            ComboBlack.ItemsSource = users;
            // Default the date picker to today's date and time
            DateGame.SelectedDate = DateTime.Now;

            // Hide the games list and show the new game creation form
            GamesListView.Visibility = Visibility.Collapsed;
            NewGameView.Visibility = Visibility.Visible;
        }

        // Event handler: Cancels new game creation and returns to the games list view
        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            // Hide the creation form and restore the games list
            NewGameView.Visibility = Visibility.Collapsed;
            GamesListView.Visibility = Visibility.Visible;
        }

        // Event handler: Validates input, creates the game in the DB, and launches both player windows
        private void BtnCreateGame_Click(object sender, RoutedEventArgs e)
        {
            // Get the selected White and Black players from the combo boxes
            Player white = ComboWhite.SelectedItem as Player;
            Player black = ComboBlack.SelectedItem as Player;

            // Validate that both players have been selected
            if (white == null || black == null)
            {
                MessageBox.Show("Please select both players.", "Missing Information",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validate that the two selected players are not the same person
            if (white.Id == black.Id)
            {
                MessageBox.Show("Players must be different.", "Invalid Selection",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validate that a game date has been selected
            if (!DateGame.SelectedDate.HasValue)
            {
                MessageBox.Show("Please select a game date.", "Missing Information",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Build the new game object with the selected players and date
            Game newGame = new Game();
            newGame.WhitePlayerUserID = white;
            newGame.BlackPlayerUserID = black;
            newGame.GameDate = DateGame.SelectedDate.Value;
            // Result is null until the game is finished
            newGame.Result = null;

            // Insert the game into the database and retrieve it with its assigned GameID
            currentlyPlayingGame = service.InsertGameAndReturn(newGame);

            // Guard: abort if the database insert failed
            if (currentlyPlayingGame == null)
            {
                MessageBox.Show("Failed to create game in database!", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Capture a local reference to the game to safely use inside lambdas
            Game localGame = currentlyPlayingGame;

            // Create the chess board UserControls for each player
            ChessMainWindowUserControl whiteWindow = new ChessMainWindowUserControl();
            ChessMainWindowUserControl blackWindow = new ChessMainWindowUserControl();

            // Configure each window with its assigned color, game ID, service, and opponent name
            whiteWindow.SetPlayerColor(ChessLogic.Player.White, localGame.GameID, service, black.UserName, white, black);
            blackWindow.SetPlayerColor(ChessLogic.Player.Black, localGame.GameID, service, white.UserName, black, white);

            // Register White's move callback: saves each move to the DB with an even MoveIndex
            whiteWindow.SetMoveCallback((fromStr, toStr, index, notation) =>
            {
                try
                {
                    // Build the move record with notation in 'From' and coordinate string in 'To'
                    ServiceReferenceChess.MoveRecord moveRecord = new ServiceReferenceChess.MoveRecord();
                    moveRecord.GameID = localGame.GameID;
                    // SAN notation (e.g. "e4", "Nf3") stored in From field
                    moveRecord.From = notation;
                    // Coordinate string (e.g. "e2e4") stored in To field
                    moveRecord.To = fromStr + toStr;
                    // White's moves use even indices (0, 2, 4, ...)
                    moveRecord.MoveIndex = index * 2;
                    moveRecord.MoveType = "Normal";
                    service.InsertMove(moveRecord);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Move save error: " + ex.Message);
                }
            });

            // Register Black's move callback: saves each move to the DB with an odd MoveIndex
            blackWindow.SetMoveCallback((fromStr, toStr, index, notation) =>
            {
                try
                {
                    // Build the move record with notation in 'From' and coordinate string in 'To'
                    ServiceReferenceChess.MoveRecord moveRecord = new ServiceReferenceChess.MoveRecord();
                    moveRecord.GameID = localGame.GameID;
                    // SAN notation stored in From field
                    moveRecord.From = notation;
                    // Coordinate string stored in To field
                    moveRecord.To = fromStr + toStr;
                    // Black's moves use odd indices (1, 3, 5, ...)
                    moveRecord.MoveIndex = (index * 2) + 1;
                    moveRecord.MoveType = "Normal";
                    service.InsertMove(moveRecord);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Move save error: " + ex.Message);
                }
            });

            // Create the WPF host window for White's chess board
            Window whitePlayerWindow = new Window
            {
                Title = "♟ White - " + white.UserName,
                Content = whiteWindow,
                Width = 950,
                Height = 700,
                WindowStartupLocation = WindowStartupLocation.Manual,
                // Position White's window on the left side of the screen
                Left = 100,
                Top = 100
            };

            // Create the WPF host window for Black's chess board
            Window blackPlayerWindow = new Window
            {
                Title = "♟ Black - " + black.UserName,
                Content = blackWindow,
                Width = 950,
                Height = 700,
                WindowStartupLocation = WindowStartupLocation.Manual,
                // Position Black's window on the right side of the screen
                Left = 1100,
                Top = 100
            };

            // Flag to prevent both window Closing handlers from firing simultaneously
            bool isClosingHandled = false;
            // Flag to prevent the GameOver handler from trying to save a game that was already deleted
            bool isGameDeleted = false;

            // Local action: Deletes the unfinished game and all its moves from the database
            Action deleteUnfinishedGame = () =>
            {
                try
                {
                    isGameDeleted = true;
                    // Remove all move records for this game
                    service.DeleteMovesByGameID(localGame.GameID);
                    // Remove the game record itself
                    service.DeleteGame(localGame);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error deleting unfinished game: " + ex.Message);
                }
                // Null out localGame to signal that cleanup is complete
                localGame = null;
            };

            // Closing handler for White's window: prompts confirmation and cleans up if confirmed
            whitePlayerWindow.Closing += (s, args) =>
            {
                // Prevent double-handling if the other window already triggered closure
                if (isClosingHandled) return;
                // If localGame is null the game is already finished or deleted — allow closing
                if (localGame == null) return;

                // Warn the admin that closing will delete the unfinished game
                MessageBoxResult confirm = MessageBox.Show(
                    "The game has not finished.\nIf you close this window, the game and all its moves will be deleted from the database.\nAre you sure you want to exit?",
                    "Exit Game", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (confirm == MessageBoxResult.No)
                {
                    // Cancel the close and let the game continue
                    args.Cancel = true;
                    return;
                }

                // Mark as handled so Black's Closing handler doesn't repeat this logic
                isClosingHandled = true;
                // Stop both polling timers before closing
                whiteWindow.StopPolling();
                blackWindow.StopPolling();
                // Delete the unfinished game from the database
                deleteUnfinishedGame();
                // Close Black's window too
                blackPlayerWindow.Close();
                // Refresh the games list on the UI thread
                Dispatcher.Invoke(() => LoadGames());
            };

            // Closing handler for Black's window: mirrors White's closing logic
            blackPlayerWindow.Closing += (s, args) =>
            {
                // Prevent double-handling if the other window already triggered closure
                if (isClosingHandled) return;
                // If localGame is null the game is already finished or deleted — allow closing
                if (localGame == null) return;

                // Warn the admin that closing will delete the unfinished game
                MessageBoxResult confirm = MessageBox.Show(
                    "The game has not finished.\nIf you close this window, the game and all its moves will be deleted from the database.\nAre you sure you want to exit?",
                    "Exit Game", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (confirm == MessageBoxResult.No)
                {
                    // Cancel the close and let the game continue
                    args.Cancel = true;
                    return;
                }

                // Mark as handled so White's Closing handler doesn't repeat this logic
                isClosingHandled = true;
                // Stop both polling timers before closing
                whiteWindow.StopPolling();
                blackWindow.StopPolling();
                // Delete the unfinished game from the database
                deleteUnfinishedGame();
                // Close White's window too
                whitePlayerWindow.Close();
                // Refresh the games list on the UI thread
                Dispatcher.Invoke(() => LoadGames());
            };

            // GameOver handler for White's window: saves the result and schedules window closure
            whiteWindow.GameOver += (winner, reason, moves) =>
            {
                // Guard: don't process if the game was already deleted (e.g. window closed early)
                if (isGameDeleted) return;
                // Guard: don't process if localGame was already nulled out
                if (localGame == null) return;
                // Cache the move list in memory for display in the detail panel
                gameMoves[localGame.GameID] = moves;

                // Resolve the winner to a service Player object (null = draw)
                Player winnerServicePlayer;
                if (winner == ChessLogic.Player.White)
                    winnerServicePlayer = localGame.WhitePlayerUserID;
                else if (winner == ChessLogic.Player.Black)
                    winnerServicePlayer = localGame.BlackPlayerUserID;
                else
                    winnerServicePlayer = null;

                // Capture the game reference and null localGame to prevent duplicate processing
                Game capturedGame = localGame;
                localGame = null;

                // Wait 5 seconds before saving result and closing windows (gives players time to see the result)
                DispatcherTimer closeTimer = new DispatcherTimer();
                closeTimer.Interval = TimeSpan.FromSeconds(5);
                closeTimer.Tick += (ts, te) =>
                {
                    closeTimer.Stop();
                    // Save the result to the database and return to the games list
                    SaveResultAndGoBack(winnerServicePlayer, capturedGame);
                };
                closeTimer.Start();
            };

            // GameOver handler for Black's window: mirrors White's GameOver logic
            blackWindow.GameOver += (winner, reason, moves) =>
            {
                // Guard: don't process if the game was already deleted
                if (isGameDeleted) return;
                // Guard: don't process if localGame was already nulled out
                if (localGame == null) return;
                // Cache the move list in memory for display in the detail panel
                gameMoves[localGame.GameID] = moves;

                // Resolve the winner to a service Player object (null = draw)
                Player winnerServicePlayer;
                if (winner == ChessLogic.Player.White)
                    winnerServicePlayer = localGame.WhitePlayerUserID;
                else if (winner == ChessLogic.Player.Black)
                    winnerServicePlayer = localGame.BlackPlayerUserID;
                else
                    winnerServicePlayer = null;

                // Capture the game reference and null localGame to prevent duplicate processing
                Game capturedGame = localGame;
                localGame = null;

                // Wait 5 seconds before saving result and closing windows
                DispatcherTimer closeTimer = new DispatcherTimer();
                closeTimer.Interval = TimeSpan.FromSeconds(5);
                closeTimer.Tick += (ts, te) =>
                {
                    closeTimer.Stop();
                    // Save the result to the database and return to the games list
                    SaveResultAndGoBack(winnerServicePlayer, capturedGame);
                };
                closeTimer.Start();
            };

            // GameExited handler for White's window: closes both windows cleanly after game over menu exit
            whiteWindow.GameExited += () =>
            {
                Dispatcher.Invoke(() =>
                {
                    // Prevent the Closing handlers from prompting for confirmation
                    isClosingHandled = true;
                    // Stop both polling timers
                    whiteWindow.StopPolling();
                    blackWindow.StopPolling();
                    // Close both player windows
                    whitePlayerWindow.Close();
                    blackPlayerWindow.Close();
                });
            };

            // GameExited handler for Black's window: mirrors White's GameExited logic
            blackWindow.GameExited += () =>
            {
                Dispatcher.Invoke(() =>
                {
                    // Prevent the Closing handlers from prompting for confirmation
                    isClosingHandled = true;
                    // Stop both polling timers
                    whiteWindow.StopPolling();
                    blackWindow.StopPolling();
                    // Close both player windows
                    whitePlayerWindow.Close();
                    blackPlayerWindow.Close();
                });
            };

            // Show both player windows side by side
            whitePlayerWindow.Show();
            blackPlayerWindow.Show();

            // Switch back to the games list view and refresh it to show the new game
            NewGameView.Visibility = Visibility.Collapsed;
            GamesListView.Visibility = Visibility.Visible;
            LoadGames();
        }

        // Private method: Saves the game result to the DB, updates player stats, and returns to the list
        private void SaveResultAndGoBack(Player winnerPlayer, Game gameToSave)
        {
            try
            {
                // Set the result on the game object and persist it to the database
                gameToSave.Result = winnerPlayer;
                service.UpdateGameResult(gameToSave);

                // Update stats based on outcome: win/loss or draw
                if (winnerPlayer != null)
                {
                    // Increment the winner's win count
                    service.UpdatePlayerStats(winnerPlayer.Id, true);

                    // Determine the loser: whichever player is not the winner
                    int loserId = winnerPlayer.Id == gameToSave.WhitePlayerUserID.Id
                        ? gameToSave.BlackPlayerUserID.Id
                        : gameToSave.WhitePlayerUserID.Id;

                    // Increment the loser's loss count
                    service.UpdatePlayerStats(loserId, false);
                }
                else
                {
                    // No winner — increment the draw count for both players
                    service.UpdatePlayerDraw(gameToSave.WhitePlayerUserID.Id);
                    service.UpdatePlayerDraw(gameToSave.BlackPlayerUserID.Id);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to save result: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Always return to the games list view on the UI thread, even if saving failed
                Dispatcher.Invoke(() =>
                {
                    // Clear the chess board container and hide the in-game view
                    ChessContainer.Content = null;
                    ChessGameView.Visibility = Visibility.Collapsed;
                    GamesListView.Visibility = Visibility.Visible;
                    // Refresh the games list to include the newly completed game
                    LoadGames();

                    // Re-fetch the finished game from the server to get its updated result
                    Game finishedGame = service.GetGameByID(gameToSave.GameID);
                    if (finishedGame != null)
                    {
                        // Update the currently playing game reference and auto-select it in the list
                        currentlyPlayingGame = finishedGame;
                        SetSelectedGame(finishedGame);
                    }
                    else
                    {
                        // Game could not be retrieved — clear the reference
                        currentlyPlayingGame = null;
                    }
                });
            }
        }

        // Event handler: Prompts for confirmation then deletes the current in-progress game and returns to the list
        private void BtnBackFromGame_Click(object sender, RoutedEventArgs e)
        {
            // Warn the admin that exiting will permanently delete the game and its moves
            System.Windows.MessageBoxResult result = MessageBox.Show(
                "Are you sure you want to exit? The game and its moves will not be saved.",
                "Exit Game", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                // If a game is currently in progress, delete it and its moves from the database
                if (currentlyPlayingGame != null)
                {
                    try
                    {
                        // Delete all move records for the abandoned game
                        service.DeleteMovesByGameID(currentlyPlayingGame.GameID);
                        // Delete the game record itself
                        service.DeleteGame(currentlyPlayingGame);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error deleting game: " + ex.Message);
                    }
                }

                // Clear the chess board container and reset the playing game reference
                ChessContainer.Content = null;
                currentlyPlayingGame = null;
                // Hide the in-game view and return to the games list
                ChessGameView.Visibility = Visibility.Collapsed;
                GamesListView.Visibility = Visibility.Visible;
                // Refresh the games list
                LoadGames();
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