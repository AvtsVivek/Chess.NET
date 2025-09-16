using Chess.Model.Piece;

namespace Chess.Model.CustomBoardIcon
{
    public class CustomBoardIcon
    {
        /// <summary>
        /// Represents the color of the chess piece.
        /// </summary>
        public readonly Color? Color;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChessPiece"/> class.
        /// </summary>
        /// <param name="color">The color of the chess piece.</param>
        public CustomBoardIcon(Color? color)
        {
            this.Color = color;
        }

        public override string ToString()
        {
            return $"CustomBoardChessIcon: {Color}";
        }

        public virtual string CustomBoardIconKey()
        {
            if(Color == null)
            {
                return this.GetType().Name;
            }
            return Color.ToString().ToLower() + this.GetType().Name;
        }
    }
}
