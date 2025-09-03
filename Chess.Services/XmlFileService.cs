using Chess.Model.Command;
using Chess.Model.Data;
using Chess.Model.Game;
using Chess.Model.Piece;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Xps.Packaging;
using System.Xml;
using System.Xml.Linq;

namespace Chess.Services
{
    public class XmlFileService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="XmlFileService"/> class.
        /// </summary>
        /// <remarks>This constructor creates an instance of the <see cref="XmlFileService"/> class, 
        /// which can be used to perform operations related to XML file processing.  Additional initialization logic can
        /// be added as needed.</remarks>
        public XmlFileService()
        {

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

        public ChessGame GetPieceMoveCommandsFromXmlFile(string fullFilePath)
        {
            ChessGame chessGame = LoadBoardFromXmlFile(fullFilePath);
            XDocument doc = XDocument.Load(fullFilePath);
            XElement pieceMoveCommandElements = doc.Descendants(XmlConstants.PieceMoveCommandsElementName).First();
            List<XElement> commandElements = pieceMoveCommandElements.Elements(nameof(SequenceCommand)).ToList();
            commandElements.Reverse(); // Reverse the order to maintain the original sequence when executing

            var parsedCommandsWithIds = new List<(ICommand, int)>();

            var success = false;
            var updateId = 0;
            foreach (XElement commandElement in commandElements)
            {
                ICommand command = ParseCommandElement(commandElement)!;
                if (commandElement.Name.LocalName == "SequenceCommand")
                {
                    if (commandElement.Attribute("Id") != null)
                    {
                        XAttribute idAttribute = commandElement.Attribute("Id")!;
                        if (idAttribute != null)
                        {
                            string idValue = idAttribute.Value;
                            success = int.TryParse(idValue, out updateId);
                        }
                    }
                }
                if (success)
                {
                    parsedCommandsWithIds.Add((command, updateId));
                }
                else
                {
                    parsedCommandsWithIds.Add((command, 0));
                }
            }

            ChessGame? updatedGame = chessGame;

            foreach (var parsedCommandWithId in parsedCommandsWithIds)
            {
                var Update = new Update(updatedGame, parsedCommandWithId.Item1, "XmlFileRead", parsedCommandWithId.Item2);
                var setLastUpdateCommand = new SetLastUpdateCommand(Update);
                ICommand command = new SequenceCommand(parsedCommandWithId.Item1, setLastUpdateCommand);
                var updates = command.Execute(updatedGame).Map(g => new Update(g, command, "XmlFileRead")).Yield();
                if (!updates.Any())
                {
                    continue;
                }
                Update? update = updates.First();
                updatedGame!.NextUpdate = new Just<Update>(update);
                updatedGame = update?.Game;
            }

            return updatedGame!;
        }

        public void WriteGameToXmlFile(ChessGame game, string filePath)
        {
            XmlDocument xmlDocument = new();

            if (game == null)
                throw new ArgumentNullException(nameof(game));

            if(filePath == null)
                throw new ArgumentNullException(nameof(filePath));

            if (game.History == null || !game.History.Any())
                throw new InvalidOperationException("The game has no history to write.");

            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be empty or whitespace.", nameof(filePath));

            if (!File.Exists(filePath))
            {
                CreateAndWriteToXmlFile(xmlDocument, game, filePath);
            }
            else
            {
                AddLatestUpdateToXmlFile(xmlDocument, game, filePath);
            }

            XmlWriterSettings settings = new();
            settings.Indent = true;
            settings.Encoding = Encoding.UTF8;
            settings.IndentChars = ("\t");
            settings.OmitXmlDeclaration = false;

            // Save with settings if needed
            using (var writer = XmlWriter.Create(filePath, settings))
            {
                xmlDocument.Save(writer);
            }
        }

        private ChessGame LoadBoardFromXmlFile(string fullFilePath)
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

        private void AddLatestUpdateToXmlFile(XmlDocument xmlDocument, ChessGame game, string filePath)
        {
            List<Update> history = game.History.ToList();

            int historyCount = history.Count;

            history.Reverse(); // Reverse the history to start with the most recent update

            var historyIdList = history.Select(h => h.Id).ToList();

            var xmlCommandNodeIdList = GetLatestUpdateIdFromXmlFile(xmlDocument, filePath, out XmlNodeList xmlCommandNodeList);

            int latestId = xmlCommandNodeIdList.Any() ? xmlCommandNodeIdList.Max() : 0;

            int xmlCommandNodeCount = xmlCommandNodeList.Count;

            if (historyCount > xmlCommandNodeCount)
            {
                foreach (int xmlCommandNodeId in xmlCommandNodeIdList)
                {
                    List<int> problematicIds = new ();

                    if (!historyIdList.Contains(xmlCommandNodeId))
                    {
                        // Problem. IDs in XML file do not match IDs in history
                        problematicIds.Add(xmlCommandNodeId);
                    }

                    if(problematicIds.Any())
                    {
                        string message = "The following IDs are present in the XML file but not in the game history: "
                            + string.Join(", ", problematicIds);
                        Debugger.Break();
                        MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        // throw new InvalidOperationException(message);
                        return;
                    }
                }

                foreach (var update in history)
                {
                    if (update.Id <= latestId)
                        continue; // Skip updates that are already in the file

                    CreateAndAddUpdateCommandXmlElement(xmlDocument, filePath, update, update.Id);
                }
            }
            else if (historyCount < xmlCommandNodeCount)
            {
                List<int> problematicIds = new();
                foreach (int historyId in historyIdList)
                {
                    if (!xmlCommandNodeIdList.Contains(historyId))
                    {
                        problematicIds.Add(historyId);
                    }
                }

                if (problematicIds.Any())
                {
                    string message = "The following IDs are present in the history but not in the xml file: "
                        + string.Join(", ", problematicIds);
                    Debugger.Break();
                    MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    // throw new InvalidOperationException(message);
                    return;
                }

                // Remove Those Ids From Xml file.

                foreach (int xmlCommandNodeId in xmlCommandNodeIdList)
                {
                    if (historyIdList.Contains(xmlCommandNodeId))
                    {
                        continue;
                    }

                    RemoveXmlCommandNodesFromXmlFile(xmlDocument, filePath, xmlCommandNodeId);
                }

            }
            else // historyCount == xmlCommandNodeCount
            {
                // Ensure ids match in both lists
                bool areSetsEqual = new HashSet<int>(historyIdList).SetEquals(xmlCommandNodeIdList);
                bool areListsEqual = historyIdList.OrderBy(x => x).SequenceEqual(xmlCommandNodeIdList.OrderBy(x => x));

                if (areSetsEqual)
                {
                    Debugger.Break();
                    MessageBox.Show("The sets historyIdList and xmlCommandNodeIdList are not equal", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    // throw new InvalidOperationException(message);
                }
                if (areListsEqual)
                {
                    Debugger.Break();
                    MessageBox.Show("The lists historyIdList and xmlCommandNodeIdList are not equal", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    // throw new InvalidOperationException(message);
                }
            }

            // xmlDocument.Save(filePath);
        }

        private void RemoveXmlCommandNodesFromXmlFile(XmlDocument xmlDocument, string filePath, int xmlCommandNodeId)
        {
            // xmlDocument.Load(filePath);

            // XPath to select nodes with the given Id attribute under PieceMoveCommands
            string xpath = $"{XmlConstants.RootElementName}//{XmlConstants.PieceMoveCommandsElementName}/*[@Id='{xmlCommandNodeId}']";
            XmlNodeList nodesToRemove = xmlDocument.SelectNodes(xpath);

            if (nodesToRemove != null)
            {
                foreach (XmlNode node in nodesToRemove)
                {
                    node.ParentNode?.RemoveChild(node);
                }
            }

            UpdateDateModifiedOfXmlFile(xmlDocument);

            // Save changes back to the file
            // xmlDocument.Save(filePath);
        }

        private List<int> GetLatestUpdateIdFromXmlFile(XmlDocument xmlDocument, string filePath, out XmlNodeList xmlCommandNodeList)
        {
            xmlDocument.Load(filePath);
            
            xmlCommandNodeList = xmlDocument.SelectNodes(XmlConstants.RootElementName + "//" + XmlConstants.PieceMoveCommandsElementName + "/*")!;

            List<int> xmlNodeIdList = new ();

            int maxId = 0;

            foreach (XmlNode commandNode in xmlCommandNodeList)
            {
                if (commandNode.Attributes != null && commandNode.Attributes["Id"] != null)
                {
                    if (int.TryParse(commandNode.Attributes["Id"].Value, out int currentId))
                    {
                        xmlNodeIdList.Add(currentId);
                    }
                }
            }
            return xmlNodeIdList;
        }

        private void CreateAndWriteToXmlFile(XmlDocument xmlDocument, ChessGame game, string filePath)
        {
            XmlElement root = xmlDocument.CreateElement(XmlConstants.RootElementName);
            xmlDocument.AppendChild(root);

            XmlElement instructionsElement = xmlDocument.CreateElement(XmlConstants.InstructionsElementName);
            root.AppendChild(instructionsElement);

            // Warning section
            XmlElement warningElement = xmlDocument.CreateElement(XmlConstants.WarningElementName);
            warningElement.AppendChild(xmlDocument.CreateComment("Please Note"));
            warningElement.AppendChild(xmlDocument.CreateComment("This file is not to be manually edited. This is edited and parsed by a computer program."));
            instructionsElement.AppendChild(warningElement);

            // GeneralNotes section
            XmlElement generalNotesElement = xmlDocument.CreateElement(XmlConstants.GeneralNotesElementName);
            generalNotesElement.AppendChild(xmlDocument.CreateComment("Any manual changes may lead to unexpected behavior when the file is processed by the program."));
            generalNotesElement.AppendChild(xmlDocument.CreateComment("This XML file represents a chess game, including the starting positions of the pieces and the sequence of moves made during the game."));
            generalNotesElement.AppendChild(xmlDocument.CreateComment("The 'StartPositions' element contains the initial arrangement of pieces on the board."));
            generalNotesElement.AppendChild(xmlDocument.CreateComment("The 'PieceMoveCommands' element contains a list of commands representing the moves made in the game."));
            generalNotesElement.AppendChild(xmlDocument.CreateComment("Each command is represented as an XML element with attributes and child elements as needed."));
            generalNotesElement.AppendChild(xmlDocument.CreateComment("The order of commands in 'PieceMoveCommands' reflects the sequence of moves made during the game."));
            instructionsElement.AppendChild(generalNotesElement);

            // Metadata section
            XmlElement metadataElement = xmlDocument.CreateElement(XmlConstants.MetadataElementName);

            XmlElement titleElement = xmlDocument.CreateElement(XmlConstants.TitleElementName);
            titleElement.InnerText = "Chess Game XML Representation";
            metadataElement.AppendChild(titleElement);

            XmlElement userElement = xmlDocument.CreateElement(XmlConstants.UserElementName);
            userElement.InnerText = "Player Name";
            metadataElement.AppendChild(userElement);

            XmlElement createdDateElement = xmlDocument.CreateElement(XmlConstants.CreatedDateElementName);
            createdDateElement.InnerText = DateTime.Now.ToString("yyyy-MM-dd-T-HH:mm:ss");
            metadataElement.AppendChild(createdDateElement);

            XmlElement modifiedDateElement = xmlDocument.CreateElement(XmlConstants.ModifiedDateElementName);
            modifiedDateElement.InnerText = DateTime.Now.ToString("yyyy-MM-dd-T-HH:mm:ss");
            metadataElement.AppendChild(modifiedDateElement);

            XmlElement descriptionElement = xmlDocument.CreateElement(XmlConstants.DescriptionElementName);
            descriptionElement.InnerText = "This XML file represents a chess game, including the starting positions of the pieces and the sequence of moves made during the game.";
            metadataElement.AppendChild(descriptionElement);

            XmlElement versionElement = xmlDocument.CreateElement(XmlConstants.VersionElementName);
            versionElement.InnerText = "1.0";
            metadataElement.AppendChild(versionElement);

            instructionsElement.AppendChild(metadataElement);

            // Append instructions to root
            xmlDocument.DocumentElement?.AppendChild(instructionsElement);

            XmlElement pieceMoveCommandsElement = xmlDocument.CreateElement(XmlConstants.PieceMoveCommandsElementName);
            root.AppendChild(pieceMoveCommandsElement);

            WriteStartPositionsToXmlFile(xmlDocument, root, game);

            WriteCommandsToXmlFile(xmlDocument, game, filePath);
        }

        private void WriteStartPositionsToXmlFile(XmlDocument xmlDocument, XmlElement root, ChessGame game)
        {
            XmlElement startPositionsElement = xmlDocument.CreateElement(XmlConstants.StartPositionsElementName);
            root.AppendChild(startPositionsElement);

            List<Update> history = game.History.ToList();
            var board = game.Board;

            if (history.Count != 0)
            {
                var lastUpdate = history.Last();
                board = lastUpdate.Game.Board;
            }

            var whitePiecesOrdered = board.Where(placedPiece => placedPiece.Color == Color.White)
                                          .OrderBy(placedPiece => placedPiece.Piece);
            var blackPiecesOrdered = board.Where(placedPiece => placedPiece.Color == Color.Black)
                                          .OrderBy(placedPiece => placedPiece.Piece);

            XmlElement piecesElement = xmlDocument.CreateElement(XmlConstants.PiecesElementName);

            // Blacks
            XmlElement blacksElement = xmlDocument.CreateElement(XmlConstants.BlacksElementName);
            AppendPiecesToXmlElement(xmlDocument, blacksElement, blackPiecesOrdered);
            piecesElement.AppendChild(blacksElement);

            // Whites
            XmlElement whitesElement = xmlDocument.CreateElement(XmlConstants.WhitesElementName);
            AppendPiecesToXmlElement(xmlDocument, whitesElement, whitePiecesOrdered);
            piecesElement.AppendChild(whitesElement);

            startPositionsElement.AppendChild(piecesElement);

            // Append to root
            xmlDocument.DocumentElement?.AppendChild(startPositionsElement);
        }

        // Helper method to group and append pieces
        private void AppendPiecesToXmlElement(XmlDocument xmlDocument, XmlElement parentElement, IEnumerable<PlacedPiece> placedPieces)
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
                    typeName += "s"; // Pluralize
                }

                XmlElement typeElement = xmlDocument.CreateElement(typeName);

                foreach (var piece in group.PlacedPieces.OrderByDescending(placedPiece => placedPiece.Position.Row))
                {
                    XmlElement positionElement = xmlDocument.CreateElement("Position");
                    positionElement.SetAttribute("Row", (piece.Position.Row + 1).ToString());
                    positionElement.SetAttribute("Column", (piece.Position.Column + 1).ToString());
                    typeElement.AppendChild(positionElement);
                }

                parentElement.AppendChild(typeElement);
            }
        }

        private void WriteCommandsToXmlFile(XmlDocument xmlDocument, ChessGame game, string filePath)
        {
            // xmlDocument.Load(filePath);

            List<Update> history = game.History.ToList();

            history.Reverse(); // Reverse the history to start with the most recent update

            foreach (var update in history)
            {
                CreateAndAddUpdateCommandXmlElement(xmlDocument, filePath, update, update.Id);
            }
            
            // xmlDocument.Save(filePath);
        }

        private void CreateAndAddUpdateCommandXmlElement(XmlDocument xmlDocument, string filePath, Update update, int id)
        {
            // xmlDocument.Load(filePath);

            XmlNode parent = xmlDocument.SelectSingleNode(XmlConstants.RootElementName + "//" + XmlConstants.PieceMoveCommandsElementName);

            var xmlElement = GetCommandXmlElement(xmlDocument, update.Command, id);

            if (parent != null && xmlElement != null)
            {
                // Insert at the beginning
                if (parent.HasChildNodes)
                {
                    parent.InsertBefore(xmlElement, parent.FirstChild);
                }
                else
                {
                    parent.AppendChild(xmlElement);
                }
            }

            UpdateDateModifiedOfXmlFile(xmlDocument);
    
            // xmlDocument.Save(filePath);
        }

        private void UpdateDateModifiedOfXmlFile(XmlDocument xmlDocument)
        {
            XmlNode modifiedDateNode = xmlDocument.SelectSingleNode(XmlConstants.RootElementName + 
                "//" + XmlConstants.InstructionsElementName + "//" + XmlConstants.MetadataElementName + "//" + XmlConstants.ModifiedDateElementName);

            if (modifiedDateNode != null)
            {
                modifiedDateNode.InnerText = DateTime.Now.ToString("yyyy-MM-dd-T-HH:mm:ss");
            }
        }

        private XmlElement? GetCommandXmlElement(XmlDocument xmlDocument, ICommand command, int id = 0)
        {
            XmlElement xmlElement = xmlDocument.CreateElement(command.GetType().Name);

            if (id != 0)
            {
                xmlElement.SetAttribute("Id", id.ToString());
                xmlElement.SetAttribute("CreatedDate", DateTime.Now.ToString("yyyy-MM-dd-T-HH:mm:ss"));
            }

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
                        var firstCommandXmlElement = GetCommandXmlElement(xmlDocument, sequenceCommand.FirstCommand);
                        var secondCommandXmlElement = GetCommandXmlElement(xmlDocument, sequenceCommand.SecondCommand);

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

                        xmlElement.SetAttribute("IsPromotion", removeCommand.IsPromotion.ToString());

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

        private ICommand? ParseCommandElement(XElement command)
        {
            // Use switch expression for command type
            return command.Name.LocalName switch
            {
                "MoveCommand" => ParseMoveCommand(command),
                "SequenceCommand" => ParseSequenceCommand(command),
                "EndTurnCommand" => new EndTurnCommand(false),
                "RemoveCommand" => ParseRemoveCommand(command),
                "SpawnCommand" => ParseSpawnCommand(command),
                "SetLastUpdateCommand" => null,
                _ => null
            };
        }

        // Helper for MoveCommand
        private ICommand ParseMoveCommand(XElement command)
        {
            var pieceElement = command.Elements().FirstOrDefault(e =>
                e.Name.LocalName is "Pawn" or "Knight" or "Bishop" or "Rook" or "Queen" or "King");

            var color = pieceElement?.Attribute(XmlConstants.PieceColorAttributeName)?.Value == "Black"
                ? Color.Black : Color.White;
            ChessPiece piece = CreatePiece(pieceElement?.Name.LocalName, color);

            var sourceElement = command.Element(XmlConstants.SourcePositionAttributeName);
            var targetElement = command.Element(XmlConstants.TargetPositionAttributeName);

            var source = new Position(
                int.Parse(sourceElement.Attribute(XmlConstants.RowAttributeName).Value) - 1,
                int.Parse(sourceElement.Attribute(XmlConstants.ColumnAttributeName).Value) - 1);

            var target = new Position(
                int.Parse(targetElement.Attribute(XmlConstants.RowAttributeName).Value) - 1,
                int.Parse(targetElement.Attribute(XmlConstants.ColumnAttributeName).Value) - 1);

            return new MoveCommand(source, target, piece, isUndo: false);
        }

        // Helper for SequenceCommand
        private ICommand ParseSequenceCommand(XElement command)
        {
            var children = command.Elements().ToList();
            if (children.Count < 2)
                throw new InvalidOperationException("SequenceCommand must have at least two child commands.");

            var firstCommand = ParseCommandElement(children[0]);
            var secondCommand = ParseCommandElement(children[1]);
            return new SequenceCommand(firstCommand, secondCommand);
        }

        // Helper for RemoveCommand
        private ICommand ParseRemoveCommand(XElement command)
        {
            var isPromotion = false;
            if (command.Attribute("Id") != null)
            {
                XAttribute promotionAttribute = command.Attribute("Id")!;
                if (promotionAttribute != null)
                {
                    string isPromotionValue = promotionAttribute.Value; 
                    isPromotion = bool.Parse(isPromotionValue); 
                }
            }

            var pieceElement = command.Elements().FirstOrDefault(e =>
                e.Name.LocalName is "Pawn" or "Knight" or "Bishop" or "Rook" or "Queen" or "King");

            var color = pieceElement?.Attribute(XmlConstants.PieceColorAttributeName)?.Value == "Black"
                ? Color.Black : Color.White;
            
            ChessPiece piece = CreatePiece(pieceElement?.Name.LocalName, color);

            var positionElement = command.Element(XmlConstants.SourcePositionAttributeName);
            var position = new Position(
                int.Parse(positionElement.Attribute(XmlConstants.RowAttributeName).Value) - 1,
                int.Parse(positionElement.Attribute(XmlConstants.ColumnAttributeName).Value) - 1);

            return new RemoveCommand(position, piece, isUndo: false, isPromotion);
        }

        // Helper for SpawnCommand
        private ICommand ParseSpawnCommand(XElement command)
        {
            var pieceElement = command.Elements().FirstOrDefault(e =>
                e.Name.LocalName is "Pawn" or "Knight" or "Bishop" or "Rook" or "Queen" or "King");

            var color = pieceElement?.Attribute(XmlConstants.PieceColorAttributeName)?.Value == "Black"
                ? Color.Black : Color.White;
            ChessPiece piece = CreatePiece(pieceElement?.Name.LocalName, color);
            var positionElement = command.Element(XmlConstants.SourcePositionAttributeName);
            var position = new Position(
                int.Parse(positionElement.Attribute(XmlConstants.RowAttributeName).Value) - 1,
                int.Parse(positionElement.Attribute(XmlConstants.ColumnAttributeName).Value) - 1);

            return new SpawnCommand(position, piece, isUndo: false);
        }

        private ChessPiece CreatePiece(string pieceType, Color color)
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
