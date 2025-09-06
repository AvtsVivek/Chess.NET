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
    using System.IO;
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

        /// <summary>
        /// Provides functionality for managing and interacting with XML files.
        /// </summary>
        /// <remarks>This service is used internally to handle operations such as reading, writing,  and
        /// processing XML files. It is not exposed publicly and is intended for internal use only.</remarks>
        private XmlFileService xmlFileService;

        private PlayModeVM playModeVM;

        private RecordReviewModeVM recordReviewModeVM;

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

            StartNewGame();

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

            xmlFileService = new();
            CurrentAppModeVM = playModeVM = new();
            recordReviewModeVM = new(windowService);
            SelectedAppModeValue = AppMode.Play; // Default mode is Play
            reviewModeHeaderDisplyVM = new(this.undoCommand, this.redoCommand);

            BoardInversionToggleCommand = new GenericCommand(
                () => true,
                ToggleBoardInvertedField
            );

            ModeAndPlayerStatusDisplayVM = statusDisplayVM = new(Status.WhiteTurn);

            DoMessengerRegistration();

            HeaderNotificationMessage = new();

            StartSaveTitleNotesTextLoop();
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

        private void DoMessengerRegistration()
        {

            WeakReferenceMessenger.Default.Register<MessageFromAutoReviewModeVMToChessGameVM>(this, async (r, m) =>
            {
                if (m.Code == "AutoReviewStoppedSuccessfully")
                {
                    if (SelectedAppModeValue == AppMode.Record
                    || SelectedAppModeValue == AppMode.Play)
                    {
                        await Task.Run(() =>
                        {
                            while (this.redoCommand.CanExecute(null))
                            {
                                recordModeNotReady = true; // Still not ready for recording until all redos are done.
                                this.redoCommand.Execute(null);
                            }
                            // SendMessageToManualReviewVM must be called on the UI thread
                            Application.Current.Dispatcher.Invoke(SendMessageToManualReviewVM);
                            recordModeNotReady = false; // Now ready for recording.
                        });
                    }
                }
            });

            WeakReferenceMessenger.Default.Register<MessageToChessGameVM>(this, (r, m) =>
            {
                StartNewGame();

                var game = m.Value;

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

                Task.Run(() =>
                {
                    while (commandToExecute.CanExecute(null))
                    {
                        commandToExecute.Execute(null);
                    }
                    // SendMessageToManualReviewVM must be called on the UI thread
                    Application.Current.Dispatcher.Invoke(SendMessageToManualReviewVM);
                });
            });
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
        public Status Status => this.rulebook.GetStatus(this.Game);

        /// <summary>
        /// Gets the command that starts a new chess game.
        /// </summary>
        /// <value>The command that starts a new chess game.</value>
        public GenericCommand NewCommand
        {
            get
            {
                return new GenericCommand(CanExecuteNewCommand, ExecuteNewCommand);
            }
        }

        private void ExecuteNewCommand()
        {
            if (SelectedAppModeValue == AppMode.Review)
            {
                return;
            }
            if (SelectedAppModeValue == AppMode.Record)
            {
                if (recordReviewModeVM.RecordingInProgress)
                {
                    var result = windowService.ShowMessageBox(
                       "Recording is in progress." + Environment.NewLine +
                       "Do you want to stop the recording and start a new game?" + Environment.NewLine +
                       "Click Yes to Stop this recording and start recording a new game." + Environment.NewLine +
                       "Click No to continue recording the current game.," + Environment.NewLine,
                       "Recording in progress", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.No)
                    {
                        return;
                    }
                    if (result == MessageBoxResult.Yes)
                    {
                        recordReviewModeVM.SetFullFilePath();
                    }
                }
                else
                {
                    recordReviewModeVM.SetFullFilePath();
                }
            }
            StartNewGame();
        }

        private void StartNewGame()
        {
            this.Game = this.rulebook.CreateGame();
            this.Board = new BoardVM(this.Game.Board);
            this.OnPropertyChanged(nameof(this.Status));
            this.Board.ClearChessMoveSequence();
            UpdateMoveCount();
        }

        private bool CanExecuteNewCommand()
        {
            if (SelectedAppModeValue == AppMode.Play)
            {
                return true;
            }

            if (SelectedAppModeValue == AppMode.Record)
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
        private object currentAppModeVM;

        [ObservableProperty]
        private object _modeAndPlayerStatusDisplayVM;

        [ObservableProperty]
        private HeaderNotificationVM _headerNotificationMessage;

        private AppMode selectedAppModeValue;
        public AppMode SelectedAppModeValue
        {
            get => selectedAppModeValue;
            set
            {
                var previousAppMode = selectedAppModeValue;
                SetProperty(ref selectedAppModeValue, value);
                AppModeChangedHandler(previousAppMode);
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
        /// Temp. Will be removed.
        /// Just to update move count.
        /// </summary>
        private void UpdateMoveCount()
        {
            var moveCount = this.Game.History.Count();
            if (playModeVM != null)
                playModeVM.GameMoveCount = moveCount;

            if (moveCount == 0)
            {
                PlaceHolderTextForTitleNotesTextBox = " Start typing to Set Title for the game here:";
            }
            else
            {
                PlaceHolderTextForTitleNotesTextBox = $" Start typing to take notes for move {moveCount} here:";
            }
        }

        [ObservableProperty]
        private string placeHolderTextForTitleNotesTextBox;


        private string titleNotesText = string.Empty;

        public string TitleNotesText
        {
            get
            {
                return titleNotesText;
            }
            set
            {
                SetProperty(ref titleNotesText, value);
            }
        }

        /// <summary>
        /// Handles App Mode Changed Event
        /// </summary>
        private void AppModeChangedHandler(AppMode previousAppMode)
        {
            if ((SelectedAppModeValue != AppMode.Play)
                && (previousSavedTitleNotes != TitleNotesText))
            {
                SaveTitleNotesText();
            }

            this.NewCommand.FireCanExecuteChanged();
            this.Board.ClearUpdates();

            this.recordModeNotReady = true; // Not ready for recording until the mode change is fully handled.

            switch (SelectedAppModeValue)
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

            recordReviewModeVM.CurrentAppMode = SelectedAppModeValue;
        }

        /// <summary>
        /// Handles the change to Play Mode.
        /// </summary>
        private void AppModeChangedToPlayMode(AppMode previousAppMode)
        {
            CurrentAppModeVM = playModeVM;
            ModeAndPlayerStatusDisplayVM = statusDisplayVM;
            IsChessMovesNotesRowCollapsed = true;
        }

        /// <summary>
        /// Handles the change to Record Mode.
        /// </summary>
        private async void AppModeChangedToRecordMode(AppMode previousAppMode)
        {
            if (recordReviewModeVM.RecordingInProgress)
            {
                await reviewModeHeaderDisplyVM.StopAutoReviewLoop();

                recordReviewModeVM.ResetRecordingState();
            }

            if (string.IsNullOrWhiteSpace(recordReviewModeVM.FullFilePath))
            {
                recordReviewModeVM.SetFullFilePath();
            }

            CurrentAppModeVM = recordReviewModeVM;
            ModeAndPlayerStatusDisplayVM = statusDisplayVM;
            recordModeNotReady = false; // Now ready for recording.
            IsChessMovesNotesRowCollapsed = false;
        }

        /// <summary>
        /// Handles the change to Review Mode.
        /// </summary>
        private void AppModeChangedToReviewMode(AppMode previousAppMode)
        {
            if (previousAppMode == AppMode.Play && this.Game.History.Count() != 0)
            {
                var result = windowService.ShowMessageBox(
                   "A play is in progress." + Environment.NewLine +
                   "Do you want to stop the play and switch to review?" + Environment.NewLine +
                   "Switching to review will reset the board." + Environment.NewLine +
                   "Click OK to Stop the play and reset the board, and proceed to review." + Environment.NewLine +
                   "Click Cancel to continue the play.," + Environment.NewLine,
                   "Play in progress", MessageBoxButton.OKCancel, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Cancel)
                {
                    SelectedAppModeValue = AppMode.Play; // Revert back to Play Mode
                    return;
                }

                if (result == MessageBoxResult.OK)
                {
                    StartNewGame();
                }
            }

            CurrentAppModeVM = recordReviewModeVM;
            ModeAndPlayerStatusDisplayVM = reviewModeHeaderDisplyVM;

            if (!File.Exists(recordReviewModeVM.FullFilePath))
            {
                StartNewGame();
            }
            SetReviewMode();
            IsChessMovesNotesRowCollapsed = false;
        }


        [ObservableProperty]
        private bool isChessMovesNotesRowCollapsed;

        private void AddUpdateXmlToFile()
        {
            if (SelectedAppModeValue != AppMode.Record)
            {
                return;
            }

            if (reviewModeHeaderDisplyVM.IsAutoReviewRunning)
            {
                return;
            }

            if (recordModeNotReady)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(recordReviewModeVM.FullFilePath))
            {
                Debug.WriteLine("No file available for recording.");
                MessageBox.Show("File Path Does not exist");
                return;
            }

            if (SelectedAppModeValue == AppMode.Record
                && xmlFileService != null)
            {
                recordReviewModeVM.WriteToXmlFile(this.Game);
            }
        }

        private string previousSavedTitleNotes = string.Empty;

        // Add a private lock object to the class
        private readonly object titleNotesLock = new();

        private void SaveTitleNotesText()
        {
            // Replace the code block with a thread-safe version using lock
            lock (titleNotesLock)
            {
                var moveCount = this.Game.History.Count();
                if (!ChessGame.TitleNotesDictionary.ContainsKey(moveCount))
                {
                    ChessGame.TitleNotesDictionary.Add(moveCount, TitleNotesText);
                }
                else
                {
                    ChessGame.TitleNotesDictionary[moveCount] = TitleNotesText;
                }
                previousSavedTitleNotes = TitleNotesText;
                recordReviewModeVM.SaveTitleNotesText(moveCount);
            }
        }

        private async void StartSaveTitleNotesTextLoop()
        {
            int waitTimeInSeconds = 2;

            await Task.Run(async () =>
            {
                while (true)
                {
                    if ((SelectedAppModeValue != AppMode.Play) ||
                        (previousSavedTitleNotes != TitleNotesText))
                    {
                        SaveTitleNotesText();
                    }

                    await Task.Delay(TimeSpan.FromSeconds(waitTimeInSeconds));
                }
            });
        }


        private void SetReviewMode()
        {
            var manualAutoReview = ChessAppSettings.Default.ManualAutoReview;
            if (!string.IsNullOrWhiteSpace(ChessAppSettings.Default.ManualAutoReview))
            {
                if (manualAutoReview.Equals("Manual", StringComparison.OrdinalIgnoreCase))
                {
                    reviewModeHeaderDisplyVM.SelectedReviewModeValue = ReviewMode.Manual;
                }
                else if (manualAutoReview.Equals("Auto", StringComparison.OrdinalIgnoreCase))
                {
                    reviewModeHeaderDisplyVM.SelectedReviewModeValue = ReviewMode.Auto;
                }
            }
            else
            {
                reviewModeHeaderDisplyVM.SelectedReviewModeValue = ReviewMode.Manual;
            }
        }
    }
}