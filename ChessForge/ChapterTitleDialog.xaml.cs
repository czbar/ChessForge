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
    /// Interaction logic for ChapterTitleDialog.xaml
    /// </summary>
    public partial class ChapterTitleDialog : Window
    {
        /// <summary>
        /// Title of the chapter as edited in this dialog.
        /// </summary>
        public string ChapterTitle { get; set; }

        /// <summary>
        /// Whether exit occurred on user's pushing the OK button  
        /// </summary>
        public bool ExitOK = false;

        public ChapterTitleDialog(Chapter chapter)
        {
            InitializeComponent();
            UiTbChapterTitle.Text = chapter.Title; 
        }

        private void UiBtnOK_Click(object sender, RoutedEventArgs e)
        {
            ChapterTitle = UiTbChapterTitle.Text;
            ExitOK = true;
            Close();
        }

        private void UiBtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
