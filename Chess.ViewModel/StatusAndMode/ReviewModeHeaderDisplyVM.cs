using System.ComponentModel;

namespace Chess.ViewModel.StatusAndMode
{
    public class ReviewModeHeaderDisplayVM : INotifyPropertyChanged
    {
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
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
