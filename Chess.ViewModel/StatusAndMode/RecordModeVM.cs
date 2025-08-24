using Chess.Model.Game;
using Chess.Services;
using Chess.ViewModel.Command;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace Chess.ViewModel.StatusAndMode
{
    public class RecordModeVM : INotifyPropertyChanged
    {
        private readonly IWindowService windowService;
        private string fullFilePath;
        private string userFolderPath;
        private readonly GenericCommand setFullFilePathCommand;
        private readonly GenericCommand openFolderInWindowsExplorerCommand;
        private readonly GenericCommand openFolderInVsCodeCommand;
        private readonly GenericCommand setParentFolderCommand;
        private readonly GenericCommand copyFolderPathCommand;

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
            openFolderInWindowsExplorerCommand = new GenericCommand(() => true, OpenFolderInWindowExplorer);
            openFolderInVsCodeCommand = new GenericCommand(() => true, OpenFolderInVsCode);
            setParentFolderCommand = new GenericCommand(() => true, SetParentFolder);
            copyFolderPathCommand = new GenericCommand(() => true, CopyFolderPath);
        }

        public GenericCommand SetFullFilePathCommand => setFullFilePathCommand;

        public GenericCommand OpenFolderInWindowsExplorerCommand => openFolderInWindowsExplorerCommand;

        public GenericCommand OpenFolderInVsCodeCommand => openFolderInVsCodeCommand;

        public GenericCommand SetParentFolderCommand => setParentFolderCommand;

        public GenericCommand CopyFolderPathCommand => copyFolderPathCommand;

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

                    CopyFolderPath();
                }
                OnPropertyChanged(nameof(FullFilePath));
            }
        }

        private void SetUserFolderPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException(nameof(path));
            }
            UserFolderPath = path;
        }

        private void SetParentFolder()
        {
            var directoryInfo = Directory.GetParent(UserFolderPath);
            if (directoryInfo != null && Directory.Exists(directoryInfo.FullName))
            {
                SetUserFolderPath(directoryInfo.FullName);
            }
        }

        private void CopyFolderPath()
        {
            SetUserFolderPath(Path.GetDirectoryName(FullFilePath));
        }


        public string UserFolderPath
        {
            get
            {
                return userFolderPath;
            }
            set
            {
                if (userFolderPath != value)
                {
                    userFolderPath = value ?? throw new ArgumentNullException(nameof(UserFolderPath));
                }
                OnPropertyChanged(nameof(UserFolderPath));
            }
        }

        public bool RecordingInProgress { get; private set; } = false;

        public void OpenFolderInWindowExplorer()
        {
            if (Directory.Exists(UserFolderPath))
            {
                Process.Start("explorer.exe", UserFolderPath);
                return;
            }
        }

        public void OpenFolderInVsCode()
        {
            if (!Directory.Exists(UserFolderPath))
                return;

            Process.Start(new ProcessStartInfo
            {
                FileName = "code",
                Arguments = $"\"{UserFolderPath}\"",
                UseShellExecute = true
            });
        }

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
