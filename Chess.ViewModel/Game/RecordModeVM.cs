using Chess.Services;
using Chess.ViewModel.Command;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows;

namespace Chess.ViewModel.Game
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

            this.setFullFilePathCommand = new GenericCommand(() => true, ResetRecordingState);
        }

        private readonly GenericCommand setFullFilePathCommand;

        public GenericCommand SetFullFilePathCommand => this.setFullFilePathCommand;

        private string fullFilePath;
        public string FullFilePath 
        {
            get 
            {
                return this.fullFilePath;
            }
            set
            {
                if (this.fullFilePath != value)
                {
                    this.fullFilePath = value ?? throw new ArgumentNullException(nameof(this.FullFilePath));
                }
                OnPropertyChanged(nameof(FullFilePath));
            }
        }

        public bool RecordingInProgress { get; set; } = false;

        public void ResetRecordingState()
        {
            var selectedPath = string.Empty;

            if (this.RecordingInProgress)
            {
                var oldRecordingPath = FullFilePath;
                var result = this.windowService.ShowMessageBox(
                    "Recording is in progress. Do you want to go ahead to change folder and file name?",
                    "Recording in Progress",
                    MessageBoxButton.YesNo
                );

                if (result == MessageBoxResult.No)
                {
                    return;
                }

                if (result == MessageBoxResult.Yes)
                {
                    selectedPath = this.windowService.ShowSetRecordFilePathWindow(Path.GetDirectoryName(FullFilePath), Path.GetFileName(FullFilePath));

                    if (string.IsNullOrWhiteSpace(selectedPath))
                    {
                        return; // User cancelled the operation
                    }

                    if (selectedPath == oldRecordingPath)
                    {
                        this.windowService.ShowMessageBox(
                            "The file path is same as before. No changes made.",
                            "No Changes Made",
                            MessageBoxButton.OK
                        );
                        return;
                    }

                    this.RecordingInProgress = false;

                    this.windowService.ShowMessageBox(
                        $"Recording will now be done to this new path {selectedPath}"
                        + Environment.NewLine +
                        $"instead of the old path {oldRecordingPath}",
                        "Recording Path Changed",
                        MessageBoxButton.OK
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
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
