using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;

namespace Chess.ViewModel.StatusAndMode
{
    public class PlayModeVM : ObservableObject
    {
        private int gameMoveCount;
        public int GameMoveCount
        {
            get => gameMoveCount;
            set
            {
                if (gameMoveCount != value)
                {
                    gameMoveCount = value;
                    OnPropertyChanged(nameof(GameMoveCount));
                }
            }
        }
    }
}
