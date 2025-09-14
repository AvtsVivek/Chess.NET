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
    using Chess.ViewModel.Messages;
    using Chess.ViewModel.StatusAndMode;
    using Chess.ViewModel.Visitor;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Messaging;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;

    /// <summary>
    /// Represents the view model of a chess game.
    /// </summary>
    public partial class ChessGameVM : ObservableObject, ICommandVisitor
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

        private readonly GenericCommand buildCustomBoardCommand;

        /// <summary>
        /// Represents the current game state.
        /// </summary>
        private ChessGame game;

        /// <summary>
        /// Represents the currently presented chess board.
        /// </summary>
        [ObservableProperty]
        private BoardVM board;

        [ObservableProperty]
        private bool isBoardInverted;

        private StatusDisplayVM statusDisplayVM;

        private ReviewModeHeaderDisplayVM reviewModeHeaderDisplayVM;

        private BuildCustomBoardVM buildCustomBoardVM;

        private StatusModeListViewVM statusModeListViewVM;

        private readonly IWindowService windowService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChessGameVM"/> class.
        /// </summary>
        /// <param name="updateSelector">The disambiguation mechanism if multiple updates are available for a target field.</param>
        public ChessGameVM(Func<IList<Update>, Update> updateSelector, IWindowService windowService)
        {
            this.buildCustomBoardVM = new();

            this.buildCustomBoardCommand = new GenericCommand
            (
                () =>
                {
                    if (selectedAppModeValue == AppMode.Review)
                    {
                        return false;
                    }

                    if (this.Game.History.Count() == 0)
                    {
                        return true;
                    }
                    return false;
                },
                () =>
                {
                    if (selectedAppModeValue == AppMode.Review)
                    {
                        return; // Do nothing in review mode.
                    }

                    if (this.Game.History.Count() > 0)
                    {
                        return;
                    }

                    if (CustomBoardStatusModeVM is BuildCustomBoardVM)
                    {
                        CustomBoardStatusModeVM = statusModeListViewVM;
                    }
                    else
                    {
                        CustomBoardStatusModeVM = buildCustomBoardVM;
                    }
                }
            );

            this.windowService = windowService ?? throw new ArgumentNullException(nameof(windowService));

            this.rulebook = new StandardRulebook();

            this.ModeAndPlayerStatusDisplayVM = statusDisplayVM = new(Status.WhiteTurn);

            this.updateSelector = updateSelector;

            this.negator = new CommandNegator();

            this.undoCommand = new GenericCommand
            (
                () => this.Game.LastUpdate.HasValue,
                () => this.Game.LastUpdate.Do
                (
                    e =>
                    {
                        statusModeListViewVM.SaveTitleNotesText();
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
                        statusModeListViewVM.SaveTitleNotesText();
                        this.Game = e.Game;
                        this.Board.ClearUpdates();
                        e.Command.Accept(this);
                    }
                )
            );

            this.reviewModeHeaderDisplayVM = new(this.undoCommand, this.redoCommand, Status.WhiteTurn);

            Func<(ChessGame Game, BoardVM Board, Action StartNewGame)> 
                getCurrentGameBoardDelegateAndStartNewGame = () => (this.Game, this.Board, StartNewGame);

            this.statusModeListViewVM = new(this.rulebook, windowService, reviewModeHeaderDisplayVM, 
                getCurrentGameBoardDelegateAndStartNewGame);

            this.OnPropertyChanged(nameof(this.Status));

            this.Board.ClearChessMoveSequence();

            RefreshAfterEndTurn();

            CustomBoardStatusModeVM = statusModeListViewVM;

            BoardInversionToggleCommand = new GenericCommand(
                () => true,
                ToggleBoardInvertedField
            );

            DoMessengerRegistration();

            HeaderNotificationMessage = new();
        }

        private void ToggleBoardInvertedField()
        {
            IsBoardInverted = !IsBoardInverted;
        }

        public GenericCommand BoardInversionToggleCommand { get; }

        /// <summary>
        /// Flag to indicate, the record mode is not yet ready for recording.
        /// </summary>
        private bool recordModeNotReady = true;

        [ObservableProperty]
        private Visibility customBoardButtonVisibility = Visibility.Visible;

        private AppMode selectedAppModeValue;

        private void DoMessengerRegistration()
        {
            WeakReferenceMessenger.Default.Register<MessageFromStatusModeListViewVMToChessGameVM>(this, (r, m) =>
            {
                var previousAppMode = selectedAppModeValue;

                selectedAppModeValue = m.AppModeValue;

                AppModeChangedHandler(previousAppMode);

                if (m.AppModeValue == AppMode.Review)
                {
                    CustomBoardButtonVisibility = Visibility.Collapsed;
                }
                else
                {
                    CustomBoardButtonVisibility = Visibility.Visible;
                }
            });

            WeakReferenceMessenger.Default.Register<MessageFromAutoReviewModeVMToChessGameVM>(this, async (r, m) =>
            {
                if (m.Code == "AutoReviewStoppedSuccessfully")
                {
                    if (selectedAppModeValue == AppMode.Record
                    || selectedAppModeValue == AppMode.Play)
                    {
                        await Task.Run(() =>
                        {
                            while (this.redoCommand.CanExecute(null))
                            {
                                recordModeNotReady = true; // Still not ready for recording until all redos are done.
                                this.redoCommand.Execute(null);
                            }
                            Application.Current?.Dispatcher.Invoke(SendMessageToManualReviewVM);
                            recordModeNotReady = false; // Now ready for recording.
                        });
                    }
                }
            });

            WeakReferenceMessenger.Default.Register<MessageFromRecordReviewModeVMToChessGameVM>(this, async (r, m) =>
            {
                if (!string.IsNullOrWhiteSpace(m.IsHeaderNotificationMessage))
                {
                    HeaderNotificationMessage.MessageText = m.IsHeaderNotificationMessage;
                    HeaderNotificationMessage.MessageFontSize = 14; // Smaller font size for longer messages.
                    return;
                }

                // Review Game. Load the game in the board.
                var game = m.Value;

                if (game == null)
                {
                    Debug.WriteLine("No game available for review.");
                    MessageBox.Show("No game available for review.");
                    Debugger.Break();
                    return;
                }

                StartNewGame();

                if (game.History.Any())
                {
                    this.Game = game.History.Last().Game; // First, oldest, farthest 
                }
                else
                {
                    // If no history, use the current game state
                    // This can happen when there are no moves in the game.
                    this.Game = game;
                }

                GenericCommand commandToExecute = null;

                if (ChessAppSettings.Default.ReviewFromLast)
                {
                    commandToExecute = this.redoCommand;
                }
                else
                {
                    commandToExecute = this.undoCommand;
                }

                await Task.Run(() =>
                {
                    while (commandToExecute.CanExecute(null))
                    {
                        commandToExecute.Execute(null);
                    }
                    Application.Current?.Dispatcher.Invoke(SendMessageToManualReviewVM);
                });

                SetReviewFileLoadComplete();
            });
        }

        private void SetReviewFileLoadComplete(bool loadComplete = true)
        {
            if (reviewModeHeaderDisplayVM != null)
            {
                AutoReviewModeVM autoReviewModeVM = reviewModeHeaderDisplayVM.CurrentReviewModeVM as AutoReviewModeVM;
                if (autoReviewModeVM != null)
                {
                    autoReviewModeVM.ReviewFileLoadCompleted = loadComplete;
                }
            }
        }

        private void SendMessageToManualReviewVM()
        {
            MessageToManualReviewVM message = new();
            WeakReferenceMessenger.Default.Send(message);
        }

        /// <summary>
        /// Gets the current status of the chess game.
        /// </summary>
        /// <value>The current status of the presented chess game.</value>
        /// Todo. Only one status should be enough. Either on ChessGameVM or on StatusModeListViewVM.
        public Status Status => this.rulebook.GetStatus(this.Game);

        /// <summary>
        /// Gets the command that starts a new chess game.
        /// </summary>
        /// <value>The command that starts a new chess game.</value>
        public GenericCommand NewGameCommand
        {
            get
            {
                return new GenericCommand(CanExecuteNewGameCommand, ExecuteNewGameCommand);
            }
        }

        public GenericCommand BuildCustomBoardCommand => this.buildCustomBoardCommand;

        private void ExecuteNewGameCommand()
        {
            if (statusModeListViewVM.ShouldExecuteNewGameCommand())
            {
                StartNewGame();
            }
        }

        private void StartNewGame()
        {
            this.Game = this.rulebook.CreateGame();
            this.Board = new BoardVM(this.Game.Board);
            this.OnPropertyChanged(nameof(this.Status));
            this.Board.ClearChessMoveSequence();

            RefreshAfterEndTurn();
        }

        private bool CanExecuteNewGameCommand()
        {
            if (selectedAppModeValue == AppMode.Play)
            {
                return true;
            }

            if (selectedAppModeValue == AppMode.Record)
            {
                return true;
            }

            return false;
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

        [ObservableProperty]
        private object customBoardStatusModeVM;

        [ObservableProperty]
        private object modeAndPlayerStatusDisplayVM;

        [ObservableProperty]
        private HeaderNotificationVM headerNotificationMessage;

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
            if (selectedAppModeValue == AppMode.Review)
            {
                Debug.WriteLine("Review Mode: Select is not allowed in Review Mode.");
                HeaderNotificationMessage.MessageText = "Chess Moves cannot be done in Review Mode";
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
                statusModeListViewVM.SaveTitleNotesText();
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
            else
            {
                // This happens when user clicks on an empty field which is not a valid target for any piece.
                // This else is added by me to understand invalid moves.
                // MessageBox.Show("Invalid Move");
                // this.Game.NextUpdate = new Nothing<Update>();
                // this.Game = null; // Game can never be null.
            }
        }

        /// <summary>
        /// Executes a <see cref="SequenceCommand"/> in order to change the presented game state.
        /// </summary>
        /// <param name="command">The <see cref="SequenceCommand"/> to be executed.</param>
        public void Visit(SequenceCommand command)
        {
            this.Board.Execute(command);
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
            RefreshAfterEndTurn();
            statusModeListViewVM.RefreshAfterEndTurn();
            statusModeListViewVM.AddUpdateXmlToFile();
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
        /// Temp. Will be removed.
        /// Just to update move count.
        /// </summary>
        private void RefreshAfterEndTurn()
        {
            this.BuildCustomBoardCommand.FireCanExecuteChanged();

            // Need to check this.
            this.OnPropertyChanged(nameof(this.Status));

            reviewModeHeaderDisplayVM?.UpdateStatus(this.Status);

            statusDisplayVM.UpdateStatus(this.Status);
        }

        /// <summary>
        /// Handles App Mode Changed Event
        /// </summary>
        private void AppModeChangedHandler(AppMode previousAppMode)
        {
            SetReviewFileLoadComplete(loadComplete: false);

            this.NewGameCommand.FireCanExecuteChanged();

            this.Board.ClearUpdates();

            this.recordModeNotReady = true; // Not ready for recording until the mode change is fully handled.
           
            switch (selectedAppModeValue)
            {
                case AppMode.Play:
                    AppModeChangedToPlayMode(previousAppMode);
                    break;
                case AppMode.Record:
                    AppModeChangedToRecordMode(previousAppMode);
                    break;
                case AppMode.Review:
                    AppModeChangedToReviewMode(previousAppMode);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Handles the change to Play Mode.
        /// </summary>
        private void AppModeChangedToPlayMode(AppMode previousAppMode)
        {
            ModeAndPlayerStatusDisplayVM = statusDisplayVM;
        }

        /// <summary>
        /// Handles the change to Record Mode.
        /// </summary>
        private void AppModeChangedToRecordMode(AppMode previousAppMode)
        {
            ModeAndPlayerStatusDisplayVM = statusDisplayVM;
            recordModeNotReady = false; // Now ready for recording.
        }

        /// <summary>
        /// Handles the change to Review Mode.
        /// </summary>
        private void AppModeChangedToReviewMode(AppMode previousAppMode)
        {
            ModeAndPlayerStatusDisplayVM = reviewModeHeaderDisplayVM;
        }
    }
}