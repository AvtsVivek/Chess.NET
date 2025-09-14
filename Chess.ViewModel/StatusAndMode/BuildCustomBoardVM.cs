using Chess.ViewModel.Game;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
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

            int boardLength = (int)BoardConstants.BoardLength / 2; // This is 4, but using the constant for clarity.

            var fieldArray = new FieldVM[boardLength, boardLength];
            var fieldVMs =
               from row in Enumerable.Range(0, boardLength)
               from column in Enumerable.Range(0, boardLength)
               select new FieldVM(row, column);

            foreach (var field in fieldVMs)
            {
                fieldArray[field.Row, field.Column] = field;
            }

            this.fields = fieldArray;
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
    }
}
