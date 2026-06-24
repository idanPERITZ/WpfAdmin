using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace WpfAdminPeritz
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            try
            {
                // Attempt to mark any current user as offline when application exits
                // Check for PlayerMainWindow (regular user)
                foreach (Window w in Current.Windows)
                {
                    if (w is PlayerMainWindow pmw)
                    {
                        try { CallbackServiceManager.Instance.UserService.PlayerLeave(pmw.GetType().GetProperty("loggedInPlayer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(pmw) as WpfAdminPeritz.ServiceReferenceUserChess.Player); } catch { }
                    }

                    if (w is AdminMainWindow amw)
                    {
                        try
                        {
                            // AdminMainWindow stores admin as ServiceReferenceChess.Player named 'admin'
                            var adminField = amw.GetType().GetField("admin", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(amw) as WpfAdminPeritz.ServiceReferenceChess.Player;
                            if (adminField != null)
                            {
                                var userToLeave = new WpfAdminPeritz.ServiceReferenceUserChess.Player
                                {
                                    Id = adminField.Id,
                                    UserName = adminField.UserName,
                                    Email = adminField.Email,
                                    DateJoined = adminField.DateJoined,
                                    GamesPlayed = adminField.GamesPlayed,
                                    Wins = adminField.Wins,
                                    Losses = adminField.Losses,
                                    Draws = adminField.Draws,
                                    GoogleId = adminField.GoogleId,
                                    UserType = adminField.UserType
                                };

                                CallbackServiceManager.Instance.UserService.PlayerLeave(userToLeave);
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }
    }
}
