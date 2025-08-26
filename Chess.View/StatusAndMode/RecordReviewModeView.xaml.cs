using Chess.ViewModel.StatusAndMode;
using System.Windows;
using System.Windows.Controls;

namespace Chess.View.StatusAndMode
{
    /// <summary>
    /// Interaction logic for RecordReviewModeView.xaml
    /// </summary>
    public partial class RecordReviewModeView : UserControl
    {
        private RecordReviewModeVM viewModel;

        public RecordReviewModeView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            viewModel = DataContext as RecordReviewModeVM;
            viewModel?.ViewLoaded();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            viewModel?.ViewUnloaded();
        }
    }
}
