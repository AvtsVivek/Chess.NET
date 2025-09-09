using Chess.Model.Game;
using Chess.ViewModel.Command;
using Chess.ViewModel.Game;
using Chess.ViewModel.Messages;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System.Threading.Tasks;


namespace Chess.ViewModel.StatusAndMode
{
    public partial class ReviewModeHeaderDisplayVM : ObservableObject
    {
        private AutoReviewModeVM autoReviewModeVM;

        private ManualReviewModeVM manualReviewModeVM;

        [ObservableProperty]
        private bool isInReviewMode;

        [ObservableProperty]
        private string headerText;

        [ObservableProperty]
        private object currentReviewModeVM;

        [ObservableProperty]
        private Status status;

        public ReviewModeHeaderDisplayVM(GenericCommand undoCommand, GenericCommand redoCommand, Status status)
        {
            Status = status;
            autoReviewModeVM = new(undoCommand, redoCommand);
            manualReviewModeVM = new(undoCommand, redoCommand);
            CurrentReviewModeVM = manualReviewModeVM;

            WeakReferenceMessenger.Default.Register<MessageFromRecordReviewModeVMToReviewModeHeaderDisplayVM>(this, async (r, m) =>
            {
                IsInReviewMode = m.StartReviewLoop;
                
                if (!m.StartReviewLoop)
                {
                    await autoReviewModeVM.StopAutoReviewLoop();
                }

                if (IsInReviewMode && selectedReviewModeValue == ReviewMode.Auto)
                {
                    autoReviewModeVM.StartAutoReviewLoop();
                }
            });
        }

        private ReviewMode selectedReviewModeValue;
        public ReviewMode SelectedReviewModeValue
        {
            get => selectedReviewModeValue;
            set
            {
                var previousReviewMode = selectedReviewModeValue;
                SetProperty(ref selectedReviewModeValue, value);
                SetSelectedReviewModeValueAsync(previousReviewMode);
            }
        }

        public void UpdateStatus(Status status)
        {
            Status = status; 
        }

        public async void SetSelectedReviewModeValueAsync(ReviewMode previousReviewMode)
        {
            SaveReviewModeSetting();

            IsInReviewMode = true;

            if (selectedReviewModeValue == ReviewMode.Auto)
            {
                if(previousReviewMode == ReviewMode.Manual)
                {
                    // If the previous mode was manual, its likely the review file was already loaded.
                    autoReviewModeVM.ReviewFileLoadCompleted = true;
                }
                CurrentReviewModeVM = autoReviewModeVM;
                autoReviewModeVM.StartAutoReviewLoop();
            }
            else
            {
                CurrentReviewModeVM = manualReviewModeVM;
                await autoReviewModeVM.StopAutoReviewLoop();
            }

            var message = new MessageFromReviewModeHeaderDisplayVMToChessGameVM(selectedReviewModeValue);
            WeakReferenceMessenger.Default.Send(message);
        }

        public async Task StopAutoReviewLoop()
        {
            if(SelectedReviewModeValue == ReviewMode.Auto)
                await autoReviewModeVM.StopAutoReviewLoop();
        }

        public bool IsAutoReviewRunning => autoReviewModeVM.IsAutoReviewRunning;

        private void SaveReviewModeSetting()
        {
            ChessAppSettings.Default.ManualAutoReview = this.SelectedReviewModeValue.ToString();
            ChessAppSettings.Default.Save();
        }
    }
}
