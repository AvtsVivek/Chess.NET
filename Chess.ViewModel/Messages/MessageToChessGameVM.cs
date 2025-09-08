namespace Chess.ViewModel.Messages
{
    using Chess.Model.Game;
    using CommunityToolkit.Mvvm.Messaging.Messages;

    public class MessageFromRecordReviewModeVMToChessGameVM : ValueChangedMessage<ChessGame>
    {
        public MessageFromRecordReviewModeVMToChessGameVM(ChessGame game) : base(game)
        {
        }
    }

    public class MessageFromAutoReviewModeVMToChessGameVM 
    {
        public string Code { get; init; } = string.Empty;
        public MessageFromAutoReviewModeVMToChessGameVM(string code) 
        {
            Code = code;
        }
    }
}