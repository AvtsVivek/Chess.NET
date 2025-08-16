using Chess.Services;
using System;
using System.Windows;

namespace Chess.View.Window
{
    public class WindowService : IWindowService
    {
        public MessageBoxResult ShowMessageBox(string text, string caption, MessageBoxButton buttons)
        {
            return MessageBox.Show(text, caption, buttons, MessageBoxImage.Information);
        }

        public string ShowSetRecordFilePathWindow(string folderPath, string fileName)
        {
            var window = new SetRecordFilePath(folderPath, fileName);
            var result = window.ShowDialog();
            return result == true ? window.SelectedFilePath : null;
        }
    }
}
