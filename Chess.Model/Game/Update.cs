//-----------------------------------------------------------------------
// <copyright file="Update.cs">
//     Copyright (c) Michael Szvetits. All rights reserved.
// </copyright>
// <author>Michael Szvetits</author>
//-----------------------------------------------------------------------
namespace Chess.Model.Game
{
    using Chess.Model.Command;
    using Chess.Model.Data;
    using System.Diagnostics;

    /// <summary>
    /// Represents an update of a chess game state.
    /// </summary>
    [DebuggerDisplay("Id: {Id}, UpdateId: {UpdateId}, Desc: {Description}")]
    public class Update
    {
        /// <summary>
        /// Gets or sets the unique identifier for the current instance.
        /// This is temporary, just for debugging and understanding purposes.
        /// Will be removed in the future.
        /// </summary>
        private static int InstanceCounter;

        private static int IdCounter = 0;

        /// <summary>
        /// Represents the chess game state before or after executing the corresponding <see cref="Command"/>,
        /// depending on the context.
        /// </summary>
        public readonly ChessGame Game;

        /// <summary>
        /// Represents the command that is involved in the game state update.
        /// </summary>
        public readonly ICommand Command;

        /// <summary>
        /// Initializes a new instance of the <see cref="Update"/> class.
        /// </summary>
        /// <param name="game">
        /// The chess game state before or after executing the corresponding command,
        /// depending on the context.
        /// </param>
        /// <param name="command">The command that is involved in the game state update.</param>
        public Update(ChessGame game, ICommand command, string description, int id = 0)
        {
            Validation.NotNull(game, nameof(game));
            Validation.NotNull(command, nameof(command));

            this.Game = game;
            this.Command = command;

            InstanceCounter++;

            this.UpdateId = InstanceCounter;

            Description = description;

            if (id != 0)
            {
                IdCounter = id;
                Id = id;
            }        
        }

        // Just for debugging and understanding purposes. This is instance counter.
        public int UpdateId { get; set; }

        // Just for debugging and understanding purposes.
        public bool IsSelected { get; set; }

        // Just for debugging and understanding purposes.
        public string Description { get; set; }

        /// <summary>
        /// Represents the external unique identifier of the update. 
        /// This interacts with each move in a chess game as per the history of chess moves.
        /// </summary>

        public int Id { get; set; }

    }
}