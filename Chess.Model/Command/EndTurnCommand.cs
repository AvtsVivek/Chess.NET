//-----------------------------------------------------------------------
// <copyright file="EndTurnCommand.cs">
//     Copyright (c) Michael Szvetits. All rights reserved.
// </copyright>
// <author>Michael Szvetits</author>
//-----------------------------------------------------------------------
namespace Chess.Model.Command
{
    using Chess.Model.Data;
    using Chess.Model.Game;
    using System;

    /// <summary>
    /// A command which indicates the end of a player's turn.
    /// </summary>
    public class EndTurnCommand : ICommand, IEquatable<EndTurnCommand>
    {
        /// <summary>
        /// Represents a command to end the current turn in a game.
        /// </summary>
        /// <param name="isUndo">A value indicating whether this command is an undo operation.  <see langword="true"/> if the command is
        /// intended to undo a previous action; otherwise, <see langword="false"/>.</param>
        public EndTurnCommand(bool isUndo = false)
        {
            this.IsUndo = isUndo;
        }

        /// <summary>
        /// Determines the sequence should be executed in reverse order
        /// </summary>
        public bool IsUndo { get; private set; } = false;

        /// <summary>
        /// Applies the command to a chess game state.
        /// </summary>
        /// <param name="game">The old chess game state.</param>
        /// <returns>The new chess game state, if the command succeeds.</returns>
        public IMaybe<ChessGame> Execute(ChessGame game)
        {
            return new Just<ChessGame>(game.EndTurn());
        }

        /// <summary>
        /// Accepts a command visitor in order to call its implementation for <see cref="EndTurnCommand"/>.
        /// </summary>
        /// <param name="visitor">The command visitor to be called.</param>
        public void Accept(ICommandVisitor visitor)
        {
            visitor.Visit(this);
        }

        /// <summary>
        /// Accepts a command visitor in order to call its implementation for <see cref="EndTurnCommand"/>.
        /// </summary>
        /// <typeparam name="T">The result type of the visitor when processing the command.</typeparam>
        /// <param name="visitor">The command visitor to be called.</param>
        /// <returns>The result of the visitor when processing the command.</returns>
        public T Accept<T>(ICommandVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }

        public bool Equals(EndTurnCommand other)
        {
            if (other is null)
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }
            return this.IsUndo == other.IsUndo;
        }

        public override bool Equals(object obj) => obj is EndTurnCommand command && this.Equals(command);

        public override int GetHashCode() => HashCode.Combine(this.IsUndo);

        public bool Equals(ICommand other)
        {
            return other is EndTurnCommand command && this.Equals(command);
        }
    }
}