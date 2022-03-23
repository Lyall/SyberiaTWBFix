using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using HarmonyLib;

[assembly: MelonInfo(typeof(SyberiaTWBMod.SyberiaTWBFix), "Syberia: The World Before Fix", "1.0.2", "Lyall")]
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

        public static bool ResPatch1HasRun = false;
        public static bool ResPatch2HasRun = false;
        public static bool SkipIntroHasRun = false;

        public override void OnApplicationStart()
        {
            LoggerInstance.Msg("Application started.");

            Fixes = MelonPreferences.CreateCategory("SyberiaTWBFix");
            Fixes.SetFilePath("Mods/SyberiaTWBFix.cfg");
            UIFix = Fixes.CreateEntry("UI_Fixes", true, "", "Fixes UI issues at ultrawide/wider");
            SkipIntro = Fixes.CreateEntry("Skip_Intro", true, "", "Skips intro videos and profile selection.");
            DesiredResolutionX = Fixes.CreateEntry("Resolution_Width", (int)Display.main.systemWidth, "", "Custom resolution width");
            DesiredResolutionY = Fixes.CreateEntry("Resolution_Height", (int)Display.main.systemHeight, "", "Custom resolution height");

        }

        // Set UI scaling consistently 
        public override void OnSceneWasLoaded(int buildIndex, string sceneName) // This only runs on scene loading so shouldn't be too inefficient?
        {
            float NewAspectRatio = (float)Screen.width / (float)Screen.height;
            LoggerInstance.Msg($"New aspect ratio = {NewAspectRatio}");

            if (SyberiaTWBFix.UIFix.Value && NewAspectRatio >= 1.8)
            {
                var CanvasObjects = GameObject.FindObjectsOfType<UnityEngine.UI.CanvasScaler>();
                foreach (var GameObject in CanvasObjects)
                {
                    GameObject.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
                    LoggerInstance.Msg("Scene Load: Changed " + GameObject.name + " screen match mode to " + GameObject.screenMatchMode.ToString());
                }
            }
            else if (SyberiaTWBFix.UIFix.Value && NewAspectRatio <= 1.8) // Set back to default
            {
                var CanvasObjects = GameObject.FindObjectsOfType<UnityEngine.UI.CanvasScaler>();
                foreach (var GameObject in CanvasObjects)
                {
                    GameObject.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                    LoggerInstance.Msg("Scene Load: Changed " + GameObject.name + " screen match mode to match (default)");
                }
            }
        }

        // Adjust UI scaling on resolution change
        [HarmonyPatch(typeof(LauncherUIManager), "CloseModalGraphicOptions")]
        class ResolutionOptionsScaling
        {
            [HarmonyPostfix]
            public static void ResolutionOptionsScalingPostfix(LauncherUIManager __instance)
            {
                var selectedRes = __instance.resolution.contentList[__instance.resolution.currentIndex].ToString();
                var splitRes = selectedRes.Split('x');
                float NewAspectRatio = float.Parse(splitRes[0]) / float.Parse(splitRes[1]);
                MelonLogger.Msg($"Selected resolution = {float.Parse(splitRes[0])} x {float.Parse(splitRes[1])}");
                MelonLogger.Msg($"New aspect ratio = {NewAspectRatio}");

                if (SyberiaTWBFix.UIFix.Value && NewAspectRatio >= 1.8)
                {
                    var CanvasObjects = GameObject.FindObjectsOfType<UnityEngine.UI.CanvasScaler>();
                    foreach (var GameObject in CanvasObjects)
                    {
                        GameObject.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
                        MelonLogger.Msg("Resolution Options: Changed " + GameObject.name + " screen match mode to " + GameObject.screenMatchMode.ToString());
                    }
                }
                else if (SyberiaTWBFix.UIFix.Value && NewAspectRatio <= 1.8) // Set back to default
                {
                    var CanvasObjects = GameObject.FindObjectsOfType<UnityEngine.UI.CanvasScaler>();
                    foreach (var GameObject in CanvasObjects)
                    {
                        GameObject.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                        MelonLogger.Msg("Resolution Options: Changed " + GameObject.name + " screen match mode to match (default)");
                    }
                }
            }
        }

        // Skip intro + fix video scaling for intro logos
        [HarmonyPatch(typeof(LauncherManager), "Start")]
        class SkipIntroPatch
        {
            [HarmonyPostfix]
            public static void SkipIntro(LauncherManager __instance)
            {
                if (SyberiaTWBFix.SkipIntro.Value & !SkipIntroHasRun) // Run once
                {
                    __instance.DisplayMenu();
                    SkipIntroHasRun = true;
                    MelonLogger.Msg("Skipped intro.");
                }
                float NewAspectRatio = (float)Screen.width / (float)Screen.height;
                if (SyberiaTWBFix.UIFix.Value && NewAspectRatio >= 1.8
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


        // Patch resolution list
        [HarmonyPatch(typeof(GameOptionsSO), "GetResolutionList")]
        class ResolutionPatch1
        {
            [HarmonyPostfix]
            public static void ResolutionPatch2Postfix(GameOptionsSO __instance)
            {
                if (!ResPatch1HasRun)
                {
                    Resolution customResolution = new Resolution
                    {
                        width = (int)DesiredResolutionX.Value,
                        height = (int)DesiredResolutionY.Value,
                        refreshRate = 0
                    };

                    __instance.ptr_resolutionlist.Add(customResolution);
                }
                ResPatch1HasRun = true;
            }
        }

        // Patch UI resolution list
        [HarmonyPatch(typeof(LauncherUIManager), "DisplayOptions")]
        class ResolutionPatch2
        {
            [HarmonyPostfix]
            public static void ResolutionPatch2Postfix(LauncherUIManager __instance)
            {
                if (!ResPatch2HasRun)
                {
                    string customResolution = $"{(int)DesiredResolutionX.Value} x {(int)DesiredResolutionY.Value}";
                    __instance.resolution.contentList.Add(customResolution);
                    __instance.SetOptionInitValue(); // Update to include added resolution
                }
                ResPatch2HasRun = true;
            }
        }

        // Fix video scaling on previously video
        [HarmonyPatch(typeof(LauncherManager), "playpreviously")]
        class VideoPatch
        {
            [HarmonyPostfix]
            public static void FixVideoScaling()
            {
                float NewAspectRatio = (float)Screen.width / (float)Screen.height;
                if (SyberiaTWBFix.UIFix.Value && NewAspectRatio >= 1.8)
                {
                    var RealCamera = GameObject.Find("RealCamera").GetComponent<VideoPlayer>();
                    RealCamera.aspectRatio = VideoAspectRatio.FitVertically;
                    MelonLogger.Msg("Changed aspect ratio of previously video to FitVertically.");
                }
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