using ChessPosition;
using GameTree;
using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;

namespace ChessForge
{
    public class SaveDiagram
    {
        /// <summary>
        /// Asks the user to select to field to save the diagram to.
        /// Once selected, generates a png image for the passed position and saves it.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="isFlipped"></param>
        /// <returns></returns>
        public static bool SaveAsImage(TreeNode node, bool isFlipped)
        {
            bool result = true;

            try
            {
                // invokes the SaveFileDialog to let the user choose the file name
                string filePath = GetUserSelectedFileName();

                if (!string.IsNullOrEmpty(filePath))
                {
                    Configuration.LastPngFile = filePath;

                    int sideSize = GetUserSelectedSideSize(out bool cancel);

                    if (!cancel)
                    {
                        sideSize = VerifySideSize(sideSize);
                        Configuration.DiagramImageSize = sideSize;
                        
                        // at this point we have all we need in the Configuration class
                        PositionImageGenerator.SaveDiagramAsImage(node, isFlipped, Configuration.LastPngFile, Configuration.DiagramImageSize);
                        AppState.MainWin.BoardCommentBox.ShowFlashAnnouncement(Properties.Resources.FlMsgDiagramImageSaved + " " 
                                                                               + Path.GetFileName(Configuration.LastPngFile), CommentBox.HintType.INFO);
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
        /// Verifies that the side size is within the allowed range.
        /// </summary>
        /// <param name="sideSize"></param>
        /// <returns></returns>
        public static int VerifySideSize(int sideSize)
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
        private static string GetUserSelectedFileName()
        {
            string filePath = Configuration.LastPngFile;

            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                string directory = Path.GetDirectoryName(filePath);
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                string baseName = ExtractBaseName(fileName);
                fileName = GetNextAvailableFileName(directory, baseName);
                filePath = Path.Combine(directory, fileName);
            }

            SaveFileDialog saveDlg = new SaveFileDialog
            {
                Filter = Properties.Resources.ImageFiles + " (*.png)|*.png"
            };

            saveDlg.OverwritePrompt = true;
            saveDlg.Title = " " + Properties.Resources.SaveDiagramAsImage;

            saveDlg.FileName = filePath;

            if (saveDlg.ShowDialog() == true)
            {
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
