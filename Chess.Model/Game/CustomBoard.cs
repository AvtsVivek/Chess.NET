//-----------------------------------------------------------------------
// <copyright file="CustomBoard.cs">
//     Copyright (c) Michael Szvetits. All rights reserved.
// </copyright>
// <author>Michael Szvetits</author>
//-----------------------------------------------------------------------
namespace Chess.Model.Game
{
    using Chess.Model.CustomBoardIcon;
    using Chess.Model.Data;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Immutable;

    public class CustomBoard : IEnumerable<CustomBoardPlacedIcon>
    {
        /// <summary>
        /// Represents the chess pieces on the chess board.
        /// </summary>
        private readonly IImmutableDictionary<Position, CustomBoardIcon> icons;

        /// <summary>
        /// Initializes a new instance of the <see cref="Board"/> class.
        /// </summary>
        /// <param name="pieces">The chess pieces on the chess board.</param>
        public CustomBoard(IImmutableDictionary<Position, CustomBoardIcon> icons)
        {
            Validation.NotNull(icons, nameof(icons));
            this.icons = icons;
        }

        public IEnumerator<CustomBoardPlacedIcon> GetEnumerator()
        {
            foreach (var pair in this.icons)
            {
                yield return new CustomBoardPlacedIcon(pair.Key, pair.Value);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}