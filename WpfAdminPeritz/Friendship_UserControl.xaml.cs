using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfAdminPeritz.ServiceReferenceChess;

// Alias to avoid ambiguity between ServiceReferenceChess.Player and other Player types
using ServicePlayer = WpfAdminPeritz.ServiceReferenceChess.Player;

namespace WpfAdminPeritz
{
    // UserControl that displays a list of all users and allows viewing/managing their friendships
    public partial class Friendship_UserControl : UserControl
    {
        // Field: WCF service client used to communicate with the chess server
        private ChessServiceAdminClient service;
        // Field: The currently logged-in admin player
        private ServicePlayer admin;
        // Field: The user whose friend list is currently being displayed on the right panel
        private ServicePlayer currentShownUser;

        // Constructor: Initializes the control with the logged-in admin and loads the user list
        public Friendship_UserControl(ServicePlayer loggedInAdmin)
        {
            // Initialize WPF components
            InitializeComponent();
            // Create the service client for server communication
            service = new ChessServiceAdminClient();
            // Store the logged-in admin reference
            admin = loggedInAdmin;
            // Populate the left panel with all registered users
            LoadUsers();
        }

        // Private method: Fetches all users from the server and populates the left user list
        private void LoadUsers()
        {
            // Clear any existing items before reloading
            ListBoxUsers.Items.Clear();
            // Fetch all users from the server
            PlayerList users = service.GetAllUsers();
            // Add only Registered and Admin users to the list
            foreach (ServicePlayer p in users)
            {
                // Skip guests or other non-standard user types
                if (p.UserType == "Registered" || p.UserType == "Admin")
                    ListBoxUsers.Items.Add(new FriendshipUC(this, p));
            }
        }

        // Event handler: Enables the view button only for the currently selected user row
        private void ListBoxUsers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Disable the view button on all rows first
            foreach (var item in ListBoxUsers.Items)
            {
                if (item is FriendshipUC uc)
                    uc.SetViewEnabled(false);
            }

            // Enable the view button only on the newly selected row
            if (ListBoxUsers.SelectedItem is FriendshipUC selected)
            {
                selected.SetViewEnabled(true);
            }
        }

        // Public method: Loads and displays the friend list for the given user in the right panel
        public void ShowFriendsOf(ServicePlayer user)
        {
            // Store the user whose friends are being shown (used for refresh after removal)
            currentShownUser = user;
            // Update the right panel header with the user's name
            SelectedUserName.Text = user.UserName + "'s Friends";
            // Clear the previous friend list
            ListBoxFriends.Items.Clear();

            // Fetch the accepted friendships for this user from the server
            FriendshipList friendships = service.GetAcceptedFriendsByUser(user.Id);
            // If the user has no friends, show a placeholder message
            if (friendships == null || friendships.Count == 0)
            {
                ListBoxFriends.Items.Add(new TextBlock
                {
                    Text = "No friends yet.",
                    Foreground = Brushes.Gray,
                    Padding = new Thickness(8),
                    FontSize = 14
                });
                return;
            }

            // Build a card UI element for each friendship
            foreach (Friendship f in friendships)
            {
                // Determine the friend's ID: whichever side of the friendship is not this user
                int friendId = f.RequesterID == user.Id ? f.ReceiverID : f.RequesterID;
                // Fetch the friend's full player object from the server
                ServicePlayer friend = service.GetUserByID(friendId);
                // Skip if the friend could not be found
                if (friend == null) continue;

                // Store the friendship ID for use in the remove button click handler
                int friendshipId = f.Id;

                // Outer card border with rounded corners and light background
                Border card = new Border
                {
                    CornerRadius = new CornerRadius(12),
                    Margin = new Thickness(0, 5, 0, 5),
                    Padding = new Thickness(14, 10, 14, 10),
                    Background = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#F9F9F9"))
                };

                // Inner grid with 4 columns: avatar | name | stats | remove button
                Grid row = new Grid();
                // Column 0: Fixed width for the avatar circle
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(44) });
                // Column 1: Star-width for the name panel (takes remaining space)
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                // Column 2: Auto-width for the stats badges
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                // Column 3: Auto-width for the remove button
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                // Avatar: Circular black border containing a person emoji
                Border avatar = new Border
                {
                    Width = 34,
                    Height = 34,
                    // Fully rounded to create a circle
                    CornerRadius = new CornerRadius(17),
                    Background = Brushes.Black,
                    VerticalAlignment = VerticalAlignment.Center
                };
                // Person emoji as the avatar icon
                avatar.Child = new TextBlock
                {
                    Text = "👤",
                    FontSize = 16,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                // Place avatar in the first column
                Grid.SetColumn(avatar, 0);

                // Name panel: Stacks the friend's username and a "Friend" subtitle
                StackPanel namePanel = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(10, 0, 0, 0)
                };
                // Friend's username in bold
                namePanel.Children.Add(new TextBlock
                {
                    Text = friend.UserName,
                    FontSize = 15,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Brushes.Black
                });
                // "Friend" subtitle in gray
                namePanel.Children.Add(new TextBlock
                {
                    Text = "Friend",
                    FontSize = 11,
                    Foreground = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#888888"))
                });
                // Place name panel in the second column
                Grid.SetColumn(namePanel, 1);

                // Stats panel: Horizontal row of colored badges showing Wins, Draws, Losses
                StackPanel statsPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 12, 0)
                };
                // Green badge for wins
                statsPanel.Children.Add(MakeBadge("W: " + friend.Wins, "#E8F5E9", "#2E7D32"));
                // Gray badge for draws
                statsPanel.Children.Add(MakeBadge("D: " + friend.Draws, "#F5F5F5", "#616161"));
                // Red badge for losses
                statsPanel.Children.Add(MakeBadge("L: " + friend.Losses, "#FFEBEE", "#C62828"));
                // Place stats panel in the third column
                Grid.SetColumn(statsPanel, 2);

                // Capture the friend's name in a local variable for use inside the lambda
                string friendName = friend.UserName;
                // Remove button: White outlined button that triggers friendship removal
                Button removeBtn = new Button
                {
                    Content = "Remove",
                    Width = 75,
                    Height = 30,
                    Background = Brushes.White,
                    Foreground = Brushes.Black,
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    BorderThickness = new Thickness(1.5),
                    BorderBrush = Brushes.Black,
                    Cursor = System.Windows.Input.Cursors.Hand,
                    VerticalAlignment = VerticalAlignment.Center
                };

                // Apply a custom rounded-corner template to the remove button
                removeBtn.Template = MakeButtonTemplate();
                // On click, confirm and remove the friendship
                removeBtn.Click += (s, e) =>
                {
                    RemoveFriendship(friendshipId, friendName);
                };
                // Place remove button in the fourth column
                Grid.SetColumn(removeBtn, 3);

                // Add all elements to the row grid
                row.Children.Add(avatar);
                row.Children.Add(namePanel);
                row.Children.Add(statsPanel);
                row.Children.Add(removeBtn);
                // Set the row grid as the card's content
                card.Child = row;

                // Add the completed card to the friends list
                ListBoxFriends.Items.Add(card);
            }
        }

        // Private method: Prompts for confirmation then removes the specified friendship
        private void RemoveFriendship(int friendshipId, string friendName)
        {
            // Show a Yes/No confirmation dialog before deleting
            var result = MessageBox.Show(
                "Remove " + friendName + " from friends?",
                "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
            // Abort if the admin clicked No
            if (result != MessageBoxResult.Yes) return;

            // Delete the friendship record from the server
            service.DeleteFriendship(friendshipId);
            // Refresh the friend list for the currently shown user
            if (currentShownUser != null)
                ShowFriendsOf(currentShownUser);
        }

        // Private method: Creates a custom ControlTemplate for a button with rounded corners
        // This is needed because WPF's default button template ignores CornerRadius
        private ControlTemplate MakeButtonTemplate()
        {
            // Create a new template targeting the Button type
            ControlTemplate template = new ControlTemplate(typeof(Button));
            // Use a Border as the root visual element
            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
            // Bind the border's Background to the button's Background property
            border.SetBinding(Border.BackgroundProperty,
                new System.Windows.Data.Binding("Background")
                {
                    RelativeSource = new System.Windows.Data.RelativeSource(
                        System.Windows.Data.RelativeSourceMode.TemplatedParent)
                });
            // Bind the border's BorderBrush to the button's BorderBrush property
            border.SetBinding(Border.BorderBrushProperty,
                new System.Windows.Data.Binding("BorderBrush")
                {
                    RelativeSource = new System.Windows.Data.RelativeSource(
                        System.Windows.Data.RelativeSourceMode.TemplatedParent)
                });
            // Bind the border's BorderThickness to the button's BorderThickness property
            border.SetBinding(Border.BorderThicknessProperty,
                new System.Windows.Data.Binding("BorderThickness")
                {
                    RelativeSource = new System.Windows.Data.RelativeSource(
                        System.Windows.Data.RelativeSourceMode.TemplatedParent)
                });
            // Set rounded corners on the border
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));

            // Add a ContentPresenter inside the border to render the button's label
            FrameworkElementFactory content = new FrameworkElementFactory(typeof(ContentPresenter));
            // Center the content horizontally
            content.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            // Center the content vertically
            content.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            // Attach the ContentPresenter as a child of the border
            border.AppendChild(content);
            // Set the border as the root of the visual tree
            template.VisualTree = border;
            return template;
        }

        // Private method: Creates a small colored badge used to display a stat (W/D/L)
        private Border MakeBadge(string text, string bgColor, string fgColor)
        {
            return new Border
            {
                // Set the badge background color from the provided hex string
                Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString(bgColor)),
                // Rounded corners for the pill/badge shape
                CornerRadius = new CornerRadius(6),
                // Internal padding around the text
                Padding = new Thickness(8, 3, 8, 3),
                // Right margin to space badges apart from each other
                Margin = new Thickness(0, 0, 6, 0),
                // Text label inside the badge
                Child = new TextBlock
                {
                    Text = text,
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    // Set the text color from the provided hex string
                    Foreground = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString(fgColor))
                }
            };
        }
    }
}