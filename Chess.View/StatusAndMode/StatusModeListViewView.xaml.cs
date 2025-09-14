using System;
using System.Windows;
using System.Windows.Controls;

namespace Chess.View.StatusAndMode
{
    /// <summary>
    /// Interaction logic for StatusModeListViewView.xaml
    /// </summary>
    public partial class StatusModeListViewView : UserControl
    {
        /// <summary>
        /// Provides the functionality to convert a <see cref="GridLength"/> to a string and vice versa.
        /// </summary>
        private readonly GridLengthConverter gridLengthConverter;

        public StatusModeListViewView()
        {
            InitializeComponent();

            this.gridLengthConverter = new();

            if (!string.IsNullOrWhiteSpace(ChessAppSettings.Default.ChessMovesListViewRowHeight))
                ChessMovesListViewRow.Height = (GridLength)gridLengthConverter.ConvertFromString(ChessAppSettings.Default.ChessMovesListViewRowHeight);

            if (!string.IsNullOrWhiteSpace(ChessAppSettings.Default.ChessMovesNotesRowHeight))
                ChessMovesNotesRow.Height = (GridLength)gridLengthConverter.ConvertFromString(ChessAppSettings.Default.ChessMovesNotesRowHeight);

            PlayRadioButton.IsChecked = true;

            _chessMovesNotesRowHeight = ChessMovesNotesRow.Height;
            ChessMovesNotesRow.Height = new GridLength(0);
            HorizontalSplitterRow.Height = new GridLength(0);

        }

        private GridLength _chessMovesNotesRowHeight;

        private void HorizontalGridSplitter_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            var chessMovesListRowHeight = gridLengthConverter.ConvertToString(ChessMovesListViewRow.Height);
            var chessMovesNotesRowHeight = gridLengthConverter.ConvertToString(ChessMovesNotesRow.Height);

            ChessAppSettings.Default.ChessMovesListViewRowHeight = chessMovesListRowHeight;
            ChessAppSettings.Default.ChessMovesNotesRowHeight = chessMovesNotesRowHeight;
            ChessAppSettings.Default.Save();
        }

        private void PlayReCordReviewRadioButton_Click(object sender, RoutedEventArgs e)
        {
            var radioButton = sender as RadioButton;
            var mode = radioButton?.Tag as string;

            switch (mode)
            {
                case "Play":
                    {
                        _chessMovesNotesRowHeight = ChessMovesNotesRow.Height;
                        ChessMovesNotesRow.Height = new GridLength(0);
                        HorizontalSplitterRow.Height = new GridLength(0);
                    }
                    break;
                case "Record":
                    {
                        ChessMovesNotesRow.Height = _chessMovesNotesRowHeight;
                        HorizontalSplitterRow.Height = new GridLength(5);
                    }
                    break;
                case "Review":
                    {
                        ChessMovesNotesRow.Height = _chessMovesNotesRowHeight;
                        HorizontalSplitterRow.Height = new GridLength(5);
                    }
                    break;
                default:
                    throw new InvalidOperationException($"Unknown mode: {mode}");
            }
        }
    }
}
