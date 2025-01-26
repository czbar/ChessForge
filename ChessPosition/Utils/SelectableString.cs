using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ChessPosition
{
    /// <summary>
    /// Encapsulates a string and a boolean value.
    /// </summary>
    public class SelectableString : INotifyPropertyChanged
    {
        // whether this line is selected in the GUI
        private bool _isSelected;

        /// <summary>
        /// The string.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Accessor to _isSelected.
        /// This is the only property that can be changed
        /// from the GUI.
        /// </summary>
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                _isSelected = value;
                NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Creates a new instance of SelectableString.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="isSelected"></param>
        public SelectableString(string text, bool isSelected)
        {
            Text = text;
            IsSelected = isSelected;
        }

        /// <summary>
        /// PropertChange event handler
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Notifies the framework of the change in the bound data.
        /// </summary>
        /// <param name="propertyName"></param>
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
