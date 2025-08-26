using CommunityToolkit.Mvvm.ComponentModel;

namespace Chess.ViewModel.StatusAndMode
{
    public partial class PlayModeVM : ObservableObject
    {
        [ObservableProperty]
        private int gameMoveCount;
    }
}
