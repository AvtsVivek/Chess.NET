using Chess.Model.Game;
using Chess.Services;
using Chess.ViewModel.Command;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows;

namespace Chess.ViewModel.StatusAndMode
{
    public class RecordModeVM : INotifyPropertyChanged
    {
        private readonly IWindowService windowService;
        public RecordModeVM(IWindowService windowService)
        {
            this.windowService = windowService ?? throw new ArgumentNullException(nameof(windowService));

            var initialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            if (!string.IsNullOrWhiteSpace(ChessAppSettings.Default.XmlFolderPath)
                && Directory.Exists(ChessAppSettings.Default.XmlFolderPath))
            {
                initialDirectory = ChessAppSettings.Default.XmlFolderPath;
            }

            var fileName = XmlFileService.GetFileName();

            FullFilePath = Path.Combine(initialDirectory, fileName);

            setFullFilePathCommand = new GenericCommand(() => true, ResetRecordingState);
        }

        private readonly GenericCommand setFullFilePathCommand;

        public GenericCommand SetFullFilePathCommand => setFullFilePathCommand;

        private string fullFilePath;
        public string FullFilePath 
        {
            get 
            {
                return fullFilePath;
            }
            set
            {
                if (fullFilePath != value)
                {
                    fullFilePath = value ?? throw new ArgumentNullException(nameof(FullFilePath));
                }
                OnPropertyChanged(nameof(FullFilePath));
            }
        }

        public bool RecordingInProgress { get; private set; } = false;

        public void ResetRecordingState()
        {
            var selectedPath = string.Empty;

            if (RecordingInProgress)
            {
                var oldRecordingPath = FullFilePath;
                var result = windowService.ShowMessageBox(
                    "Recording is in progress at the following file location " + Environment.NewLine +
                    $"{oldRecordingPath}" + Environment.NewLine +
                    "Do you want to go ahead and use the same file to record?" + Environment.NewLine +
                    "Click Yes, set a new file for recording." + Environment.NewLine +
                    "Click No, to continue to use the same file for recording." + Environment.NewLine,
                    "Recording in Progress",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information
                );

                if (result == MessageBoxResult.No)
                {
                    return;
                }

                if (result == MessageBoxResult.Yes)
                {
                    selectedPath = windowService.ShowSetRecordFilePathWindow(Path.GetDirectoryName(FullFilePath), Path.GetFileName(FullFilePath));

                    if (string.IsNullOrWhiteSpace(selectedPath))
                    {
                        return; // User cancelled the operation
                    }

                    if (selectedPath == oldRecordingPath)
                    {
                        windowService.ShowMessageBox(
                            "The file path is same as before. No changes made.",
                            "No Changes Made",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information
                        );
                        return;
                    }

                    RecordingInProgress = false;

                    windowService.ShowMessageBox(
                        $"Recording will now be done to this new path {selectedPath}"
                        + Environment.NewLine +
                        $"instead of the old path {oldRecordingPath}",
                        "Recording Path Changed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
            }
            else
            {
                selectedPath = windowService.ShowSetRecordFilePathWindow(Path.GetDirectoryName(FullFilePath), Path.GetFileName(FullFilePath));
            }

            if (!string.IsNullOrEmpty(selectedPath))
            {
                FullFilePath = selectedPath;
                ChessAppSettings.Default.XmlFolderPath = Path.GetDirectoryName(selectedPath);
                ChessAppSettings.Default.Save();
            }
        }

        public void WriteToXmlFile(ChessGame chessGame)
        {
            if (string.IsNullOrWhiteSpace(FullFilePath))
            {
                throw new InvalidOperationException("Full file path must be set before writing to XML file.");
            }

            new XmlFileService().WriteToXmlFile(chessGame, FullFilePath);
            RecordingInProgress = true;
        }

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Fires the <see cref="PropertyChanged"/> event.
        /// </summary>
        /// <param name="propertyName">The name of the property that has been changed.</param>
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
