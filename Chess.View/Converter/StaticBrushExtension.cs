using System;
using System.Windows.Markup;
using System.Windows.Media;
using Chess.ViewModel.Game;

namespace Chess.View.Converter
{
    public class StaticBrushExtension : MarkupExtension
    {
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return new SolidColorBrush(BoardConstants.BoardFieldDarkBrushColor);
        }
    }
}