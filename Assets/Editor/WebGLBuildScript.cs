using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEngine;

namespace LexiconLegends.EditorTools
{
    /// <summary>
    /// One-click WebGL build configured for mobile (portrait, touch) and ready to upload
    /// to itch.io as a zip. Run via Tools > Lexicon Legends > Build WebGL (itch.io), or in
    /// batch mode: Unity.exe -batchmode -quit -projectPath &lt;path&gt;
    /// -executeMethod LexiconLegends.EditorTools.WebGLBuildScript.BuildForItch
    /// </summary>
    public static class WebGLBuildScript
    {
        private const string OutputFolder = "Builds/WebGL";
        private const string ZipPath = "Builds/LexiconLegends_WebGL.zip";
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";

        [MenuItem("Tools/Lexicon Legends/Build WebGL (itch.io)")]
        public static void BuildForItch()
        {
            // Mobile portrait sizing: matches the game's own canvas reference resolution's
            // aspect ratio (1080x1920) so the itch.io page embed defaults to a phone shape.
            PlayerSettings.productName = "Lexicon Legends";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.defaultScreenWidth = 540;
            PlayerSettings.defaultScreenHeight = 960;

            // Built-in "Minimal" WebGL template: on phones it already stretches the canvas to
            // fill the browser viewport and sets a no-zoom mobile viewport meta tag, and the
            // WebGL canvas forwards browser touch events to Unity's input system automatically
            // — no extra touch-input wiring needed for this to work in a phone browser.
            PlayerSettings.WebGL.template = "APPLICATION:Minimal";
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;

            string projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
            string outputPath = Path.Combine(projectRoot, OutputFolder);
            string zipPath = Path.Combine(projectRoot, ZipPath);

            if (Directory.Exists(outputPath)) Directory.Delete(outputPath, recursive: true);
            Directory.CreateDirectory(outputPath);

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = outputPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            });

            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.LogError($"Lexicon Legends WebGL build failed: {report.summary.result} ({report.summary.totalErrors} errors).");
                return;
            }

            if (File.Exists(zipPath)) File.Delete(zipPath);
            ZipFile.CreateFromDirectory(outputPath, zipPath, System.IO.Compression.CompressionLevel.Optimal, includeBaseDirectory: false);

            Debug.Log($"Lexicon Legends WebGL build succeeded.\nFolder: {outputPath}\nZip (upload this to itch.io): {zipPath}");
        }
    }
}
