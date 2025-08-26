using System.ComponentModel;

namespace Chess.ViewModel.StatusAndMode
{
    public class HeaderNotificationVM: INotifyPropertyChanged
    {
        public HeaderNotificationVM()
        {
        }

        private string messageText;
        public string MessageText
        {
            get => messageText;
            set
            {
                if (messageText != value)
                {
                    messageText = value;
                    OnPropertyChanged(nameof(MessageText));
                }
            }
        }

        public void ClearMessage()
        {
            MessageText = string.Empty;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
