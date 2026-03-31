using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace ChessUI
{
    // Represents a single row in the move history table displayed during a chess game
    // Implements INotifyPropertyChanged to update the UI when BlackMove is set after white moves
    public class MoveRecord : INotifyPropertyChanged
    {
        // Field: Stores the black player's move notation
        private string blackMove;

        // Property: The move number displayed in the # column (1, 2, 3, ...)
        public int MoveNumber { get; set; }

        // Property: White player's move in SAN notation (e.g. "e4", "Nf3", "O-O")
        public string WhiteMove { get; set; }

        // Property: Black player's move in SAN notation
        // Notifies the UI when set so the ListView updates immediately
        public string BlackMove
        {
            get => blackMove;
            set
            {
                // Update the backing field
                blackMove = value;
                // Notify the UI that BlackMove has changed
                OnPropertyChanged(nameof(BlackMove));
            }
        }

        // Event: Required by INotifyPropertyChanged interface
        // Subscribed to by WPF bindings to detect property changes
        public event PropertyChangedEventHandler PropertyChanged;

        // Method: Raises the PropertyChanged event for the specified property
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}