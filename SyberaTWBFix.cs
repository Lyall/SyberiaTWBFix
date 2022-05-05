using MelonLoader;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections.Generic;

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
        public static bool ResPatch3HasRun = false;
        public static bool SkipIntroHasRun = false;

        public override void OnApplicationStart()
        {
            LoggerInstance.Msg("Application started.");

            Fixes = MelonPreferences.CreateCategory("SyberiaTWBFix");
            Fixes.SetFilePath("Mods/SyberiaTWBFix.cfg");
            UIFix = Fixes.CreateEntry("UI_Fixes", true, "", "Fixes UI issues at ultrawide/wider");
            SkipIntro = Fixes.CreateEntry("Skip_Intro", true, "", "Skips intro videos and profile selection");
            DesiredResolutionX = Fixes.CreateEntry("Resolution_Width", Display.main.systemWidth, "", "Custom resolution width"); // Set default to something safe
            DesiredResolutionY = Fixes.CreateEntry("Resolution_Height", Display.main.systemHeight, "", "Custom resolution height"); // Set default to something safe
        }

        // Set UI scaling on every scene load 
        public override void OnSceneWasLoaded(int buildIndex, string sceneName) // This only runs on scene loading so shouldn't be too inefficient?
        {
            float NewAspectRatio = (float)Screen.width / (float)Screen.height;
            LoggerInstance.Msg($"New aspect ratio = {NewAspectRatio}");

            if (UIFix.Value && NewAspectRatio >= 1.8)
            {
                var CanvasObjects = Object.FindObjectsOfType<CanvasScaler>();
                foreach (var GameObject in CanvasObjects)
                {
                    GameObject.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
                    LoggerInstance.Msg("Scene Load: Changed " + GameObject.name + " screen match mode to " + GameObject.screenMatchMode.ToString());
                }
            }
            else if (UIFix.Value && NewAspectRatio <= 1.8) // Set back to default
            {
                var CanvasObjects = Object.FindObjectsOfType<CanvasScaler>();
                foreach (var GameObject in CanvasObjects)
                {
                    GameObject.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                    LoggerInstance.Msg("Scene Load: Changed " + GameObject.name + " screen match mode to match (default)");
                }
            }
        }

        // Adjust UI scaling on launcher resolution change
        [HarmonyPatch(typeof(LauncherUIManager), "CloseModalGraphicOptions")]
        private class ResolutionOptionsScaling
        {
            [HarmonyPostfix]
            public static void ResolutionOptionsScalingPostfix(LauncherUIManager __instance, ref UIMenuList ___resolution)
            {
                string selectedRes = ___resolution.contentList[GameOptionsSO.Instance.CurrentResolutionIndex].ToString();
                string[] splitRes = selectedRes.Split('x');
                float NewAspectRatio = float.Parse(splitRes[0]) / float.Parse(splitRes[1]); // Calculate AR off selected resolution
                MelonLogger.Msg($"Selected resolution = {float.Parse(splitRes[0])} x {float.Parse(splitRes[1])}");
                MelonLogger.Msg($"New aspect ratio = {NewAspectRatio}");

                if (UIFix.Value && NewAspectRatio >= 1.8)
                {
                    var CanvasObjects = Object.FindObjectsOfType<CanvasScaler>();
                    foreach (var GameObject in CanvasObjects)
                    {
                        GameObject.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
                        MelonLogger.Msg("Resolution Options: Changed " + GameObject.name + " screen match mode to " + GameObject.screenMatchMode.ToString());
                    }
                }
                else if (UIFix.Value && NewAspectRatio <= 1.8) // Set back to default
                {
                    var CanvasObjects = Object.FindObjectsOfType<CanvasScaler>();
                    foreach (var GameObject in CanvasObjects)
                    {
                        GameObject.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                        MelonLogger.Msg("Resolution Options: Changed " + GameObject.name + " screen match mode to match (default)");
                    }
                }
            }
        }

        // Adjust UI scaling on pause menu resolution change
        [HarmonyPatch(typeof(UICanvasPause), "CloseModalGraphicOptions")]
        private class PauseResolutionOptionsScaling
        {
            [HarmonyPostfix]
            public static void PauseResolutionOptionsScalingPostfix(UICanvasPause __instance, ref UIMenuList ___resolution)
            {
                var selectedRes = ___resolution.contentList[GameOptionsSO.Instance.CurrentResolutionIndex].ToString();
                var splitRes = selectedRes.Split('x');
                float NewAspectRatio = float.Parse(splitRes[0]) / float.Parse(splitRes[1]);
                MelonLogger.Msg($"Selected resolution = {float.Parse(splitRes[0])} x {float.Parse(splitRes[1])}"); // Calculate AR off selected resolution
                MelonLogger.Msg($"New aspect ratio = {NewAspectRatio}");

                if (UIFix.Value && NewAspectRatio >= 1.8)
                {
                    var CanvasObjects = Object.FindObjectsOfType<CanvasScaler>();
                    foreach (var GameObject in CanvasObjects)
                    {
                        GameObject.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
                        MelonLogger.Msg("Pause Resolution Options: Changed " + GameObject.name + " screen match mode to " + GameObject.screenMatchMode.ToString());
                    }
                }
                else if (UIFix.Value && NewAspectRatio <= 1.8) // Set back to default
                {
                    var CanvasObjects = Object.FindObjectsOfType<CanvasScaler>();
                    foreach (var GameObject in CanvasObjects)
                    {
                        GameObject.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                        MelonLogger.Msg("Pause Resolution Options: Changed " + GameObject.name + " screen match mode to match (default)");
                    }
                }
            }
        }

        // Skip intro + fix video scaling for intro logos
        [HarmonyPatch(typeof(LauncherManager), "Start")]
        private class SkipIntroPatch
        {
            [HarmonyPostfix]
            public static void SkipIntroPatchPostfix(LauncherManager __instance)
            {
                if (SkipIntro.Value && !SkipIntroHasRun) // Run once
                {
                    __instance.DisplayMenu();
                    SkipIntroHasRun = true;
                    MelonLogger.Msg("Skipped intro.");
                }
            }
        }

        // Inject custom resolution into resolution list
        [HarmonyPatch(typeof(GameOptionsSO), "GetResolutionList")]
        private class ResolutionPatch1
        {
            [HarmonyPostfix]
            public static void ResolutionPatch2Postfix(ref List<Resolution> ___ptr_resolutionlist)
            {
                if (!ResPatch1HasRun)
                {
                    Resolution customResolution = new Resolution
                    {
                        width = DesiredResolutionX.Value,
                        height = DesiredResolutionY.Value,
                        refreshRate = 0 // 0 = use highest available
                    };
                    ___ptr_resolutionlist.Add(customResolution);
                    MelonLogger.Msg($"Added {customResolution.ToString()} to resolution list.");
                }
                ResPatch1HasRun = true;
            }
        }

        // Inject custom resolution into launcher UI resolution list
        [HarmonyPatch(typeof(LauncherUIManager), "DisplayOptions")]
        private class ResolutionPatch2
        {
            [HarmonyPostfix]
            public static void ResolutionPatch2Postfix(LauncherUIManager __instance, ref UIMenuList ___resolution)
            {
                if (!ResPatch2HasRun)
                {
                    string customResolution = $"{DesiredResolutionX.Value} x {DesiredResolutionY.Value}";
                    ___resolution.contentList.Add(customResolution);
                    __instance.SetOptionInitValue(); // Update to include added resolution
                    MelonLogger.Msg($"Added {customResolution} to launcher UI resolution list.");
                }
                ResPatch2HasRun = true;
            }
        }

        // Inject custom resolution into pause UI resolution list
        [HarmonyPatch(typeof(UICanvasPause), "DisplayOptions")]
        private class ResolutionPatch3
        {
            [HarmonyPostfix]
            public static void ResolutionPatch3Postfix(UICanvasPause __instance, ref UIMenuList ___resolution)
            {
                if (!ResPatch3HasRun)
                {
                    string customResolution = $"{DesiredResolutionX.Value} x {DesiredResolutionY.Value}";
                    ___resolution.contentList.Add(customResolution);
                    __instance.SetOptionInitValue(); // Update to include added resolution
                    MelonLogger.Msg($"Added {customResolution} to pause UI resolution list.");
                }
                ResPatch3HasRun = true;
            }
        }

        // Fix video scaling on previously video
        [HarmonyPatch(typeof(LauncherManager), "playpreviously")]
        private class VideoPatch
        {
            [HarmonyPostfix]
            public static void VideoPatchPostfix()
            {
                float NewAspectRatio = (float)Screen.width / (float)Screen.height;
                if (UIFix.Value && NewAspectRatio >= 1.8)
                {
                    var RealCamera = GameObject.Find("RealCamera").GetComponent<VideoPlayer>();
                    RealCamera.aspectRatio = VideoAspectRatio.FitVertically;
                    MelonLogger.Msg("Changed aspect ratio of previously video to FitVertically.");
                }
            }
        }

        // Fix loading screen background
        [HarmonyPatch(typeof(UICanvasMainLoading), "InitiateLoading")]
        private class FixLoadingBGPatch
        {
            [HarmonyPostfix]
            public static void FixLoadingBGPatchPostfix(UICanvasMainLoading __instance)
            {
                if (UIFix.Value)
                {
                    var CanvasMainLoadingBG = GameObject.Find("CanvasMainLoading/background");
                    CanvasMainLoadingBG.transform.localPosition = new Vector3(0, 0, 0);
                }
            }
        }
    }
}