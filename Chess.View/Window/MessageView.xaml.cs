using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.ComponentModel;
using Chess.ViewModel.StatusAndMode;

namespace Chess.View.Window
{
    /// <summary>
    /// Interaction logic for MessageView.xaml
    /// </summary>
    public partial class MessageView : UserControl
    {
        public MessageView()
        {
            InitializeComponent();
            this.DataContextChanged += MessageView_DataContextChanged;
        }

        private void MessageView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is INotifyPropertyChanged npc)
            {
                npc.PropertyChanged += ViewModel_PropertyChanged;
            }
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MessageVM.MessageText))
            {
                // Stop any previous animation
                MessageTextBlock.BeginAnimation(UIElement.OpacityProperty, null);

                // Reset opacity
                MessageTextBlock.Opacity = 1;

                var fadeOut = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    BeginTime = TimeSpan.FromSeconds(3),
                    Duration = TimeSpan.FromSeconds(1)
                };

                fadeOut.Completed += (s, args) =>
                {
                    if (this.DataContext is MessageVM vm)
                    {
                        vm.MessageText = string.Empty;
                    }
                    MessageTextBlock.Opacity = 1;
                };

                MessageTextBlock.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            }
        }
    }
}
