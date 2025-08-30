using Chess.Model.Game;
using Chess.ViewModel.Command;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Chess.ViewModel.StatusAndMode
{
    public partial class StatusDisplayVM : ObservableObject
    {
        [ObservableProperty]
        private string statusText;

        private GenericCommand toggleBoardInversionCommand;

        /// <summary>
        /// Represents the undo command, which reverts to a previous game state.
        /// </summary>
        private readonly GenericCommand invertBoardCommand;
        public StatusDisplayVM(Status status, GenericCommand toggleBoardInversionCommand)
        {
            UpdateStatus(status);

            this.invertBoardCommand = new GenericCommand(() => true, InvertBoard);

            this.toggleBoardInversionCommand = toggleBoardInversionCommand;
        }

        private void InvertBoard()
        {
            toggleBoardInversionCommand.Execute(null);
        }

        /// <summary>
        /// Gets the command that reverts the last action of the presented chess game.
        /// </summary>
        /// <value>The command that reverts the last action of the presented chess game.</value>
        public GenericCommand InvertBoardCommand => this.invertBoardCommand;

        public void UpdateStatus(Status status)
        {
            StatusText = new Services.StatusToStringConverter().Convert(status);
        }
    }
}
