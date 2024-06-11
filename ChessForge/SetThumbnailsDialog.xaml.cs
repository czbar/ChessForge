using ChessPosition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for SetThumbnailsDialog.xaml
    /// </summary>
    public partial class SetThumbnailsDialog : Window
    {
        /// <summary>
        /// Number of the move at which to place a thumbnail 
        /// </summary>
        public int ThumbnailMove = -1;

        /// <summary>
        /// Whether to overwrite existing thumbnails
        /// </summary>
        public bool OverwriteThumbnails = false;

        /// <summary>
        /// Whether the thumbnail should be placed at the White's or Black's move.
        /// </summary>
        public PieceColor ThumbnailMoveColor = PieceColor.White;

        /// <summary>
        /// Whether the Sort command should be applied to all chapters.
        /// </summary>
        public bool ApplyToAllChapters = false;

        /// <summary>
        /// Constructors. Initializes the list box values.
        /// </summary>
        public SetThumbnailsDialog(Chapter chapter)
        {
            InitializeComponent();

            UiLabelChapterTitle.Content = Properties.Resources.Chapter + ": " + chapter.GetTitle();

            UiTbThumbMove.Text = "";
            EnableThumbnailControls(false);
        }

        /// <summary>
        /// Collect the states and exit.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnOk_Click(object sender, RoutedEventArgs e)
        {
            ThumbnailMove = -1;
            if (int.TryParse(UiTbThumbMove.Text, out int moveNo))
            {
                if (moveNo > 0)
                {
                    ThumbnailMove = moveNo;
                    ThumbnailMoveColor = UiRbBlack.IsChecked == true ? PieceColor.Black : PieceColor.White;
                    OverwriteThumbnails = UiCbOverwriteThumb.IsChecked == true;
                }
            }

            ApplyToAllChapters = UiCbAllChapters.IsChecked == true;

            DialogResult = true;
        }

        /// <summary>
        /// Links to the relevant Wiki page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnHelp_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/czbar/ChessForge/wiki/Managing-Chapter");
        }

        /// <summary>
        /// Text changed in the thumbnail move number box
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiTbThumbMove_TextChanged(object sender, TextChangedEventArgs e)
        {
            bool enable = false;

            if (int.TryParse(UiTbThumbMove.Text, out int moveNo))
            {
                if (moveNo > 0)
                {
                    enable = true;
                }
            }
            EnableThumbnailControls(enable);
        }

        /// <summary>
        /// Enable/disable thumbnail group box controls.
        /// </summary>
        /// <param name="enable"></param>
        private void EnableThumbnailControls(bool enable)
        {
            UiRbWhite.IsEnabled = enable;
            UiRbBlack.IsEnabled = enable;
            UiCbOverwriteThumb.IsEnabled = enable;
        }
    }
}
