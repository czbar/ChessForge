using GameTree;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ChessForge
{
    /// <summary>
    /// Objects of this class are meant for use in ObservableCollection
    /// for Lists of items to select from
    /// </summary>
    public class ArticleListItem : INotifyPropertyChanged
    {
        // whether this game is selected in the GUI
        private bool _isSelected;

        // is selection checkbox visble
        private bool _isVisible;

        // representes Article (null if this object was created for a Chapter
        private Article _article;

        // Chapter object. Null if this object was not created for a chapter.
        private Chapter _chapter;

        // type of content in the Article (NONE if this is for a Chapter)
        private GameData.ContentType _contentType;

        // index of the Article in its chapter
        private int _index = -1;

        /// <summary>
        /// Creates the object and sets IsSelected to true.
        /// The games in ListView are selected by default.
        /// </summary>
        public ArticleListItem(Chapter chapter, Article art, int index)
        {
            _isSelected = true;
            _isVisible = true;

            _article = art;
            _chapter = chapter;
            _index = index;

            if (art != null)
            {
                _contentType = art.Tree.Header.GetContentType(out _);
            }
            else
            {
                _contentType = GameData.ContentType.NONE;
            }
        }

        /// <summary>
        /// The property that binds in the SelectGames ListView control.
        /// </summary>
        public string GameTitleForList
        {
            get
            {
                string header = string.Empty;
                if (_article != null)
                {
                    header = _article.Tree.Header.BuildGameHeaderLine(true);
                }
                else if (_chapter != null)
                {
                    header = _chapter.Title;
                }

                string prefix = string.Empty;
                if (_contentType == GameData.ContentType.MODEL_GAME)
                {
                    prefix = "Game: ";
                }
                else if (_contentType == GameData.ContentType.EXERCISE)
                {
                    prefix = "Exercise: ";
                }

                return prefix + header;
            }
        }

        /// <summary>
        /// Accessor to _isSelected.
        /// This is the only property that can be changed
        /// from the GUI.
        /// </summary>
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                _isSelected = value;
                NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Accessor to _isVisible.
        /// This is set upon creation and not subject to editing.
        /// It will be "Hidden" for Chapter lines and "Visible" for Articles.
        /// </summary>
        public string Visibility
        {
            get { return _isVisible ? "Visible" : "Hidden"; }
        }

        /// <summary>
        /// PropertChange event handler
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Notifies the framework of the change in the bound data.
        /// </summary>
        /// <param name="propertyName"></param>
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
