using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

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
        /// Quiz points
        /// </summary>
        public int QuizPoints { get; set; }

        /// <summary>
        /// NAGs string for the position.
        /// </summary>
        public string Nags { get; set; }

        /// <summary>
        /// Whether exit occurred on user's pushing the OK button  
        /// </summary>
        public bool ExitOk = false;

        /// <summary>
        /// Constructs the dialog.
        /// Sets the values passed by the caller.
        /// </summary>
        /// <param name="ass"></param>
        /// <param name="comment"></param>
        public AnnotationsDialog(TreeNode nd)
        {
            InitializeComponent();
            SetPositionButtons(nd.Nags);
            SetMoveButtons(nd.Nags);
            Nags = nd.Nags;
            UiTbComment.Text = nd.Comment ?? "";
            UiTbQuizPoints.Text = nd.QuizPoints == 0 ? "" : nd.QuizPoints.ToString();
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
                int id = GetNagId(11, 19, nags);
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
                int id = GetNagId(1, 6, nags);
                if (id != 0)
                {
                    SelectMoveButton(id);
                }
            }
        }

        /// <summary>
        /// Parses the string in the form "$[id] $[id]"
        /// and returns the first id found in the requested range.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="nags"></param>
        /// <returns></returns>
        private int GetNagId(int from, int to, string nags)
        {
            int ret = 0;
            if (!string.IsNullOrEmpty(nags))
            {
                string[] tokens = nags.Split(' ');
                foreach (string token in tokens)
                {
                    // skip the leading $ sign
                    if (token.Length > 1 && int.TryParse(token.Substring(1), out int id))
                    {
                        if (id >= from && id <= to)
                        {
                            ret = id;
                            break;
                        }
                    }
                }
            }

            return ret;
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
        /// Closes the dialog after user pushed the Cancel button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnCancel_Click(object sender, RoutedEventArgs e)
        {
            ExitOk = false;
            Close();
        }

        /// <summary>
        /// Closes the dialog after user pushed the OK button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnOk_Click(object sender, RoutedEventArgs e)
        {
            Comment = UiTbComment.Text;
            QuizPoints = ParseQuizPoints(UiTbQuizPoints.Text);
            BuildNagsString();
            ExitOk = true;
            Close();
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
                    if (quizPoints > 100 || quizPoints < -100)
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
