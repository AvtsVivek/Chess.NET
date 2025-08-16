using Chess.Model.Command;
using Chess.Model.Game;
using Chess.Model.Piece;
using System.Diagnostics;
using System.Xml;

namespace Chess.Services
{
    public class XmlFileService
    {
        private XmlDocument xmlDocument;

        private XmlWriterSettings settings;

        private string filePath = string.Empty;

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

        public void AddUpdateXmlToFile(Update update)
        {
            CreateAndAddUpdateCommandXmlElement(update);

            using (XmlWriter writer = XmlWriter.Create(filePath, settings))
            {
                this.xmlDocument.Save(writer);
            }
        }

        /// <summary>
        /// Writes the state of the specified chess game to an XML file in the specified folder.
        /// </summary>
        /// <remarks>The method generates a file name for the XML file and writes the chess game's state,
        /// including the starting positions, to the file. The XML file is formatted with indentation for readability,
        /// and the XML declaration is omitted.</remarks>
        /// <param name="folderPath">The path to the folder where the XML file will be created. This must be a valid, writable directory path.</param>
        /// <param name="game">The <see cref="ChessGame"/> object representing the current state of the chess game to be written to the XML
        /// file. Cannot be <see langword="null"/>.</param>
        public void WriteXmlFile(string folderPath, ChessGame game)
        {
            var fileName = GetFileName();

            filePath = Path.Combine(folderPath, fileName);

            Debug.WriteLine($"Writing game state to XML file: {filePath}");

            using (XmlWriter writer = XmlWriter.Create(filePath, settings))
            {
                writer.WriteStartElement(XmlConstants.RootElementName);
                writer.WriteStartElement(XmlConstants.MoveCommandsElementName);
                writer.WriteEndElement();

                WriteStartPositionsToXmlFile(writer, game);

                writer.WriteEndElement();

                xmlDocument.Save(writer);
            }

            WriteCommandsToXmlFile(game);
        }

        /// <summary>
        /// Generates a unique file name for a chess game, based on the current date and time.
        /// </summary>
        /// <returns>A string representing the file name in the format "ChessGame-yyyy-MM-dd-HH-mm-ss.xml", where the timestamp
        /// corresponds to the current date and time.</returns>
        private string GetFileName()
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

        private void WriteCommandsToXmlFile(ChessGame game)
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
            XmlNode parent = xmlDocument.SelectSingleNode(XmlConstants.RootElementName + "//" + XmlConstants.MoveCommandsElementName);

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
    }
}
