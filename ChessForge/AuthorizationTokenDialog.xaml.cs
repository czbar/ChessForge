using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using WebAccess;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for AuthorizationTokenDialog.xaml
    /// </summary>
    public partial class AuthorizationTokenDialog : Window
    {
        public AuthorizationTokenDialog()
        {
            InitializeComponent();

            CreatePara_1();
            CreatePara_2();
            CreatePara_3();

            UiTbToken.Text = RestApiRequest.LichessAuthToken;
            UiTbToken.Focus();
            UiTbToken.SelectAll();
        }

        /// <summary>
        /// Create the first Info paragraph with text from resources.
        /// </summary>
        private void CreatePara_1()
        {
            Paragraph para1 = new Paragraph();
            para1.Inlines.Add(new Run(Properties.Resources.AuthTokenInfo_1));
            UiRtbInfo.Document.Blocks.Add(para1);
        }

        /// <summary>
        /// Create the second Info paragraph with text from resources and a hyperlink to lichess token creation page.
        /// </summary>
        private void CreatePara_2()
        {
            Paragraph para2 = new Paragraph();
            para2.Inlines.Add(new Run(Properties.Resources.AuthTokenInfo_2 + " "));

            string urlCreateToken = UrlTarget.LichessCreateAuthToken;
            Hyperlink link = new Hyperlink(new Run(urlCreateToken + "."));
            link.Foreground = ChessForgeColors.CurrentTheme.HyperlinkForeground;
            link.NavigateUri = new Uri(urlCreateToken);
            link.MouseDown += GuiUtilities.EventHyperlinkClicked;
            if (link.Inlines.Count > 0)
            {
                link.Inlines.FirstInline.Cursor = Cursors.Hand;
            }

            para2.Inlines.Add(link);
            UiRtbInfo.Document.Blocks.Add(para2);
        }

        /// <summary>
        /// Create the third Info paragraph with text from resources.
        /// </summary>
        private void CreatePara_3()
        {
            Paragraph para3 = new Paragraph();
            para3.Inlines.Add(new Run(Properties.Resources.AuthTokenInfo_3));
            UiRtbInfo.Document.Blocks.Add(para3);
        }

        /// <summary>
        /// Saves the token and closes the dialog with DialogResult = true. 
        /// The caller can then read the token from OpeningExplorer.LichessAuthToken.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnOk_Click(object sender, RoutedEventArgs e)
        {
            SecureTokenStore.Save(UiTbToken.Text);
            RestApiRequest.LichessAuthToken = UiTbToken.Text;

            Configuration.LichessAuthTokenRequestCount++;
            DialogResult = true;
        }

        /// Closes the dialog with DialogResult = false, without saving the token.
        private void UiBtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        /// <summary>
        /// Links to the relevant Wiki page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnHelp_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(WebAccess.UrlTarget.HelpFolder + "Authorization-Token");
        }
    }
}
