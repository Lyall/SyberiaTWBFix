using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using HarmonyLib;

[assembly:MelonInfo(typeof(SyberiaTWBMod.SyberiaTWBFix), "Syberia The World Before Fix", "1.0.0", "Lyall")]
[assembly: MelonGame("Microids", "SyberiaTWB")]
namespace SyberiaTWBMod
{
    public class SyberiaTWBFix : MelonMod
    {
        public static MelonPreferences_Category Fixes;
        public static MelonPreferences_Entry<bool> UIFix;
        public static MelonPreferences_Entry<bool> SkipIntro;
        public static MelonPreferences_Entry<int> DesiredResolutionX;
        public static MelonPreferences_Entry<int> DesiredResolutionY;
        public static MelonPreferences_Entry<bool> CustomFullscreen;
        public override void OnApplicationStart()
        {
            LoggerInstance.Msg("Application started.");

            Fixes = MelonPreferences.CreateCategory("SyberiaTWBFix");
            Fixes.SetFilePath("Mods/SyberiaTWBFix.cfg");
            UIFix = Fixes.CreateEntry("UI_Fixes", true, "", "Fixes UI issues at ultrawide/wider");
            SkipIntro = Fixes.CreateEntry("Skip_Intro", true, "", "Skips intro videos and profile selection.");
            DesiredResolutionX = Fixes.CreateEntry("Resolution_Width", (int)Display.main.systemWidth, "", "Custom resolution width");
            DesiredResolutionY = Fixes.CreateEntry("Resolution_Height", (int)Display.main.systemHeight, "", "Custom resolution height");
            CustomFullscreen = Fixes.CreateEntry("Fullscreen", true, "", "Set to true for fullscreen windowed or false for windowed.");
            
            if (SyberiaTWBFix.CustomFullscreen.Value) { Screen.SetResolution(SyberiaTWBFix.DesiredResolutionX.Value, SyberiaTWBFix.DesiredResolutionY.Value, FullScreenMode.FullScreenWindow);  }
            if (!SyberiaTWBFix.CustomFullscreen.Value) { Screen.SetResolution(SyberiaTWBFix.DesiredResolutionX.Value, SyberiaTWBFix.DesiredResolutionY.Value, false); }
        }

        // Set scaling mode 
        public override void OnSceneWasLoaded(int buildIndex, string sceneName) // This only runs on scene loading so shouldn't be too inefficient?
        {
            float NewAspectRatio = (float)SyberiaTWBFix.DesiredResolutionX.Value / (float)SyberiaTWBFix.DesiredResolutionY.Value;
            LoggerInstance.Msg($"New aspect ratio = {NewAspectRatio}");

            if (SyberiaTWBFix.UIFix.Value && NewAspectRatio > 1.78)
            {
                var CanvasObjects = GameObject.FindObjectsOfType<UnityEngine.UI.CanvasScaler>();
                foreach (var GameObject in CanvasObjects)
                {
                    GameObject.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
                    LoggerInstance.Msg("Scene Load: Changed " + GameObject.name + " screen match mode to " + GameObject.screenMatchMode.ToString());
                }
            }
            else if (SyberiaTWBFix.UIFix.Value && NewAspectRatio < 1.78) // Set back to default
            {
                var CanvasObjects = GameObject.FindObjectsOfType<UnityEngine.UI.CanvasScaler>();
                foreach (var GameObject in CanvasObjects)
                {
                    GameObject.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                    LoggerInstance.Msg("Scene Load: Changed " + GameObject.name + " screen match mode to match (default)");
                }
            }
        }

        // Fix video scaling on previously video
        [HarmonyPatch(typeof(LauncherManager), "playpreviously")]
        class VideoPatch
        {
            [HarmonyPostfix]
            public static void FixVideoScaling()
            {
                float NewAspectRatio = (float)SyberiaTWBFix.DesiredResolutionX.Value / (float)SyberiaTWBFix.DesiredResolutionY.Value;
                if (SyberiaTWBFix.UIFix.Value && NewAspectRatio > 1.78)
                {
                    var RealCamera = GameObject.Find("RealCamera").GetComponent<VideoPlayer>();
                    RealCamera.aspectRatio = VideoAspectRatio.FitVertically;
                    MelonLogger.Msg("Changed aspect ratio of previously video to FitVertically.");
                }
            }
        }

        // Skip intro and fix intro video scaling
        [HarmonyPatch(typeof(LauncherManager), "Update")]
        class SkipIntroPatch
        {
            [HarmonyPostfix]
            public static void SkipIntro(LauncherManager __instance)
            {
                if (LauncherManager.alreadystarted == false && SyberiaTWBFix.SkipIntro.Value) // Don't run more than once after alreadystarted is flagged
                {
                    __instance.DisplayMenu();
                    MelonLogger.Msg("Skipped intro.");
                }
                float NewAspectRatio = (float)SyberiaTWBFix.DesiredResolutionX.Value / (float)SyberiaTWBFix.DesiredResolutionY.Value;
                if (SyberiaTWBFix.UIFix.Value && NewAspectRatio > 1.78
                    &
                    __instance.currentlauncherstate == LauncherManager.launcherstate.videoplaymicroids
                    ^ 
                    __instance.currentlauncherstate == LauncherManager.launcherstate.videoplaykoalabs)
                {
                    var MainCamera = GameObject.Find("MainCamera").GetComponent<VideoPlayer>();
                    MainCamera.aspectRatio = VideoAspectRatio.FitVertically;
                    MelonLogger.Msg("Changed aspect ratio of intro video to FitVertically.");
                } 
            }
        }

        // Brute force resolution change constantly. (This is bad, there is definitely a better way to do this)
        [HarmonyPatch(typeof(platformcontrol), nameof(platformcontrol.Update))]
        class ResolutionPatch
        {
            [HarmonyPrefix]
            static bool ResolutionPatchPrefix()
            {
                if (SyberiaTWBFix.CustomFullscreen.Value) { Screen.SetResolution(SyberiaTWBFix.DesiredResolutionX.Value, SyberiaTWBFix.DesiredResolutionY.Value, FullScreenMode.FullScreenWindow); }
                if (!SyberiaTWBFix.CustomFullscreen.Value) { Screen.SetResolution(SyberiaTWBFix.DesiredResolutionX.Value, SyberiaTWBFix.DesiredResolutionY.Value, false); }
                return false;
            }
        }

        // Fix loading screen background
        [HarmonyPatch(typeof(UICanvasMainLoading), "InitiateLoading")]
        class FixLoadingBGPatch
        {
            [HarmonyPostfix]
            public static void FixLoadingBG(UICanvasMainLoading __instance)
            {
                if (SyberiaTWBFix.UIFix.Value)
                {
                    var CanvasMainLoadingBG = GameObject.Find("CanvasMainLoading/background");
                    CanvasMainLoadingBG.transform.localPosition = new Vector3(0, 0, 0);
                }
            }
        }
    }
}