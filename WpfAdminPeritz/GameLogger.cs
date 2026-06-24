using System;
using System.IO;

namespace WpfAdminPeritz
{
    public static class GameLogger
    {
        private static readonly string logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            "ChessGameLog.txt"
        );

        private static readonly object lockObj = new object();

        public static void Log(string message)
        {
            try
            {
                lock (lockObj)
                {
                    string logMessage = $"[{DateTime.Now:HH:mm:ss.fff}] {message}\n";
                    File.AppendAllText(logPath, logMessage);
                    System.Diagnostics.Debug.WriteLine(logMessage);
                }
            }
            catch
            {
                // If logging fails, don't crash the game
            }
        }

        public static void ClearLog()
        {
            try
            {
                lock (lockObj)
                {
                    if (File.Exists(logPath))
                        File.Delete(logPath);
                }
            }
            catch { }
        }
    }
}
