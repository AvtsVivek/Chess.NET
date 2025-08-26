//-----------------------------------------------------------------------
// <copyright file="FieldVM.cs">
//     Copyright (c) Michael Szvetits. All rights reserved.
// </copyright>
// <author>Michael Szvetits</author>
//-----------------------------------------------------------------------
namespace Chess.ViewModel.Game
{
    using CommunityToolkit.Mvvm.ComponentModel;
    using System.ComponentModel;

    /// <summary>
    /// Represents the view model of a chess board field.
    /// </summary>
    public class FieldVM : ObservableObject
    {
        /// <summary>
        /// Indicates if the field is a possible target for the user to select.
        /// </summary>
        private bool isTarget;

        /// <summary>
        /// Initializes a new instance of the <see cref="FieldVM"/> class.
        /// </summary>
        /// <param name="row">The row of the field, where 0 represents the bottom row.</param>
        /// <param name="column">The column of the field, where 0 represents the leftmost column.</param>
        public FieldVM(int row, int column)
        {
            this.Row = row;
            this.Column = column;
            this.IsTarget = false;
        }

        /// <summary>
        /// Gets the distance of the column from the left boundary of the board, in double units
        /// </summary>
        /// <value>The distance of the field from the left of the board</value>
        public double DistanceFromLeft 
        {
            get 
            {
                return Column + BoardConstants.BoardMarginForId;
            } 
        }

        /// <summary>
        /// Gets the distance of the row from the bottom of the board, in double units
        /// </summary>
        /// <value>The distance of the field from the bottom of the board</value>
        public double DistanceFromBottom
        {
            get
            {
                return Row - BoardConstants.BoardMarginForId;
            }
        }

        /// <summary>
        /// Gets the row of the field, where 0 represents the bottom row.
        /// </summary>
        /// <value>The row index of the field.</value>
        public int Row { get; }

        /// <summary>
        /// Gets the column of the field, where 0 represents the leftmost column.
        /// </summary>
        /// <value>The column index of the field.</value>
        public int Column { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the field is a possible target for the user to select.
        /// </summary>
        /// <value>True if the field is a possible target, or else false.</value>
        public bool IsTarget
        {
            get
            {
                return this.isTarget;
            }

            set
            {
                if (this.isTarget != value)
                {
                    this.isTarget = value;
                    this.OnPropertyChanged(nameof(this.IsTarget));
                }
            }
        }
    }
}