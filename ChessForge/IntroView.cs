using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows;
using ChessPosition;

namespace ChessForge
{
    /// <summary>
    /// Encapsulates Intro Tab view with RichTextBox  
    /// </summary>
    public class IntroView
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public IntroView(Chapter parentChapter)
        {
            ParentChapter = parentChapter;
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
            string xaml = EncodingUtils.Base64Decode(Intro.CodedContent);
            AppState.MainWin.UiRtbIntroView.Document = StringToFlowDocument(xaml);
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
            Intro.CodedContent = EncodingUtils.Base64Encode(xamlText);
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
