using Chess.Model.Piece;

namespace Chess.Services
{
    public class PieceFactory
    {
        public ChessPiece CreatePiece(string pieceType, Color color)
        {
            return pieceType switch
            {
                "Pawn" => new Pawn(color),
                "Knight" => new Knight(color),
                "Bishop" => new Bishop(color),
                "Rook" => new Rook(color),
                "Queen" => new Queen(color),
                "King" => new King(color),
                _ => throw new InvalidOperationException("Unknown piece type")
            };
        }
    }
}
