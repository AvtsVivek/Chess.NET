using Chess.Model.Command;
using Chess.Model.Data;
using Chess.Model.Game;
using Chess.Model.Piece;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Xml;
using System.Xml.Linq;

namespace Chess.Services
{
    public class XmlFileService
    {
        private XmlDocument xmlDocument;

        private XmlWriterSettings settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlFileService"/> class.
        /// </summary>
        /// <remarks>This constructor creates an instance of the <see cref="XmlFileService"/> class, 
        /// which can be used to perform operations related to XML file processing.  Additional initialization logic can
        /// be added as needed.</remarks>
        public XmlFileService()
        {
            this.xmlDocument = new();
            this.settings = new();
            this.settings.Indent = true;
            this.settings.IndentChars = ("\t");
            this.settings.OmitXmlDeclaration = true;
        }

        public List<ICommand> GetPieceMoveCommandsFromXmlFile(string fullFilePath)
        {
            XDocument doc = XDocument.Load(fullFilePath);
            XElement pieceMoveCommandElements = doc.Descendants(XmlConstants.PieceMoveCommandsElementName).First();
            List<XElement> commandElements = pieceMoveCommandElements.Elements(nameof(SequenceCommand)).ToList();
            commandElements.Reverse(); // Reverse the order to maintain the original sequence when executing

            List<ICommand> commands = new List<ICommand>();
            foreach (XElement commandElement in commandElements)
            {
                ICommand command = ParseCommandElement(commandElement);
                commands.Add(command);
            }

            return commands;
        }


        // Helper method to parse command XElement to ICommand
        private ICommand ParseCommandElement(XElement command)
        {
            if (command.Name.LocalName == "MoveCommand")
            {
                // Get piece element
                var pieceElement = command.Elements().FirstOrDefault(e =>
                    e.Name.LocalName == "Pawn" ||
                    e.Name.LocalName == "Knight" ||
                    e.Name.LocalName == "Bishop" ||
                    e.Name.LocalName == "Rook" ||
                    e.Name.LocalName == "Queen" ||
                    e.Name.LocalName == "King");

                // Get color
                var colorAttr = pieceElement?.Attribute(XmlConstants.PieceColorAttributeName);
                Color color = colorAttr != null && colorAttr.Value == "Black" ? Color.Black : Color.White;

                // Instantiate piece
                ChessPiece piece = pieceElement?.Name.LocalName switch
                {
                    "Pawn" => new Pawn(color),
                    "Knight" => new Knight(color),
                    "Bishop" => new Bishop(color),
                    "Rook" => new Rook(color),
                    "Queen" => new Queen(color),
                    "King" => new King(color),
                    _ => throw new InvalidOperationException("Unknown piece type")
                };

                // Get source and target positions
                var sourceElement = command.Element(XmlConstants.SourcePositionAttributeName);
                var targetElement = command.Element(XmlConstants.TargetPositionAttributeName);

                Position source = new Position(
                    int.Parse(sourceElement.Attribute(XmlConstants.RowAttributeName).Value) - 1,
                    int.Parse(sourceElement.Attribute(XmlConstants.ColumnAttributeName).Value) - 1);

                Position target = new Position(
                    int.Parse(targetElement.Attribute(XmlConstants.RowAttributeName).Value) - 1,
                    int.Parse(targetElement.Attribute(XmlConstants.ColumnAttributeName).Value) - 1);

                // Create MoveCommand instance
                var moveCommand = new MoveCommand(source, target, piece, isUndo: false);
                // Use moveCommand as needed
                return moveCommand;
            }
            // Example: parse SequenceCommand
            else if (command.Name.LocalName == "SequenceCommand")
            {
                var firstCommandElement = command.Elements().First();
                var secondCommandElement = command.Elements().Skip(1).First();

                // Recursively parse child commands
                ICommand firstCommand = ParseCommandElement(firstCommandElement);
                ICommand secondCommand = ParseCommandElement(secondCommandElement);

                var sequenceCommand = new SequenceCommand(firstCommand, secondCommand);
                // Use sequenceCommand as needed
                return sequenceCommand;
            }
            else if (command.Name.LocalName == "EndTurnCommand")
            {
                return new EndTurnCommand(false);
            }
            else if (command.Name.LocalName == "RemoveCommand")
            {
                // Get piece element
                var pieceElement = command.Elements().FirstOrDefault(e =>
                    e.Name.LocalName == "Pawn" ||
                    e.Name.LocalName == "Knight" ||
                    e.Name.LocalName == "Bishop" ||
                    e.Name.LocalName == "Rook" ||
                    e.Name.LocalName == "Queen" ||
                    e.Name.LocalName == "King");
                // Get color
                var colorAttr = pieceElement?.Attribute(XmlConstants.PieceColorAttributeName);
                Color color = colorAttr != null && colorAttr.Value == "Black" ? Color.Black : Color.White;
                // Instantiate piece
                ChessPiece piece = pieceElement?.Name.LocalName switch
                {
                    "Pawn" => new Pawn(color),
                    "Knight" => new Knight(color),
                    "Bishop" => new Bishop(color),
                    "Rook" => new Rook(color),
                    "Queen" => new Queen(color),
                    "King" => new King(color),
                    _ => throw new InvalidOperationException("Unknown piece type")
                };
                // Get position
                var positionElement = command.Element(XmlConstants.SourcePositionAttributeName);
                Position position = new Position(
                    int.Parse(positionElement.Attribute(XmlConstants.RowAttributeName).Value) - 1,
                    int.Parse(positionElement.Attribute(XmlConstants.ColumnAttributeName).Value) - 1);
                var removeCommand = new RemoveCommand(position, piece, isUndo: false);
                return removeCommand;
            }
            else if (command.Name.LocalName == "SpawnCommand")
            {
                // Get piece element
                var pieceElement = command.Elements().FirstOrDefault(e =>
                    e.Name.LocalName == "Pawn" ||
                    e.Name.LocalName == "Knight" ||
                    e.Name.LocalName == "Bishop" ||
                    e.Name.LocalName == "Rook" ||
                    e.Name.LocalName == "Queen" ||
                    e.Name.LocalName == "King");
                // Get color
                var colorAttr = pieceElement?.Attribute(XmlConstants.PieceColorAttributeName);
                Color color = colorAttr != null && colorAttr.Value == "Black" ? Color.Black : Color.White;
                // Instantiate piece
                ChessPiece piece = pieceElement?.Name.LocalName switch
                {
                    "Pawn" => new Pawn(color),
                    "Knight" => new Knight(color),
                    "Bishop" => new Bishop(color),
                    "Rook" => new Rook(color),
                    "Queen" => new Queen(color),
                    "King" => new King(color),
                    _ => throw new InvalidOperationException("Unknown piece type")
                };
                // Get position
                var positionElement = command.Element(XmlConstants.SourcePositionAttributeName);
                Position position = new Position(
                    int.Parse(positionElement.Attribute(XmlConstants.RowAttributeName).Value) - 1,
                    int.Parse(positionElement.Attribute(XmlConstants.ColumnAttributeName).Value) - 1);
                var spawnCommand = new SpawnCommand(position, piece, isUndo: false);
                return spawnCommand;
            }
            else if (command.Name.LocalName == "SetLastUpdateCommand")
            {
                // return new SetLastUpdateCommand();
                return null;
            }

            return null; // Placeholder
        }

        public ChessGame LoadBoardFromXmlFile(string fullFilePath)
        {
            var doc = XDocument.Load(fullFilePath);

            var piecesNode = doc.Descendants("Pieces").First();

            var allPlacedPieces = new List<PlacedPiece>();

            var pieceTypes = new[] { "Pawns", "Knights", "Bishops", "Rooks", "Queens", "King" };

            foreach (var pieceType in pieceTypes)
            {
                var whitePieceList = GetPieces(piecesNode, pieceType, Color.White);
                var blackPieceList = GetPieces(piecesNode, pieceType, Color.Black);
                allPlacedPieces.AddRange(whitePieceList);
                allPlacedPieces.AddRange(blackPieceList);
            }

            var firstPiece = allPlacedPieces[0];
            var weight = firstPiece.Piece.Weight;

            var emptyDictionary = ImmutableSortedDictionary.Create<Position, ChessPiece>(PositionComparer.DefaultComparer);
            
            var dict = allPlacedPieces.Aggregate(emptyDictionary, (s, p) => s.Add(p.Position, p.Piece));

            var board = new Board(dict);
            var whitePlayer = new Player(Color.White);
            var blackPlayer = new Player(Color.Black);
            return new ChessGame(board, whitePlayer, blackPlayer);
        }

        public void WriteToXmlFile(ChessGame game, string filePath)
        {
            Debug.WriteLine($"Writing game state to XML file: {filePath}");

            this.xmlDocument.RemoveAll();

            using (XmlWriter writer = XmlWriter.Create(filePath, settings))
            {
                writer.WriteStartElement(XmlConstants.RootElementName);
                writer.WriteStartElement(XmlConstants.PieceMoveCommandsElementName);
                writer.WriteEndElement();

                WriteStartPositionsToXmlFile(writer, game);

                writer.WriteEndElement();

                xmlDocument.Save(writer);
            }

            WriteCommandsToXmlFile(game, filePath);
        }

        /// <summary>
        /// Generates a unique file name for a chess game, based on the current date and time.
        /// </summary>
        /// <returns>A string representing the file name in the format "ChessGame-yyyy-MM-dd-HH-mm-ss.xml", where the timestamp
        /// corresponds to the current date and time.</returns>
        public static string GetFileName()
        {
            DateTime now = DateTime.Now;
            string fileName = $"ChessGame-{now:yyyy-MM-dd-HH-mm-ss}.xml";
            return fileName;
        }

        /// <summary>
        /// Writes the starting positions of chess pieces to an XML file using the specified <see cref="XmlWriter"/>.
        /// </summary>
        /// <remarks>This method generates an XML representation of the starting positions of the chess
        /// pieces based on the current state of the game board. If the game has a history of moves, the board state
        /// from the last move is used. The pieces are grouped by color (black and white) and ordered by piece type
        /// within each group.</remarks>
        /// <param name="writer">The <see cref="XmlWriter"/> used to write the XML content.</param>
        /// <param name="game">The <see cref="ChessGame"/> instance containing the game state and history.</param>
        private void WriteStartPositionsToXmlFile(XmlWriter writer, ChessGame game)
        {
            writer.WriteStartElement(XmlConstants.StartPositionsElementName);

            List<Update> history = game.History.ToList();

            var board = game.Board;

            if (history.Count != 0)
            {
                var lastUpdate = history.Last();
                board = lastUpdate.Game.Board;
            }

            var whitePiecesOrdered = board.Where(placedPiece
                => placedPiece.Color == Color.White)
                .OrderBy(placedPiece => placedPiece.Piece);

            var blackPiecesOrdered = board.Where(placedPiece
                => placedPiece.Color == Color.Black)
                .OrderBy(placedPiece => placedPiece.Piece);

            writer.WriteStartElement(XmlConstants.PiecesElementName);

            writer.WriteStartElement(XmlConstants.BlacksElementName);

            WritePieces(blackPiecesOrdered, writer);

            writer.WriteEndElement(); // End of Black

            writer.WriteStartElement(XmlConstants.WhitesElementName);

            WritePieces(whitePiecesOrdered, writer);

            writer.WriteEndElement(); // End of White

            writer.WriteEndElement(); // End of Pieces

            writer.WriteEndElement();
        }

        /// <summary>
        /// Writes the details of placed chess pieces to an XML writer, grouping them by weight and type.
        /// </summary>
        /// <remarks>The method groups the chess pieces by their weight and type, and writes them in
        /// ascending order of weight. Each group is represented as an XML element named after the piece type, with
        /// pluralization applied for all types except "King". Within each group, the positions of the pieces are
        /// written as child elements, ordered by row in descending order. The row and column values in the XML are
        /// 1-based.</remarks>
        /// <param name="placedPieces">A collection of <see cref="PlacedPiece"/> objects representing the placed chess pieces to be written.</param>
        /// <param name="writer">The <see cref="XmlWriter"/> used to write the XML output.</param>
        private void WritePieces(IEnumerable<PlacedPiece> placedPieces, XmlWriter writer)
        {
            var groupedPlacedPieces = placedPieces.GroupBy(
                    placedPiece => placedPiece.Piece.Weight,
                    placedPiece => placedPiece,
                    (key, g) => new
                    {
                        Weight = key,
                        PlacedPieces = g.ToList()
                    });

            foreach (var group in groupedPlacedPieces.OrderBy(g => g.Weight))
            {
                var typeName = group.PlacedPieces.First().Piece.GetType().Name;

                if (typeName != "King")
                {
                    typeName = typeName + "s"; // Pluralize the type name for all except King
                }

                writer.WriteStartElement(typeName);

                foreach (var piece in group.PlacedPieces.OrderByDescending(placedPiece => placedPiece.Position.Row))
                {
                    writer.WriteStartElement("Position");
                    writer.WriteAttributeString("Row", (piece.Position.Row + 1).ToString());
                    writer.WriteAttributeString("Column", (piece.Position.Column + 1).ToString());
                    writer.WriteEndElement();
                }

                writer.WriteEndElement(); // End of group (e.g., Pawn, Knight, etc.)
            }
        }

        private void WriteCommandsToXmlFile(ChessGame game, string filePath)
        {
            xmlDocument.Load(filePath);

            List<Update> history = game.History.ToList();

            history.Reverse(); // Reverse the history to start with the most recent update

            foreach (var update in history)
            {
                CreateAndAddUpdateCommandXmlElement(update);
            }

            // 5. Save the document
            using (XmlWriter writer = XmlWriter.Create(filePath, settings))
            {
                xmlDocument.Save(writer);
            }
        }

        private void CreateAndAddUpdateCommandXmlElement(Update update)
        {
            XmlNode parent = xmlDocument.SelectSingleNode(XmlConstants.RootElementName + "//" + XmlConstants.PieceMoveCommandsElementName);

            var xmlElement = GetCommandXmlElement(update.Command);

            parent?.PrependChild(xmlElement!);
        }

        private XmlElement? GetCommandXmlElement(ICommand command)
        {
            XmlElement xmlElement = xmlDocument.CreateElement(command.GetType().Name);
            switch (command)
            {
                case MoveCommand moveCommand:
                    {
                        XmlElement pieceElement = xmlDocument.CreateElement(moveCommand.Piece.GetType().Name);
                        pieceElement.SetAttribute(XmlConstants.PieceColorAttributeName, moveCommand.Piece.Color.ToString());

                        XmlElement sourceElement = xmlDocument.CreateElement(XmlConstants.SourcePositionAttributeName);
                        sourceElement.SetAttribute(XmlConstants.RowAttributeName, (moveCommand.Source.Row + 1).ToString());
                        sourceElement.SetAttribute(XmlConstants.ColumnAttributeName, (moveCommand.Source.Column + 1).ToString());

                        XmlElement targetElement = xmlDocument.CreateElement(XmlConstants.TargetPositionAttributeName);
                        targetElement.SetAttribute(XmlConstants.RowAttributeName, (moveCommand.Target.Row + 1).ToString());
                        targetElement.SetAttribute(XmlConstants.ColumnAttributeName, (moveCommand.Target.Column + 1).ToString());

                        xmlElement.AppendChild(pieceElement);
                        xmlElement.AppendChild(sourceElement);
                        xmlElement.AppendChild(targetElement);

                        return xmlElement;
                    }
                case SequenceCommand sequenceCommand:
                    {
                        var firstCommandXmlElement = GetCommandXmlElement(sequenceCommand.FirstCommand);
                        var secondCommandXmlElement = GetCommandXmlElement(sequenceCommand.SecondCommand);

                        if (firstCommandXmlElement != null)
                            xmlElement.AppendChild(firstCommandXmlElement);

                        if (secondCommandXmlElement != null)
                            xmlElement.AppendChild(secondCommandXmlElement);

                        return xmlElement;
                    }
                case EndTurnCommand endTurnCommand:
                    {
                        return xmlElement; // No additional attributes needed for EndTurnCommand
                    }
                case RemoveCommand removeCommand:
                    {
                        XmlElement pieceElement = xmlDocument.CreateElement(removeCommand.Piece.GetType().Name);
                        pieceElement.SetAttribute(XmlConstants.PieceColorAttributeName, removeCommand.Piece.Color.ToString());

                        XmlElement positionElement = xmlDocument.CreateElement(XmlConstants.SourcePositionAttributeName);
                        positionElement.SetAttribute(XmlConstants.RowAttributeName, (removeCommand.Position.Row + 1).ToString());
                        positionElement.SetAttribute(XmlConstants.ColumnAttributeName, (removeCommand.Position.Column + 1).ToString());

                        xmlElement.AppendChild(pieceElement);
                        xmlElement.AppendChild(positionElement);

                        return xmlElement;
                    }
                case SpawnCommand spawnCommand:
                    {
                        XmlElement pieceElement = xmlDocument.CreateElement(spawnCommand.Piece.GetType().Name);
                        pieceElement.SetAttribute(XmlConstants.PieceColorAttributeName, spawnCommand.Piece.Color.ToString());

                        XmlElement positionElement = xmlDocument.CreateElement(XmlConstants.SourcePositionAttributeName);
                        positionElement.SetAttribute(XmlConstants.RowAttributeName, (spawnCommand.Position.Row + 1).ToString());
                        positionElement.SetAttribute(XmlConstants.ColumnAttributeName, (spawnCommand.Position.Column + 1).ToString());

                        xmlElement.AppendChild(pieceElement);
                        xmlElement.AppendChild(positionElement);

                        return xmlElement;
                    }
                case SetLastUpdateCommand setLastUpdateCommand:
                    return xmlElement;
                default:
                    throw new NotSupportedException($"Unsupported command type: {command.GetType().Name}");
            }
        }

        private List<PlacedPiece> GetPieces(XElement piecesNode, string pieceType, Color color)
        {
            var placedPieces = new List<PlacedPiece>();

            var colorPieceNodes = piecesNode.Descendants(color.ToString() + "s");

            // Map pieceType string to the correct ChessPiece constructor
            Func<Color, ChessPiece> pieceFactory = pieceType switch
            {
                "Pawns" => c => new Pawn(c),
                "Knights" => c => new Knight(c),
                "Bishops" => c => new Bishop(c),
                "Rooks" => c => new Rook(c),
                "Queens" => c => new Queen(c),
                "King" => c => new King(c),
                _ => c => new Pawn(c) // fallback, should not happen
            };

            foreach (var piece in colorPieceNodes.Descendants(pieceType))
            {
                foreach (var position in piece.Descendants("Position"))
                {
                    var rowAttribute = position.Attribute("Row");
                    var columnAttribute = position.Attribute("Column");
                    if (rowAttribute == null || columnAttribute == null)
                        continue;
                    if (int.TryParse(rowAttribute.Value, out int row) && int.TryParse(columnAttribute.Value, out int column))
                    {
                        var placedPiece = new PlacedPiece(
                            new Position(row - 1, column - 1),
                            pieceFactory(color)
                        );
                        placedPieces.Add(placedPiece);
                    }
                }
            }
            return placedPieces;
        }
    }
}
