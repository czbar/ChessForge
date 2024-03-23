using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for SingleLineEditor.xaml
    /// </summary>
    public partial class SingleLineEditor : Window
    {
        /// <summary>
        /// Creates a simple one line text editor
        /// </summary>
        /// <param name="title">Text to show in the title bar.</param>
        /// <param name="textOnEntry">Initial text in the TextBox.</param>
        public SingleLineEditor(string title, string textOnEntry)
        {
            InitializeComponent();

            this.Title = title;
            UiTbText.Text = textOnEntry;
            UiTbText.Focus();
            UiTbText.SelectAll();
        }

        /// <summary>
        /// Close the dialog when user presses OK and return true.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
