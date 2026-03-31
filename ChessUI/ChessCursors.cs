using System.IO;
using System.Windows;
using System.Windows.Input;
using System;

namespace ChessUI
{
    // Static class for loading and managing custom mouse cursors for chess pieces
    public static class ChessCursors
    {
        // Static field: Custom cursor for white pieces (loaded from file)
        public static readonly Cursor WhiteCursor = LoadCursor("Assets/CursorW.cur");
        // Static field: Custom cursor for black pieces (loaded from file)
        public static readonly Cursor BlackCursor = LoadCursor("Assets/CursorB.cur");

        // Private static method: Loads a cursor from a file path
        private static Cursor LoadCursor(string filePath)
        {
            // Get the resource stream for the cursor file
            Stream stream = Application.GetResourceStream(new Uri(filePath, UriKind.Relative)).Stream;
            // Create and return a new Cursor object from the stream
            return new Cursor(stream, true);
        }
    }
}