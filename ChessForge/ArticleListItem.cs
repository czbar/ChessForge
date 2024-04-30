using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

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

        // the Chapter object (null if the item represents an Article).
        private Chapter _chapter;

        // index of the Chapter
        private int _chapterIndex = -1;

        // the Article object (null if we are only representing a Chapter here)
        private Article _article;

        // index of the Article in its chapter
        private int _articleIndex = -1;

        // type of content in the Article (NONE if this is for a Chapter)
        private GameData.ContentType _contentType;

        // node represented by this item, if any
        private TreeNode _node;

        // line from the start to this item's node
        private string _stemLineText = null;

        // main line from this items' node to the end
        private string _tailLineText = null;

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
        }

        /// <summary>
        /// Simplified constructor for the Chapter item.
        /// </summary>
        /// <param name="chapter"></param>
        public ArticleListItem(Chapter chapter, int chapterIndex) : this(chapter, chapterIndex, null, -1)
        {
            _isSelected = false;
        }

        /// <summary>
        /// Returns the chapter this item belongs to.
        /// </summary>
        public Chapter Chapter
        {
            get { return _chapter; }
        }

        /// <summary>
        /// Returns true if this item represents a chapter header kine.
        /// </summary>
        public bool IsChapterHeader
        {
            get => _chapter != null && _contentType == GameData.ContentType.NONE;
        }

        /// <summary>
        /// Returns the chapter this item belongs to.
        /// </summary>
        public string ChapterIndexColumnText
        {
            get { return !IsChapterHeader ? string.Empty : Properties.Resources.Chapter + " " + (_chapterIndex + 1).ToString(); }
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
        /// Returns the TreeNode associated with this item.
        /// </summary>
        public TreeNode Node
        {
            get { return _node; }
        }

        /// <summary>
        /// Returns content type of this item.
        /// </summary>
        public GameData.ContentType ContentType
        {
            get
            {
                return _contentType;
            }
        }

        /// <summary>
        /// The property that can be bound in the SelectGames ListView control as the title of an Article.
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
                else if (IsChapterHeader)
                {
                    header = "[" + (Chapter.Index + 1).ToString() + ".] " + _chapter.Title;
                }

                string prefix = string.Empty;
                if (_contentType == GameData.ContentType.MODEL_GAME)
                {
                    prefix = "    " + Properties.Resources.Game + " " + (ArticleIndex + 1).ToString() + ": ";
                }
                else if (_contentType == GameData.ContentType.EXERCISE)
                {
                    prefix = "    " + Properties.Resources.Exercise + " " + (ArticleIndex + 1).ToString() + ": ";
                }
                else if (_contentType == GameData.ContentType.STUDY_TREE)
                {
                    prefix = "    " + Properties.Resources.Study + ": ";
                }

                return prefix + header;
            }
        }

        /// <summary>
        /// The property that can be bound in the SelectGames ListView control 
        /// as the title of an Article in the duplicates list.
        /// </summary>
        public string ArticleTitleForDuplicateList
        {
            get
            {
                StringBuilder sb = new StringBuilder();

                sb.Append(Properties.Resources.Chapter + " " + (ChapterIndex + 1).ToString() + ", ");
                if (_contentType == GameData.ContentType.MODEL_GAME)
                {
                    sb.Append(Properties.Resources.Game);
                }
                else if (_contentType == GameData.ContentType.EXERCISE)
                {
                    sb.Append(Properties.Resources.Exercise);
                }
                sb.Append(" " + (ArticleIndex + 1).ToString());

                if (_article != null)
                {
                    sb.Append(": " + _article.Tree.Header.BuildGameHeaderLine(true, _contentType == GameData.ContentType.MODEL_GAME));
                }

                return sb.ToString();
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
                return IsChapterHeader ? Properties.Resources.Chapter : string.Empty;
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
                if (IsChapterHeader)
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
                        header = _article.Tree.Header.BuildGameHeaderLine(true, _contentType == GameData.ContentType.MODEL_GAME, true, true);
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
                uint moveNumberOffset = 0;
                if (_article != null && _article.Tree != null)
                {
                    moveNumberOffset = _article.Tree.MoveNumberOffset;
                }
                return MoveUtils.BuildSingleMoveText(_node, true, false, moveNumberOffset);
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
        /// List of nodes for the stem line. 
        /// </summary>
        public List<TreeNode> StemLine;

        /// <summary>
        /// List of nodes for the tail line.
        /// </summary>
        public List<TreeNode> TailLine;

        /// <summary>
        /// Whether the TailLine is the main line ("game text") of the tree.
        /// </summary>
        public bool IsTailLineMain;

        /// <summary>
        /// List of nodes for the main line.
        /// </summary>
        public List<TreeNode> MainLine;

        /// <summary>
        /// Text of the line from the first node of the Tree to this item's node
        /// </summary>
        public string StemLineText
        {
            get => _stemLineText;
            set => _stemLineText = value;
        }

        /// <summary>
        /// Text of the main line from a certain node of the Tree to th end
        /// </summary>
        public string TailLineText
        {
            get => _tailLineText;
            set => _tailLineText = value;
        }

        /// <summary>
        /// Number of plies in the tail line
        /// </summary>
        public int TailLinePlyCount;

        /// <summary>
        /// Tool tip that shows the stem line if the item is an Article,
        /// or the chapter index if the item is a Chapter.
        /// </summary>
        public string StemLineToolTip
        {
            get
            {
                if (!string.IsNullOrEmpty(_stemLineText))
                {
                    return _stemLineText;
                }
                else
                {
                    if (IsChapterHeader)
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
            get => !IsChapterHeader ? _chapterIndex : _chapter.Index;
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

        //////////////////////////////////////////////////////////////////        
        ///
        /// The following attributes determine visibility of the selection
        /// CheckBox in the row of the SelectArticle dialog.
        /// 
        /// There are 3 instances of the CheckBox initiated in the dialog
        /// and precisely one will be visible (note that for a non-chapter 
        /// item, the entire row may be hidden).
        /// 
        /// If the Chapter is expanded, the regular Chapter-style CheckBox will be shown.
        /// If the Chapter is collapsed, the parent dialog will check if all the items
        /// are checked or unchecked and show the regular CheckBox appropriately set.
        /// If some items are checked and some are not, the special CheckBox
        /// with a gray background will be shown.
        ///
        //////////////////////////////////////////////////////////////////        

        // whether the regular chapter CheckBox is visible
        private string _isChapterCheckboxVisible = "Visible";

        // whether the "grayed" chapter CheckBox is visible
        private string _isChapterGrayedCheckboxVisible = "Collapsed";

        // whether the parent chapter is expanded
        private bool _isChapterExpanded = true;

        // whether all items in the the parent chapter are selected
        private bool _isChapterAllSelected = true;

        // whether all items in the the parent chapter are unselected
        private bool _isChapterAllUnselected = false;

        /// <summary>
        /// Set from outside to indicate if the regular chapter
        /// CheckBox is visible.
        /// </summary>
        public string ChapterCheckBoxVisible
        {
            get => IsChapterHeader ? _isChapterCheckboxVisible : "Collapsed";
            set
            {
                _isChapterCheckboxVisible = value;
                NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Set from outside to indicate if the regular chapter
        /// CheckBox is visible.
        /// </summary>
        public string ChapterGrayedCheckBoxVisible
        {
            get => IsChapterHeader ? _isChapterGrayedCheckboxVisible : "Collapsed";
            set
            {
                _isChapterGrayedCheckboxVisible = value;
                NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Always visible, if this is not a chapter item.
        /// </summary>
        public string NonChapterCheckBoxVisible
        {
            get { return !IsChapterHeader ? "Visible" : "Collapsed"; }
        }

        /// <summary>
        /// If true, the chapter state is "expanded".
        /// If false, the chapter state is "collapsed".
        /// </summary>
        public bool IsChapterExpanded
        {
            get => _isChapterExpanded;
            set => _isChapterExpanded = value;
        }

        /// <summary>
        /// If true, all articles within the chapter are selected.
        /// If false, not all articles within the chapter are selected.
        /// </summary>
        public bool IsChapterAllSelected
        {
            get => _isChapterAllSelected;
            set => _isChapterAllSelected = value;
        }

        /// <summary>
        /// If true, all articles within the chapter are unselected.
        /// If false, not all articles within the chapter are unselected.
        /// </summary>
        public bool IsChapterAllUnselected
        {
            get => _isChapterAllUnselected;
            set => _isChapterAllUnselected = value;
        }


        //////////////////////////////////////////////////////////////////        


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
