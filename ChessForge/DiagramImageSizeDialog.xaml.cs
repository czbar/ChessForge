using ChessPosition;
using System;
using System.Windows;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for DiagramImageSizeDialog.xaml
    /// </summary>
    public partial class DiagramImageSize : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DiagramImageSize"/> class.
        /// </summary>
        public DiagramImageSize()
        {
            InitializeComponent();

            Title = TextUtils.RemoveTrailingElipsis(Title);
            UiTbSideSize.Text = Configuration.DiagramImageSideSize.ToString();
            UiCbDoNotAskAgain.IsChecked = Configuration.DoNotAskDiagramImageSideSize;
        }

        /// <summary>
        /// Handles the click event for the save button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnSave_Click(object sender, RoutedEventArgs e)
        {
            int.TryParse(UiTbSideSize.Text, out int sideSize);
            sideSize = Math.Max(sideSize, 120);
            sideSize = Math.Min(sideSize, 480);
            
            Configuration.DiagramImageSideSize = sideSize;
            Configuration.DoNotAskDiagramImageSideSize = UiCbDoNotAskAgain.IsChecked == true;
            
            DialogResult = true;
        }

        /// <summary>
        /// Handles the click event for the cancel button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
