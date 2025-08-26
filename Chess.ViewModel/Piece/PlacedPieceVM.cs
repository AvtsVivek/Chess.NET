//-----------------------------------------------------------------------
// <copyright file="PlacedPieceVM.cs">
//     Copyright (c) Michael Szvetits. All rights reserved.
// </copyright>
// <author>Michael Szvetits</author>
//-----------------------------------------------------------------------
namespace Chess.ViewModel.Piece
{
    using Chess.Model.Game;
    using Chess.Model.Piece;
    using Chess.ViewModel.Game;
    using CommunityToolkit.Mvvm.ComponentModel;

    /// <summary>
    /// Represents the view model of a chess piece placed on a chess board.
    /// </summary>
    public partial class PlacedPieceVM : ObservableObject, IChessPieceVisitable
    {
        /// <summary>
        /// Indicates whether the placed chess piece is marked for removal.
        /// </summary>
        [ObservableProperty]
        private bool removed;

        /// <summary>
        /// Represents the position of the placed chess piece.
        /// </summary>
        [ObservableProperty]
        private PositionVM position;

        /// <summary>
        /// Represents the placed chess piece.
        /// </summary>
        [ObservableProperty]
        private ChessPiece piece;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlacedPieceVM"/> class.
        /// </summary>
        /// <param name="piece">The placed chess piece, including its position.</param>
        public PlacedPieceVM(PlacedPiece piece) : this(piece.Position, piece.Piece)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlacedPieceVM"/> class.
        /// </summary>
        /// <param name="position">The position of the placed chess piece.</param>
        /// <param name="piece">The placed chess piece.</param>
        public PlacedPieceVM(Position position, ChessPiece piece)
        {
            this.Removed = false;
            this.Position = new PositionVM(position);
            this.Piece = piece;
        }

        /// <summary>
        /// Accepts a chess piece visitor in order to call it back based on the type of the piece.
        /// </summary>
        /// <param name="visitor">The chess piece visitor to be called back by the piece.</param>
        public void Accept(IChessPieceVisitor visitor)
        {
            this.Piece.Accept(visitor);
        }

        /// <summary>
        /// Accepts a chess piece visitor in order to call it back based on the type of the piece.
        /// </summary>
        /// <typeparam name="T">The result type of the visitor when processing the chess piece.</typeparam>
        /// <param name="visitor">The chess piece visitor to be called back by the piece.</param>
        /// <returns>The result of the visitor when processing the chess piece.</returns>
        public T Accept<T>(IChessPieceVisitor<T> visitor)
        {
            return this.Piece.Accept(visitor);
        }
    }
}