using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Chess.View.Converter
{
    public class PlaceholderAdorner : Adorner
    {
        private readonly string _placeholder;
        public PlaceholderAdorner(UIElement adornedElement, string placeholder)
            : base(adornedElement)
        {
            _placeholder = placeholder;
            IsHitTestVisible = false;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            var textBox = AdornedElement as TextBox;
            if (textBox == null) return;

            var typeface = new Typeface(textBox.FontFamily, textBox.FontStyle, textBox.FontWeight, textBox.FontStretch);
            var formattedText = new FormattedText(
                _placeholder,
                System.Globalization.CultureInfo.CurrentCulture,
                textBox.FlowDirection,
                typeface,
                textBox.FontSize,
                Brushes.Gray,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);

            drawingContext.DrawText(formattedText, new Point(2, 2));
        }
    }
}
