using GameTree;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for AnnotationsDialog.xaml
    /// </summary>
    public partial class AnnotationsDialog : Window
    {
        /// <summary>
        /// Comment for the move for which this dialog was invoked.
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// Combined References
        /// </summary>
        public string References { get; set; }

        /// <summary>
        /// Game/Exercise References
        /// </summary>
        private string _gameExerciseRefs = "";

        /// <summary>
        /// Chapter References
        /// </summary>
        private string _chapterRefs = "";

        /// <summary>
        /// Quiz points
        /// </summary>
        public int QuizPoints { get; set; }

        /// <summary>
        /// NAGs string for the position.
        /// </summary>
        public string Nags { get; set; }

        /// <summary>
        /// Whether we are editing an Exercise and therefore should allow
        /// editing quiz points.
        /// </summary>
        private bool _isExerciseEditing = false;

        /// <summary>
        /// Node for which the comment is being handled.
        /// </summary>
        private TreeNode _node;

        /// <summary>
        /// Constructs the dialog.
        /// Sets the values passed by the caller.
        /// </summary>
        /// <param name="ass"></param>
        /// <param name="comment"></param>
        public AnnotationsDialog(TreeNode nd)
        {
            _node = nd;

            InitializeComponent();
            SetPositionButtons(nd.Nags);
            SetMoveButtons(nd.Nags);

            UiGbReferences.Header += " (" + Properties.Resources.ClickToEdit + ")";
            UiGbSeeChapter.Header += " (" + Properties.Resources.ClickToEdit + ")";

            UiTbComment.Text = nd.Comment ?? "";
            UiTbComment.Focus();
            UiTbComment.SelectAll();

            Nags = nd.Nags;

            QuizPoints = nd.QuizPoints;
            _isExerciseEditing = AppState.CurrentSolvingMode == VariationTree.SolvingMode.EDITING;
            if (_isExerciseEditing)
            {
                UiTbQuizPoints.Text = nd.QuizPoints == 0 ? "" : nd.QuizPoints.ToString();
            }
            else
            {
                UiGbQuizPoints.Visibility = Visibility.Collapsed;
                UiTbQuizPoints.Visibility = Visibility.Collapsed;

                MoveButtonHporizontally(UiBtnOk, -50);
                MoveButtonHporizontally(UiBtnCancel, -50);
                MoveButtonHporizontally(UiBtnHelp, -50);
            }

            SplitReferencesString(_node.References);

            SetRefsLabelContent(_gameExerciseRefs, UiLblGameExerciseRefs);
            SetRefsLabelContent(_chapterRefs, UiLblGameExerciseRefs);

            UiTbComment.Focus();
        }

        /// <summary>
        /// Splits a '|' separated list of references into Game/Exercise refs
        /// and Chapter refs.
        /// </summary>
        /// <param name="refs"></param>
        private void SplitReferencesString(string refs)
        {
            StringBuilder sbGameExerciseRefs = new StringBuilder();
            StringBuilder sbChapterRefs = new StringBuilder();

            List<Article> lstRefs = GuiUtilities.BuildReferencedArticlesList(refs);
            if (lstRefs != null && lstRefs.Count > 0)
            {
                bool firstArticle = true;
                bool firstChapter = true;

                foreach (Article article in lstRefs)
                {
                    if (article.ContentType == GameData.ContentType.MODEL_GAME || article.ContentType == GameData.ContentType.EXERCISE)
                    {
                        if (!firstArticle)
                        {
                            sbGameExerciseRefs.Append("; ");
                        }
                        sbGameExerciseRefs.Append(article.Tree.Header.BuildGameReferenceTitle(true));
                        firstArticle = false;
                    }
                    else if (article.ContentType == GameData.ContentType.STUDY_TREE)
                    {
                        if (!firstChapter)
                        {
                            sbChapterRefs.Append("; ");
                        }
                        Chapter chapter = AppState.Workbook.GetChapterByGuid(article.Guid, out int index);
                        sbChapterRefs.Append(chapter.TitleWithNumber);
                        firstChapter = false;
                    }
                }
            }
            _gameExerciseRefs = sbGameExerciseRefs.ToString();
            _chapterRefs = sbChapterRefs.ToString();
        }

        /// <summary>
        /// Sets the text for a reference label
        /// </summary>
        private void SetRefsLabelContent(string refs, Label lblRefs)
        {
            StringBuilder sbRefs = new StringBuilder();

            List<Article> lstRefs = GuiUtilities.BuildReferencedArticlesList(refs);
            if (lstRefs != null && lstRefs.Count > 0)
            {
                bool firstArticle = true;

                foreach (Article article in lstRefs)
                {
                    if (!firstArticle)
                    {
                        sbRefs.Append("; ");
                    }

                    if (article.ContentType == GameData.ContentType.STUDY_TREE)
                    {
                        Chapter chapter = AppState.Workbook.GetChapterByGuid(article.Guid, out int index);
                        sbRefs.Append(chapter.TitleWithNumber);
                    }
                    else
                    {
                        sbRefs.Append(article.Tree.Header.BuildGameReferenceTitle(true));
                    }

                    firstArticle = false;
                }
            }
            lblRefs.Content = sbRefs.ToString();
        }

        /// <summary>
        /// Moves a Button control horizontally.
        /// </summary>
        /// <param name="btn"></param>
        /// <param name="shift"></param>
        private void MoveButtonHporizontally(Button btn, double shift)
        {
            btn.HorizontalAlignment = HorizontalAlignment.Left;
            btn.Margin = new Thickness(btn.Margin.Left + shift, btn.Margin.Top, btn.Margin.Right, btn.Margin.Bottom);
        }

        /// <summary>
        /// Sets the text ("Content") for the position buttons
        /// and selects one if the passed string contains a relevant NAG.
        /// The position NAGs have ids in the 11-19 range.
        /// </summary>
        private void SetPositionButtons(string nags)
        {
            UiRbWhiteWin.Content = "+-";
            UiRbWhiteBetter.Content = '\u00B1'.ToString();
            UiRbWhiteEdge.Content = '\u2A72'.ToString();
            UiRbUnclear.Content = '\u221E'.ToString();
            UiRbEqual.Content = "=";
            UiRbBlackEdge.Content = '\u2A71'.ToString();
            UiRbBlackBetter.Content = '\u2213'.ToString();
            UiRbBlackWin.Content = "-+";

            if (!string.IsNullOrEmpty(nags))
            {
                int id = NagUtils.GetNagId(11, 19, nags);
                if (id != 0)
                {
                    SelectPositionButton(id);
                }
            }
        }


        /// <summary>
        /// Checks the radio button coresponding to the Move NAG value.
        /// </summary>
        private void SetMoveButtons(string nags)
        {
            if (!string.IsNullOrEmpty(nags))
            {
                int id = NagUtils.GetNagId(1, 6, nags);
                if (id != 0)
                {
                    SelectMoveButton(id);
                }
            }
        }

        /// <summary>
        /// Selects the button according to the value of the passed NagId.
        /// </summary>
        /// <param name="nagId"></param>
        private void SelectPositionButton(int nagId)
        {
            switch (nagId)
            {
                case 11:
                    UiRbEqual.IsChecked = true;
                    break;
                case 12:
                    UiRbEqual.IsChecked = true;
                    break;
                case 13:
                    UiRbUnclear.IsChecked = true;
                    break;
                case 14:
                    UiRbWhiteEdge.IsChecked = true;
                    break;
                case 15:
                    UiRbBlackEdge.IsChecked = true;
                    break;
                case 16:
                    UiRbWhiteBetter.IsChecked = true;
                    break;
                case 17:
                    UiRbBlackBetter.IsChecked = true;
                    break;
                case 18:
                    UiRbWhiteWin.IsChecked = true;
                    break;
                case 19:
                    UiRbBlackWin.IsChecked = true;
                    break;
            }
        }

        /// <summary>
        /// Selects the button according to the value of the passed NagId.
        /// </summary>
        /// <param name="nagId"></param>
        private void SelectMoveButton(int nagId)
        {
            switch (nagId)
            {
                case 1:
                    UiRbGood.IsChecked = true;
                    break;
                case 2:
                    UiRbMistake.IsChecked = true;
                    break;
                case 3:
                    UiRbGreat.IsChecked = true;
                    break;
                case 4:
                    UiRbBlunder.IsChecked = true;
                    break;
                case 5:
                    UiRbInteresting.IsChecked = true;
                    break;
                case 6:
                    UiRbDubious.IsChecked = true;
                    break;
            }
        }

        /// <summary>
        /// Converts the content of the Quiz Points text box into an integer.
        /// </summary>
        /// <param name="pts"></param>
        /// <returns></returns>
        private int ParseQuizPoints(string pts)
        {
            if (string.IsNullOrWhiteSpace(pts))
            {
                return 0;
            }
            else
            {
                if (int.TryParse(pts, out int quizPoints))
                {
                    if (quizPoints > 100 || quizPoints < 0)
                    {
                        return 0;
                    }
                    else
                    {
                        return quizPoints;
                    }
                }
                else
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// Builds the NAGs string based on the selected
        /// buttons
        /// </summary>
        private void BuildNagsString()
        {
            // move evaluation first
            Nags = "";
            if (UiRbGood.IsChecked == true)
            {
                Nags += " " + "$1";
            }
            else if (UiRbMistake.IsChecked == true)
            {
                Nags += " " + "$2";
            }
            else if (UiRbGreat.IsChecked == true)
            {
                Nags += " " + "$3";
            }
            else if (UiRbBlunder.IsChecked == true)
            {
                Nags += " " + "$4";
            }
            else if (UiRbInteresting.IsChecked == true)
            {
                Nags += " " + "$5";
            }
            else if (UiRbDubious.IsChecked == true)
            {
                Nags += " " + "$6";
            }

            // append position evaluation
            if (UiRbEqual.IsChecked == true)
            {
                Nags += " " + "$11";
            }
            else if (UiRbUnclear.IsChecked == true)
            {
                Nags += " " + "$13";
            }
            else if (UiRbWhiteEdge.IsChecked == true)
            {
                Nags += " " + "$14";
            }
            else if (UiRbBlackEdge.IsChecked == true)
            {
                Nags += " " + "$15";
            }
            else if (UiRbWhiteBetter.IsChecked == true)
            {
                Nags += " " + "$16";
            }
            else if (UiRbBlackBetter.IsChecked == true)
            {
                Nags += " " + "$17";
            }
            else if (UiRbWhiteWin.IsChecked == true)
            {
                Nags += " " + "$18";
            }
            else if (UiRbBlackWin.IsChecked == true)
            {
                Nags += " " + "$19";
            }
        }

        /// <summary>
        /// Clears the Move button selection.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnClearMove_Click(object sender, RoutedEventArgs e)
        {
            UiRbGood.IsChecked = false;
            UiRbMistake.IsChecked = false;
            UiRbGreat.IsChecked = false;
            UiRbBlunder.IsChecked = false;
            UiRbInteresting.IsChecked = false;
            UiRbDubious.IsChecked = false;
        }

        /// <summary>
        /// Clears the Position button selection.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnClearPosition_Click(object sender, RoutedEventArgs e)
        {
            UiRbEqual.IsChecked = false;
            UiRbUnclear.IsChecked = false;
            UiRbWhiteEdge.IsChecked = false;
            UiRbBlackEdge.IsChecked = false;
            UiRbWhiteBetter.IsChecked = false;
            UiRbBlackBetter.IsChecked = false;
            UiRbWhiteWin.IsChecked = false;
            UiRbBlackWin.IsChecked = false;
        }

        /// <summary>
        /// Handles the key down event in the text box. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiTbComment_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (GuiUtilities.InsertFigurine(UiTbComment, sender, e))
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// Same as clicking the label.
        /// Invokes chapters references editor.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiLblChapter_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var dlg = new SelectChapterRefsDialog(_chapterRefs);
            GuiUtilities.PositionDialog(dlg, AppState.MainWin, 100);

            if (dlg.ShowDialog() == true)
            {
                _chapterRefs = dlg.ChapterRefs ?? "";
                SetRefsLabelContent(_chapterRefs, UiLblChapterRefs);
            }

            e.Handled = true;
        }

        /// <summary>
        /// Invokes games/exercises references editor.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiLblReferences_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var dlg = new SelectArticleRefsDialog(_node);
            GuiUtilities.PositionDialog(dlg, AppState.MainWin, 100);

            if (dlg.ShowDialog() == true)
            {
                _gameExerciseRefs = dlg.GameExerciseRefs ?? "";
                SetRefsLabelContent(_gameExerciseRefs, UiLblGameExerciseRefs);
            }

            e.Handled = true;
        }

        /// <summary>
        /// Same as clicking the label.
        /// Invokes games/exercises references editor.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiGbReferences_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            UiLblReferences_MouseLeftButtonDown(sender, e);
        }

        /// <summary>
        /// Invokes chapter references editor.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiGbSeeChapter_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            UiLblChapter_MouseLeftButtonDown(sender, e);
        }

        /// <summary>
        /// Combines the GameExercise and Chapter references strings.
        /// </summary>
        private void CombineRefs()
        {
            References = _gameExerciseRefs;
            if (_gameExerciseRefs.Length > 0 && _chapterRefs.Length > 0)
            {
                References += "|";
            }
            References += _chapterRefs;
        }

        /// <summary>
        /// Closes the dialog after user pushed the OK button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnOk_Click(object sender, RoutedEventArgs e)
        {
            Comment = UiTbComment.Text;
            CombineRefs();
            if (_isExerciseEditing)
            {
                QuizPoints = ParseQuizPoints(UiTbQuizPoints.Text);
            }
            BuildNagsString();
            DialogResult = true;
        }

        /// <summary>
        /// Closes the dialog after user pushed the Cancel button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
            System.Diagnostics.Process.Start("https://github.com/czbar/ChessForge/wiki/Annotation-Editor");
        }

    }
}

