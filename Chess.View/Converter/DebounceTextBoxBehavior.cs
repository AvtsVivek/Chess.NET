using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Chess.View.Converter
{
    public static class DebounceTextBoxBehavior
    {
        public static readonly DependencyProperty DebounceDelayProperty =
            DependencyProperty.RegisterAttached(
                "DebounceDelay",
                typeof(TimeSpan),
                typeof(DebounceTextBoxBehavior),
                new PropertyMetadata(TimeSpan.Zero, OnDebounceDelayChanged));

        public static TimeSpan GetDebounceDelay(TextBox textBox) => (TimeSpan)textBox.GetValue(DebounceDelayProperty);
        public static void SetDebounceDelay(TextBox textBox, TimeSpan value) => textBox.SetValue(DebounceDelayProperty, value);

        private static void OnDebounceDelayChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox)
            {
                textBox.TextChanged -= TextBox_TextChanged;
                if ((TimeSpan)e.NewValue > TimeSpan.Zero)
                {
                    textBox.TextChanged += TextBox_TextChanged;
                }
            }
        }

        private static void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            var delay = GetDebounceDelay(textBox);
            if (delay <= TimeSpan.Zero) return;

            textBox.Dispatcher.InvokeAsync(() =>
            {
                var timer = textBox.Tag as DispatcherTimer;
                timer?.Stop();

                timer = new DispatcherTimer { Interval = delay };
                timer.Tick += (s, args) =>
                {
                    timer.Stop();
                    var binding = textBox.GetBindingExpression(TextBox.TextProperty);
                    binding?.UpdateSource();
                };
                textBox.Tag = timer;
                timer.Start();
            }, DispatcherPriority.Background);
        }
    }

}
