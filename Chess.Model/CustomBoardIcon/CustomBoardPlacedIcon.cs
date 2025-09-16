using Chess.Model.Data;
using Chess.Model.Game;


namespace Chess.Model.CustomBoardIcon
{
    public class CustomBoardPlacedIcon : CustomBoardIcon
    {
        /// <summary>
        /// Represents the position of the chess piece on the chess board.
        /// </summary>
        public readonly Position Position;

        /// <summary>
        /// Represents the positioned chess piece.
        /// </summary>
        public readonly CustomBoardIcon Icon;

        public CustomBoardPlacedIcon(Position position, CustomBoardIcon icon) : base(icon.Color)
        {
            Validation.NotNull(position, nameof(position));
            Validation.NotNull(icon, nameof(icon));

            this.Position = position;
            this.Icon = icon;
        }

        public override string CustomBoardIconKey()
        {
            return this.Icon.CustomBoardIconKey();
        }
    }
}
