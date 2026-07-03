/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Editor.UI;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using com.IvanMurzak.Unity.MCP.Utils;
using Microsoft.Extensions.Logging;
using R3;
using UnityEditor;
using UnityEngine;
using McpConsts = com.IvanMurzak.McpPlugin.Common.Consts;

namespace com.IvanMurzak.Unity.MCP.Editor
{
    using static com.IvanMurzak.McpPlugin.Common.Consts.MCP.Server;
    using Consts = McpPlugin.Common.Consts;
    using ILogger = Microsoft.Extensions.Logging.ILogger;
    using AiAgentConfig = McpPlugin.AgentConfig.AiAgentConfig;

    public enum McpServerStatus
    {
        Stopped,
        Starting,
        Running,
        Stopping,
        External,
        // The server binary is being downloaded/unpacked (issue #845). Distinct from Starting so the UI
        // can show an honest "Downloading server…" state instead of a misleading "Starting…" while the
        // process has not been launched yet.
        Downloading
    }

    /// <summary>
    /// Manages the MCP server binary and process lifecycle independently from UI.
    /// Provides cross-platform support for Windows, macOS, and Linux.
    /// </summary>
    [InitializeOnLoad]
    public static class McpServerManager
    {
        const string ProcessIdKey = "McpServerManager_ProcessId";
        const string McpServerProcessName = "gamedev-mcp-server";

        static readonly ILogger _logger = UnityLoggerFactory.LoggerFactory.CreateLogger(typeof(McpServerManager));
        static readonly ReactiveProperty<McpServerStatus> _serverStatus = new(McpServerStatus.Stopped);
        // Last server-binary download/extract/checksum failure reason, or null when there is no outstanding
        // failure. The editor window observes this to surface the error + a "Download / Retry server" button
        // (issue #845). Cleared at the start of every download attempt and on a confirmed-current binary.
        static readonly ReactiveProperty<string?> _lastDownloadError = new(null);
        static readonly object _processMutex = new();

        static Process? _serverProcess;

        public static ReadOnlyReactiveProperty<McpServerStatus> ServerStatus => _serverStatus;

        /// <summary>
        /// Last server-binary download failure reason (null when none). The AI Game Developer window
        /// subscribes to surface the failure + a retry button instead of silently dead-ending (issue #845).
        /// </summary>
        public static ReadOnlyReactiveProperty<string?> LastDownloadError => _lastDownloadError;

        public static bool IsRunning => _serverStatus.CurrentValue == McpServerStatus.Running;
        public static bool IsStarting => _serverStatus.CurrentValue == McpServerStatus.Starting;

        /// <summary>
        /// True when a verified, version-matching server binary is present on disk and can be launched
        /// without a download. The Start path (<c>HandleServerButton</c>) uses this to decide whether to
        /// recover a missing/outdated binary before launching (issue #845).
        /// </summary>
        public static bool IsBinaryReadyToStart() => IsBinaryExists() && IsVersionMatches();

        static McpServerManager()
        {
            // Register for editor quit to clean up the server process
            EditorApplication.quitting += OnEditorQuitting;

            // Check if server process is still running (e.g., after domain reload)
            EditorApplication.update += CheckExistingProcess;

            DownloadServerBinaryIfNeeded(unattended: true)
                .ContinueWith(task =>
                {
                    if (task.IsFaulted || !task.Result)
                        return; // Failed to download binaries, skip auto-start

                    if (!task.Result)
                        return; // No binaries available (either in CI or failed to download), skip auto-start

                    if (EnvironmentUtils.IsCi())
                        return; // Skip auto-start in CI environment

                    EditorApplication.update += StartServerIfNeeded;
                });
        }

        #region Binary Metadata

        /// <summary>
        /// The PINNED version of the shared <c>GameDev-MCP-Server</c> this plugin downloads and runs.
        /// The plugin version (<see cref="UnityMcpPlugin.Version"/>, 0.x) and the shared server version
        /// (8.x) DIVERGE — the server is released from its own repo
        /// (https://github.com/IvanMurzak/GameDev-MCP-Server) on its own cadence — so the download URL
        /// must NEVER be derived from the plugin version. Bumping the consumed server is an explicit
        /// plugin change: update THIS constant (and make sure the corresponding
        /// <c>v&lt;ServerVersion&gt;</c> release with all 7 RID zips exists on GameDev-MCP-Server
        /// BEFORE cutting a plugin release that pins it — otherwise the download 404s).
        /// </summary>
        public const string ServerVersion = "8.0.1";

        public const string ExecutableName = "gamedev-mcp-server";

        public static string McpServerName
            => string.IsNullOrEmpty(Application.productName)
                ? "Unity Unknown"
                : $"Unity {Application.productName}";

        public static string OperationSystem =>
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "win" :
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "osx" :
            RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "linux" :
            "unknown";

        public static string CpuArch => RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X86 => "x86",
            Architecture.X64 => "x64",
            Architecture.Arm => "arm",
            Architecture.Arm64 => "arm64",
            _ => "unknown"
        };

        public static string PlatformName => $"{OperationSystem}-{CpuArch}";

        // Server executable file name
        // Sample (mac linux): gamedev-mcp-server
        // Sample   (windows): gamedev-mcp-server.exe
        public static string ExecutableFullName
            => ExecutableName.ToLowerInvariant() + (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? ".exe"
                : string.Empty);

        // Full path to the server executable
        // Sample (mac linux): ../Library/mcp-server
        // Sample   (windows): ../Library/mcp-server
        public static string ExecutableFolderRootPath
            => Path.GetFullPath(
                Path.Combine(
                    Application.dataPath,
                    "../Library",
                    "mcp-server"
                )
            );

        // Full path to the server executable
        // Sample (mac linux): ../Library/mcp-server/osx-x64
        // Sample   (windows): ../Library/mcp-server/win-x64
        public static string ExecutableFolderPath
            => Path.GetFullPath(
                Path.Combine(
                    ExecutableFolderRootPath,
                    PlatformName
                )
            );

        // Full path to the server executable
        // Sample (mac linux): ../Library/mcp-server/osx-x64/gamedev-mcp-server
        // Sample   (windows): ../Library/mcp-server/win-x64/gamedev-mcp-server.exe
        public static string ExecutableFullPath
            => Path.GetFullPath(
                Path.Combine(
                    ExecutableFolderPath,
                    ExecutableFullName
                )
            );

        public static string VersionFullPath
            => Path.GetFullPath(
                Path.Combine(
                    ExecutableFolderPath,
                    "version"
                )
            );

        /// <summary>
        /// The Git release TAG for a server version: the version with a leading <c>v</c>
        /// (e.g. <c>8.0.0</c> → <c>v8.0.0</c>). GameDev-MCP-Server tags every release
        /// <c>v&lt;version&gt;</c> and the per-platform server zips are attached to THAT tag — so the
        /// download path MUST use the v-prefixed tag, never the bare version (a bare-version path
        /// 404s). Already-v-prefixed input is passed through unchanged so a caller cannot
        /// accidentally double-prefix.
        /// </summary>
        public static string ServerReleaseTag(string serverVersion)
        {
            var version = (serverVersion ?? string.Empty).Trim();
            return version.StartsWith("v", StringComparison.Ordinal) ? version : "v" + version;
        }

        /// <summary>
        /// The release-asset zip NAME for the current platform: <c>gamedev-mcp-server-&lt;rid&gt;.zip</c>
        /// (e.g. <c>gamedev-mcp-server-win-x64.zip</c>). This is the exact key looked up in the release's
        /// <c>SHA256SUMS</c> integrity manifest (exact-key Ordinal — see <see cref="McpServerChecksum"/>), and
        /// the trailing segment of <see cref="ExecutableZipUrl"/> — so the verified asset name can never drift
        /// from the downloaded asset name.
        /// </summary>
        public static string ExecutableZipName
            => $"{ExecutableName.ToLowerInvariant()}-{PlatformName}.zip";

        /// <summary>
        /// The download URL of the shared GameDev-MCP-Server release zip for the current platform,
        /// pinned by <see cref="ServerVersion"/> — NEVER the plugin version (the two diverge).
        /// </summary>
        public static string ExecutableZipUrl
            => $"https://github.com/IvanMurzak/GameDev-MCP-Server/releases/download/{ServerReleaseTag(ServerVersion)}/{ExecutableZipName}";

        #endregion // Binary Metadata

        #region Binary Lifecycle

        public static bool IsBinaryExists()
        {
            if (string.IsNullOrEmpty(ExecutableFullPath))
                return false;

            return File.Exists(ExecutableFullPath);
        }

        public static string? GetBinaryVersion()
        {
            if (!File.Exists(VersionFullPath))
                return null;

            return File.ReadAllText(VersionFullPath);
        }

        public static bool IsVersionMatches()
        {
            var binaryVersion = GetBinaryVersion();
            if (binaryVersion == null)
                return false;

            // Compared against the pinned shared-server version, NOT the plugin version —
            // the cached binary is a GameDev-MCP-Server release.
            return binaryVersion == ServerVersion;
        }

        /// <param name="interactive">
        /// When true (menu / user-initiated paths) a blocking <see cref="EditorUtility.DisplayDialog"/> asks the
        /// user to retry/skip if the folder can't be deleted (e.g. the server is still holding a file lock).
        /// When false (the unattended <c>[InitializeOnLoad]</c> / package-update download path) the blocking
        /// dialog is SKIPPED — after the silent retries the failure is rethrown so the caller surfaces it via the
        /// non-modal failure popup + retry button instead of freezing editor startup behind a modal (issue #845).
        /// </param>
        public static bool DeleteBinaryFolderIfExists(bool interactive = true)
        {
            if (Directory.Exists(ExecutableFolderRootPath))
            {
                // Intentional infinite loop (interactive path only):
                // - Deletion can fail while the MCP server binaries are in use (e.g., server still running).
                // - On the first failure, we automatically attempt to stop the server process via McpServerManager.
                // - The retry/exit behavior is fully controlled by the user via the dialog below.
                // - We do not impose a fixed maximum retry count so the user can take as long as needed
                //   to shut down their MCP client and release file locks before trying again.
                // - The loop terminates when the user selects "Skip", at which point the exception is rethrown.
                // In the unattended path the blocking dialog is skipped: after the silent retries the exception
                // is rethrown so the download path fails-loud (non-modal popup) instead of blocking startup.
                var silentRetries = 0;
                while (true)
                {
                    try
                    {
                        Directory.Delete(ExecutableFolderRootPath, recursive: true);
                        UnityEngine.Debug.Log($"Deleted existing MCP server folder: <color=orange>{ExecutableFolderRootPath}</color>");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        // First failure: try to stop the running server process that may be locking files
                        if (silentRetries == 0)
                        {
                            silentRetries++;
                            UnityEngine.Debug.Log($"Failed to delete MCP server folder. Attempting to stop the server process...");
                            try
                            {
                                if (!StopServer(force: true))
                                {
                                    UnityEngine.Debug.LogWarning($"No running MCP server process found to stop.");
                                }
                                else
                                {
                                    UnityEngine.Debug.Log($"Stop signal sent to MCP server process. Retrying deletion...");
                                    Thread.Sleep(2000); // Wait a moment for the process to exit and release file locks
                                }
                            }
                            catch (Exception stopEx)
                            {
                                UnityEngine.Debug.LogWarning($"Failed to stop MCP server: {stopEx.Message}");
                            }
                            continue; // Retry deletion after stopping the server
                        }

                        // Second failure: retry once more silently (OS may need time to release file locks)
                        if (silentRetries <= 1)
                        {
                            silentRetries++;
                            continue;
                        }

                        // Unattended path: never block startup behind a modal — rethrow so the caller
                        // surfaces the failure via the non-modal popup + retry button (issue #845).
                        if (!interactive)
                        {
                            UnityEngine.Debug.LogError(
                                $"Failed to delete MCP server folder (unattended): {ex.Message}");
                            throw;
                        }

                        var retry = EditorUtility.DisplayDialog(
                            title: "Failed to Delete MCP Server Binaries",
                            message: $"The current gamedev-mcp-server binaries can't be deleted. " +
                                $"This is very likely because the MCP server is currently running.\n\n" +
                                $"Please close your MCP client to make sure the server is not running, then click \"Retry\".\n\n" +
                                $"Path: {ExecutableFolderRootPath}\n\n" +
                                $"Error: {ex.Message}",
                            ok: "Retry",
                            cancel: "Skip"
                        );

                        if (!retry)
                        {
                            throw;
                        }
                        // If retry is true, loop continues and tries again
                    }
                }
            }
            return false;
        }

        /// <param name="unattended">
        /// When true (the <c>[InitializeOnLoad]</c> editor-startup path and the package-update re-check) the
        /// download runs without any blocking modal and without the result popup — failures are surfaced only
        /// through <see cref="LastDownloadError"/> (the in-window error + retry button). When false (menu /
        /// Start button / retry button — user-initiated) the result popup is shown and the delete step may
        /// prompt interactively. See <see cref="DownloadAndUnpackBinary"/>.
        /// </param>
        public static Task<bool> DownloadServerBinaryIfNeeded(bool unattended = false)
        {
            if (EnvironmentUtils.IsCi())
            {
                // Ignore in CI environment
                UnityEngine.Debug.Log($"Ignore MCP server downloading in CI environment");
                return Task.FromResult(false);
            }

            // Deduplication guard (issue #845): UPM fires MULTIPLE registeredPackages events for a single
            // package install/update, and ServerBinaryUpdateWatcher's _isRechecking flag resets synchronously
            // (in its finally) before the fire-and-forget download Task completes. Without this guard, each
            // event past the flag would start its own DownloadAndUnpackBinary, and the two would race at the
            // shared archive path (corrupting the zip → checksum failure) and at PublishStagedBinary (one Move
            // deleting the folder the other just installed). The Downloading status the lifecycle machine
            // already tracks is the real idempotency guard: if a download is in flight, let it complete.
            if (_serverStatus.CurrentValue == McpServerStatus.Downloading)
                return Task.FromResult(true); // download already in progress; let it complete

            if (IsBinaryExists() && IsVersionMatches())
            {
                // Binary is present and current — clear any stale failure so the window hides the error/retry UI.
                _lastDownloadError.Value = null;
                return Task.FromResult(true);
            }

            return DownloadAndUnpackBinary(unattended);
        }

        /// <summary>
        /// Downloads, verifies, and ATOMICALLY publishes the pinned GameDev-MCP-Server binary for this RID.
        ///
        /// <para>Atomicity (issue #845): the previous flow wiped the cache folder, pre-created an EMPTY
        /// <c>Library/mcp-server/&lt;rid&gt;/</c>, then downloaded + extracted + wrote the version file LAST — so an
        /// interrupted run (process kill, crash, cancelled domain reload) left an empty per-RID folder behind,
        /// which then read as "binary present but version missing" forever while the UI hung on "Starting…".
        /// This implementation instead extracts into a SAME-VOLUME staging folder, fully prepares the payload
        /// there (binary + sidecars + exec bit + version marker), and only then performs a single
        /// <see cref="Directory.Move"/> rename into the per-RID cache folder. The destination folder therefore
        /// never exists in a partial state: it is either absent (download not finished) or complete. The old
        /// working binary is left untouched until the replacement is fully staged + verified.</para>
        /// </summary>
        /// <param name="unattended">
        /// When true ([InitializeOnLoad] startup / package-update re-check) no blocking modal and no result
        /// popup are shown — failures surface only via <see cref="LastDownloadError"/> (the in-window error +
        /// retry button) so editor startup is never blocked and is not spammed with a popup on every reload.
        /// When false (menu / Start button / retry button — user-initiated) the result popup is shown on BOTH
        /// success and EVERY failure branch, and the delete step may prompt interactively.
        /// </param>
        public static async Task<bool> DownloadAndUnpackBinary(bool unattended = false)
        {
            UnityEngine.Debug.Log($"Downloading GameDev-MCP-Server binary from: <color=yellow>{ExecutableZipUrl}</color>");

            // Clear any prior failure + reflect the in-progress download in the status machine.
            _lastDownloadError.Value = null;
            SetDownloadingStatus();

            string? stagingRoot = null;
            string? archiveFilePath = null;
            try
            {
                var previousKeepServerRunning = UnityMcpPluginEditor.KeepServerRunning;

                archiveFilePath = Path.GetFullPath($"{Application.temporaryCachePath}/{ExecutableName.ToLowerInvariant()}-{PlatformName}-{ServerVersion}.zip");
                UnityEngine.Debug.Log($"Temporary archive file path: <color=yellow>{archiveFilePath}</color>");

                // Download the zip file from the GitHub release notes
                using (var client = new WebClient())
                {
                    await client.DownloadFileTaskAsync(ExecutableZipUrl, archiveFilePath);
                }

                // FAIL-CLOSED INTEGRITY GATE (verify-before-execute). The zip is on disk but UNTRUSTED. Before
                // extracting or launching it, download the release's SHA256SUMS manifest (sibling of the zip URL
                // under the same v<ServerVersion> tag), compute the downloaded zip's SHA256 (pure BCL), and
                // compare against the manifest entry for THIS RID. On MISMATCH / MISSING entry / unparsable-or-
                // unfetchable manifest we delete the temp zip and return WITHOUT extracting or launching — an
                // unverified binary must NEVER be executed (a compromised release asset or a trusted-CA MITM
                // would otherwise yield arbitrary code execution; issue #841).
                if (!await VerifyDownloadedArchive(archiveFilePath, ServerVersion, ExecutableZipName))
                {
                    try { File.Delete(archiveFilePath); } catch { /* best effort */ }
                    return FailDownload(
                        "Checksum verification failed for the downloaded server binary (see logs).", unattended);
                }

                // Unpack zip archive into a SAME-VOLUME staging root (a sibling of the cache root, so the final
                // publish is an atomic rename, never a cross-volume copy that could be interrupted mid-write).
                // The shared GameDev-MCP-Server release zips are NOT layout-uniform: the win zips are FLAT
                // (gamedev-mcp-server.exe + its sidecar files at the zip root) while the osx/linux zips wrap
                // everything in a <rid>/ folder. Extract, FIND the binary wherever it landed, prepare the
                // payload folder, then atomically Move it into the per-platform cache folder — so BOTH layouts
                // (and any future re-arrangement) resolve correctly. The sidecar files (appsettings.json,
                // NLog.config, server.json, ...) are LOAD-BEARING and must travel with the binary.
                stagingRoot = Path.GetFullPath($"{ExecutableFolderRootPath}-staging-{Guid.NewGuid():N}");
                var extractFolder = Path.Combine(stagingRoot, "extract");
                Directory.CreateDirectory(extractFolder);
                UnityEngine.Debug.Log($"Unpacking GameDev-MCP-Server binary to staging: <color=yellow>{extractFolder}</color>");
                ZipFile.ExtractToDirectory(archiveFilePath, extractFolder, overwriteFiles: true);
                try { File.Delete(archiveFilePath); } catch { /* best effort */ }

                var extractedBinary = FindExtractedBinary(extractFolder, ExecutableFullName);
                if (extractedBinary == null)
                {
                    return FailDownload(
                        $"'{ExecutableFullName}' not found inside the downloaded zip.", unattended);
                }

                // The folder that holds the binary + its sidecars is the payload we publish.
                var payloadFolder = Path.GetDirectoryName(extractedBinary)!;

                // Set executable permission on macOS and Linux BEFORE publishing, so the published payload is
                // launch-ready the instant it appears under the cache folder.
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    UnityEngine.Debug.Log($"Setting executable permission for: <color=green>{extractedBinary}</color>");
                    UnixUtils.Set0755(extractedBinary);
                }

                // Write the version marker INTO the staged payload, so the published per-RID folder is complete
                // the instant it appears — there is no window where the binary exists without its version file.
                File.WriteAllText(Path.Combine(payloadFolder, "version"), ServerVersion);

                // Stop the running server + remove the OLD cache folder. Only now do we touch the live binary;
                // everything above operated on staging, so a failure before this point leaves the working copy
                // intact. Unattended path never blocks behind a modal (see DeleteBinaryFolderIfExists).
                DeleteBinaryFolderIfExists(interactive: !unattended);

                // Atomic publish: a single same-volume rename of the fully-prepared payload into the per-RID
                // cache folder. Either it lands complete or not at all.
                PublishStagedBinary(payloadFolder, ExecutableFolderPath);

                if (!File.Exists(ExecutableFullPath))
                {
                    return FailDownload(
                        $"Server binary missing after publish at: {ExecutableFullPath}", unattended);
                }

                var success = IsBinaryExists() && IsVersionMatches();
                if (!success)
                {
                    return FailDownload(
                        "The published server binary failed the post-publish version check.", unattended);
                }

                UnityEngine.Debug.Log($"Downloaded and unpacked GameDev-MCP-Server binary to: <color=green>{ExecutableFullPath}</color>");
                UnityEngine.Debug.Log($"MCP server version file created at: <color=green><b>COMPLETED</b></color>");

                if (previousKeepServerRunning && IsAutoStartAllowedForMode(UnityMcpPluginEditor.ConnectionMode))
                {
                    // StartServer() moves the status machine Downloading -> Starting. If it early-returns false
                    // on its !IsBinaryExists() path it never wrote Starting, so the status is still Downloading
                    // — reset it to Stopped (no-op for the Running/Starting/Stopping early-return) so the UI does
                    // not hang on "Downloading server…" with the Start button permanently disabled (issue #845).
                    if (!StartServer())
                    {
                        UnityEngine.Debug.LogError($"Failed to start MCP server after updating binary. Please try starting the server manually.");
                        ResetDownloadingToStopped();
                    }
                }
                else
                {
                    if (previousKeepServerRunning)
                        _logger.LogDebug("DownloadAndUnpackBinary: Cloud mode active, skipping local server auto-start after binary update");
                    ResetDownloadingToStopped();
                }

                if (!unattended)
                    ShowUpdateResultPopup(success: true);

                return true;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
                return FailDownload($"Failed to download and unpack server binary: {ex.Message}", unattended);
            }
            finally
            {
                if (stagingRoot != null)
                {
                    try { if (Directory.Exists(stagingRoot)) Directory.Delete(stagingRoot, recursive: true); }
                    catch { /* best effort */ }
                }
                // Clean up the downloaded temp zip on EVERY exit path. The inline File.Delete calls above
                // free it on the happy/checksum-fail paths, but if ZipFile.ExtractToDirectory (or the download
                // itself) throws, neither runs and the zip would leak in Application.temporaryCachePath. The
                // File.Exists guard makes this a no-op when the inline delete already removed it.
                if (archiveFilePath != null)
                {
                    try { if (File.Exists(archiveFilePath)) File.Delete(archiveFilePath); }
                    catch { /* best effort */ }
                }
            }
        }

        /// <summary>
        /// Records a download failure: logs it, stores the reason in <see cref="LastDownloadError"/> (so the
        /// window shows the error + retry button), returns the status machine to Stopped, and — for
        /// user-initiated (non-unattended) calls — shows the "Update Failed" popup. Always returns false so it
        /// can be used as the single return expression of every failure branch.
        /// </summary>
        static bool FailDownload(string reason, bool unattended)
        {
            UnityEngine.Debug.LogError($"MCP server binary download failed: {reason}");
            _lastDownloadError.Value = reason;
            ResetDownloadingToStopped();
            if (!unattended)
                ShowUpdateResultPopup(success: false);
            return false;
        }

        /// <summary>Moves the status machine into Downloading from an idle state (Stopped/Downloading only).</summary>
        static void SetDownloadingStatus()
        {
            var current = _serverStatus.CurrentValue;
            if (current == McpServerStatus.Stopped || current == McpServerStatus.Downloading)
                _serverStatus.Value = McpServerStatus.Downloading;
        }

        /// <summary>Returns the status machine to Stopped, but ONLY if it is still Downloading (so it never
        /// stomps a Starting/Running/Stopping state that a concurrent path may have moved it to).</summary>
        static void ResetDownloadingToStopped()
        {
            if (_serverStatus.CurrentValue == McpServerStatus.Downloading)
                _serverStatus.Value = McpServerStatus.Stopped;
        }

        /// <summary>Shows the non-modal server-binary download result popup (success or failure).</summary>
        static void ShowUpdateResultPopup(bool success)
        {
            NotificationPopupWindow.Show(
                windowTitle: success ? "Updated" : "Update Failed",
                height: 235,
                minHeight: 235,
                title: success ? "Server Binary Updated" : "Server Binary Update Failed",
                message: success
                    ? "The MCP server binary was successfully downloaded and updated. \n\n" +
                        $"Version: {GetBinaryVersion()}\n\n" +
                        "You may need to restart your AI agent to reconnect to the updated server."
                    : "Failed to download and update the MCP server binary. Please check the logs for details.");
        }

        /// <summary>
        /// Atomically publishes a fully-prepared staged payload folder into <paramref name="destFolder"/> via a
        /// single same-volume <see cref="Directory.Move"/>. Ensures the destination's parent exists and removes
        /// any existing destination first (Directory.Move requires the target to not exist). The caller
        /// guarantees <paramref name="stagedFolder"/> and <paramref name="destFolder"/> share a volume (staging
        /// is a sibling of the cache root) so the rename is atomic, never a partial cross-volume copy.
        /// </summary>
        internal static void PublishStagedBinary(string stagedFolder, string destFolder)
        {
            var parent = Path.GetDirectoryName(destFolder);
            if (!string.IsNullOrEmpty(parent) && !Directory.Exists(parent))
                Directory.CreateDirectory(parent);

            if (Directory.Exists(destFolder))
                Directory.Delete(destFolder, recursive: true);

            Directory.Move(stagedFolder, destFolder);
        }

        /// <summary>
        /// Locate the extracted server binary under the staging folder, wherever the zip layout put it —
        /// at the root (the FLAT win zips) or nested in a <c>&lt;rid&gt;/</c> folder (the osx/linux zips).
        /// Prefers the SHALLOWEST match so a hypothetical nested duplicate cannot shadow the real binary.
        /// Returns null when the zip contains no file with the expected name.
        /// </summary>
        internal static string? FindExtractedBinary(string stagingFolder, string executableFileName)
        {
            string? best = null;
            var bestDepth = int.MaxValue;
            foreach (var candidate in Directory.GetFiles(stagingFolder, executableFileName, SearchOption.AllDirectories))
            {
                var relative = candidate.Substring(stagingFolder.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var depth = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Length;
                if (depth < bestDepth)
                {
                    best = candidate;
                    bestDepth = depth;
                }
            }
            return best;
        }

        /// <summary>
        /// The number of attempts for the SHA256SUMS manifest fetch (1 initial + retries) before we
        /// fail-closed. A TRANSIENT network error on the manifest fetch is retried (the binary is already
        /// downloaded; only the integrity manifest is missing) — but a persistent failure NEVER falls through
        /// to executing an unverified binary.
        /// </summary>
        const int Sha256SumsFetchAttempts = 3;

        /// <summary>Backoff between SHA256SUMS fetch attempts.</summary>
        static readonly TimeSpan Sha256SumsRetryDelay = TimeSpan.FromSeconds(1.0);

        /// <summary>
        /// Fail-closed verify-before-execute gate. Downloads the release's <c>SHA256SUMS</c> manifest (with a
        /// bounded transient-retry), computes the downloaded zip's SHA256 (pure BCL — the same
        /// <c>SHA256.Create().ComputeHash</c> idiom the plugin already uses in <c>UnityMcpPlugin</c>, so it is
        /// .NET-Standard-2.1-safe on the Unity 2022.3 floor; no new deps), and compares against the manifest
        /// entry for <paramref name="assetZipName"/> via the pure-managed
        /// <see cref="McpServerChecksum.VerifyZipChecksum"/>. Returns true ONLY when the digest matched the
        /// manifest. Every failure path — a manifest we could not fetch after all retries, an unparsable
        /// manifest, a missing entry, or a digest mismatch — returns false with a clear, actionable error so
        /// the caller skips extraction/launch. Never throws.
        /// </summary>
        static async Task<bool> VerifyDownloadedArchive(string archiveFilePath, string serverVersion, string assetZipName)
        {
            var sumsUrl = McpServerChecksum.Sha256SumsUrl(serverVersion);

            // 1) Fetch the integrity manifest (bounded transient-retry). A null result means every attempt
            //    failed — fail-closed (do NOT execute an unverified binary).
            var sha256SumsText = await FetchSha256SumsText(sumsUrl);
            if (sha256SumsText == null)
            {
                UnityEngine.Debug.LogError(
                    $"Refusing to launch MCP server: could not download the {McpServerChecksum.Sha256SumsAssetName} " +
                    $"integrity manifest from {sumsUrl} after {Sha256SumsFetchAttempts} attempt(s). " +
                    "The downloaded binary was NOT verified and will not be executed (fail-closed).");
                return false;
            }

            // 2) Compute the downloaded zip's SHA256 (pure BCL; .NET-Standard-2.1-safe on the Unity 2022.3
            //    floor — SHA256.HashDataAsync / Convert.ToHexString are .NET 5+ and would not compile there).
            string actualHexDigest;
            try
            {
                byte[] hashBytes;
                using (var sha256 = SHA256.Create())
                using (var zipStream = File.OpenRead(archiveFilePath))
                {
                    hashBytes = sha256.ComputeHash(zipStream);
                }
                actualHexDigest = BitConverter.ToString(hashBytes).Replace("-", string.Empty); // upper-case hex; compare is case-insensitive
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError(
                    $"Refusing to launch MCP server: failed to compute the downloaded zip's SHA256: {ex.Message}");
                return false;
            }

            // 3) Parse + compare via the pure-managed verifier (unit-tested with no editor).
            var verdict = McpServerChecksum.VerifyZipChecksum(sha256SumsText, assetZipName, actualHexDigest);
            if (verdict != McpServerChecksum.ChecksumVerdict.Verified)
            {
                UnityEngine.Debug.LogError(
                    $"Refusing to launch MCP server: {McpServerChecksum.ChecksumFailureReason(verdict, assetZipName)}. " +
                    "The binary will not be extracted or executed (fail-closed).");
                return false;
            }

            UnityEngine.Debug.Log(
                $"Verified '{assetZipName}' against {McpServerChecksum.Sha256SumsAssetName} (SHA256 OK).");
            return true;
        }

        /// <summary>
        /// Download the <c>SHA256SUMS</c> manifest text with a bounded transient-retry. Returns the manifest
        /// body, or null when every attempt failed (the fail-closed signal). The manifest is small text — read
        /// it fully into a string. Never throws.
        /// </summary>
        static async Task<string?> FetchSha256SumsText(string sumsUrl)
        {
            for (var attempt = 1; attempt <= Sha256SumsFetchAttempts; attempt++)
            {
                try
                {
                    using var client = new HttpClient();
                    using var response = await client.GetAsync(sumsUrl);
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadAsStringAsync();
                }
                catch (Exception ex)
                {
                    if (attempt < Sha256SumsFetchAttempts)
                    {
                        UnityEngine.Debug.LogWarning(
                            $"{McpServerChecksum.Sha256SumsAssetName} fetch attempt {attempt}/{Sha256SumsFetchAttempts} " +
                            $"failed ({ex.Message}); retrying…");
                        try { await Task.Delay(Sha256SumsRetryDelay); } catch { /* ignore */ }
                    }
                    else
                    {
                        UnityEngine.Debug.LogWarning(
                            $"{McpServerChecksum.Sha256SumsAssetName} fetch attempt {attempt}/{Sha256SumsFetchAttempts} " +
                            $"failed ({ex.Message}).");
                    }
                }
            }

            return null;
        }

        #endregion // Binary Lifecycle

        #region Client Configuration

        /// <summary>
        /// Generates a JSON configuration for stdio transport.
        /// <code>
        /// {
        ///   "mcpServers": {
        ///     "Unity ProjectName": {
        ///       "type": "...",    // optional, only if provided
        ///       "command": "path/to/gamedev-mcp-server",
        ///       "args": ["port=...", "plugin-timeout=...", "client-transport=stdio" /*, "token=..." if auth required */]
        ///     }
        ///   }
        /// }
        /// </code>
        /// </summary>
        public static JsonNode RawJsonConfigurationStdio(
            int port,
            string bodyPath = "mcpServers",
            int timeoutMs = Consts.Hub.DefaultTimeoutMs,
            string? type = null)
        {
            var pathSegments = BodyPathSegments(bodyPath);

            // Build innermost content first
            var serverConfig = new JsonObject();

            if (type != null)
                serverConfig["type"] = type;

            serverConfig["command"] = ExecutableFullPath.Replace('\\', '/');

            var args = new JsonArray
            {
                $"{Args.Port}={port}",
                $"{Args.PluginTimeout}={timeoutMs}",
                $"{Args.ClientTransportMethod}={TransportMethod.stdio}",
                $"{Args.Authorization}={UnityMcpPluginEditor.AuthOption}"
            };

            var authRequired = UnityMcpPluginEditor.AuthOption == AuthOption.required;
            if (authRequired && !string.IsNullOrEmpty(UnityMcpPluginEditor.Token))
                args.Add($"{Args.Token}={UnityMcpPluginEditor.Token}");

            serverConfig["args"] = args;

            var innerContent = new JsonObject
            {
                [AiAgentConfig.DefaultMcpServerName] = serverConfig
            };

            // Build nested structure from innermost to outermost
            var result = innerContent;
            for (int i = pathSegments.Length - 1; i >= 0; i--)
            {
                result = new JsonObject { [pathSegments[i]] = result };
            }

            return result;
        }

        /// <summary>
        /// Generates a JSON configuration for HTTP transport.
        /// <code>
        /// {
        ///   "mcpServers": {
        ///     "Unity ProjectName": {
        ///       "type": "...",  // optional, only if provided
        ///       "url": "http://localhost:port",
        ///      "headers": {     // only if token is provided
        ///        "Authorization": "Bearer token"
        ///      }
        ///     }
        ///   }
        /// }
        /// </code>
        /// </summary>
        public static JsonNode RawJsonConfigurationHttp(
            string url,
            string bodyPath = "mcpServers",
            string? type = null)
        {
            var pathSegments = BodyPathSegments(bodyPath);

            // Build innermost content first
            var serverConfig = new JsonObject();

            if (type != null)
                serverConfig["type"] = type;

            serverConfig["url"] = url;

            var authRequired = UnityMcpPluginEditor.AuthOption == AuthOption.required;
            if (authRequired && !string.IsNullOrEmpty(UnityMcpPluginEditor.Token))
            {
                serverConfig["headers"] = new JsonObject
                {
                    ["Authorization"] = $"Bearer {UnityMcpPluginEditor.Token}"
                };
            }

            var innerContent = new JsonObject
            {
                [AiAgentConfig.DefaultMcpServerName] = serverConfig
            };

            // Build nested structure from innermost to outermost
            var result = innerContent;
            for (int i = pathSegments.Length - 1; i >= 0; i--)
            {
                result = new JsonObject { [pathSegments[i]] = result };
            }

            return result;
        }

        public static string DockerSetupRunCommand()
        {
            var dockerPortMapping = $"-p {UnityMcpPluginEditor.Port}:{UnityMcpPluginEditor.Port}";
            var dockerEnvVars =
                $"-e {Env.ClientTransportMethod}={TransportMethod.streamableHttp} " +
                $"-e {Env.Port}={UnityMcpPluginEditor.Port} " +
                $"-e {Env.PluginTimeout}={UnityMcpPluginEditor.TimeoutMs} " +
                $"-e {Env.Authorization}={UnityMcpPluginEditor.AuthOption}";

            var authRequired = UnityMcpPluginEditor.AuthOption == AuthOption.required;
            var token = UnityMcpPluginEditor.Token;
            if (authRequired && !string.IsNullOrEmpty(token))
                dockerEnvVars += $" -e {Env.Token}={token}";

            var dockerContainer = $"--name {ExecutableName}-{UnityMcpPluginEditor.Port}";
            // The shared GameDev-MCP-Server Docker image, tagged by the pinned ServerVersion
            // (NOT the plugin version — the two diverge).
            var dockerImage = $"aigamedeveloper/mcp-server:{ServerVersion}";
            return $"docker run -d {dockerPortMapping} {dockerEnvVars} {dockerContainer} {dockerImage}";
        }

        public static string DockerRunCommand()
        {
            return $"docker start {ExecutableName}-{UnityMcpPluginEditor.Port}";
        }

        public static string DockerStopCommand()
        {
            return $"docker stop {ExecutableName}-{UnityMcpPluginEditor.Port}";
        }

        public static string DockerRemoveCommand()
        {
            return $"docker rm {ExecutableName}-{UnityMcpPluginEditor.Port}";
        }

        #endregion // Client Configuration

        #region Process Lifecycle

        static void CheckExistingProcess()
        {
            EditorApplication.update -= CheckExistingProcess;
            // Try to find an existing server process by checking if our tracked PID is still running
            // This helps maintain state across domain reloads
            var savedPid = EditorPrefs.GetInt(ProcessIdKey, -1);
            if (savedPid > 0)
            {
                try
                {
                    var process = Process.GetProcessById(savedPid);
                    if (process != null && !process.HasExited)
                    {
                        var processName = process.ProcessName.ToLowerInvariant();
                        if (processName.Contains(McpServerProcessName))
                        {
                            _serverProcess = process;
                            _serverStatus.Value = McpServerStatus.Running;
                            _logger.LogInformation("Reconnected to existing MCP server process (PID: {pid})", savedPid);

                            // Re-attach exit handler
                            process.EnableRaisingEvents = true;
                            process.Exited += OnProcessExited;

                            // Schedule verification check to detect if process crashes shortly after reconnection
                            ScheduleStartupVerification(savedPid);
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("Could not reconnect to previous process: {message}", ex.Message);
                }

                // Clear stale PID
                EditorPrefs.DeleteKey(ProcessIdKey);
            }
        }

        static void OnEditorQuitting()
        {
            StopServer(force: true);
        }

        public static bool StartServer()
        {
            lock (_processMutex)
            {
                if (_serverStatus.CurrentValue == McpServerStatus.Running ||
                    _serverStatus.CurrentValue == McpServerStatus.Starting ||
                    _serverStatus.CurrentValue == McpServerStatus.Stopping)
                {
                    _logger.LogWarning("MCP server is already {status}", _serverStatus.CurrentValue);
                    return false;
                }

                if (!IsBinaryExists())
                {
                    _logger.LogError("MCP server binary not found at: {path}", ExecutableFullPath);
                    return false;
                }

                _serverStatus.Value = McpServerStatus.Starting;

                // Kill any orphaned server processes to free the port
                KillOrphanedServerProcesses();

                try
                {
                    var executablePath = ExecutableFullPath;
                    var arguments = BuildArguments();

                    _logger.LogInformation("Starting MCP server: {path} {args}", executablePath, arguments);

                    var startInfo = new ProcessStartInfo
                    {
                        FileName = executablePath,
                        Arguments = arguments,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        WorkingDirectory = ExecutableFolderPath
                    };

                    // Set executable permissions on Unix-like systems
                    if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        UnixUtils.Set0755(executablePath);
                    }

                    _serverProcess = new Process
                    {
                        StartInfo = startInfo,
                        EnableRaisingEvents = true
                    };
                    _serverProcess.Exited += OnProcessExited;
                    _serverProcess.OutputDataReceived += OnOutputDataReceived;
                    _serverProcess.ErrorDataReceived += OnErrorDataReceived;

                    if (!_serverProcess.Start())
                    {
                        _logger.LogError("Failed to start MCP server process");
                        CleanupProcess();
                        return false;
                    }

                    _serverProcess.BeginOutputReadLine();
                    _serverProcess.BeginErrorReadLine();

                    // Save PID for reconnection after domain reload
                    EditorPrefs.SetInt(ProcessIdKey, _serverProcess.Id);

                    // Keep status as Starting - it will be set to Running after verification
                    _logger.LogInformation("MCP server process started (PID: {pid}), awaiting verification...", _serverProcess.Id);

                    // Schedule a delayed check to verify the process is still running
                    // This catches early crashes that might not trigger the Exited event reliably
                    // Status will be set to Running only after successful verification
                    ScheduleStartupVerification(_serverProcess.Id);

                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError("Failed to start MCP server: {message}", ex.Message);
                    CleanupProcess();
                    return false;
                }
            }
        }

        /// <summary>
        /// Stops the MCP server process.
        /// By default, this method is non-blocking: it sends the kill/terminate signal
        /// and lets the Exited event handler perform cleanup asynchronously.
        /// When force is true (e.g., editor quitting), it blocks until the process exits.
        /// </summary>
        public static bool StopServer(bool force = false)
        {
            lock (_processMutex)
            {
                if (_serverStatus.CurrentValue == McpServerStatus.Stopped ||
                    _serverStatus.CurrentValue == McpServerStatus.Stopping)
                {
                    _logger.LogDebug("MCP server is already stopped or stopping");
                    return true;
                }

                if (_serverProcess == null)
                {
                    _serverStatus.Value = McpServerStatus.Stopped;
                    EditorPrefs.DeleteKey(ProcessIdKey);
                    return true;
                }

                _serverStatus.Value = McpServerStatus.Stopping;

                try
                {
                    _logger.LogInformation("Stopping MCP server (PID: {pid})", _serverProcess.Id);

                    if (!_serverProcess.HasExited)
                    {
                        SendTerminateSignal();
                    }

                    if (force)
                    {
                        // Synchronous path: block until exit (used during editor quitting)
                        WaitForExitAndForceKillIfNeeded();
                        CleanupProcess();
                    }
                    else
                    {
                        if (_serverProcess.HasExited)
                        {
                            CleanupProcess();
                        }
                        else
                        {
                            // Non-blocking path: schedule background wait + force kill safety net.
                            // CleanupProcess will be called by OnProcessExited or the background task.
                            ScheduleForceKillIfNeeded();
                        }
                    }

                    _logger.LogInformation("MCP server stop initiated");
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error stopping MCP server: {message}", ex.Message);
                    CleanupProcess();
                    return false;
                }
            }
        }

        /// <summary>
        /// Sends the platform-appropriate terminate signal without waiting for exit.
        /// </summary>
        static void SendTerminateSignal()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _serverProcess!.Kill();
            }
            else
            {
                // On Unix-like systems, send SIGTERM for graceful shutdown
                try
                {
                    using var killProcess = Process.Start(new ProcessStartInfo
                    {
                        FileName = "kill",
                        Arguments = $"-TERM {_serverProcess!.Id}",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    });
                    killProcess?.WaitForExit(1000);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("SIGTERM failed, falling back to Kill(): {message}", ex.Message);
                    _serverProcess!.Kill();
                }
            }
        }

        /// <summary>
        /// Blocking wait for process exit, with force-kill fallback.
        /// Used only during editor quitting to prevent orphaned processes.
        /// </summary>
        static void WaitForExitAndForceKillIfNeeded()
        {
            if (_serverProcess == null || _serverProcess.HasExited)
                return;

            if (!_serverProcess.WaitForExit(5000))
            {
                _logger.LogWarning("MCP server did not exit gracefully, forcing termination");
                try
                {
                    _serverProcess.Kill();
                    _serverProcess.WaitForExit(2000);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("Force kill failed: {message}", ex.Message);
                }
            }
        }

        /// <summary>
        /// Background safety net: waits for the process to exit and force-kills after timeout.
        /// Calls CleanupProcess on the main thread when done.
        /// </summary>
        static void ScheduleForceKillIfNeeded()
        {
            var process = _serverProcess;
            if (process == null)
                return;

            Task.Run(() =>
            {
                try
                {
                    if (!process.HasExited && !process.WaitForExit(5000))
                    {
                        _logger.LogWarning("MCP server did not exit gracefully, forcing termination");
                        try
                        {
                            process.Kill();
                            process.WaitForExit(2000);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogDebug("Force kill error: {message}", ex.Message);
                        }
                    }
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogDebug("Process already exited or disposed while waiting for exit: {message}", ex.Message);
                }

                // Ensure cleanup on the main thread.
                // Safe to call even if OnProcessExited already triggered cleanup.
                MainThread.Instance.Run(CleanupProcess);
            });
        }

        /// <summary>
        /// Kills an orphaned gamedev-mcp-server process that is occupying this project's port.
        /// Only targets the specific process listening on <see cref="UnityMcpPluginEditor.Port"/>.
        /// If the port owner cannot be determined, does nothing (fails safe).
        /// </summary>
        static void KillOrphanedServerProcesses()
        {
            try
            {
                var port = UnityMcpPluginEditor.Port;
                var currentPid = _serverProcess?.Id ?? -1;

                var listeningPid = GetPidListeningOnPort(port);

                if (listeningPid <= 0)
                {
                    _logger.LogDebug("No process found listening on port {port}, port is available", port);
                    return;
                }

                if (listeningPid == currentPid)
                {
                    _logger.LogDebug("Our own server process (PID: {pid}) is listening on port {port}", listeningPid, port);
                    return;
                }

                try
                {
                    using var process = Process.GetProcessById(listeningPid);
                    if (process == null || process.HasExited)
                    {
                        _logger.LogDebug("Process (PID: {pid}) on port {port} has already exited", listeningPid, port);
                        return;
                    }

                    var processName = process.ProcessName.ToLowerInvariant();
                    if (!processName.Contains(McpServerProcessName))
                    {
                        _logger.LogWarning(
                            "Port {port} is occupied by a non-MCP process '{processName}' (PID: {pid}). " +
                            "The MCP server may fail to start. Please free the port or change the port in settings.",
                            port, process.ProcessName, listeningPid);
                        return;
                    }

                    _logger.LogWarning("Killing orphaned MCP server process (PID: {pid}) occupying port {port}", listeningPid, port);
                    process.Kill();

                    if (!process.WaitForExit(3000))
                        _logger.LogWarning("Orphaned MCP server process (PID: {pid}) did not exit within 3 seconds after kill", listeningPid);
                    else
                        _logger.LogDebug("Orphaned MCP server process (PID: {pid}) exited successfully", listeningPid);
                }
                catch (ArgumentException)
                {
                    _logger.LogDebug("Process (PID: {pid}) on port {port} no longer exists", listeningPid, port);
                }
                catch (InvalidOperationException)
                {
                    _logger.LogDebug("Process (PID: {pid}) on port {port} exited before it could be terminated", listeningPid, port);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("Failed to kill orphaned process (PID: {pid}) on port {port}: {message}", listeningPid, port, ex.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Error in orphaned server process cleanup: {message}", ex.Message);
            }
        }

        /// <summary>
        /// Returns the PID of the process listening on the specified TCP port,
        /// or -1 if no process is found or the lookup fails.
        /// </summary>
        static int GetPidListeningOnPort(int port)
        {
            try
            {
                var startInfo = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? new ProcessStartInfo
                    {
                        FileName = "netstat",
                        Arguments = "-ano -p tcp",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true
                    }
                    : new ProcessStartInfo
                    {
                        FileName = "lsof",
                        Arguments = $"-ti tcp:{port} -sTCP:LISTEN",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                using var process = Process.Start(startInfo);
                if (process == null) return -1;

                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit(5000);

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var portSuffix = $":{port}";
                    foreach (var line in output.Split('\n'))
                    {
                        var trimmed = line.Trim();
                        if (!trimmed.Contains("LISTENING"))
                            continue;

                        var parts = trimmed.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length < 5)
                            continue;

                        var localAddress = parts[1];
                        if (localAddress.EndsWith(portSuffix) && int.TryParse(parts[parts.Length - 1], out var pid))
                            return pid;
                    }
                }
                else
                {
                    var trimmed = output.Trim();
                    if (string.IsNullOrEmpty(trimmed))
                        return -1;

                    var firstLine = trimmed.Split('\n')[0].Trim();
                    if (int.TryParse(firstLine, out var pid))
                        return pid;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Failed to determine PID listening on port {port}: {message}", port, ex.Message);
            }

            return -1;
        }

        static string BuildArguments()
        {
            var port = UnityMcpPluginEditor.Port;
            var timeout = UnityMcpPluginEditor.TimeoutMs;
            var transportMethod = TransportMethod.streamableHttp; // always must be streamableHttp for launching the server.
            var token = UnityMcpPluginEditor.Token;
            var authOption = UnityMcpPluginEditor.AuthOption;

            // Arguments format: port=XXXXX plugin-timeout=XXXXX client-transport=<TransportMethod> token=<Token>
            var args =
                $"{Args.Port}={port} " +
                $"{Args.PluginTimeout}={timeout} " +
                $"{Args.ClientTransportMethod}={transportMethod} " +
                $"{Args.Authorization}={authOption}";

            if (authOption == AuthOption.required && !string.IsNullOrEmpty(token))
                args += $" {Args.Token}={token}";

            return args;
        }

        /// <summary>
        /// Schedules a verification check 5 seconds after startup to detect early crashes.
        /// If the process is still running after verification, the status is set to Running.
        /// If the process has exited and no longer exists, the status is set to Stopped.
        /// </summary>
        static void ScheduleStartupVerification(int processId)
        {
            var startTime = DateTime.UtcNow;
            const double verificationDelaySeconds = 5.0;

            void CheckProcess()
            {
                // If status is no longer Starting (e.g., OnProcessExited already cleaned up), unsubscribe
                if (_serverStatus.CurrentValue != McpServerStatus.Starting)
                {
                    EditorApplication.update -= CheckProcess;
                    return;
                }

                var elapsed = DateTime.UtcNow - startTime;

                // If we haven't reached verification delay yet, wait for next frame
                if (elapsed.TotalSeconds < verificationDelaySeconds)
                    return;

                // Detect early process exit before the verification delay
                // This catches crashes that happen within the first few seconds (e.g., port already in use)
                if (!IsProcessRunning(processId))
                {
                    _logger.LogError("MCP server process (PID: {pid}) exited early within {seconds:F1} seconds after launch",
                        processId, elapsed.TotalSeconds);

                    EditorApplication.update -= CheckProcess;
                    if (_serverStatus.CurrentValue == McpServerStatus.Starting)
                        CleanupProcess();
                    return;
                }

                // Process is still running after the verification delay - mark as Running
                _logger.LogDebug("MCP server process (PID: {pid}) is still running after {seconds:F1}s verification",
                    processId, elapsed.TotalSeconds);

                EditorApplication.update -= CheckProcess;
                if (_serverStatus.CurrentValue == McpServerStatus.Starting)
                {
                    _serverStatus.Value = McpServerStatus.Running;
                    _logger.LogInformation("MCP server verified and running (PID: {pid})", processId);
                }
            }

            EditorApplication.update += CheckProcess;
        }

        /// <summary>
        /// Checks if a process with the given ID is still running and is the MCP server.
        /// </summary>
        static bool IsProcessRunning(int processId)
        {
            try
            {
                var process = Process.GetProcessById(processId);
                if (process == null || process.HasExited)
                    return false;

                var processName = process.ProcessName.ToLowerInvariant();
                return processName.Contains(McpServerProcessName);
            }
            catch (ArgumentException)
            {
                // Process with this ID does not exist
                return false;
            }
            catch (InvalidOperationException)
            {
                // Process has exited
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Error checking process status: {message}", ex.Message);
                return false;
            }
        }

        static void OnProcessExited(object? sender, EventArgs e)
        {
            _logger.LogInformation("MCP server process exited");
            // Marshal to main thread since this event is raised from a thread pool thread
            // and CleanupProcess modifies reactive properties that may be observed on the main thread
            MainThread.Instance.Run(CleanupProcess);
        }

        static void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                _logger.LogDebug("[MCP Server] {output}", e.Data);
            }
        }

        static void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                _logger.LogWarning("[MCP Server Error] {error}", e.Data);
            }
        }

        static void CleanupProcess()
        {
            _logger.LogDebug("Cleaning up MCP server process resources");
            lock (_processMutex)
            {
                var processToDispose = _serverProcess;
                _serverProcess = null;

                if (processToDispose != null)
                {
                    processToDispose.Exited -= OnProcessExited;
                    processToDispose.OutputDataReceived -= OnOutputDataReceived;
                    processToDispose.ErrorDataReceived -= OnErrorDataReceived;

                    // Dispose on a background thread to prevent deadlock.
                    // Process.Dispose() can hang on the main thread when redirected
                    // stdout/stderr streams are active, even after CancelOutputRead/CancelErrorRead.
                    Task.Run(() =>
                    {
                        try
                        {
                            try { processToDispose.CancelOutputRead(); } catch { }
                            try { processToDispose.CancelErrorRead(); } catch { }
                            processToDispose.Dispose();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogDebug("Error disposing MCP server process: {message}", ex.Message);
                        }
                    });
                }

                EditorPrefs.DeleteKey(ProcessIdKey);
                _serverStatus.Value = McpServerStatus.Stopped;
            }
        }

        /// <summary>
        /// Returns true when the local MCP server may be auto-started for the given connection mode.
        /// Only Custom mode targets the local server, so auto-start is allowed there (subject to
        /// other gates such as <see cref="UnityMcpPluginEditor.KeepServerRunning"/>). Every other
        /// mode (Cloud today, plus any future addition) connects to a remote endpoint and must
        /// never auto-start the local server on Editor launch or after a binary update.
        /// Pure (no Unity API access) so it can be unit-tested in EditMode.
        /// </summary>
        public static bool IsAutoStartAllowedForMode(ConnectionMode mode)
            => mode == ConnectionMode.Custom;

        /// <summary>
        /// Starts the MCP server if KeepServerRunning is enabled and no external server is detected.
        /// This method is called during Unity Editor startup to auto-start the server based on user preference.
        /// The external server check is performed asynchronously to avoid blocking the main thread.
        /// </summary>
        public static void StartServerIfNeeded()
        {
            EditorApplication.update -= StartServerIfNeeded;

            // Skip local server auto-start in Cloud mode — Unity connects to the cloud server instead
            if (!IsAutoStartAllowedForMode(UnityMcpPluginEditor.ConnectionMode))
            {
                _logger.LogDebug("StartServerIfNeeded: Cloud mode active, skipping local server auto-start");
                return;
            }

            // Check if user wants the server to keep running
            if (!UnityMcpPluginEditor.KeepServerRunning)
            {
                _logger.LogDebug("StartServerIfNeeded: KeepServerRunning is false, skipping auto-start");
                return;
            }

            // Check if server is already running (either local or detected from previous session)
            if (_serverStatus.CurrentValue == McpServerStatus.Running ||
                _serverStatus.CurrentValue == McpServerStatus.Starting)
            {
                _logger.LogDebug("StartServerIfNeeded: Server is already running or starting");
                return;
            }

            // Check if an external server is available on the port (non-blocking)
            var port = UnityMcpPluginEditor.Port;
            CheckExternalServerAsync(port, externalAvailable =>
            {
                if (externalAvailable)
                {
                    _logger.LogInformation("StartServerIfNeeded: External MCP server detected on port {port}, skipping local server start", port);
                    return;
                }

                // Start the local server
                _logger.LogInformation("StartServerIfNeeded: Starting local MCP server (KeepServerRunning=true)");
                StartServer();
            });
        }

        /// <summary>
        /// Checks if an external server is listening on the given port on a background thread,
        /// then invokes the callback on the main thread with the result.
        /// </summary>
        static void CheckExternalServerAsync(int port, Action<bool> onResult)
        {
            Task.Run(() =>
            {
                var result = false;
                try
                {
                    using var client = new System.Net.Sockets.TcpClient();
                    var connectTask = client.ConnectAsync("localhost", port);
                    var completed = connectTask.Wait(500); // 500ms timeout

                    if (completed && client.Connected)
                    {
                        _logger.LogDebug("CheckExternalServerAsync: Port {port} is in use by another process", port);
                        result = true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("CheckExternalServerAsync: No server detected on port {port} ({message})", port, ex.Message);
                }
                return result;
            })
            .ContinueWith(task => onResult(task.Result), TaskScheduler.FromCurrentSynchronizationContext());
        }

        #endregion // Process Lifecycle
    }
}
