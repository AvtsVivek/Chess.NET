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
    using Chess.ViewModel.Command;
    using Chess.ViewModel.Visitor;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;

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
                    OnPropertyChanged(nameof(SelectedAppModeValue));
                }
            }
        }


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
                // Not sure about the following. Need to check if this is correct.
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
        /// Prints the current game state to the debug console for debugging purposes.
        /// </summary>
        /// <param name="moveType"></param>
        private void PrintCurrentGameStateForDebuggingPurposes(string moveType)
        {
            var lastUpdate = this.Game.LastUpdate.Yield().FirstOrDefault();
            if (lastUpdate != null)
            {
                var debugString =
                    $"Move Type: {moveType}, " +
                    $"MoveCount: {this.GameMoveCount}, " +
                    $"LastUpdateId: {lastUpdate.Game.LastUpdateId}, " +
                    $"LastUpdateGameId: {lastUpdate.Game.GameId}, " +
                    $"Current Update: {lastUpdate.UpdateId}, " +
                    $"Current GameId: {this.Game.GameId}, " +
                    $"NextUpdateId: {lastUpdate.Game.NextUpdateId}, ";
                Debug.WriteLine(debugString);
            }
        }
    }
}