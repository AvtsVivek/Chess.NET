using Chess.Model.CustomBoardIcon;
using Chess.Model.Game;
using Chess.Model.Piece;
using Chess.ViewModel.Game;
using Chess.ViewModel.Piece;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace Chess.ViewModel.StatusAndMode
{
    public partial class BuildCustomBoardVM: ObservableObject
    {
        /// <summary>
        /// Represents the currently presented fields of the chess board.
        /// </summary>
        private readonly FieldVM[,] fields;

        public BuildCustomBoardVM()
        {

            int boardLength = (int)BoardConstants.BoardFieldLength / 2; // This is 4, but using the constant for clarity.

            var fieldArray = new FieldVM[boardLength, boardLength];
            var fieldVMs =
               from row in Enumerable.Range(0, boardLength)
               from column in Enumerable.Range(0, boardLength)
               select new FieldVM(row, column, 0);

            foreach (var field in fieldVMs)
            {
                fieldArray[field.Row, field.Column] = field;
            }

            this.fields = fieldArray;

            // Set up pieces in starting position for testing purposes.
            IEnumerable<CustomBoardPlacedIcon> makeBaseLineForCustomBoardPlacedIcon()
            {
                yield return new CustomBoardPlacedIcon(new Position(3, 0), new CustomBoardKingIcon(Color.White));
                yield return new CustomBoardPlacedIcon(new Position(3, 1), new CustomBoardKingIcon(Color.Black));
                yield return new CustomBoardPlacedIcon(new Position(3, 2), new CustomBoardQueenIcon(Color.White));
                yield return new CustomBoardPlacedIcon(new Position(3, 3), new CustomBoardQueenIcon(Color.Black));

                yield return new CustomBoardPlacedIcon(new Position(2, 0), new CustomBoardRookIcon(Color.White));
                yield return new CustomBoardPlacedIcon(new Position(2, 1), new CustomBoardRookIcon(Color.Black));
                yield return new CustomBoardPlacedIcon(new Position(2, 2), new CustomBoardBishopIcon(Color.White));
                yield return new CustomBoardPlacedIcon(new Position(2, 3), new CustomBoardBishopIcon(Color.Black));

                yield return new CustomBoardPlacedIcon(new Position(1, 0), new CustomBoardKnightIcon(Color.White));
                yield return new CustomBoardPlacedIcon(new Position(1, 1), new CustomBoardKnightIcon(Color.Black));
                yield return new CustomBoardPlacedIcon(new Position(1, 2), new CustomBoardPawnIcon(Color.White));
                yield return new CustomBoardPlacedIcon(new Position(1, 3), new CustomBoardPawnIcon(Color.Black));

                yield return new CustomBoardPlacedIcon(new Position(0, 1), new CustomBoardStdBoardIcon());
                yield return new CustomBoardPlacedIcon(new Position(0, 2), new CustomBoardDeleteDustbinIcon());
                yield return new CustomBoardPlacedIcon(new Position(0, 3), new CustomBoardOkTickMarkIcon());

            }

            IImmutableDictionary<Position, CustomBoardIcon> makeCustomBoardIcons()
            {
                var pieces = makeBaseLineForCustomBoardPlacedIcon();
                var empty = ImmutableSortedDictionary.Create<Position, CustomBoardIcon>(PositionComparer.DefaultComparer);
                return pieces.Aggregate(empty, (s, p) => s.Add(p.Position, p.Icon));
            }

            var allCustomBoardIcons = makeCustomBoardIcons();
            var emptyPieceSetForTesting = ImmutableSortedDictionary<Position, ChessPiece>.Empty;
            var customBoard = new CustomBoard(allCustomBoardIcons);
            var icons = customBoard.Select(p => new CustomBoardPlacedIconVM(p));
            this.Icons = new ObservableCollection<CustomBoardPlacedIconVM>(icons);
        }

        public void HandleBoardClick(int row, int column)
        { 
            var selectedPosition = new Position(row, column);

            if (this.Fields.Where(field => field.Row == row && field.Column == column).Any())
            {
                if(row == 0 && column == 0)
                {
                    return;
                }

                this.Fields.ToList().ForEach(field => field.IsTarget = false);
                this.Fields.Where(field => field.Row == row && field.Column == column).First().IsTarget = true;
            }

            if (this.Icons.Where(icon => icon.Position.Position.Equals(selectedPosition)).Any())
            {
                var iconSelected = this.Icons.Where(icon => icon.Position.Position.Equals(selectedPosition)).First().Icon.CustomBoardIconKey();
                Debug.WriteLine($"BuildCustomBoardVM: Icon {iconSelected} selected.");
            }
        }

        /// <summary>
        /// Gets a sequence of the currently presented chess board fields.
        /// </summary>
        /// <value>A sequence of the chess board fields.</value>
        public IEnumerable<FieldVM> Fields
        {
            get
            {
                var rowCount = this.fields.GetLength(0);
                var columnCount = this.fields.GetLength(1);

                return
                    from row in Enumerable.Range(0, rowCount)
                    from column in Enumerable.Range(0, columnCount)
                    select this.fields[row, column];
            }
        }

        public ObservableCollection<CustomBoardPlacedIconVM> Icons { get; }
        public ObservableCollection<PlacedPieceVM> Pieces { get; }
    }
}
