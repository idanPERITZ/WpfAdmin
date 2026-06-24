using ChessLogic;
using ChessUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using WpfAdminPeritz.ServiceReferenceUserChess;

using ChessMoveRecord = ChessUI.MoveRecord;
using ServiceMoveRecord = WpfAdminPeritz.ServiceReferenceUserChess.MoveRecord;
using ServicePlayer = WpfAdminPeritz.ServiceReferenceUserChess.Player;
using ServiceGame = WpfAdminPeritz.ServiceReferenceUserChess.Game;

namespace WpfAdminPeritz
{
    public partial class ChessMainWindowUserControl : UserControl
    {
        public event Action<ChessLogic.Player, string, List<string[]>> GameOver;
        public event Action GameExited;

        private readonly Image[,] pieceImages = new Image[8, 8];
        private readonly Rectangle[,] highlights = new Rectangle[8, 8];
        private readonly Dictionary<Position, Move> moveCache = new Dictionary<Position, Move>();

        private readonly ObservableCollection<ChessMoveRecord> moveHistory =
            new ObservableCollection<ChessMoveRecord>();

        private readonly List<Board> boardSnapshots = new List<Board>();

        // Limit board snapshots to prevent memory issues in long games
        private const int MAX_SNAPSHOT_HISTORY = 50;

        private Position selectedPosition;
        private GameState gameState;

        private Action<string, string, int, string> onMoveSaved;

        // moveIndex tracks the total number of half-moves played so far in the game.
        // It is incremented by both my moves and opponent moves.
        private int moveIndex;

        // lastAppliedMoveIndex is the MoveIndex of the last DB move we applied from the opponent.
        // We use this to avoid re-applying moves we've already processed.
        private int lastAppliedMoveIndex = -1;

        private int viewingSnapshot = -1;

        private bool gameOverFired;
        private bool gameOverMenuShown;
        private bool suppressSelectionChanged;



        private ChessLogic.Player myColor;
        private ServiceGame currentGame;
        private ChessServiceUserClient service;
        private WcfClientHelper<ChessServiceUserClient> serviceHelper;
        private DispatcherTimer pollTimer;

        private ServicePlayer opponentPlayer;
        private ServicePlayer myPlayer;

        public ObservableCollection<ChessMoveRecord> MoveHistory => moveHistory;

        public ChessMainWindowUserControl()
        {
            InitializeComponent();
            DataContext = this;
            InitializeBoard();
            gameState = new GameState(ChessLogic.Player.White, Board.Initial());
            DrawBoard(gameState.Board);
            SetCursor(gameState.CurrentPlayer);
        }

        public void SetPlayerColor(
            ChessLogic.Player color,
            ServiceGame game,
            ChessServiceUserClient svc,
            string opponentName,
            ServicePlayer me,
            ServicePlayer opponent)
        {
            myColor = color;
            currentGame = game;
            service = svc;
            myPlayer = me;
            opponentPlayer = opponent;

            // Initialize WCF client helper with factory to recreate service
            serviceHelper = new WcfClientHelper<ChessServiceUserClient>(() => service);

            OpponentNameText.Text = "Playing against: " + opponentName;

            // Set opponent statistics
            TxtOpponentWins.Text = opponent.Wins.ToString();
            TxtOpponentDraws.Text = opponent.Draws.ToString();
            TxtOpponentLosses.Text = opponent.Losses.ToString();

            if (myColor == ChessLogic.Player.Black)
            {
                BoardGrid.LayoutTransform = new ScaleTransform(1, -1);
                for (int row = 0; row < 8; row++)
                    for (int col = 0; col < 8; col++)
                        pieceImages[row, col].LayoutTransform = new ScaleTransform(1, -1);
            }

            pollTimer = new DispatcherTimer();
            pollTimer.Interval = TimeSpan.FromSeconds(1);
            pollTimer.Tick += PollForOpponentMove;
            pollTimer.Start();

            Loaded -= OnLoadedHookKeyboard;
            Loaded += OnLoadedHookKeyboard;
        }

        public void SetMoveCallback(Action<string, string, int, string> callback)
        {
            onMoveSaved = callback;
        }

        public void StopPolling()
        {
            try
            {
                if (pollTimer != null)
                {
                    pollTimer.Stop();
                    pollTimer.Tick -= PollForOpponentMove;
                    pollTimer = null;
                }
                UnhookKeyboard();
            }
            catch (Exception ex)
            {
                // Don't let StopPolling failure cause a crash
            }
        }

        private void OnLoadedHookKeyboard(object sender, RoutedEventArgs e)
        {
            Window parentWindow = Window.GetWindow(this);
            if (parentWindow == null) return;
            parentWindow.KeyDown -= ChessWindow_KeyDown;
            parentWindow.KeyDown += ChessWindow_KeyDown;
        }

        private void UnhookKeyboard()
        {
            Window parentWindow = Window.GetWindow(this);
            if (parentWindow != null)
                parentWindow.KeyDown -= ChessWindow_KeyDown;
        }

        private void PollForOpponentMove(object sender, EventArgs e)
        {
            // If game is over or not started yet, stop polling
            if (gameState == null || gameState.IsGameOver() || currentGame == null)
            {
                StopPolling();
                return;
            }

            // Only poll when it is the opponent's turn
            if (gameState.CurrentPlayer == myColor)
                return;

            // Run the network call off the UI thread to avoid blocking the UI.
            System.Threading.Tasks.Task.Run(() =>
            {
                // Periodically check service health (every 20 moves or so) - do this in background thread
                if (moveIndex > 0 && moveIndex % 20 == 0)
                {
                    try
                    {
                        CallbackServiceManager.Instance.EnsureChannelHealth();
                    }
                    catch (Exception ex)
                    {
                        // Ignore channel health check failures
                    }
                }

                try
                {
                    // Check if service is still available
                    if (serviceHelper == null || currentGame == null)
                        return;

                    // Use the helper to execute the service call with automatic recovery
                    MoveList moves = serviceHelper.Execute(
                        svc => svc.GetMovesByGameID(currentGame),
                        defaultValue: null
                    );

                    if (moves == null || moves.Count == 0)
                        return;

                    var pending = moves
                        .Cast<ServiceMoveRecord>()
                        .Where(m => m.MoveIndex > lastAppliedMoveIndex)
                        .OrderBy(m => m.MoveIndex)
                        .ToList();

                    if (pending.Count == 0) return;

                    // Marshal back to UI thread to apply the moves.
                    // Use Invoke with priority to ensure we can check if control still exists
                    try
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            // Double-check game isn't over before processing
                            if (gameState != null && !gameState.IsGameOver())
                            {
                                ProcessPendingDbMoves(pending);
                            }
                        }));
                    }
                    catch
                    {
                        // Control was disposed or dispatcher shut down, ignore
                    }
                }
                catch (System.ServiceModel.CommunicationException commEx)
                {
                    try
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            StopPolling();
                            if (gameState != null && !gameState.IsGameOver())
                            {
                                MessageBox.Show(
                                    "Network error occurred. The game connection has been closed.",
                                    "Connection Error",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                            }
                        }));
                    }
                    catch { }
                }
                catch (System.TimeoutException timeoutEx)
                {
                    // Just skip this iteration, don't stop the game
                }
                catch (Exception ex)
                {
                    try
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            if (pollTimer != null)
                                pollTimer.Stop();

                            // Only show error if not already game over
                            if (gameState != null && !gameState.IsGameOver())
                            {
                                MessageBox.Show(
                                    "Failed to receive opponent move: " + ex.Message,
                                    "Game Error",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                            }
                        }));
                    }
                    catch
                    {
                        // Control was disposed or dispatcher shut down, ignore
                    }
                }
            });
        }

        private void ProcessPendingDbMoves(List<ServiceMoveRecord> pending)
        {
            try
            {
                foreach (var dbMove in pending)
                {
                    // Try to apply this DB move. Prefer exact coordinate match when available.
                    bool applied = false;

                    string coordinates = dbMove.To;
                    if (!string.IsNullOrEmpty(coordinates) && coordinates.Length == 4)
                    {
                        int fromColumn = coordinates[0] - 'a';
                        int fromRow = 8 - (coordinates[1] - '0');
                        int toColumn = coordinates[2] - 'a';
                        int toRow = 8 - (coordinates[3] - '0');

                        Position fromPos = new Position(fromRow, fromColumn);
                        Position toPos = new Position(toRow, toColumn);

                        foreach (Move move in gameState.AllLegalMovesFor(gameState.CurrentPlayer))
                        {
                            if (move.FromPosition.Row == fromPos.Row &&
                                move.FromPosition.Column == fromPos.Column &&
                                move.ToPosition.Row == toPos.Row &&
                                move.ToPosition.Column == toPos.Column)
                            {
                                // For promotions, we must also match the promoted piece type from the SAN notation
                                if (move.Type == MoveType.PawnPromotion && !string.IsNullOrEmpty(dbMove.From))
                                {
                                    // Extract promotion piece from SAN notation (e.g., "e8=Q", "d1=N")
                                    PawnPromotion promotion = move as PawnPromotion;
                                    string expectedSAN = CreateSAN(move);
                                    if (expectedSAN != dbMove.From)
                                    {
                                        // This promotion has the wrong piece type, skip it
                                        continue;
                                    }
                                }

                                ApplyOpponentMove(move, dbMove.From, dbMove.MoveIndex);
                                applied = true;
                                break;
                            }
                        }
                    }

                    if (applied) continue;

                    // Fallback to SAN match
                    foreach (Move move in gameState.AllLegalMovesFor(gameState.CurrentPlayer))
                    {
                        if (CreateSAN(move) == dbMove.From)
                        {
                            ApplyOpponentMove(move, dbMove.From, dbMove.MoveIndex);
                            applied = true;
                            break;
                        }
                    }

                    // If we couldn't apply a move, stop processing further to avoid corrupting state.
                    if (!applied)
                    {
                        // Log and stop; the next poll may succeed once DB or local state aligns.
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error processing opponent moves: " + ex.Message,
                    "Move Processing Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyOpponentMove(Move move, string notation, int dbMoveIndex)
        {
            try
            {
                // Safety check: don't apply moves if game is already over
                if (gameState == null)
                {
                    return;
                }

                if (gameState.IsGameOver())
                {
                    return;
                }

                bool wasWhiteMove = gameState.CurrentPlayer == ChessLogic.Player.White;

                try
                {
                    gameState.MakeMove(move);
                }
                catch (Exception ex)
                {
                    // Surface the exception and prevent the app from crashing so we can
                    // diagnose the underlying issue causing crashes after captures.
                    MessageBox.Show("Error while applying opponent move: " + ex.Message + "\n" + ex.StackTrace,
                        "Move Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                moveIndex++;
                // Remember that we've applied this DB move so we don't apply it again.
                lastAppliedMoveIndex = dbMoveIndex;

                AddBoardSnapshot(gameState.Board);

                AddMoveToHistory(notation, wasWhiteMove);

                DrawBoard(gameState.Board);

                SetCursor(gameState.CurrentPlayer);

                if (gameState.IsGameOver())
                {
                    // Stop polling immediately when game ends to prevent race conditions
                    StopPolling();
                    ShowGameOverMenu();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Critical error applying opponent move: " + ex.Message,
                    "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InitializeBoard()
        {
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    Image image = new Image();
                    pieceImages[row, col] = image;
                    PieceGrid.Children.Add(image);

                    Rectangle highlight = new Rectangle();
                    highlights[row, col] = highlight;
                    HighlightGrid.Children.Add(highlight);
                }
            }
        }

        private void DrawBoard(Board board)
        {
            try
            {
                if (board == null)
                {
                    return;
                }

                for (int row = 0; row < 8; row++)
                {
                    for (int col = 0; col < 8; col++)
                    {
                        if (pieceImages[row, col] == null)
                        {
                            continue;
                        }

                        pieceImages[row, col].Source = Images.GetImage(board[row, col]);
                        if (myColor == ChessLogic.Player.Black)
                            pieceImages[row, col].LayoutTransform = new ScaleTransform(1, -1);
                    }
                }
            }
            catch (Exception ex)
            {
                // Don't crash the game if drawing fails
            }
        }

        private void BoardGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (IsMenuOnScreen()) return;
            if (viewingSnapshot != -1) return;
            if (gameState.CurrentPlayer != myColor) return;

            Point point = e.GetPosition(BoardGrid);
            Position position = ToSquarePosition(point);

            if (selectedPosition != null &&
                position.Row == selectedPosition.Row &&
                position.Column == selectedPosition.Column)
            {
                selectedPosition = null;
                HideHighlights();
                return;
            }

            if (!gameState.Board.IsEmpty(position) &&
                gameState.Board[position].Color == gameState.CurrentPlayer)
            {
                selectedPosition = null;
                HideHighlights();
                OnFromPositionSelected(position);
                return;
            }

            if (selectedPosition == null)
                OnFromPositionSelected(position);
            else
                OnToPositionSelected(position);
        }

        private Position ToSquarePosition(Point point)
        {
            double squareSize = BoardGrid.ActualWidth / 8;
            int row = (int)(point.Y / squareSize);
            int column = (int)(point.X / squareSize);
            return new Position(row, column);
        }

        private void OnFromPositionSelected(Position position)
        {
            IEnumerable<Move> moves = gameState.LegalMovesForPiece(position);
            if (!moves.Any()) return;
            selectedPosition = position;
            CacheMoves(moves);
            ShowHighlightMoves();
        }

        private void OnToPositionSelected(Position position)
        {
            selectedPosition = null;
            HideHighlights();

            Move move;
            if (!moveCache.TryGetValue(position, out move)) return;

            if (move.Type == MoveType.PawnPromotion)
                HandlePromotion(move.FromPosition, move.ToPosition);
            else
                HandleMove(move);
        }

        private void HandlePromotion(Position from, Position to)
        {
            pieceImages[to.Row, to.Column].Source =
                Images.GetImage(gameState.CurrentPlayer, PieceType.Pawn);
            pieceImages[from.Row, from.Column].Source = null;

            PromotionMenu promotionMenu = new PromotionMenu(gameState.CurrentPlayer);
            MenuContainer.Content = promotionMenu;

            // Flip the promotion menu if playing as Black (same as board and pieces)
            if (myColor == ChessLogic.Player.Black)
            {
                promotionMenu.LayoutTransform = new ScaleTransform(1, -1);
            }

            promotionMenu.PieceSelected += pieceType =>
            {
                MenuContainer.Content = null;
                Move promotionMove = new PawnPromotion(from, to, pieceType);
                HandleMove(promotionMove);
            };
        }

        private void HandleMove(Move move)
        {
            try
            {
                if (gameState == null)
                {
                    return;
                }

                bool wasWhiteMove = gameState.CurrentPlayer == ChessLogic.Player.White;
                string notation = CreateSAN(move);

                // Save to DB BEFORE making the move on the local board.
                // We pass moveIndex (current half-move count) to the callback.
                SaveMoveToDb(move, notation);

                try
                {
                    gameState.MakeMove(move);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error while making move: " + ex.Message + "\n" + ex.StackTrace,
                        "Move Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    // Revert UI changes we optimistically applied
                    DrawBoard(gameState.Board);
                    return;
                }

                AddBoardSnapshot(gameState.Board);
                moveIndex++;

                AddMoveToHistory(notation, wasWhiteMove);
                DrawBoard(gameState.Board);
                SetCursor(gameState.CurrentPlayer);

                if (gameState.IsGameOver())
                {
                    // Stop polling immediately when game ends to prevent race conditions
                    StopPolling();
                    ShowGameOverMenu();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Critical error in HandleMove: " + ex.Message,
                    "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveMoveToDb(Move move, string notation)
        {
            if (onMoveSaved == null) return;

            string fromString =
                ((char)('a' + move.FromPosition.Column)).ToString() +
                (8 - move.FromPosition.Row).ToString();

            string toString =
                ((char)('a' + move.ToPosition.Column)).ToString() +
                (8 - move.ToPosition.Row).ToString();

            // moveIndex is the current half-move index (0-based) for THIS move.
            // We pass it directly; the server stores it so the opponent can find it.
            // Run the network save on a background thread so the UI isn't blocked.
            try
            {
                var capturedFrom = fromString;
                var capturedTo = toString;
                var capturedIndex = moveIndex;
                var capturedNotation = notation;

                System.Threading.Tasks.Task.Run(() =>
                {
                    int retries = 0;
                    const int maxRetries = 3;
                    bool success = false;

                    while (!success && retries < maxRetries)
                    {
                        try
                        {
                            onMoveSaved(capturedFrom, capturedTo, capturedIndex, capturedNotation);
                            success = true;
                        }
                        catch (Exception ex)
                        {
                            retries++;
                            System.Diagnostics.Debug.WriteLine($"Move save attempt {retries} failed: {ex.Message}");

                            if (retries < maxRetries)
                            {
                                // Wait before retrying (exponential backoff)
                                System.Threading.Thread.Sleep(500 * retries);
                            }
                            else
                            {
                                // Final failure - log but don't crash
                                System.Diagnostics.Debug.WriteLine($"Failed to save move after {maxRetries} attempts: {ex.Message}");
                                Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    // Only show error if game is still active
                                    if (gameState != null && !gameState.IsGameOver())
                                    {
                                        MessageBox.Show(
                                            "Warning: Failed to save move to server. The game may desync.",
                                            "Network Warning",
                                            MessageBoxButton.OK,
                                            MessageBoxImage.Warning);
                                    }
                                }));
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                // Log but don't block the UI.
                System.Diagnostics.Debug.WriteLine("Error queuing move save: " + ex.Message);
            }

            // Mark this index as already known so our own poll skips it
            lastAppliedMoveIndex = moveIndex;
        }

        // Add a board snapshot with memory management
        private void AddBoardSnapshot(Board board)
        {
            try
            {
                if (board == null)
                {
                    System.Diagnostics.Debug.WriteLine("AddBoardSnapshot: board is null!");
                    return;
                }

                boardSnapshots.Add(board.Copy());

                // Keep only the most recent snapshots to prevent memory issues in long games
                // Keep some history for move navigation, but not unlimited
                if (boardSnapshots.Count > MAX_SNAPSHOT_HISTORY)
                {
                    // Remove the oldest snapshots, keeping the most recent MAX_SNAPSHOT_HISTORY
                    int toRemove = boardSnapshots.Count - MAX_SNAPSHOT_HISTORY;
                    boardSnapshots.RemoveRange(0, toRemove);

                    // Adjust viewingSnapshot index if needed
                    if (viewingSnapshot >= 0)
                    {
                        viewingSnapshot = Math.Max(0, viewingSnapshot - toRemove);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AddBoardSnapshot failed: {ex.Message}");
                // Don't crash the game if snapshot fails
            }
        }

        private void CacheMoves(IEnumerable<Move> moves)
        {
            moveCache.Clear();
            foreach (Move move in moves)
                moveCache[move.ToPosition] = move;
        }

        private void ShowHighlightMoves()
        {
            SolidColorBrush brush = new SolidColorBrush(Color.FromArgb(128, 100, 255, 100));
            foreach (Position destination in moveCache.Keys)
                highlights[destination.Row, destination.Column].Fill = brush;
        }

        private void HideHighlights()
        {
            foreach (Position destination in moveCache.Keys)
                highlights[destination.Row, destination.Column].Fill = Brushes.Transparent;
        }

        private void SetCursor(ChessLogic.Player player)
        {
            Cursor = player == ChessLogic.Player.White
                ? ChessCursors.WhiteCursor
                : ChessCursors.BlackCursor;
        }

        private bool IsMenuOnScreen() => MenuContainer.Content != null;

        private void ShowGameOverMenu()
        {
            if (gameOverMenuShown) return;
            gameOverMenuShown = true;
            StopPolling();

            Result result = gameState.Result;
            List<string[]> moves = new List<string[]>();

            foreach (ChessMoveRecord record in moveHistory)
                moves.Add(new string[] { record.WhiteMove, record.BlackMove });

            if (!gameOverFired)
            {
                gameOverFired = true;
                GameOver?.Invoke(result.Winner, result.Reason.ToString(), moves);
            }

            GameOverMenu gameOverMenu = new GameOverMenu(gameState);

            if (myColor == ChessLogic.Player.Black)
                gameOverMenu.LayoutTransform = new ScaleTransform(1, -1);

            MenuContainer.Content = gameOverMenu;

            gameOverMenu.OnOptionSelected += option =>
            {
                MenuContainer.Content = null;
                GameExited?.Invoke();
            };
        }

        private void AddMoveToHistory(string notation, bool wasWhite)
        {
            if (wasWhite)
            {
                moveHistory.Add(new ChessMoveRecord
                {
                    MoveNumber = moveHistory.Count + 1,
                    WhiteMove = notation
                });
            }
            else
            {
                if (moveHistory.Count == 0)
                {
                    moveHistory.Add(new ChessMoveRecord
                    {
                        MoveNumber = 1,
                        WhiteMove = "",
                        BlackMove = notation
                    });
                }
                else
                {
                    moveHistory[moveHistory.Count - 1].BlackMove = notation;
                }
            }
        }

        private string CreateSAN(Move move)
        {
            try
            {
                if (move == null)
                    return "??";

                if (move.Type == MoveType.CastleKingside) return "O-O";
                if (move.Type == MoveType.CastleQueenside) return "O-O-O";

                StringBuilder builder = new StringBuilder();

                Piece movingPiece = gameState.Board[move.FromPosition];
                if (movingPiece == null)
                {
                    System.Diagnostics.Debug.WriteLine("CreateSAN: Moving piece is null");
                    return "??";
                }

                builder.Append(PieceToChar(movingPiece.Type));

                bool isCapture = !gameState.Board.IsEmpty(move.ToPosition) || move.Type == MoveType.EnPassant;

                if (movingPiece.Type == PieceType.Pawn && isCapture)
                    builder.Append((char)('a' + move.FromPosition.Column));

                if (isCapture) builder.Append('x');

                builder.Append((char)('a' + move.ToPosition.Column));
                builder.Append(8 - move.ToPosition.Row);

                if (move.Type == MoveType.PawnPromotion)
                {
                    PawnPromotion promotion = move as PawnPromotion;
                    if (promotion != null)
                    {
                        builder.Append('=');
                        builder.Append(PieceToChar(promotion.NewType));
                    }
                }

                // Create a copy to check for check/checkmate without modifying the main game state
                GameState copy = null;
                try
                {
                    copy = new GameState(gameState.CurrentPlayer, gameState.Board.Copy());
                    copy.MakeMove(move);

                    if (copy.IsGameOver()) builder.Append('#');
                    else if (copy.Board.IsInCheck(copy.CurrentPlayer)) builder.Append('+');
                }
                catch (Exception ex)
                {
                    // If we can't determine check/checkmate, just log and continue without the symbol
                    System.Diagnostics.Debug.WriteLine($"CreateSAN: Error checking for check/mate: {ex.Message}");
                }

                return builder.ToString();
            }
            catch (Exception ex)
            {
                // If anything fails in SAN generation, return a fallback notation
                System.Diagnostics.Debug.WriteLine($"CreateSAN failed: {ex.Message}");
                MessageBox.Show("Error generating move notation: " + ex.Message + "\nThe game will continue but move history may be incomplete.",
                    "Notation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return "??";
            }
        }

        private void MoveHistoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (suppressSelectionChanged) return;
            if (MoveHistoryList.SelectedIndex < 0) return;
            if (boardSnapshots.Count == 0) return;
            if (gameState.IsGameOver()) return;

            int rowIndex = MoveHistoryList.SelectedIndex;
            int blackSnapshotIndex = (rowIndex * 2) + 1;
            int whiteSnapshotIndex = rowIndex * 2;

            int snapshotIndex = blackSnapshotIndex < boardSnapshots.Count
                ? blackSnapshotIndex
                : whiteSnapshotIndex;

            viewingSnapshot = snapshotIndex;
            DrawBoard(boardSnapshots[snapshotIndex]);
        }

        private void ChessWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.IsRepeat) return;

            if (e.Key == Key.Left)
            {
                if (viewingSnapshot == -1)
                {
                    if (boardSnapshots.Count == 0) return;
                    viewingSnapshot = boardSnapshots.Count - 1;
                }
                else if (viewingSnapshot == 0)
                {
                    viewingSnapshot = -2;
                    suppressSelectionChanged = true;
                    MoveHistoryList.SelectedIndex = -1;
                    suppressSelectionChanged = false;
                    DrawBoard(Board.Initial());
                    return;
                }
                else if (viewingSnapshot == -2) return;
                else viewingSnapshot--;
            }
            else if (e.Key == Key.Right)
            {
                if (viewingSnapshot == -2) viewingSnapshot = 0;
                else if (viewingSnapshot == -1) return;
                else
                {
                    viewingSnapshot++;
                    if (viewingSnapshot >= boardSnapshots.Count)
                    {
                        viewingSnapshot = -1;
                        suppressSelectionChanged = true;
                        MoveHistoryList.SelectedIndex = -1;
                        suppressSelectionChanged = false;
                        DrawBoard(gameState.Board);
                        return;
                    }
                }
            }
            else return;

            suppressSelectionChanged = true;
            MoveHistoryList.SelectedIndex = viewingSnapshot / 2;
            suppressSelectionChanged = false;
            DrawBoard(boardSnapshots[viewingSnapshot]);
        }

        private void BtnFirst_Click(object sender, RoutedEventArgs e)
        {
            if (boardSnapshots.Count == 0) return;
            viewingSnapshot = -2;
            suppressSelectionChanged = true;
            MoveHistoryList.SelectedIndex = -1;
            suppressSelectionChanged = false;
            DrawBoard(Board.Initial());
        }

        private void BtnPrev_Click(object sender, RoutedEventArgs e)
        {
            KeyEventArgs args = new KeyEventArgs(
                Keyboard.PrimaryDevice,
                PresentationSource.FromVisual(this),
                0, Key.Left);
            args.RoutedEvent = KeyDownEvent;
            ChessWindow_KeyDown(sender, args);
        }

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            KeyEventArgs args = new KeyEventArgs(
                Keyboard.PrimaryDevice,
                PresentationSource.FromVisual(this),
                0, Key.Right);
            args.RoutedEvent = KeyDownEvent;
            ChessWindow_KeyDown(sender, args);
        }

        private void BtnLast_Click(object sender, RoutedEventArgs e)
        {
            if (boardSnapshots.Count == 0) return;
            viewingSnapshot = -1;
            suppressSelectionChanged = true;
            MoveHistoryList.SelectedIndex = -1;
            suppressSelectionChanged = false;
            DrawBoard(gameState.Board);
        }

        private string PieceToChar(PieceType type)
        {
            switch (type)
            {
                case PieceType.King: return "K";
                case PieceType.Queen: return "Q";
                case PieceType.Rook: return "R";
                case PieceType.Bishop: return "B";
                case PieceType.Knight: return "N";
                default: return "";
            }
        }
    }
}
