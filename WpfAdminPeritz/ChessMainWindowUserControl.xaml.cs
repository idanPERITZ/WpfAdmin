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
using WpfAdminPeritz.ServiceReferenceChess;

// Alias to distinguish between ChessUI.MoveRecord and ServiceReferenceChess.MoveRecord
using ChessMoveRecord = ChessUI.MoveRecord;
// Alias to distinguish between ServiceReferenceChess.MoveRecord and ChessUI.MoveRecord
using ServiceMoveRecord = WpfAdminPeritz.ServiceReferenceChess.MoveRecord;
// Alias to distinguish between ServiceReferenceChess.Player and ChessLogic.Player
using ServicePlayer = WpfAdminPeritz.ServiceReferenceChess.Player;

namespace WpfAdminPeritz
{
    // UserControl that contains the main chess game UI and all game logic for an online match
    // Handles board rendering, move input, opponent polling, move history, and game over
    public partial class ChessMainWindowUserControl : UserControl
    {
        // Event: Fired when the game ends, passes the winner, reason, and full move list
        public event Action<ChessLogic.Player, string, List<string[]>> GameOver;
        // Event: Fired when the player exits the game after it ends
        public event Action GameExited;

        // Field: 2D array of Image controls representing pieces on each square
        private readonly Image[,] pieceImages = new Image[8, 8];
        // Field: 2D array of Rectangle controls used to highlight valid move squares
        private readonly Rectangle[,] highlights = new Rectangle[8, 8];
        // Field: Maps destination positions to their corresponding Move objects for the selected piece
        private readonly Dictionary<Position, Move> moveCache = new Dictionary<Position, Move>();
        // Field: The currently selected board position (null if no piece is selected)
        private Position selectedPosition = null;
        // Field: The current state of the chess game (board, current player, result)
        private GameState gameState;
        // Field: Observable collection of move records displayed in the move history list
        private readonly ObservableCollection<ChessMoveRecord> moveHistory = new ObservableCollection<ChessMoveRecord>();
        // Property: Public read-only access to the move history collection (used for data binding)
        public ObservableCollection<ChessMoveRecord> MoveHistory => moveHistory;

        // Field: Callback invoked when a move needs to be saved to the database
        private Action<string, string, int, string> onMoveSaved = null;
        // Field: Tracks the sequential index of the next move to be saved
        private int moveIndex = 0;

        // Field: The color assigned to the local player (White or Black)
        private ChessLogic.Player myColor;
        // Field: The ID of the current game in the database
        private int gameID;
        // Field: WCF service client used to communicate with the chess server
        private ChessServiceAdminClient service;
        // Field: Timer that periodically polls the server for the opponent's move
        private DispatcherTimer pollTimer;
        // Field: The MoveIndex of the last move retrieved from the server (prevents reprocessing)
        private int lastKnownMoveIndex = -1;
        // Field: Prevents the GameOver event from being fired more than once
        private bool gameOverFired = false;
        // Field: Prevents the game over menu from being shown more than once
        private bool gameOverMenuShown = false;
        // Field: Stores a copy of the board after each move for history navigation
        private readonly List<Board> boardSnapshots = new List<Board>();
        // Field: Index into boardSnapshots currently being viewed (-1 = live board, -2 = initial position)
        private int viewingSnapshot = -1;
        // Field: Suppresses the MoveHistoryList SelectionChanged handler during programmatic selection changes
        private bool suppressSelectionChanged = false;

        // Field: The service player object representing the opponent (used for stats popup)
        private ServicePlayer opponentPlayer;
        // Field: The service player object representing the local player (used for stats popup)
        private ServicePlayer myPlayer;

        // Constructor: Initializes the control, sets up the board UI, and starts with White to move
        public ChessMainWindowUserControl()
        {
            // Initialize WPF components
            InitializeComponent();
            // Set DataContext to self for move history data binding
            DataContext = this;
            // Create Image and Rectangle controls for all 64 squares
            InitializeBoard();
            // Create a new game starting with White to move
            gameState = new GameState(ChessLogic.Player.White, Board.Initial());
            // Render all pieces in their starting positions
            DrawBoard(gameState.Board);
            // Set the mouse cursor to match the current player's color
            SetCursor(gameState.CurrentPlayer);
        }

        // Public method: Configures the control for an online game with a specific color and opponent
        public void SetPlayerColor(ChessLogic.Player color, int gameId,
            ChessServiceAdminClient svc, string opponentName, ServicePlayer me, ServicePlayer opponent)
        {
            // Store the local player's assigned color
            myColor = color;
            // Store the game ID for server communication
            gameID = gameId;
            // Store the service client reference
            service = svc;
            // Store the local player's service object
            myPlayer = me;
            // Store the opponent's service object
            opponentPlayer = opponent;

            // Display the opponent's name in the UI label
            OpponentNameText.Content = "Playing against: " + opponentName;

            // If the local player is Black, flip the board and all piece images so Black is at the bottom
            if (myColor == ChessLogic.Player.Black)
            {
                // Flip the entire board grid vertically
                BoardGrid.LayoutTransform = new ScaleTransform(1, -1);
                // Flip each piece image individually to keep them right-side up
                for (int row = 0; row < 8; row++)
                    for (int col = 0; col < 8; col++)
                        pieceImages[row, col].LayoutTransform = new ScaleTransform(1, -1);
            }

            // Create a timer to poll for the opponent's move every second
            pollTimer = new DispatcherTimer();
            pollTimer.Interval = TimeSpan.FromSeconds(1);
            pollTimer.Tick += PollForOpponentMove;
            pollTimer.Start();

            // FIX: unsubscribe before subscribing to prevent duplicate handlers
            this.Loaded -= OnLoadedHookKeyboard;
            this.Loaded += OnLoadedHookKeyboard;
        }

        // Event handler: Hooks keyboard navigation to the parent window once the control is loaded
        private void OnLoadedHookKeyboard(object sender, RoutedEventArgs e)
        {
            // Get the parent window that contains this UserControl
            Window parentWindow = Window.GetWindow(this);
            if (parentWindow != null)
            {
                // Unsubscribe first to avoid duplicate handlers if called multiple times
                parentWindow.KeyDown -= ChessWindow_KeyDown;
                // Subscribe to the parent window's KeyDown event for arrow key navigation
                parentWindow.KeyDown += ChessWindow_KeyDown;
            }
        }

        // Event handler: Opens the opponent stats popup when the opponent name label is clicked
        private void OpponentName_Click(object sender, RoutedEventArgs e)
        {
            // Guard: both player objects must be available
            if (opponentPlayer == null || myPlayer == null) return;
            // Create and show the stats popup dialog
            OpponentStatsPopup popup = new OpponentStatsPopup(service, opponentPlayer, myPlayer, myColor, gameID);
            popup.ShowDialog();
        }

        // Private method: Removes the keyboard handler from the parent window
        private void UnhookKeyboard()
        {
            // Get the parent window
            Window parentWindow = Window.GetWindow(this);
            if (parentWindow != null)
                // Unsubscribe the keyboard handler
                parentWindow.KeyDown -= ChessWindow_KeyDown;
        }

        // Public method: Registers the callback that saves each move to the database
        public void SetMoveCallback(Action<string, string, int, string> callback)
        {
            onMoveSaved = callback;
        }

        // Public method: Stops the polling timer and removes the keyboard hook (called on game exit)
        public void StopPolling()
        {
            // Stop the opponent move polling timer if it exists
            pollTimer?.Stop();
            // Remove the keyboard handler from the parent window
            UnhookKeyboard();
        }

        // Timer callback: Polls the server for the opponent's latest move and applies it if new
        private void PollForOpponentMove(object sender, EventArgs e)
        {
            // Don't poll if the game is already over
            if (gameState.IsGameOver()) return;

            try
            {
                // Fetch all moves for this game from the server
                MoveList moves = service.GetMovesByGameID(gameID);
                // If no moves exist yet, nothing to do
                if (moves == null || moves.Count == 0) return;

                // Get the total number of moves and the most recent one
                int totalMoves = moves.Count;
                ServiceMoveRecord lastMove = moves[totalMoves - 1];

                // Skip if this move has already been processed
                if (lastMove.MoveIndex <= lastKnownMoveIndex) return;
                // Skip if it's currently our turn (we don't need to apply our own move)
                if (gameState.CurrentPlayer == myColor) return;

                // Mark this move as processed
                lastKnownMoveIndex = lastMove.MoveIndex;

                // The 'To' field stores coords as a 4-char string: "e2e4" (fromFile fromRank toFile toRank)
                string coords = lastMove.To;
                if (coords != null && coords.Length == 4)
                {
                    // Parse from-column from file letter ('a'=0, 'b'=1, ...)
                    int fromCol = coords[0] - 'a';
                    // Parse from-row from rank digit (rank 1 = row 7, rank 8 = row 0)
                    int fromRow = 8 - (coords[1] - '0');
                    // Parse to-column from file letter
                    int toCol = coords[2] - 'a';
                    // Parse to-row from rank digit
                    int toRow = 8 - (coords[3] - '0');

                    // Build Position objects for from and to squares
                    Position fromPos = new Position(fromRow, fromCol);
                    Position toPos = new Position(toRow, toCol);

                    // Try to match the received move against current player's legal moves
                    IEnumerable<Move> currentPlayerMoves = gameState.AllLegalMovesFor(gameState.CurrentPlayer);
                    foreach (Move move in currentPlayerMoves)
                    {
                        // If from and to positions match, apply this move
                        if (move.FromPosition.Row == fromPos.Row &&
                            move.FromPosition.Column == fromPos.Column &&
                            move.ToPosition.Row == toPos.Row &&
                            move.ToPosition.Column == toPos.Column)
                        {
                            ApplyOpponentMove(move, lastMove.From);
                            return;
                        }
                    }

                    // Fallback: determine the opponent's color explicitly
                    ChessLogic.Player opponent = gameState.CurrentPlayer == ChessLogic.Player.White
                        ? ChessLogic.Player.Black
                        : ChessLogic.Player.White;

                    // Try matching against the opponent's legal moves as a secondary fallback
                    IEnumerable<Move> opponentMoves = gameState.AllLegalMovesFor(opponent);
                    foreach (Move move in opponentMoves)
                    {
                        // If from and to positions match, apply this move
                        if (move.FromPosition.Row == fromPos.Row &&
                            move.FromPosition.Column == fromPos.Column &&
                            move.ToPosition.Row == toPos.Row &&
                            move.ToPosition.Column == toPos.Column)
                        {
                            ApplyOpponentMove(move, lastMove.From);
                            return;
                        }
                    }
                }

                // Last resort fallback: match by SAN notation stored in the 'From' field
                IEnumerable<Move> sanFallbackMoves = gameState.AllLegalMovesFor(gameState.CurrentPlayer);
                foreach (Move move in sanFallbackMoves)
                {
                    // Generate SAN notation for this move and compare to the stored notation
                    string notation = CreateSAN(move);
                    if (notation == lastMove.From)
                    {
                        ApplyOpponentMove(move, notation);
                        return;
                    }
                }
            }
            // Silently swallow all exceptions to prevent the timer from crashing on network errors
            catch (Exception) { }
        }

        // Private method: Applies the opponent's move to the game state and updates the UI
        private void ApplyOpponentMove(Move move, string notation)
        {
            // Record whether it was White's move before applying it
            bool wasWhiteMove = gameState.CurrentPlayer == ChessLogic.Player.White;
            // Apply the move to the game state
            gameState.MakeMove(move);
            // Save a copy of the board for history navigation
            boardSnapshots.Add(gameState.Board.Copy());
            // Add the move notation to the move history list
            AddMoveToHistory(notation, wasWhiteMove);
            // Re-render the board with the new piece positions
            DrawBoard(gameState.Board);
            // Update the cursor to reflect the new current player
            SetCursor(gameState.CurrentPlayer);
            // Show the game over menu if the game has ended
            if (gameState.IsGameOver())
                ShowGameOverMenu();
        }

        // Private method: Creates the Image and Rectangle controls for all 64 board squares
        private void InitializeBoard()
        {
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    // Create an Image control for displaying the piece sprite
                    Image image = new Image();
                    // Store it in the 2D array at the correct square
                    pieceImages[row, col] = image;
                    // Add it to the PieceGrid for rendering
                    PieceGrid.Children.Add(image);

                    // Create a Rectangle control for highlighting valid move targets
                    Rectangle highlight = new Rectangle();
                    // Store it in the 2D array at the correct square
                    highlights[row, col] = highlight;
                    // Add it to the HighlightGrid for rendering
                    HighlightGrid.Children.Add(highlight);
                }
            }
        }

        // Private method: Updates all piece image sources to match the current board state
        private void DrawBoard(Board board)
        {
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    // Set the image source to the correct piece sprite (or null if empty)
                    pieceImages[row, col].Source = Images.GetImage(board[row, col]);
                    // If playing as Black, flip each piece image to keep it upright on the flipped board
                    if (myColor == ChessLogic.Player.Black)
                        pieceImages[row, col].LayoutTransform = new ScaleTransform(1, -1);
                }
            }
        }

        // Event handler: Handles mouse clicks on the board grid to select pieces and execute moves
        private void BoardGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Ignore clicks if a menu (promotion or game over) is currently displayed
            if (IsMenuOnScreen()) return;
            // Ignore clicks if we're viewing a historical board snapshot
            if (viewingSnapshot != -1) return;
            // Ignore clicks if it's not the local player's turn
            if (gameState.CurrentPlayer != myColor) return;

            // Convert the click point to a board position
            Point point = e.GetPosition(BoardGrid);
            Position position = ToSquarePosition(point);

            // If the clicked square is the already-selected square, deselect it
            if (selectedPosition != null &&
                position.Row == selectedPosition.Row &&
                position.Column == selectedPosition.Column)
            {
                selectedPosition = null;
                HideHighlights();
                return;
            }

            // If the clicked square has a friendly piece, switch selection to that piece
            if (!gameState.Board.IsEmpty(position) && gameState.Board[position].Color == gameState.CurrentPlayer)
            {
                selectedPosition = null;
                HideHighlights();
                OnFromPositionSelected(position);
                return;
            }

            // If no piece is selected yet, treat this click as selecting a from-position
            if (selectedPosition == null)
                OnFromPositionSelected(position);
            // Otherwise treat this click as selecting a to-position (executing a move)
            else
                OnToPositionSelected(position);
        }

        // Private method: Converts a pixel point on the board grid to a row/column Position
        private Position ToSquarePosition(Point point)
        {
            // Calculate the size of a single square in pixels
            double squareSize = BoardGrid.ActualWidth / 8;
            // Determine row by dividing Y by square size
            int row = (int)(point.Y / squareSize);
            // Determine column by dividing X by square size
            int column = (int)(point.X / squareSize);
            return new Position(row, column);
        }

        // Private method: Handles selection of the piece to move; shows legal move highlights
        private void OnFromPositionSelected(Position position)
        {
            // Get all legal moves for the piece at this position
            IEnumerable<Move> moves = gameState.LegalMovesForPiece(position);
            // Only proceed if there are legal moves available
            if (moves.Any())
            {
                // Store the selected position
                selectedPosition = position;
                // Cache legal moves keyed by destination square
                CacheMoves(moves);
                // Highlight all valid destination squares
                ShowHighlightMoves();
            }
        }

        // Private method: Handles selection of the destination square; executes or ignores the move
        private void OnToPositionSelected(Position position)
        {
            // Clear the selection state
            selectedPosition = null;
            // Remove all highlights from the board
            HideHighlights();

            // Look up the move for this destination square in the cache
            if (moveCache.TryGetValue(position, out Move move))
            {
                // If this is a pawn promotion, show the promotion piece selection menu
                if (move.Type == MoveType.PawnPromotion)
                    HandlePromotion(move.FromPosition, move.ToPosition);
                // Otherwise execute the move directly
                else
                    HandleMove(move);
            }
        }

        // Private method: Shows the pawn promotion menu and handles the resulting piece selection
        private void HandlePromotion(Position from, Position to)
        {
            // Show a pawn sprite on the destination square as a preview
            pieceImages[to.Row, to.Column].Source = Images.GetImage(gameState.CurrentPlayer, PieceType.Pawn);
            // Clear the image on the source square
            pieceImages[from.Row, from.Column].Source = null;

            // Create the promotion selection menu for the current player
            PromotionMenu promotionMenu = new PromotionMenu(gameState.CurrentPlayer);
            // Display the menu in the overlay container
            MenuContainer.Content = promotionMenu;

            // When the player selects a piece type, finalize the promotion move
            promotionMenu.PieceSelected += pieceType =>
            {
                // Hide the promotion menu
                MenuContainer.Content = null;
                // Create the promotion move with the chosen piece type
                Move promotionMove = new PawnPromotion(from, to, pieceType);
                // Execute the promotion move
                HandleMove(promotionMove);
            };
        }

        // Private method: Executes a move, updates the game state, saves it, and refreshes the UI
        private void HandleMove(Move move)
        {
            // Record whether it was White's move before applying it
            bool wasWhiteMove = gameState.CurrentPlayer == ChessLogic.Player.White;
            // Generate SAN notation before applying the move (board state needed for notation)
            string notation = CreateSAN(move);

            // Persist the move to the database via the callback
            SaveMoveToDb(move);
            // Apply the move to the game state
            gameState.MakeMove(move);
            // Save a board snapshot for history navigation
            boardSnapshots.Add(gameState.Board.Copy());
            // Add the move to the visible move history list
            AddMoveToHistory(notation, wasWhiteMove);
            // Re-render the board with updated piece positions
            DrawBoard(gameState.Board);
            // Update the cursor for the new current player
            SetCursor(gameState.CurrentPlayer);

            // If the game is over after this move, show the game over menu
            if (gameState.IsGameOver())
                ShowGameOverMenu();
        }

        // Private method: Serializes the move and invokes the database save callback
        private void SaveMoveToDb(Move move)
        {
            // If no callback is registered, skip saving
            if (onMoveSaved == null) return;

            // Convert from-position to algebraic notation (e.g., column 4, row 6 → "e2")
            string fromStr = ((char)('a' + move.FromPosition.Column)).ToString() + (8 - move.FromPosition.Row).ToString();
            // Convert to-position to algebraic notation (e.g., column 4, row 4 → "e4")
            string toStr = ((char)('a' + move.ToPosition.Column)).ToString() + (8 - move.ToPosition.Row).ToString();
            // Generate the SAN notation string for this move
            string notation = CreateSAN(move);

            // Invoke the callback with from, to, move index, and notation
            onMoveSaved(fromStr, toStr, moveIndex, notation);
            // Increment the move index for the next move
            moveIndex++;
        }

        // Private method: Rebuilds the moveCache dictionary from a set of legal moves
        private void CacheMoves(IEnumerable<Move> moves)
        {
            // Clear any previously cached moves
            moveCache.Clear();
            // Map each move's destination position to the move object
            foreach (Move move in moves)
                moveCache[move.ToPosition] = move;
        }

        // Private method: Highlights all cached destination squares with a semi-transparent green
        private void ShowHighlightMoves()
        {
            // Create a semi-transparent green color (alpha=128)
            Color color = Color.FromArgb(128, 100, 255, 100);
            SolidColorBrush brush = new SolidColorBrush(color);
            // Apply the highlight color to each destination square's rectangle
            foreach (Position to in moveCache.Keys)
                highlights[to.Row, to.Column].Fill = brush;
        }

        // Private method: Clears all move highlights by setting rectangles to transparent
        private void HideHighlights()
        {
            foreach (Position to in moveCache.Keys)
                highlights[to.Row, to.Column].Fill = Brushes.Transparent;
        }

        // Private method: Changes the mouse cursor to match the current player's color
        private void SetCursor(ChessLogic.Player player)
        {
            Cursor = player == ChessLogic.Player.White ? ChessCursors.WhiteCursor : ChessCursors.BlackCursor;
        }

        // Private method: Returns true if a menu (promotion or game over) is currently displayed
        private bool IsMenuOnScreen() => MenuContainer.Content != null;

        // Private method: Shows the game over menu and fires the GameOver event exactly once
        private void ShowGameOverMenu()
        {
            // Guard: only show the menu once
            if (gameOverMenuShown) return;
            gameOverMenuShown = true;

            // Stop polling and remove keyboard hooks since the game is over
            StopPolling();

            // Get the game result (winner and reason)
            Result result = gameState.Result;

            // Build the move list as pairs of [whiteMove, blackMove] strings
            List<string[]> moves = new List<string[]>();
            foreach (ChessMoveRecord record in moveHistory)
            {
                // Each pair contains White's and Black's move for one full move
                string[] pair = new string[2];
                pair[0] = record.WhiteMove;
                pair[1] = record.BlackMove;
                moves.Add(pair);
            }

            // Fire the GameOver event exactly once with the result and move list
            if (!gameOverFired)
            {
                gameOverFired = true;
                GameOver?.Invoke(result.Winner, result.Reason.ToString(), moves);
            }

            // Create the game over menu UI, passing the current game state
            GameOverMenu gameOverMenu = new GameOverMenu(gameState);

            // If the local player is Black, flip the menu to match the flipped board orientation
            if (myColor == ChessLogic.Player.Black)
                gameOverMenu.LayoutTransform = new ScaleTransform(1, -1);

            // Display the game over menu in the overlay container
            MenuContainer.Content = gameOverMenu;

            // When the player selects an option (e.g., exit), hide the menu and fire GameExited
            gameOverMenu.OnOptionSelected += option =>
            {
                // Remove the game over menu from the overlay
                MenuContainer.Content = null;
                // Notify the parent that the player has exited the game
                GameExited?.Invoke();
            };
        }

        // Private method: Resets all game state and UI to start a fresh game from the beginning
        private void RestartGame()
        {
            // Clear move highlights from the board
            HideHighlights();
            // Clear the cached moves for the previously selected piece
            moveCache.Clear();
            // Clear the move history list
            moveHistory.Clear();
            // Clear all saved board snapshots
            boardSnapshots.Clear();
            // Reset the snapshot viewer to the live board
            viewingSnapshot = -1;
            // Reset the move index counter
            moveIndex = 0;
            // Reset the last known move index so the next poll starts fresh
            lastKnownMoveIndex = -1;
            // Allow the GameOver event to fire again for the new game
            gameOverFired = false;
            // Allow the game over menu to be shown again for the new game
            gameOverMenuShown = false;
            // Create a new game starting with White to move
            gameState = new GameState(ChessLogic.Player.White, Board.Initial());
            // Render the starting position
            DrawBoard(gameState.Board);
            // Set the cursor for White
            SetCursor(gameState.CurrentPlayer);
        }

        // Private method: Appends or updates the move history list with the given notation
        private void AddMoveToHistory(string notation, bool wasWhite)
        {
            // White's move starts a new row in the history list
            if (wasWhite)
                moveHistory.Add(new ChessMoveRecord { MoveNumber = moveHistory.Count + 1, WhiteMove = notation });
            // Black's move fills in the second column of the most recent row
            else
                moveHistory[moveHistory.Count - 1].BlackMove = notation;
        }

        // Private method: Generates Standard Algebraic Notation (SAN) for a given move
        private string CreateSAN(Move move)
        {
            // Castling has fixed notation regardless of piece positions
            if (move.Type == MoveType.CastleKingside) return "O-O";
            if (move.Type == MoveType.CastleQueenside) return "O-O-O";

            StringBuilder sb = new StringBuilder();
            // Get the piece that is moving
            Piece movingPiece = gameState.Board[move.FromPosition];
            // Append the piece letter (empty string for pawns)
            sb.Append(PieceToChar(movingPiece.Type));

            // Determine if this move is a capture (occupied destination or en passant)
            bool isCapture = !gameState.Board.IsEmpty(move.ToPosition) || move.Type == MoveType.EnPassant;
            // For pawn captures, prefix with the file letter of the pawn's origin column
            if (movingPiece.Type == PieceType.Pawn && isCapture)
                sb.Append((char)('a' + move.FromPosition.Column));

            // Append 'x' for captures
            if (isCapture) sb.Append('x');

            // Append the destination file letter (a-h)
            sb.Append((char)('a' + move.ToPosition.Column));
            // Append the destination rank number (1-8)
            sb.Append(8 - move.ToPosition.Row);

            // Append promotion piece indicator (always promotes to Queen in this implementation)
            if (move.Type == MoveType.PawnPromotion) sb.Append("=Q");

            // Simulate the move on a copy of the game state to check for check/checkmate
            GameState copy = new GameState(gameState.CurrentPlayer, gameState.Board.Copy());
            copy.MakeMove(move);
            // Append '#' if the move results in checkmate
            if (copy.IsGameOver())
                sb.Append('#');
            // Append '+' if the move results in check
            else if (copy.Board.IsInCheck(copy.CurrentPlayer))
                sb.Append('+');

            return sb.ToString();
        }

        // Event handler: Navigates to the board snapshot corresponding to the selected move history row
        private void MoveHistoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Ignore programmatic selection changes triggered by snapshot navigation
            if (suppressSelectionChanged) return;
            // Ignore if no row is selected
            if (MoveHistoryList.SelectedIndex < 0) return;
            // Ignore if no snapshots exist yet
            if (boardSnapshots.Count == 0) return;
            // Ignore if the game is over (navigation disabled post-game)
            if (gameState.IsGameOver()) return;

            // Each row in the history list represents one full move (White + Black)
            int rowIndex = MoveHistoryList.SelectedIndex;
            // Black's snapshot is at an odd index (rowIndex * 2 + 1)
            int blackSnapshotIndex = (rowIndex * 2) + 1;
            // White's snapshot is at an even index (rowIndex * 2)
            int whiteSnapshotIndex = rowIndex * 2;
            // Prefer to show Black's snapshot; fall back to White's if Black hasn't moved yet
            int snapshotIndex = blackSnapshotIndex < boardSnapshots.Count
                ? blackSnapshotIndex
                : whiteSnapshotIndex;

            // Update the viewing snapshot index and render the corresponding board
            viewingSnapshot = snapshotIndex;
            DrawBoard(boardSnapshots[snapshotIndex]);
        }

        // Event handler: Handles Left/Right arrow keys to step through board snapshots one half-move at a time
        private void ChessWindow_KeyDown(object sender, KeyEventArgs e)
        {
            // FIX 1: ignore key-repeat (held key) so each press = exactly one half-move
            if (e.IsRepeat) return;

            if (e.Key == Key.Left)
            {
                // If currently on the live board, jump to the most recent snapshot
                if (viewingSnapshot == -1)
                {
                    if (boardSnapshots.Count == 0) return;
                    viewingSnapshot = boardSnapshots.Count - 1;
                }
                // If at the first snapshot, go back to the initial starting position
                else if (viewingSnapshot == 0)
                {
                    // Signal that we're viewing the initial position (before any moves)
                    viewingSnapshot = -2;
                    // Suppress list selection event while clearing the selection
                    suppressSelectionChanged = true;
                    MoveHistoryList.SelectedIndex = -1;
                    suppressSelectionChanged = false;
                    // Render the initial board position
                    DrawBoard(Board.Initial());
                    return;
                }
                // If already at the initial position, do nothing
                else if (viewingSnapshot == -2)
                {
                    return;
                }
                // Otherwise step one snapshot backward
                else
                {
                    viewingSnapshot--;
                }
            }
            else if (e.Key == Key.Right)
            {
                // If at the initial position, step forward to the first snapshot
                if (viewingSnapshot == -2)
                {
                    viewingSnapshot = 0;
                }
                // If already on the live board, do nothing
                else if (viewingSnapshot == -1)
                {
                    return;
                }
                else
                {
                    // Step one snapshot forward
                    viewingSnapshot++;
                    // If we've gone past the last snapshot, return to the live board
                    if (viewingSnapshot >= boardSnapshots.Count)
                    {
                        // -1 signals the live board
                        viewingSnapshot = -1;
                        // Clear the move history selection without triggering the handler
                        suppressSelectionChanged = true;
                        MoveHistoryList.SelectedIndex = -1;
                        suppressSelectionChanged = false;
                        // Render the current live board
                        DrawBoard(gameState.Board);
                        return;
                    }
                }
            }
            // Ignore all other keys
            else return;

            // Highlight the corresponding row in the move history list
            suppressSelectionChanged = true;
            // Each row covers two snapshots (White and Black), so divide by 2
            int rowIndex = viewingSnapshot / 2;
            MoveHistoryList.SelectedIndex = rowIndex;
            suppressSelectionChanged = false;
            // Render the board at the current snapshot
            DrawBoard(boardSnapshots[viewingSnapshot]);
        }

        // Event handler: Navigates to the very first position (before any moves were made)
        private void BtnFirst_Click(object sender, RoutedEventArgs e)
        {
            // Nothing to navigate to if no moves have been made
            if (boardSnapshots.Count == 0) return;
            // Signal that we're viewing the initial position
            viewingSnapshot = -2;
            // Clear the move history selection without triggering the handler
            suppressSelectionChanged = true;
            MoveHistoryList.SelectedIndex = -1;
            suppressSelectionChanged = false;
            // Render the starting board position
            DrawBoard(Board.Initial());
        }

        // Event handler: Steps one half-move backward by simulating a Left arrow key press
        private void BtnPrev_Click(object sender, RoutedEventArgs e)
        {
            // Create a synthetic Left arrow KeyEventArgs and pass it to the keyboard handler
            var args = new KeyEventArgs(Keyboard.PrimaryDevice,
                PresentationSource.FromVisual(this), 0, Key.Left);
            args.RoutedEvent = KeyDownEvent;
            ChessWindow_KeyDown(sender, args);
        }

        // Event handler: Steps one half-move forward by simulating a Right arrow key press
        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            // Create a synthetic Right arrow KeyEventArgs and pass it to the keyboard handler
            var args = new KeyEventArgs(Keyboard.PrimaryDevice,
                PresentationSource.FromVisual(this), 0, Key.Right);
            args.RoutedEvent = KeyDownEvent;
            ChessWindow_KeyDown(sender, args);
        }

        // Event handler: Navigates to the most recent position (the live board)
        private void BtnLast_Click(object sender, RoutedEventArgs e)
        {
            // Nothing to navigate to if no moves have been made
            if (boardSnapshots.Count == 0) return;
            // -1 signals the live board
            viewingSnapshot = -1;
            // Clear the move history selection without triggering the handler
            suppressSelectionChanged = true;
            MoveHistoryList.SelectedIndex = -1;
            suppressSelectionChanged = false;
            // Render the current live board
            DrawBoard(gameState.Board);
        }

        // Private method: Converts a PieceType to its SAN letter (empty string for Pawn)
        private string PieceToChar(PieceType type)
        {
            switch (type)
            {
                case PieceType.King: return "K";
                case PieceType.Queen: return "Q";
                case PieceType.Rook: return "R";
                case PieceType.Bishop: return "B";
                case PieceType.Knight: return "N";
                // Pawns have no letter prefix in SAN notation
                default: return "";
            }
        }
    }
}