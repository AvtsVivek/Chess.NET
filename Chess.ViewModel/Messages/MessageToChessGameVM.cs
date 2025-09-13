namespace Chess.ViewModel.Messages
{
    using Chess.Model.Game;
    using CommunityToolkit.Mvvm.Messaging.Messages;

    public class MessageFromRecordReviewModeVMToChessGameVM : ValueChangedMessage<ChessGame>
    {
        public string IsHeaderNotificationMessage { get; set; } = string.Empty;
        public MessageFromRecordReviewModeVMToChessGameVM(ChessGame game, string isHeaderNotificationMessage = "") : base(game)
        {
            this.IsHeaderNotificationMessage = isHeaderNotificationMessage;
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