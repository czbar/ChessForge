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
        // version to report
        private Version _ver;

        // source of the new version: 1 if MS, -1 if SourceForge
        private int _updateSource;

        /// <summary>
        /// Constructor. Takes the new available version number
        /// as the argument
        /// </summary>
        /// <param name="ver"></param>
        public UpdateAvailableDialog(Version ver, int updSource)
        {
            _ver = ver;
            _updateSource = updSource;
            InitializeComponent();
            string s = Properties.Resources.NewVersionAvailable;
            s = s.Replace("$0", ver.ToString());
            UiLblPreamble.Content = s;

            if (updSource == -1)
            {
                UiTbDownloadLink.Text = Properties.Resources.SourceForgeSite;
            }
            else
            {
                UiTbDownloadLink.Text = Properties.Resources.MicrosoftAppStore;
            }
        }

        /// <summary>
        /// Points the host's browser to the download page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiLblDownloadLink_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_updateSource == -1)
            {
                System.Diagnostics.Process.Start("https://sourceforge.net/projects/chessforge/");
            }
            else
            {
                System.Diagnostics.Process.Start("https://apps.microsoft.com/store/detail/chess-forge/XPDC18VV71LM34");
            }
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
