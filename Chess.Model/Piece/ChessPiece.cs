//-----------------------------------------------------------------------
// <copyright file="ChessPiece.cs">
//     Copyright (c) Michael Szvetits. All rights reserved.
// </copyright>
// <author>Michael Szvetits</author>
//-----------------------------------------------------------------------
namespace Chess.Model.Piece
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Represents a chess piece.
    /// </summary>
    [DebuggerDisplay("{Color} {GetType().Name}")]
    public abstract class ChessPiece : IChessPieceVisitable, IEquatable<ChessPiece>, IComparable<ChessPiece>
    {
        /// <summary>
        /// Represents the color of the chess piece.
        /// </summary>
        public readonly Color Color;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChessPiece"/> class.
        /// </summary>
        /// <param name="color">The color of the chess piece.</param>
        public ChessPiece(Color color)
        {
            this.Color = color;
        }

        /// <summary>
        /// Accepts a chess piece visitor in order to call it back based on the type of the piece.
        /// </summary>
        /// <param name="visitor">The chess piece visitor to be called back by the piece.</param>
        public abstract void Accept(IChessPieceVisitor visitor);

        /// <summary>
        /// Accepts a chess piece visitor in order to call it back based on the type of the piece.
        /// </summary>
        /// <typeparam name="T">The result type of the visitor when processing the chess piece.</typeparam>
        /// <param name="visitor">The chess piece visitor to be called back by the piece.</param>
        /// <returns>The result of the visitor when processing the chess piece.</returns>
        public abstract T Accept<T>(IChessPieceVisitor<T> visitor);

        /// <summary>
        /// Indicates whether the current chess piece is equal to another chess piece.
        /// </summary>
        /// <param name="other">The chess piece to compare with this chess piece.</param>
        /// <returns>True if the current chess piece is equal to the other one, or else false.</returns>
        public virtual bool Equals(ChessPiece other)
        {
            return
                this.Color == other.Color &&
                this.GetType() == other.GetType();
        }

        /// <summary>
        /// Indicates whether the current chess piece is equal to another object.
        /// </summary>
        /// <param name="obj">The object to compare with this chess piece.</param>
        /// <returns>True if the current chess piece is equal to the other object, or else false.</returns>
        public override bool Equals(object obj)
        {
            return
                obj is ChessPiece other &&
                this.Color == other.Color &&
                this.GetType() == other.GetType();
        }

        /// <summary>
        /// Calculates a hash code which represents the chess piece.
        /// </summary>
        /// <returns>A hash code for the chess piece.</returns>
        public override int GetHashCode()
        {
            var hashCodeBuilder = new HashCode();
            hashCodeBuilder.Add(this.GetType());
            hashCodeBuilder.Add(this.Color);
            return hashCodeBuilder.ToHashCode();
        }

        public virtual int Weight
        {
            get
            {
                int baseWeight = this switch
                {
                    Pawn _ => 1,
                    Knight _ => 3,
                    Bishop _ => 4,
                    Rook _ => 5,
                    Queen _ => 9,
                    King _ => 10,
                    _ => throw new NotSupportedException($"Unsupported chess piece type: {this.GetType().Name}"),
                };

                return this.Color == Color.White ? baseWeight : 20 + baseWeight;
            }
        }

        public int CompareTo(ChessPiece otherPiece)
        {
            return this.Weight - otherPiece.Weight;
        }

        public override string ToString()
        {
            return $"{this.Color} {this.GetType().Name}";
        }
    }
}