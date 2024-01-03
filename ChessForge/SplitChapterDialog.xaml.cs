using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
    /// Interaction logic for SplitChapterDialog.xaml
    /// </summary>
    public partial class SplitChapterDialog : Window
    {
        public static bool LastSortBy;
        public static bool LastGranularity;

        /// <summary>
        /// Sets default selection and visibility
        /// </summary>
        public SplitChapterDialog()
        {
            InitializeComponent();

            UiRbEco.IsChecked = true;
            UiRbAtoE.IsChecked = true;

            UiGbEcoCrit.Visibility = Visibility.Visible;
            UiGbDateCrit.Visibility = Visibility.Collapsed;
        }
    }
}
