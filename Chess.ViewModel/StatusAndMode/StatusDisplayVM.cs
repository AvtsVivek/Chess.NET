using Chess.Model.Game;
using System.ComponentModel;

namespace Chess.ViewModel.StatusAndMode
{
    public class StatusDisplayVM : INotifyPropertyChanged
    {
        public StatusDisplayVM(Status status)
        {
            UpdateStatus(status);
        }
        private string statusText;
        public string StatusText
        {
            get => statusText;
            set
            {
                if (statusText != value)
                {
                    statusText = value;
                    OnPropertyChanged(nameof(StatusText));
                }
            }
        }

        public void UpdateStatus(Status status)
        {
            StatusText = new Services.StatusToStringConverter().Convert(status);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
