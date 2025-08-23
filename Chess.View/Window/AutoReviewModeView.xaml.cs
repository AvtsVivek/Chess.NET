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

namespace Chess.View.Window
{
    /// <summary>
    /// Interaction logic for AutoReviewModeView.xaml
    /// </summary>
    public partial class AutoReviewModeView : UserControl
    {
        public AutoReviewModeView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as AutoReviewModeVM;
            vm.ViewLoaded();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as AutoReviewModeVM;
            vm.ViewUnloaded();
        }
    }
}
