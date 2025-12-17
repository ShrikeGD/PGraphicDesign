// Assets/Editor/GameExport/FFmpeg/FfmpegHelper.cs
#if UNITY_EDITOR
using UnityEngine;

namespace GameExport
{
    /// <summary>Minimal facade: record frames + encode via ffmpeg.</summary>
    public static class FfmpegHelper
    {
        public static bool ExportGifFromCamera(Camera camera, int w, int h, int fps, int frames, string outputPath)
        {
            if (!FFmpegPaths.EnsureFFmpegOrError(out var exe)) return false;

            string dir = FrameRecorder.RenderPngFramesToTemp(camera, w, h, fps, frames, "GIF");
            if (string.IsNullOrEmpty(dir)) return false;

            bool ok = FFmpegEncoder.EncodeGif(dir, fps, outputPath, exe);
            FrameRecorder.Cleanup(dir);
            if (ok) Debug.Log($"[FFmpeg] GIF written → {outputPath}");
            return ok;
        }

        public static bool ExportMp4FromCamera(Camera camera, int w, int h, int fps, int frames, string outputPath, int crf = 20, int preset = 5)
        {
            if (!FFmpegPaths.EnsureFFmpegOrError(out var exe)) return false;

            string dir = FrameRecorder.RenderPngFramesToTemp(camera, w, h, fps, frames, "MP4");
            if (string.IsNullOrEmpty(dir)) return false;

            bool ok = FFmpegEncoder.EncodeMp4(dir, fps, outputPath, exe, crf, preset);
            FrameRecorder.Cleanup(dir);
            if (ok) Debug.Log($"[FFmpeg] MP4 written → {outputPath}");
            return ok;
        }
    }
}
#endif