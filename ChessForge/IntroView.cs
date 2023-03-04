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

namespace ChessForge
{
    /// <summary>
    /// Encapsulates Intro Tab view with RichTextBox  
    /// </summary>
    public class IntroView
    {
        // flag to use to prevent unnecessary saving after the load.
        private bool _ignoreTextChange = false;

        /// <summary>
        /// Constructor. Builds the content if not empty.
        /// </summary>
        public IntroView(Chapter parentChapter)
        {
            AppState.MainWin.UiRtbIntroView.Document.Blocks.Clear();
            ParentChapter = parentChapter;

            // set the event handler we loaded the document.
            AppState.MainWin.UiRtbIntroView.TextChanged += UiRtbIntroView_TextChanged;
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
            AppState.MainWin.UiRtbIntroView.Document.Blocks.Clear();
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
                AppState.MainWin.UiRtbIntroView.Document = StringToFlowDocument(xaml);
            }
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

        /// <summary>
        /// Saves content of the view.
        /// </summary>
        /// <returns></returns>
        public void SaveXAMLContent()
        {
            FlowDocument doc = AppState.MainWin.UiRtbIntroView.Document;

            TextRange t = new TextRange(doc.ContentStart, doc.ContentEnd);
            MemoryStream ms = new MemoryStream();
            t.Save(ms, DataFormats.Xaml);

            string xamlText = XamlWriter.Save(AppState.MainWin.UiRtbIntroView.Document);
            Intro.Tree.RootNode.Data = EncodingUtils.Base64Encode(xamlText);
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
    }
}
