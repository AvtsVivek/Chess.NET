using Chess.ViewModel.Command;
using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;

namespace Chess.ViewModel.StatusAndMode
{
    public class ManualReviewModeVM : ObservableObject
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
    }
}