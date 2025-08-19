using Chess.Model.Game;

namespace Chess.Services
{
    public class StatusToStringConverter
    {
        public StatusToStringConverter() { }

        public string Convert(Status status)
        {
            switch (status)
            {
                case Status.WhiteTurn:
                    return "Status: White Playing";
                case Status.WhiteWin:
                    return "Game End: 1-0";
                case Status.BlackTurn:
                    return "Status: Black Playing";
                case Status.BlackWin:
                    return "Game End: 0-1";
                case Status.Draw:
                    return "Game End: \u00bd-\u00bd";
                default:
                    return string.Empty;
            }
        }
    }
}
