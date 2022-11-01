using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using ChessPosition;
using ChessPosition.GameTree;
using GameTree;

namespace ChessForge
{
    /// <summary>
    /// A Workbook is the highest level ChessForge data entity
    /// and there can only be one open at any time. 
    /// 
    /// A Workbook consists of one or more chapters;
    /// Each chapter can hold one or more Variation Tree.
    /// </summary>
    public class Workbook
    {
        /// <summary>
        /// The list of chapters.
        /// </summary>
        private List<Chapter> _chapters = new List<Chapter>();

        public List<Chapter> Chapters
        {
            get
            {
                return _chapters;
            }
        }

        /// <summary>
        /// The training side.
        /// </summary>
        public PieceColor TrainingSide = PieceColor.None;

        /// <summary>
        /// Indicates whether the main board was flipped when
        /// StudyTree last lost focus so we can restore
        /// it when getting focus back
        /// </summary>
        public bool? IsStudyBoardFlipped = null;

        /// <summary>
        /// Indicates whether the main board was flipped when
        /// Model Game view last lost focus so we can restore
        /// it when getting focus back
        /// </summary>
        public bool? IsModelGameBoardFlipped = null;

        /// <summary>
        /// Indicates whether the main board was flipped when
        /// Exercise view last lost focus so we can restore
        /// it when getting focus back
        /// </summary>
        public bool? IsExerciseBoardFlipped = null;

        // chapter currently open in the session
        private Chapter _activeChapter;

        // last update date
        private DateTime? _lastUpdate;

        // workbook description string
        private string _description;

        // workbook title
        private string _title;

        /// <summary>
        /// Empty constructor.
        /// </summary>
        public Workbook()
        {
        }

        /// <summary>
        /// The chapter currently open in the session.
        /// </summary>
        public Chapter ActiveChapter
        {
            get
            {
                if (_activeChapter == null)
                {
                    return SelectDefaultActiveChapter();
                }
                return _activeChapter;
            }
            set => _activeChapter = value;
        }

        /// <summary>
        ///  Returns the 1-based Active Chapter number.
        ///  Returns 0 if there is no active chapter.
        /// </summary>
        public int ActiveChapterNumber
        {
            get
            {
                return GetChapterNumber(_activeChapter);
            }
        }

        /// <summary>
        ///  Returns the 1-based chapter number.
        ///  Returns 0 if not found.
        /// </summary>
        /// <param name="chapter"></param>
        /// <returns></returns>
        public int GetChapterNumber(Chapter chapter)
        {
            for (int i = 0; i < _chapters.Count; i++)
            {
                if (_chapters[i] == chapter)
                    return i + 1;
            }

            return 0;
        }

        /// <summary>
        /// Returns the number of chapters in this workbook.
        /// </summary>
        /// <returns></returns>
        public int GetChapterCount()
        {
            return _chapters.Count;
        }

        /// <summary>
        /// Selects the default (first in the list) chapter as ActiveChapter
        /// </summary>
        /// <returns></returns>
        public Chapter SelectDefaultActiveChapter()
        {
            if (Chapters.Count == 0)
            {
                _activeChapter = null;
            }
            else
            {
                _activeChapter = Chapters[0];
            }

            return _activeChapter;
        }

        /// <summary>
        /// Creates a new "default" chapter
        /// </summary>
        /// <returns></returns>
        public Chapter CreateDefaultChapter()
        {
            return CreateNewChapter();
        }

        /// <summary>
        /// Sets Active Chapter and Tree given the index of the chapter in the Chapters list.
        /// </summary>
        /// <param name="chapterIndex">index of the requested chapter in the Chapters list.</param>
        /// <param name="gameType"></param>
        /// <param name="gameIndex">index in the list of elements of the requested type i.e. Model Games or Exercises </param>
        public void SetActiveChapterTreeByIndex(int chapterIndex, GameData.ContentType gameType, int gameIndex = 0)
        {
            if (chapterIndex < 0 || chapterIndex >= Chapters.Count)
            {
                return;
            }

            _activeChapter = Chapters[chapterIndex];
            _activeChapter.SetActiveVariationTree(gameType, gameIndex);
        }

        /// <summary>
        /// Sets Active Chapter and Tree given the Id of the chapter.
        /// </summary>
        /// <param name="chapterId"></param>
        /// <param name="gameType"></param>
        /// <param name="gameIndex"></param>
        public void SetActiveChapterTreeById(int chapterId, GameData.ContentType gameType, int gameIndex = 0)
        {
            foreach (Chapter chapter in Chapters)
            {
                if (chapter.Id == chapterId)
                {
                    _activeChapter = chapter;
                    _activeChapter.SetActiveVariationTree(gameType, gameIndex);
                    break;
                }
            }

        }

        /// <summary>
        /// Returns the Active Tree which
        /// is the Active Tree of the active chapter.
        /// </summary>
        public VariationTree ActiveVariationTree
        {
            get
            {
                if (_activeChapter == null)
                {
                    SelectDefaultActiveChapter();
                }

                if (_activeChapter == null)
                {
                    return null;
                }
                else
                {
                    return _activeChapter.ActiveVariationTree;
                }
            }
        }

        /// <summary>
        /// Returns the Content Type of the current tab.
        /// </summary>
        public GameData.ContentType ActiveContentType
        {
            get
            {
                GameData.ContentType contentType = GameData.ContentType.NONE;

                switch (WorkbookManager.ActiveTab)
                {
                    case WorkbookManager.TabViewType.STUDY:
                        contentType = GameData.ContentType.STUDY_TREE;
                        break;
                    case WorkbookManager.TabViewType.MODEL_GAME:
                        contentType = GameData.ContentType.MODEL_GAME;
                        break;
                    case WorkbookManager.TabViewType.EXERCISE:
                        contentType = GameData.ContentType.EXERCISE;
                        break;
                    default:
                        contentType = GameData.ContentType.NONE;
                        break;
                }

                return contentType;
            }
        }

        /// <summary>
        /// Workbook's last update date.
        /// </summary>
        public DateTime? LastUpdate
        {
            get => _lastUpdate;
            set => _lastUpdate = value;
        }

        /// <summary>
        /// Description of the Workbook.
        /// In the file, this will be stored as the comment in the Workbook Preface.
        /// </summary>
        public string Description
        {
            get => _description;
            set => _description = value;
        }

        /// <summary>
        /// The title of this Workbook.
        /// </summary>
        public string Title
        {
            get
            {
                if (string.IsNullOrEmpty(_title))
                {
                    return "Untitled Workbook";
                }
                else
                {
                    return _title;
                }
            }
            set => _title = value;
        }

        /// <summary>
        /// Returns the Chapter object with the passed id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Chapter GetChapterById(int id)
        {
            return Chapters.FirstOrDefault(ch => ch.Id == id);
        }

        /// <summary>
        /// Return chapter from a given position in the Chapters list
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Chapter GetChapterByIndex(int idx)
        {
            if (idx >= 0 && idx < Chapters.Count)
            {
                return Chapters[idx];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Returns position of a chapter with a given id
        /// in the Chapters list.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public int GetChapterIndexFromId(int id)
        {
            int idx = -1;

            for (int i = 0; i < Chapters.Count; i++)
            {
                if (Chapters[i].Id == id)
                {
                    idx = i;
                    break;
                }
            }

            return idx;
        }

        /// <summary>
        /// Creates a new chapter.
        /// </summary>
        /// <param name="tree"></param>
        public Chapter CreateNewChapter()
        {
            Chapter chapter = new Chapter();
            chapter.StudyTree = new VariationTree(GameData.ContentType.STUDY_TREE);
            chapter.StudyTree.CreateNew();
            //TODO: we need to have a chapter specific version of SetupGuiForNewSession 
            chapter.Id = GenerateChapterId();

            Chapters.Add(chapter);
            _activeChapter = chapter;

            return chapter;
        }

        /// <summary>
        /// Creates a new chapter and adds the passed tree as the chapter's Study Tree.
        /// </summary>
        /// <param name="tree"></param>
        public Chapter CreateNewChapter(VariationTree tree)
        {
            Chapter chapter = new Chapter();
            chapter.StudyTree = tree;
            chapter.Id = 1;

            Chapters.Add(chapter);
            _activeChapter = chapter;

            TrainingSide = tree.TrainingSide;

            return chapter;
        }

        /// <summary>
        /// Returns the expand/collapse status of the chapter in the ChaptersView.
        /// If true the chapter view is expanded, if false, the chapter view is collapsed,
        /// null if chapter not found.
        /// </summary>
        /// <param name="chapterId"></param>
        /// <returns></returns>
        public bool? IsChapterViewExpanded(int chapterId)
        {
            bool? ret = null;

            foreach (Chapter chapter in Chapters)
            {
                if (chapter.Id == chapterId)
                {
                    ret = chapter.IsViewExpanded;
                    break;
                }
            }

            return ret;
        }

        /// <summary>
        /// Finds the highest chapter id in the list
        /// and increments it by one.
        /// </summary>
        /// <returns></returns>
        private int GenerateChapterId()
        {
            int id = 1;

            foreach (Chapter chapter in Chapters)
            {
                if (chapter.Id >= id)
                {
                    id = chapter.Id + 1;
                }
            }

            return id;
        }
    }
}
