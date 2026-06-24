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
        public App()
        {
            // Catch all unhandled exceptions
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            string errorMessage = $"UNHANDLED EXCEPTION:\n\n{e.Exception.Message}\n\nStack Trace:\n{e.Exception.StackTrace}";

            System.Diagnostics.Debug.WriteLine(errorMessage);

            MessageBox.Show(errorMessage, "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);

            // Mark as handled so the app doesn't crash
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;
            string errorMessage = $"FATAL UNHANDLED EXCEPTION:\n\n{ex?.Message}\n\nStack Trace:\n{ex?.StackTrace}";

            System.Diagnostics.Debug.WriteLine(errorMessage);

            MessageBox.Show(errorMessage, "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

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
