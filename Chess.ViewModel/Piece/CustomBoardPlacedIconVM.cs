using Chess.Model.CustomBoardIcon;
using Chess.Model.Game;
using Chess.ViewModel.Game;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Media;

namespace Chess.ViewModel.Piece
{
    public partial class CustomBoardPlacedIconVM: ObservableObject
    {
        /// <summary>
        /// Represents the position of the placed chess piece.
        /// </summary>
        [ObservableProperty]
        private PositionVM position;

        /// <summary>
        /// Represents the placed chess piece.
        /// </summary>
        [ObservableProperty]
        private CustomBoardIcon icon;

        public CustomBoardPlacedIconVM(CustomBoardPlacedIcon customBoardPlacedIcon) : 
            this(customBoardPlacedIcon.Position, customBoardPlacedIcon.Icon)
        {
        }

        public CustomBoardPlacedIconVM(Position position, CustomBoardIcon customBoardIcon)
        {
            this.Position = new PositionVM(position);
            this.Icon = customBoardIcon;
        }

        public Brush SquareFill { get; set; } = new SolidColorBrush(BoardConstants.BoardFieldDarkBrushColor);
    }
}
