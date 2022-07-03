using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Threading;
using ChessForge;
using ChessPosition;
using GameTree;
using System.Windows.Media.Animation;
using Microsoft.Win32;
using System.IO;

namespace ChessForge
{
    /// <summary>
    /// Functions handling the Engine Game view (DataGrid)
    /// </summary>
    public partial class MainWindow : Window
    {
        private void ShowEngineGameGuiControls()
        {
            _dgEngineGame.Visibility = Visibility.Visible;
            _lblGameInProgress.Visibility = Visibility.Visible;
            _dgActiveLine.Visibility = Visibility.Hidden;
            _dgActiveLine.IsEnabled = false;
        }

        private void HideEngineGameGuiControls()
        {
            _dgEngineGame.Visibility = Visibility.Hidden;
            _lblGameInProgress.Visibility = Visibility.Hidden;
            _dgActiveLine.Visibility = Visibility.Visible;
            _dgActiveLine.IsEnabled = true;
        }

    }
}
