//-----------------------------------------------------------------------
// <copyright file="ChessGame.cs">
//     Copyright (c) Michael Szvetits. All rights reserved.
// </copyright>
// <author>Michael Szvetits</author>
//-----------------------------------------------------------------------
namespace Chess.Model.Game
{
    using Chess.Model.Data;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    /// <summary>
    /// Represents a chess game.
    /// </summary>
    [DebuggerDisplay("GameId: {GameId}, LastUpdateId: {LastUpdateId} , NextUpdateId: {NextUpdateId}")]
    public class ChessGame
    {
        // Just for debugging and understanding purposes.
        // Represents a counter for the number of instances of <see cref="ChessGame"/> created so far.
        public static int InstanceCounter;

        // Just for debugging and understanding purposes.
        // Represents a unique identifier for the current instance.
        public int GameId { get; set; }

        // Just for debugging and understanding purposes.
        // Represents the id of the last update that has led to this game state.
        public int LastUpdateId 
        {
            get
            {
                var update = this.LastUpdate;
                if (update != null && update.HasValue)
                {
                    return update.Yield().ToList().FirstOrDefault().UpdateId;
                }
                else
                {
                    return -1; // represents null.
                }
            }
        }

        // Just for debugging and understanding purposes.
        // Represents the id of the next update that will lead to the next game state.
        public int NextUpdateId
        {
            get
            {
                var update = this.NextUpdate;
                if (update != null && update.HasValue)
                {
                    return update.Yield().ToList().FirstOrDefault().UpdateId;
                }
                else
                {
                    return -1; // represents null.
                }
            }
        }

        /// <summary>
        /// Represents the current state of the chess board.
        /// </summary>
        public readonly Board Board;

        /// <summary>
        /// Represents the player who has currently the right to move.
        /// </summary>
        public readonly Player ActivePlayer;

        /// <summary>
        /// Represents the player who is currently waiting for the opponent's move.
        /// </summary>
        public readonly Player PassivePlayer;

        /// <summary>
        /// Represents the last update that has led to this game state.
        /// </summary>
        public readonly IMaybe<Update> LastUpdate;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChessGame"/> class.
        /// </summary>
        /// <param name="board">The current state of the chess board.</param>
        /// <param name="activePlayer">The player who has currently the right to move.</param>
        /// <param name="passivePlayer">The player who is currently waiting for the opponent's move.</param>
        public ChessGame(Board board, Player activePlayer, Player passivePlayer)
            : this(board, activePlayer, passivePlayer, Nothing<Update>.Instance)
        {
        }


        private ChessGame(Board board, Player activePlayer, Player passivePlayer, IMaybe<Update> lastUpdate)
            : this(board, activePlayer, passivePlayer, lastUpdate, Nothing<Update>.Instance)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChessGame"/> class.
        /// </summary>
        /// <param name="board">The current state of the chess board.</param>
        /// <param name="activePlayer">The player who has currently the right to move.</param>
        /// <param name="passivePlayer">The player who is currently waiting for the opponent's move.</param>
        /// <param name="lastUpdate">The update that has led to the newly created game state.</param>
        /// <param name="nextUpdate">The update that will lead next game state.</param>
        private ChessGame(Board board, Player activePlayer, Player passivePlayer, IMaybe<Update> lastUpdate, IMaybe<Update> nextUpdate)
        {
            Validation.NotNull(board, nameof(board));
            Validation.NotNull(activePlayer, nameof(activePlayer));
            Validation.NotNull(passivePlayer, nameof(passivePlayer));
            Validation.NotNull(lastUpdate, nameof(lastUpdate));
            Validation.NotNull(nextUpdate, nameof(nextUpdate));

            this.Board = board;
            this.ActivePlayer = activePlayer;
            this.PassivePlayer = passivePlayer;
            this.LastUpdate = lastUpdate;
            this.NextUpdate = nextUpdate;

            InstanceCounter++;
            
            this.GameId = InstanceCounter;
        }

        public static Dictionary<int, string> TitleNotesDictionary = new();

        /// <summary>
        /// Represents the update that will lead to the next game state.
        /// </summary>
        public IMaybe<Update> NextUpdate { get; set; }

        /// <summary>
        /// Gets the history of updates that led to this game state.
        /// </summary>
        /// <value>An enumerable which contains the history of updates, starting with the newest update.</value>
        public IEnumerable<Update> History
        {
            get
            {
                return this.LastUpdate.GetOrElse
                (
                    u =>
                    {
                        // Just for debugging and understanding purposes.
                        //var history = u.Game.History;
                        //var prependedHistory = Enumerable.Prepend(history, u);
                        //return prependedHistory;
                        var update = u.Yield().First();
                        //update.AssignId();
                        var historyCount = u.Game.History.Count();
                        update.Id = historyCount + 1;
                        var t = Enumerable.Prepend(u.Game.History, u);
                        // var historyCountTwo = u.Game.History.Count();
                        return t;
                    },
                    
                    Enumerable.Empty<Update>()
                );
            }
        }

        /// <summary>
        /// Sets the last update that has led to this game state.
        /// </summary>
        /// <param name="update">The last update that has led to this game state.</param>
        /// <returns>The new chess game state, including the specified <see cref="LastUpdate"/>.</returns>
        public ChessGame SetLastUpdate(IMaybe<Update> update)
        {
            Validation.NotNull(update, nameof(update));

            return new ChessGame
            (
                this.Board,
                this.ActivePlayer,
                this.PassivePlayer,
                update,
                this.NextUpdate
            );
        }

        /// <summary>
        /// Sets a new state of the chess board.
        /// </summary>
        /// <param name="board">The new state of the chess board.</param>
        /// <returns>The new chess game state with the updated chess board.</returns>
        public ChessGame SetBoard(Board board)
        {
            Validation.NotNull(board, nameof(board));

            return new ChessGame
            (
                board,
                this.ActivePlayer,
                this.PassivePlayer,
                this.LastUpdate,
                this.NextUpdate
            );
        }

        /// <summary>
        /// Finishes the turn of the active player.
        /// This essentially swaps <see cref="ActivePlayer"/> and <see cref="PassivePlayer"/>.
        /// </summary>
        /// <returns>The new chess game state, including the swapped player status.</returns>
        public ChessGame EndTurn()
        {
            return new ChessGame
            (
                this.Board,
                this.PassivePlayer,
                this.ActivePlayer,
                this.LastUpdate,
                this.NextUpdate
            );
        }
    }
}