using Chess.ViewModel.Game;
using Chess.ViewModel.StatusAndMode;
using System;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Input;

namespace Chess.View.StatusAndMode
{
    /// <summary>
    /// Interaction logic for BuildCustomBoardView.xaml
    /// </summary>
    public partial class BuildCustomBoardView : UserControl
    {
        public BuildCustomBoardView()
        {
            InitializeComponent();
        }

        private void MainBoardCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var point = Mouse.GetPosition(sender as Canvas);
            var maxRowOrColumn = (int)BoardConstants.CustomBoardFieldLength - 1;

            var row = maxRowOrColumn - (int)(point.Y - BoardConstants.CustomBoardMargin);
            var column = (int)(point.X - BoardConstants.CustomBoardMargin);

            var validRow = Math.Max(0, Math.Min(maxRowOrColumn, row));
            var validColumn = Math.Max(0, Math.Min(maxRowOrColumn, column));

            Debug.WriteLine($"valid row: {validRow}, valid column: {validColumn}");

            var buildCustomBoardVM = this.DataContext as BuildCustomBoardVM;

            buildCustomBoardVM?.HandleBoardClick(validRow, validColumn);
        }
    }
}
