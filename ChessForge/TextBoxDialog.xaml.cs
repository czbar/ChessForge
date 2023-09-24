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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for TextBoxDialog.xaml
    /// </summary>
    public partial class TextBoxDialog : Window
    {
        public TextBoxDialog(string title, string content)
        {
            InitializeComponent();
            this.Title = title;
            UiTextBox.Text = content;
            UiTextBox.Focus();
        }

        private void UiBtnOk_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
