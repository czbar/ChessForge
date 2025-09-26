using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        // file name passed as the first command line parameter
        public static string CmdLineFileName = "";

        // full aplication path and name
        public static string AppFileName;

        // full aplication path
        public static string AppPath;

        /// <summary>
        /// Invoked on application start up.
        /// Processes command line parameters.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            AppFileName = Environment.GetCommandLineArgs()[0];
            AppPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Chess Forge");

            if (e.Args.Length > 0)
            {
                CmdLineFileName = e.Args[0];
            }

            // Register a class-level command binding for all TextBoxes
            CommandManager.RegisterClassCommandBinding(
                typeof(TextBox),
                new CommandBinding(ApplicationCommands.Paste, OnExecutedPaste, OnCanExecutePaste)
            );
        }

        /// <summary>
        /// Allow text pasting.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnCanExecutePaste(object sender, CanExecuteRoutedEventArgs e)
        {
            if (e.Command == ApplicationCommands.Paste)
            {
                e.CanExecute = true;   // Always allow paste if there's text
                e.Handled = true;      // Prevent default determination
            }
        }

        /// <summary>
        /// When pasting text into a TextBox replace all new line characters with spaces.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnExecutedPaste(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Command == ApplicationCommands.Paste)
            {
                if (Clipboard.ContainsText())
                {
                    try
                    {
                        string text = Clipboard.GetText();

                        // Replace newlines with spaces
                        text = text.Replace("\r\n", " ")
                                   .Replace("\n", " ")
                                   .Replace("\r", " ");

                        if (sender is TextBox tb)
                        {
                            tb.SelectedText = text;
                        }
                    }
                    catch { }

                    e.Handled = true;  // Stop default paste
                }
            }
        }
    }
}
