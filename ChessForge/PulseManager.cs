using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Threading;
using GameTree;

namespace ChessForge
{
    /// <summary>
    /// Manages various actions and states in response to 
    /// a PULSE timer event.
    /// </summary>
    public class PulseManager
    {
        // index of the chapter to bring into view
        private static int _chaperIndexToBringIntoView = -1;

        /// <summary>
        /// Index of the chapter to bring into view.
        /// </summary>
        public static int ChaperIndexToBringIntoView
        {
            set => _chaperIndexToBringIntoView = value;
        }

        // identifies the article that needs to be brought into view
        private static ArticleIdentifier _articleToBringIntoView = new ArticleIdentifier();

        /// <summary>
        /// Handles the PULSE timer event.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        public static void PulseEventHandler(object source, ElapsedEventArgs e)
        {
            WebAccessManager.UpdateWebAccess();
            UpdateEvaluationBar();
            if (_chaperIndexToBringIntoView >= 0)
            {
                AppState.MainWin.BringChapterIntoView(_chaperIndexToBringIntoView);
                _chaperIndexToBringIntoView = -1;
            }
            else if (_articleToBringIntoView.ArticleIndex >= 0)
            {
                AppState.MainWin.BringArticleIntoView(_articleToBringIntoView.ChapterIndex,
                    _articleToBringIntoView.ContentType,
                    _articleToBringIntoView.ArticleIndex);

                _articleToBringIntoView.ArticleIndex = -1;
            }
        }

        /// <summary>
        /// Sets the article that will be brought into view on the next pulse.
        /// </summary>
        /// <param name="chapterIndex"></param>
        /// <param name="contentType"></param>
        /// <param name="articleIndex"></param>
        public static void SetArticleToBringIntoView(int chapterIndex, GameData.ContentType contentType, int articleIndex)
        {
            _articleToBringIntoView.ChapterIndex = chapterIndex;
            _articleToBringIntoView.ContentType = contentType;
            _articleToBringIntoView.ArticleIndex = articleIndex;
        }

        /// <summary>
        /// Updates the position of the evaluation bar.
        /// </summary>
        private static void UpdateEvaluationBar()
        {
            AppState.MainWin.Dispatcher.Invoke(() =>
            {
                if (AppState.IsVariationTreeTabType
                || TrainingSession.IsTrainingInProgress
                   && (EvaluationManager.CurrentMode == EvaluationManager.Mode.LINE || EvaluationManager.CurrentMode == EvaluationManager.Mode.CONTINUOUS)
               )
                {
                    TreeNode nd = null;
                    if (TrainingSession.IsTrainingInProgress)
                    {
                        nd = EvaluationManager.GetEvaluatedNode(out _);
                    }
                    else if (AppState.MainWin.ActiveTreeView != null)
                    {
                        nd = AppState.MainWin.ActiveTreeView.GetSelectedNode();
                        if (nd == null && AppState.MainWin.ActiveVariationTree != null && AppState.MainWin.ActiveVariationTree.Nodes.Count > 0)
                        {
                            nd = AppState.MainWin.ActiveVariationTree.Nodes[0];
                        }
                    }

                    EvaluationBar.ShowEvaluation(nd);
                }
                else
                {
                    EvaluationBar.Show(false);
                }
            });
        }

    }

    /// <summary>
    /// Encapsulates attributes identifying a chapter in the Workbook.
    /// </summary>
    class ArticleIdentifier
    {
        public int ChapterIndex;
        public GameData.ContentType ContentType;
        public int ArticleIndex = -1;
    }
}
