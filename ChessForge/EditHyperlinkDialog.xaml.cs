using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for EditHyperlinkDialog.xaml
    /// </summary>
    public partial class EditHyperlinkDialog : Window
    {
        /// <summary>
        /// Flags whether the URL should be deleted
        /// because the user cleared the Uri field.
        /// </summary>
        public bool DeleteUrl = false;

        /// <summary>
        /// Run with the text for the hyperlink.
        /// </summary>
        public Run HyperlinkRun
        {
            get => _runLink;
        }

        // Hyperlink being edited
        private Hyperlink _hyperlink;

        // Run with the text for the hyperlink
        private Run _runLink;

        /// <summary>
        /// Constructor.
        /// </summary>
        public EditHyperlinkDialog(Hyperlink hyperlink, string txt)
        {
            InitializeComponent();
            _hyperlink = hyperlink;

            if (_hyperlink != null)
            {
                UiTbUrl.Text = _hyperlink.NavigateUri.ToString();
                if (_hyperlink.Inlines.Count > 0)
                {
                    _runLink = _hyperlink.Inlines.FirstInline as Run;
                    if (_runLink != null)
                    {
                        UiTbText.Text = _runLink.Text;
                    }
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(txt))
                {
                    UiTbText.Text = txt;
                }
            }

            UiTbText.Focus();
            UiTbText.SelectAll();
        }

        /// <summary>
        /// The user clicked OK.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnOk_Click(object sender, RoutedEventArgs e)
        {
            bool exit = true;

            if (string.IsNullOrWhiteSpace(UiTbUrl.Text))
            {
                DeleteUrl = true;
            }
            else
            {
                try
                {
                    Uri uri = new Uri(UiTbUrl.Text);
                }
                catch
                {
                    MessageBox.Show(Properties.Resources.MsgNeedValidUrlOrEmpty, Properties.Resources.Information, MessageBoxButton.OK, MessageBoxImage.Stop);
                    exit = false;
                }
            }

            if (exit)
            {
                if (string.IsNullOrEmpty(UiTbText.Text))
                {
                    UiTbText.Text = UiTbUrl.Text;
                }
                DialogResult = true;
            }
        }
    }
}
