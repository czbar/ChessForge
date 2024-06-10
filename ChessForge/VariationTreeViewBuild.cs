using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Windows.Documents;
using System.Windows;

namespace ChessForge
{
    /// <summary>
    /// Contains some methods related to building elements of the FlowDocument.
    /// </summary>
    public partial class VariationTreeView : RichTextBuilder
    {
        /// <summary>
        /// Builds the top paragraph for the page if applicable.
        /// It will be Game/Exercise header or the chapter's title
        /// if we are in the Chapters View
        /// </summary>
        /// <returns></returns>
        private Paragraph BuildPageHeader(VariationTree tree, GameData.ContentType contentType)
        {
            Paragraph headerPara = null;

            if (tree != null)
            {
                switch (contentType)
                {
                    case GameData.ContentType.MODEL_GAME:
                    case GameData.ContentType.EXERCISE:
                        string whitePlayer = tree.Header.GetWhitePlayer(out _);
                        string blackPlayer = tree.Header.GetBlackPlayer(out _);

                        string whitePlayerElo = tree.Header.GetWhitePlayerElo(out _);
                        string blackPlayerElo = tree.Header.GetBlackPlayerElo(out _);

                        headerPara = CreateParagraph("0", true);
                        headerPara.Margin = new Thickness(0, 0, 0, 0);
                        headerPara.Name = RichTextBoxUtilities.HeaderParagraphName;
                        headerPara.MouseLeftButtonDown += EventPageHeaderClicked;

                        bool hasPlayerNames = !(string.IsNullOrWhiteSpace(whitePlayer) && string.IsNullOrWhiteSpace(blackPlayer));

                        if (hasPlayerNames)
                        {
                            Run rWhiteSquare = CreateRun("0", (Constants.CharWhiteSquare.ToString() + " "), true);
                            rWhiteSquare.FontWeight = FontWeights.Normal;
                            headerPara.Inlines.Add(rWhiteSquare);

                            Run rWhite = CreateRun("0", (BuildPlayerLine(whitePlayer, whitePlayerElo) + "\n"), true);
                            headerPara.Inlines.Add(rWhite);

                            Run rBlackSquare = CreateRun("0", (Constants.CharBlackSquare.ToString() + " "), true);
                            rBlackSquare.FontWeight = FontWeights.Normal;
                            headerPara.Inlines.Add(rBlackSquare);

                            Run rBlack = CreateRun("0", (BuildPlayerLine(blackPlayer, blackPlayerElo)) + "\n", true);
                            headerPara.Inlines.Add(rBlack);
                        }

                        if (!string.IsNullOrEmpty(tree.Header.GetEventName(out _)))
                        {
                            if (hasPlayerNames)
                            {
                                string round = tree.Header.GetRound(out _);
                                if (!string.IsNullOrWhiteSpace(round))
                                {
                                    round = " (" + round + ")";
                                }
                                else
                                {
                                    round = "";
                                }
                                string eventName = tree.Header.GetEventName(out _) + round;
                                Run rEvent = CreateRun("1", "      " + eventName + "\n", true);
                                headerPara.Inlines.Add(rEvent);
                            }
                            else
                            {
                                Run rEvent = CreateRun("0", tree.Header.GetEventName(out _) + "\n", true);
                                headerPara.Inlines.Add(rEvent);
                            }
                        }

                        string annotator = tree.Header.GetAnnotator(out _);
                        if (!string.IsNullOrWhiteSpace(annotator))
                        {
                            Run rAnnotator = CreateRun("1", "      " + Properties.Resources.Annotator + ": " + annotator + "\n", true);
                            headerPara.Inlines.Add(rAnnotator);
                        }

                        string dateForDisplay = TextUtils.BuildDateFromDisplayFromPgnString(tree.Header.GetDate(out _));
                        if (!string.IsNullOrEmpty(dateForDisplay))
                        {
                            Run rDate = CreateRun("1", "      " + Properties.Resources.Date + ": " + dateForDisplay + "\n", true);
                            headerPara.Inlines.Add(rDate);
                        }

                        string eco = tree.Header.GetECO(out _);
                        string result = tree.Header.GetResult(out _);
                        BuildResultAndEcoLine(eco, result, out Run rEco, out Run rResult);
                        if (rEco != null || rResult != null)
                        {
                            Run rIndent = new Run("      ");
                            rIndent.FontWeight = FontWeights.Normal;
                            headerPara.Inlines.Add(rIndent);

                            if (rEco != null)
                            {
                                rEco.FontWeight = FontWeights.Bold;
                                headerPara.Inlines.Add(rEco);
                            }
                            if (rResult != null)
                            {
                                rResult.FontWeight = FontWeights.Normal;
                                headerPara.Inlines.Add(rResult);
                            }

                            Run rNewLine = new Run("\n");
                            headerPara.Inlines.Add(rNewLine);
                        }
                        break;
                    case GameData.ContentType.STUDY_TREE:
                        UpdateChapterTitle();
                        break;
                }
            }

            return headerPara;
        }
    }
}
