using Chess.ViewModel.Command;
using Chess.ViewModel.Game;
using Chess.ViewModel.Messages;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;


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

            WeakReferenceMessenger.Default.Register<ReviewMessage>(this, (r, m) =>
            {
                IsInReviewMode = m.StartReviewLoop;
            });
        }

        private ReviewMode selectedReviewModeValue;
        public ReviewMode SelectedReviewModeValue
        {
            get => selectedReviewModeValue; 
            set
            {
                SetProperty(ref selectedReviewModeValue, value);
                SaveReviewModeSetting();
                CurrentReviewModeVM = selectedReviewModeValue == ReviewMode.Auto ? autoReviewModeVM : manualReviewModeVM;
            }
        }

        private void SaveReviewModeSetting()
        {
            ChessAppSettings.Default.ManualAutoReview = this.SelectedReviewModeValue.ToString();
            ChessAppSettings.Default.Save();
        }
    }
}
