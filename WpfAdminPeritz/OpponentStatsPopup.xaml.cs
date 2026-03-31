using System.Windows;
using System.Windows.Input;
using WpfAdminPeritz.ServiceReferenceChess;
// Alias to avoid ambiguity between ServiceReferenceChess.Player and ChessLogic.Player
using ServicePlayer = WpfAdminPeritz.ServiceReferenceChess.Player;

namespace WpfAdminPeritz
{
    // Popup window that displays the opponent's stats and friendship controls during a chess game
    // Closes automatically when clicked outside or when the window loses focus
    public partial class OpponentStatsPopup : Window
    {
        // Field: WCF service client used to communicate with the chess server
        private ChessServiceAdminClient service;
        // Field: The opponent player whose stats and friendship status are being displayed
        private ServicePlayer opponent;
        // Field: The local (logged-in) player viewing this popup
        private ServicePlayer me;
        // Field: ID of a pending friend request sent TO the local player BY the opponent (-1 if none)
        private int pendingFriendshipID = -1;
        // Field: ID of an existing accepted friendship between the two players (-1 if none)
        private int existingFriendshipID = -1;

        // Constructor: Initializes the popup with service, player references, and loads all data
        public OpponentStatsPopup(ChessServiceAdminClient svc, ServicePlayer opponent,
            ServicePlayer me, ChessLogic.Player myColor, int gameID)
        {
            // Initialize WPF components
            InitializeComponent();
            // Store the service client reference
            this.service = svc;
            // Store the opponent player reference
            this.opponent = opponent;
            // Store the local player reference
            this.me = me;
            // Load and display the opponent's stats and friendship state
            LoadData();
        }

        // Private method: Fetches opponent stats and determines which friendship UI state to show
        // Three possible states: pending request from opponent, already friends, or no relationship
        private void LoadData()
        {
            // Display the opponent's username and win/draw/loss record
            TxtName.Text = opponent.UserName;
            TxtWins.Text = opponent.Wins.ToString();
            TxtDraws.Text = opponent.Draws.ToString();
            TxtLosses.Text = opponent.Losses.ToString();

            // Check if the opponent has already sent a friend request to the local player
            FriendshipList pendingForMe = service.GetPendingFriendRequestsForUser(me.Id);
            if (pendingForMe != null)
            {
                foreach (Friendship f in pendingForMe)
                {
                    // If the opponent is the requester, a pending request exists for us to respond to
                    if (f.RequesterID == opponent.Id)
                    {
                        // Store the pending friendship ID for use in Accept/Decline handlers
                        pendingFriendshipID = f.Id;

                        // Hide the main friend button and show the Accept/Decline inline actions instead
                        BtnFriend.Visibility = Visibility.Collapsed;
                        InlineActions.Visibility = Visibility.Visible;
                        // Early return: no need to check further friendship states
                        return;
                    }
                }
            }

            // No pending request from opponent — show the main friend button
            BtnFriend.Visibility = Visibility.Visible;
            // Check if the two players are already friends
            FriendshipList friendships = service.GetAcceptedFriendsByUser(me.Id);
            if (friendships != null)
            {
                foreach (Friendship f in friendships)
                {
                    // Match the friendship where either side is the opponent
                    if (f.RequesterID == opponent.Id || f.ReceiverID == opponent.Id)
                    {
                        // Store the existing friendship ID for use in the remove handler
                        existingFriendshipID = f.Id;
                        // Change the button to a red "Remove Friend" action
                        BtnFriend.Content = "Remove Friend";
                        BtnFriend.Foreground = new System.Windows.Media.SolidColorBrush(
                            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E53935"));
                        BtnFriend.IsEnabled = true;
                        // Hide the Accept/Decline actions since this is not a pending request
                        InlineActions.Visibility = Visibility.Collapsed;
                        // Early return: friendship state is fully resolved
                        return;
                    }
                }
            }

            // No existing friendship and no pending request — show the green "Send Friend Request" button
            BtnFriend.Content = "Send Friend Request";
            BtnFriend.Foreground = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#4CAF50"));
            BtnFriend.IsEnabled = true;
            // Hide the Accept/Decline actions since there is no pending request
            InlineActions.Visibility = Visibility.Collapsed;
        }

        // Event handler: Handles clicks on the main friend button (Send Request or Remove Friend)
        private void BtnFriend_Click(object sender, RoutedEventArgs e)
        {
            // If an existing friendship is present, the button acts as "Remove Friend"
            if (existingFriendshipID != -1)
            {
                // Prompt for confirmation before removing the friendship
                var result = MessageBox.Show(
                    "Remove " + opponent.UserName + " from friends?",
                    "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    // Delete the friendship record on the server
                    service.DeleteFriendship(existingFriendshipID);
                    // Reload the UI to reflect the updated friendship state
                    LoadData();
                }
            }
            else
            {
                // No existing friendship — send a new friend request to the opponent
                service.SendFriendRequest(me.Id, opponent.Id);
                // Update the button to a disabled gray "Request Sent" state to prevent duplicate sends
                BtnFriend.Content = "Request Sent";
                BtnFriend.Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#777777"));
                BtnFriend.IsEnabled = false;
            }
        }

        // Event handler: Accepts the opponent's pending friend request and refreshes the UI
        private void Accept_Click(object sender, MouseButtonEventArgs e)
        {
            // Guard: only proceed if there is a valid pending friendship to accept
            if (pendingFriendshipID != -1)
            {
                // Accept the friend request on the server
                service.AcceptFriendRequest(pendingFriendshipID);
                // Reset the pending ID since the request has been resolved
                pendingFriendshipID = -1;
                // Reload the UI to reflect the new accepted friendship state
                LoadData();
            }
        }

        // Event handler: Declines the opponent's pending friend request and refreshes the UI
        private void Decline_Click(object sender, MouseButtonEventArgs e)
        {
            // Guard: only proceed if there is a valid pending friendship to decline
            if (pendingFriendshipID != -1)
            {
                // Decline the friend request on the server
                service.DeclineFriendRequest(pendingFriendshipID);
                // Reset the pending ID since the request has been resolved
                pendingFriendshipID = -1;
                // Reload the UI to reflect the removed pending request
                LoadData();
            }
        }

        // Event handler: Closes the popup when the user clicks anywhere on the window background
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        // Event handler: Closes the popup when the window loses focus (e.g. user clicks elsewhere)
        private void Window_Deactivated(object sender, System.EventArgs e)
        {
            this.Close();
        }
    }
}