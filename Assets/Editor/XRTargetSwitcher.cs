using System.Linq;
using UnityEditor;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;

namespace VRInteractiveMaps.Editor
{
    /// <summary>
    /// Switches the Android OpenXR feature set between Meta Quest and PICO.
    /// Both headsets run the same OpenXR project, but their vendor features and
    /// controller profiles can't be enabled at the same time, so each build
    /// targets one headset.
    /// Command line: -executeMethod VRInteractiveMaps.Editor.XRTargetSwitcher.SwitchToQuest (or SwitchToPico)
    /// </summary>
    public static class XRTargetSwitcher
    {
        const string k_MenuRoot = "VR Map/XR Target/";
        const string k_QuestMenu = k_MenuRoot + "Meta Quest";
        const string k_PicoMenu = k_MenuRoot + "PICO";

        static readonly string[] k_QuestFeatures =
        {
            "com.unity.openxr.feature.metaquest",
            "com.unity.openxr.feature.input.oculustouch",
            "com.unity.openxr.feature.input.metaquestplus",
            "com.unity.openxr.feature.input.metaquestpro",
            "com.unity.openxr.feature.meta-display-utilities",
        };

        static readonly string[] k_PicoFeatures =
        {
            "com.unity.openxr.feature.pico",
            "com.unity.openxr.pico.features",
            "com.unity.openxr.feature.input.PICO4touch",
            "com.unity.openxr.feature.input.PICO4Ultratouch",
            "com.unity.openxr.feature.input.PICONeo3touch",
            "com.unity.openxr.feature.input.PICOG3touch",
        };

        public enum Target { Unknown, Quest, Pico }

        [MenuItem(k_QuestMenu, priority = 0)]
        public static void SwitchToQuest() => Apply(Target.Quest);

        [MenuItem(k_PicoMenu, priority = 1)]
        public static void SwitchToPico() => Apply(Target.Pico);

        [MenuItem(k_QuestMenu, true)]
        static bool ValidateQuest()
        {
            Menu.SetChecked(k_QuestMenu, CurrentTarget == Target.Quest);
            return true;
        }

        [MenuItem(k_PicoMenu, true)]
        static bool ValidatePico()
        {
            Menu.SetChecked(k_PicoMenu, CurrentTarget == Target.Pico);
            return true;
        }

        public static Target CurrentTarget
        {
            get
            {
                if (IsEnabled("com.unity.openxr.feature.pico"))
                    return Target.Pico;
                if (IsEnabled("com.unity.openxr.feature.metaquest"))
                    return Target.Quest;
                return Target.Unknown;
            }
        }

        static void Apply(Target target)
        {
            FeatureHelpers.RefreshFeatures(BuildTargetGroup.Android);
            var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            if (settings == null)
            {
                Debug.LogError("[XRTargetSwitcher] No OpenXR settings found for Android.");
                return;
            }

            var enable = target == Target.Pico ? k_PicoFeatures : k_QuestFeatures;
            foreach (var feature in settings.GetFeatures<OpenXRFeature>())
            {
                var id = GetFeatureId(feature);
                if (enable.Contains(id))
                    feature.enabled = true;
                else if (k_QuestFeatures.Contains(id) || k_PicoFeatures.Contains(id) || ConflictsWith(target, feature, id))
                    feature.enabled = false;
            }

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log($"[XRTargetSwitcher] Android OpenXR features set for {target}.");
        }

        // PICO only accepts its own controller profiles and conflicts with the
        // other vendors' loader features.
        static bool ConflictsWith(Target target, OpenXRFeature feature, string id)
        {
            if (target != Target.Pico)
                return false;
            return feature is OpenXRInteractionFeature
                || id.Contains("meta")
                || id.Contains("oculus")
                || id.Contains("androidxr");
        }

        static bool IsEnabled(string featureId)
        {
            var feature = FeatureHelpers.GetFeatureWithIdForBuildTarget(BuildTargetGroup.Android, featureId);
            return feature != null && feature.enabled;
        }

        static string GetFeatureId(OpenXRFeature feature)
        {
            var attr = feature.GetType().GetCustomAttributes(typeof(OpenXRFeatureAttribute), true)
                .OfType<OpenXRFeatureAttribute>().FirstOrDefault();
            return attr != null ? attr.FeatureId : string.Empty;
        }
    }
}
