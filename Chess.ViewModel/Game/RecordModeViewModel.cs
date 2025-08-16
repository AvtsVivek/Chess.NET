using Chess.ViewModel.Command;

namespace Chess.ViewModel.Game
{
    public class RecordModeViewModel
    {
        public RecordModeViewModel() 
        {
            this.selectFolderCommand = new GenericCommand
            (
                () => true,
                () => {

                }
            );

            this.setFileNameCommand = new GenericCommand
            (
                () => true,
                () => {

                }
            );
        }

        private readonly GenericCommand selectFolderCommand;

        public GenericCommand SelectFolderCommand => this.selectFolderCommand;

        private readonly GenericCommand setFileNameCommand;

        public GenericCommand SetFileNameCommand => this.setFileNameCommand;
    }
}
