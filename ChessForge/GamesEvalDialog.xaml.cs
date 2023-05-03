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

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for GamesEvalDialog.xaml
    /// </summary>
    public partial class GamesEvalDialog : Window
    {
        public GamesEvalDialog(int plyCountToEvaluate, long estEvaluationTime)
        {
            InitializeComponent();
        }

        private void UiBtnCancel_Click(object sender, RoutedEventArgs e)
        {
            AppState.MainWin.StopEvaluation(true);
            DialogResult = false;
        }
    }
}
