using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Chess.View.Converter
{
    public static class TextBoxPlaceholderBehavior
    {
        public static readonly DependencyProperty PlaceholderProperty =
            DependencyProperty.RegisterAttached(
                "Placeholder",
                typeof(string),
                typeof(TextBoxPlaceholderBehavior),
                new PropertyMetadata(string.Empty, OnPlaceholderChanged));

        public static string GetPlaceholder(TextBox textBox) => (string)textBox.GetValue(PlaceholderProperty);
        public static void SetPlaceholder(TextBox textBox, string value) => textBox.SetValue(PlaceholderProperty, value);

        private static void OnPlaceholderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox)
            {
                // Remove previous event handlers to avoid multiple subscriptions
                textBox.Loaded -= TextBox_EventsHandler;
                textBox.TextChanged -= TextBox_EventsHandler;
                textBox.GotFocus -= TextBox_EventsHandler;
                textBox.LostFocus -= TextBox_EventsHandler;

                textBox.Loaded += TextBox_EventsHandler;
                textBox.TextChanged += TextBox_EventsHandler;
                textBox.GotFocus += TextBox_EventsHandler;
                textBox.LostFocus += TextBox_EventsHandler;

                // Also update immediately when the property changes
                UpdatePlaceholder(textBox);

            }
        }

        private static void TextBox_EventsHandler(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
                UpdatePlaceholder(textBox);
        }

        private static void UpdatePlaceholder(TextBox textBox)
        {
            var adornerLayer = AdornerLayer.GetAdornerLayer(textBox);
            if (adornerLayer == null) return;

            var adorners = adornerLayer.GetAdorners(textBox);
            if (adorners != null)
            {
                foreach (var adorner in adorners)
                {
                    if (adorner is PlaceholderAdorner)
                        adornerLayer.Remove(adorner);
                }
            }

            if (string.IsNullOrEmpty(textBox.Text) && !textBox.IsFocused)
            {
                adornerLayer.Add(new PlaceholderAdorner(textBox, GetPlaceholder(textBox)));
            }
        }
    }
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
