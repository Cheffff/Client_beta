using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Threading;
using Windows.Networking.BackgroundTransfer;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Windows.Storage;


// Pour plus d'informations sur le modèle d'élément Page vierge, voir la page http://go.microsoft.com/fwlink/?LinkId=234238

namespace PassportLogin.Pages
{
    /// <summary>
    /// Une page vide peut être utilisée seule ou constituer une page de destination au sein d'un frame.
    /// </summary>
    public sealed partial class DL_chess : Page
    {
        DownloadOperation downloadOperation;
        CancellationTokenSource cancellationToken;
        BackgroundDownloader backgroundDownloader = new BackgroundDownloader();

        string fileName = "UWP.zip";
        string urlName = "http://eip.epitech.eu/2018/virtualdeck/Epicture-UWP-master.zip";

        public DL_chess()
        {
            this.InitializeComponent();
        }

        /// Download
        /// PART


        private async void btnDownload_Click(object sender, RoutedEventArgs e)
        {
            FolderPicker folderPicker = new FolderPicker();
            folderPicker.SuggestedStartLocation = PickerLocationId.Downloads;
            folderPicker.ViewMode = PickerViewMode.Thumbnail;
            folderPicker.FileTypeFilter.Add("*");
            StorageFolder folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                StorageFile file = await folder.CreateFileAsync(fileName, CreationCollisionOption.GenerateUniqueName);
                downloadOperation = backgroundDownloader.CreateDownload(new Uri(urlName), file);
                Progress<DownloadOperation> progress = new Progress<DownloadOperation>(progressChanged);
                cancellationToken = new CancellationTokenSource();
                btnDownload.IsEnabled = false;
                btnCancel.IsEnabled = true;
                btnPauseResume.IsEnabled = true;

                try
                {
                    txtStatus.Text = "Initializing...";
                    await downloadOperation.StartAsync().AsTask(cancellationToken.Token, progress);
                }
                catch (TaskCanceledException)
                {
                    txtStatus.Text = "Download cancelled";
                    downloadOperation.ResultFile.DeleteAsync();
                    btnPauseResume.Content = "Resume";
                    btnCancel.IsEnabled = false;
                    btnPauseResume.IsEnabled = false;
                    btnDownload.IsEnabled = true;
                    downloadOperation = null;

                }
            }
        }

        private void btnPauseResume_Click(object sender, RoutedEventArgs e)
        {
            if (btnPauseResume.Content.ToString().ToLower().Equals("pause"))
            {
                try
                {
                    downloadOperation.Pause();
                }
                catch (InvalidOperationException)
                {

                }
            }
            else
            {
                try
                {
                    downloadOperation.Resume();
                }
                catch (InvalidOperationException)
                { }
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            cancellationToken.Cancel();
            cancellationToken.Dispose();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            IReadOnlyList<DownloadOperation> downloads = await BackgroundDownloader.GetCurrentDownloadsAsync();
            if (downloads.Count > 0)
            {
                downloadOperation = downloads.First();
                cancellationToken = new CancellationTokenSource();
                Progress<DownloadOperation> progress = new Progress<DownloadOperation>(progressChanged);
                btnDownload.IsEnabled = false;
                btnCancel.IsEnabled = true;
                btnPauseResume.IsEnabled = true;
                try
                {
                    txtStatus.Text = "Initializing...";
                    await downloadOperation.AttachAsync().AsTask(cancellationToken.Token, progress);
                }
                catch (TaskCanceledException)
                {
                    txtStatus.Text = "Download cancelled";
                    downloadOperation.ResultFile.DeleteAsync();
                    btnPauseResume.Content = "Resume";
                    btnCancel.IsEnabled = false;
                    btnPauseResume.IsEnabled = false;
                    btnDownload.IsEnabled = true;
                    downloadOperation = null;

                }
            }
        }

        private void progressChanged(DownloadOperation downloadOperation)
        {
            int progress = (int)(100 * ((double)downloadOperation.Progress.BytesReceived / (double)downloadOperation.Progress.TotalBytesToReceive));
            txtProgress.Text = String.Format("{0} of {1}kb. downloaded - %{2} complete.",
                downloadOperation.Progress.BytesReceived / 1024,
                downloadOperation.Progress.TotalBytesToReceive / 1024, progress);

            ProgressBarDownload.Value = progress;
            switch (downloadOperation.Progress.Status)
            {
                case BackgroundTransferStatus.Running:
                    {
                        txtStatus.Text = "Downloading...";
                        btnPauseResume.Content = "Pause";
                        break;
                    }
                case BackgroundTransferStatus.PausedByApplication:
                    {
                        txtStatus.Text = "Downloading paused";
                        btnPauseResume.Content = "Resume";
                        break;
                    }
                case BackgroundTransferStatus.PausedCostedNetwork:
                    {
                        txtStatus.Text = "Downloading paused because of metered connection";
                        btnPauseResume.Content = "Resume";
                        break;
                    }
                case BackgroundTransferStatus.PausedNoNetwork:
                    {
                        txtStatus.Text = "No network detected. Please check your internet connection";

                        break;
                    }
                case BackgroundTransferStatus.Error:
                    {
                        txtStatus.Text = "An error occured while downloading.";

                        break;
                    }
            }
            if (progress >= 100)
            {
                txtStatus.Text = "Download Completed";
                btnCancel.IsEnabled = false;
                btnPauseResume.IsEnabled = false;
                btnDownload.IsEnabled = true;
                downloadOperation = null;
            }

        }

        //Donload END

        private void button_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(Jeu));
        }
    }
}
