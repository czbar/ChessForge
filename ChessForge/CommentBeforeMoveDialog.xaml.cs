using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for AnnotationsDialog.xaml
    /// </summary>
    public partial class CommentBeforeMoveDialog : Window
    {
        /// <summary>
        /// Comment to be placed before the move for which this dialog was invoked.
        /// </summary>
        public string CommentBeforeMove { get; set; }

        /// <summary>
        /// Constructs the dialog.
        /// Sets the values passed by the caller.
        /// </summary>
        /// <param name="nd"></param>
        public CommentBeforeMoveDialog(TreeNode nd)
        {
            InitializeComponent();
            UiTbCommentBeforeMove.Text = nd.CommentBeforeMove ?? "";
            UiTbCommentBeforeMove.Focus();
            UiTbCommentBeforeMove.SelectAll();
        }

        /// <summary>
        /// Closes the dialog after user pushed the Cancel button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        /// <summary>
        /// Closes the dialog after user pushed the OK button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnOk_Click(object sender, RoutedEventArgs e)
        {
            CommentBeforeMove = UiTbCommentBeforeMove.Text;
            DialogResult = true;
            Close();
        }

        /// <summary>
        /// Links to the relevant Wiki page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnHelp_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/czbar/ChessForge/wiki/Comment-Before-Move-Editor");
        }

        /// <summary>
        /// Handles the key down event in the text box. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiTbComment_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (GuiUtilities.InsertFigurine(UiTbCommentBeforeMove, sender, e))
            {
                e.Handled = true;
            }
        }

    }
}

