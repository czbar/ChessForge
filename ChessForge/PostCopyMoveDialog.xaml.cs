using System;
using System.Text;
using System.Windows;
using ChessPosition;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for PostCopyMoveDialog.xaml
    /// </summary>
    public partial class PostCopyMoveDialog : Window
    {
        /// <summary>
        /// A dialog allowing the user to choose to navigate to the target chapter
        /// or stay in the current view.
        /// </summary>
        /// <param name="chapter"></param>
        /// <param name="action"></param>
        /// <param name="itemCount"></param>
        public PostCopyMoveDialog(Chapter chapter, ArticlesAction action, int itemCount)
        {
            InitializeComponent();

            StringBuilder sbDialogTitle = new StringBuilder();

            switch (action)
            {
                case ArticlesAction.COPY:
                    sbDialogTitle.AppendLine(Properties.Resources.LblNumberOfItemsCopied);
                    break;
                case ArticlesAction.MOVE:
                    sbDialogTitle.AppendLine(Properties.Resources.LblNumberOfItemsMoved);
                    break;
                case ArticlesAction.DELETE:
                    sbDialogTitle.AppendLine(Properties.Resources.LblNumberOfItemsDeleted);
                    break;
                default:
                    sbDialogTitle.AppendLine(Properties.Resources.LblNumberOfItemsActedOn);
                    break;
            }
            sbDialogTitle.Append(": " + itemCount.ToString());
            Title = sbDialogTitle.ToString();

            UiLblTargetChapterTitle.Content = Properties.Resources.Chapter + " " + (chapter.Index + 1).ToString() + ": " + chapter.Title;
        }

        /// <summary>
        /// The user chooses to navigate to the Chapters Vuiew.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnGoToTarget_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        /// <summary>
        /// The user chooses to stay in the current view.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnStayHere_Click(object sender, RoutedEventArgs e)
        {
            DialogResult= false;
        }
    }
}
