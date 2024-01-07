using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Курсовая.model;

namespace Курсовая
{
    public partial class MainWindow : Window
    {
        private readonly HttpClient httpClient;
        private readonly List<DownloadItem> downloadTasks;
        private const string StateFilePath = "downloadState.json";
        private readonly SemaphoreSlim downloadSemaphore = new SemaphoreSlim(5, 5); 


        public MainWindow()
        {
            InitializeComponent();

            httpClient = new HttpClient();
            downloadTasks = new List<DownloadItem>();

            LoadDownloadState();
        }

        private async void AddDownloadButton_Click(object sender, RoutedEventArgs e)
        {
            var url = UrlTextBox.Text;
            if (string.IsNullOrWhiteSpace(url))
            {
                MessageBox.Show("Введите URL.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var saveFileDialog = new SaveFileDialog
            {
                AddExtension = true,
                DefaultExt = System.IO.Path.GetExtension(url),
                Filter = "All Files (*.*)|*.*",
                FileName = Path.GetFileName(url)
            };

            if (downloadSemaphore.Wait(0))
            {
                try
                {
                    if (saveFileDialog.ShowDialog() == true)
                    {
                        var downloadTask = new DownloadItem(url, saveFileDialog.FileName);
                        downloadTasks.Add(downloadTask);

                        DownloadList.Items.Add(downloadTask);

                        await downloadTask.StartDownloadAsync(httpClient);
                    }
                }
                finally
                {
                    downloadSemaphore.Release();
                }
            }
            else
            {
                MessageBox.Show("Вы не можете скачивать более 5 файлов.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }




        private void Window_Closing(object sender, CancelEventArgs e)
        {
            foreach (var downloadItem in downloadTasks)
            {
                if (downloadItem.Status == "Downloading" || downloadItem.Status == "Pause")
                {
                    downloadItem.Status = "Canceled";
                }

            }

            SaveDownloadState();
        }
        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (DownloadList.SelectedItem is DownloadItem selectedDownload && selectedDownload.Status != "Pause" && selectedDownload.Status != "Canceled")
            {
                selectedDownload.PauseDownload();
                downloadSemaphore.Release();
            }
        }

        private void ResumeButton_Click(object sender, RoutedEventArgs e)
        {
            if (DownloadList.SelectedItem is DownloadItem selectedDownload)
            {
                if (selectedDownload.Status != "Downloading" && selectedDownload.Status != "Complete" && downloadSemaphore.Wait(0))
                {
                    selectedDownload.ResumeDownload(httpClient);
                }
                else
                {
                    MessageBox.Show("Вы не можете скачивать более 5 файлов.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                   
                }
                
                
            }
        }

        private void SaveDownloadState()
        {
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(downloadTasks, Newtonsoft.Json.Formatting.Indented);

            File.WriteAllText(StateFilePath, json);
        }

        private void LoadDownloadState()
        {
            if (File.Exists(StateFilePath))
            {
                string json = File.ReadAllText(StateFilePath);

                downloadTasks.Clear();
                downloadTasks.AddRange(Newtonsoft.Json.JsonConvert.DeserializeObject<List<DownloadItem>>(json));

                foreach (var downloadItem in downloadTasks)
                {
                    DownloadList.Items.Add(downloadItem);
                }
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (DownloadList.SelectedItem is DownloadItem selectedDownload)
            {
                if (selectedDownload.Status == "Downloading")
                {
                    downloadSemaphore.Release();
                }
                
                downloadTasks.Remove(selectedDownload);
                DownloadList.Items.Remove(selectedDownload);
                selectedDownload.Status = "Stop";
                
                SaveDownloadState();
            }
        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            if (DownloadList.SelectedItem is DownloadItem selectedDownload)
            {
                string downloadFolder = Path.GetDirectoryName(selectedDownload.SavePath);

                if (Directory.Exists(downloadFolder))
                {
                    string filePathToSelect = $@"/select,""{selectedDownload.SavePath}""";
                    Process.Start("explorer.exe", filePathToSelect);
                }
                else
                {
                    MessageBox.Show("Путь не найден.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Выберите загрузку из списка.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void SortButton_Click(object sender, RoutedEventArgs e)
        {
            if (sort.Content != "Сортировка: прогресс")
            {
                downloadTasks.Sort((item1, item2) => item2.Progress.CompareTo(item1.Progress));
                sort.Content = "Сортировка: прогресс";
            }
            else
            {
                downloadTasks.Sort((item1, item2) => item1.TimeAdded.CompareTo(item2.TimeAdded));
                sort.Content = "Сортировка: время";
            }


            DownloadList.Items.Clear();
            foreach (var downloadItem in downloadTasks)
            {
                DownloadList.Items.Add(downloadItem);
            }
        }
    }

    
}
