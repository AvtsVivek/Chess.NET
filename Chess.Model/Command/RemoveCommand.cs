//-----------------------------------------------------------------------
// <copyright file="RemoveCommand.cs">
//     Copyright (c) Michael Szvetits. All rights reserved.
// </copyright>
// <author>Michael Szvetits</author>
//-----------------------------------------------------------------------
namespace Chess.Model.Command
{
    using Chess.Model.Data;
    using Chess.Model.Game;
    using Chess.Model.Piece;
    using System;

    /// <summary>
    /// A command which indicates a chess piece removal.
    /// </summary>
    public class RemoveCommand : ICommand, IEquatable<RemoveCommand>
    {
        /// <summary>
        /// Represents the position of the chess piece to be removed.
        /// </summary>
        public readonly Position Position;

        /// <summary>
        /// Represents the chess piece to be removed.
        /// </summary>
        public readonly ChessPiece Piece;

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoveCommand"/> class.
        /// </summary>
        /// <param name="piece">The placed chess piece to be removed.</param>
        public RemoveCommand(PlacedPiece piece, bool isUndo = false, bool isPromotion = false) : this(piece.Position, piece.Piece, isUndo, isPromotion)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoveCommand"/> class.
        /// </summary>
        /// <param name="position">The position of the chess piece to be removed.</param>
        /// <param name="piece">The chess piece to be removed.</param>
        /// <param name="isUndo">Determines whether the command is being executed during undo.</param>
        /// <param name="isPromotion">Determines whether the command is a part of promotion.</param>
        public RemoveCommand(Position position, ChessPiece piece, bool isUndo = false, bool isPromotion = false)
        {
            Validation.NotNull(position, nameof(position));
            Validation.NotNull(piece, nameof(piece));

            this.Position = position;
            this.Piece = piece;
            this.IsUndo = isUndo;
            this.IsPromotion = isPromotion;
        }

        /// <summary>
        /// Determines the sequence should be executed in reverse order
        /// </summary>
        public bool IsUndo { get; private set; } = false;


        /// <summary>
        /// Gets a value indicating whether the current remove command is a part of promotion.
        /// </summary>
        public bool IsPromotion { get; private set; } = false;

        /// <summary>
        /// Applies the command to a chess game state.
        /// </summary>
        /// <param name="game">The old chess game state.</param>
        /// <returns>The new chess game state, if the command succeeds.</returns>
        public IMaybe<ChessGame> Execute(ChessGame game)
        {
            return game.Board.Remove(this.Position).Map
            (
                newBoard => game.SetBoard(newBoard)
            );
        }

        /// <summary>
        /// Accepts a command visitor in order to call its implementation for <see cref="RemoveCommand"/>.
        /// </summary>
        /// <param name="visitor">The command visitor to be called.</param>
        public void Accept(ICommandVisitor visitor)
        {
            visitor.Visit(this);
        }

        /// <summary>
        /// Accepts a command visitor in order to call its implementation for <see cref="RemoveCommand"/>.
        /// </summary>
        /// <typeparam name="T">The result type of the visitor when processing the command.</typeparam>
        /// <param name="visitor">The command visitor to be called.</param>
        /// <returns>The result of the visitor when processing the command.</returns>
        public T Accept<T>(ICommandVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }

        public bool Equals(RemoveCommand other)
        {
            return
                this.Position.Equals(other.Position) &&
                this.Piece.Equals(other.Piece) &&
                this.IsUndo == other.IsUndo &&
                this.IsPromotion == other.IsPromotion;
        }

        public override bool Equals(object obj)
        {
            return obj is RemoveCommand command && this.Equals(command);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.Position, this.Piece, this.IsUndo, this.IsPromotion);
        }

        public bool Equals(ICommand other)
        {
            return other is RemoveCommand command && this.Equals(command);
        }
    }
}