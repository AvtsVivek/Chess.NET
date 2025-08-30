using CommunityToolkit.Mvvm.ComponentModel;
using System.Diagnostics;
using System.Windows;

namespace Chess.ViewModel.Game
{
    /// <summary>
    /// Represents the view model of a chess board row or a column.
    /// </summary>
    [DebuggerDisplay("R:{Row}, C:{Column}, L:{Label}, H:{Height}, W:{Width}")]
    public class RowColumnLabelVM : ObservableObject
    {
        /// <summary>
        /// The row of the label, where 0 represents the bottom row.
        /// </summary>
        /// <value>The row index of the label.</value>
        public int Row { get; set; }

        /// <summary>
        /// The column of the label, where 0 represents the left column.
        /// </summary>
        /// <value>The column index of the label.</value>
        public int Column { get; set; }

        /// <summary>
        /// The text value of the label. Its 1 till 8 for rows displayed to the left and right of the board and 
        /// 'A' till 'H' for columns displayed below and above the board.
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// The width of the label
        /// </summary>
        public double Width { get; set; }

        /// <summary>
        /// The Height of the label
        /// </summary>
        public double Height { get; set; }

        /// <summary>
        /// Gets the distance from the bottom of the board, where the label needs to be placed in double units.
        /// </summary>
        /// <value>The distance of the label from the bottom of the board</value>
        public double DistanceFromBottom { get; set; }

        /// <summary>
        /// Gets the distance from the left of the board, where the label needs to be placed in double units.
        /// </summary>
        /// <value>The distance of the label from the left of the board</value>
        public double DistanceFromLeft { get; set; }

        /// <summary>
        /// Gets or sets the resource key for the label, which is used to find the right data template in the application resources.
        /// </summary>
        /// <value>The resource key for the row or column label.</value>
        public string LabelResourceKey { get; set; }

        /// <summary>
        /// Gets or sets the margin to be applied to the label, which is used to position the label correctly within its container.
        /// </summary>
        public Thickness RowColumnIdDynamicMargin { get; set; }
    }
}
