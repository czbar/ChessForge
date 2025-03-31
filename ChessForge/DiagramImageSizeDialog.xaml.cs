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
        /// The selected color set for the diagram.
        /// </summary>
        private int _selectedColorSet = 1;

        /// <summary>
        /// The maximum number of color sets.
        /// </summary>
        private const int MAX_COLOR_SET = 6;

        /// <summary>
        /// Initializes a new instance of the <see cref="DiagramImageSize"/> class.
        /// </summary>
        public DiagramImageSize()
        {
            InitializeComponent();

            Title = TextUtils.RemoveTrailingElipsis(Title);
            UiTbSideSize.Text = CorrectSideSize(Configuration.DiagramImageSize).ToString();
            UiTbBorderWidth.Text = CorrectBorderWidth(Configuration.DiagramImageBorderWidth).ToString();

            UiCbDoNotAskAgain.IsChecked = Configuration.DoNotAskDiagramImageSize;
            UiLblSizeMinMax.Content = "(" + Properties.Resources.Min + " " + Constants.MIN_DIAGRAM_SIZE 
                                    + " - " + Properties.Resources.Max + " " + Constants.MAX_DIAGRAM_SIZE + ")";
            UiLblBorderMinMax.Content = "( 0"
                                    + " - " + Constants.MAX_DIAGRAM_IMAGE_BORDER_WIDTH + ")";
            SelectColorSet();

        }

        /// <summary>
        /// Selects the color set based on the configuration.
        /// </summary>
        private void SelectColorSet()
        {
            _selectedColorSet = Math.Min(MAX_COLOR_SET, Configuration.DiagramImageColors);
            _selectedColorSet = Math.Max(1, _selectedColorSet);

            switch (_selectedColorSet)
            {
                case 1:
                    UiImgColors_1_MouseDown(null, null);
                    break;
                case 2:
                    UiImgColors_2_MouseDown(null, null);
                    break;
                case 3:
                    UiImgColors_3_MouseDown(null, null);
                    break;
                case 4:
                    UiImgColors_4_MouseDown(null, null);
                    break;
                case 5:
                    UiImgColors_5_MouseDown(null, null);
                    break;
                case 6:
                    UiImgColors_6_MouseDown(null, null);
                    break;
                default:
                    UiImgColors_1_MouseDown(null, null);
                    break;
            }
        }

        /// <summary>
        /// Corrects the border width if outside bounds.
        /// </summary>
        /// <param name="borderWidth"></param>
        /// <returns></returns>
        private int CorrectBorderWidth(int borderWidth)
        {
            return Math.Max(0, Math.Min(borderWidth, Constants.MAX_DIAGRAM_IMAGE_BORDER_WIDTH));
        }

        /// <summary>
        /// Corrects the side size if outside bounds.
        /// </summary>
        /// <param name="sideSize"></param>
        /// <returns></returns>
        private int CorrectSideSize(int sideSize)
        {
            return Math.Max(Constants.MIN_DIAGRAM_SIZE, Math.Min(sideSize, Constants.MAX_DIAGRAM_SIZE));
        }
        /// <summary>
        /// Handles the click event for the color set buttons.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImgColors_1_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            UiImgColorsSelector.Margin = new Thickness(UiImgColors_1.Margin.Left - 1, UiImgColors_1.Margin.Top - 1,
                                                       UiImgColors_1.Margin.Right + 1, UiImgColors_1.Margin.Bottom + 1);
            _selectedColorSet = 1;
        }

        /// <summary>
        /// Handles the click event for the color set buttons.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImgColors_2_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            UiImgColorsSelector.Margin = new Thickness(UiImgColors_2.Margin.Left - 1, UiImgColors_2.Margin.Top - 1,
                                                       UiImgColors_2.Margin.Right + 1, UiImgColors_2.Margin.Bottom + 1);
            _selectedColorSet = 2;
        }

        /// <summary>
        /// Handles the click event for the color set buttons.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImgColors_3_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            UiImgColorsSelector.Margin = new Thickness(UiImgColors_3.Margin.Left - 1, UiImgColors_3.Margin.Top - 1,
                                                       UiImgColors_3.Margin.Right + 1, UiImgColors_3.Margin.Bottom + 1);
            _selectedColorSet = 3;
        }

        /// <summary>
        /// Handles the click event for the color set buttons.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImgColors_4_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            UiImgColorsSelector.Margin = new Thickness(UiImgColors_4.Margin.Left - 1, UiImgColors_4.Margin.Top - 1,
                                                       UiImgColors_4.Margin.Right + 1, UiImgColors_4.Margin.Bottom + 1);
            _selectedColorSet = 4;
        }

        /// <summary>
        /// Handles the click event for the color set buttons.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImgColors_5_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            UiImgColorsSelector.Margin = new Thickness(UiImgColors_5.Margin.Left - 1, UiImgColors_5.Margin.Top - 1,
                                                       UiImgColors_5.Margin.Right + 1, UiImgColors_5.Margin.Bottom + 1);
            _selectedColorSet = 5;
        }

        /// <summary>
        /// Handles the click event for the color set buttons.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImgColors_6_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            UiImgColorsSelector.Margin = new Thickness(UiImgColors_6.Margin.Left - 1, UiImgColors_6.Margin.Top - 1,
                                                       UiImgColors_6.Margin.Right + 1, UiImgColors_6.Margin.Bottom + 1);
            _selectedColorSet = 6;
        }

        /// <summary>
        /// Handles the click event for the save button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnSave_Click(object sender, RoutedEventArgs e)
        {
            int.TryParse(UiTbSideSize.Text, out int sideSize);
            sideSize = CorrectSideSize(sideSize);            
            Configuration.DiagramImageSize = sideSize;
            
            int.TryParse(UiTbBorderWidth.Text, out int borderWidth);
            borderWidth = CorrectBorderWidth(borderWidth);
            Configuration.DiagramImageBorderWidth = borderWidth;

            Configuration.DiagramImageColors = _selectedColorSet;
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

        /// <summary>
        /// Links to the relevant Wiki page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnHelp_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/czbar/ChessForge/wiki/Save-Diagram");
        }
    }
}
