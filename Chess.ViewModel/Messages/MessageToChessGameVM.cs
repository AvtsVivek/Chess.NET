namespace Chess.ViewModel.Messages
{
    using Chess.Model.Game;
    using CommunityToolkit.Mvvm.Messaging.Messages;

    public class MessageToChessGameVM : ValueChangedMessage<ChessGame>
    {
        public MessageToChessGameVM(ChessGame game) : base(game)
        {
        }
    }
}