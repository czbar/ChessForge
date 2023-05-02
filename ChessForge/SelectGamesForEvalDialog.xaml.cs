using ChessPosition.GameTree;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using GameTree;
using System.Windows.Shapes;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for SelectGamesDialog.xaml
    /// </summary>
    public partial class SelectGamesForEvalDialog : Window
    {
        /// <summary>
        /// The list of games to process.
        /// </summary>
        private ObservableCollection<ArticleListItem> _gameList;

        /// <summary>
        /// Creates the dialog object. Sets ItemsSource for the ListView
        /// to GamesHeaders list.
        /// This dialog will be invoked in the follwoing contexts:
        /// - Selecting Games and Exercise to create a new Chapter
        /// - Importing Games into a chapter
        /// - Importing Exercises into a chapter
        /// - Creating a new Workbook
        /// </summary>
        public SelectGamesForEvalDialog(Chapter chapter, int chapterIndex, List<Article> articles)
        {
            _gameList = new ObservableCollection<ArticleListItem>();
            for(int i = 0; i < articles.Count; i++)
            {
                ArticleListItem game = new ArticleListItem(chapter, chapterIndex, articles[i], i);
                _gameList.Add(game);
            }
            InitializeComponent();

            double dval = (double)Configuration.EngineEvaluationTime / 1000.0;
            UiTbEngEvalTime.Text = dval.ToString("F1");

            UiLvGames.ItemsSource = _gameList;

            SetInstructionText();
        }

        /// <summary>
        /// Sets the text above the selection list depending on the dialog mode
        /// and content of the game list.
        /// </summary>
        private void SetInstructionText()
        {
            UiLblInstruct.Content = Properties.Resources.SelectGames;
        }

        /// <summary>
        /// SelectAll box was checked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCbSelectAll_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var item in _gameList)
            {
                item.IsSelected = true;
            }
        }

        /// <summary>
        /// SelectAll box was unchecked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCbSelectAll_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (var item in _gameList)
            {
                item.IsSelected = false;
            }
        }

        /// <summary>
        /// OK button was clicked. Exits with the result = true
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnOk_Click(object sender, RoutedEventArgs e)
        {
            int plyCount = 0;
            foreach (ArticleListItem game in _gameList)
            {
                if (game.IsSelected)
                {
                    plyCount += (game.Article.Tree.SelectLine("1").Count - 1);
                }
            }

            if (plyCount > 0)
            {
                if (double.TryParse(UiTbEngEvalTime.Text, out double dval))
                {
                    Configuration.EngineEvaluationTime = (int)(dval * 1000);
                }

                long estTime = plyCount * Configuration.EngineEvaluationTime;
                TimeSpan ts = TimeSpan.FromMilliseconds(estTime);
                bool hasDays = false;
                bool hasHours = false;
                bool hasMinutes = false;

                StringBuilder sb = new StringBuilder();
                sb.Append(Properties.Resources.EstimatedEvalTime);
                sb.Append(": ");

                bool done = false;
                if (ts.Days > 0)
                {
                    sb.Append(Properties.Resources.Days + " " + ts.Days.ToString());
                    hasDays = true;
                    if (ts.Days > 10)
                    {
                        done = true;
                    }
                    if (!done)
                    {
                        sb.Append(", ");
                    }
                }

                if (!done && (hasDays || ts.Hours > 0))
                {
                    sb.Append(Properties.Resources.Hours + " " + ts.Hours.ToString());
                    hasHours = true;
                    if (ts.Hours > 10 || hasDays)
                    {
                        done = true;
                    }
                    if (!done)
                    {
                        sb.Append(", ");
                    }
                }

                if (!done && (hasHours || ts.Minutes > 0))
                {
                    sb.Append(Properties.Resources.Minutes + " " + ts.Minutes.ToString());
                    hasMinutes = true;
                    if (ts.Minutes > 10 || hasHours)
                    {
                        done = true;
                    }
                    if (!done)
                    {
                        sb.Append(", ");
                    }
                }

                if (!done && (hasMinutes || ts.Seconds > 0))
                {
                    sb.Append(Properties.Resources.Seconds + " " + ts.Seconds.ToString());
                }

                sb.Append(". " + Properties.Resources.Proceed);
                MessageBox.Show(sb.ToString(), Properties.Resources.Information, MessageBoxButton.YesNo, MessageBoxImage.Question);

                PerformEvaluations();
            }

            DialogResult = true;
        }

        /// <summary>
        /// Kicks off the evaluation process.
        /// This dialog will close and the games eval process will open a new
        /// one with the progress bar.
        /// </summary>
        private void PerformEvaluations()
        {
            GamesEvaluationManager.InitializeProcess(_gameList);
        }

        /// <summary>
        /// Cancel button was clicked. Exits with the result = false
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
