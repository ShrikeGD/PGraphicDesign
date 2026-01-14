// --- START FILE: CaptureUtil.cs ---
// --- Path: CaptureUtil.cs ---
#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace GameExport
{
    public static class CaptureUtil
    {
        private struct CanvasState
        {
            public Canvas canvas;
            public RenderMode renderMode;
            public Camera worldCamera;
            public float planeDistance;
        }

        public static void CaptureScreenshot(string fullPath, int w, int h, FileFormat format)
        {
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            TextureFormat texFormat = (format == FileFormat.PNG24) ? TextureFormat.RGB24 : TextureFormat.RGBA32;
            var tex = new Texture2D(w, h, texFormat, false);

            var cam = Camera.main;
            GameObject tempGo = null;
            if (cam == null)
            {
                tempGo = new GameObject("~TempExportCamera");
                cam = tempGo.AddComponent<Camera>();
                cam.backgroundColor = Color.black;
                cam.clearFlags = CameraClearFlags.SolidColor;
            }

            // --- NEW: make Overlay canvases renderable by this camera ---
            var canvasStates = SnapshotAndRedirectOverlayCanvases(cam);

            var prev = cam.targetTexture;
            cam.targetTexture = rt;

            cam.Render();

            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();

            byte[] data = format switch
            {
                FileFormat.JPG => tex.EncodeToJPG(70),
                FileFormat.PNG => tex.EncodeToPNG(),
                FileFormat.PNG24 => tex.EncodeToPNG(),
                FileFormat.ICO => tex.EncodeToPNG(), // placeholder
                _ => tex.EncodeToPNG()
            };

            File.WriteAllBytes(fullPath, data);

            cam.targetTexture = prev;
            RenderTexture.active = null;

            // --- NEW: restore canvases ---
            RestoreOverlayCanvases(canvasStates);

            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
            if (tempGo != null) Object.DestroyImmediate(tempGo);

            Debug.Log($"[Export] Wrote {format} → {fullPath}");
        }

        private static List<CanvasState> SnapshotAndRedirectOverlayCanvases(Camera captureCam)
        {
            var list = new List<CanvasState>();

            // include inactive canvases too
            var canvases = Object.FindObjectsOfType<Canvas>(true);
            foreach (var c in canvases)
            {
                if (c == null) continue;
                if (c.renderMode != RenderMode.ScreenSpaceOverlay) continue;

                list.Add(new CanvasState
                {
                    canvas = c,
                    renderMode = c.renderMode,
                    worldCamera = c.worldCamera,
                    planeDistance = c.planeDistance
                });

                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = captureCam;
                c.planeDistance = 100f; // usually safe; adjust if needed
            }

            return list;
        }

        private static void RestoreOverlayCanvases(List<CanvasState> snapshot)
        {
            if (snapshot == null) return;
            foreach (var s in snapshot)
            {
                if (s.canvas == null) continue;
                s.canvas.renderMode = s.renderMode;
                s.canvas.worldCamera = s.worldCamera;
                s.canvas.planeDistance = s.planeDistance;
            }
        }
    }
}
#endif