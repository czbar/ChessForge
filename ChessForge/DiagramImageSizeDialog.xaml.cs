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
            UiTbSideSize.Text = Configuration.DiagramImageSize.ToString();
            UiCbDoNotAskAgain.IsChecked = Configuration.DoNotAskDiagramImageSize;
            UiLblMinMax.Content = "(" + Properties.Resources.Min + " " + Constants.MIN_DIAGRAM_SIZE 
                                    + " - " + Properties.Resources.Max + " " + Constants.MAX_DIAGRAM_SIZE + ")";
        }

        /// <summary>
        /// Handles the click event for the save button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnSave_Click(object sender, RoutedEventArgs e)
        {
            int.TryParse(UiTbSideSize.Text, out int sideSize);
            sideSize = Math.Max(sideSize, Constants.MIN_DIAGRAM_SIZE);
            sideSize = Math.Min(sideSize, Constants.MAX_DIAGRAM_SIZE);
            
            Configuration.DiagramImageSize = sideSize;
            Configuration.DoNotAskDiagramImageSize = UiCbDoNotAskAgain.IsChecked == true;
            
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
