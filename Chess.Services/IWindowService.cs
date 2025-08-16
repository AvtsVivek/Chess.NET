using System.Windows;

namespace Chess.Services
{
    public interface IWindowService
    {
        string ShowSetRecordFilePathWindow(string folderPath, string fileName);

        MessageBoxResult ShowMessageBox(string text, string caption, MessageBoxButton buttons);
    }
}
