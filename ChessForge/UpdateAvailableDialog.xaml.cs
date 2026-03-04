using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;

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
        /// Constructor. Takes the new available version number and its source as arguments. 
        /// If the version is null, the dialog is displayed without the version information and download link, 
        /// showing only the message passed to the dialog (if any) as the argument.
        /// Therefore, the dialog can be used to display only a message to the user, only the information about a new version
        /// or both.
        /// </summary>
        /// <param name="ver"></param>
        public UpdateAvailableDialog(Version ver, int updSource, string message)
        {
            _ver = ver;
            _updateSource = updSource;

            InitializeComponent();

            bool showVersion = ver != null;
            bool showMessage = !string.IsNullOrWhiteSpace(message);

            if (showMessage)
            {
                UiRtbMessage.Visibility = Visibility.Visible;
                PopulateMessageRtb(message);
            }
            if (!showVersion)
            {
                UiLblPreamble.Visibility = Visibility.Collapsed;
                UiTbDownloadLink.Visibility = Visibility.Collapsed;
                UiCbDontShowAgain.Visibility = Visibility.Collapsed;
            }

            SetWindowHeight(showVersion, showMessage);

            UiRtbMessage.Margin = new Thickness(20, 0, 20, 60);
            UiBtnClose.Margin = new Thickness(120, 0, 0, 25);

            if (showVersion)
            {
                PopulateVersionControls(ver, updSource);
            }
        }

        /// <summary>
        /// Sets dialog window height based on the content to display.
        /// </summary>
        /// <param name="showVersion"></param>
        /// <param name="showMessage"></param>
        private void SetWindowHeight(bool showVersion, bool showMessage)
        {
            if (showVersion && showMessage)
            {
                this.Height = 295;
            }
            else if (showMessage)
            {
                this.Height = 200;
            }
            else if (showVersion)
            {
                this.Height = 180;
            }
        }

        /// <summary>
        /// Populates the controls related to the new version information: the preamble label and the download link textbox.
        /// </summary>
        /// <param name="ver"></param>
        /// <param name="updSource"></param>
        private void PopulateVersionControls(Version ver, int updSource)
        {
            string sVersion = Properties.Resources.NewVersionAvailable;
            sVersion = sVersion.Replace("$0", ver.ToString());
            UiLblPreamble.Content = sVersion;

            if (updSource == -1)
            {
                UiTbDownloadLink.Text = Properties.Resources.SourceForgeSite;
            }
            else
            {
                UiTbDownloadLink.Text = Properties.Resources.MicrosoftAppStore;
            }
        }

        // prefixes for special url tokens in the message text
        private readonly string _urlCommandPrefix = "url=";

        // prefixes for special style tokens in the message text
        private readonly string _styleCommandPrefix = "style=";

        /// <summary>
        /// Populates the message RichTextBox by parsing the message text for special tokens.
        /// </summary>
        /// <param name="message"></param>
        private void PopulateMessageRtb(string message)
        {
            UiRtbMessage.Document.Blocks.Clear();

            Paragraph para = new Paragraph();

            // all special tokens are between < and >, so split the message by these characters
            string[] tokens = message.Split('<', '>');

            foreach (string token in tokens)
            {
                if (token.StartsWith(_urlCommandPrefix))
                {
                    HandleUrlToken(token, para);
                }
                else if (token.StartsWith(_styleCommandPrefix))
                {
                    HandleStyleToken(token, para);
                }
                else
                {
                    para.Inlines.Add(new Run(token));
                }
            }
            UiRtbMessage.Document.Blocks.Add(para);
        }

        /// <summary>
        /// Processes a url token and adds the corresponding hyperlink to the passed paragraph.
        /// A token has the format url=urlAddress|linkText, 
        /// where urlAddress is the URL to navigate to when the link is clicked, 
        /// and linkText is the text to display for the hyperlink.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="para"></param>
        private void HandleUrlToken(string token, Paragraph para)
        {
            string url = token.Substring(_urlCommandPrefix.Length);
            string[] urlTokens = url.Split('|');
            if (urlTokens.Length < 2)
            {
                para.Inlines.Add(new Run(token));
            }
            else
            {
                Hyperlink link = new Hyperlink(new Run(urlTokens[1]));
                link.Foreground = ChessForgeColors.CurrentTheme.HyperlinkForeground;
                link.NavigateUri = new Uri(urlTokens[0]);
                link.MouseDown += GuiUtilities.EventHyperlinkClicked;
                if (link.Inlines.Count > 0)
                {
                    link.Inlines.FirstInline.Cursor = Cursors.Hand;
                }
                para.Inlines.Add(link);
            }
        }

        /// <summary>
        /// Processes a style token and adds the corresponding styled text to the passed paragraph.
        /// A token has the format style=styleCommands|text, 
        /// where styleCommands is a combination of "b" for bold and "i" for italic, 
        /// and text is the text to be styled.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="para"></param>
        private void HandleStyleToken(string token, Paragraph para)
        {
            string text = token.Substring(_styleCommandPrefix.Length);
            string[] styleTokens = text.Split('|');
            if (styleTokens.Length < 2)
            {
                para.Inlines.Add(new Run(token));
            }
            else
            {
                Run run = new Run(styleTokens[1]);
                if (styleTokens[0].Contains("b"))
                {
                    run.FontWeight = FontWeights.Bold;
                }
                if (styleTokens[0].Contains("i"))
                {
                    run.FontStyle = FontStyles.Italic;
                }
                para.Inlines.Add(run);
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
