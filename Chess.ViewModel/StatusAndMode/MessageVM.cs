using CommunityToolkit.Mvvm.ComponentModel;

namespace Chess.ViewModel.StatusAndMode
{
    public partial class HeaderNotificationVM : ObservableObject
    {
        public HeaderNotificationVM()
        {
        }

        [ObservableProperty]
        private string messageText;

        public void ClearMessage()
        {
            MessageText = string.Empty;
        }

    }
}
