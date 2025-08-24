using Chess.ViewModel.StatusAndMode;
using System.Windows;
using System.Windows.Controls;

namespace Chess.View.Window
{
    /// <summary>
    /// Interaction logic for AutoReviewModeView.xaml
    /// </summary>
    public partial class AutoReviewModeView : UserControl
    {
        private AutoReviewModeVM viewModel;

        public AutoReviewModeView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            viewModel = DataContext as AutoReviewModeVM;
            viewModel?.ViewLoaded();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            viewModel?.ViewUnloaded();
        }
    }
}
