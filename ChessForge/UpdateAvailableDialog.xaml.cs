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
    /// Interaction logic for UpdateAvailableDialog.xaml
    /// </summary>
    public partial class UpdateAvailableDialog : Window
    {
        Version _ver;

        public UpdateAvailableDialog(Version ver)
        {
            _ver = ver;
            InitializeComponent();
            UiLblPreamble.Content = "New version " + ver.ToString() + " available from:";
        }

        /// <summary>
        /// Points the host's browser to the downalod page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiLblDownloadLink_MouseDown(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start("https://sourceforge.net/projects/chessforge/");
        }

        /// <summary>
        /// Closes the dialog.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnClose_Click(object sender, RoutedEventArgs e)
        {
            if (UiCbDontShowAgain.IsChecked == true)
            {
                Configuration.DoNotShowVersion = _ver.ToString();
                Configuration.WriteOutConfiguration();
            }

            Close();
        }
    }
}
