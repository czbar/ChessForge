using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows;
using ChessPosition;
using System.Windows.Controls;
using GameTree;
using System.Windows.Media;

namespace ChessForge
{
    /// <summary>
    /// Encapsulates Intro Tab view with RichTextBox  
    /// </summary>
    public class IntroView
    {
        /// <summary>
        /// The list of diagrams in this view.
        /// </summary>
        private List<IntroViewDiagram> DiagramList = new List<IntroViewDiagram>();

        // refrence to the RichTextBox of this view.
        private RichTextBox _rtb = AppState.MainWin.UiRtbIntroView;

        // flag to use to prevent unnecessary saving after the load.
        private bool _ignoreTextChange = false;

        /// <summary>
        /// Constructor. Builds the content if not empty.
        /// </summary>
        public IntroView(Chapter parentChapter)
        {
            _rtb.Document.Blocks.Clear();
            ParentChapter = parentChapter;

            // set the event handler we loaded the document.
            _rtb.TextChanged += UiRtbIntroView_TextChanged;
            if (!string.IsNullOrEmpty(Intro.Tree.RootNode.Data))
            {
                _ignoreTextChange = true;
                LoadXAMLContent();
            }
        }

        /// <summary>
        /// Clear the content of the RTB
        /// </summary>
        public void Clear()
        {
            _ignoreTextChange = true;
            _rtb.Document.Blocks.Clear();
        }

        /// <summary>
        /// Chapter for which this view was created.
        /// </summary>
        public Chapter ParentChapter { get; set; }

        /// <summary>
        /// The Intro article shown in the view.
        /// </summary>
        public Article Intro
        {
            get => ParentChapter.Intro;
        }

        /// <summary>
        /// Loads content of the view
        /// </summary>
        public void LoadXAMLContent()
        {
            if (!string.IsNullOrEmpty(Intro.CodedContent))
            {
                string xaml = EncodingUtils.Base64Decode(Intro.CodedContent);
                _rtb.Document = StringToFlowDocument(xaml);

                //Paragraph para = BuildDiagramParagraph();
                //_rtb.Document.Blocks.Add(para);
            }
        }

        /// <summary>
        /// Saves content of the view.
        /// </summary>
        /// <returns></returns>
        public void SaveXAMLContent()
        {
            FlowDocument doc = _rtb.Document;

            TextRange t = new TextRange(doc.ContentStart, doc.ContentEnd);
            MemoryStream ms = new MemoryStream();
            t.Save(ms, DataFormats.Xaml);
            ms.Position = 0;
            var sr = new StreamReader(ms);
            string myStr = sr.ReadToEnd();
            string xamlText = XamlWriter.Save(_rtb.Document);
            Intro.Tree.RootNode.Data = EncodingUtils.Base64Encode(xamlText);
        }

        /// <summary>
        /// Invokes a PositionSetup dialog,
        /// creates GUI objects for the position
        /// and inserts in the document.
        /// </summary>
        public void CreateDiagram()
        {
            try
            {
                TextPointer tp = _rtb.CaretPosition.InsertParagraphBreak();
                Paragraph nextPara = tp.Paragraph;

                DiagramSetupDialog dlg = new DiagramSetupDialog(null)
                {
                    Left = AppState.MainWin.ChessForgeMain.Left + 100,
                    Top = AppState.MainWin.Top + 100,
                    Topmost = false,
                    Owner = AppState.MainWin
                };

                if (dlg.ShowDialog() == true)
                {
                    BoardPosition pos = dlg.PositionSetup;
                    IntroViewDiagram diag = new IntroViewDiagram();
                    Paragraph para = BuildDiagramParagraph(diag, pos);
                    diag.Chessboard.DisplayPosition(null, pos);
                    AppState.IsDirty = true;

                    _rtb.Document.Blocks.InsertBefore(nextPara, para);
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("CreateDiagram()", ex);
            }
        }

        /// <summary>
        /// Builds a paragraph with the diagram.
        /// </summary>
        /// <param name="diag"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        private Paragraph BuildDiagramParagraph(IntroViewDiagram diag, BoardPosition pos)
        {
            Paragraph para = new Paragraph();
            para.Margin = new Thickness(20, 20, 0, 20);
            para.Name = "Chessboard";

            Canvas canvas = SetupDiagramCanvas();
            Image imgChessBoard = CreateChessBoard(canvas, diag);
            canvas.Children.Add(imgChessBoard);
            Viewbox viewBox = SetupDiagramViewbox(canvas);

            InlineUIContainer uIContainer = new InlineUIContainer();
            uIContainer.Child = viewBox;
            para.Inlines.Add(uIContainer);

            return para;
        }

        /// <summary>
        /// Creates the chessboard control.
        /// </summary>
        /// <param name="canvas"></param>
        /// <returns></returns>
        private Image CreateChessBoard(Canvas canvas, IntroViewDiagram diag)
        {
            Image imgChessBoard = new Image();
            imgChessBoard.Margin = new Thickness(5, 5, 5, 5);
            imgChessBoard.Source = ChessBoards.ChessBoardGreySmall;

            diag.Chessboard = new ChessBoardSmall(canvas, imgChessBoard, null, null, false, false);
            AlignExerciseAndMainBoards();

            return imgChessBoard;
        }

        /// <summary>
        /// Sets the "passive" exercise board to the same
        /// orientation as the main board.
        /// </summary>
        public void AlignExerciseAndMainBoards()
        {
        }

        /// <summary>
        /// Creates a Canvas for the chessboard. 
        /// </summary>
        /// <returns></returns>
        private Canvas SetupDiagramCanvas()
        {
            Canvas canvas = new Canvas();
            canvas.Background = Brushes.Black;
            canvas.Width = 250;
            canvas.Height = 250;

            return canvas;
        }

        /// <summary>
        /// Creates a Viewbox for the chessboard
        /// </summary>
        /// <param name="canvas"></param>
        /// <returns></returns>
        private Viewbox SetupDiagramViewbox(Canvas canvas)
        {
            Viewbox viewBox = new Viewbox();
            viewBox.Child = canvas;
            viewBox.Width = 250;
            viewBox.Height = 250;
            viewBox.Visibility = Visibility.Visible;

            return viewBox;
        }

        /// <summary>
        /// Creates a FlowDocument from XAML string.
        /// </summary>
        /// <param name="xamlString"></param>
        /// <returns></returns>
        private FlowDocument StringToFlowDocument(string xamlString)
        {
            return XamlReader.Parse(xamlString) as FlowDocument;
        }

        /// <summary>
        /// Handles the Text Change event. Sets the Workbook's dirty flag.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiRtbIntroView_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_ignoreTextChange)
            {
                _ignoreTextChange = false;
            }
            else
            {
                AppState.IsDirty = true;
            }
        }

    }
}
