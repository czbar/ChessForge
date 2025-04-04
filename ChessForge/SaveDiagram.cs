using ChessPosition;
using GameTree;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace ChessForge
{
    public class SaveDiagram
    {
        /// <summary>
        /// Saves all diagrams found in the requested scope as images.
        /// </summary>
        public static void SaveDiagramsAsImages()
        {
            try
            {
                // determine the required scope
                OperationScopeDialog scopeDlg = new OperationScopeDialog(Properties.Resources.SaveDiagramsAsImages, OperationScopeDialog.ScopedAction.SAVE_DIAGRAM);
                GuiUtilities.PositionDialog(scopeDlg, AppState.MainWin, 100);
                if (scopeDlg.ShowDialog() == true)
                {
                    List<TreeNode> lstDiagsToSave = null;

                    // collect the configuration
                    if (scopeDlg.ApplyScope == OperationScope.ACTIVE_ITEM)
                    {
                        VariationTree tree = AppState.MainWin.ActiveVariationTree;
                        lstDiagsToSave = new List<TreeNode>();
                        GetDiagramListFromTree(tree, lstDiagsToSave);
                    }
                    else
                    {
                        lstDiagsToSave = GetDiagramList(scopeDlg.ApplyScope, scopeDlg.ApplicableViews);
                    }

                    // proceed according to whether we got 0, 1 or more results
                    int count = lstDiagsToSave.Count;
                    if (count == 0)
                    {
                        // show message that no diagrams were found
                        MessageBox.Show(Properties.Resources.MsgNoDiagramsFound, Properties.Resources.SaveDiagramsAsImages, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    }
                    else if (count == 1)
                    {
                        // perform the save operation as for a single diagram
                        SaveAsImage(lstDiagsToSave[0], lstDiagsToSave[0].IsDiagramFlipped);
                    }
                    else
                    {
                        // allow the user to select the first file name
                        // we will then generate the rest of the file names
                        // as we save them
                        string filePath = GetUserSelectedFileName(true);

                        if (!string.IsNullOrEmpty(filePath) && CollectConfiguration())
                        {
                            if (!string.IsNullOrEmpty(filePath))
                            {
                                Mouse.SetCursor(Cursors.Wait);

                                string directory = Path.GetDirectoryName(filePath);
                                string fileName = Path.GetFileNameWithoutExtension(filePath);
                                string baseName = ExtractBaseName(fileName);

                                for (int i = 0; i < count; i++)
                                {
                                    fileName = GetNextAvailableFileName(directory, baseName);
                                    filePath = Path.Combine(directory, fileName);
                                    PositionImageGenerator.SaveDiagramAsImage(lstDiagsToSave[i], 
                                                                              lstDiagsToSave[i].IsDiagramFlipped, 
                                                                              filePath, 
                                                                              Configuration.DiagramImageSize);
                                }

                                string message = Properties.Resources.FlMsgNumberOfDiagramsSaved + ": " + count.ToString();
                                AppState.MainWin.BoardCommentBox.ShowFlashAnnouncement(message, CommentBox.HintType.INFO);
                                Mouse.SetCursor(Cursors.Arrow);
                            }
                        }                     
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                AppLog.Message("SaveDiagramsAsImages()", ex);

                Mouse.SetCursor(Cursors.Arrow);
            }
        }

        /// <summary>
        /// Saves the diagram as an image.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void SaveDiagramAsImage(bool requestFromMainBoard)
        {
            try
            {
                TreeNode nd = AppState.ActiveVariationTree.SelectedNode;
                if (nd != null)
                {
                    if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
                    {
                        Configuration.DoNotAskDiagramImageSize = false;
                    }

                    bool isFlipped = false;
                    if (nd.IsDiagram && !requestFromMainBoard)
                    {
                        isFlipped = nd.IsDiagramFlipped;
                    }
                    else
                    {
                        isFlipped = AppState.MainWin.MainChessBoard.IsFlipped;
                    }

                    SaveAsImage(nd, isFlipped);
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("SaveDiagramAsImage()", ex);
            }
        }

        /// <summary>
        /// Asks the user to select to field to save the diagram to.
        /// Once selected, generates a png image for the passed position and saves it.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="isFlipped"></param>
        /// <returns></returns>
        private static bool SaveAsImage(TreeNode node, bool isFlipped)
        {
            bool result = true;

            try
            {
                // invokes the SaveFileDialog to let the user choose the file name
                string filePath = GetUserSelectedFileName(false);

                if (!string.IsNullOrEmpty(filePath))
                {
                    int sideSize = GetUserSelectedSideSize(out bool cancel);

                    if (!cancel)
                    {
                        sideSize = VerifySideSize(sideSize);
                        Configuration.DiagramImageSize = sideSize;

                        // at this point we have all we need in the Configuration class
                        PositionImageGenerator.SaveDiagramAsImage(node, isFlipped, Configuration.LastPngExportFile, Configuration.DiagramImageSize);
                        AppState.MainWin.BoardCommentBox.ShowFlashAnnouncement(Properties.Resources.FlMsgDiagramImageSaved + " "
                                                                               + Path.GetFileName(Configuration.LastPngExportFile), CommentBox.HintType.INFO);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                result = false;
            }

            return result;
        }

        /// <summary>
        /// Asks the user to select the attributes of the diagram image
        /// </summary>
        /// <returns></returns>
        private static bool CollectConfiguration()
        {
            int sideSize = GetUserSelectedSideSize(out bool cancel);

            if (!cancel)
            {
                Configuration.DiagramImageSize = VerifySideSize(sideSize);
            }

            return !cancel;
        }

        /// <summary>
        /// Returns the list of nodes with a diagram from the passed tree.
        /// </summary>
        /// <param name="tree"></param>
        /// <returns></returns>
        private static void GetDiagramListFromTree(VariationTree tree, List<TreeNode> lstDiags)
        {
            if (tree != null)
            {
                foreach (TreeNode nd in tree.Nodes)
                {
                    if (nd.IsDiagram)
                    {
                        lstDiags.Add(nd);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the list of nodes with diagrams within the Variation Trees
        /// defined by the passed scope definitions.
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="viewTypes"></param>
        /// <returns></returns>
        private static List<TreeNode> GetDiagramList(OperationScope scope, ViewTypeScope viewTypes)
        {
            List<TreeNode> lstDiags = new List<TreeNode>();

            if (AppState.Workbook != null)
            {
                foreach (Chapter chapter in AppState.Workbook.Chapters)
                {
                    if (scope != OperationScope.CHAPTER || chapter == AppState.Workbook.ActiveChapter)
                    {
                        if ((viewTypes & ViewTypeScope.INTRO) != 0)
                        {
                            GetDiagramListFromTree(chapter.Intro.Tree, lstDiags);
                        }
                        if ((viewTypes & ViewTypeScope.STUDY) != 0)
                        {
                            GetDiagramListFromTree(chapter.StudyTree.Tree, lstDiags);
                        }
                        if ((viewTypes & ViewTypeScope.MODEL_GAMES) != 0)
                        {
                            foreach (Article game in chapter.ModelGames)
                            {
                                GetDiagramListFromTree(game.Tree, lstDiags);
                            }
                        }
                        if ((viewTypes & ViewTypeScope.EXERCISES) != 0)
                        {
                            foreach (Article exercises in chapter.Exercises)
                            {
                                GetDiagramListFromTree(exercises.Tree, lstDiags);
                            }
                        }
                    }
                }
            }

            return lstDiags;
        }

        /// <summary>
        /// Verifies that the side size is within the allowed range.
        /// </summary>
        /// <param name="sideSize"></param>
        /// <returns></returns>
        private static int VerifySideSize(int sideSize)
        {
            sideSize = Math.Max(sideSize, Constants.MIN_DIAGRAM_SIZE);
            sideSize = Math.Min(sideSize, Constants.MAX_DIAGRAM_SIZE);
            return sideSize;
        }

        /// <summary>
        /// Asks the user to select a file name to save the diagram to.
        /// The dialog will be pre-populated with the last file name used.
        /// If the file with that name already exisist, the initial value will be the same with a number appended.
        /// </summary>
        /// <returns></returns>
        private static string GetUserSelectedFileName(bool isMultiDiagrams)
        {
            string filePath = Configuration.LastPngExportFile;
            string fileName = "";

            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                try
                {
                    string directory = Path.GetDirectoryName(filePath);
                    fileName = Path.GetFileNameWithoutExtension(filePath);
                    string baseName = ExtractBaseName(fileName);
                    fileName = GetNextAvailableFileName(directory, baseName);
                    filePath = Path.Combine(directory, fileName);
                }
                catch
                {
                    filePath = "";
                }
            }

            SaveFileDialog saveDlg = new SaveFileDialog
            {
                Filter = Properties.Resources.ImageFiles + " (*.png)|*.png"
            };

            saveDlg.OverwritePrompt = true;
            saveDlg.Title = " " + Properties.Resources.SaveDiagramAsImage;

            try
            {
                saveDlg.InitialDirectory = Path.GetDirectoryName(filePath);
            }
            catch
            {
                saveDlg.InitialDirectory = "";
            }

            saveDlg.FileName = fileName;

            if (saveDlg.ShowDialog() == true)
            {
                filePath = saveDlg.FileName;
                if (!string.IsNullOrEmpty(filePath))
                {
                    Configuration.LastPngExportFile = filePath;
                }
                return saveDlg.FileName;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Asks the user to select the size of the diagram image
        /// and retruns the value.
        /// -1 indicates that the user cancelled the dialog.
        /// </summary>
        /// <returns></returns>
        private static int GetUserSelectedSideSize(out bool cancel)
        {
            cancel = false;

            if (!Configuration.DoNotAskDiagramImageSize)
            {
                DiagramImageSize dlg = new DiagramImageSize();
                GuiUtilities.PositionDialog(dlg, AppState.MainWin, 100);

                if (dlg.ShowDialog() != true)
                {
                    cancel = true;
                }
            }

            return Configuration.DiagramImageSize;
        }

        /// <summary>
        /// Extracts the base name from a file name.
        /// The base name is the file name without the trailing number.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private static string ExtractBaseName(string fileName)
        {
            Match match = Regex.Match(fileName, @"^(.*?)(?:\s+\d+)?$", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value : fileName;
        }

        /// <summary>
        /// Returns the next available file name in the directory
        /// in the format of base name followed by a number.
        /// </summary>
        /// <param name="directoryPath"></param>
        /// <param name="baseName"></param>
        /// <returns></returns>
        private static string GetNextAvailableFileName(string directoryPath, string baseName)
        {
            string pattern = $"^{Regex.Escape(baseName)}\\s*(\\d+)\\.png$";
            Regex regex = new Regex(pattern);

            var existingNumbers = Directory.EnumerateFiles(directoryPath, "*.png")
                .Select(Path.GetFileName)
                .Select(fileName => regex.Match(fileName))
                .Where(match => match.Success)
                .Select(match => int.Parse(match.Groups[1].Value))
                .ToHashSet();

            int M = 1;
            while (existingNumbers.Contains(M))
            {
                M++;
            }

            return $"{baseName} {M}.png";
        }
    }
}
