using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for ChapterFromLineDialog.xaml
    /// </summary>
    public partial class ChapterFromLineDialog : Window
    {
        /// <summary>
        /// True if the user pushed Yes or No to exit.
        /// </summary>
        public bool ExitOK = false;

        /// <summary>
        /// Title of the chapter.
        /// </summary>
        public string ChapterTitle;

        /// <summary>
        /// Whether the user chose to go to the new chapter
        /// after creating it.
        /// </summary>
        public bool GoToNewChapter = false;

        /// <summary>
        /// Whether to delete the subtree of the new chapter
        /// from the origin Study.
        /// </summary>
        public bool DeleteOriginal = false;

        /// <summary>
        /// Creates the dialog.
        /// Show the Chapter's title.
        /// </summary>
        /// <param name="chapter"></param>
        public ChapterFromLineDialog(Chapter chapter)
        {
            InitializeComponent();
            UiTbChapterTitle.Text = chapter.GetTitle();
        }

        /// <summary>
        /// Gets data from the controls.
        /// </summary>
        private void CollectResponses()
        {
            GoToNewChapter = UiCbGoToNew.IsChecked == true;
            DeleteOriginal = UiCbDeleteOrig.IsChecked == true;
            ChapterTitle = UiTbChapterTitle.Text ?? "";
        }

        /// <summary>
        /// User chose to remove the variation from the origin.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnOk_Click(object sender, RoutedEventArgs e)
        {
            CollectResponses();

            ExitOK = true;
            Close();
        }

        /// <summary>
        /// User chose to keep the variation in the origin.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
