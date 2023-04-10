using ChessForge;
using GameTree;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ChessPosition;

namespace ChessForge
{
    /// <summary>
    /// Utilities for handling Finding Identical Positions
    /// </summary>
    public class FindIdenticalPositions
    {
        public enum Mode
        {
            FIND_AND_REPORT,
            CHECK_IF_ANY,
        }

        /// <summary>
        /// Finds positions identical to the one in the current node.
        /// Returns true if any such position found.
        /// If mode is set to CHECK_IF_ANY, this is all it does,
        /// otherwise it pops up an appropriate message or dialog for the user.
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        public static bool Search(TreeNode nd, Mode mode)
        {
            ObservableCollection<ArticleListItem> lstIdenticalPositions = ArticleListBuilder.BuildIdenticalPositionsList(nd, mode == Mode.CHECK_IF_ANY);

            bool anyFound = lstIdenticalPositions.Count > 0;

            if (mode == Mode.FIND_AND_REPORT)
            {
                if (lstIdenticalPositions.Count == 0)
                {
                    MessageBox.Show(Properties.Resources.MsgNoIdenticalPositions, Properties.Resources.ChessForge, MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    IdenticalPositionsExDialog dlgEx = new IdenticalPositionsExDialog(nd, ref lstIdenticalPositions)
                    {
                        Left = AppState.MainWin.ChessForgeMain.Left + 100,
                        Top = AppState.MainWin.ChessForgeMain.Top + 100,
                        Topmost = false,
                        Owner = AppState.MainWin
                    };

                    if (dlgEx.ShowDialog() == true && dlgEx.ArticleIndexId >= 0 && dlgEx.ArticleIndexId < lstIdenticalPositions.Count)
                    {
                        ArticleListItem item = lstIdenticalPositions[dlgEx.ArticleIndexId];
                        List<TreeNode> nodelList = null;
                        switch (dlgEx.Request)
                        {
                            case IdenticalPositionsExDialog.Action.CopyLine:
                                nodelList = TreeUtils.CopyNodeList(item.TailLine);
                                ChfClipboard.HoldNodeList(nodelList);
                                break;
                            case IdenticalPositionsExDialog.Action.CopyTree:
                                nodelList = TreeUtils.CopySubtree(item.TailLine[0]);
                                ChfClipboard.HoldNodeList(nodelList);
                                break;
                            case IdenticalPositionsExDialog.Action.OpenView:
                                // TODO: this should be something encapsulated in TabNavigator
                                AppState.MainWin.SelectArticle(item.ChapterIndex, item.Article.Tree.ContentType, item.ArticleIndex);
                                if (item.Article.Tree.ContentType == GameData.ContentType.STUDY_TREE)
                                {
                                    AppState.MainWin.SetupGuiForActiveStudyTree(true);
                                }
                                AppState.MainWin.SetActiveLine(item.Node.LineId, item.Node.NodeId);
                                AppState.MainWin.ActiveTreeView.SelectLineAndMove(item.Node.LineId, item.Node.NodeId);
                                break;
                        }
                    }
                }
            }

            return anyFound;
        }
    }
}