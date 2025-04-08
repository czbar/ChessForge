using System.Windows;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for WaitDialog.xaml
    /// </summary>
    public partial class WaitDialog : Window
    {
        public WaitDialog(string operation)
        {
            InitializeComponent();

            UiLblProcess.Content = operation;
            UiLblPleaseWait.Content = Properties.Resources.PleaseWait + "...";
        }
    }
}
