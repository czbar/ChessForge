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

        // is selection checkbox visible
        private bool _isSelectCheckBoxVisible;

        // the Chapter object (may be null if not needed for the current purpose).
        private Chapter _chapter;

        // index of the Chapter
        private int _chapterIndex = -1;

        // the Article object (may be null if we are only representing a Chapter here)
        private Article _article;

        // index of the Article in its chapter
        private int _articleIndex = -1;

        // type of content in the Article (NONE if this is for a Chapter)
        private GameData.ContentType _contentType;

        // node represented by this item, if any
        private TreeNode _node;

        // line from the start to this item's node
        private string _stemLine = null;

        /// <summary>
        /// Creates the object and sets IsSelected to true.
        /// The games in ListView are selected by default.
        /// </summary>
        public ArticleListItem(Chapter chapter, int chapterIndex, Article art, int articleIndex, TreeNode node = null)
        {
            _isSelected = false;
            _isSelectCheckBoxVisible = true;

            _chapter = chapter;
            _chapterIndex = chapterIndex;

            _article = art;
            _articleIndex = articleIndex;

            _node = node;

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
        /// Simplified constructor for the Chapter item.
        /// </summary>
        /// <param name="chapter"></param>
        public ArticleListItem(Chapter chapter) : this(chapter, -1, null, -1)
        {
            _isSelected = false;
            _isSelectCheckBoxVisible = false;
        }

        /// <summary>
        /// Returns the chapter this item belongs to.
        /// </summary>
        public Chapter Chapter
        {
            get { return _chapter; }
        }

        /// <summary>
        /// Returns the chapter this item belongs to.
        /// </summary>
        public string ChapterIndexColumnText
        {
            get { return _chapter == null ? string.Empty : Properties.Resources.Chapter + " " + (_chapterIndex + 1).ToString(); }
        }

        /// <summary>
        /// Returns the article object of this item.
        /// It can be null.
        /// </summary>
        public Article Article
        {
            get { return _article; }
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
                    header = Properties.Resources.Chapter.ToUpper() + ": " + _chapter.Title;
                }

                string prefix = string.Empty;
                if (_contentType == GameData.ContentType.MODEL_GAME)
                {
                    prefix = "    " + Properties.Resources.Game + ": ";
                }
                else if (_contentType == GameData.ContentType.EXERCISE)
                {
                    prefix = "    " + Properties.Resources.Exercise + ": ";
                }

                return prefix + header;
            }
        }

        /// <summary>
        /// Text for the column with article type.
        /// This is used in the Identical Positions dialog to put
        /// the word "Chapter" in the column before entries from
        /// a given chapter are shown.
        /// </summary>
        public string ArticleTypeForDisplay
        {
            get
            {
                return _chapter != null ? Properties.Resources.Chapter : string.Empty;
            }
        }

        /// <summary>
        /// Title to display for the Identical Positions dialog.
        /// If this is a Chapter entry, then produce a Chapter title
        /// including the index.
        /// Otherwise produce an Article title including the index.
        /// </summary>
        public string ElementTitleForDisplay
        {
            get
            {
                if (_chapter != null)
                {
                    return _chapter.Title;
                }
                else
                {
                    string header;
                    if (_contentType == GameData.ContentType.STUDY_TREE)
                    {
                        header = Properties.Resources.Study;
                    }
                    else
                    {
                        header = _article.Tree.Header.BuildGameHeaderLine(true, _contentType == GameData.ContentType.MODEL_GAME);
                    }

                    string prefix = string.Empty;
                    if (_contentType == GameData.ContentType.MODEL_GAME)
                    {
                        prefix = Properties.Resources.Game + " " + (_articleIndex + 1).ToString() + ": ";
                    }
                    else if (_contentType == GameData.ContentType.EXERCISE)
                    {
                        prefix = Properties.Resources.Exercise + " " + (_articleIndex + 1).ToString() + ": ";
                    }

                    return prefix + header;
                }
            }
        }

        /// <summary>
        /// Algebraic notation of the move to show.
        /// </summary>
        public string Move
        {
            get
            {
                return MoveUtils.BuildSingleMoveText(_node, true, false);
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
        /// Text of the line from the first node of the Tree to this item's node
        /// </summary>
        public string StemLine
        {
            get => _stemLine;
            set => _stemLine = value;
        }

        /// <summary>
        /// Tool tip that shows the stem line if the item is an Article,
        /// or the chapter index if the item is a Chapter.
        /// </summary>
        public string StemLineToolTip
        {
            get
            {
                if (!string.IsNullOrEmpty(_stemLine))
                {
                    return _stemLine;
                }
                else
                {
                    if (_chapter != null)
                    {
                        return Properties.Resources.Chapter + " " + (_chapterIndex + 1).ToString();
                    }
                    else
                    {
                        return "";
                    }
                }
            }
        }

        /// <summary>
        /// Index of the chapter on the Workbook's chapter list.
        /// </summary>
        public int ChapterIndex
        {
            get => _chapterIndex;
            set => _chapterIndex = value;
        }

        /// <summary>
        /// Index of the Article on Chapter's ModelGame or Exercise list.
        /// </summary>
        public int ArticleIndex
        {
            get => _articleIndex;
            set => _articleIndex = value;
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
