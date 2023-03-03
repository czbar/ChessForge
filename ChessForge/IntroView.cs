using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows;
using System.Windows.Controls;

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
        public IntroView()
        {
        }

        /// <summary>
        /// Loads content of the view
        /// </summary>
        public void LoadXAMLContent()
        {
            //string txt = "<FlowDocument PagePadding =\"5,0,5,0\" AllowDrop=\"True\" xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Paragraph>dddd ffff <Run FontWeight=\"Bold\">afddfav</Run> ddd</Paragraph></FlowDocument>";
            //AppState.MainWin.UiRtbIntroView.Document = StringToFlowDocument(txt);
        }

        /// <summary>
        /// Saves content of the view.
        /// </summary>
        /// <returns></returns>
        public string SaveXAMLContent()
        {
            //FlowDocument doc = new FlowDocument();

            //TextRange t = new TextRange(doc.ContentStart, doc.ContentEnd);
            //MemoryStream ms = new MemoryStream();
            //t.Save(ms, DataFormats.XamlPackage);

            string xamlText = XamlWriter.Save(AppState.MainWin.UiRtbIntroView.Document);

            return xamlText;
        }

        /// <summary>
        /// Creates a FlowDocument from XAML string.
        /// </summary>
        /// <param name="xamlString"></param>
        /// <returns></returns>
        private FlowDocument StringToFlowDocument(string xamlString)
        {
            //var xamlString = string.Format("<FlowDocument xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"><Paragraph>{0}</Paragraph></FlowDocument>", s);
            return XamlReader.Parse(xamlString) as FlowDocument;
        }
    }
}
