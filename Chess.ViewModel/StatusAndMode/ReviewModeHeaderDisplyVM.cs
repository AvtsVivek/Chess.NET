using Chess.ViewModel.Command;
using Chess.ViewModel.Game;
using Chess.ViewModel.Messages;
using CommunityToolkit.Mvvm.Messaging;
using System.ComponentModel;
using System.Diagnostics;

namespace Chess.ViewModel.StatusAndMode
{
    public class ReviewModeHeaderDisplayVM : INotifyPropertyChanged
    {
        private AutoReviewModeVM autoReviewModeVM;

        private ManualReviewModeVM manualReviewModeVM;

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

        private bool isInReviewMode;
        public bool IsInReviewMode
        {
            get => isInReviewMode;
            set
            {
                if (isInReviewMode != value)
                {
                    isInReviewMode = value;
                    OnPropertyChanged(nameof(IsInReviewMode));
                }
            }
        }

        private string headerText;
        public string HeaderText
        {
            get => headerText;
            set
            {
                if (headerText != value)
                {
                    headerText = value;
                    OnPropertyChanged(nameof(HeaderText));
                }
            }
        }

        private object _currentReviewModeVM;
        public object CurrentReviewModeVM
        {
            get { return _currentReviewModeVM; }
            set
            {
                _currentReviewModeVM = value;
                OnPropertyChanged(nameof(CurrentReviewModeVM));
            }
        }

        private ReviewMode selectedReviewModeValue;
        public ReviewMode SelectedReviewModeValue
        {
            get { return selectedReviewModeValue; }
            set
            {
                if (selectedReviewModeValue != value)
                {
                    selectedReviewModeValue = value;
                    Debug.WriteLine($"Selected Review Mode: {selectedReviewModeValue}");
                    OnPropertyChanged(nameof(SelectedReviewModeValue));
                    SaveReviewModeSetting();
                }
                CurrentReviewModeVM = selectedReviewModeValue == ReviewMode.Auto ? autoReviewModeVM : manualReviewModeVM;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void SaveReviewModeSetting()
        {
            ChessAppSettings.Default.ManualAutoReview = this.SelectedReviewModeValue.ToString();
            ChessAppSettings.Default.Save();
        }
    }
}
