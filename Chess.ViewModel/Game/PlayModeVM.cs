using System.ComponentModel;

namespace Chess.ViewModel.Game
{
    public class PlayModeVM : INotifyPropertyChanged
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

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Fires the <see cref="PropertyChanged"/> event.
        /// </summary>
        /// <param name="propertyName">The name of the property that has been changed.</param>
        protected void OnPropertyChanged(string propertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
