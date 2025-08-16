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
    /// Interaction logic for RecordModeView.xaml
    /// </summary>
    public partial class RecordModeView : UserControl
    {
        public RecordModeView()
        {
            InitializeComponent();
        }

        private void btnSelectFolder_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Select Folder button clicked!");
        }

        private void btnSetFileName_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Set file name button clicked!");
        }
    }
}
