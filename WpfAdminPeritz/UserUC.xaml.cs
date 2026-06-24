using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfAdminPeritz.ServiceReferenceChess;

namespace WpfAdminPeritz
{
    public partial class UserUC : UserControl
    {
        private Users_UserControl adminListUsers;
        private Player user;

        public UserUC(Users_UserControl adminList, Player player, bool isOnline)
        {
            InitializeComponent();

            adminListUsers = adminList;
            user = player;

            DataContext = player;

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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (adminListUsers != null)
                adminListUsers.Set(user);
        }

        public Player GetPlayer()
        {
            return user;
        }
    }
}