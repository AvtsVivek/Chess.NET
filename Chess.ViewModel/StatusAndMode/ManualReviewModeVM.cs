using Chess.ViewModel.Command;
using Chess.ViewModel.Messages;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System.Windows;

namespace Chess.ViewModel.StatusAndMode
{
    public class ManualReviewModeVM : ObservableObject
    {
        private readonly GenericCommand undoOriginalCommand;

        private readonly GenericCommand redoOriginalCommand;

        private readonly GenericCommand undoCommand;

        private readonly GenericCommand redoCommand;

        private readonly GenericCommand getToStartCommand;

        private readonly GenericCommand getToLastCommand;

        public ManualReviewModeVM(GenericCommand undoCommand, GenericCommand redoCommand)
        {
            undoOriginalCommand = undoCommand;

            redoOriginalCommand = redoCommand;

            this.undoCommand = new GenericCommand(() => undoCommand.CanExecute(null), Undo);

            this.redoCommand = new GenericCommand(() => redoCommand.CanExecute(null), Redo);

            this.getToStartCommand = new GenericCommand(() => undoCommand.CanExecute(null), GetToStart);

            this.getToLastCommand = new GenericCommand(() => redoCommand.CanExecute(null), GetToLast);

            WeakReferenceMessenger.Default.Register<MessageToManualReviewVM>(this, (r, m) =>
            {
                RaiseCanExecuteChanged();
            });
        }

        public GenericCommand UndoCommand => this.undoCommand;

        public GenericCommand RedoCommand => this.redoCommand;

        public GenericCommand GetToStartCommand => this.getToStartCommand;

        public GenericCommand GetToLastCommand => this.getToLastCommand;

        private void Undo()
        {
            this.undoOriginalCommand.Execute(null);
            RaiseCanExecuteChanged();
        }

        private void Redo()
        {
            this.redoOriginalCommand.Execute(null);
            RaiseCanExecuteChanged();
        }

        public void GetToStart()
        {
            System.Threading.Tasks.Task.Run(() =>
            {
                while (this.undoCommand.CanExecute(null))
                {
                    this.undoCommand.Execute(null);
                }

                // First, oldest, farthest 
                ChessAppSettings.Default.ReviewFromLast = false;
                ChessAppSettings.Default.Save();

                // RaiseCanExecuteChanged must be called on the UI thread
                Application.Current.Dispatcher.Invoke(RaiseCanExecuteChanged);
            });
        }

        public void GetToLast()
        {
            System.Threading.Tasks.Task.Run(() =>
            {
                while (this.redoCommand.CanExecute(null))
                {
                    this.redoCommand.Execute(null);
                }

                // Last, most recent, closest 
                ChessAppSettings.Default.ReviewFromLast = true;
                ChessAppSettings.Default.Save();
                // SendMessageToManualReviewVM must be called on the UI thread
                Application.Current.Dispatcher.Invoke(RaiseCanExecuteChanged);
            });
        }

        private void RaiseCanExecuteChanged()
        {
            this.undoCommand.FireCanExecuteChanged();
            this.redoCommand.FireCanExecuteChanged();
            this.getToStartCommand.FireCanExecuteChanged();
            this.getToLastCommand.FireCanExecuteChanged();
        }
    }
}