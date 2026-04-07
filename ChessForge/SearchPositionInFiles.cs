using GameTree;
using System.Collections.Generic;
using System.IO;

namespace ChessForge
{
    public class SearchPositionInFiles
    {
        public static void Search(SearchPositionCriteria crits)
        {
            SearchPgnFilesDialog dlg = new SearchPgnFilesDialog(crits);
            GuiUtilities.PositionDialog(dlg, AppState.MainWin, 100);

            if (dlg.ShowDialog() == true)
            {
                // Open a PGN file marked as selected.
                // TODO: implement
            }
        }

        public static List<string> FindFilesWithPosition(List<string> paths, SearchPositionCriteria crits)
        {
            List<string> filesWithPosition = new List<string>();
            foreach (string path in paths)
            {
                List<GameData> games = PgnMultiGameParser.ParsePgnMultiGameText(File.ReadAllText(path), out _);

                foreach (GameData gm in games)
                {
                    string fen = gm.Header.GetFenString();
                    if (!gm.Header.IsExercise())
                    {
                        fen = null;
                    }

                    try
                    {
                        VariationTree tree = new VariationTree(gm.GetContentType(false));
                        PgnGameParser parser = new PgnGameParser(gm.GameText, tree, fen);
                        List<TreeNode> lstNodes = SearchPosition.FindIdenticalNodes(tree, crits, true);
                        if (lstNodes.Count > 0)
                        {
                            filesWithPosition.Add(path);
                            break;
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }
            }

            return filesWithPosition;
        }
    }
}
