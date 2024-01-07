using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;

namespace Курсовая.model
{
    public class DownloadItem : INotifyPropertyChanged
    {
        private string _status;
        private int _progress;
        private string _speed;
        private long _totalBytes;
        private DateTime _timeAdded;

        public string FileName { get; set; }
        public string Url { get; set; }
        public string SavePath { get; set; }
        public long BytesDownloaded { get; set; }

        public DateTime TimeAdded
        {
            get { return _timeAdded; }
            set
            {
                if (_timeAdded != value)
                {
                    _timeAdded = value;
                    OnPropertyChanged(nameof(TimeAdded));
                }
            }
        }

        public long TotalBytes
        {
            get { return _totalBytes; }
            set
            {
                if (_totalBytes != value)
                {
                    _totalBytes = value;
                    OnPropertyChanged(nameof(TotalBytes));
                    OnPropertyChanged(nameof(FormattedTotalBytes));
                }
            }
        }

        public string FormattedTotalBytes => FormatBytes(TotalBytes);

        public string Status
        {
            get { return _status; }
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged(nameof(Status));
                }
            }
        }

        public int Progress
        {
            get { return _progress; }
            set
            {
                if (_progress != value)
                {
                    _progress = value;
                    OnPropertyChanged(nameof(Progress));
                }
            }
        }

        public string Speed
        {
            get { return _speed; }
            set
            {
                if (_speed != value)
                {
                    _speed = value;
                    OnPropertyChanged(nameof(Speed));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public DownloadItem(string url, string savePath)
        {
            Url = url;
            SavePath = savePath;
            FileName = Path.GetFileName(savePath);
            Status = "Downloading";
            BytesDownloaded = 0;
            TotalBytes = 0;
            Speed = "0 KB/s";
            TimeAdded = DateTime.Now;
        }

        public async Task StartDownloadAsync(HttpClient httpClient)
        {
            await Task.Run(async () =>
            {
                bool success = false;

                while (!success)
                {
                    try
                    {
                        using (var response = await httpClient.GetAsync(Url, HttpCompletionOption.ResponseHeadersRead))
                        {
                            TotalBytes = response.Content.Headers.ContentLength.GetValueOrDefault(0);

                            if (TotalBytes > 0)
                            {
                                success = true;

                                using (var stream = await response.Content.ReadAsStreamAsync())
                                using (var fileStream = File.Create(SavePath))
                                {
                                    var buffer = new byte[8192];
                                    int bytesRead;
                                    var stopwatch = Stopwatch.StartNew();
                                    long previousBytesDownloaded = 0;

                                    while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                                    {
                                        while (Status == "Pause") { }

                                        if (Status == "Stop")
                                        {
                                            break;
                                        }

                                        await fileStream.WriteAsync(buffer, 0, bytesRead);

                                        BytesDownloaded += bytesRead;

                                        if (stopwatch.ElapsedMilliseconds >= 1000)
                                        {
                                            long bytesInSecond = BytesDownloaded - previousBytesDownloaded;
                                            Speed = $"{FormatBytes(bytesInSecond)}/s";
                                            previousBytesDownloaded = BytesDownloaded;
                                            stopwatch.Restart();
                                        }

                                        if (TotalBytes > 0)
                                        {
                                            Progress = (int)((double)BytesDownloaded / TotalBytes * 100);
                                        }
                                        else
                                        {
                                            Progress = 0;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch 
                    {
                        MessageBox.Show("Введите корректный URL.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        break;
                    }

                }
                Status = "Complete";
                Speed = "*";
            });
        }

        public void PauseDownload()
        {
            if (Status == "Complete")
            {
                return;
            }
            Status = "Pause";
        }

        public void ResumeDownload(HttpClient httpClient)
        {
            if (Status == "Complete")
            {
                return;
            }

            if (Status == "Canceled")
            {
                BytesDownloaded = 0;
                TotalBytes = 0;
                Speed = "0 KB/s";
                Status = "Downloading";
                Progress = 0;

                if (File.Exists(SavePath))
                {
                    try
                    {
                        File.Delete(SavePath);
                    }
                    catch { }
                }

                StartDownloadAsync(httpClient);
            }
            else
            {
                Status = "Downloading";
            }
        }

        private string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            double number = bytes;

            while (number >= 1024 && counter < suffixes.Length - 1)
            {
                number /= 1024;
                counter++;
            }

            return $"{number:0.##} {suffixes[counter]}";
        }
    }
}
