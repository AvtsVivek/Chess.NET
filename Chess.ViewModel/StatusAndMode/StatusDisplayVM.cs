using Chess.Model.Game;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Chess.ViewModel.StatusAndMode
{
    public partial class StatusDisplayVM : ObservableObject
    {
        [ObservableProperty]
        private string statusText;

        public StatusDisplayVM(Status status)
        {
            UpdateStatus(status);
        }

        public void UpdateStatus(Status status)
        {
            StatusText = new Services.StatusToStringConverter().Convert(status);
        }
    }
}
