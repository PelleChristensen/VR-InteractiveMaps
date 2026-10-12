using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace VRInteractiveMaps.Editor
{
    /// <summary>
    /// Builds the project's XR Origin from the XRI hands demo rig, keeping only
    /// hand tracking interaction and head tracking. Controllers, gaze, teleport and
    /// all locomotion are removed, so the player can only move physically.
    /// </summary>
    public static class HandsOnlyRigBuilder
    {
        const string k_SourcePrefab = "Assets/Samples/XR Interaction Toolkit/3.5.1/Hands Interaction Demo/Prefabs/XR Origin Hands (XR Rig).prefab";
        public const string PrefabPath = "Assets/XR/Prefabs/XR Origin (Hands Only).prefab";

        // Children of Camera Offset / XR Origin that only serve controllers, gaze or locomotion.
        static readonly string[] k_RemovedChildren =
        {
            "Camera Offset/Gaze Interactor",
            "Camera Offset/Gaze Stabilized",
            "Camera Offset/Left Controller",
            "Camera Offset/Right Controller",
            "Camera Offset/Left Controller Teleport Stabilized Origin",
            "Camera Offset/Right Controller Teleport Stabilized Origin",
            "Locomotion",
        };

        [MenuItem("VR Map/XR Origin/Rebuild Hands-Only Prefab", priority = 20)]
        public static void BuildPrefab()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(k_SourcePrefab);
            if (source == null)
            {
                Debug.LogError($"[HandsOnlyRigBuilder] Source rig not found at {k_SourcePrefab}.");
                return;
            }

            var rig = (GameObject)PrefabUtility.InstantiatePrefab(source);
            try
            {
                PrefabUtility.UnpackPrefabInstance(rig, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                rig.name = "XR Origin (Hands Only)";
                Strip(rig);

                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(PrefabPath));
                PrefabUtility.SaveAsPrefabAsset(rig, PrefabPath);
                Debug.Log($"[HandsOnlyRigBuilder] Saved {PrefabPath}\n{Describe(rig.transform)}");
            }
            finally
            {
                Object.DestroyImmediate(rig);
            }
        }

        [MenuItem("VR Map/XR Origin/Use Hands-Only Rig In Open Scene", priority = 21)]
        public static void ReplaceInOpenScene()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
                BuildPrefab();
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

            var scene = EditorSceneManager.GetActiveScene();
            var existing = scene.GetRootGameObjects().Where(go => go.GetComponent<XROrigin>() != null).ToList();
            var rig = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            Undo.RegisterCreatedObjectUndo(rig, "Use Hands-Only XR Origin");

            if (existing.Count > 0)
            {
                var old = existing[0].transform;
                rig.transform.SetPositionAndRotation(old.position, old.rotation);
                rig.transform.SetSiblingIndex(old.GetSiblingIndex());
            }
            foreach (var go in existing)
                Undo.DestroyObjectImmediate(go);

            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = rig;
            Debug.Log($"[HandsOnlyRigBuilder] Replaced {existing.Count} XR Origin(s) in {scene.path}. Save the scene to keep it.");
        }

        static void Strip(GameObject rig)
        {
            foreach (var path in k_RemovedChildren)
            {
                var child = rig.transform.Find(path);
                if (child != null)
                    Object.DestroyImmediate(child.gameObject);
                else
                    Debug.LogWarning($"[HandsOnlyRigBuilder] '{path}' not found in source rig.");
            }

            // No synthetic movement: the origin only follows the player's real-world position.
            RemoveComponent(rig, "XRGazeAssistance");
            RemoveComponent(rig, "CharacterController");

            var origin = rig.GetComponent<XROrigin>();
            origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
            origin.CameraYOffset = 0f;

            // The modality manager switches between hands and controllers; leave it hands only.
            var modality = rig.GetComponents<Component>().FirstOrDefault(c => c != null && c.GetType().Name == "XRInputModalityManager");
            if (modality != null)
            {
                var so = new SerializedObject(modality);
                foreach (var name in new[] { "m_LeftController", "m_RightController" })
                {
                    var prop = so.FindProperty(name);
                    if (prop != null)
                        prop.objectReferenceValue = null;
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            ReportMissingReferences(rig);
        }

        static void RemoveComponent(GameObject go, string typeName)
        {
            foreach (var c in go.GetComponents<Component>().Where(c => c != null && c.GetType().Name == typeName).ToArray())
                Object.DestroyImmediate(c);
        }

        static void ReportMissingReferences(GameObject rig)
        {
            var missing = new List<string>();
            foreach (var component in rig.GetComponentsInChildren<Component>(true))
            {
                if (component == null)
                    continue;
                var it = new SerializedObject(component).GetIterator();
                while (it.NextVisible(true))
                {
                    if (it.propertyType == SerializedPropertyType.ObjectReference
                        && it.objectReferenceValue == null
                        && !it.objectReferenceEntityIdValue.Equals(default(EntityId)))
                        missing.Add($"{GetPath(component.transform)} {component.GetType().Name}.{it.propertyPath}");
                }
            }
            if (missing.Count > 0)
                Debug.LogWarning("[HandsOnlyRigBuilder] Missing references after stripping:\n" + string.Join("\n", missing));
        }

        static string GetPath(Transform t) => t.parent == null ? t.name : GetPath(t.parent) + "/" + t.name;

        static string Describe(Transform root)
        {
            var sb = new StringBuilder();
            void Walk(Transform t, int depth)
            {
                if (depth > 3)
                    return;
                var comps = t.GetComponents<Component>().Where(c => c != null && !(c is Transform)).Select(c => c.GetType().Name);
                sb.AppendLine(new string(' ', depth * 2) + t.name + "  [" + string.Join(", ", comps) + "]");
                foreach (Transform c in t)
                    Walk(c, depth + 1);
            }
            Walk(root, 0);
            return sb.ToString();
        }
    }
}
