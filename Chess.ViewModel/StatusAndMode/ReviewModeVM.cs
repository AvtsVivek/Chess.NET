using System;
using System.ComponentModel;

namespace Chess.ViewModel.StatusAndMode
{
    public class ReviewModeVM: INotifyPropertyChanged
    {

        public ReviewModeVM()
        {
            IsReviewFileInRecording = false;
        }

        public bool IsReviewFileInRecording { get; set; }

        private string fullFilePath;
        public string FullFilePath
        {
            get
            {
                return fullFilePath;
            }
            set
            {
                if (fullFilePath != value)
                {
                    fullFilePath = value ?? throw new ArgumentNullException(nameof(FullFilePath));
                }
                OnPropertyChanged(nameof(FullFilePath));
            }
        }

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Fires the <see cref="PropertyChanged"/> event.
        /// </summary>
        /// <param name="propertyName">The name of the property that has been changed.</param>
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
