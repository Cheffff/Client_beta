using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Web;

// Pour plus d'informations sur le modèle d'élément Page vierge, voir la page http://go.microsoft.com/fwlink/?LinkId=234238

namespace PassportLogin.Pages
{
    public sealed partial class DL_card : Page
    {
        public enum NotifyType
        {
            StatusMessage,
            ErrorMessage
        };

        private string downloadFolderPath;

        #region for download
        private List<DownloadOperation> activeDownloads;
        private CancellationTokenSource cts;

        public  DL_card()
        {
            this.InitializeComponent();
            cts = new CancellationTokenSource();
        }

        private async void StartDownload_Click(object sender, RoutedEventArgs e)
        {
            // In this sample, we just use the default priority.
            // For more information about background transfer, please refer to the SDK Background transfer sample:
            // http://code.msdn.microsoft.com/windowsapps/Background-Transfer-Sample-d7833f61
            String ZipFileUrlTextBox = "http://eip.epitech.eu/2018/virtualdeck/Epicture-UWP-master.zip";
            String FileNameField = "UWP.zip";

            //var downloadFile = await BackgroundDownloadAsync(this.ZipFileUrlTextBox.Text, this.FileNameField.Text.Trim());
            var downloadFile = await BackgroundDownloadAsync(ZipFileUrlTextBox, FileNameField);

            //unzip zip file
            if (downloadFile != null)
            {
                StorageFolder downloadFolder = await StorageFolder.GetFolderFromPathAsync(downloadFolderPath);

                StorageFolder unzipFolder =
                    await downloadFolder.CreateFolderAsync(Path.GetFileNameWithoutExtension(downloadFile.Name),
                    CreationCollisionOption.GenerateUniqueName);

                await UnZipFileAsync(downloadFile, unzipFolder);
            }
        }

        async protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            await DiscoverActiveDownloadsAsync();
        }

        // Enumerate the downloads that were going on in the background while the app was closed.
        private async Task DiscoverActiveDownloadsAsync()
        {
            activeDownloads = new List<DownloadOperation>();

            IReadOnlyList<DownloadOperation> downloads = null;
            try
            {
                downloads = await BackgroundDownloader.GetCurrentDownloadsAsync();
            }
            catch (Exception ex)
            {
                if (!IsExceptionHandled("Discovery error", ex))
                {
                    throw;
                }
                return;
            }

            Log($"Loading background downloads: {downloads.Count}");

            if (downloads.Count > 0)
            {
                List<Task> tasks = new List<Task>();
                foreach (DownloadOperation download in downloads)
                {
                    Log(String.Format(System.Globalization.CultureInfo.CurrentCulture, $"Discovered background download: {download.Guid}, Status: {download.Progress.Status}"));

                    // Attach progress and completion handlers.
                    tasks.Add(HandleDownloadAsync(download, false));
                }

                // Don't await HandleDownloadAsync() in the foreach loop since we would attach to the second
                // download only when the first one completed; attach to the third download when the second one
                // completes etc. We want to attach to all downloads immediately.
                // If there are actions that need to be taken once downloads complete, await tasks here, outside
                // the loop. 
                await Task.WhenAll(tasks);
            }
        }

        private async Task<StorageFile> BackgroundDownloadAsync(string uri, string localFileName)
        {
            // The URI is validated by calling Uri.TryCreate() that will return 'false' for strings that are not valid URIs.
            // Note that when enabling the text box users may provide URIs to machines on the intrAnet that require
            // the "Home or Work Networking" capability.
            Uri source;
            if (!Uri.TryCreate(uri.Trim(), UriKind.Absolute, out source))
            {
                NotifyUser("Invalid URI.", NotifyType.ErrorMessage);
                return null;
            }

            FolderPicker folderPicker = new FolderPicker();
            folderPicker.SuggestedStartLocation = PickerLocationId.Downloads;
            folderPicker.FileTypeFilter.Add(".zip");

            StorageFolder destinationFolder = await folderPicker.PickSingleFolderAsync();

            if (destinationFolder != null)
            {
                // Application now has read/write access to all contents in the picked folder 
                // (including other sub-folder contents)
                StorageApplicationPermissions.FutureAccessList.AddOrReplace("PickedFolderToken", destinationFolder);
                Log("Picked folder: {destinationFolder.Name}");

                downloadFolderPath = destinationFolder.Path;
            }
            else
            {
                Log("Operation cancelled.");
                return null;
            }

            String ext = Path.GetExtension(localFileName);
            if (!String.Equals(ext, ".zip", StringComparison.OrdinalIgnoreCase))
            {
                NotifyUser("Invalid file type. Please make sure the file type is zip.", NotifyType.ErrorMessage);
                return null;
            }

            try
            {
                StorageFile localFile = await destinationFolder.CreateFileAsync(localFileName, CreationCollisionOption.GenerateUniqueName);

                BackgroundDownloader downloader = new BackgroundDownloader();
                DownloadOperation download = downloader.CreateDownload(source, localFile);
                download.Priority = BackgroundTransferPriority.Default;

                Log($"Downloading {source.AbsoluteUri} to {destinationFolder.Name} with {download.Priority} priority, {download.Guid}");

                // In this sample, we do not show how to request unconstrained download.
                // For more information about background transfer, please refer to the SDK Background transfer sample:
                // http://code.msdn.microsoft.com/windowsapps/Background-Transfer-Sample-d7833f61

                // Attach progress and completion handlers.
                await HandleDownloadAsync(download, true);

                return localFile;
            }
            catch (Exception ex)
            {
                LogStatus(ex.Message, NotifyType.ErrorMessage);
                return null;
            }
        }

        // Note that this event is invoked on a background thread, so we cannot access the UI directly.
        private void DownloadProgress(DownloadOperation download)
        {
            MarshalLog(String.Format(System.Globalization.CultureInfo.CurrentCulture, "Progress: {0}, Status: {1}", download.Guid,
                download.Progress.Status));

            double percent = 100;
            if (download.Progress.TotalBytesToReceive > 0)
            {
                percent = download.Progress.BytesReceived * 100 / download.Progress.TotalBytesToReceive;
            }

            MarshalLog(String.Format(System.Globalization.CultureInfo.CurrentCulture, " - Transfered bytes: {0} of {1}, {2}%",
                download.Progress.BytesReceived, download.Progress.TotalBytesToReceive, percent));

            if (download.Progress.HasRestarted)
            {
                MarshalLog(" - Download restarted");
            }

            if (download.Progress.HasResponseChanged)
            {
                // We've received new response headers from the server.
                MarshalLog(" - Response updated; Header count: " + download.GetResponseInformation().Headers.Count);

                // If you want to stream the response data this is a good time to start.
                // download.GetResultStreamAt(0);
            }
        }

        private async Task HandleDownloadAsync(DownloadOperation download, bool start)
        {
            try
            {
                LogStatus($"Running: {download.Guid}", NotifyType.StatusMessage);

                // Store the download so we can pause/resume.
                activeDownloads.Add(download);

                Progress<DownloadOperation> progressCallback = new Progress<DownloadOperation>(DownloadProgress);
                if (start)
                {
                    // Start the download and attach a progress handler.
                    await download.StartAsync().AsTask(cts.Token, progressCallback);
                }
                else
                {
                    // The download was already running when the application started, re-attach the progress handler.
                    await download.AttachAsync().AsTask(cts.Token, progressCallback);
                }

                ResponseInformation response = download.GetResponseInformation();

                LogStatus($"Completed: {download.Guid}, Status Code: {response.StatusCode}", NotifyType.StatusMessage);
            }
            catch (TaskCanceledException)
            {
                LogStatus($"Canceled: {download.Guid}", NotifyType.StatusMessage);
            }
            catch (Exception ex)
            {
                if (!IsExceptionHandled("Execution error", ex, download))
                {
                    throw;
                }
            }
            finally
            {
                activeDownloads.Remove(download);
            }
        }

        private void PauseAll_Click(object sender, RoutedEventArgs e)
        {
            Log($"Downloads: {activeDownloads.Count}");

            foreach (DownloadOperation download in activeDownloads)
            {
                if (download.Progress.Status == BackgroundTransferStatus.Running)
                {
                    download.Pause();
                    Log($"Paused: {download.Guid}");
                }
                else
                {
                    Log($"Skipped: {download.Guid}, Status: {download.Progress.Status}");
                }
            }
        }

        private void ResumeAll_Click(object sender, RoutedEventArgs e)
        {
            Log($"Downloads: {activeDownloads.Count}");

            foreach (DownloadOperation download in activeDownloads)
            {
                if (download.Progress.Status == BackgroundTransferStatus.PausedByApplication)
                {
                    download.Resume();
                    Log("Resumed: " + download.Guid);
                }
                else
                {
                    Log($"Skipped: {download.Guid}, Status: {download.Progress.Status}");
                }
            }
        }

        private void CancelAll_Click(object sender, RoutedEventArgs e)
        {
            Log($"Canceling Downloads: {activeDownloads.Count}");

            cts.Cancel();
            cts.Dispose();

            // Re-create the CancellationTokenSource and activeDownloads for future downloads.
            cts = new CancellationTokenSource();
            activeDownloads = new List<DownloadOperation>();
        }

        #endregion

        #region for unzip
        private async Task UnZipFileAsync(StorageFile zipFile, StorageFolder unzipFolder)
        {
            try
            {
                LogStatus($"Unziping file: {zipFile.DisplayName}...", NotifyType.StatusMessage);
                await ZipHelper.UnZipFileAsync(zipFile, unzipFolder);
                LogStatus($"Unzip file '{zipFile.DisplayName}' successfully!", NotifyType.StatusMessage);
            }
            catch (Exception ex)
            {
                LogStatus($"Failed to unzip file ...{ex.Message}", NotifyType.ErrorMessage);
            }
        }

        #endregion

        #region notify func
        private bool IsExceptionHandled(string title, Exception ex, DownloadOperation download = null)
        {
            WebErrorStatus error = BackgroundTransferError.GetStatus(ex.HResult);
            if (error == WebErrorStatus.Unknown)
            {
                return false;
            }

            if (download == null)
            {
                LogStatus($"Error: {title}: {error}", NotifyType.ErrorMessage);
            }
            else
            {
                LogStatus($"Error: {download.Guid} - {title}: {error}", NotifyType.ErrorMessage);
            }

            return true;
        }

        // When operations happen on a background thread we have to marshal UI updates back to the UI thread.
        private void MarshalLog(string value)
        {
            var ignore = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                Log(value);
            });
        }

        private void Log(string message)
        {
            OutputField.Text += message + Environment.NewLine;
        }

        private void LogStatus(string message, NotifyType type)
        {
            NotifyUser(message, type);
            Log(message);
        }

        public void NotifyUser(string strMessage, NotifyType type)
        {
            switch (type)
            {
                // Use the status message style.
                case NotifyType.StatusMessage:
                    statusText.Style = Resources["StatusStyle"] as Style;
                    break;
                // Use the error message style.
                case NotifyType.ErrorMessage:
                    statusText.Style = Resources["ErrorStyle"] as Style;
                    break;
            }

            statusText.Text = strMessage;

            // Collapse the statusText if it has no text to conserve real estate.
            if (statusText.Text != String.Empty)
            {
                statusText.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }
            else
            {
                statusText.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
        }

        #endregion

        async private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri((sender as HyperlinkButton).Tag.ToString()));
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(Jeu));
        }
    }
}
