using System.Windows;
using System.Windows.Controls;
using WpfAdminPeritz.ServiceReferenceUserChess;

namespace WpfAdminPeritz
{
    public partial class GameUC_Player : UserControl
    {
        private PlayerGames_UserControl playerGames;

        private Game game;

        public GameUC_Player(PlayerGames_UserControl gamesControl, Game g)
        {
            InitializeComponent();

            playerGames = gamesControl;

            game = g;

            DataContext = g;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            playerGames.SetSelectedGame(game);
        }

        public Game GetGame()
        {
            return game;
        }
    }
}