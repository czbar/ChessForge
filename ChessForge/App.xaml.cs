using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        // file name passed as the first command line parameter
        public static string CmdLineFileName = "";

        /// <summary>
        /// Invoked on application start up.
        /// Processes command line parameters.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (e.Args.Length > 0)
            {
                CmdLineFileName = e.Args[0];
            }
        }
    }
}
