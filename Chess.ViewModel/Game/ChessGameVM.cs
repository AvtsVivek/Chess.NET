//-----------------------------------------------------------------------
// <copyright file="ChessGameVM.cs">
//     Copyright (c) Michael Szvetits. All rights reserved.
// </copyright>
// <author>Michael Szvetits</author>
//-----------------------------------------------------------------------
namespace Chess.ViewModel.Game
{
    using Chess.Model.Command;
    using Chess.Model.Data;
    using Chess.Model.Game;
    using Chess.Model.Piece;
    using Chess.Model.Rule;
    using Chess.ViewModel.Command;
    using Chess.ViewModel.Piece;
    using Chess.ViewModel.Visitor;
    using Microsoft.Win32;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Windows;
    using System.Xml;

    /// <summary>
    /// Represents the view model of a chess game.
    /// </summary>
    public class ChessGameVM : ICommandVisitor, INotifyPropertyChanged
    {
        /// <summary>
        /// Represents the rulebook for the game.
        /// </summary>
        private readonly IRulebook rulebook;

        /// <summary>
        /// Represents the disambiguation mechanism if multiple updates are available for a target field.
        /// </summary>
        private readonly Func<IList<Update>, Update> updateSelector;

        /// <summary>
        /// Represents an object who can negate/invert a given command.
        /// </summary>
        private readonly CommandNegator negator;

        /// <summary>
        /// Represents the undo command, which reverts to a previous game state.
        /// </summary>
        private readonly GenericCommand undoCommand;

        /// <summary>
        /// Represents the redo command, which reverts the previous undo.
        /// </summary>
        private readonly GenericCommand redoCommand;

        /// <summary>
        /// Represents the current game state.
        /// </summary>
        private ChessGame game;

        /// <summary>
        /// Represents the currently presented chess board.
        /// </summary>
        private BoardVM board;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChessGameVM"/> class.
        /// </summary>
        /// <param name="updateSelector">The disambiguation mechanism if multiple updates are available for a target field.</param>
        public ChessGameVM(Func<IList<Update>, Update> updateSelector)
        {
            this.rulebook = new StandardRulebook();
            this.Game = this.rulebook.CreateGame();
            this.board = new BoardVM(this.Game.Board);
            this.updateSelector = updateSelector;
            this.negator = new CommandNegator();

            this.undoCommand = new GenericCommand
            (
                () => this.Game.LastUpdate.HasValue,
                () => this.Game.LastUpdate.Do
                (
                    e =>
                    {
                        // PrintCurrentGameStateForDebuggingPurposes("Undo");
                        this.Game = e.Game;
                        // PrintCurrentGameStateForDebuggingPurposes("Undo");
                        this.Board.ClearUpdates();
                        e.Command.Accept(this.negator).Accept(this);
                    }
                )
            );

            this.redoCommand = new GenericCommand
            (
                () => this.Game.NextUpdate.HasValue,
                () => this.Game.NextUpdate.Do
                (
                    e =>
                    {
                        // PrintCurrentGameStateForDebuggingPurposes("Redo");
                        this.Game = e.Game;
                        // PrintCurrentGameStateForDebuggingPurposes("Redo");
                        this.Board.ClearUpdates();
                        e.Command.Accept(this);
                    }
                )
            );

            SelectedAppModeValue = AppMode.Play; // Default mode is Play
        }

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets the currently presented state of the chess board.
        /// </summary>
        /// <value>The current state of the chess board.</value>
        public BoardVM Board
        {
            get
            {
                return this.board;
            }

            private set
            {
                if (this.board != value)
                {
                    this.board = value ?? throw new ArgumentNullException(nameof(this.Board));
                    this.OnPropertyChanged(nameof(this.Board));
                }
            }
        }

        /// <summary>
        /// Gets the current status of the chess game.
        /// </summary>
        /// <value>The current status of the presented chess game.</value>
        public Status Status => this.rulebook.GetStatus(this.Game);

        /// <summary>
        /// Gets the command that starts a new chess game.
        /// </summary>
        /// <value>The command that starts a new chess game.</value>
        public GenericCommand NewCommand
        {
            get
            {
                return new GenericCommand
                (
                    () => true,
                    () =>
                    {
                        this.Game = this.rulebook.CreateGame();
                        this.Board = new BoardVM(this.Game.Board);
                        this.OnPropertyChanged(nameof(this.Status));
                        this.Board.ClearChessMoveSequence();
                    }
                );
            }
        }

        /// <summary>
        /// Gets the command that reverts the last action of the presented chess game.
        /// </summary>
        /// <value>The command that reverts the last action of the presented chess game.</value>
        public GenericCommand UndoCommand => this.undoCommand;

        /// <summary>
        /// Gets the command that reverts the last undo
        /// </summary>
        /// <value>The command that reverts the last undo.</value>
        public GenericCommand RedoCommand => this.redoCommand;

        /// <summary>
        /// Gets or sets the current chess game state.
        /// </summary>
        private ChessGame Game
        {
            get
            {
                return this.game;
            }

            set
            {
                if (this.game != value)
                {
                    this.game = value ?? throw new ArgumentNullException(nameof(this.Game));
                    this.UndoCommand?.FireCanExecuteChanged();
                    this.RedoCommand?.FireCanExecuteChanged();
                }
                UpdateMoveCount();
            }
        }

        /// <summary>
        /// Temp to be removed. GameCount.
        /// </summary>
        public int GameMoveCount { get; set; }


        private AppMode _selectedAppModeValue;
        public AppMode SelectedAppModeValue
        {
            get { return _selectedAppModeValue; }
            set
            {
                if (_selectedAppModeValue != value)
                {
                    _selectedAppModeValue = value;
                    Debug.WriteLine($"Selected App Mode: {_selectedAppModeValue}");
                    AppModeChangedHandler();
                    OnPropertyChanged(nameof(SelectedAppModeValue));

                }
            }
        }


        public string FilePath { get; set; }


        /// <summary>
        /// Selects a specific field of the chess board.
        /// </summary>
        /// <param name="row">The row of the field.</param>
        /// <param name="column">The column of the field.</param>
        public void Select(int row, int column)
        {
            UpdateMoveCount();

            var position = new Position(row, column);
            var field = this.Board.GetField(position);

            if (this.Board.Source == field)
            {
                this.Board.ClearUpdates();
                return;
            }

            var updates = this.Board.GetUpdates(field);
            var updateCount = updates.Count;
            var selectedUpdate = this.updateSelector(updates);
            this.Board.ClearUpdates();

            if (selectedUpdate != null)
            {
                // PrintCurrentGameStateForDebuggingPurposes("Regular");
                this.Game.NextUpdate = new Just<Update>(selectedUpdate);
                this.Game = selectedUpdate.Game;
                // PrintCurrentGameStateForDebuggingPurposes("Regular");
                selectedUpdate.Command.Accept(this);
            }
            else if (this.game.Board.IsOccupied(position, this.game.ActivePlayer.Color))
            {
                this.Game.NextUpdate = new Nothing<Update>();
                var newUpdates = this.rulebook.GetUpdates(this.Game, position);
                this.Board.SetSource(position);
                this.Board.SetTargets(newUpdates);
            }
        }

        /// <summary>
        /// Executes a <see cref="SequenceCommand"/> in order to change the presented game state.
        /// </summary>
        /// <param name="command">The <see cref="SequenceCommand"/> to be executed.</param>
        public void Visit(SequenceCommand command)
        {
            command.FirstCommand.Accept(this);
            command.SecondCommand.Accept(this);
        }

        /// <summary>
        /// Executes a <see cref="EndTurnCommand"/> in order to change the presented game state.
        /// </summary>
        /// <param name="command">The <see cref="EndTurnCommand"/> to be executed.</param>
        /// <remarks>This method is executed once all of the commands are done executing the end of a player's turn.
        /// For example, castling involves both the king and one rook, and here two move commands are executed in sequence.
        /// Then the <see cref="EndTurnCommand"/> is executed to indicate the end of the player's turn.
        /// In a more common scenario, the <see cref="EndTurnCommand"/> is executed after a capture occurred, or a pawn was promoted.
        /// When a capture occurs, a move command and a remove command is executed. 
        /// Then the <see cref="EndTurnCommand"/> is executed to indicate the end of the player's turn.
        /// So the end command can be used to indicate the end of a player's turn in a chess game.
        /// And so this can be used to update the game state, such as switching the active player, this.Status
        /// Also this can be used to count the number of turns in a game.
        /// </remarks>
        public void Visit(EndTurnCommand command)
        {
            this.Board.Execute(command);
            this.OnPropertyChanged(nameof(this.Status));
        }

        /// <summary>
        /// Executes a <see cref="MoveCommand"/> in order to change the presented game state.
        /// </summary>
        /// <param name="command">The <see cref="MoveCommand"/> to be executed.</param>
        public void Visit(MoveCommand command)
        {
            this.Board.Execute(command);
        }

        /// <summary>
        /// Executes a <see cref="RemoveCommand"/> in order to change the presented game state.
        /// </summary>
        /// <param name="command">The <see cref="RemoveCommand"/> to be executed.</param>
        public void Visit(RemoveCommand command)
        {
            this.Board.Execute(command);
        }

        /// <summary>
        /// Executes a <see cref="SetLastUpdateCommand"/> in order to change the presented game state.
        /// </summary>
        /// <param name="command">The <see cref="SetLastUpdateCommand"/> to be executed.</param>
        public void Visit(SetLastUpdateCommand command)
        {
            // Not used at the moment, can be used to display the game history in the GUI.
            // Not clear how to use this to display the game history in the GUI.
            this.Board.Execute(command);
        }

        /// <summary>
        /// Executes a <see cref="SpawnCommand"/> in order to change the presented game state.
        /// </summary>
        /// <param name="command">The <see cref="SpawnCommand"/> to be executed.</param>
        public void Visit(SpawnCommand command)
        {
            this.Board.Execute(command);
        }

        /// <summary>
        /// Fires the <see cref="PropertyChanged"/> event.
        /// </summary>
        /// <param name="propertyName">The name of the property that has been changed.</param>
        protected void OnPropertyChanged(string propertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Temp. Will be removed.
        /// Just to update move count.
        /// </summary>
        private void UpdateMoveCount()
        {
            GameMoveCount = this.Game.History.Count();

            OnPropertyChanged(nameof(GameMoveCount));
        }

        /// <summary>
        /// Handles App Mode Changed Event
        /// </summary>
        private void AppModeChangedHandler()
        {
            switch (_selectedAppModeValue)
            {
                case AppMode.Play:
                    AppModeChangedToPlayMode();
                    break;
                case AppMode.Record:
                    AppModeChangedToRecordMode();
                    break;
                case AppMode.Review:
                    AppModeChangedToReviewMode();
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Handles the change to Play Mode.
        /// </summary>
        private void AppModeChangedToPlayMode()
        {

        }

        private string GetFileName()
        {
            DateTime now = DateTime.Now;
            string fileName = $"ChessGame-{now:yyyy-MM-dd-HH-mm-ss}.xml";
            return fileName;
        }

        private string GetXmlFolderPath()
        {
            var initialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (!string.IsNullOrWhiteSpace(ChessAppSettings.Default.XmlFolderPath)
                && Directory.Exists(ChessAppSettings.Default.XmlFolderPath)
                )
            {
                initialDirectory = ChessAppSettings.Default.XmlFolderPath;
            }

            var folderDialog = new OpenFolderDialog
            {
                Title = "Select Folder",
                InitialDirectory = initialDirectory
            };


            if (folderDialog.ShowDialog() == true)
            {
                var folderName = folderDialog.FolderName;

                if (!ArePathsSame(ChessAppSettings.Default.XmlFolderPath, folderName))
                {
                    ChessAppSettings.Default.XmlFolderPath = folderName;
                    ChessAppSettings.Default.Save();
                }
            }
            else
            {
                Debug.WriteLine("No folder selected. Cannot record game history.");
                return null;
            }

            return folderDialog.FolderName;
        }

        /// <summary>
        /// Handles the change to Record Mode.
        /// </summary>
        private void AppModeChangedToRecordMode()
        {
            // In Record Mode, we can record the game state and the moves made by the players.
            // This can be used to create a game history, which can be used to review the game later.
            // First ensure we have a valid file path to save the game history.

            if (string.IsNullOrWhiteSpace(FilePath))
            {
                Debug.WriteLine("File path is not set. Cannot record game history.");
                var folderPath = GetXmlFolderPath();

                if (string.IsNullOrWhiteSpace(folderPath))
                {
                    Debug.WriteLine("Folder path is still not set. Cannot record game history.");
                    return;
                }

                var fileName = GetFileName();

                FilePath = Path.Combine(folderPath, fileName);

            }

            WriteXmlFile(FilePath);
        }

        /// <summary>
        /// Handles the change to Review Mode.
        /// </summary>
        private void AppModeChangedToReviewMode()
        {

        }

        private void WriteXmlFile(string filePath)
        {
            Debug.WriteLine($"Writing game state to XML file: {filePath}");
            XmlWriterSettings settings = new();
            settings.Indent = true;
            settings.IndentChars = ("\t");
            settings.OmitXmlDeclaration = true;

            XmlDocument doc = new();

            using (XmlWriter writer = XmlWriter.Create(filePath, settings))
            {
                writer.WriteStartElement("ChessMoves");

                WriteStartPositionsToXmlFile(writer);

                writer.WriteEndElement();

                doc.Save(writer);
            }
        }

        /// <summary>
        /// Writes the starting positions of the game pieces to an XML file using the specified <see cref="XmlWriter"/>.
        /// </summary>
        /// <remarks>This method generates an XML structure representing the starting positions of the
        /// game pieces for both black and white players. If the game history is empty, no piece information is written.
        /// The pieces are grouped by color and ordered by type.</remarks>
        /// <param name="writer">The <see cref="XmlWriter"/> used to write the XML content. Cannot be <see langword="null"/>.</param>
        private void WriteStartPositionsToXmlFile(XmlWriter writer)
        {
            writer.WriteStartElement("Start");

            List<Update> history = this.Game.History.ToList();

            var board = this.Game.Board;

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

            writer.WriteStartElement("Pieces");

            writer.WriteStartElement("Black");

            WritePieces(blackPiecesOrdered, writer);

            writer.WriteEndElement(); // End of Black

            writer.WriteStartElement("White");

            WritePieces(whitePiecesOrdered, writer);

            writer.WriteEndElement(); // End of White

            writer.WriteEndElement(); // End of Pieces

            writer.WriteEndElement();
        }

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
                writer.WriteStartElement(group.PlacedPieces.First().Piece.GetType().Name);

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

        private bool ArePathsSame(string path1, string path2)
        {
            return NormalizePath(path1) == NormalizePath(path2);
        }

        private string NormalizePath(string path)
        {
            return Path.GetFullPath(new Uri(path).LocalPath)
                       .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                       .ToUpperInvariant();
        }
    }
}