using Chess.Model.Command;
using Chess.Model.Game;
using Chess.Model.Rule;
using Chess.Services;
using Chess.ViewModel.Command;
using Chess.ViewModel.Game;
using Chess.ViewModel.Messages;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Chess.ViewModel.StatusAndMode
{
    public partial class StatusModeListViewVM : ObservableObject
    {
        /// <summary>
        /// Represents the rulebook for the game.
        /// </summary>
        private readonly IRulebook rulebook;

        [ObservableProperty]
        private object currentAppModeVM;

        private PlayModeVM playModeVM;

        private RecordReviewModeVM recordReviewModeVM;

        private ReviewModeHeaderDisplayVM reviewModeHeaderDisplayVM;

        [ObservableProperty]
        private BoardVM board;

        [ObservableProperty]
        private bool titleNotesTextBoxIsEnabled = true;

        [ObservableProperty]
        private bool titleNotesTextBoxFocused;

        private readonly IWindowService windowService;

        private readonly GenericCommand titleNotesTextBoxBorderMouseDownCommand;
        private readonly GenericCommand titleNotesLostFocusCommand;
        //private readonly GenericCommand buildCustomBoardCommand;

        /// <summary>
        /// Flag to indicate, the record mode is not yet ready for recording.
        /// </summary>
        private bool recordModeNotReady = true;

        private readonly Func<(ChessGame Game, BoardVM Board, Action StartNewGame)> getCurrentGameBoardDelegateAndStartNewGame;

        public StatusModeListViewVM(IRulebook rulebook, IWindowService windowService, ReviewModeHeaderDisplayVM reviewModeHeaderDisplayVM, 
            Func<(ChessGame Game, BoardVM Board, Action StartNewGame)> getCurrentGameBoardDelegateAndStartNewGame)
        {
            this.titleNotesTextBoxBorderMouseDownCommand = new GenericCommand(() => true, OnTitleNotesTextBoxBorderMouseDown);
            
            this.getCurrentGameBoardDelegateAndStartNewGame = getCurrentGameBoardDelegateAndStartNewGame ?? throw new ArgumentNullException(nameof(getCurrentGameBoardDelegateAndStartNewGame));

            this.titleNotesLostFocusCommand = new GenericCommand(() => true, OnTitleNotesLostFocus);

            this.rulebook = rulebook ?? throw new ArgumentNullException(nameof(rulebook));
            this.getCurrentGameBoardDelegateAndStartNewGame().StartNewGame(); // Start a new game on initialization.
            
            TitleNotesText = string.Empty;

            this.reviewModeHeaderDisplayVM = reviewModeHeaderDisplayVM ?? throw new ArgumentNullException(nameof(reviewModeHeaderDisplayVM));

            this.windowService = windowService ?? throw new ArgumentNullException(nameof(windowService));
            this.playModeVM = new();
            this.recordReviewModeVM = new(windowService);

            this.SelectedAppModeValue = AppMode.Play; // Default mode is Play

            AppModeChangedHandler(AppMode.Play);

            DoMessengerRegistration();

            StartSaveTitleNotesTextLoop();
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
                if (string.IsNullOrWhiteSpace(value))
                {
                    SetPlaceHolderTextForTitleNotesTextBox();
                }
                else
                {

                }

                SetProperty(ref titleNotesText, value);
            }
        }

        public GenericCommand TitleNotesTextBoxBorderMouseDownCommand => this.titleNotesTextBoxBorderMouseDownCommand;
        public GenericCommand TitleNotesLostFocusCommand => this.titleNotesLostFocusCommand;

        /// <summary>
        /// Gets the current status of the chess game.
        /// </summary>
        /// <value>The current status of the presented chess game.</value>
        /// Todo. Only one status should be enough. Either on ChessGameVM or here.
        public Status Status => this.rulebook.GetStatus(this.getCurrentGameBoardDelegateAndStartNewGame().Game);
        
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

        private void OnTitleNotesTextBoxBorderMouseDown()
        {
            PlaceHolderTextForTitleNotesTextBox = string.Empty;

            if (selectedAppModeValue == AppMode.Review &&
                reviewModeHeaderDisplayVM.SelectedReviewModeValue == ReviewMode.Auto)
            {
                // If in Auto Review mode, switch to Manual mode when user tries to edit title notes.
                reviewModeHeaderDisplayVM.SelectedReviewModeValue = ReviewMode.Manual;
                TitleNotesTextBoxFocused = true; // Set focus to title notes text box after switching to manual mode.
            }
        }

        private void OnTitleNotesLostFocus()
        {
            SetPlaceHolderTextForTitleNotesTextBox();
        }

        private void DoMessengerRegistration()
        {
            WeakReferenceMessenger.Default.Register<MessageFromReviewModeHeaderDisplayVMToChessGameVM>(this, (r, m) =>
            {
                if (m.ReviewModeValue == ReviewMode.Auto)
                {
                    StopSaveTitleNotesTextLoop();
                    // Set focus to the main board grid to avoid accidental edits to title notes while in auto review mode.
                    TitleNotesTextBoxIsEnabled = false; // Disable title notes text box in auto review mode.
                }
                else if (m.ReviewModeValue == ReviewMode.Manual)
                {
                    StartSaveTitleNotesTextLoop();
                    TitleNotesTextBoxIsEnabled = true; // Enable title notes text box in manual review mode.
                }
            });
        }

        private void SetPlaceHolderTextForTitleNotesTextBox()
        {
            var moveCount = this.getCurrentGameBoardDelegateAndStartNewGame().Game.History.Count();

            if (moveCount == 0)
            {
                PlaceHolderTextForTitleNotesTextBox = "Click here to set Title for the game";
            }
            else
            {
                PlaceHolderTextForTitleNotesTextBox = $"Click here to take notes for move {moveCount}";
            }
        }

        public bool ShouldExecuteNewGameCommand()
        {
            if (selectedAppModeValue == AppMode.Review)
            {
                return false;
            }
            if (selectedAppModeValue == AppMode.Record)
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
                        return false;
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
            return true;
        }

        private async void StartSaveTitleNotesTextLoop()
        {
            if (selectedAppModeValue == AppMode.Play)
            {
                return; // Only start the loop in Record or review mode.
            }

            // If in Review Mode and Auto Review is running, do not start the loop.
            // Start only in Manual review mode.
            if (selectedAppModeValue == AppMode.Review &&
                reviewModeHeaderDisplayVM.SelectedReviewModeValue == ReviewMode.Auto)
            {
                return; // Do not start the loop in Auto Review mode
            }

            if (saveNotesCts != null && !saveNotesCts.IsCancellationRequested)
            {
                // Already running, do nothing
                return;
            }

            saveNotesCts?.Cancel(); // Cancel any previous loop
            saveNotesCts = new CancellationTokenSource();
            var token = saveNotesCts.Token;
            int waitTimeInMilliSeconds = 5000;

            await Task.Run(async () =>
            {
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        if ((selectedAppModeValue != AppMode.Play) ||
                            (previousSavedTitleNotes != TitleNotesText))
                        {
                            Debug.WriteLine("Auto Saving Title Notes...");
                            SaveTitleNotesText();
                        }
                        await Task.Delay(TimeSpan.FromMilliseconds(waitTimeInMilliSeconds), token);
                    }
                }
                catch (TaskCanceledException)
                {
                    Debug.WriteLine("Task was canceled.");
                }
            }, token);
        }

        private CancellationTokenSource? saveNotesCts;

        private void StopSaveTitleNotesTextLoop()
        {
            saveNotesCts?.Cancel();
        }

        private void AppModeChangedHandler(AppMode previousAppMode)
        {
            SetReviewFileLoadComplete(loadComplete: false);

            SaveTitleNotesText();

            if (selectedAppModeValue == AppMode.Review &&
                reviewModeHeaderDisplayVM.SelectedReviewModeValue == ReviewMode.Manual)
            {
                StartSaveTitleNotesTextLoop();
            }

            if (selectedAppModeValue == AppMode.Record)
            {
                StartSaveTitleNotesTextLoop();
            }

            var message = new MessageFromStatusModeListViewVMToChessGameVM(SelectedAppModeValue);
            WeakReferenceMessenger.Default.Send(message);

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

            recordReviewModeVM.CurrentAppMode = selectedAppModeValue;
        }

        /// <summary>
        /// Handles the change to Play Mode.
        /// </summary>
        private void AppModeChangedToPlayMode(AppMode previousAppMode)
        {
            CurrentAppModeVM = playModeVM;
        }

        /// <summary>
        /// Handles the change to Record Mode.
        /// </summary>
        private async void AppModeChangedToRecordMode(AppMode previousAppMode)
        {
            if (recordReviewModeVM.RecordingInProgress)
            {
                await reviewModeHeaderDisplayVM.StopAutoReviewLoop();

                recordReviewModeVM.ResetRecordingState();
            }

            if (string.IsNullOrWhiteSpace(recordReviewModeVM.FullFilePath))
            {
                recordReviewModeVM.SetFullFilePath();
            }

            CurrentAppModeVM = recordReviewModeVM;
            recordModeNotReady = false; // Now ready for recording.
        }

        /// <summary>
        /// Handles the change to Review Mode.
        /// </summary>
        private void AppModeChangedToReviewMode(AppMode previousAppMode)
        {
            if (previousAppMode == AppMode.Play && this.getCurrentGameBoardDelegateAndStartNewGame().Game.History.Count() != 0)
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
                    this.getCurrentGameBoardDelegateAndStartNewGame().StartNewGame(); // Start a new game to reset the board.
                }
            }

            // CurrentAppModeVM = recordReviewModeVM;
            CurrentAppModeVM = recordReviewModeVM;

            //ModeAndPlayerStatusDisplayVM = reviewModeHeaderDisplayVM;

            if (!File.Exists(recordReviewModeVM.FullFilePath))
            {
                this.getCurrentGameBoardDelegateAndStartNewGame().StartNewGame(); // Start a new game to reset the board.
            }

            SetReviewMode();

            // If coming from Record mode, and recording is in progress, this means the file is already loaded.
            if (previousAppMode == AppMode.Record && recordReviewModeVM.RecordingInProgress)
            {
                SetReviewFileLoadComplete(loadComplete: true);
            }
        }

        public void AddUpdateXmlToFile()
        {
            if (selectedAppModeValue != AppMode.Record)
            {
                return;
            }

            if (reviewModeHeaderDisplayVM.IsAutoReviewRunning)
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

            if (selectedAppModeValue == AppMode.Record)
            {
                recordReviewModeVM.WriteToXmlFile(this.getCurrentGameBoardDelegateAndStartNewGame().Game);
            }
        }


        private string previousSavedTitleNotes = string.Empty;

        // Add a private lock object to the class
        private readonly object titleNotesLock = new();

        public void SaveTitleNotesText()
        {
            if (selectedAppModeValue == AppMode.Play)
            {
                return; // Only in Record or review mode.
            }

            // Replace the code block with a thread-safe version using lock
            lock (titleNotesLock)
            {
                if (previousSavedTitleNotes == TitleNotesText)
                {
                    return; // No change in title notes, no need to save.
                }

                var moveCount = this.getCurrentGameBoardDelegateAndStartNewGame().Game.History.Count();

                var latestUpdate = this.getCurrentGameBoardDelegateAndStartNewGame().Game.History.FirstOrDefault();

                if (!ChessGame.TitleNotesConcurrentDictionary.ContainsKey(moveCount))
                {
                    if (!ChessGame.TitleNotesConcurrentDictionary.TryAdd(moveCount, (TitleNotesText, this.getCurrentGameBoardDelegateAndStartNewGame().Game.History.FirstOrDefault())))
                    {
                        Debug.WriteLine("Failed to add to TitleNotesDictionaryNew");
                    }
                }
                else
                {
                    var update = ChessGame.TitleNotesConcurrentDictionary[moveCount].update;
                    ChessGame.TitleNotesConcurrentDictionary[moveCount] = (TitleNotesText, update);
                }

                ResetPreviousSavedTitleNotes();

                if (selectedAppModeValue == AppMode.Record)
                {
                    recordReviewModeVM.SaveTitleNotesText(moveCount);
                }

                if (selectedAppModeValue == AppMode.Review &&
                    reviewModeHeaderDisplayVM.SelectedReviewModeValue == ReviewMode.Manual)
                {
                    recordReviewModeVM.SaveTitleNotesText(moveCount);
                }
            }
        }

        private object previousSavedTitleNotesLock = new();
        private void ResetPreviousSavedTitleNotes()
        {
            lock (previousSavedTitleNotesLock)
            {
                previousSavedTitleNotes = TitleNotesText;
            }
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

        private void SetReviewMode()
        {
            var manualAutoReview = ChessAppSettings.Default.ManualAutoReview;
            if (!string.IsNullOrWhiteSpace(ChessAppSettings.Default.ManualAutoReview))
            {
                if (manualAutoReview.Equals("Manual", StringComparison.OrdinalIgnoreCase))
                {
                    reviewModeHeaderDisplayVM.SelectedReviewModeValue = ReviewMode.Manual;
                }
                else if (manualAutoReview.Equals("Auto", StringComparison.OrdinalIgnoreCase))
                {
                    reviewModeHeaderDisplayVM.SelectedReviewModeValue = ReviewMode.Auto;
                }
            }
            else
            {
                reviewModeHeaderDisplayVM.SelectedReviewModeValue = ReviewMode.Manual;
            }
        }

        /// <summary>
        /// Temp. Will be removed.
        /// Just to update move count.
        /// </summary>
        public void RefreshAfterEndTurn()
        {
            // Todo. Only one status should be enough. Either on ChessGameVM or here.
            this.OnPropertyChanged(nameof(this.Status));

            this.Board = this.getCurrentGameBoardDelegateAndStartNewGame().Board;

            this.OnPropertyChanged(nameof(this.Board));

            var moveCount = this.getCurrentGameBoardDelegateAndStartNewGame().Game.History.Count();
            var latestUpdate = this.getCurrentGameBoardDelegateAndStartNewGame().Game.History.FirstOrDefault();

            playModeVM.GameMoveCount = moveCount;

            if (ChessGame.TitleNotesConcurrentDictionary.ContainsKey(moveCount))
            {
                var earlierUpdate = ChessGame.TitleNotesConcurrentDictionary[moveCount].update;

                // If the latest update is different from the earlier update, it means we are taking a different update for the same move count.

                if (earlierUpdate != null)
                {
                    bool isUpdateSameAsLatest = false;
                    if (earlierUpdate.Command is SequenceCommand && latestUpdate.Command is SequenceCommand)
                    {
                        var earlierUpdateFirstCommand = (earlierUpdate.Command as SequenceCommand).FirstCommand;
                        var latestUpdateFirstCommand = (latestUpdate.Command as SequenceCommand).FirstCommand;
                        if (earlierUpdateFirstCommand != null && latestUpdateFirstCommand != null)
                        {
                            if (earlierUpdateFirstCommand.Equals(latestUpdateFirstCommand))
                            {
                                isUpdateSameAsLatest = true;
                            }
                        }
                    }
                    if (!isUpdateSameAsLatest)
                    {
                        // We are taking a different update for the same move count.
                        foreach (var key in ChessGame.TitleNotesConcurrentDictionary.Keys.ToList())
                        {
                            if (key >= moveCount)
                            {
                                ChessGame.TitleNotesConcurrentDictionary.Remove(key, out _);
                            }
                        }
                        TitleNotesText = string.Empty; // Reset title notes text as we are taking a different update for the same move count.
                        if (!ChessGame.TitleNotesConcurrentDictionary.TryAdd(moveCount, (string.Empty, latestUpdate)))
                        {
                            Debug.WriteLine("Failed to add to TitleNotesDictionaryNew");
                        }
                    }
                }

                TitleNotesText = ChessGame.TitleNotesConcurrentDictionary[moveCount].titleNotes;
            }
            else
            {
                TitleNotesText = string.Empty;
            }

            ResetPreviousSavedTitleNotes();
        }
    }
}
