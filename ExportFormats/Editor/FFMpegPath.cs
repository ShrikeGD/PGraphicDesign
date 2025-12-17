// Assets/Editor/GameExport/FFmpeg/FFmpegPaths.cs
#if UNITY_EDITOR
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace GameExport
{
    /// <summary>FFmpeg path storage + availability checks.</summary>
    public static class FFmpegPaths
    {
        private const string kPrefKey = "GameExport.FFmpegPath";
        private const string kDefaultWin = @"C:\ffmpeg-2025\bin\ffmpeg.exe";

        /// <summary>Path to ffmpeg executable. Defaults to C:\ffmpeg-2025\bin\ffmpeg.exe on Windows, or 'ffmpeg' (PATH) on others.</summary>
        public static string FFmpegPath
        {
            get
            {
                var cached = EditorPrefs.GetString(kPrefKey, string.Empty);
                if (!string.IsNullOrEmpty(cached)) return cached;
                return Application.platform == RuntimePlatform.WindowsEditor ? kDefaultWin : "ffmpeg";
            }
            set
            {
                if (string.IsNullOrEmpty(value)) EditorPrefs.DeleteKey(kPrefKey);
                else EditorPrefs.SetString(kPrefKey, value);
            }
        }

        /// <summary>Returns true when ffmpeg is callable. Logs a friendly error if not.</summary>
        public static bool EnsureFFmpegOrError(out string resolvedPath)
        {
            resolvedPath = FFmpegPath;

            try
            {
                if (Application.platform == RuntimePlatform.WindowsEditor && !File.Exists(resolvedPath))
                    return MissingFFmpegError(resolvedPath);

                var psi = new ProcessStartInfo
                {
                    FileName = resolvedPath,
                    Arguments = "-version",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using var p = Process.Start(psi);
                p.WaitForExit(4000);
                if (p.ExitCode == 0) return true;
            }
            catch { /* fallthrough */ }

            return MissingFFmpegError(resolvedPath);
        }

        private static bool MissingFFmpegError(string attemptedPath)
        {
            Debug.LogError(
                "[FFmpeg] FFmpeg not found or not callable.\n" +
                $"Tried: '{attemptedPath}'\n\n" +
                "Install & Setup:\n" +
                "  • Windows builds: https://www.gyan.dev/ffmpeg/builds/\n" +
                "  • Then either set:\n" +
                "      FfmpegPaths.FFmpegPath = \"C:/path/to/ffmpeg.exe\" (we persist it)\n" +
                "    or add ffmpeg to your system PATH.\n" +
                "Retry the export afterwards."
            );
            return false;
        }
    }
}
#endif
