using GameTree;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for ConfirmUndoDialog.xaml
    /// </summary>
    public partial class ConfirmUndoDialog : Window
    {
        // names for different operation types from Resources
        private static Dictionary<WorkbookOperationType, string> _dictOpTypeToTitle;

        // operation being processed
        private WorkbookOperation _operation;

        /// <summary>
        /// Creates the dialog and initializes controls.
        /// </summary>
        /// <param name="operation"></param>
        public ConfirmUndoDialog(WorkbookOperation operation)
        {
            _operation = operation;
            InitializeComponent();

            if (_dictOpTypeToTitle == null)
            {
                InitializeOperationsDictionary();
            }

            SetControlsText();
        }

        /// <summary>
        /// Sets texts in the label and the text box.
        /// </summary>
        private void SetControlsText()
        {
            string opName = GetOperationName();
            UiLblOperation.Content = "    " + Properties.Resources.Undo + ": " + opName + "    ";

            UiTbDetails.Text = GetOperationDetailsText();
        }

        /// <summary>
        /// Gets the display name of the operation.
        /// </summary>
        /// <returns></returns>
        private string GetOperationName()
        {
            string opName = Properties.Resources.OpUnknown;

            if (_dictOpTypeToTitle.ContainsKey(_operation.OpType))
            {
                opName = _dictOpTypeToTitle[_operation.OpType];
            }

            return opName;
        }

        /// <summary>
        /// Gets text to show as details of the operation.
        /// </summary>
        /// <returns></returns>
        private string GetOperationDetailsText()
        {
            StringBuilder sb = new StringBuilder();
            UiTbDetails.TextWrapping = TextWrapping.Wrap;

            try
            {
                switch (_operation.OpType)
                {
                    case WorkbookOperationType.COPY_ARTICLES:
                    case WorkbookOperationType.INSERT_ARTICLES:
                    case WorkbookOperationType.IMPORT_CHAPTERS:
                    case WorkbookOperationType.DELETE_MODEL_GAMES:
                    case WorkbookOperationType.MOVE_ARTICLES_MULTI_CHAPTER:
                    case WorkbookOperationType.MOVE_ARTICLES:
                    case WorkbookOperationType.DELETE_ARTICLES:
                    case WorkbookOperationType.DELETE_EXERCISES:
                        UiTbDetails.TextWrapping = TextWrapping.NoWrap;
                        if (_operation.OpData_1 is List<ArticleListItem> articles)
                        {
                            foreach (ArticleListItem article in articles)
                            {
                                if (article.Chapter != null && article.Article == null)
                                {
                                    sb.Append(Properties.Resources.Chapter + ": ");
                                }
                                sb.Append(article.GameTitleForList.Trim() + '\n');
                            }
                        }
                        break;
                    case WorkbookOperationType.DELETE_CHAPTER:
                    case WorkbookOperationType.SPLIT_CHAPTER:
                    case WorkbookOperationType.CREATE_CHAPTER:
                    case WorkbookOperationType.RENAME_CHAPTER:
                        sb.Append(Properties.Resources.Chapter + ": " + _operation.Chapter.Title);
                        break;
                    case WorkbookOperationType.DELETE_CHAPTERS:
                    case WorkbookOperationType.MERGE_CHAPTERS:
                    case WorkbookOperationType.REGENERATE_STUDIES:
                        UiTbDetails.TextWrapping = TextWrapping.NoWrap;
                        if (_operation.OpData_1 is List<Chapter> chapters)
                        {
                            foreach (Chapter chapter in chapters)
                            {
                                sb.Append(Properties.Resources.Chapter + ": " + chapter.Title + '\n');
                            }
                        }
                        break;
                    case WorkbookOperationType.EDIT_MODEL_GAME_HEADER:
                    case WorkbookOperationType.EDIT_EXERCISE_HEADER:
                    case WorkbookOperationType.IMPORT_LICHESS_GAME:
                    case WorkbookOperationType.CREATE_MODEL_GAME:
                    case WorkbookOperationType.CREATE_EXERCISE:
                        sb.Append(_operation.Article.Tree.Header.BuildGameHeaderLine(true));
                        break;
                    case WorkbookOperationType.DELETE_COMMENTS:
                    case WorkbookOperationType.DELETE_ENGINE_EVALS:
                    case WorkbookOperationType.CLEAN_LINES_AND_COMMENTS:
                        if (_operation.OpData_1 is Dictionary<Article, List<MoveAttributes>> dictUndoData)
                        {
                            int count = 0;
                            foreach (List<MoveAttributes> lst in dictUndoData.Values)
                            {
                                count += lst.Count;
                            }
                            sb.Append("Number of affected moves: " + count.ToString());
                        }
                        break;
                    default:
                        UiTbDetails.TextWrapping = TextWrapping.Wrap;
                        break;
                }
            }
            catch { }

            return sb.ToString();
        }

        /// <summary>
        /// Initializes the dictionary of operation names.
        /// </summary>
        private void InitializeOperationsDictionary()
        {
            _dictOpTypeToTitle = new Dictionary<WorkbookOperationType, string>();
            _dictOpTypeToTitle[WorkbookOperationType.DELETE_CHAPTER] = Properties.Resources.OpDeleteChapter;
            _dictOpTypeToTitle[WorkbookOperationType.DELETE_CHAPTERS] = Properties.Resources.OpDeleteChapters;
            _dictOpTypeToTitle[WorkbookOperationType.MERGE_CHAPTERS] = Properties.Resources.OpMergeChapters;
            _dictOpTypeToTitle[WorkbookOperationType.SPLIT_CHAPTER] = Properties.Resources.OpSplitChapter;
            _dictOpTypeToTitle[WorkbookOperationType.CREATE_CHAPTER] = Properties.Resources.OpCreateChapter;
            _dictOpTypeToTitle[WorkbookOperationType.RENAME_CHAPTER] = Properties.Resources.OpRenameChapter;
            _dictOpTypeToTitle[WorkbookOperationType.IMPORT_LICHESS_GAME] = Properties.Resources.OpImportLichessGame;
            _dictOpTypeToTitle[WorkbookOperationType.CREATE_MODEL_GAME] = Properties.Resources.OpCreateModelGame;
            _dictOpTypeToTitle[WorkbookOperationType.CREATE_EXERCISE] = Properties.Resources.OpCreateExercise;
            _dictOpTypeToTitle[WorkbookOperationType.DELETE_MODEL_GAMES] = Properties.Resources.OpDeleteGames;
            _dictOpTypeToTitle[WorkbookOperationType.DELETE_EXERCISES] = Properties.Resources.OpDeleteExercises;
            _dictOpTypeToTitle[WorkbookOperationType.DELETE_ARTICLES] = Properties.Resources.OpDeleteArticles;
            _dictOpTypeToTitle[WorkbookOperationType.REGENERATE_STUDIES] = Properties.Resources.OpRegenerateStudies;
            _dictOpTypeToTitle[WorkbookOperationType.EDIT_MODEL_GAME_HEADER] = Properties.Resources.OpEditGameHeader;
            _dictOpTypeToTitle[WorkbookOperationType.EDIT_EXERCISE_HEADER] = Properties.Resources.OpEditExerciseHeader;
            _dictOpTypeToTitle[WorkbookOperationType.MOVE_ARTICLES_MULTI_CHAPTER] = Properties.Resources.OpMoveArticles;
            _dictOpTypeToTitle[WorkbookOperationType.MOVE_ARTICLES] = Properties.Resources.OpMoveArticles;
            _dictOpTypeToTitle[WorkbookOperationType.COPY_ARTICLES] = Properties.Resources.OpCopyArticles;
            _dictOpTypeToTitle[WorkbookOperationType.INSERT_ARTICLES] = Properties.Resources.OpInsertArticles;
            _dictOpTypeToTitle[WorkbookOperationType.IMPORT_CHAPTERS] = Properties.Resources.OpImportChapters;
            _dictOpTypeToTitle[WorkbookOperationType.DELETE_COMMENTS] = Properties.Resources.OpDeleteComments;
            _dictOpTypeToTitle[WorkbookOperationType.DELETE_ENGINE_EVALS] = Properties.Resources.OpDeleteEngineEvals;
            _dictOpTypeToTitle[WorkbookOperationType.CLEAN_LINES_AND_COMMENTS] = Properties.Resources.OpDeleteNotes;
            _dictOpTypeToTitle[WorkbookOperationType.SORT_GAMES] = Properties.Resources.OpSortGames;
        }

        /// <summary>
        /// Sets the DialogResult to true and exits.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        /// <summary>
        /// Links to the relevant Wiki page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnHelp_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/czbar/ChessForge/wiki/Confirm-Undo");
        }
    }
}
