using WpfAdminPeritz.ServiceReferenceChess;
using System.Windows;
using System.Windows.Controls;

namespace WpfAdminPeritz
{
    // UserControl representing a single user row in the left panel of the Friendship_UserControl
    // Displays the user's name and type, and provides a button to view their friend list
    public partial class FriendshipUC : UserControl
    {
        // Field: Reference to the parent control used to trigger the friend list display
        private Friendship_UserControl parentControl;
        // Field: The player this row represents
        private Player player;

        // Constructor: Initializes the row with the parent control reference and player data
        public FriendshipUC(Friendship_UserControl parent, Player p)
        {
            // Initialize WPF components
            InitializeComponent();
            // Store the parent control for callback use
            parentControl = parent;
            // Store the player this row represents
            player = p;
            // Display the player's username in the name label
            TxtUserName.Text = p.UserName;
            // Display the player's user type (e.g. "Registered", "Admin") in the type label
            TxtUserType.Text = p.UserType;
        }

        // Public method: Enables or disables the View button based on whether this row is selected
        public void SetViewEnabled(bool enabled)
        {
            BtnView.IsEnabled = enabled;
        }

        // Event handler: Tells the parent control to display the friend list for this row's player
        private void BtnView_Click(object sender, RoutedEventArgs e)
        {
            parentControl.ShowFriendsOf(player);
        }

        // Public method: Returns the player object associated with this row
        public Player GetPlayer()
        {
            return player;
        }
    }
}