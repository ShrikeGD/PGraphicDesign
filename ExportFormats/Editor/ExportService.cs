// --- START FILE: ExportService.cs ---
// --- Path: Assets/GameExport/Editor/ExportService.cs ---
#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GameExport
{
    public static class ExportService
    {
        // ===================================================================================
        // =========================  EDIT MODE EXPORTS (CaptureUtil)  =======================
        // ===================================================================================

        public static string ExportPresetAndGetFolder(
            ExportRegistry registry,
            ExportPreset preset,
            string currentExportName,
            ExportTag windowTagMask,
            bool openFolderAfter = false)
        {
            string root = ExportPaths.ResolveRootFolder(registry.rootFolderPath);
            string presetFolder = ExportPaths.GetPresetFolder(root, preset.presetType);

            var originalSnapshot = ExportTagMasking.SnapshotAllTags();

            try
            {
                int total = preset.entries != null ? preset.entries.Count : 0;
                if (total == 0)
                {
                    Debug.LogWarning($"[Export] Preset '{preset.GetDisplayName()}' has no entries.");
                    return presetFolder;
                }

                for (int i = 0; i < total; i++)
                {
                    var e = preset.entries[i];
                    int w = Mathf.RoundToInt(e.resolution.x);
                    int h = Mathf.RoundToInt(e.resolution.y);

                    // Per-entry tag override, else use window mask
                    ExportTag effectiveMask = (e.entryTagMask != ExportTag.None) ? e.entryTagMask : windowTagMask;
                    ExportTagMasking.SetTagMaskNoSnapshot(effectiveMask);

                    float progress = (float)i / total;
                    bool cancel = EditorUtility.DisplayCancelableProgressBar(
                        $"Exporting Preset: {preset.GetDisplayName()}",
                        $"[{i + 1}/{total}] {e.entryName}  •  {w}x{h}  /  {e.fileFormat}",
                        progress
                    );
                    if (cancel) break;

                    if (w <= 0 || h <= 0) continue;

                    string fileName = ExportPaths.BuildFileName(preset.presetType, e, currentExportName);
                    string fullPath = Path.Combine(presetFolder, fileName);

                    if (e.dontOverwrite && File.Exists(fullPath))
                    {
                        Debug.Log($"[Export] Skipped (dontOverwrite): {fullPath}");
                        continue;
                    }

                    // FIX: PNG24 must be handled here too (was skipped before)
                    switch (e.fileFormat)
                    {
                        case FileFormat.JPG:
                        case FileFormat.PNG:
                        case FileFormat.PNG24:
                        case FileFormat.ICO:
                            CaptureUtil.CaptureScreenshot(fullPath, w, h, e.fileFormat);
                            break;

                        case FileFormat.GIF:
                        case FileFormat.MP4:
                            HandleVideoExport(e, w, h, fullPath);
                            break;
                    }
                }

                AssetDatabase.Refresh();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                ExportTagMasking.RestoreTagSnapshot(originalSnapshot);
            }

            if (openFolderAfter) ExportPaths.OpenFolder(presetFolder);
            return presetFolder;
        }

        /// <summary>
        /// NEW: Export a single entry from a preset (Edit Mode).
        /// Uses the same tag masking + dontOverwrite semantics as ExportPresetAndGetFolder.
        /// </summary>
        public static string ExportSingleEntryAndGetFolder(
            ExportRegistry registry,
            ExportPreset preset,
            ExportEntry entry,
            string currentExportName,
            ExportTag windowTagMask,
            bool openFolderAfter = false)
        {
            string root = ExportPaths.ResolveRootFolder(registry.rootFolderPath);
            string presetFolder = ExportPaths.GetPresetFolder(root, preset.presetType);

            var originalSnapshot = ExportTagMasking.SnapshotAllTags();

            try
            {
                if (preset == null || entry.Equals(null))
                {
                    Debug.LogWarning("[Export] Cannot export single entry: preset or entry is null.");
                    return presetFolder;
                }

                int w = Mathf.RoundToInt(entry.resolution.x);
                int h = Mathf.RoundToInt(entry.resolution.y);
                if (w <= 0 || h <= 0)
                {
                    Debug.LogWarning($"[Export] Skipped single entry (invalid res): {entry.entryName} {w}x{h}");
                    return presetFolder;
                }

                // Apply tag mask for this entry
                ExportTag effectiveMask = (entry.entryTagMask != ExportTag.None) ? entry.entryTagMask : windowTagMask;
                ExportTagMasking.SetTagMaskNoSnapshot(effectiveMask);

                // Progress bar (single step)
                bool cancel = EditorUtility.DisplayCancelableProgressBar(
                    $"Exporting Entry: {preset.GetDisplayName()}",
                    $"{entry.entryName}  •  {w}x{h}  /  {entry.fileFormat}",
                    0.5f
                );
                if (cancel) return presetFolder;

                string fileName = ExportPaths.BuildFileName(preset.presetType, entry, currentExportName);
                string fullPath = Path.Combine(presetFolder, fileName);

                if (entry.dontOverwrite && File.Exists(fullPath))
                {
                    Debug.Log($"[Export] Skipped (dontOverwrite): {fullPath}");
                    return presetFolder;
                }

                switch (entry.fileFormat)
                {
                    case FileFormat.JPG:
                    case FileFormat.PNG:
                    case FileFormat.PNG24:
                    case FileFormat.ICO:
                        CaptureUtil.CaptureScreenshot(fullPath, w, h, entry.fileFormat);
                        break;

                    case FileFormat.GIF:
                    case FileFormat.MP4:
                        HandleVideoExport(entry, w, h, fullPath);
                        break;
                }

                AssetDatabase.Refresh();
                return presetFolder;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                ExportTagMasking.RestoreTagSnapshot(originalSnapshot);
            }
        }

        public static string ExportAllAndGetFolder(
            ExportRegistry registry,
            string currentExportName,
            ExportTag windowTagMask,
            bool openFolderAfter = false)
        {
            string root = ExportPaths.ResolveRootFolder(registry.rootFolderPath);
            var allPresets = new List<ExportPreset>(registry.ValidPresets());

            var originalSnapshot = ExportTagMasking.SnapshotAllTags();

            try
            {
                int grandTotal = 0;
                foreach (var p in allPresets) if (p.entries != null) grandTotal += p.entries.Count;

                int currentIdx = 0;

                foreach (var preset in allPresets)
                {
                    string presetFolder = ExportPaths.GetPresetFolder(root, preset.presetType);
                    if (preset.entries == null) continue;

                    foreach (var e in preset.entries)
                    {
                        int w = Mathf.RoundToInt(e.resolution.x);
                        int h = Mathf.RoundToInt(e.resolution.y);

                        ExportTag effectiveMask = (e.entryTagMask != ExportTag.None) ? e.entryTagMask : windowTagMask;
                        ExportTagMasking.SetTagMaskNoSnapshot(effectiveMask);

                        if (EditorUtility.DisplayCancelableProgressBar(
                                "Exporting All",
                                $"{preset.GetDisplayName()} - {e.entryName}",
                                grandTotal <= 0 ? 1f : (float)currentIdx / grandTotal))
                            goto FINISH;

                        // Guard invalid res to avoid odd capture errors
                        if (w <= 0 || h <= 0)
                        {
                            currentIdx++;
                            continue;
                        }

                        string fileName = ExportPaths.BuildFileName(preset.presetType, e, currentExportName);
                        string fullPath = Path.Combine(presetFolder, fileName);

                        // FIX: Export ALL must respect dontOverwrite too (was missing)
                        if (e.dontOverwrite && File.Exists(fullPath))
                        {
                            Debug.Log($"[Export] Skipped (dontOverwrite): {fullPath}");
                            currentIdx++;
                            continue;
                        }

                        if (e.fileFormat == FileFormat.GIF || e.fileFormat == FileFormat.MP4)
                            HandleVideoExport(e, w, h, fullPath);
                        else
                            CaptureUtil.CaptureScreenshot(fullPath, w, h, e.fileFormat); // includes PNG24 safely

                        currentIdx++;
                    }
                }

            FINISH:
                AssetDatabase.Refresh();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                ExportTagMasking.RestoreTagSnapshot(originalSnapshot);
            }

            if (openFolderAfter) ExportPaths.OpenFolder(root);
            return root;
        }

        private static void HandleVideoExport(ExportEntry e, int w, int h, string fullPath)
        {
            // Simple wrapper for the ffmpeg logic
            int fps = e.fileFormat == FileFormat.GIF ? 12 : 30;
            int frames = Mathf.Max(1, Mathf.RoundToInt(fps * Mathf.Max(0.01f, e.duration)));

            var cam = Camera.main;
            GameObject tempCam = null;
            if (cam == null)
            {
                tempCam = new GameObject("~TempCam");
                cam = tempCam.AddComponent<Camera>();
            }

            if (e.fileFormat == FileFormat.GIF)
                FfmpegHelper.ExportGifFromCamera(cam, w, h, fps, frames, fullPath);
            else
                FfmpegHelper.ExportMp4FromCamera(cam, w, h, fps, frames, fullPath);

            if (tempCam != null) Object.DestroyImmediate(tempCam);
        }

        // ===================================================================================
        // =========================  PLAY MODE EXPORTS (Runner)  ============================
        // ===================================================================================

        public static void ExportPreset_PlayModeScreenCapture(
            ExportRegistry registry,
            ExportPreset preset,
            string currentExportName,
            ExportTag windowTagMask)
        {
            var jobs = new List<PlayModeExportRunner.ExportJob>
            {
                new PlayModeExportRunner.ExportJob
                {
                    preset = preset,
                    currentExportName = currentExportName,
                    windowTagMask = windowTagMask
                }
            };

            StartPlayModeRunner(registry, jobs);
        }

        public static void ExportAll_PlayModeScreenCapture(
            ExportRegistry registry,
            string currentExportName,
            ExportTag windowTagMask)
        {
            var jobs = new List<PlayModeExportRunner.ExportJob>();

            foreach (var preset in registry.ValidPresets())
            {
                jobs.Add(new PlayModeExportRunner.ExportJob
                {
                    preset = preset,
                    currentExportName = currentExportName,
                    windowTagMask = windowTagMask
                });
            }

            if (jobs.Count > 0)
                StartPlayModeRunner(registry, jobs);
            else
                Debug.LogWarning("[Export] No valid presets found.");
        }

        private static void StartPlayModeRunner(ExportRegistry registry, List<PlayModeExportRunner.ExportJob> jobs)
        {
            if (!Application.isPlaying)
            {
                Debug.LogError("[Export] PlayMode capture requires the game to be in Play Mode.");
                return;
            }

            var go = new GameObject("~PlayModeExportRunner");
            Object.DontDestroyOnLoad(go);

            var runner = go.AddComponent<PlayModeExportRunner>();
            runner.StartCoroutine(runner.RunBatchExport(registry, jobs));
        }

        // ===================================================================================
        // ========================  COMPATIBILITY / HELPERS  ================================
        // ===================================================================================

        // RESTORED: These methods are required by ExportWindow.cs to compile.

        public static string ResolveRootFolder(string input) => ExportPaths.ResolveRootFolder(input);

        public static string GetPresetFolder(string root, PresetType type) => ExportPaths.GetPresetFolder(root, type);

        public static string BuildFileName(PresetType presetType, ExportEntry e, string currentExportName)
            => ExportPaths.BuildFileName(presetType, e, currentExportName);

        public static void OpenFolder(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath) || !Directory.Exists(absolutePath))
            {
                Debug.LogWarning($"[Export] Folder does not exist: {absolutePath}");
                return;
            }
            Application.OpenURL($"file://{absolutePath}");
        }

        public static string PreviewPath(ExportRegistry registry, ExportPreset preset, ExportEntry e, string currentExportName)
        {
            string root = ExportPaths.ResolveRootFolder(registry.rootFolderPath);
            string presetFolder = ExportPaths.GetPresetFolder(root, preset.presetType);
            return Path.Combine(presetFolder, ExportPaths.BuildFileName(preset.presetType, e, currentExportName));
        }
    }
}
#endif
