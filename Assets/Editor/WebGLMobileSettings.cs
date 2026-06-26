using UnityEditor;
using UnityEngine;

namespace LexiconLegends.EditorTools
{
    /// <summary>
    /// Applies the mobile-portrait WebGL Player Settings automatically whenever the Editor
    /// loads or recompiles scripts, so the regular File > Build Settings > Build button
    /// always picks them up too -- no need to remember to run a separate menu item first.
    /// </summary>
    [InitializeOnLoad]
    public static class WebGLMobileSettings
    {
        static WebGLMobileSettings()
        {
            PlayerSettings.productName = "Lexicon Legends";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

            // These two fields are shared by Standalone and WebGL (WebGL has no separate
            // canvas-size fields) -- matches this game's own 1080x1920 canvas aspect ratio.
            PlayerSettings.defaultScreenWidth = 540;
            PlayerSettings.defaultScreenHeight = 960;

            PlayerSettings.WebGL.template = "PROJECT:MobilePortrait";
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        }
    }
}
