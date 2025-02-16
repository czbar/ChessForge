using GameTree;
using System;
using System.Windows.Documents;

namespace ChessForge
{
    /// <summary>
    /// Performs updating of the TreeView after a change has occured in the underlying Tree.
    /// </summary>
    public class TreeViewUpdater
    {
        /// <summary>
        /// The LineSectorManager for the pre-update view.
        /// </summary>
        private LineSectorManager _currentManager;

        /// <summary>
        /// The LineSectorManager for the post-update view.
        /// </summary>
        private LineSectorManager _updatedManager;

        /// <summary>
        /// The StudyTreeView object to be updated.
        /// </summary>
        private StudyTreeView _view;

        /// <summary>
        /// Constructs the updater object.
        /// </summary>
        /// <param name="current"></param>
        /// <param name="updated"></param>
        /// <param name="view"></param>
        public TreeViewUpdater(LineSectorManager current, LineSectorManager updated, StudyTreeView view)
        {
            _currentManager = current;
            _updatedManager = updated;
            _view = view;
        }

        /// <summary>
        /// Performs an update of the TreeView GUI 
        /// after a move was added to the underlying Tree.
        /// This is a special case of the update where we can assume the following.
        /// - the number of resulting sectors/paragraphs will be the same or greater then before.
        /// - any new sectors/paragraphs will be added in betwen the existing ones without
        ///   changing the order of the existing ones.
        /// - the existing sectors/paragraphs will start with the same move as before
        ///   but could be shorter or longer in terms of nodes.
        /// - if the number of sectors/paragraphs is the same, the existing sectors/paragraphs
        ///   will not need to have their attributes updated but a new node/run will be added
        ///   somewhere (not necessarily at the end).
        /// </summary>
        /// <param name="current"></param>
        /// <param name="updated"></param>
        /// <param name="view"></param>
        public void MoveAdded()
        {
            // Identified the added and modified sectors.
            // Insert the added ones in the current LineSectorManager
            // and update the modified ones.
            try
            {
                for (int i = 0; i < _updatedManager.LineSectors.Count; i++)
                {
                    TreeNode updatedNode = _updatedManager.LineSectors[i].Nodes[0];
                    TreeNode currentNode = i < _currentManager.LineSectors.Count ? _currentManager.LineSectors[i].Nodes[0] : null;

                    if (updatedNode == currentNode)
                    {
                        // update the existing sector if needed
                        UpdateSectorParagraph(_currentManager.LineSectors[i], _updatedManager.LineSectors[i]);
                    }
                    else
                    {
                        // insert the new sector
                        LineSector newSector = _updatedManager.LineSectors[i];
                        _currentManager.LineSectors.Insert(i, newSector);
                        InsertSectorParagraph(newSector);
                    }
                }
            }
            catch (Exception ex)
            {
                // log the exception
                AppLog.Message("TreeViewUpdater.MoveAdded()", ex);
            }
        }

        /// <summary>
        /// Checks if the sectors are identical in terms of content (nodes) and attributes.
        /// If they are not, the current sector is updated with the new sector's attributes
        /// and any runs with changed attributes are updated too.
        /// </summary>
        /// <param name="currentSector"></param>
        /// <param name="updatedSector"></param>
        private void UpdateSectorParagraph(LineSector currentSector, LineSector updatedSector)
        {
            // update the paragraph attributes
            currentSector.ParaAttrs = updatedSector.ParaAttrs;
            // update the paragraph runs
            // this is a bit more complicated because the runs could be different
            // in number and content.
            // We will need to compare the runs and update the current paragraph
            // with the updated runs.
        }

        /// <summary>
        /// Creates a paragraph for the new sector,
        /// builds its Runs and inserts it in the view.
        /// </summary>
        /// <param name="newSector"></param>
        private void InsertSectorParagraph(LineSector newSector)
        {
            // insert the new sector
            Paragraph para = LineSectorManager.CreateStudyParagraph(newSector.ParaAttrs);
            _view.BuildSectorRuns(newSector, para, null);
        }
    }
}
