using Chess.Model.Game;
using Chess.Model.Piece;
using System.Collections.Immutable;
using System.Text.Json;

namespace Chess.Services
{
    public class BoardSerializationService
    {
        public BoardSerializationService() { }
        public void SerializeBoard(ChessGame game)
        {
            // Convert board to a serializable DTO
            var pieces = game.Board.Select(placedPiece => new SerializablePiece
            {
                Row = placedPiece.Position.Row,
                Column = placedPiece.Position.Column,
                PieceType = placedPiece.Piece.GetType().Name,
                Color = placedPiece.Piece.Color.ToString()
            }).ToList();

            string json = JsonSerializer.Serialize(pieces);
            SerializeSettings.Default.BoardSerializedString = json;
            SerializeSettings.Default.Save();
        }
        public ChessGame? DeserializeBoardAndGetGame()
        {
            var pieceFactory = new PieceFactory();

            string boardDataFromSettings = SerializeSettings.Default.BoardSerializedString;
            if (string.IsNullOrWhiteSpace(boardDataFromSettings))
                return null;

            var pieces = JsonSerializer.Deserialize<List<SerializablePiece>>(boardDataFromSettings);
            if (pieces == null)
                return null;

            var dict = ImmutableSortedDictionary.CreateBuilder<Position, ChessPiece>(PositionComparer.DefaultComparer);
            foreach (var p in pieces)
            {
                var position = new Position(p.Row, p.Column);
                var piece = pieceFactory.CreatePiece(p.PieceType, p.Color == "White" ? Color.White : Color.Black);
                dict[position] = piece;
            }
            
            var board = new Board(dict.ToImmutable());

            var whitePlayer = new Player(Color.White);
            var blackPlayer = new Player(Color.Black);

            return new ChessGame(board, whitePlayer, blackPlayer);
        }

        private class SerializablePiece
        {
            public int Row { get; set; }
            public int Column { get; set; }
            public string PieceType { get; set; } = "";
            public string Color { get; set; } = "";
        }
    }
}
