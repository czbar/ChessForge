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
            // Identifies the added and modified sectors.
            // Inserts the added ones in the current LineSectorManager
            // and updates the modified ones.
            try
            {
                for (int i = 0; i < _updatedManager.LineSectors.Count; i++)
                {
                    TreeNode updatedNode = _updatedManager.LineSectors[i].Nodes[0];
                    TreeNode currentNode = i < _currentManager.LineSectors.Count ? _currentManager.LineSectors[i].Nodes[0] : null;

                    if (updatedNode == currentNode)
                    {
                        // update the existing sector if needed
                        bool needsupdate = UpdateSectorParagraph(_currentManager.LineSectors[i], _updatedManager.LineSectors[i]);
                        if (needsupdate)
                        {
                            _currentManager.LineSectors[i].ParaAttrs = _updatedManager.LineSectors[i].ParaAttrs;
                            _currentManager.LineSectors[i].Nodes = _updatedManager.LineSectors[i].Nodes;

                            foreach (TreeNode node in _currentManager.LineSectors[i].Nodes)
                            {
                                _view.RemoveNodeFromDictionaries(node);
                            }

                            if (_currentManager.LineSectors[i].HostPara == null)
                            {
                                CreateParagraphForNewSector(i);
                            }
                            _view.BuildSectorRuns(_currentManager.LineSectors[i]);
                        }
                    }
                    else
                    {
                        // insert the new sector
                        LineSector newSector = _updatedManager.LineSectors[i];
                        _currentManager.LineSectors.Insert(i, newSector);
                        CreateSectorParagraph(newSector);

                        foreach (TreeNode node in newSector.Nodes)
                        {
                            _view.RemoveNodeFromDictionaries(node);
                        }
                        _view.BuildSectorRuns(newSector);

                        if (i > 0)
                        {
                            _view.HostRtb.Document.Blocks.InsertAfter(_currentManager.LineSectors[i - 1].HostPara, newSector.HostPara);
                        }
                        else
                        {
                            _view.HostRtb.Document.Blocks.Add(newSector.HostPara);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("TreeViewUpdater.MoveAdded()", ex);
            }
        }

        /// <summary>
        /// Creates a Paragraph for sector that does not have one yet.
        /// </summary>
        /// <param name="index"></param>
        private void CreateParagraphForNewSector(int index)
        {
            LineSector newSector = _currentManager.LineSectors[index];
            CreateSectorParagraph(newSector);
            if (index > 0)
            {
                // insert after the paragraph of the previous sector
                LineSector prevSector = _currentManager.LineSectors[index - 1];
                _view.HostRtb.Document.Blocks.InsertAfter(prevSector.HostPara, newSector.HostPara);
            }
            else
            {
                // if the index paragraphs have been created, insert the new paragraph after them,
                // otherwise insert it after the header paragraph (which is always created earlier).
                if (_currentManager.IndexContentPara != null)
                {
                    _view.HostRtb.Document.Blocks.InsertAfter(_currentManager.IndexContentPara, _currentManager.LineSectors[index].HostPara);
                }
                else
                {
                    _view.HostRtb.Document.Blocks.InsertAfter(_view.PageHeaderParagraph, _currentManager.LineSectors[index].HostPara);
                }
            }
        }

        /// <summary>
        /// Checks if the sectors are identical in terms of content (nodes) and attributes.
        /// If they are not, the current sector is updated with the new sector's attributes
        /// and any runs with changed attributes are updated too.
        /// </summary>
        /// <param name="currentSector"></param>
        /// <param name="updatedSector"></param>
        private bool UpdateSectorParagraph(LineSector currentSector, LineSector updatedSector)
        {
            // update the paragraph attributes
            bool needsUpdate = UpdateParagraphAttrs(currentSector, updatedSector);

            if (!needsUpdate)
            {
                bool isOneSubsetOfOther = CompareNodeList(currentSector, updatedSector, out int commonNodes, out int extraNodes);
                if (isOneSubsetOfOther)
                {
                    needsUpdate = extraNodes != 0;
                }
                else
                {
                    needsUpdate = true;
                }
            }

            return needsUpdate;
        }

        /// <summary>
        /// Creates a paragraph for the new sector.
        /// </summary>
        /// <param name="newSector"></param>
        private void CreateSectorParagraph(LineSector newSector)
        {
            // insert the new sector
            Paragraph para = LineSectorManager.CreateStudyParagraph(newSector.ParaAttrs, newSector.DisplayLevel);
            newSector.HostPara = para;
        }

        /// <summary>
        /// Compares the lists of nodes of the two sectors.
        /// If one list is a subset of the other, retruns true,
        /// and the number of common nodes as well as the number extra nodes.
        /// If the updated sector has more nodes, the extra nodes will be positive
        /// otherwise negative.
        /// </summary>
        /// <param name="currentSector"></param>
        /// <param name="updatedSector"></param>
        /// <param name="commonNodes"></param>
        /// <param name="extraNodes"></param>
        /// <returns></returns>
        private bool CompareNodeList(LineSector currentSector, LineSector updatedSector, out int commonNodes, out int extraNodes)
        {
            bool isOneSubsetOfOther = true;

            extraNodes = 0;
            commonNodes = 0;

            int commonCount = Math.Min(currentSector.Nodes.Count, updatedSector.Nodes.Count);

            for (int i = 0; i < commonCount; i++)
            {
                if (currentSector.Nodes[i] != updatedSector.Nodes[i])
                {
                    isOneSubsetOfOther = false;
                    break;
                }
            }

            if (isOneSubsetOfOther)
            {
                extraNodes = updatedSector.Nodes.Count - currentSector.Nodes.Count;
            }

            return isOneSubsetOfOther;
        }

        /// <summary>
        /// Update only those attributes of the paragraph that have changed.
        /// We believe it may have some performance benefit. 
        /// </summary>
        /// <param name="currPara"></param>
        /// <param name="updatePara"></param>
        private bool UpdateParagraphAttrs(LineSector currentSector, LineSector updatedSector)
        {
            bool needsUpdate = false;

            SectorParaAttrs currAttrs = currentSector.ParaAttrs;
            SectorParaAttrs updatedAttrs = updatedSector.ParaAttrs;

            if (currentSector.BranchLevel != updatedSector.BranchLevel)
            {
                currentSector.BranchLevel = updatedSector.BranchLevel;
                needsUpdate = true;
            }

            if (currentSector.DisplayLevel != updatedSector.DisplayLevel)
            {
                currentSector.DisplayLevel = updatedSector.DisplayLevel;
                needsUpdate = true;
            }

            if (currAttrs.LevelGroup != updatedAttrs.LevelGroup)
            {
                currAttrs.LevelGroup = updatedAttrs.LevelGroup;
                needsUpdate = true;
            }

            if (currAttrs.FontSize != updatedAttrs.FontSize)
            {
                currAttrs.FontSize = updatedAttrs.FontSize;
                needsUpdate = true;
            }

            if (currAttrs.FontWeight != updatedAttrs.FontWeight)
            {
                currAttrs.FontWeight = updatedAttrs.FontWeight;
                needsUpdate = true;
            }

            if (currAttrs.FirstNodeColor != updatedAttrs.FirstNodeColor)
            {
                currAttrs.FirstNodeColor = updatedAttrs.FirstNodeColor;
                needsUpdate = true;
            }

            if (currAttrs.LastNodeColor != updatedAttrs.LastNodeColor)
            {
                currAttrs.LastNodeColor = updatedAttrs.LastNodeColor;
                needsUpdate = true;
            }

            if (currAttrs.Margin != updatedAttrs.Margin)
            {
                currAttrs.Margin = updatedAttrs.Margin;
                needsUpdate = true;
            }

            if (currAttrs.TopMarginExtra != updatedAttrs.TopMarginExtra)
            {
                currAttrs.TopMarginExtra = updatedAttrs.TopMarginExtra;
                needsUpdate = true;
            }

            if (currAttrs.BottomMarginExtra != updatedAttrs.BottomMarginExtra)
            {
                currAttrs.BottomMarginExtra = updatedAttrs.BottomMarginExtra;
                needsUpdate = true;
            }

            if (currAttrs.Foreground != updatedAttrs.Foreground)
            {
                currAttrs.Foreground = updatedAttrs.Foreground;
                needsUpdate = true;
            }

            if (currAttrs.Background != updatedAttrs.Background)
            {
                currAttrs.Background = updatedAttrs.Background;
                needsUpdate = true;
            }

            return needsUpdate;
        }
    }
}
