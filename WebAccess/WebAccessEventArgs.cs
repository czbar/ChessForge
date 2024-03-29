﻿using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebAccess
{
    /// <summary>
    /// EventArgs for the Web Access events
    /// </summary>
    public class WebAccessEventArgs : EventArgs
    {
        /// <summary>
        /// Whether event's result was success
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Id of the tree to which the handled Node belongs
        /// </summary>
        public int TreeId { get; set; }

        /// <summary>
        /// Id of the Node being handled.
        /// </summary>
        public int NodeId { get; set; }

        /// <summary>
        /// Lichess Id of a game 
        /// </summary>
        public string GameId { get; set; }

        /// <summary>
        /// Games filter if used in the event
        /// </summary>
        public GamesFilter GamesFilter { get; set; }

        /// <summary>
        /// Opening stats received from Lichess
        /// </summary>
        public LichessOpeningsStats OpeningStats { get; set; }

        /// <summary>
        /// List of GameData objects
        /// </summary>
        public List<GameData> GameData { get; set; }

        /// <summary>
        /// Text received from the server in reponse the query.
        /// For example, text of downloaded games.
        /// </summary>
        public string TextData { get; set; }
        
        /// <summary>
        /// A message to pass back to the caller
        /// e.g. the Exception error message.
        /// </summary>
        public string Message { get; set; }
    }
}
