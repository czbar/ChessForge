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
    /// Interaction logic for ChapterTitleDialog.xaml
    /// </summary>
    public partial class ChapterTitleDialog : Window
    {
        /// <summary>
        /// Title of the chapter as edited in this dialog.
        /// </summary>
        public string ChapterTitle { get; set; }

        /// <summary>
        /// Author of the chapter.
        /// </summary>
        public string Author{ get; set; }

        /// <summary>
        /// Constructor.
        /// Sets the text to the current title of the chapter.
        /// </summary>
        /// <param name="chapter"></param>
        public ChapterTitleDialog(Chapter chapter)
        {
            InitializeComponent();
            UiTbChapterTitle.Text = chapter.GetTitle();
            UiTbChapterTitle.Focus();
            UiTbChapterTitle.SelectAll();

            UiTbAuthor.Text = chapter.GetAuthor();
        }

        /// <summary>
        /// Set the title property and Exit.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnOK_Click(object sender, RoutedEventArgs e)
        {
            ChapterTitle = UiTbChapterTitle.Text;
            Author = UiTbAuthor.Text;

            DialogResult = true;
        }

        /// <summary>
        /// Exit without setting the Title property.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        /// <summary>
        /// Check if the user pressed key combination to enter a figurine.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiTbChapterTitle_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (GuiUtilities.InsertFigurine(UiTbChapterTitle, sender, e))
            {
                e.Handled = true;
            }
        }
    }
}
