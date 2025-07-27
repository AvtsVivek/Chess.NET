using Chess.ViewModel.Game;
using System.Windows;
using System.Windows.Controls;

namespace Chess.View.Selector
{
    public class RowColumnIdSelector : DataTemplateSelector
    {
        public RowColumnIdSelector()
        {
        }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is RowColumnLabelVM id)
            {
                return Application.Current.FindResource(id.LabelResourceKey) as DataTemplate;
            }
            return base.SelectTemplate(item, container);
        }
    }

    public class PieceSelectorForMoveSequenceListView : DataTemplateSelector
    {
        public PieceSelectorForMoveSequenceListView()
        {
        }
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is ChessMoveVM chessMove)
            {
                return Application.Current.FindResource(chessMove.PieceId) as DataTemplate;
            }
            return base.SelectTemplate(item, container);
        }
    }
}
