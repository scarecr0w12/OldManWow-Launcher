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
        private const string DefaultGitHubApiBaseUrl = "https://api.github.com";
        private const string LauncherAssetName = "Wow-Launcher.exe";
        private const int MaxConcurrentDownloads = 4;
        private const string DefaultNewsText = "Click Check Updates to load the latest " + ServerName + " news.";

        private readonly HttpClient httpClient = new HttpClient();
        private UpdateManifest currentManifest;
        private bool launcherUpdateCheckStarted;
        private List<PendingUpdateFile> pendingFiles = new List<PendingUpdateFile>();
        private string skippedLauncherVersion;

        public Form1()
        {
            InitializeComponent();
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
            SetStatusText("Ready");
            SetNewsText(DefaultNewsText);
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
            await CheckForLauncherUpdatesAsync(false);
        }

        private async void btnCheckUpdates_Click(object sender, EventArgs e)
        {
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
                dialog.Description = "Select your World of Warcraft 3.3.5a client folder.";
                dialog.SelectedPath = Directory.Exists(txtClientPath.Text) ? txtClientPath.Text : string.Empty;

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
                    MessageBox.Show(this, "Could not find the game executable in the selected folder.", "Launch failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                Process.Start(launchPath);
                AppendLog("Launched " + Path.GetFileName(launchPath) + ".");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Launch failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnClearCache_Click(object sender, EventArgs e)
        {
            string cachePath;
            if (!TryGetCacheDirectoryPath(out cachePath))
            {
                MessageBox.Show(this, "Select the local client folder first.", "Missing client folder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!Directory.Exists(cachePath))
            {
                MessageBox.Show(this, "No Cache folder was found in the selected client directory.", "Cache not found", MessageBoxButtons.OK, MessageBoxIcon.Information);
                btnClearCache.Enabled = false;
                return;
            }

            if (MessageBox.Show(this, "Delete the Cache folder from the selected client directory?", "Clear cache", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }

            try
            {
                Directory.Delete(cachePath, true);
                ResetLauncherState(false);
                SetStatusText("Cache cleared.");
                btnLaunchGame.Enabled = CanLaunchGame();
                btnClearCache.Enabled = CanClearCache();
                AppendLog("Deleted Cache folder.");
            }
            catch (Exception ex)
            {
                SetStatusText("Cache clear failed.");
                AppendLog("Cache clear failed: " + ex.Message);
                MessageBox.Show(this, ex.Message, "Cache clear failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void txtClientPath_TextChanged(object sender, EventArgs e)
        {
            ResetLauncherState(false);
            btnClearCache.Enabled = CanClearCache();
            btnLaunchGame.Enabled = CanLaunchGame();
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
            SetStatusText("Checking for updates...");
            SetRemoteVersionText("-");
            lstFilesToUpdate.Items.Clear();
            SetNewsText("Gathering realm news...");
            AppendLog("Loading manifest from " + manifestUri + ".");

            try
            {
                currentManifest = await LoadManifestAsync(manifestUri);

                try
                {
                    if (await TryHandleLauncherUpdateAsync(true))
                    {
                        return;
                    }
                }
                catch (Exception ex)
                {
                    AppendLog("Launcher update check skipped: " + ex.Message);
                }

                await LoadNewsAsync(currentManifest, manifestUri);
                pendingFiles = await BuildPendingFileListAsync(clientPath, manifestUri, currentManifest);
                SetRemoteVersionText(currentManifest.Version);
                UpdatePendingFilesList();

                if (pendingFiles.Count == 0)
                {
                    SetStatusText("Client is up to date.");
                    AppendLog("No updates are required.");
                }
                else
                {
                    SetStatusText(pendingFiles.Count + " file(s) need to be updated.");
                    AppendLog("Found " + pendingFiles.Count + " file(s) to update.");
                }
            }
            catch (Exception ex)
            {
                currentManifest = null;
                pendingFiles = new List<PendingUpdateFile>();
                UpdatePendingFilesList();
                SetStatusText("Update check failed.");
                SetNewsText("Unable to load realm news until the manifest can be reached.");
                AppendLog("Update check failed: " + ex.Message);
                MessageBox.Show(this, ex.Message, "Update check failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            SetStatusText("Downloading updates with " + concurrentDownloads + " threads...");

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
                            SetStatusText(string.Format("Downloaded {0}/{1} file(s)...", finishedFiles, filesToDownload.Count));
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
                SetStatusText(pendingFiles.Count == 0 ? "Update complete." : "Update finished with remaining files.");
                AppendLog("Update completed.");
            }
            catch (Exception ex)
            {
                SetStatusText("Update failed.");
                AppendLog("Update failed: " + ex.Message);
                MessageBox.Show(this, ex.Message, "Update failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            SetStatusText("Ready");
            progressUpdate.Value = 0;
            btnUpdateClient.Enabled = false;

            if (clearNews)
            {
                SetNewsText(DefaultNewsText);
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
            clientPath = txtClientPath.Text.Trim();
            if (!Uri.TryCreate(ManifestUrl, UriKind.Absolute, out manifestUri) || (manifestUri.Scheme != Uri.UriSchemeHttp && manifestUri.Scheme != Uri.UriSchemeHttps))
            {
                MessageBox.Show(this, "The hardcoded manifest URL is invalid.", "Invalid manifest URL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(clientPath))
            {
                MessageBox.Show(this, "Select the local client folder first.", "Missing client folder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (!Directory.Exists(clientPath))
            {
                MessageBox.Show(this, "The selected client folder does not exist.", "Invalid client folder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                AppendLog("Launcher update check failed: " + ex.Message);

                if (userInitiated)
                {
                    MessageBox.Show(this, ex.Message, "Launcher update check failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

            var prompt = string.Format(
                "A launcher update is available.\r\n\r\nCurrent version: {0}\r\nAvailable version: {1}\r\n\r\nInstall it now? The launcher will restart after the update finishes.",
                currentVersion,
                remoteVersion);

            if (MessageBox.Show(this, prompt, "Launcher update available", MessageBoxButtons.YesNo, MessageBoxIcon.Information) != DialogResult.Yes)
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
            SetStatusText("Updating launcher...");
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
                    throw new InvalidOperationException("The launcher update could not be started.");
                }

                AppendLog("Launcher update downloaded. Restarting to apply it.");
                progressUpdate.Value = 100;
                Close();
                return true;
            }
            catch (Exception ex)
            {
                SetStatusText("Launcher update failed.");
                AppendLog("Launcher update failed: " + ex.Message);
                MessageBox.Show(this, ex.Message, "Launcher update failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        private async Task<GitHubRelease> LoadLatestLauncherReleaseAsync()
        {
            using (var latestResponse = await httpClient.GetAsync(GetLauncherReleaseApiUrl()))
            {
                if (latestResponse.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }

                latestResponse.EnsureSuccessStatusCode();
                using (var stream = await latestResponse.Content.ReadAsStreamAsync())
                {
                    var serializer = new DataContractJsonSerializer(typeof(GitHubRelease));
                    return serializer.ReadObject(stream) as GitHubRelease;
                }
            }
        }

        private static string GetLauncherReleaseApiUrl()
        {
            string repository = (GetAppSetting(LauncherGitHubRepositorySettingName) ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(repository) || string.Equals(repository, "owner/Wow-Launcher", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Configure LauncherGitHubRepository in App.config before using launcher self-updates.");
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
            using (var response = await httpClient.GetAsync(manifestUri))
            {
                response.EnsureSuccessStatusCode();
                using (var stream = await response.Content.ReadAsStreamAsync())
                {
                    var serializer = new XmlSerializer(typeof(UpdateManifest));
                    var manifest = serializer.Deserialize(stream) as UpdateManifest;

                    if (manifest == null)
                    {
                        throw new InvalidOperationException("The remote manifest is empty.");
                    }

                    manifest.Files = manifest.Files ?? new List<UpdateFileEntry>();
                    if (manifest.Files.Count == 0)
                    {
                        throw new InvalidOperationException("The remote manifest does not contain any files.");
                    }

                    return manifest;
                }
            }
        }

        private async Task LoadNewsAsync(UpdateManifest manifest, Uri manifestUri)
        {
            if (manifest == null)
            {
                SetNewsText(DefaultNewsText);
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
                SetNewsText("No realm news has been published yet.");
                return;
            }

            try
            {
                Uri newsUri;
                if (!Uri.TryCreate(manifest.BreakingNewsUrl, UriKind.Absolute, out newsUri))
                {
                    newsUri = new Uri(manifestUri, manifest.BreakingNewsUrl);
                }

                var newsText = await httpClient.GetStringAsync(newsUri);
                ReleaseNotesFeed releaseNotes;
                if (TryDeserializeReleaseNotes(newsText, out releaseNotes))
                {
                    ShowReleaseNotes(releaseNotes);
                }
                else
                {
                    SetNewsText(newsText);
                }

                AppendLog("Loaded realm news from " + newsUri + ".");
            }
            catch (Exception ex)
            {
                SetNewsText("Realm news could not be loaded right now.");
                AppendLog("News load failed: " + ex.Message);
            }
        }

        private async Task<List<PendingUpdateFile>> BuildPendingFileListAsync(string clientPath, Uri manifestUri, UpdateManifest manifest)
        {
            var results = new List<PendingUpdateFile>();
            var baseUri = GetBaseUri(manifestUri, manifest.BaseUrl);

            foreach (var entry in manifest.Files.Where(file => !string.IsNullOrWhiteSpace(file.FilePath)))
            {
                if (ContainsDotPrefixedPathSegment(entry.FilePath))
                {
                    AppendLog("Skipping manifest entry from dot-prefixed path: " + entry.FilePath + ".");
                    continue;
                }

                ValidateRelativePath(entry.FilePath);

                string localPath = GetSafeLocalPath(clientPath, entry.FilePath);
                string reason = await GetPendingReasonAsync(localPath, entry);

                if (reason == null)
                {
                    continue;
                }

                results.Add(new PendingUpdateFile
                {
                    Entry = entry,
                    LocalPath = localPath,
                    DownloadUri = GetRemoteFileUri(baseUri, entry.FilePath),
                    Reason = reason
                });
            }

            return results;
        }

        private async Task<string> GetPendingReasonAsync(string localPath, UpdateFileEntry entry)
        {
            if (!File.Exists(localPath))
            {
                return "Missing locally";
            }

            var info = new FileInfo(localPath);
            if (entry.Size > 0 && info.Length != entry.Size)
            {
                return "Size mismatch";
            }

            if (!string.IsNullOrWhiteSpace(entry.Sha256))
            {
                var localHash = await Task.Run(() => ComputeSha256(localPath));
                if (!string.Equals(localHash, entry.Sha256, StringComparison.OrdinalIgnoreCase))
                {
                    return "Hash mismatch";
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
            int value;

            if (totalBytes > 0)
            {
                value = (int)Math.Min(100, (downloadedBytes * 100L) / totalBytes);
            }
            else
            {
                value = (int)Math.Min(100, (completedFiles * 100L) / Math.Max(1, fileCount));
            }

            progressUpdate.Value = Math.Max(progressUpdate.Minimum, Math.Min(progressUpdate.Maximum, value));
        }

        private void UpdatePendingFilesList()
        {
            lstFilesToUpdate.BeginUpdate();
            lstFilesToUpdate.Items.Clear();

            foreach (var pendingFile in pendingFiles)
            {
                lstFilesToUpdate.Items.Add(pendingFile.Entry.FilePath + " - " + pendingFile.Reason);
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
            if (string.IsNullOrWhiteSpace(txtClientPath.Text) || !Directory.Exists(txtClientPath.Text))
            {
                return null;
            }

            string relativePath = currentManifest != null && !string.IsNullOrWhiteSpace(currentManifest.LaunchFile)
                ? currentManifest.LaunchFile
                : "Wow.exe";

            try
            {
                ValidateRelativePath(relativePath);
                string launchPath = GetSafeLocalPath(txtClientPath.Text.Trim(), relativePath);
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

            string clientPath = txtClientPath.Text.Trim();
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
            Text = ServerName + " Launcher";
            lblTitle.Text = ServerName;
            lblSubtitle.Text = "Wrath of the Lich King 3.3.5a launcher for patches, realm news, and your next adventure.";
            grpConnection.Text = ServerName + " Client";
            lblClientHint.Text = "Choose your 3.3.5a client folder, sync with " + ServerName + ", then enter the realm.";
            grpNews.Text = ServerName + " News";
            lblNewsHint.Text = "Breaking news and recent updates from the " + ServerName + " realm feed.";
            grpUpdates.Text = ServerName + " Operations";
            lblUpdatesHint.Text = "Track downloads, pending files, and live launcher activity for " + ServerName + ".";
            lblRemoteVersion.Text = "OLD MAN WARCRAFT BUILD";
        }

        private static void ConfigureActionButton(Button button, Color backColor, Color hoverColor, Color textColor)
        {
            button.BackColor = backColor;
            button.ForeColor = textColor;
            button.FlatAppearance.MouseOverBackColor = hoverColor;
            button.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(hoverColor);
            button.Cursor = Cursors.Hand;
        }

        private void SetStatusText(string statusText)
        {
            string normalized = (statusText ?? string.Empty).Trim();
            lblStatusValue.Text = string.IsNullOrWhiteSpace(normalized) ? "-" : normalized;

            string lowerStatus = normalized.ToLowerInvariant();
            if (lowerStatus.Contains("failed"))
            {
                lblStatusValue.ForeColor = Color.FromArgb(232, 109, 103);
            }
            else if (lowerStatus.Contains("up to date") || lowerStatus.Contains("complete") || lowerStatus.Contains("cleared") || lowerStatus == "ready")
            {
                lblStatusValue.ForeColor = Color.FromArgb(166, 208, 124);
            }
            else if (lowerStatus.Contains("check") || lowerStatus.Contains("download") || lowerStatus.Contains("update"))
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
            string displayVersion = string.IsNullOrWhiteSpace(versionText) ? "-" : versionText.Trim();
            lblRemoteVersionValue.Text = displayVersion;
            lblRemoteVersionValue.ForeColor = displayVersion == "-"
                ? Color.FromArgb(186, 184, 176)
                : Color.FromArgb(239, 203, 110);
        }

        private void SetNewsText(string message)
        {
            txtNews.Clear();
            AppendNewsHeading(ServerName + " News");
            AppendNewsBody(string.IsNullOrWhiteSpace(message) ? DefaultNewsText : message.Trim());
            txtNews.Select(0, 0);
        }

        private string FormatNewsText(string newsText)
        {
            if (string.IsNullOrWhiteSpace(newsText))
            {
                return "No realm news has been published yet.";
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

        private static string BuildReleaseNotesSummary(ReleaseNotesFeed releaseNotes)
        {
            var latest = releaseNotes.Latest ?? releaseNotes.History.FirstOrDefault();
            var builder = new StringBuilder();

            if (latest != null)
            {
                builder.Append("Breaking News");
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

                builder.AppendLine("Recent Updates");
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

            return builder.Length == 0 ? "No realm news has been published yet." : builder.ToString().Trim();
        }

        private static string BuildReleaseNoteMetadata(ReleaseNotesEntry entry)
        {
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(entry.CreatedAt))
            {
                parts.Add("Published: " + entry.CreatedAt.Trim());
            }

            if (!string.IsNullOrWhiteSpace(entry.CreatedBy))
            {
                parts.Add("By: " + entry.CreatedBy.Trim());
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
            txtNews.Clear();
            AppendNewsHeading(ServerName + " News");
            AppendNewsSpacing();

            var latest = releaseNotes.Latest ?? (releaseNotes.History ?? new List<ReleaseNotesEntry>()).FirstOrDefault();
            if (latest != null)
            {
                AppendNewsHeading("Breaking News");

                if (!string.IsNullOrWhiteSpace(latest.Version))
                {
                    AppendNewsSubheading("Build " + latest.Version.Trim());
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
                AppendNewsHeading("Recent Updates");

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
                SetNewsText("No realm news has been published yet.");
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
