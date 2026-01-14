// --- START FILE: PlayModeCapture.cs ---
// --- Path: Assets/GameExport/Scripts/PlayModeCapture.cs ---
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GameExport
{
    public class PlayModeExportRunner : MonoBehaviour
    {
        public class ExportJob
        {
            public ExportPreset preset;
            public ExportTag windowTagMask;
            public string currentExportName;
        }

        public IEnumerator RunBatchExport(ExportRegistry registry, List<ExportJob> jobs)
        {
            if (registry == null || jobs == null || jobs.Count == 0)
            {
                CleanUp();
                yield break;
            }

#if UNITY_EDITOR
            int originalIndex = GameViewSizeUtil.GetCurrentSizeIndex();
#endif
            var originalSnapshot = ExportTagMasking.SnapshotAllTags();

            try
            {
                foreach (var job in jobs)
                {
                    yield return RunSinglePreset(registry, job);
                }
            }
            finally
            {
                ExportTagMasking.RestoreTagSnapshot(originalSnapshot);
#if UNITY_EDITOR
                GameViewSizeUtil.SetSizeIndex(originalIndex);
#endif
                Debug.Log("[Export] Batch PlayMode export complete.");
                CleanUp();
            }
        }

        private IEnumerator RunSinglePreset(ExportRegistry registry, ExportJob job)
        {
            string root = ExportPaths.ResolveRootFolder(registry.rootFolderPath);
            string presetFolder = ExportPaths.GetPresetFolder(root, job.preset.presetType);

            var preset = job.preset;
            if (preset.entries == null) yield break;

            for (int i = 0; i < preset.entries.Count; i++)
            {
                var e = preset.entries[i];
                int w = Mathf.RoundToInt(e.resolution.x);
                int h = Mathf.RoundToInt(e.resolution.y);

                if (w <= 0 || h <= 0) continue;

                ExportTag effectiveMask = (e.entryTagMask != ExportTag.None) ? e.entryTagMask : job.windowTagMask;
                ExportTagMasking.SetTagMaskNoSnapshot(effectiveMask);

#if UNITY_EDITOR
                GameViewSizeUtil.EnsureSizeAndSelect(w, h);
                FocusGameView();
#endif
                yield return null;
                yield return new WaitForEndOfFrame();
                yield return new WaitForEndOfFrame();

                string fileName = ExportPaths.BuildFileName(preset.presetType, e, job.currentExportName);
                string fullPath = Path.Combine(presetFolder, fileName);
                
                if (e.dontOverwrite && File.Exists(fullPath))
                {
                    Debug.Log($"[Export] Skipped (dontOverwrite): {fullPath}");
                    continue;
                }

                // 1. Capture Raw Screen
                var rawTex = ScreenCapture.CaptureScreenshotAsTexture();

                if (rawTex != null)
                {
                    // 2. Fix Color Space (Linear -> sRGB)
                    // If we are in Linear space, the screenshot is likely Linear. 
                    // We must Blit it to an sRGB RenderTexture to bake the Gamma curve.
                    Texture2D finalTex = rawTex;
                    bool needsDestroy = false;

// Convert to sRGB if Linear
                    if (QualitySettings.activeColorSpace == ColorSpace.Linear)
                    {
                        finalTex = ConvertToSRGB(rawTex, e.fileFormat == FileFormat.PNG24);
                        needsDestroy = true;
                    }
                    else if (e.fileFormat == FileFormat.PNG24)
                    {
                        // Not Linear, but still need to strip Alpha for PNG24
                        finalTex = StripAlpha(rawTex);
                        needsDestroy = true;
                    }

// 3. Save
                    byte[] bytes;
                    if (e.fileFormat == FileFormat.JPG) bytes = finalTex.EncodeToJPG(90);
                    else bytes = finalTex.EncodeToPNG(); // This handles PNG and PNG24 correctly if finalTex is RGB24

                    File.WriteAllBytes(fullPath, bytes);
                    if (needsDestroy) Destroy(finalTex);
                    Destroy(rawTex);
                }

                Debug.Log($"[Export] Captured: {fileName}");
            }
        }

        /// <summary>
        /// Blits a Linear Texture into an sRGB RenderTexture and reads it back.
        /// This applies Gamma correction (darkening) to fix the "washed out" look.
        /// </summary>
        private Texture2D ConvertToSRGB(Texture2D source)
        {
            var rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            
            // Blit does the color conversion because rt is marked sRGB
            Graphics.Blit(source, rt);
            
            var result = new Texture2D(source.width, source.height, TextureFormat.RGB24, false);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            
            result.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            result.Apply();
            
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            
            return result;
        }

        private void FocusGameView()
        {
#if UNITY_EDITOR
            var assembly = typeof(EditorWindow).Assembly;
            var type = assembly.GetType("UnityEditor.GameView");
            var gameView = EditorWindow.GetWindow(type);
            if (gameView != null) gameView.Focus();
#endif
        }

        private void CleanUp()
        {
            if (gameObject != null) Destroy(gameObject);
        }
        
        private Texture2D ConvertToSRGB(Texture2D source, bool stripAlpha)
        {
            // If stripAlpha is true, we use RGB24
            TextureFormat targetFormat = stripAlpha ? TextureFormat.RGB24 : TextureFormat.RGBA32;
            var rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
    
            Graphics.Blit(source, rt);
    
            var result = new Texture2D(source.width, source.height, targetFormat, false);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            result.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            result.Apply();
    
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            return result;
        }

        private Texture2D StripAlpha(Texture2D source)
        {
            var result = new Texture2D(source.width, source.height, TextureFormat.RGB24, false);
            result.SetPixels(source.GetPixels());
            result.Apply();
            return result;
        }
    }
}
// --- END FILE: PlayModeCapture.cs ---