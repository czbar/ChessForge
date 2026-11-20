using System.Windows;

namespace ChessForge.LibraryContentEditor
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            string initialPath = e.Args.Length > 0 ? e.Args[0] : null;
            LibraryContentEditorDialog editor = new LibraryContentEditorDialog(initialPath);
            MainWindow = editor;
            editor.ShowDialog();
            Shutdown();
        }
    }
}
