using Chess.Services;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Chess.View.Window
{
    /// <summary>
    /// Interaction logic for SetRecordFilePath.xaml
    /// </summary>
    public partial class SetRecordFilePath : System.Windows.Window
    {
        public string SelectedFilePath { get; private set; }

        public SetRecordFilePath(string folderPath, string fileName)
        {
            InitializeComponent();
            txtFolderPath.Text = folderPath;
            txtFileName.Text = fileName;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //if (this.DataContext is RecordModeViewModel vm)
            //{
            //    vm.Cleanup();
            //}
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            if (!IsFileNameValid(txtFileName.Text))
            {
                txtMessage.Text = "Invalid file name. Please enter a valid file name without special characters.";
                txtMessage.Visibility = Visibility.Visible;
                txtMessage.Foreground = System.Windows.Media.Brushes.Red;
                txtFileName.Focus();
                return;
            }
            else
            {
                txtMessage.Visibility = Visibility.Collapsed;
                txtMessage.Text = string.Empty;
            }

            SelectedFilePath = Path.Combine(txtFolderPath.Text, txtFileName.Text);
            this.DialogResult = true;
            this.Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void btnSetFolderPath_Click(object sender, RoutedEventArgs e)
        {
            txtFolderPath.Text = GetXmlFolderPath();
        }

        private string GetXmlFolderPath()
        {
            var folderDialog = new OpenFolderDialog
            {
                Title = "Select Folder",
                InitialDirectory = txtFolderPath.Text
            };


            if (folderDialog.ShowDialog() == true)
            {
                return folderDialog.FolderName;
            }
            else
            {
                Debug.WriteLine("No folder selected. Cannot record game history.");
                return string.Empty;
            }
        }
        private void btnGenerateFileName_Click(object sender, RoutedEventArgs e)
        {
            XmlFileService xmlFileService = new();
            txtFileName.Text = xmlFileService.GetFileName();
        }

        private bool IsFileNameValid(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return false;
            }

            var t = Path.GetInvalidFileNameChars().ToList();

            if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                return false;
            }

            return true;
        }
    }
}
