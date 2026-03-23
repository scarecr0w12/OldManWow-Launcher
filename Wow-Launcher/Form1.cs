using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Wow_Launcher
{
    public partial class Form1 : Form
    {
        private const string ServerName = "Old Man Warcraft";
        private const string ManifestUrl = "https://updates.oldmanwarcraft.com/updates/manifest.xml";
        private const string LauncherGitHubRepositorySettingName = "LauncherGitHubRepository";
        private const string LauncherGitHubApiBaseUrlSettingName = "LauncherGitHubApiBaseUrl";
        private const string DefaultLauncherGitHubRepository = "scarecr0w12/OldManWow-Launcher";
        private const string DefaultGitHubApiBaseUrl = "https://api.github.com";
        private const string LauncherAssetName = "Wow-Launcher.exe";
        private const string RealmStatusApiUrl = "http://140.150.202.236:8081/api/server";
        private const string RealmStatusApiKey = "sadlkjflasdkjg438gh";
        private const int MaxConcurrentDownloads = 4;
        private const string SourceUpdateServer = "Old Man Warcraft update server";
        private const string SourceLauncherUpdateService = "Launcher self-update service";
        private const string SourceLauncherConfiguration = "Launcher configuration";
        private const string SourceNetwork = "Network or connectivity";
        private const string SourceLocalClient = "Local client files";

        private readonly HttpClient httpClient = new HttpClient();
        private UpdateManifest currentManifest;
        private bool launcherUpdateCheckStarted;
        private List<PendingUpdateFile> pendingFiles = new List<PendingUpdateFile>();
        private string skippedLauncherVersion;
        private string currentLanguageCode;
        private bool suppressClientPathPersistence;
        private bool suppressLanguageSelectionPersistence;
        private string currentStatusText;
        private StatusTone currentStatusTone = StatusTone.Neutral;
        private RealmStatusState currentRealmStatusState = RealmStatusState.Checking;
        private string currentOnlinePlayerCountText = "-";
        private string currentRemoteVersionText = "-";
        private string currentNewsMessage;
        private ReleaseNotesFeed currentReleaseNotes;

        private enum StatusTone
        {
            Neutral,
            Success,
            Warning,
            Error
        }

        private enum RealmStatusState
        {
            Unknown,
            Checking,
            Online,
            Offline
        }

        private sealed class LauncherOperationException : Exception
        {
            public LauncherOperationException(string likelySource, string step, string detail, Uri requestUri = null, HttpStatusCode? statusCode = null, Exception innerException = null)
                : base(detail, innerException)
            {
                LikelySource = string.IsNullOrWhiteSpace(likelySource) ? "Unknown" : likelySource;
                Step = string.IsNullOrWhiteSpace(step) ? "Unknown" : step;
                RequestUri = requestUri;
                StatusCode = statusCode;
            }

            public string LikelySource { get; private set; }

            public string Step { get; private set; }

            public Uri RequestUri { get; private set; }

            public HttpStatusCode? StatusCode { get; private set; }
        }

        public Form1()
        {
            InitializeComponent();
            InitializeLanguageSelection();
            ConfigureHttpClient();
            ServicePointManager.DefaultConnectionLimit = Math.Max(ServicePointManager.DefaultConnectionLimit, MaxConcurrentDownloads + 2);
            DoubleBuffered = true;
            Shown += Form1_Shown;
            ApplyBranding();
            InitializeVisualStyle();
            btnClearCache.Enabled = false;
            btnUpdateClient.Enabled = false;
            btnLaunchGame.Enabled = false;
            progressUpdate.Minimum = 0;
            progressUpdate.Maximum = 100;
            SetStatusText(L("Ready"), StatusTone.Success);
            SetRealmStatusIndicator(RealmStatusState.Checking);
            SetOnlinePlayerCountText("-");
            SetNewsText(null);
            InitializeClientPath();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            httpClient.Dispose();
            base.OnFormClosed(e);
        }

        private async void Form1_Shown(object sender, EventArgs e)
        {
            if (launcherUpdateCheckStarted)
            {
                return;
            }

            launcherUpdateCheckStarted = true;
            await RefreshRealmStatusAsync();
            await CheckForLauncherUpdatesAsync(false);
        }

        private async void btnCheckUpdates_Click(object sender, EventArgs e)
        {
            await RefreshRealmStatusAsync();
            await CheckForUpdatesAsync();
        }

        private async void btnUpdateClient_Click(object sender, EventArgs e)
        {
            await UpdateClientAsync();
        }

        private void btnBrowseClient_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = L("SelectClientFolderDescription");
                string selectedClientPath = GetSelectedClientPath();
                dialog.SelectedPath = Directory.Exists(selectedClientPath) ? selectedClientPath : string.Empty;

                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    txtClientPath.Text = dialog.SelectedPath;
                }
            }
        }

        private void btnLaunchGame_Click(object sender, EventArgs e)
        {
            try
            {
                var launchPath = TryGetLaunchFilePath();
                if (string.IsNullOrEmpty(launchPath))
                {
                    MessageBox.Show(this, L("LaunchFileNotFound"), L("LaunchFailedTitle"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                Process.Start(launchPath);
                AppendLog("Launched " + Path.GetFileName(launchPath) + ".");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, L("LaunchFailedTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnClearCache_Click(object sender, EventArgs e)
        {
            string cachePath;
            if (!TryGetCacheDirectoryPath(out cachePath))
            {
                MessageBox.Show(this, L("MissingClientFolderMessage"), L("MissingClientFolderTitle"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!Directory.Exists(cachePath))
            {
                MessageBox.Show(this, L("CacheNotFoundMessage"), L("CacheNotFoundTitle"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                btnClearCache.Enabled = false;
                return;
            }

            if (MessageBox.Show(this, L("ClearCacheConfirm"), L("ClearCacheTitle"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }

            try
            {
                Directory.Delete(cachePath, true);
                ResetLauncherState(false);
                SetStatusText(L("CacheCleared"), StatusTone.Success);
                btnLaunchGame.Enabled = CanLaunchGame();
                btnClearCache.Enabled = CanClearCache();
                AppendLog("Deleted Cache folder.");
            }
            catch (Exception ex)
            {
                SetStatusText(L("CacheClearFailed"), StatusTone.Error);
                AppendLog("Cache clear failed: " + ex.Message);
                MessageBox.Show(this, ex.Message, L("CacheClearTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void txtClientPath_TextChanged(object sender, EventArgs e)
        {
            if (suppressClientPathPersistence)
            {
                return;
            }

            string normalizedClientPath = NormalizeClientPath(txtClientPath.Text);
            if (!string.Equals(txtClientPath.Text, normalizedClientPath, StringComparison.Ordinal))
            {
                SetClientPathText(normalizedClientPath);
                return;
            }

            if (string.IsNullOrWhiteSpace(normalizedClientPath))
            {
                SaveClientPathPreference(string.Empty);
            }
            else if (Directory.Exists(normalizedClientPath))
            {
                SaveClientPathPreference(normalizedClientPath);
            }

            ResetLauncherState(false);
            btnClearCache.Enabled = CanClearCache();
            btnLaunchGame.Enabled = CanLaunchGame();
        }

        private void cmbLanguage_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (suppressLanguageSelectionPersistence)
            {
                return;
            }

            var selectedLanguageCode = cmbLanguage.SelectedValue as string;
            if (!string.IsNullOrWhiteSpace(selectedLanguageCode) && !string.Equals(currentLanguageCode, selectedLanguageCode, StringComparison.OrdinalIgnoreCase))
            {
                ApplyLanguage(selectedLanguageCode, true);
            }
        }

        private string L(string key)
        {
            return AppLocalization.Get(key, currentLanguageCode);
        }

        private string LF(string key, params object[] args)
        {
            return AppLocalization.Format(key, currentLanguageCode, args);
        }

        private string GetDefaultNewsText()
        {
            return LF("DefaultNewsTextFormat", ServerName);
        }

        private void InitializeLanguageSelection()
        {
            suppressLanguageSelectionPersistence = true;

            try
            {
                cmbLanguage.DisplayMember = "DisplayName";
                cmbLanguage.ValueMember = "Code";
                cmbLanguage.DataSource = AppLocalization.GetSupportedLanguages().ToList();
            }
            finally
            {
                suppressLanguageSelectionPersistence = false;
            }

            ApplyLanguage(AppLocalization.ResolveLanguageCode(Properties.Settings.Default.LanguageCode), false);
        }

        private void ApplyLanguage(string languageCode, bool persistPreference)
        {
            currentLanguageCode = AppLocalization.ResolveLanguageCode(languageCode);
            AppLocalization.ApplyCulture(currentLanguageCode);

            suppressLanguageSelectionPersistence = true;

            try
            {
                cmbLanguage.SelectedValue = currentLanguageCode;
            }
            finally
            {
                suppressLanguageSelectionPersistence = false;
            }

            ApplyLocalizedText();

            if (persistPreference)
            {
                SaveLanguagePreference(currentLanguageCode);
            }
        }

        private void SaveLanguagePreference(string languageCode)
        {
            if (string.Equals(Properties.Settings.Default.LanguageCode, languageCode, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            try
            {
                Properties.Settings.Default.LanguageCode = languageCode;
                Properties.Settings.Default.Save();
            }
            catch (Exception ex)
            {
                AppendLog("Language preference could not be saved: " + ex.Message);
            }
        }

        private void ApplyLocalizedText()
        {
            ApplyBranding();
            lblLanguage.Text = L("LanguageLabel");
            lblClientPath.Text = L("ClientPath");
            btnBrowseClient.Text = L("Browse");
            btnCheckUpdates.Text = L("CheckUpdates");
            btnUpdateClient.Text = L("UpdateClient");
            btnLaunchGame.Text = L("EnterWorld");
            btnClearCache.Text = L("ClearCache");
            lblOnlinePlayerCount.Text = L("OnlinePlayersLabel");
            lblFilesToUpdate.Text = L("PendingUpdates");
            lblLog.Text = L("ActivityLog");
            lblStatus.Text = L("StatusLabel");

            ApplyCurrentStatusDisplay();
            ApplyCurrentRealmStatusDisplay();
            ApplyCurrentOnlinePlayerCountDisplay();
            ApplyCurrentRemoteVersionDisplay();

            if (currentReleaseNotes != null)
            {
                ShowReleaseNotes(currentReleaseNotes);
            }
            else
            {
                SetNewsText(currentNewsMessage);
            }
        }

        private void InitializeClientPath()
        {
            string savedClientPath = NormalizeClientPath(Properties.Settings.Default.ClientPath);
            if (Directory.Exists(savedClientPath))
            {
                SetClientPathText(savedClientPath);
                return;
            }

            if (!string.IsNullOrWhiteSpace(savedClientPath))
            {
                SaveClientPathPreference(string.Empty);
                AppendLog("Saved client path was not found and has been cleared.");
            }

            string detectedClientPath;
            if (TryDetectClientPath(out detectedClientPath))
            {
                SetClientPathText(detectedClientPath);
                SaveClientPathPreference(detectedClientPath);
                AppendLog("Detected World of Warcraft client folder next to the launcher.");
            }
        }

        private string GetSelectedClientPath()
        {
            return NormalizeClientPath(txtClientPath.Text);
        }

        private void SetClientPathText(string clientPath)
        {
            suppressClientPathPersistence = true;

            try
            {
                txtClientPath.Text = clientPath;
                txtClientPath.SelectionStart = txtClientPath.TextLength;
            }
            finally
            {
                suppressClientPathPersistence = false;
            }

            ResetLauncherState(false);
            btnClearCache.Enabled = CanClearCache();
            btnLaunchGame.Enabled = CanLaunchGame();
        }

        private void SaveClientPathPreference(string clientPath)
        {
            string normalizedClientPath = NormalizeClientPath(clientPath);
            string currentSavedPath = NormalizeClientPath(Properties.Settings.Default.ClientPath);
            if (string.Equals(currentSavedPath, normalizedClientPath, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            try
            {
                Properties.Settings.Default.ClientPath = normalizedClientPath;
                Properties.Settings.Default.Save();
            }
            catch (Exception ex)
            {
                AppendLog("Client path could not be saved: " + ex.Message);
            }
        }

        private static string NormalizeClientPath(string clientPath)
        {
            string normalizedPath = (clientPath ?? string.Empty).Trim().Trim('"');
            if (string.IsNullOrWhiteSpace(normalizedPath))
            {
                return string.Empty;
            }

            if (File.Exists(normalizedPath) && string.Equals(Path.GetFileName(normalizedPath), "Wow.exe", StringComparison.OrdinalIgnoreCase))
            {
                normalizedPath = Path.GetDirectoryName(normalizedPath) ?? string.Empty;
            }

            return normalizedPath;
        }

        private static bool TryDetectClientPath(out string clientPath)
        {
            string startupPath = NormalizeClientPath(Application.StartupPath);
            if (Directory.Exists(startupPath) && File.Exists(Path.Combine(startupPath, "Wow.exe")))
            {
                clientPath = startupPath;
                return true;
            }

            clientPath = null;
            return false;
        }

        private async Task CheckForUpdatesAsync()
        {
            string clientPath;
            Uri manifestUri;

            if (!TryGetInputs(out clientPath, out manifestUri))
            {
                return;
            }

            SetBusyState(true, true);
            SetStatusText(L("CheckingForUpdates"), StatusTone.Warning);
            SetRemoteVersionText("-");
            lstFilesToUpdate.Items.Clear();
            SetNewsText(L("GatheringRealmNews"));
            AppendLog("Loading manifest from " + manifestUri + ".");

            string currentStep = "downloading the update manifest";
            string currentSource = SourceUpdateServer;
            Uri currentRequestUri = manifestUri;

            try
            {
                currentManifest = await LoadManifestAsync(manifestUri);

                try
                {
                    currentStep = "checking for launcher updates";
                    currentSource = SourceLauncherUpdateService;
                    currentRequestUri = null;

                    if (await TryHandleLauncherUpdateAsync(true))
                    {
                        return;
                    }
                }
                catch (Exception ex)
                {
                    AppendDetailedFailureLog("Launcher update check skipped.", ex, currentSource, currentStep, currentRequestUri);
                }

                currentStep = "loading realm news";
                currentSource = SourceUpdateServer;
                currentRequestUri = ResolveNewsUri(currentManifest, manifestUri);
                await LoadNewsAsync(currentManifest, manifestUri);

                currentStep = "comparing local files to the manifest";
                currentSource = SourceLocalClient;
                currentRequestUri = null;
                pendingFiles = await BuildPendingFileListAsync(clientPath, manifestUri, currentManifest);
                SetRemoteVersionText(currentManifest.Version);
                UpdatePendingFilesList();

                if (pendingFiles.Count == 0)
                {
                    SetStatusText(L("ClientUpToDate"), StatusTone.Success);
                    AppendLog("No updates are required.");
                }
                else
                {
                    SetStatusText(LF("FilesNeedUpdateFormat", pendingFiles.Count), StatusTone.Warning);
                    AppendLog("Found " + pendingFiles.Count + " file(s) to update.");
                }
            }
            catch (Exception ex)
            {
                currentManifest = null;
                pendingFiles = new List<PendingUpdateFile>();
                UpdatePendingFilesList();
                SetStatusText(L("UpdateCheckFailed"), StatusTone.Error);
                SetNewsText(L("UpdateCheckFailedNews"));
                AppendDetailedFailureLog("Update check failed.", ex, currentSource, currentStep, currentRequestUri);
                MessageBox.Show(this, BuildDetailedErrorMessage(ex, currentSource, currentStep, currentRequestUri), L("UpdateCheckFailed"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetBusyState(false, false);
            }
        }

        private async Task UpdateClientAsync()
        {
            string clientPath;
            Uri manifestUri;

            if (!TryGetInputs(out clientPath, out manifestUri))
            {
                return;
            }

            if (currentManifest == null || pendingFiles.Count == 0)
            {
                await CheckForUpdatesAsync();
                if (currentManifest == null || pendingFiles.Count == 0)
                {
                    return;
                }
            }

            SetBusyState(true, false);
            progressUpdate.Value = 0;
            var filesToDownload = pendingFiles.ToList();
            int concurrentDownloads = Math.Min(MaxConcurrentDownloads, Math.Max(1, filesToDownload.Count));
            SetStatusText(LF("DownloadingUpdatesFormat", concurrentDownloads), StatusTone.Warning);

            try
            {
                long totalBytes = filesToDownload.Where(file => file.Entry.Size > 0).Sum(file => file.Entry.Size);
                long downloadedBytes = 0;
                int completedFiles = 0;

                using (var downloadThrottle = new SemaphoreSlim(concurrentDownloads))
                {
                    var downloadTasks = filesToDownload.Select(async pendingFile =>
                    {
                        await downloadThrottle.WaitAsync();

                        try
                        {
                            AppendLog("Downloading " + pendingFile.Entry.FilePath + ".");
                            await DownloadFileAsync(
                                pendingFile,
                                bytesRead =>
                                {
                                    long currentBytes = Interlocked.Add(ref downloadedBytes, bytesRead);
                                    UpdateProgress(totalBytes, currentBytes, Volatile.Read(ref completedFiles), filesToDownload.Count);
                                });

                            int finishedFiles = Interlocked.Increment(ref completedFiles);
                            AppendLog("Completed " + pendingFile.Entry.FilePath + ".");
                            SetStatusText(LF("DownloadedFilesFormat", finishedFiles, filesToDownload.Count), StatusTone.Warning);
                            UpdateProgress(totalBytes, Interlocked.Read(ref downloadedBytes), finishedFiles, filesToDownload.Count);
                        }
                        finally
                        {
                            downloadThrottle.Release();
                        }
                    }).ToList();

                    await Task.WhenAll(downloadTasks);
                }

                progressUpdate.Value = 100;
                pendingFiles = await BuildPendingFileListAsync(clientPath, manifestUri, currentManifest);
                UpdatePendingFilesList();
                SetStatusText(pendingFiles.Count == 0 ? L("UpdateComplete") : L("UpdateFinishedWithRemaining"), pendingFiles.Count == 0 ? StatusTone.Success : StatusTone.Warning);
                AppendLog(pendingFiles.Count == 0 ? "All downloads completed successfully." : "Downloads finished, but some files still need attention.");

                MessageBox.Show(
                    this,
                    pendingFiles.Count == 0
                        ? L("UpdateCompleteMessage")
                        : LF("UpdateRemainingMessageFormat", pendingFiles.Count),
                    pendingFiles.Count == 0 ? L("UpdateCompleteTitle") : L("UpdateFinishedTitle"),
                    MessageBoxButtons.OK,
                    pendingFiles.Count == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                SetStatusText(L("UpdateFailed"), StatusTone.Error);
                AppendDetailedFailureLog("Update failed.", ex, SourceLocalClient, "downloading and applying client files", null);
                MessageBox.Show(this, BuildDetailedErrorMessage(ex, SourceLocalClient, "downloading and applying client files", null), L("UpdateFailed"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetBusyState(false, false);
            }
        }

        private void ResetLauncherState(bool clearNews)
        {
            currentManifest = null;
            pendingFiles = new List<PendingUpdateFile>();
            UpdatePendingFilesList();
            SetRemoteVersionText("-");
            SetStatusText(L("Ready"), StatusTone.Success);
            progressUpdate.Value = 0;
            btnUpdateClient.Enabled = false;

            if (clearNews)
            {
                SetNewsText(null);
            }
        }

        private void SetBusyState(bool busy, bool checking)
        {
            btnBrowseClient.Enabled = !busy;
            btnCheckUpdates.Enabled = !busy;
            btnClearCache.Enabled = !busy && CanClearCache();
            btnUpdateClient.Enabled = !busy && pendingFiles.Count > 0;
            btnLaunchGame.Enabled = !busy && CanLaunchGame();
            progressUpdate.Style = busy && checking ? ProgressBarStyle.Marquee : ProgressBarStyle.Continuous;

            if (!busy && pendingFiles.Count == 0)
            {
                progressUpdate.Value = 0;
            }
        }

        private bool CanLaunchGame()
        {
            return !string.IsNullOrEmpty(TryGetLaunchFilePath());
        }

        private bool CanClearCache()
        {
            string cachePath;
            return TryGetCacheDirectoryPath(out cachePath) && Directory.Exists(cachePath);
        }

        private bool TryGetInputs(out string clientPath, out Uri manifestUri)
        {
            clientPath = GetSelectedClientPath();
            if (!Uri.TryCreate(ManifestUrl, UriKind.Absolute, out manifestUri) || (manifestUri.Scheme != Uri.UriSchemeHttp && manifestUri.Scheme != Uri.UriSchemeHttps))
            {
                MessageBox.Show(this, L("InvalidManifestUrlMessage"), L("InvalidManifestUrlTitle"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (!string.Equals(txtClientPath.Text, clientPath, StringComparison.Ordinal))
            {
                SetClientPathText(clientPath);
            }

            if (string.IsNullOrWhiteSpace(clientPath))
            {
                MessageBox.Show(this, L("MissingClientFolderMessage"), L("MissingClientFolderTitle"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (!Directory.Exists(clientPath))
            {
                MessageBox.Show(this, L("InvalidClientFolderMessage"), L("InvalidClientFolderTitle"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        private async Task CheckForLauncherUpdatesAsync(bool userInitiated)
        {
            try
            {
                AppendLog("Checking for launcher updates.");
                await TryHandleLauncherUpdateAsync(userInitiated);
            }
            catch (Exception ex)
            {
                AppendDetailedFailureLog("Launcher update check failed.", ex, SourceLauncherUpdateService, "checking for launcher updates", null);

                if (userInitiated)
                {
                    MessageBox.Show(this, BuildDetailedErrorMessage(ex, SourceLauncherUpdateService, "checking for launcher updates", null), L("LauncherUpdateCheckFailedTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async Task<bool> TryHandleLauncherUpdateAsync(bool userInitiated)
        {
            var release = await LoadLatestLauncherReleaseAsync();
            if (release == null)
            {
                if (userInitiated)
                {
                    AppendLog("No launcher release was found.");
                }

                return false;
            }

            var asset = GetLauncherAsset(release);
            if (asset == null)
            {
                if (userInitiated)
                {
                    AppendLog("No launcher asset named " + LauncherAssetName + " was found in the latest GitHub release.");
                }

                return false;
            }

            string currentVersion = GetCurrentLauncherVersion();
            string remoteVersion = GetReleaseVersion(release);

            if (!IsLauncherUpdateAvailable(currentVersion, remoteVersion))
            {
                if (userInitiated)
                {
                    AppendLog("Launcher is already up to date.");
                }

                return false;
            }

            if (!userInitiated && string.Equals(skippedLauncherVersion, remoteVersion, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var prompt = LF("LauncherUpdatePromptFormat", currentVersion, remoteVersion);

            if (MessageBox.Show(this, prompt, L("LauncherUpdateAvailableTitle"), MessageBoxButtons.YesNo, MessageBoxIcon.Information) != DialogResult.Yes)
            {
                skippedLauncherVersion = remoteVersion;
                AppendLog("Launcher update postponed.");
                return false;
            }

            skippedLauncherVersion = null;
            return await DownloadAndApplyLauncherUpdateAsync(asset.DownloadUri);
        }

        private async Task<bool> DownloadAndApplyLauncherUpdateAsync(Uri downloadUri)
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), "OldManWarcraftLauncher", Guid.NewGuid().ToString("N"));
            string currentExecutablePath = Application.ExecutablePath;
            string tempExecutablePath = Path.Combine(tempDirectory, Path.GetFileName(currentExecutablePath));

            SetBusyState(true, false);
            progressUpdate.Style = ProgressBarStyle.Continuous;
            progressUpdate.Value = 0;
            SetStatusText(L("UpdatingLauncher"), StatusTone.Warning);
            AppendLog("Downloading launcher update from " + downloadUri + ".");

            try
            {
                Directory.CreateDirectory(tempDirectory);
                await DownloadLauncherFileAsync(downloadUri, tempExecutablePath);

                var startInfo = new ProcessStartInfo
                {
                    FileName = tempExecutablePath,
                    Arguments = SelfUpdater.BuildArguments(currentExecutablePath, Process.GetCurrentProcess().Id),
                    WorkingDirectory = tempDirectory,
                    UseShellExecute = true
                };

                if (Process.Start(startInfo) == null)
                {
                    throw new InvalidOperationException(L("LauncherUpdateStartFailed"));
                }

                AppendLog("Launcher update downloaded. Restarting to apply it.");
                progressUpdate.Value = 100;
                Close();
                return true;
            }
            catch (Exception ex)
            {
                SetStatusText(L("LauncherUpdateFailed"), StatusTone.Error);
                AppendDetailedFailureLog("Launcher update failed.", ex, SourceLauncherUpdateService, "downloading and applying the launcher update", downloadUri);
                MessageBox.Show(this, BuildDetailedErrorMessage(ex, SourceLauncherUpdateService, "downloading and applying the launcher update", downloadUri), L("LauncherUpdateFailed"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                SetBusyState(false, false);
                return false;
            }
        }

        private async Task DownloadLauncherFileAsync(Uri downloadUri, string destinationPath)
        {
            try
            {
                using (var response = await httpClient.GetAsync(downloadUri, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    long totalBytes = response.Content.Headers.ContentLength ?? 0;
                    if (totalBytes <= 0)
                    {
                        progressUpdate.Style = ProgressBarStyle.Marquee;
                    }

                    using (var sourceStream = await response.Content.ReadAsStreamAsync())
                    using (var targetStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        var buffer = new byte[81920];
                        int bytesRead;
                        long downloadedBytes = 0;

                        while ((bytesRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await targetStream.WriteAsync(buffer, 0, bytesRead);
                            downloadedBytes += bytesRead;

                            if (totalBytes > 0)
                            {
                                progressUpdate.Value = (int)Math.Min(100, (downloadedBytes * 100L) / totalBytes);
                            }
                        }
                    }
                }
            }
            catch
            {
                if (File.Exists(destinationPath))
                {
                    File.Delete(destinationPath);
                }

                throw;
            }
        }

        private Uri GetRemoteFileUri(Uri baseUri, string pathOrUrl)
        {
            Uri downloadUri;
            if (Uri.TryCreate(pathOrUrl, UriKind.Absolute, out downloadUri))
            {
                return downloadUri;
            }

            ValidateRelativePath(pathOrUrl);
            string normalizedPath = string.Join(
                "/",
                pathOrUrl
                    .Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(Uri.EscapeDataString));

            return new Uri(baseUri, normalizedPath);
        }

        private void ConfigureHttpClient()
        {
            httpClient.DefaultRequestHeaders.UserAgent.Clear();
            httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("WowLauncher", "1.0"));

            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        }

        private static LauncherOperationException FindOperationException(Exception ex)
        {
            while (ex != null)
            {
                var operationException = ex as LauncherOperationException;
                if (operationException != null)
                {
                    return operationException;
                }

                ex = ex.InnerException;
            }

            return null;
        }

        private static string GetMostRelevantErrorMessage(Exception ex)
        {
            var operationException = FindOperationException(ex);
            if (operationException != null && !string.IsNullOrWhiteSpace(operationException.Message))
            {
                return operationException.Message.Trim();
            }

            string message = ex == null ? null : ex.GetBaseException().Message;
            return string.IsNullOrWhiteSpace(message) ? "No additional details were provided." : message.Trim();
        }

        private string BuildDetailedErrorMessage(Exception ex, string fallbackSource, string fallbackStep, Uri fallbackRequestUri)
        {
            var operationException = FindOperationException(ex);
            string likelySource = operationException != null ? operationException.LikelySource : fallbackSource;
            string step = operationException != null ? operationException.Step : fallbackStep;
            Uri requestUri = operationException != null && operationException.RequestUri != null ? operationException.RequestUri : fallbackRequestUri;
            HttpStatusCode? statusCode = operationException != null ? operationException.StatusCode : null;
            string details = GetMostRelevantErrorMessage(ex);

            var lines = new List<string>
            {
                "Likely source: " + (string.IsNullOrWhiteSpace(likelySource) ? "Unknown" : likelySource),
                "Step: " + (string.IsNullOrWhiteSpace(step) ? "Unknown" : step)
            };

            if (requestUri != null)
            {
                lines.Add("Request: " + requestUri);
            }

            if (statusCode.HasValue)
            {
                lines.Add("HTTP status: " + (int)statusCode.Value + " " + statusCode.Value);
            }

            lines.Add("Details: " + details);
            return string.Join(Environment.NewLine, lines);
        }

        private void AppendDetailedFailureLog(string prefix, Exception ex, string fallbackSource, string fallbackStep, Uri fallbackRequestUri)
        {
            var operationException = FindOperationException(ex);
            string likelySource = operationException != null ? operationException.LikelySource : fallbackSource;
            string step = operationException != null ? operationException.Step : fallbackStep;
            Uri requestUri = operationException != null && operationException.RequestUri != null ? operationException.RequestUri : fallbackRequestUri;
            HttpStatusCode? statusCode = operationException != null ? operationException.StatusCode : null;

            var parts = new List<string>
            {
                prefix,
                "Source: " + (string.IsNullOrWhiteSpace(likelySource) ? "Unknown" : likelySource),
                "Step: " + (string.IsNullOrWhiteSpace(step) ? "Unknown" : step)
            };

            if (requestUri != null)
            {
                parts.Add("Request: " + requestUri);
            }

            if (statusCode.HasValue)
            {
                parts.Add("HTTP status: " + (int)statusCode.Value + " " + statusCode.Value);
            }

            parts.Add("Details: " + GetMostRelevantErrorMessage(ex));
            AppendLog(string.Join(" | ", parts));
        }

        private async Task EnsureSuccessStatusCodeAsync(HttpResponseMessage response, string likelySource, string step)
        {
            if (response != null && response.IsSuccessStatusCode)
            {
                return;
            }

            throw await CreateHttpFailureExceptionAsync(response, likelySource, step);
        }

        private async Task<LauncherOperationException> CreateHttpFailureExceptionAsync(HttpResponseMessage response, string likelySource, string step)
        {
            Uri requestUri = response == null || response.RequestMessage == null ? null : response.RequestMessage.RequestUri;
            HttpStatusCode? statusCode = response == null ? (HttpStatusCode?)null : response.StatusCode;
            string detail = "The remote endpoint returned an unsuccessful response.";

            if (statusCode.HasValue)
            {
                detail = string.Format("The remote endpoint returned HTTP {0} {1}.", (int)statusCode.Value, statusCode.Value);
            }

            string responsePreview = response == null ? null : await TryReadResponsePreviewAsync(response.Content);
            if (!string.IsNullOrWhiteSpace(responsePreview))
            {
                detail += " Response preview: " + responsePreview;
            }

            return new LauncherOperationException(likelySource, step, detail, requestUri, statusCode);
        }

        private static async Task<string> TryReadResponsePreviewAsync(HttpContent content)
        {
            if (content == null)
            {
                return null;
            }

            try
            {
                string text = await content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(text))
                {
                    return null;
                }

                string singleLine = text.Trim().Replace("\r", " ").Replace("\n", " ");
                return singleLine.Length <= 300 ? singleLine : singleLine.Substring(0, 300) + "...";
            }
            catch
            {
                return null;
            }
        }

        private async Task<GitHubRelease> LoadLatestLauncherReleaseAsync()
        {
            string releaseApiUrl;
            try
            {
                releaseApiUrl = GetLauncherReleaseApiUrl();
            }
            catch (Exception ex)
            {
                throw new LauncherOperationException(SourceLauncherConfiguration, "reading launcher update settings", GetMostRelevantErrorMessage(ex), null, null, ex);
            }

            Uri releaseUri;
            if (!Uri.TryCreate(releaseApiUrl, UriKind.Absolute, out releaseUri))
            {
                throw new LauncherOperationException(SourceLauncherConfiguration, "building the launcher update request", "The launcher update API URL is invalid.", null);
            }

            try
            {
                using (var latestResponse = await httpClient.GetAsync(releaseUri))
                {
                    if (latestResponse.StatusCode == HttpStatusCode.NotFound)
                    {
                        return null;
                    }

                    await EnsureSuccessStatusCodeAsync(latestResponse, SourceLauncherUpdateService, "checking for launcher updates");
                    using (var stream = await latestResponse.Content.ReadAsStreamAsync())
                    {
                        try
                        {
                            var serializer = new DataContractJsonSerializer(typeof(GitHubRelease));
                            var release = serializer.ReadObject(stream) as GitHubRelease;
                            if (release == null)
                            {
                                throw new LauncherOperationException(SourceLauncherUpdateService, "parsing launcher update data", "The launcher update response was empty.", releaseUri);
                            }

                            return release;
                        }
                        catch (LauncherOperationException)
                        {
                            throw;
                        }
                        catch (SerializationException ex)
                        {
                            throw new LauncherOperationException(SourceLauncherUpdateService, "parsing launcher update data", GetMostRelevantErrorMessage(ex), releaseUri, null, ex);
                        }
                    }
                }
            }
            catch (LauncherOperationException)
            {
                throw;
            }
            catch (TaskCanceledException ex)
            {
                throw new LauncherOperationException(SourceNetwork, "checking for launcher updates", "The request timed out while contacting the launcher update service.", releaseUri, null, ex);
            }
            catch (HttpRequestException ex)
            {
                throw new LauncherOperationException(SourceNetwork, "checking for launcher updates", GetMostRelevantErrorMessage(ex), releaseUri, null, ex);
            }
        }

        private static string GetLauncherReleaseApiUrl()
        {
            string repository = (GetAppSetting(LauncherGitHubRepositorySettingName) ?? DefaultLauncherGitHubRepository).Trim();
            if (string.IsNullOrWhiteSpace(repository) || string.Equals(repository, "owner/Wow-Launcher", StringComparison.OrdinalIgnoreCase))
            {
                repository = DefaultLauncherGitHubRepository;
            }

            string[] segments = repository.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length != 2 || segments.Any(string.IsNullOrWhiteSpace))
            {
                throw new InvalidOperationException("LauncherGitHubRepository must use the format 'owner/repository'.");
            }

            string apiBaseUrl = (GetAppSetting(LauncherGitHubApiBaseUrlSettingName) ?? DefaultGitHubApiBaseUrl).Trim();
            Uri baseUri;
            if (!Uri.TryCreate(apiBaseUrl.EndsWith("/", StringComparison.Ordinal) ? apiBaseUrl : apiBaseUrl + "/", UriKind.Absolute, out baseUri))
            {
                throw new InvalidOperationException("LauncherGitHubApiBaseUrl in App.config is invalid.");
            }

            return new Uri(baseUri, "repos/" + segments[0] + "/" + segments[1] + "/releases/latest").ToString();
        }

        private static string GetAppSetting(string key)
        {
            string configurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
            if (string.IsNullOrWhiteSpace(configurationFile) || !File.Exists(configurationFile))
            {
                return null;
            }

            var document = XDocument.Load(configurationFile);
            var appSettings = document.Root == null ? null : document.Root.Element("appSettings");
            if (appSettings == null)
            {
                return null;
            }

            var setting = appSettings.Elements("add")
                .FirstOrDefault(element => string.Equals((string)element.Attribute("key"), key, StringComparison.Ordinal));

            return setting == null ? null : (string)setting.Attribute("value");
        }

        private LauncherReleaseAsset GetLauncherAsset(GitHubRelease release)
        {
            var assets = release == null || release.Assets == null
                ? new List<GitHubReleaseAsset>()
                : release.Assets;

            var matchingAsset = assets.FirstOrDefault(asset =>
                asset != null
                && (!string.IsNullOrWhiteSpace(asset.Name) && string.Equals(asset.Name.Trim(), LauncherAssetName, StringComparison.OrdinalIgnoreCase)
                    || !string.IsNullOrWhiteSpace(asset.BrowserDownloadUrl) && asset.BrowserDownloadUrl.IndexOf(LauncherAssetName, StringComparison.OrdinalIgnoreCase) >= 0
                    || !string.IsNullOrWhiteSpace(asset.Url) && asset.Url.IndexOf(LauncherAssetName, StringComparison.OrdinalIgnoreCase) >= 0));

            if (matchingAsset == null)
            {
                return null;
            }

            Uri downloadUri;

            if (!TryCreateAbsoluteUri(matchingAsset.BrowserDownloadUrl, null, out downloadUri)
                && !TryCreateAbsoluteUri(matchingAsset.Url, null, out downloadUri))
            {
                return null;
            }

            return new LauncherReleaseAsset
            {
                DownloadUri = downloadUri,
                Version = GetReleaseVersion(release)
            };
        }

        private static string GetReleaseVersion(GitHubRelease release)
        {
            string version = release == null ? null : release.TagName;
            if (string.IsNullOrWhiteSpace(version))
            {
                version = release == null ? null : release.Name;
            }

            version = (version ?? string.Empty).Trim();
            if (version.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            {
                version = version.Substring(1);
            }

            return string.IsNullOrWhiteSpace(version) ? "Unknown" : version;
        }

        private static bool TryCreateAbsoluteUri(string candidate, Uri baseUri, out Uri absoluteUri)
        {
            absoluteUri = null;
            if (string.IsNullOrWhiteSpace(candidate))
            {
                return false;
            }

            if (Uri.TryCreate(candidate, UriKind.Absolute, out absoluteUri))
            {
                return true;
            }

            if (baseUri == null)
            {
                return false;
            }

            return Uri.TryCreate(baseUri, candidate, out absoluteUri);
        }

        private static string GetCurrentLauncherVersion()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            return version == null ? "Unknown" : version.ToString();
        }

        private static bool IsLauncherUpdateAvailable(string currentVersion, string remoteVersion)
        {
            Version current;
            Version remote;
            if (TryParseLauncherVersion(currentVersion, out current) && TryParseLauncherVersion(remoteVersion, out remote))
            {
                return remote > current;
            }

            string normalizedCurrent = (currentVersion ?? string.Empty).Trim();
            string normalizedRemote = (remoteVersion ?? string.Empty).Trim();
            if (normalizedRemote.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            {
                normalizedRemote = normalizedRemote.Substring(1);
            }

            if (string.IsNullOrWhiteSpace(normalizedRemote))
            {
                return false;
            }

            return !string.Equals(normalizedCurrent, normalizedRemote, StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryParseLauncherVersion(string versionText, out Version version)
        {
            version = null;

            string normalized = (versionText ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return false;
            }

            if (normalized.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring(1);
            }

            string[] rawParts = normalized.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            if (rawParts.Length < 2 || rawParts.Length > 4 || rawParts.Any(part => !part.All(char.IsDigit)))
            {
                return Version.TryParse(normalized, out version);
            }

            var parts = rawParts.ToList();
            while (parts.Count < 4)
            {
                parts.Add("0");
            }

            return Version.TryParse(string.Join(".", parts), out version);
        }

        private async Task<UpdateManifest> LoadManifestAsync(Uri manifestUri)
        {
            try
            {
                using (var response = await httpClient.GetAsync(manifestUri))
                {
                    await EnsureSuccessStatusCodeAsync(response, SourceUpdateServer, "downloading the update manifest");
                    using (var stream = await response.Content.ReadAsStreamAsync())
                    {
                        try
                        {
                            var serializer = new XmlSerializer(typeof(UpdateManifest));
                            var manifest = serializer.Deserialize(stream) as UpdateManifest;

                            if (manifest == null)
                            {
                                throw new LauncherOperationException(SourceUpdateServer, "validating the update manifest", "The remote manifest is empty.", manifestUri);
                            }

                            manifest.Files = manifest.Files ?? new List<UpdateFileEntry>();
                            if (manifest.Files.Count == 0)
                            {
                                throw new LauncherOperationException(SourceUpdateServer, "validating the update manifest", "The remote manifest does not contain any files.", manifestUri);
                            }

                            return manifest;
                        }
                        catch (LauncherOperationException)
                        {
                            throw;
                        }
                        catch (InvalidOperationException ex)
                        {
                            throw new LauncherOperationException(SourceUpdateServer, "parsing the update manifest", GetMostRelevantErrorMessage(ex), manifestUri, null, ex);
                        }
                    }
                }
            }
            catch (LauncherOperationException)
            {
                throw;
            }
            catch (TaskCanceledException ex)
            {
                throw new LauncherOperationException(SourceNetwork, "downloading the update manifest", "The request timed out while contacting the update server.", manifestUri, null, ex);
            }
            catch (HttpRequestException ex)
            {
                throw new LauncherOperationException(SourceNetwork, "downloading the update manifest", GetMostRelevantErrorMessage(ex), manifestUri, null, ex);
            }
        }

        private Uri ResolveNewsUri(UpdateManifest manifest, Uri manifestUri)
        {
            if (manifest == null || string.IsNullOrWhiteSpace(manifest.BreakingNewsUrl))
            {
                return null;
            }

            Uri newsUri;
            return Uri.TryCreate(manifest.BreakingNewsUrl, UriKind.Absolute, out newsUri)
                ? newsUri
                : new Uri(manifestUri, manifest.BreakingNewsUrl);
        }

        private async Task LoadNewsAsync(UpdateManifest manifest, Uri manifestUri)
        {
            if (manifest == null)
            {
                SetNewsText(null);
                return;
            }

            if (!string.IsNullOrWhiteSpace(manifest.NewsContent))
            {
                SetNewsText(manifest.NewsContent);
                AppendLog("Loaded inline realm news.");
                return;
            }

            if (string.IsNullOrWhiteSpace(manifest.BreakingNewsUrl))
            {
                SetNewsText(L("NoRealmNews"));
                return;
            }

            Uri newsUri = ResolveNewsUri(manifest, manifestUri);

            try
            {
                using (var response = await httpClient.GetAsync(newsUri))
                {
                    await EnsureSuccessStatusCodeAsync(response, SourceUpdateServer, "loading realm news");
                    var newsText = await response.Content.ReadAsStringAsync();
                    ReleaseNotesFeed releaseNotes;
                    if (TryDeserializeReleaseNotes(newsText, out releaseNotes))
                    {
                        ShowReleaseNotes(releaseNotes);
                    }
                    else
                    {
                        SetNewsText(newsText);
                    }
                }

                AppendLog("Loaded realm news from " + newsUri + ".");
            }
            catch (LauncherOperationException ex)
            {
                SetNewsText(L("RealmNewsLoadFailed"));
                AppendDetailedFailureLog("News load failed.", ex, SourceUpdateServer, "loading realm news", newsUri);
            }
            catch (TaskCanceledException ex)
            {
                SetNewsText(L("RealmNewsLoadFailed"));
                AppendDetailedFailureLog("News load failed.", new LauncherOperationException(SourceNetwork, "loading realm news", "The request timed out while loading realm news.", newsUri, null, ex), SourceUpdateServer, "loading realm news", newsUri);
            }
            catch (HttpRequestException ex)
            {
                SetNewsText(L("RealmNewsLoadFailed"));
                AppendDetailedFailureLog("News load failed.", new LauncherOperationException(SourceNetwork, "loading realm news", GetMostRelevantErrorMessage(ex), newsUri, null, ex), SourceUpdateServer, "loading realm news", newsUri);
            }
            catch (Exception ex)
            {
                SetNewsText(L("RealmNewsLoadFailed"));
                AppendDetailedFailureLog("News load failed.", ex, SourceUpdateServer, "loading realm news", newsUri);
            }
        }

        private async Task<List<PendingUpdateFile>> BuildPendingFileListAsync(string clientPath, Uri manifestUri, UpdateManifest manifest)
        {
            var results = new List<PendingUpdateFile>();
            Uri baseUri;
            try
            {
                baseUri = GetBaseUri(manifestUri, manifest.BaseUrl);
            }
            catch (Exception ex)
            {
                throw new LauncherOperationException(SourceUpdateServer, "validating the update manifest base URL", GetMostRelevantErrorMessage(ex), manifestUri, null, ex);
            }

            foreach (var entry in manifest.Files.Where(file => !string.IsNullOrWhiteSpace(file.FilePath)))
            {
                if (ContainsDotPrefixedPathSegment(entry.FilePath))
                {
                    AppendLog("Skipping manifest entry from dot-prefixed path: " + entry.FilePath + ".");
                    continue;
                }

                try
                {
                    ValidateRelativePath(entry.FilePath);
                }
                catch (Exception ex)
                {
                    throw new LauncherOperationException(SourceUpdateServer, "validating the update manifest file list", GetMostRelevantErrorMessage(ex), manifestUri, null, ex);
                }

                string localPath;
                try
                {
                    localPath = GetSafeLocalPath(clientPath, entry.FilePath);
                }
                catch (Exception ex)
                {
                    throw new LauncherOperationException(SourceUpdateServer, "validating the update manifest file list", GetMostRelevantErrorMessage(ex), manifestUri, null, ex);
                }

                string reason;
                try
                {
                    reason = await GetPendingReasonAsync(localPath, entry);
                }
                catch (Exception ex)
                {
                    throw new LauncherOperationException(SourceLocalClient, "reading local client files", entry.FilePath + ": " + GetMostRelevantErrorMessage(ex), null, null, ex);
                }

                if (reason == null)
                {
                    continue;
                }

                Uri downloadUri;
                try
                {
                    downloadUri = GetRemoteFileUri(baseUri, entry.FilePath);
                }
                catch (Exception ex)
                {
                    throw new LauncherOperationException(SourceUpdateServer, "building remote file download URLs", GetMostRelevantErrorMessage(ex), manifestUri, null, ex);
                }

                results.Add(new PendingUpdateFile
                {
                    Entry = entry,
                    LocalPath = localPath,
                    DownloadUri = downloadUri,
                    ReasonKey = reason
                });
            }

            return results;
        }

        private async Task<string> GetPendingReasonAsync(string localPath, UpdateFileEntry entry)
        {
            if (!File.Exists(localPath))
            {
                return "PendingReasonMissing";
            }

            var info = new FileInfo(localPath);
            if (entry.Size > 0 && info.Length != entry.Size)
            {
                return "PendingReasonSizeMismatch";
            }

            if (!string.IsNullOrWhiteSpace(entry.Sha256))
            {
                var localHash = await Task.Run(() => ComputeSha256(localPath));
                if (!string.Equals(localHash, entry.Sha256, StringComparison.OrdinalIgnoreCase))
                {
                    return "PendingReasonHashMismatch";
                }
            }

            return null;
        }

        private async Task DownloadFileAsync(PendingUpdateFile pendingFile, Action<int> reportBytesDownloaded)
        {
            string tempPath = pendingFile.LocalPath + ".download";
            string directory = Path.GetDirectoryName(pendingFile.LocalPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            try
            {
                DeleteFileIfExists(tempPath);

                using (var response = await httpClient.GetAsync(pendingFile.DownloadUri, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    long remoteLength = response.Content.Headers.ContentLength ?? pendingFile.Entry.Size;

                    using (var sourceStream = await response.Content.ReadAsStreamAsync())
                    using (var targetStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        var buffer = new byte[81920];
                        int bytesRead;
                        long fileBytesRead = 0;

                        while ((bytesRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await targetStream.WriteAsync(buffer, 0, bytesRead);
                            fileBytesRead += bytesRead;
                            if (reportBytesDownloaded != null)
                            {
                                reportBytesDownloaded(bytesRead);
                            }
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(pendingFile.Entry.Sha256))
                {
                    string downloadedHash = await Task.Run(() => ComputeSha256(tempPath));
                    if (!string.Equals(downloadedHash, pendingFile.Entry.Sha256, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("Downloaded file failed hash verification: " + pendingFile.Entry.FilePath);
                    }
                }

                ReplaceFile(tempPath, pendingFile.LocalPath);
            }
            catch
            {
                DeleteFileIfExists(tempPath);

                throw;
            }
        }

        private static void DeleteFileIfExists(string path)
        {
            if (!File.Exists(path))
            {
                return;
            }

            ClearReadOnlyAttribute(path);
            File.Delete(path);
        }

        private static void ReplaceFile(string sourcePath, string destinationPath)
        {
            if (!File.Exists(destinationPath))
            {
                File.Move(sourcePath, destinationPath);
                return;
            }

            ClearReadOnlyAttribute(destinationPath);
            File.Replace(sourcePath, destinationPath, null, true);
        }

        private static void ClearReadOnlyAttribute(string path)
        {
            var attributes = File.GetAttributes(path);
            if ((attributes & FileAttributes.ReadOnly) != 0)
            {
                File.SetAttributes(path, attributes & ~FileAttributes.ReadOnly);
            }
        }

        private void UpdateProgress(long totalBytes, long downloadedBytes, int completedFiles, int fileCount)
        {
            int fileProgress = (int)Math.Min(100, (completedFiles * 100L) / Math.Max(1, fileCount));
            int value;

            if (totalBytes > 0)
            {
                int byteProgress = (int)Math.Min(100, (downloadedBytes * 100L) / totalBytes);
                value = Math.Max(byteProgress, fileProgress);
            }
            else
            {
                value = fileProgress;
            }

            progressUpdate.Value = Math.Max(progressUpdate.Minimum, Math.Min(progressUpdate.Maximum, value));
        }

        private void UpdatePendingFilesList()
        {
            lstFilesToUpdate.BeginUpdate();
            lstFilesToUpdate.Items.Clear();

            foreach (var pendingFile in pendingFiles)
            {
                lstFilesToUpdate.Items.Add(pendingFile.Entry.FilePath + " - " + L(pendingFile.ReasonKey));
            }

            lstFilesToUpdate.EndUpdate();
        }

        private Uri GetBaseUri(Uri manifestUri, string manifestBaseUrl)
        {
            if (string.IsNullOrWhiteSpace(manifestBaseUrl))
            {
                return new Uri(manifestUri, ".");
            }

            Uri baseUri;
            if (Uri.TryCreate(manifestBaseUrl, UriKind.Absolute, out baseUri))
            {
                return baseUri;
            }

            return new Uri(manifestUri, manifestBaseUrl);
        }

        private string GetSafeLocalPath(string clientPath, string relativePath)
        {
            string rootPath = Path.GetFullPath(clientPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar);
            string combinedPath = Path.GetFullPath(Path.Combine(rootPath, relativePath.Replace('/', Path.DirectorySeparatorChar)));

            if (!combinedPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Manifest path escapes the client folder: " + relativePath);
            }

            return combinedPath;
        }

        private void ValidateRelativePath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
            {
                throw new InvalidOperationException("Manifest contains an invalid relative path: " + relativePath);
            }

            var segments = relativePath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            if (segments.Any(segment => segment == ".."))
            {
                throw new InvalidOperationException("Manifest contains an invalid relative path: " + relativePath);
            }
        }

        private static bool ContainsDotPrefixedPathSegment(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return false;
            }

            return relativePath
                .Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries)
                .Any(segment => segment.StartsWith(".", StringComparison.Ordinal) && segment != "." && segment != "..");
        }

        private string TryGetLaunchFilePath()
        {
            string clientPath = GetSelectedClientPath();
            if (string.IsNullOrWhiteSpace(clientPath) || !Directory.Exists(clientPath))
            {
                return null;
            }

            string relativePath = currentManifest != null && !string.IsNullOrWhiteSpace(currentManifest.LaunchFile)
                ? currentManifest.LaunchFile
                : "Wow.exe";

            try
            {
                ValidateRelativePath(relativePath);
                string launchPath = GetSafeLocalPath(clientPath, relativePath);
                return File.Exists(launchPath) ? launchPath : null;
            }
            catch
            {
                return null;
            }
        }

        private bool TryGetCacheDirectoryPath(out string cachePath)
        {
            cachePath = null;

            string clientPath = GetSelectedClientPath();
            if (string.IsNullOrWhiteSpace(clientPath) || !Directory.Exists(clientPath))
            {
                return false;
            }

            try
            {
                cachePath = GetSafeLocalPath(clientPath, "Cache");
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void InitializeVisualStyle()
        {
            ConfigureActionButton(btnBrowseClient, Color.FromArgb(198, 162, 84), Color.FromArgb(221, 188, 114), Color.FromArgb(31, 24, 18));
            ConfigureActionButton(btnCheckUpdates, Color.FromArgb(198, 162, 84), Color.FromArgb(221, 188, 114), Color.FromArgb(31, 24, 18));
            ConfigureActionButton(btnUpdateClient, Color.FromArgb(198, 162, 84), Color.FromArgb(221, 188, 114), Color.FromArgb(31, 24, 18));
            ConfigureActionButton(btnLaunchGame, Color.FromArgb(118, 154, 82), Color.FromArgb(143, 183, 101), Color.FromArgb(21, 24, 18));
            ConfigureActionButton(btnClearCache, Color.FromArgb(130, 92, 62), Color.FromArgb(158, 116, 83), Color.FromArgb(245, 235, 220));
            txtNews.DetectUrls = true;
            SetRemoteVersionText("-");
        }

        private void ApplyBranding()
        {
            Text = LF("FormTitleFormat", ServerName);
            lblTitle.Text = ServerName;
            grpConnection.Text = LF("ConnectionGroupFormat", ServerName);
            lblClientHint.Text = LF("ClientHintFormat", ServerName);
            grpNews.Text = LF("NewsGroupFormat", ServerName);
            lblNewsHint.Text = LF("NewsHintFormat", ServerName);
            grpUpdates.Text = LF("UpdatesGroupFormat", ServerName);
            lblUpdatesHint.Text = LF("UpdatesHintFormat", ServerName);
            lblRemoteVersion.Text = LF("RemoteVersionLabelFormat", ServerName.ToUpperInvariant());
            lblRealmStatusIndicator.Text = LF("RealmStatusLabelFormat", ServerName.ToUpperInvariant());
            UpdateSubtitleText();
        }

        private async Task RefreshRealmStatusAsync()
        {
            try
            {
                AppendLog("Checking realm status.");
                var realmStatus = await LoadRealmStatusAsync();
                SetRealmStatusIndicator(RealmStatusState.Online);
                SetOnlinePlayerCountText(BuildOnlinePlayerCountText(realmStatus));
                AppendLog("Realm is online. " + BuildRealmStatusLogMessage(realmStatus));
            }
            catch (Exception ex)
            {
                SetRealmStatusIndicator(RealmStatusState.Offline);
                SetOnlinePlayerCountText("-");
                AppendLog("Realm status check failed: " + ex.Message);
            }
        }

        private async Task<RealmStatusResponse> LoadRealmStatusAsync()
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, RealmStatusApiUrl))
            {
                request.Headers.Add("X-API-Key", RealmStatusApiKey);

                using (var response = await httpClient.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                    using (var stream = await response.Content.ReadAsStreamAsync())
                    {
                        var serializer = new DataContractJsonSerializer(typeof(RealmStatusResponse));
                        var realmStatus = serializer.ReadObject(stream) as RealmStatusResponse;
                        if (realmStatus == null)
                        {
                            throw new InvalidOperationException("The realm status response was empty.");
                        }

                        return realmStatus;
                    }
                }
            }
        }

        private void UpdateSubtitleText()
        {
            lblSubtitle.Text = LF("SubtitleFormat", GetCurrentLauncherDisplayVersion());
        }

        private static string GetCurrentLauncherDisplayVersion()
        {
            var informationalVersion = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            string version = informationalVersion == null ? null : informationalVersion.InformationalVersion;
            return string.IsNullOrWhiteSpace(version) ? GetCurrentLauncherVersion() : version.Trim();
        }

        private void SetRealmStatusIndicator(RealmStatusState statusState)
        {
            currentRealmStatusState = statusState;
            ApplyCurrentRealmStatusDisplay();
        }

        private void ApplyCurrentRealmStatusDisplay()
        {
            lblRealmStatusIndicatorValue.Text = L(GetRealmStatusTextKey(currentRealmStatusState));

            if (currentRealmStatusState == RealmStatusState.Online)
            {
                lblRealmStatusIndicatorValue.ForeColor = Color.FromArgb(166, 208, 124);
            }
            else if (currentRealmStatusState == RealmStatusState.Offline)
            {
                lblRealmStatusIndicatorValue.ForeColor = Color.FromArgb(232, 109, 103);
            }
            else if (currentRealmStatusState == RealmStatusState.Checking)
            {
                lblRealmStatusIndicatorValue.ForeColor = Color.FromArgb(239, 203, 110);
            }
            else
            {
                lblRealmStatusIndicatorValue.ForeColor = Color.WhiteSmoke;
            }
        }

        private void SetOnlinePlayerCountText(string playerCountText)
        {
            currentOnlinePlayerCountText = string.IsNullOrWhiteSpace(playerCountText) ? "-" : playerCountText.Trim();
            ApplyCurrentOnlinePlayerCountDisplay();
        }

        private void ApplyCurrentOnlinePlayerCountDisplay()
        {
            lblOnlinePlayerCountValue.Text = currentOnlinePlayerCountText;
            lblOnlinePlayerCountValue.ForeColor = currentOnlinePlayerCountText == "-"
                ? Color.FromArgb(186, 184, 176)
                : Color.FromArgb(239, 203, 110);
        }

        private static string BuildOnlinePlayerCountText(RealmStatusResponse realmStatus)
        {
            if (realmStatus == null)
            {
                return "-";
            }

            int maxPlayerCount = Math.Max(realmStatus.PlayerCount, realmStatus.MaxPlayerCount);
            return maxPlayerCount > 0
                ? string.Format("{0}/{1}", realmStatus.PlayerCount, maxPlayerCount)
                : realmStatus.PlayerCount.ToString();
        }

        private string BuildRealmStatusLogMessage(RealmStatusResponse realmStatus)
        {
            if (realmStatus == null)
            {
                return "Realm status unavailable.";
            }

            var details = new List<string>
            {
                string.Format("{0}: {1}/{2}", L("PlayersPrefix"), realmStatus.PlayerCount, Math.Max(realmStatus.PlayerCount, realmStatus.MaxPlayerCount))
            };

            if (realmStatus.QueuedSessions > 0)
            {
                details.Add(L("QueuePrefix") + ": " + realmStatus.QueuedSessions);
            }

            if (!string.IsNullOrWhiteSpace(realmStatus.UptimeFormatted))
            {
                details.Add(L("UptimePrefix") + ": " + realmStatus.UptimeFormatted.Trim());
            }

            return string.Join(" | ", details);
        }

        private static void ConfigureActionButton(Button button, Color backColor, Color hoverColor, Color textColor)
        {
            button.BackColor = backColor;
            button.ForeColor = textColor;
            button.FlatAppearance.MouseOverBackColor = hoverColor;
            button.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(hoverColor);
            button.Cursor = Cursors.Hand;
        }

        private void SetStatusText(string statusText, StatusTone statusTone)
        {
            currentStatusText = (statusText ?? string.Empty).Trim();
            currentStatusTone = statusTone;
            ApplyCurrentStatusDisplay();
        }

        private void ApplyCurrentStatusDisplay()
        {
            lblStatusValue.Text = string.IsNullOrWhiteSpace(currentStatusText) ? "-" : currentStatusText;

            if (currentStatusTone == StatusTone.Error)
            {
                lblStatusValue.ForeColor = Color.FromArgb(232, 109, 103);
            }
            else if (currentStatusTone == StatusTone.Success)
            {
                lblStatusValue.ForeColor = Color.FromArgb(166, 208, 124);
            }
            else if (currentStatusTone == StatusTone.Warning)
            {
                lblStatusValue.ForeColor = Color.FromArgb(239, 203, 110);
            }
            else
            {
                lblStatusValue.ForeColor = Color.WhiteSmoke;
            }
        }

        private void SetRemoteVersionText(string versionText)
        {
            currentRemoteVersionText = string.IsNullOrWhiteSpace(versionText) ? "-" : versionText.Trim();
            ApplyCurrentRemoteVersionDisplay();
        }

        private void ApplyCurrentRemoteVersionDisplay()
        {
            lblRemoteVersionValue.Text = currentRemoteVersionText;
            lblRemoteVersionValue.ForeColor = currentRemoteVersionText == "-"
                ? Color.FromArgb(186, 184, 176)
                : Color.FromArgb(239, 203, 110);
        }

        private void SetNewsText(string message)
        {
            currentReleaseNotes = null;
            currentNewsMessage = string.IsNullOrWhiteSpace(message) ? GetDefaultNewsText() : message.Trim();
            txtNews.Clear();
            AppendNewsHeading(LF("NewsHeadingFormat", ServerName));
            AppendNewsBody(currentNewsMessage);
            txtNews.Select(0, 0);
        }

        private string FormatNewsText(string newsText)
        {
            if (string.IsNullOrWhiteSpace(newsText))
            {
                return L("NoRealmNews");
            }

            ReleaseNotesFeed releaseNotes;
            if (!TryDeserializeReleaseNotes(newsText, out releaseNotes))
            {
                return newsText.Trim();
            }

            return BuildReleaseNotesSummary(releaseNotes);
        }

        private static bool TryDeserializeReleaseNotes(string newsText, out ReleaseNotesFeed releaseNotes)
        {
            releaseNotes = null;

            try
            {
                var serializer = new DataContractJsonSerializer(typeof(ReleaseNotesFeed));
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(newsText)))
                {
                    releaseNotes = serializer.ReadObject(stream) as ReleaseNotesFeed;
                }

                return releaseNotes != null && (releaseNotes.Latest != null || (releaseNotes.History != null && releaseNotes.History.Count > 0));
            }
            catch
            {
                return false;
            }
        }

        private string BuildReleaseNotesSummary(ReleaseNotesFeed releaseNotes)
        {
            var latest = releaseNotes.Latest ?? releaseNotes.History.FirstOrDefault();
            var builder = new StringBuilder();

            if (latest != null)
            {
                builder.Append(L("BreakingNewsHeading"));
                if (!string.IsNullOrWhiteSpace(latest.Version))
                {
                    builder.Append(" - ").Append(latest.Version.Trim());
                }

                builder.AppendLine();

                if (!string.IsNullOrWhiteSpace(latest.ReleaseNotes))
                {
                    builder.AppendLine(latest.ReleaseNotes.Trim());
                }

                var metadata = BuildReleaseNoteMetadata(latest);
                if (!string.IsNullOrWhiteSpace(metadata))
                {
                    builder.AppendLine(metadata);
                }
            }

            var history = (releaseNotes.History ?? new List<ReleaseNotesEntry>())
                .Where(entry => entry != null && !AreSameReleaseNotesEntry(entry, latest) && !string.IsNullOrWhiteSpace(entry.ReleaseNotes))
                .Take(4)
                .ToList();

            if (history.Count > 0)
            {
                if (builder.Length > 0)
                {
                    builder.AppendLine();
                }

                builder.AppendLine(L("RecentUpdatesHeading"));
                foreach (var entry in history)
                {
                    builder.Append("- ");
                    if (!string.IsNullOrWhiteSpace(entry.Version))
                    {
                        builder.Append(entry.Version.Trim()).Append(": ");
                    }

                    builder.Append(entry.ReleaseNotes.Trim());

                    if (!string.IsNullOrWhiteSpace(entry.CreatedAt))
                    {
                        builder.Append(" (").Append(entry.CreatedAt.Trim()).Append(")");
                    }

                    builder.AppendLine();
                }
            }

            return builder.Length == 0 ? L("NoRealmNews") : builder.ToString().Trim();
        }

        private string BuildReleaseNoteMetadata(ReleaseNotesEntry entry)
        {
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(entry.CreatedAt))
            {
                parts.Add(L("PublishedPrefix") + ": " + entry.CreatedAt.Trim());
            }

            if (!string.IsNullOrWhiteSpace(entry.CreatedBy))
            {
                parts.Add(L("ByPrefix") + ": " + entry.CreatedBy.Trim());
            }

            return string.Join(" | ", parts);
        }

        private static bool AreSameReleaseNotesEntry(ReleaseNotesEntry left, ReleaseNotesEntry right)
        {
            return left != null && right != null
                && string.Equals(left.Version, right.Version, StringComparison.OrdinalIgnoreCase)
                && string.Equals(left.CreatedAt, right.CreatedAt, StringComparison.OrdinalIgnoreCase)
                && string.Equals(left.ReleaseNotes, right.ReleaseNotes, StringComparison.Ordinal);
        }

        private void ShowReleaseNotes(ReleaseNotesFeed releaseNotes)
        {
            currentReleaseNotes = releaseNotes;
            currentNewsMessage = null;
            txtNews.Clear();
            AppendNewsHeading(LF("NewsHeadingFormat", ServerName));
            AppendNewsSpacing();

            var latest = releaseNotes.Latest ?? (releaseNotes.History ?? new List<ReleaseNotesEntry>()).FirstOrDefault();
            if (latest != null)
            {
                AppendNewsHeading(L("BreakingNewsHeading"));

                if (!string.IsNullOrWhiteSpace(latest.Version))
                {
                    AppendNewsSubheading(L("BuildPrefix") + " " + latest.Version.Trim());
                }

                if (!string.IsNullOrWhiteSpace(latest.ReleaseNotes))
                {
                    AppendNewsBody(latest.ReleaseNotes.Trim());
                }

                string metadata = BuildReleaseNoteMetadata(latest);
                if (!string.IsNullOrWhiteSpace(metadata))
                {
                    AppendNewsMuted(metadata);
                }
            }

            var history = (releaseNotes.History ?? new List<ReleaseNotesEntry>())
                .Where(entry => entry != null && !AreSameReleaseNotesEntry(entry, latest) && !string.IsNullOrWhiteSpace(entry.ReleaseNotes))
                .Take(4)
                .ToList();

            if (history.Count > 0)
            {
                AppendNewsSpacing();
                AppendNewsHeading(L("RecentUpdatesHeading"));

                foreach (var entry in history)
                {
                    var summary = new StringBuilder();
                    if (!string.IsNullOrWhiteSpace(entry.Version))
                    {
                        summary.Append(entry.Version.Trim()).Append(" - ");
                    }

                    summary.Append(entry.ReleaseNotes.Trim());
                    if (!string.IsNullOrWhiteSpace(entry.CreatedAt))
                    {
                        summary.Append(" (").Append(entry.CreatedAt.Trim()).Append(")");
                    }

                    AppendNewsBullet(summary.ToString());
                }
            }

            if (txtNews.TextLength == 0)
            {
                SetNewsText(L("NoRealmNews"));
                return;
            }

            txtNews.Select(0, 0);
        }

        private void AppendNewsHeading(string text)
        {
            AppendNewsText(text + Environment.NewLine, 10.75f, FontStyle.Bold, Color.FromArgb(236, 198, 106));
        }

        private void AppendNewsSubheading(string text)
        {
            AppendNewsText(text + Environment.NewLine, 9.5f, FontStyle.Bold, Color.FromArgb(244, 226, 172));
        }

        private void AppendNewsBody(string text)
        {
            AppendNewsText(text + Environment.NewLine, 9.25f, FontStyle.Regular, Color.FromArgb(229, 225, 214));
        }

        private void AppendNewsMuted(string text)
        {
            AppendNewsText(text + Environment.NewLine, 8.75f, FontStyle.Italic, Color.FromArgb(164, 172, 186));
        }

        private void AppendNewsBullet(string text)
        {
            AppendNewsText("• " + text + Environment.NewLine, 9.25f, FontStyle.Regular, Color.FromArgb(229, 225, 214));
        }

        private void AppendNewsSpacing()
        {
            txtNews.AppendText(Environment.NewLine);
        }

        private void AppendNewsText(string text, float size, FontStyle style, Color color)
        {
            using (var font = new Font("Segoe UI", size, style))
            {
                txtNews.SelectionStart = txtNews.TextLength;
                txtNews.SelectionLength = 0;
                txtNews.SelectionFont = font;
                txtNews.SelectionColor = color;
                txtNews.AppendText(text);
            }
        }

        private void AppendLog(string message)
        {
            txtLog.AppendText(string.Format("[{0:HH:mm:ss}] {1}{2}", DateTime.Now, message, Environment.NewLine));
        }

        private static string GetRealmStatusTextKey(RealmStatusState statusState)
        {
            switch (statusState)
            {
                case RealmStatusState.Checking:
                    return "RealmStatusChecking";
                case RealmStatusState.Online:
                    return "RealmStatusOnline";
                case RealmStatusState.Offline:
                    return "RealmStatusOffline";
                default:
                    return "RealmStatusUnknown";
            }
        }

        private static string ComputeSha256(string filePath)
        {
            using (var algorithm = SHA256.Create())
            using (var stream = File.OpenRead(filePath))
            {
                byte[] hash = algorithm.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", string.Empty);
            }
        }
    }
}
