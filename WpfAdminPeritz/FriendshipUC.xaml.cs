using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfAdminPeritz.ServiceReferenceChess;

namespace WpfAdminPeritz
{
    public partial class FriendshipUC : UserControl
    {
        private Friendship_UserControl parentControl;
        private Player player;

        public FriendshipUC(Friendship_UserControl parent, Player p, bool isOnline)
        {
            InitializeComponent();

            parentControl = parent;
            player = p;

            TxtUserName.Text = p.UserName;
            TxtUserType.Text = p.UserType;

            if (isOnline)
            {
                TxtStatus.Text = "● Online";
                TxtStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60"));
            }
            else
            {
                TxtStatus.Text = "● Offline";
                TxtStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C0392B"));
            }
        }

        public void SetViewEnabled(bool enabled)
        {
            BtnView.IsEnabled = enabled;
        }

        private void BtnView_Click(object sender, RoutedEventArgs e)
        {
            parentControl.ShowFriendsOf(player);
        }

        public Player GetPlayer()
        {
            return player;
        }
    }
}