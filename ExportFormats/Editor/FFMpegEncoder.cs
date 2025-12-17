// Assets/Editor/GameExport/FFmpeg/FFmpegEncoder.cs
#if UNITY_EDITOR
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace GameExport
{
    /// <summary>Encodes a sequence of PNG frames to GIF/MP4 using ffmpeg.</summary>
    public static class FFmpegEncoder
    {
        public static bool EncodeGif(string framesDir, int fps, string outputPath, string ffmpegExe)
        {
            if (string.IsNullOrEmpty(framesDir)) return false;
            string pattern = System.IO.Path.Combine(framesDir, "frame_%05d.png");
            string args =
                $"-y -hide_banner -loglevel error -framerate {fps} -i \"{pattern}\" " +
                "-vf \"split[s0][s1];[s0]palettegen[p];[s1][p]paletteuse\" " +
                $"\"{outputPath}\"";

            return RunFFmpeg(ffmpegExe, args, "Encoding GIF");
        }

        public static bool EncodeMp4(string framesDir, int fps, string outputPath, string ffmpegExe, int crf = 20, int preset = 5)
        {
            if (string.IsNullOrEmpty(framesDir)) return false;
            string pattern = System.IO.Path.Combine(framesDir, "frame_%05d.png");
            string presetName = H264PresetName(preset);
            string args =
                $"-y -hide_banner -loglevel error -framerate {fps} -i \"{pattern}\" " +
                $"-r {fps} -c:v libx264 -pix_fmt yuv420p -crf {crf} -preset {presetName} " +
                $"\"{outputPath}\"";

            return RunFFmpeg(ffmpegExe, args, "Encoding MP4");
        }

        private static bool RunFFmpeg(string exe, string args, string progressTitle)
        {
            try
            {
                EditorUtility.DisplayProgressBar(progressTitle, "Invoking FFmpeg…", 0.95f);

                var psi = new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                };

                using var p = Process.Start(psi);
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                p.WaitForExit();

                if (p.ExitCode != 0)
                {
                    Debug.LogError($"[FFmpeg] Process failed (code {p.ExitCode}). Args: {args}");
                    return false;
                }
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[FFmpeg] Failed to run ffmpeg: {e.Message}\nArgs: {args}");
                return false;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static string H264PresetName(int preset)
        {
            switch (Mathf.Clamp(preset, 0, 8))
            {
                case 0: return "ultrafast";
                case 1: return "superfast";
                case 2: return "veryfast";
                case 3: return "faster";
                case 4: return "fast";
                case 5: return "medium";
                case 6: return "slow";
                case 7: return "slower";
                case 8: return "veryslow";
                default: return "medium";
            }
        }
    }
}
#endif
