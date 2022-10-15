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
    /// Shows info to the user explaining that they are about
    /// to import games from a generic PGN file and merge them into
    /// a single Study tree.
    /// </summary>
    public partial class GenericPgnInfoDialog : Window
    {
        /// <summary>
        /// The dialog return result.
        /// Set to true if the user pressed OK, otherwise false.
        /// </summary>
        public bool ExitOk = false;
        public bool ShowGenericPgnInfo = true;

        /// <summary>
        /// The constructor. Sets the text of the warning.
        /// </summary>
        public GenericPgnInfoDialog()
        {
            InitializeComponent();
            SetMessage();
            UiCbDoNotShow.IsChecked = !Configuration.ShowGenericPgnInfo;
        }

        /// <summary>
        /// Text of the warning to display.
        /// </summary>
        private void SetMessage()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("The requested file is not a Chess Forge\'s workbook.");
            sb.AppendLine("Click the OK button to view the list of games found in the file.");
            sb.AppendLine("");
            sb.AppendLine("You will be able to select games for Chess Forge to merge them into a single Study tree.");
            sb.AppendLine("Later on, you will be able to import other games from the file as Model Games or Exercises.");
            sb.AppendLine("");
            sb.AppendLine("Click the Help button for more info.");
            UiLblInfo.Content = sb.ToString();
        }

        /// <summary>
        /// Exits with a result of true, after the user clicked OK.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnOk_Click(object sender, RoutedEventArgs e)
        {
            ExitOk = true;
            ShowGenericPgnInfo = !(UiCbDoNotShow.IsChecked == true);
            Close();
        }

        /// <summary>
        /// Exits with a result of false, after the user clicked Cancel.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnCancel_Click(object sender, RoutedEventArgs e)
        {
            ExitOk = false;
            Close();
        }

        /// <summary>
        /// Invokes online help for the item.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnHelp_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/czbar/ChessForge/wiki/Opening-a-Workbook");
        }
    }
}
