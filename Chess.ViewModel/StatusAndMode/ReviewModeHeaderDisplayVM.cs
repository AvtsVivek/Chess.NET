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

        public ReviewModeHeaderDisplayVM(GenericCommand undoCommand, GenericCommand redoCommand)
        {
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
                // Optionally, call the async method without await (fire-and-forget)
                SetSelectedReviewModeValueAsync(value);
                // Or just set the value and let the caller handle the async logic
                // SetProperty(ref selectedReviewModeValue, value);
            }
        }

        public async void SetSelectedReviewModeValueAsync(ReviewMode value)
        {
            SetProperty(ref selectedReviewModeValue, value);

            SaveReviewModeSetting();

            IsInReviewMode = true;

            if (selectedReviewModeValue == ReviewMode.Auto)
            {
                CurrentReviewModeVM = autoReviewModeVM;
                autoReviewModeVM.StartAutoReviewLoop();
            }
            else
            {
                CurrentReviewModeVM = manualReviewModeVM;
                await autoReviewModeVM.StopAutoReviewLoop();
            }
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
