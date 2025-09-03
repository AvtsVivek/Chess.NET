using Chess.Model.Command;
using Chess.Model.Data;
using Chess.Model.Game;
using Chess.Services;
using Chess.ViewModel.Command;
using Chess.ViewModel.Game;
using Chess.ViewModel.Messages;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Chess.ViewModel.StatusAndMode
{
    public partial class RecordReviewModeVM : ObservableObject
    {
        private readonly IWindowService windowService;
        private readonly XmlFileService xmlFileService = new XmlFileService();

        private string fullFilePath;
        [ObservableProperty]
        private string userFolderPath;
        private readonly GenericCommand setFullFilePathCommand;
        private readonly GenericCommand openFolderInWindowsExplorerCommand;
        private readonly GenericCommand openFolderInVsCodeCommand;
        private readonly GenericCommand setParentFolderCommand;
        private readonly GenericCommand copyFolderPathCommand;

        public RecordReviewModeVM(IWindowService windowService)
        {
            this.windowService = windowService ?? throw new ArgumentNullException(nameof(windowService));

            SetFullFilePath();

            setFullFilePathCommand = new GenericCommand(() => true, SetUserFolderPath);
            openFolderInWindowsExplorerCommand = new GenericCommand(() => true, OpenFolderInWindowExplorer);
            openFolderInVsCodeCommand = new GenericCommand(() => true, OpenFolderInVsCode);
            setParentFolderCommand = new GenericCommand(() => true, SetParentFolder);
            copyFolderPathCommand = new GenericCommand(() => true, CopyFolderPath);
        }

        public void SetFullFilePath()
        {
            var initialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            if (!string.IsNullOrWhiteSpace(ChessAppSettings.Default.XmlFolderPath)
                && Directory.Exists(ChessAppSettings.Default.XmlFolderPath))
            {
                initialDirectory = ChessAppSettings.Default.XmlFolderPath;
            }

            var fileName = XmlFileService.GetFileName();

            FullFilePath = Path.Combine(initialDirectory, fileName);
        }

        public GenericCommand SetFullFilePathCommand => setFullFilePathCommand;

        public GenericCommand OpenFolderInWindowsExplorerCommand => openFolderInWindowsExplorerCommand;

        public GenericCommand OpenFolderInVsCodeCommand => openFolderInVsCodeCommand;

        public GenericCommand SetParentFolderCommand => setParentFolderCommand;

        public GenericCommand CopyFolderPathCommand => copyFolderPathCommand;

        private AppMode currentAppMode;

        public AppMode CurrentAppMode
        {
            get => currentAppMode;
            set
            {
                var previousAppMode = currentAppMode;
                SetProperty(ref currentAppMode, value);
                AppModeChanged(previousAppMode);
            }
        }

        [ObservableProperty]
        private string firstButtonName;

        public string FullFilePath
        {
            get => fullFilePath;
            set
            {
                SetProperty(ref fullFilePath, value);
                CopyFolderPath();
                OnPropertyChanged(nameof(BrowseButtonsEnabled));
            }
        }

        public bool BrowseButtonsEnabled
        {
            get
            {
                return !string.IsNullOrWhiteSpace(FullFilePath);
            }
        }

        private void SetUserFolderPath(string path)
        {
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
                    "Click Yes to continue to use the same file for recording." + Environment.NewLine +
                    "Recording will start from the next move." + Environment.NewLine +
                    "Click No to set a new file for recording." + Environment.NewLine,
                    "Recording in Progress",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information
                );

                if (result == MessageBoxResult.Yes)
                {
                    return;
                }

                if (result == MessageBoxResult.No)
                {
                    SetFullFilePath();

                    RecordingInProgress = false;

                    windowService.ShowMessageBox(
                        $"Recording will now be done to this new path {FullFilePath}"
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
                MessageBox.Show("Full file path must be set before writing to XML file." + Environment.NewLine + 
                    "Cannot continue.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Task.Run(() =>
            {
                xmlFileService.WriteGameToXmlFile(chessGame, FullFilePath);
            }).Wait();
            RecordingInProgress = true;
        }

        private void SetUserFolderPath()
        {
            if(this.CurrentAppMode == AppMode.Record)
            {
                ResetRecordingState();
            }

            if(this.CurrentAppMode == AppMode.Review)
            {
                OpenFileDialog openFileDialog = new();
                openFileDialog.Filter = "xml files (*.xml)|*.xml";

                var initialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                if (!string.IsNullOrWhiteSpace(ChessAppSettings.Default.ReviewXmlLoadFolderPath)
                    && Directory.Exists(ChessAppSettings.Default.ReviewXmlLoadFolderPath))
                {
                    initialDirectory = ChessAppSettings.Default.ReviewXmlLoadFolderPath;
                }

                openFileDialog.InitialDirectory = initialDirectory;

                bool? result = openFileDialog.ShowDialog();

                if (result == true)
                {
                    SetFullFilePathForReview(openFileDialog.FileName);
                }
            }
        }

        private ChessGame LoadFromXmlFile()
        {
            if (string.IsNullOrWhiteSpace(FullFilePath))
            {
                throw new InvalidOperationException("Full file path must be set before loading from XML file.");
            }

            if(!Path.Exists(FullFilePath))
            {
                throw new FileNotFoundException("The specified file was not found.", FullFilePath);
            }

            ChessGame updatedGame = xmlFileService.GetPieceMoveCommandsFromXmlFile(FullFilePath);

            return updatedGame;
        }

        private void AppModeChanged(AppMode previousAppMode)
        {
            if (CurrentAppMode == AppMode.Record)
            {
                FirstButtonName = "Set File";
            }

            if (CurrentAppMode == AppMode.Review)
            {
                FirstButtonName = "Load File";
            }

            if (this.RecordingInProgress && previousAppMode == AppMode.Record)
            {
                if (CurrentAppMode == AppMode.Play)
                {
                    PublishReviewMessage(false);
                }
                else
                {
                    PublishReviewMessage(true);
                }
                return;
            }

            if (CurrentAppMode == AppMode.Record && previousAppMode == AppMode.Play)
            {
                PublishReviewMessage(false);
                return;
            }

            if (CurrentAppMode == AppMode.Record && previousAppMode == AppMode.Review)
            {
                PublishReviewMessage(false);
                return;
            }

            if (CurrentAppMode == AppMode.Play && previousAppMode == AppMode.Review)
            {
                PublishReviewMessage(false);
                return;
            }

            if (CurrentAppMode == AppMode.Review && previousAppMode == AppMode.Record && !this.RecordingInProgress)
            {
                LoadFileForReviewFromSettings();
            }

            if (CurrentAppMode == AppMode.Review && previousAppMode == AppMode.Play)
            {
                LoadFileForReviewFromSettings();
            }
        }

        private void LoadFileForReviewFromSettings()
        {
            if (!string.IsNullOrWhiteSpace(ChessAppSettings.Default.ReviewXmlFilePath) 
                && File.Exists(ChessAppSettings.Default.ReviewXmlFilePath))
            {
                SetFullFilePathForReview(ChessAppSettings.Default.ReviewXmlFilePath);
            }
            else
            {
                this.FullFilePath = string.Empty;
                SaveReviewFileAndFolderPathToSettings();
                PublishReviewMessage(false);
            }
        }

        private void SaveReviewFileAndFolderPathToSettings()
        {
            if (!string.IsNullOrWhiteSpace(FullFilePath))
            {
                var dir = Path.GetDirectoryName(FullFilePath);

                if(!string.IsNullOrWhiteSpace(dir))
                    ChessAppSettings.Default.ReviewXmlLoadFolderPath = dir;

            }
            ChessAppSettings.Default.ReviewXmlFilePath = FullFilePath;
            ChessAppSettings.Default.Save();
        }

        private void SetFullFilePathForReview(string fullFilePathForReview)
        {
            FullFilePath = fullFilePathForReview;
            SaveReviewFileAndFolderPathToSettings();
            var game = LoadFromXmlFile();
            var message = new MessageToChessGameVM(game);
            WeakReferenceMessenger.Default.Send(message);
            PublishReviewMessage(true);
        }

        private void PublishReviewMessage(bool bStartReview)
        {
            var reviewMessage = new MessageFromRecordReviewModeVMToReviewModeHeaderDisplayVM(bStartReview);
            WeakReferenceMessenger.Default.Send(reviewMessage);
        }

        public void ViewLoaded()
        {
            if (CurrentAppMode == AppMode.Review)
            {

            }

            if (CurrentAppMode == AppMode.Record)
            {

            }
        }

        public void ViewUnloaded()
        {
            if (CurrentAppMode == AppMode.Review)
            {

            }

            if (CurrentAppMode == AppMode.Record)
            {

            }
        }
    }
}