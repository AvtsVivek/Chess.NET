using Chess.ViewModel.Command;
using System.ComponentModel;
using System.Windows;

namespace Chess.ViewModel.StatusAndMode
{
    public class ManualReviewModeVM: INotifyPropertyChanged
    {
        private readonly GenericCommand undoCommand;

        private readonly GenericCommand redoCommand;
        public ManualReviewModeVM(GenericCommand undoCommand, GenericCommand redoCommand)
        {
            this.undoCommand = undoCommand;

            this.redoCommand = redoCommand;
        }

        public GenericCommand UndoCommand => this.undoCommand;

        public GenericCommand RedoCommand => this.redoCommand;

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