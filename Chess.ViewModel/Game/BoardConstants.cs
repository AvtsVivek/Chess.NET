using System.Windows.Media;

namespace Chess.ViewModel.Game
{
    public class BoardConstants
    {
        public const double BoardMarginForId = 0.25;
        public const double BoardFieldLength = 8;
        public const double FullCanvasLength = BoardFieldLength + 2 * BoardMarginForId;
        public static Color BoardFieldLightBrushColor = Colors.NavajoWhite;
        public static Color BoardFieldDarkBrushColor = Colors.Peru;

        public static double CustomBoardMargin = 0.1;
        public static double CustomBoardFieldLength = 4;
        public static double CustomBoardFullCanvasLength = CustomBoardFieldLength + 2 * CustomBoardMargin;
    }
}
