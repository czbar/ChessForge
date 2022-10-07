using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public List<Chapter> Chapters = new List<Chapter>();

        /// <summary>
        /// The training side.
        /// </summary>
        public PieceColor TrainingSide = PieceColor.None;

        // chapter currently open in the session
        private Chapter _activeChapter;

        // last update date
        private DateTime? _lastUpdate;

        // workbook description string
        private string _description;

        // workbook title
        private string _title;

        /// <summary>
        /// The chapter currently open in the session.
        /// </summary>
        public Chapter ActiveChapter
        {
            get
            {
                if (_activeChapter == null)
                {
                    return Chapters[0];
                }
                return _activeChapter;
            }
            set => _activeChapter = value;
        }

        /// <summary>
        /// Sets Active Chapter and Tree given the index of the chapter in the Chapters list.
        /// </summary>
        /// <param name="chapterIndex">index of the requested chapter in the Chapters list.</param>
        /// <param name="gameType"></param>
        /// <param name="gameIndex">index in the list of elements of the requested type i.e. Model Games or Exercises </param>
        public void SetActiveChapterTreeByIndex(int chapterIndex, GameMetadata.GameType gameType, int gameIndex = 0)
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
        public void SetActiveChapterTreeById(int chapterId, GameMetadata.GameType gameType, int gameIndex = 0)
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
                return _activeChapter.ActiveVariationTree;
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
        public Chapter GetChapter(int id)
        {
            return Chapters.FirstOrDefault(ch => ch.Id == id);
        }

        /// <summary>
        /// Creates a new chapter.
        /// </summary>
        /// <param name="tree"></param>
        public Chapter CreateNewChapter()
        {
            Chapter chapter = new Chapter();
            chapter.StudyTree = new VariationTree();
            chapter.StudyTree.CreateNew();
            //TODO: we need to have a chapter specific version of SetupGuiForNewSession 
            chapter.Id = Chapters.Count + 1;

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
    }
}
