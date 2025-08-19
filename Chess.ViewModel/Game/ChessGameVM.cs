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
    using Chess.Model.Rule;
    using Chess.Services;
    using Chess.ViewModel.Command;
    using Chess.ViewModel.StatusAndMode;
    using Chess.ViewModel.Visitor;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Windows;

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
        /// Provides functionality for managing and interacting with XML files.
        /// </summary>
        /// <remarks>This service is used internally to handle operations such as reading, writing,  and
        /// processing XML files. It is not exposed publicly and is intended for internal use only.</remarks>
        private XmlFileService xmlFileService;

        private PlayModeVM playModeVM;

        private RecordModeVM recordModeVM;

        private ReviewModeVM reviewModeVM;

        private StatusDisplayVM statusDisplayVM;

        private ReviewModeHeaderDisplayVM reviewModeHeaderDisplyVM;

        private readonly IWindowService windowService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChessGameVM"/> class.
        /// </summary>
        /// <param name="updateSelector">The disambiguation mechanism if multiple updates are available for a target field.</param>
        public ChessGameVM(Func<IList<Update>, Update> updateSelector, IWindowService windowService)
        {
            this.windowService = windowService ?? throw new ArgumentNullException(nameof(windowService));
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
                        this.Game = e.Game;
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
                        this.Game = e.Game;
                        this.Board.ClearUpdates();
                        e.Command.Accept(this);
                    }
                )
            );

            SelectedAppModeValue = AppMode.Play; // Default mode is Play

            xmlFileService = new();

            CurrentAppModeVM = playModeVM = new PlayModeVM();
            recordModeVM = new RecordModeVM(windowService);
            reviewModeVM = new ReviewModeVM();
            reviewModeHeaderDisplyVM = new ReviewModeHeaderDisplayVM();

            ModeAndPlayerStatusDisplayVM = statusDisplayVM 
                = new StatusDisplayVM(Status.WhiteTurn); ; // Default display is Status Display
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
            }
        }

        private object _currentAppModeVM;
        public object CurrentAppModeVM
        {
            get { return _currentAppModeVM; }
            set
            {
                _currentAppModeVM = value;
                OnPropertyChanged(nameof(CurrentAppModeVM));
            }
        }

        private object _modeAndPlayerStatusDisplayVM;
        public object ModeAndPlayerStatusDisplayVM
        {
            get { return _modeAndPlayerStatusDisplayVM; }
            set
            {
                _modeAndPlayerStatusDisplayVM = value;
                OnPropertyChanged(nameof(ModeAndPlayerStatusDisplayVM));
            }
        }

        private AppMode selectedAppModeValue;
        public AppMode SelectedAppModeValue
        {
            get { return selectedAppModeValue; }
            set
            {
                if (selectedAppModeValue != value)
                {
                    selectedAppModeValue = value;
                    Debug.WriteLine($"Selected App Mode: {selectedAppModeValue}");
                    AppModeChangedHandler();
                    OnPropertyChanged(nameof(SelectedAppModeValue));
                }
            }
        }

        public double BoardBorderThickness => BoardConstants.BoardMarginForId;

        public string FilePath { get; set; }

        public string FolderPath { get; set; }

        /// <summary>
        /// Selects a specific field of the chess board.
        /// </summary>
        /// <param name="row">The row of the field.</param>
        /// <param name="column">The column of the field.</param>
        public void Select(int row, int column)
        {
            if (SelectedAppModeValue == AppMode.Review)
            {
                Debug.WriteLine("Review Mode: Select is not allowed in Review Mode.");
                return;
            }

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
                this.Game.NextUpdate = new Just<Update>(selectedUpdate);
                this.Game = selectedUpdate.Game;
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

        private void AddUpdateXmlToFile()
        {
            if (SelectedAppModeValue != AppMode.Record)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(recordModeVM.FullFilePath))
            {
                Debug.WriteLine("No file available for recording.");
                MessageBox.Show("File Path Does not exist");
                return;
            }

            if (SelectedAppModeValue == AppMode.Record && xmlFileService != null)
            {
                recordModeVM.WriteToXmlFile(this.Game);
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

            UpdateMoveCount();

            statusDisplayVM.UpdateStatus(this.Status);

            AddUpdateXmlToFile();
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
            if(playModeVM != null)
                playModeVM.GameMoveCount = this.Game.History.Count();
        }

        /// <summary>
        /// Handles App Mode Changed Event
        /// </summary>
        private void AppModeChangedHandler()
        {
            switch (SelectedAppModeValue)
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
            CurrentAppModeVM = playModeVM;
            ModeAndPlayerStatusDisplayVM = statusDisplayVM;
        }

        /// <summary>
        /// Handles the change to Record Mode.
        /// </summary>
        private void AppModeChangedToRecordMode()
        {
            if (recordModeVM.RecordingInProgress)
            {
                recordModeVM.ResetRecordingState();
            }
            CurrentAppModeVM = recordModeVM;
            ModeAndPlayerStatusDisplayVM = statusDisplayVM;
        }

        /// <summary>
        /// Handles the change to Review Mode.
        /// </summary>
        private void AppModeChangedToReviewMode()
        {
            if (CurrentAppModeVM != null
                && CurrentAppModeVM is RecordModeVM)
            {
                if (recordModeVM.RecordingInProgress)
                {
                    reviewModeVM.FullFilePath = recordModeVM.FullFilePath;
                    reviewModeVM.IsReviewFileInRecording = true;
                }
            }
            CurrentAppModeVM = reviewModeVM;
            ModeAndPlayerStatusDisplayVM = reviewModeHeaderDisplyVM;
        }
    }
}