using System;
using System.IO;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEngine;

namespace PixelRunner.Editor
{
    public static class RunnerValidation
    {
        [CliCommand("runner_build_player", "Build a Windows player for the main gameplay scene")]
        public static string BuildPlayer()
        {
            Directory.CreateDirectory("Build");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { RunnerSceneBuilder.ScenePath },
                locationPathName = "Build/PixelUnicornRunner.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new InvalidOperationException($"Player build failed with {report.summary.totalErrors} errors.");
            return Path.GetFullPath("Build/PixelUnicornRunner.exe");
        }
        [CliCommand("runner_capture", "Capture gameplay and the overlay HUD at a specified output resolution")]
        public static string Capture(int width = 1280,int height = 720,string output = "Logs/gameplay-1280.png")
        {
            var camera = Camera.main;
            var hud = UnityEngine.Object.FindAnyObjectByType<RunnerHud>();
            if (camera == null || hud == null) throw new InvalidOperationException("Open MainGameplay first.");
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            var oldMode = hud.canvas.renderMode;
            var oldCamera = hud.canvas.worldCamera;
            float oldScale = hud.canvas.scaleFactor;
            var scaler = hud.GetComponent<UnityEngine.UI.CanvasScaler>();
            bool oldScaler = scaler.enabled;
            var rt = new RenderTexture(width,height,24,RenderTextureFormat.ARGB32) { antiAliasing = 1, filterMode = FilterMode.Point };
            var uiRt = new RenderTexture(width,height,24,RenderTextureFormat.ARGB32) { antiAliasing = 1, filterMode = FilterMode.Point };
            var uiObject = new GameObject("Capture UI Camera");
            var ui = uiObject.AddComponent<Camera>();
            var texture = new Texture2D(width,height,TextureFormat.RGBA32,false);
            var transforms = hud.GetComponentsInChildren<Transform>(true);
            int[] layers = new int[transforms.Length];
            int oldMask = camera.cullingMask;
            try
            {
                for (int i = 0; i < transforms.Length; i++) { layers[i] = transforms[i].gameObject.layer; transforms[i].gameObject.layer = 5; }
                camera.cullingMask &= ~(1 << 5);
                rt.Create(); camera.targetTexture = rt;
                camera.Render();
                RenderTexture.active = rt;
                texture.ReadPixels(new Rect(0,0,width,height),0,0); texture.Apply();
                var worldPixels = texture.GetPixels32();
                ui.enabled = false; ui.orthographic = true; ui.orthographicSize = 5;
                ui.clearFlags = CameraClearFlags.SolidColor; ui.backgroundColor = Color.clear; ui.cullingMask = 1 << 5;
                ui.allowHDR = ui.allowMSAA = false;
                uiRt.Create(); ui.targetTexture = uiRt;
                var data = uiObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                data.renderPostProcessing = false;
                hud.canvas.renderMode = RenderMode.ScreenSpaceCamera;
                hud.canvas.worldCamera = ui; hud.canvas.planeDistance = 1;
                scaler.enabled = false;
                hud.canvas.scaleFactor = Mathf.Max(1,Mathf.Min(width/320,height/180));
                Canvas.ForceUpdateCanvases();
                ui.Render();
                RenderTexture.active = uiRt;
                texture.ReadPixels(new Rect(0,0,width,height),0,0); texture.Apply();
                var uiPixels = texture.GetPixels32();
                for (int i = 0; i < uiPixels.Length; i++)
                    if (uiPixels[i].a > 0) worldPixels[i] = Color32.Lerp(worldPixels[i],uiPixels[i],uiPixels[i].a/255f);
                texture.SetPixels32(worldPixels); texture.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(output) ?? "Logs");
                File.WriteAllBytes(output,texture.EncodeToPNG());
                return Path.GetFullPath(output);
            }
            finally
            {
                for (int i = 0; i < transforms.Length; i++) transforms[i].gameObject.layer = layers[i];
                camera.targetTexture = previousTarget; camera.cullingMask = oldMask;
                hud.canvas.renderMode = oldMode; hud.canvas.worldCamera = oldCamera; hud.canvas.scaleFactor = oldScale; scaler.enabled = oldScaler;
                RenderTexture.active = previousActive;
                UnityEngine.Object.DestroyImmediate(uiObject);
                UnityEngine.Object.DestroyImmediate(texture);
                rt.Release(); UnityEngine.Object.DestroyImmediate(rt);
                uiRt.Release(); UnityEngine.Object.DestroyImmediate(uiRt);
            }
        }
    }
}
