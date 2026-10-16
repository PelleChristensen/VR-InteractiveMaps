using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace VRInteractiveMaps
{
    /// <summary>
    /// Shows a poke button above one palm while that palm faces the player's head,
    /// and quits the app when the other hand pokes it. Gives hand-tracking users a
    /// way out of the app that doesn't depend on the headset's system gesture.
    /// </summary>
    public class PalmExitButton : MonoBehaviour
    {
        [SerializeField, Tooltip("The hand whose palm shows the button.")]
        Handedness m_Hand = Handedness.Left;

        [SerializeField, Tooltip("The poke button. Shown and moved above the palm by this component.")]
        XRSimpleInteractable m_Button;

        [SerializeField, Tooltip("Distance from the palm to the button, along the palm's normal.")]
        float m_PalmOffset = 0.08f;

        [SerializeField, Range(0f, 1f), Tooltip("How directly the palm must face the head before the button appears.")]
        float m_ShowFacing = 0.75f;

        [SerializeField, Range(0f, 1f), Tooltip("The button hides again when facing drops below this.")]
        float m_HideFacing = 0.55f;

        [SerializeField, Tooltip("Seconds the button must be visible before a poke counts, so it can't be hit while it pops in.")]
        float m_ArmDelay = 0.4f;

        static readonly List<XRHandSubsystem> s_Subsystems = new List<XRHandSubsystem>();

        XRHandSubsystem m_Subsystem;
        XROrigin m_Origin;
        Camera m_Head;
        bool m_Visible;
        float m_ShownAt;

        void Awake()
        {
            m_Origin = GetComponentInParent<XROrigin>();
            if (m_Button != null)
                m_Button.gameObject.SetActive(false);
        }

        void OnEnable()
        {
            if (m_Button != null)
                m_Button.selectEntered.AddListener(OnPoked);
        }

        void OnDisable()
        {
            if (m_Button != null)
                m_Button.selectEntered.RemoveListener(OnPoked);
            SetVisible(false);
        }

        void Update()
        {
            if (m_Button == null || m_Origin == null)
                return;

            if (m_Head == null)
                m_Head = m_Origin.Camera;

            if (m_Subsystem == null || !m_Subsystem.running)
            {
                SubsystemManager.GetSubsystems(s_Subsystems);
                m_Subsystem = s_Subsystems.Find(s => s.running);
            }

            if (m_Subsystem == null || m_Head == null || !TryGetPalm(out var palmPosition, out var palmNormal))
            {
                SetVisible(false);
                return;
            }

            var headPosition = m_Head.transform.position;
            var facing = Vector3.Dot(palmNormal, (headPosition - palmPosition).normalized);
            if (facing >= m_ShowFacing)
                SetVisible(true);
            else if (facing < m_HideFacing)
                SetVisible(false);

            if (!m_Visible)
                return;

            // +Z points away from the viewer, which is the poke filter's default poke direction.
            var buttonPosition = palmPosition + palmNormal * m_PalmOffset;
            m_Button.transform.SetPositionAndRotation(buttonPosition,
                Quaternion.LookRotation(buttonPosition - headPosition, m_Origin.transform.up));
        }

        bool TryGetPalm(out Vector3 position, out Vector3 normal)
        {
            position = normal = default;
            var hand = m_Hand == Handedness.Right ? m_Subsystem.rightHand : m_Subsystem.leftHand;
            if (!hand.isTracked || !hand.GetJoint(XRHandJointID.Palm).TryGetPose(out var pose))
                return false;

            // Hand poses are relative to the origin's tracking space. The OpenXR palm
            // joint's +Y points out of the back of the hand, so the palm faces -Y.
            var space = m_Origin.CameraFloorOffsetObject.transform;
            position = space.TransformPoint(pose.position);
            normal = space.rotation * (pose.rotation * Vector3.down);
            return true;
        }

        void SetVisible(bool visible)
        {
            if (m_Visible == visible || m_Button == null)
                return;
            m_Visible = visible;
            m_ShownAt = Time.unscaledTime;
            m_Button.gameObject.SetActive(visible);
        }

        void OnPoked(SelectEnterEventArgs args)
        {
            if (!m_Visible || Time.unscaledTime - m_ShownAt < m_ArmDelay)
                return;

            Debug.Log("[PalmExitButton] Exit poked, quitting.");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
