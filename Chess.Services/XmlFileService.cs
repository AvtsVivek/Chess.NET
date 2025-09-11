using Chess.Model.Command;
using Chess.Model.Data;
using Chess.Model.Game;
using Chess.Model.Piece;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Xml;
using System.Xml.Linq;

namespace Chess.Services
{
    public class XmlFileService
    {
        /// <summary>
        /// Generates a unique file name for a chess game, based on the current date and time.
        /// </summary>
        public static string GetFileName()
        {
            DateTime now = DateTime.Now;
            string fileName = $"ChessGame-{now:yyyy-MM-dd-HH-mm-ss}.xml";
            return fileName;
        }

        XmlWriterSettings settings;
        /// <summary>
        /// Initializes a new instance of the <see cref="XmlFileService"/> class.
        /// </summary>
        /// <remarks>This constructor creates an instance of the <see cref="XmlFileService"/> class, 
        /// which can be used to perform operations related to XML file processing.  Additional initialization logic can
        /// be added as needed.</remarks>
        public XmlFileService()
        {
            settings = new();
            settings.Indent = true;
            settings.Encoding = Encoding.UTF8;
            settings.IndentChars = ("\t");
            settings.OmitXmlDeclaration = false;
        }

        public ChessGame GetPieceMoveCommandsFromXmlFile(string fullFilePath)
        {
            XDocument doc = XDocument.Load(fullFilePath);
            ChessGame chessGame = LoadBoardFromXmlFile(doc);

            // Read Title
            string title = doc
                .Element(XmlConstants.RootElementName)?
                .Element(XmlConstants.InstructionsElementName)?
                .Element(XmlConstants.MetadataElementName)?
                .Element(XmlConstants.TitleElementName)?
                .Value ?? string.Empty;

            ChessGame.TitleNotesConcurrentDictionary[0] = (title, null);

            XElement pieceMoveCommandElement = doc.Descendants(XmlConstants.PieceMoveCommandsElementName).First();
            List<XElement> commandElements = pieceMoveCommandElement.Elements(nameof(SequenceCommand)).ToList();
            commandElements.Reverse();

            var parsedCommandsWithIds = new List<(ICommand, int)>();

            foreach (XElement commandElement in commandElements)
            {
                ICommand command = ParseCommandElement(commandElement)!;
                int updateId = 0;
                bool success = false;
                if (commandElement.Name.LocalName == "SequenceCommand")
                {
                    var idAttr = commandElement.Attribute("Id");
                    if (idAttr != null)
                        success = int.TryParse(idAttr.Value, out updateId);
                }
                if (success)
                {
                    parsedCommandsWithIds.Add((command, updateId));
                    // Read the content of the Notes element (direct child of commandElement)
                    var notesElement = commandElement.Element("Notes");
                    string notesText = notesElement != null ? notesElement.Value : string.Empty;
                    if (!string.IsNullOrWhiteSpace(notesText))
                    {
                        // Store in the dictionary. But the update is not yet created.
                        // So we store null for now, and update it later.
                        // ChessGame.NewTitleNotesDictionary[updateId] = (notesText, null);

                        // ToDo. Need to check the following.
                        // Earlier, the following is what it was. But now, this is replaced by the above line.
                        if (ChessGame.TitleNotesConcurrentDictionary.ContainsKey(updateId))
                        {
                            ChessGame.TitleNotesConcurrentDictionary[updateId] = (notesText, null);
                        }
                        else
                        {
                            if (ChessGame.TitleNotesConcurrentDictionary.TryAdd(updateId, (notesText, null)))
                            {
                                Debug.WriteLine($"Added notes for Id {updateId} to NewTitleNotesDictionary.");
                            }
                            else
                            {
                                Debug.WriteLine($"Failed to add notes for Id {updateId} to NewTitleNotesDictionary.");
                            }
                        }
                    }
                }
                else
                {
                    Debug.WriteLine("Failed to parse Id attribute or Id attribute is missing. Defaulting to 0.");
                    parsedCommandsWithIds.Add((command, 0));
                }
            }

            ChessGame updatedGame = chessGame;
            foreach (var parsedCommandWithId in parsedCommandsWithIds)
            {
                var parsedCommand = parsedCommandWithId.Item1;
                var parsedUpdate = new Update(updatedGame, parsedCommand, "XmlFileRead", parsedCommandWithId.Item2);
                var setLastUpdateCommand = new SetLastUpdateCommand(parsedUpdate);
                ICommand command = new SequenceCommand(parsedCommand, setLastUpdateCommand);
                var updates = command.Execute(updatedGame).Map(g => new Update(g, command, "XmlFileRead")).Yield();
                if (!updates.Any()) continue;
                Update? update = updates.First();
                updatedGame!.NextUpdate = new Just<Update>(update);
                updatedGame = update.Game;

                ChessGame.TitleNotesConcurrentDictionary[parsedCommandWithId.Item2] =
                    (ChessGame.TitleNotesConcurrentDictionary.ContainsKey(parsedCommandWithId.Item2) ?
                    ChessGame.TitleNotesConcurrentDictionary[parsedCommandWithId.Item2].titleNotes : string.Empty, parsedUpdate);
            }

            return updatedGame;
        } // Done

        public void WriteGameToXmlFile(ChessGame game, string filePath)
        {
            if (game == null)
                throw new ArgumentNullException(nameof(game));

            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be empty or whitespace.", nameof(filePath));

            if (game.History == null || !game.History.Any())
                // No history to write
                // throw new InvalidOperationException("The game has no history to write.");
                Debug.WriteLine("The game has no history to write.");


            XDocument doc;
            if (!File.Exists(filePath))
            {
                doc = CreateAndWriteToXmlFile(game);
            }
            else
            {
                doc = XDocument.Load(filePath);
                AddLatestUpdateToXmlFile(doc, game);
            }
            
            SaveDocument(doc, filePath);
        }  // Done

        private void SaveDocument(XDocument doc, string filePath)
        {
            using (var writer = XmlWriter.Create(filePath, settings))
            {
                doc.Save(writer);
            }
        }

        public void SaveTitleNotesText(int titleNotesId, string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                Debug.WriteLine("File path is null or whitespace. Cannot save title/notes.");
                Debug.WriteLine($"Or file does not exist: {filePath}. Cannot update title/notes.");
                return;
            }

            XDocument doc = XDocument.Load(filePath);
            var textToUpdate = ChessGame.TitleNotesConcurrentDictionary[titleNotesId].titleNotes;

            if (titleNotesId == 0)
            {
                var titleElement = doc
                    .Element(XmlConstants.RootElementName)?
                    .Element(XmlConstants.InstructionsElementName)?
                    .Element(XmlConstants.MetadataElementName)?
                    .Element(XmlConstants.TitleElementName);

                if (titleElement != null)
                {
                    titleElement.Value = textToUpdate;
                    UpdateDateModifiedOfXmlFile(doc);
                    SaveDocument(doc, filePath);
                }
            }
            else
            {
                SaveSequenceCommandNotes(doc, titleNotesId, textToUpdate, filePath);
            }
        } // Done

        private void SaveSequenceCommandNotes(XDocument doc, int titleNotesId, string textToUpdate, string filePath)
        {
            var sequenceCommand = doc
                .Descendants(XmlConstants.PieceMoveCommandsElementName)
                .Elements( nameof(SequenceCommand))
                .FirstOrDefault(e => (int.TryParse(e.Attribute("Id")?.Value, out int id) && id == titleNotesId));
            if (sequenceCommand == null) return;

            var notesElement = sequenceCommand.Element(XmlConstants.CommandNotesElementName);
            if (notesElement == null)
            {
                notesElement = new XElement(XmlConstants.CommandNotesElementName, textToUpdate);
                sequenceCommand.Add(notesElement);
            }
            else
            {
                notesElement.Value = textToUpdate;
            }
            UpdateDateModifiedOfXmlFile(doc);
            SaveDocument(doc, filePath);
        } // Done

        private ChessGame LoadBoardFromXmlFile(XDocument doc)
        {
            var piecesNode = doc.Descendants(XmlConstants.PiecesElementName).First();
            var allPlacedPieces = new List<PlacedPiece>();
            var pieceTypes = new[] { "Pawns", "Knights", "Bishops", "Rooks", "Queens", "King" };

            foreach (var pieceType in pieceTypes)
            {
                var whitePieceList = GetPieces(piecesNode, pieceType, Color.White);
                var blackPieceList = GetPieces(piecesNode, pieceType, Color.Black);
                allPlacedPieces.AddRange(whitePieceList);
                allPlacedPieces.AddRange(blackPieceList);
            }

            var emptyDictionary = ImmutableSortedDictionary.Create<Position, ChessPiece>(PositionComparer.DefaultComparer);
            var dict = allPlacedPieces.Aggregate(emptyDictionary, (s, p) => s.Add(p.Position, p.Piece));
            var board = new Board(dict);
            var whitePlayer = new Player(Color.White);
            var blackPlayer = new Player(Color.Black);
            return new ChessGame(board, whitePlayer, blackPlayer);
        } // Done

        private XDocument CreateAndWriteToXmlFile(ChessGame game)
        {
            var root = new XElement(XmlConstants.RootElementName);
            var instructions = new XElement(XmlConstants.InstructionsElementName);

            // Warning section
            var warning = new XElement(XmlConstants.WarningElementName,
                new XComment("Please Note"),
                new XComment("This file is not to be manually edited. This is edited and parsed by a computer program."));
            instructions.Add(warning);

            // GeneralNotes section
            var generalNotes = new XElement(XmlConstants.GeneralNotesElementName,
                new XComment("Any manual changes may lead to unexpected behavior when the file is processed by the program."),
                new XComment("This XML file represents a chess game, including the starting positions of the pieces and the sequence of moves made during the game."),
                new XComment("The 'StartPositions' element contains the initial arrangement of pieces on the board."),
                new XComment("The 'PieceMoveCommands' element contains a list of commands representing the moves made in the game."),
                new XComment("Each command is represented as an XML element with attributes and child elements as needed."),
                new XComment("The order of commands in 'PieceMoveCommands' reflects the sequence of moves made during the game."));
            instructions.Add(generalNotes);

            // Metadata section
            var metadata = new XElement(XmlConstants.MetadataElementName);
            var title = new XElement(XmlConstants.TitleElementName,
                ChessGame.TitleNotesConcurrentDictionary.ContainsKey(0) ? ChessGame.TitleNotesConcurrentDictionary[0].titleNotes : string.Empty);
            metadata.Add(title);
            metadata.Add(new XElement(XmlConstants.UserElementName, "Player Name"));
            metadata.Add(new XElement(XmlConstants.CreatedDateElementName, DateTime.Now.ToString("yyyy-MM-dd-T-HH:mm:ss")));
            metadata.Add(new XElement(XmlConstants.ModifiedDateElementName, DateTime.Now.ToString("yyyy-MM-dd-T-HH:mm:ss")));
            metadata.Add(new XElement(XmlConstants.DescriptionElementName, "This XML file represents a chess game, including the starting positions of the pieces and the sequence of moves made during the game."));
            metadata.Add(new XElement(XmlConstants.VersionElementName, "1.0"));
            instructions.Add(metadata);

            root.Add(instructions);

            var pieceMoveCommands = new XElement(XmlConstants.PieceMoveCommandsElementName);
            root.Add(pieceMoveCommands);

            WriteStartPositionsToXmlFile(root, game);
            WriteCommandsToXmlFile(pieceMoveCommands, game);

            return new XDocument(root);
        } // Done

        private void WriteStartPositionsToXmlFile(XElement root, ChessGame game)
        {
            var startPositions = new XElement(XmlConstants.StartPositionsElementName);
            var history = game.History.ToList();
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

            var piecesElement = new XElement(XmlConstants.PiecesElementName);

            // Blacks
            var blacksElement = new XElement(XmlConstants.BlacksElementName);
            AppendPiecesToXElement(blacksElement, blackPiecesOrdered);
            piecesElement.Add(blacksElement);

            // Whites
            var whitesElement = new XElement(XmlConstants.WhitesElementName);
            AppendPiecesToXElement(whitesElement, whitePiecesOrdered);
            piecesElement.Add(whitesElement);

            startPositions.Add(piecesElement);
            root.Add(startPositions);
        } // Done

        private void AppendPiecesToXElement(XElement parentElement, IEnumerable<PlacedPiece> placedPieces)
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
                    typeName += "s";
                }

                var typeElement = new XElement(typeName);
                foreach (var piece in group.PlacedPieces.OrderByDescending(placedPiece => placedPiece.Position.Row))
                {
                    var positionElement = new XElement("Position",
                        new XAttribute("Row", (piece.Position.Row + 1).ToString()),
                        new XAttribute("Column", (piece.Position.Column + 1).ToString()));
                    typeElement.Add(positionElement);
                }
                parentElement.Add(typeElement);
            }
        } // Done

        private void WriteCommandsToXmlFile(XElement pieceMoveCommandsElement, ChessGame game)
        {
            List<Update> history = game.History.ToList();

            var orderedHistory = history.OrderByDescending(u => u.Id).ToList();

            foreach (var update in orderedHistory)
            {
                var xmlElement = GetCommandXElement(update.Command, update.Id);
                var textToUpdate = ChessGame.TitleNotesConcurrentDictionary.ContainsKey(update.Id)
                    ? ChessGame.TitleNotesConcurrentDictionary[update.Id].titleNotes
                    : string.Empty;
                xmlElement.Add(new XElement(XmlConstants.CommandNotesElementName, textToUpdate));
                pieceMoveCommandsElement.Add(xmlElement); // Add to the end
            }
        } // Done

        private void AddLatestUpdateToXmlFile(XDocument doc, ChessGame game)
        {
            var pieceMoveCommandsElement = doc.Descendants(XmlConstants.PieceMoveCommandsElementName).First();
            // ToDo the following line needs to be removed.
            var existingIds = pieceMoveCommandsElement.Elements().Select(e => int.TryParse(e.Attribute("Id")?.Value, out int id) ? id : 0).ToList();
            var history = game.History.ToList();
            int historyCount = history.Count;
            history.Reverse(); // Reverse the history to start with the most recent update
            var historyIdList = history.Select(h => h.Id).ToList();

            var xmlCommandNodeIdList = GetLatestUpdateIdFromXmlFile(doc, out List<XElement> xmlCommandNodeList);

            int latestId = xmlCommandNodeIdList.Any() ? xmlCommandNodeIdList.Max() : 0;

            int xmlCommandNodeCount = xmlCommandNodeList.Count;

            if (historyCount > xmlCommandNodeCount)
            {
                foreach (int xmlCommandNodeId in xmlCommandNodeIdList)
                {
                    List<int> problematicIds = new();

                    if (!historyIdList.Contains(xmlCommandNodeId))
                    {
                        // Problem. IDs in XML file do not match IDs in history
                        problematicIds.Add(xmlCommandNodeId);
                    }

                    if (problematicIds.Any())
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

                    CreateAndAddUpdateCommandXmlElement(doc, update, update.Id);
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
                    RemoveXmlCommandNodesFromXmlFile(doc, xmlCommandNodeId);
                }

            }
            else // historyCount == xmlCommandNodeCount
            {
                // Ensure ids match in both lists. If they don't, it's an error.
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
        } // Done

        private void RemoveXmlCommandNodesFromXmlFile(XDocument doc, int xmlCommandNodeId)
        {
            //// xmlDocument.Load(filePath);

            //// XPath to select nodes with the given Id attribute under PieceMoveCommands
            //string xpath = $"{XmlConstants.RootElementName}//{XmlConstants.PieceMoveCommandsElementName}/*[@Id='{xmlCommandNodeId}']";
            //XmlNodeList nodesToRemove = doc.SelectNodes(xpath);

            //if (nodesToRemove != null)
            //{
            //    foreach (XmlNode node in nodesToRemove)
            //    {
            //        node.ParentNode?.RemoveChild(node);
            //    }
            //}


            // Assume doc is your XDocument and xmlCommandNodeId is the int Id to remove
            var pieceMoveCommandsElement = doc.Descendants(XmlConstants.PieceMoveCommandsElementName).FirstOrDefault();
            if (pieceMoveCommandsElement != null)
            {
                var elementToRemove = pieceMoveCommandsElement
                    .Elements()
                    .FirstOrDefault(e =>
                        int.TryParse(e.Attribute("Id")?.Value, out int id) && id == xmlCommandNodeId);

                if (elementToRemove != null)
                {
                    elementToRemove.Remove();
                }
            }

            UpdateDateModifiedOfXmlFile(doc);
        } // Done

        private void CreateAndAddUpdateCommandXmlElement(XDocument doc, Update update, int id)
        {
            var pieceMoveCommandsElement = doc.Descendants(XmlConstants.PieceMoveCommandsElementName).First();
            // XmlNode parent = xmlDocument.SelectSingleNode(XmlConstants.RootElementName + "//" + XmlConstants.PieceMoveCommandsElementName);

            // var xmlElement = GetCommandXmlElement(doc, update.Command, id);
            var xmlElement = GetCommandXElement(update.Command, update.Id);

            var textToUpdate = string.Empty;

            if (ChessGame.TitleNotesConcurrentDictionary.ContainsKey(id))
            {
                textToUpdate = ChessGame.TitleNotesConcurrentDictionary[id].titleNotes;
            }

            var notesElement = new XElement(XmlConstants.CommandNotesElementName, textToUpdate);
            xmlElement.Add(notesElement); // Adds as the last child

            // XmlElement notesElement = xmlDocument.CreateElement(XmlConstants.CommandNotesElementName);

            //var textToUpdate = string.Empty;

            //if (ChessGame.TitleNotesDictionary.ContainsKey(id))
            //{
            //    textToUpdate = ChessGame.TitleNotesDictionary[id].titleNotes;
            //}

            // notesElement.InnerText = textToUpdate;
            // xmlElement.AppendChild(notesElement);

            if (pieceMoveCommandsElement != null && xmlElement != null)
            {
                // Insert at the beginning
                if (pieceMoveCommandsElement.HasElements)
                {
                    pieceMoveCommandsElement.AddFirst(xmlElement);
                }
                else
                {
                    pieceMoveCommandsElement.Add(xmlElement);
                }
            }
            UpdateDateModifiedOfXmlFile(doc);
        } // Done

        private List<int> GetLatestUpdateIdFromXmlFile(XDocument doc, out List<XElement> sequenceCommandElements)
        {
            // xmlDocument.Load(filePath);

            // xmlCommandNodeList = xmlDocument.SelectNodes(XmlConstants.RootElementName + "//" + XmlConstants.PieceMoveCommandsElementName + "/*")!;

            //var pieceMoveCommandElement = doc.Element(XmlConstants.RootElementName)?
            //    .Element(XmlConstants.PieceMoveCommandsElementName);

            XElement pieceMoveCommandElement = doc.Descendants(XmlConstants.PieceMoveCommandsElementName).First();
            sequenceCommandElements = pieceMoveCommandElement.Elements(nameof(SequenceCommand)).ToList();

            /*

                             var titleElement = doc
                    .Element(XmlConstants.RootElementName)?
                    .Element(XmlConstants.PieceMoveCommandsElementName)?
                    .Element(XmlConstants.MetadataElementName)?
                    .Element(XmlConstants.TitleElementName);
             */



            // List<int> xmlNodeIdList = new();

            // int maxId = 0;

            //foreach (XElement element in sequenceCommandElements)
            //{
            //    List<XAttribute> attributeList = element.Attributes().ToList();

            //    if (attributeList != null && attributeList["Id"] != null)
            //    {
            //        if (int.TryParse(element.Attributes["Id"].Value, out int currentId))
            //        {
            //            xmlNodeIdList.Add(currentId);
            //        }
            //    }
            //}

            var xmlNodeIdList = sequenceCommandElements
                .Select(e => e.Attribute("Id")?.Value)
                .Where(val => !string.IsNullOrEmpty(val))
                .Select(val => int.TryParse(val, out int id) ? id : (int?)null)
                .Where(id => id.HasValue)
                .Select(id => id.Value)
                .ToList();

            return xmlNodeIdList;
        } // Done

        private void UpdateDateModifiedOfXmlFile(XDocument doc)
        {
            var modifiedDateElement = doc
                .Element(XmlConstants.RootElementName)?
                .Element(XmlConstants.InstructionsElementName)?
                .Element(XmlConstants.MetadataElementName)?
                .Element(XmlConstants.ModifiedDateElementName);
            if (modifiedDateElement != null)
            {
                modifiedDateElement.Value = DateTime.Now.ToString("yyyy-MM-dd-T-HH:mm:ss");
            }
        } // Done

        private XElement GetCommandXElement(ICommand command, int id = 0)
        {
            var xmlElement = new XElement(command.GetType().Name);
            if (id != 0)
            {
                xmlElement.SetAttributeValue("Id", id.ToString());
                xmlElement.SetAttributeValue("CreatedDate", DateTime.Now.ToString("yyyy-MM-dd-T-HH:mm:ss"));
            }

            switch (command)
            {
                case MoveCommand moveCommand:
                    {
                        var pieceElement = new XElement(moveCommand.Piece.GetType().Name,
                            new XAttribute(XmlConstants.PieceColorAttributeName, moveCommand.Piece.Color.ToString()));
                        var sourceElement = new XElement(XmlConstants.SourcePositionAttributeName,
                            new XAttribute(XmlConstants.RowAttributeName, (moveCommand.Source.Row + 1).ToString()),
                            new XAttribute(XmlConstants.ColumnAttributeName, (moveCommand.Source.Column + 1).ToString()));
                        var targetElement = new XElement(XmlConstants.TargetPositionAttributeName,
                            new XAttribute(XmlConstants.RowAttributeName, (moveCommand.Target.Row + 1).ToString()),
                            new XAttribute(XmlConstants.ColumnAttributeName, (moveCommand.Target.Column + 1).ToString()));
                        xmlElement.Add(pieceElement, sourceElement, targetElement);
                        return xmlElement;
                    }
                case SequenceCommand sequenceCommand:
                    {
                        var firstCommandXmlElement = GetCommandXElement(sequenceCommand.FirstCommand);
                        var secondCommandXmlElement = GetCommandXElement(sequenceCommand.SecondCommand);
                        xmlElement.Add(firstCommandXmlElement, secondCommandXmlElement);
                        return xmlElement;
                    }
                case EndTurnCommand:
                    return xmlElement;
                case RemoveCommand removeCommand:
                    {
                        var pieceElement = new XElement(removeCommand.Piece.GetType().Name,
                            new XAttribute(XmlConstants.PieceColorAttributeName, removeCommand.Piece.Color.ToString()));
                        var positionElement = new XElement(XmlConstants.SourcePositionAttributeName,
                            new XAttribute(XmlConstants.RowAttributeName, (removeCommand.Position.Row + 1).ToString()),
                            new XAttribute(XmlConstants.ColumnAttributeName, (removeCommand.Position.Column + 1).ToString()));
                        xmlElement.Add(pieceElement, positionElement);
                        xmlElement.SetAttributeValue("IsPromotion", removeCommand.IsPromotion.ToString());
                        return xmlElement;
                    }
                case SpawnCommand spawnCommand:
                    {
                        var pieceElement = new XElement(spawnCommand.Piece.GetType().Name,
                            new XAttribute(XmlConstants.PieceColorAttributeName, spawnCommand.Piece.Color.ToString()));
                        var positionElement = new XElement(XmlConstants.SourcePositionAttributeName,
                            new XAttribute(XmlConstants.RowAttributeName, (spawnCommand.Position.Row + 1).ToString()),
                            new XAttribute(XmlConstants.ColumnAttributeName, (spawnCommand.Position.Column + 1).ToString()));
                        xmlElement.Add(pieceElement, positionElement);
                        return xmlElement;
                    }
                case SetLastUpdateCommand:
                    return xmlElement;
                default:
                    throw new NotSupportedException($"Unsupported command type: {command.GetType().Name}");
            }
        } // Done

        private ICommand? ParseCommandElement(XElement command)
        {
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
        } // Done

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
        } // Done

        private ICommand ParseSequenceCommand(XElement command)
        {
            var children = command.Elements().ToList();
            if (children.Count < 2)
                throw new InvalidOperationException("SequenceCommand must have at least two child commands.");

            var firstCommand = ParseCommandElement(children[0]);
            var secondCommand = ParseCommandElement(children[1]);
            return new SequenceCommand(firstCommand, secondCommand);
        } // Done

        private ICommand ParseRemoveCommand(XElement command)
        {
            var isPromotion = false;
            var promotionAttr = command.Attribute("IsPromotion");
            if (promotionAttr != null)
            {
                bool.TryParse(promotionAttr.Value, out isPromotion);
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
        } // Done

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
        } // Done

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
        } // Done

        private List<PlacedPiece> GetPieces(XElement piecesNode, string pieceType, Color color)
        {
            var placedPieces = new List<PlacedPiece>();
            var colorPieceNodes = piecesNode.Descendants(color.ToString() + "s");

            Func<Color, ChessPiece> pieceFactory = pieceType switch
            {
                "Pawns" => c => new Pawn(c),
                "Knights" => c => new Knight(c),
                "Bishops" => c => new Bishop(c),
                "Rooks" => c => new Rook(c),
                "Queens" => c => new Queen(c),
                "King" => c => new King(c),
                _ => c => new Pawn(c)
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
        } // Done
    }
}