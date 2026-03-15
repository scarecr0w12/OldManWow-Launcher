using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace Wow_Launcher
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            AppLocalization.ApplyCulture(AppLocalization.ResolveLanguageCode(Properties.Settings.Default.LanguageCode));
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (SelfUpdater.TryHandleSelfUpdate(args))
            {
                return;
            }

            Application.Run(new Form1());
        }
    }

    internal static class SelfUpdater
    {
        private const string ApplySelfUpdateArgument = "--apply-self-update";

        private static string LanguageCode
        {
            get { return AppLocalization.ResolveLanguageCode(Properties.Settings.Default.LanguageCode); }
        }

        public static string BuildArguments(string targetExecutablePath, int sourceProcessId)
        {
            return string.Format("{0} \"{1}\" {2}", ApplySelfUpdateArgument, targetExecutablePath, sourceProcessId);
        }

        public static bool TryHandleSelfUpdate(string[] args)
        {
            if (args == null || args.Length < 3 || !string.Equals(args[0], ApplySelfUpdateArgument, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string targetExecutablePath = args[1];
            int sourceProcessId;
            if (string.IsNullOrWhiteSpace(targetExecutablePath) || !int.TryParse(args[2], out sourceProcessId))
            {
                MessageBox.Show(
                    AppLocalization.Get("SelfUpdateInvalidArgumentsMessage", LanguageCode),
                    AppLocalization.Get("SelfUpdateFailedTitle", LanguageCode),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return true;
            }

            try
            {
                ApplySelfUpdate(targetExecutablePath, sourceProcessId);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    AppLocalization.Format("SelfUpdateFailedMessageFormat", LanguageCode, ex.Message),
                    AppLocalization.Get("SelfUpdateFailedTitle", LanguageCode),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }

            return true;
        }

        private static void ApplySelfUpdate(string targetExecutablePath, int sourceProcessId)
        {
            string sourceExecutablePath = Application.ExecutablePath;

            WaitForProcessExit(sourceProcessId);
            CopyFileWithRetries(sourceExecutablePath, targetExecutablePath);
            StartUpdatedLauncher(targetExecutablePath);
            ScheduleCleanup(sourceExecutablePath, targetExecutablePath);
        }

        private static void WaitForProcessExit(int processId)
        {
            try
            {
                using (var process = Process.GetProcessById(processId))
                {
                    if (!process.HasExited)
                    {
                        process.WaitForExit(15000);
                    }
                }
            }
            catch
            {
            }

            Thread.Sleep(500);
        }

        private static void CopyFileWithRetries(string sourceExecutablePath, string targetExecutablePath)
        {
            Exception lastError = null;

            for (int attempt = 0; attempt < 20; attempt++)
            {
                try
                {
                    string targetDirectory = Path.GetDirectoryName(targetExecutablePath);
                    if (!string.IsNullOrEmpty(targetDirectory))
                    {
                        Directory.CreateDirectory(targetDirectory);
                    }

                    File.Copy(sourceExecutablePath, targetExecutablePath, true);
                    return;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    Thread.Sleep(500);
                }
            }

            throw lastError ?? new IOException("The updated launcher could not replace the current executable.");
        }

        private static void StartUpdatedLauncher(string targetExecutablePath)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = targetExecutablePath,
                WorkingDirectory = Path.GetDirectoryName(targetExecutablePath) ?? Environment.CurrentDirectory,
                UseShellExecute = true
            };

            Process.Start(startInfo);
        }

        private static void ScheduleCleanup(string sourceExecutablePath, string targetExecutablePath)
        {
            if (string.Equals(Path.GetFullPath(sourceExecutablePath), Path.GetFullPath(targetExecutablePath), StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            string sourceDirectory = Path.GetDirectoryName(sourceExecutablePath);
            string cleanupCommand = string.Format("/C ping 127.0.0.1 -n 3 > nul & del /f /q \"{0}\"", sourceExecutablePath);

            if (!string.IsNullOrEmpty(sourceDirectory))
            {
                cleanupCommand += string.Format(" & rmdir /s /q \"{0}\"", sourceDirectory);
            }

            var cleanupStartInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(Environment.SystemDirectory, "cmd.exe"),
                Arguments = cleanupCommand,
                CreateNoWindow = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            Process.Start(cleanupStartInfo);
        }
    }
}
