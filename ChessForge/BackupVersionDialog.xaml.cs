using System.Windows;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for BackupVersionDialog.xaml
    /// </summary>
    public partial class BackupVersionDialog : Window
    {
        /// <summary>
        /// The path to which the backup will be saved.
        /// </summary>
        public string BackupPath
        {
            get => _backupPath;
        }

        /// <summary>
        /// The new version of the Workbook after being incremented
        /// </summary>
        public string IncrementedVersion
        {
            get => _incrementedVersion;
        }

        // version after increment
        private string _incrementedVersion;

        // path to back up the workbook to
        private string _backupPath;

        // reference to the Workbook object
        private Workbook _workbook;

        /// <summary>
        /// Creates the dialog.
        /// </summary>
        /// <param name="workbook"></param>
        public BackupVersionDialog(Workbook workbook)
        {
            InitializeComponent();
            _workbook = workbook;
            SetLabelsText();
        }

        /// <summary>
        /// Exit after user pressed OK.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        /// <summary>
        /// Exit after user pressed Cancel.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        /// <summary>
        /// Sets texts in the dialog.
        /// </summary>
        private void SetLabelsText()
        {
            string txt = Properties.Resources.CurrentVersion;
            txt = txt.Replace("$0", _workbook.Version.ToString());
            UiLblBackupNotification.Content = txt;

            // this will be ".pgn" but we call this method for future proofing
            string ext = System.IO.Path.GetExtension(AppState.WorkbookFilePath);

            string workbookPathNoExt = AppState.WorkbookFilePath.Substring(0, AppState.WorkbookFilePath.Length - ext.Length);

            _backupPath = workbookPathNoExt + " v" + _workbook.Version.ToString().Replace('.', '_') + ext;
            UiTbBackupFileName.Text = System.IO.Path.GetFileName(_backupPath);
            UiTbBackupFileName.ToolTip = _backupPath;

            SetNewVersionLabel();
        }

        /// <summary>
        /// Sets text of the new version Label.
        /// </summary>
        private void SetNewVersionLabel()
        {
            UiLblWorkbookNewVersion.Content = Properties.Resources.UpdatedVersion + " " + IncrementVersionNumber();
        }

        /// <summary>
        /// Increments the version number.
        /// Increments the major or the minor version number depending
        /// on the state of the MajorVersion check box
        /// </summary>
        /// <returns></returns>
        private string IncrementVersionNumber()
        {
            bool isMajorUpdate = UiCbMajorVersion.IsChecked == true;
            uint major = _workbook.Version.Major;
            uint minor = _workbook.Version.Minor;

            if (isMajorUpdate)
            {
                major++;
                minor = 0;
            }
            else
            {
                minor++;
            }

            _incrementedVersion = major + "." + minor;
            return _incrementedVersion;
        }

        /// <summary>
        /// The user checked the MajorVersion box
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCbMajorVersion_Unchecked(object sender, RoutedEventArgs e)
        {
            SetNewVersionLabel();
        }

        /// <summary>
        /// The user checked the MinorVersion box
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCbMajorVersion_Checked(object sender, RoutedEventArgs e)
        {
            SetNewVersionLabel();
        }
    }
}
