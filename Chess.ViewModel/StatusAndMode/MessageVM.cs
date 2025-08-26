using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;

namespace Chess.ViewModel.StatusAndMode
{
    public class HeaderNotificationVM : ObservableObject
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

    }
}
