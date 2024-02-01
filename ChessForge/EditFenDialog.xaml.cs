using GameTree;
using ChessPosition;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for ChapterTitleDialog.xaml
    /// </summary>
    public partial class EditFenDialog : Window
    {
        /// <summary>
        /// Board position to be created from FEN edited in this dialog.
        /// </summary>
        public BoardPosition DlgBoardPosition = new BoardPosition();

        /// <summary>
        /// Constructor.
        /// Sets the text to the current title of the chapter.
        /// </summary>
        public EditFenDialog()
        {
            InitializeComponent();
            UiTbFen.Text = PositionUtils.GetFenFromClipboard();
            UiTbFen.Focus();
            UiTbFen.SelectAll();
        }

        /// <summary>
        /// Set the title property and Exit.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnOK_Click(object sender, RoutedEventArgs e)
        {
            bool result = true;
            try
            {
                FenParser.ParseFenIntoBoard(UiTbFen.Text, ref DlgBoardPosition);
            }
            catch
            {
                result = false;
            }

            if (result)
            {
                DialogResult = true;
            }
            else
            {
                MessageBox.Show(Properties.Resources.InvalidFen, Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
    }
}
