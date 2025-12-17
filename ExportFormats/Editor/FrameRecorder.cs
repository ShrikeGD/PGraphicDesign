// Assets/Editor/GameExport/FFmpeg/FrameRecorder.cs
#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GameExport
{
    /// <summary>Captures a sequence of PNG frames from a Camera into a temp folder.</summary>
    public static class FrameRecorder
    {
        public static string RenderPngFramesToTemp(Camera cam, int w, int h, int fps, int frames, string label)
        {
            if (cam == null) { Debug.LogError("[FFmpeg] Camera is null."); return null; }
            if (w <= 0 || h <= 0 || fps <= 0 || frames <= 0) { Debug.LogError("[FFmpeg] Invalid capture params."); return null; }

            string dir = Path.Combine(Path.GetTempPath(), $"UnityExport_{label}_{Guid.NewGuid():N}");
            Directory.CreateDirectory(dir);

            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var prevRT = cam.targetTexture;

            try
            {
                for (int i = 0; i < frames; i++)
                {
                    float progress = (float)i / frames;
                    if (EditorUtility.DisplayCancelableProgressBar($"Recording Frames ({label})", $"Frame {i + 1}/{frames} @ {fps} fps", progress))
                    {
                        Debug.LogWarning("[FFmpeg] Capture cancelled by user.");
                        Cleanup(dir);
                        return null;
                    }

                    cam.targetTexture = rt;
                    cam.Render();
                    RenderTexture.active = rt;
                    tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                    tex.Apply();

                    File.WriteAllBytes(Path.Combine(dir, $"frame_{i:00000}.png"), tex.EncodeToPNG());
                }
                return dir;
            }
            catch (Exception e)
            {
                Debug.LogError($"[FFmpeg] Error while recording frames: {e.Message}");
                Cleanup(dir);
                return null;
            }
            finally
            {
                cam.targetTexture = prevRT;
                RenderTexture.active = null;
                UnityEngine.Object.DestroyImmediate(rt);
                UnityEngine.Object.DestroyImmediate(tex);
                EditorUtility.ClearProgressBar();
            }
        }

        public static void Cleanup(string dir)
        {
            if (string.IsNullOrEmpty(dir)) return;
            try { Directory.Delete(dir, true); } catch { /* ignore */ }
        }
    }
}
#endif
