using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
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
        // is items to be shown in the list
        private bool _isShown;

        // whether this game is selected in the GUI
        private bool _isSelected;

        // is selection checkbox visble
        private bool _isSelectCheckBoxVisible;

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
            _isSelectCheckBoxVisible = true;

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
        /// Returns the chapter this item belongs to.
        /// </summary>
        public Chapter Chapter
        {
            get { return _chapter; }
        }

        /// <summary>
        /// Simplified constructor for the Chapter item.
        /// </summary>
        /// <param name="chapter"></param>
        public ArticleListItem(Chapter chapter) : this(chapter, null, -1)
        {
            _isSelected = false;
            _isSelectCheckBoxVisible = false;
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
                    header = _article.Tree.Header.BuildGameHeaderLine(true, _contentType == GameData.ContentType.MODEL_GAME);
                }
                else if (_chapter != null)
                {
                    header = "CHAPTER: " + _chapter.Title;
                }

                string prefix = string.Empty;
                if (_contentType == GameData.ContentType.MODEL_GAME)
                {
                    prefix = "    Game: ";
                }
                else if (_contentType == GameData.ContentType.EXERCISE)
                {
                    prefix = "    Exercise: ";
                }

                return prefix + header;
            }
        }

        /// <summary>
        /// Date string for display
        /// </summary>
        public string Date
        {
            get
            {
                if (_article == null)
                {
                    return "";
                }
                else
                {
                    string date = _article.Tree.Header.GetDate(out _);
                    return TextUtils.BuildDateFromDisplayFromPgnString(date);
                }
            }
        }

        /// <summary>
        /// Accessor to _isShown.
        /// Indicates whether the item should be shown in the list.
        /// </summary>
        public bool IsShown
        {
            get { return _isShown; }
            set
            {
                _isShown = value;
                NotifyPropertyChanged();
            }
        }


        /// <summary>
        /// Accessor to _isSelected.
        /// Indicates wheter the item is selected.
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
            get { return _isSelectCheckBoxVisible ? "Visible" : "Hidden"; }
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
