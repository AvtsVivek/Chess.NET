using Chess.ViewModel.StatusAndMode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
