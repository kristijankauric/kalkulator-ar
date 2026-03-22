using UnityEngine;

public class EditorPreviewMouseOrbit : MonoBehaviour
{
    [SerializeField] private float m_RotationSpeed = 0.35f;
    [SerializeField] private float m_PitchMin = -35f;
    [SerializeField] private float m_PitchMax = 35f;
    [SerializeField] private float m_FocusSpeed = 4f;
    [SerializeField] private float m_ZoomSpeed = 0.7f;
    [SerializeField] private float m_MinCameraDistance = 0.35f;
    [SerializeField] private float m_MaxCameraDistance = 6.5f;

    private float m_Yaw;
    private float m_Pitch;
    private Vector3 m_LastMousePosition;
    private float m_TargetYaw;
    private float m_TargetPitch;
    private bool m_IsFocusing;

    private void Awake()
    {
        var euler = transform.localEulerAngles;
        m_Yaw = NormalizeAngle(euler.y);
        m_Pitch = NormalizeAngle(euler.x);
        m_TargetYaw = m_Yaw;
        m_TargetPitch = m_Pitch;
    }

    public void FocusOnPoint(Vector3 worldPos)
    {
#if UNITY_EDITOR
        var dir = worldPos - transform.position;
        if (dir.sqrMagnitude < 0.0001f) return;
        var parentRot = transform.parent != null ? transform.parent.rotation : Quaternion.identity;
        var localDir = Quaternion.Inverse(parentRot) * dir.normalized;
        m_TargetYaw   = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;
        m_TargetPitch  = -Mathf.Asin(Mathf.Clamp(localDir.y, -1f, 1f)) * Mathf.Rad2Deg;
        m_TargetPitch  = Mathf.Clamp(m_TargetPitch, m_PitchMin, m_PitchMax);
        m_IsFocusing   = true;
#endif
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) return;

        // Any mouse drag overrides focus
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
        {
            m_LastMousePosition = Input.mousePosition;
            m_IsFocusing = false;
        }

        if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
        {
            var mousePosition = Input.mousePosition;
            var delta = mousePosition - m_LastMousePosition;
            m_LastMousePosition = mousePosition;
            m_Yaw   += delta.x * m_RotationSpeed;
            m_Pitch -= delta.y * m_RotationSpeed;
            m_Pitch  = Mathf.Clamp(m_Pitch, m_PitchMin, m_PitchMax);
        }
        else if (m_IsFocusing)
        {
            m_Yaw   = Mathf.LerpAngle(m_Yaw,   m_TargetYaw,   Time.deltaTime * m_FocusSpeed);
            m_Pitch = Mathf.LerpAngle(m_Pitch, m_TargetPitch, Time.deltaTime * m_FocusSpeed);
            if (Mathf.Abs(Mathf.DeltaAngle(m_Yaw, m_TargetYaw))   < 0.5f &&
                Mathf.Abs(Mathf.DeltaAngle(m_Pitch, m_TargetPitch)) < 0.5f)
            {
                m_Yaw   = m_TargetYaw;
                m_Pitch = m_TargetPitch;
                m_IsFocusing = false;
            }
        }

        transform.localRotation = Quaternion.Euler(m_Pitch, m_Yaw, 0f);
        HandleZoom();
#endif
    }

    private void HandleZoom()
    {
        var scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) < 0.0001f) return;

        var cam = Camera.main;
        if (!cam) return;

        var focusPoint = transform.position;
        var toCam = cam.transform.position - focusPoint;
        var distance = toCam.magnitude;
        if (distance < 0.0001f) return;

        var targetDistance = Mathf.Clamp(distance - (scroll * m_ZoomSpeed), m_MinCameraDistance, m_MaxCameraDistance);
        cam.transform.position = focusPoint + (toCam.normalized * targetDistance);
    }

    private static float NormalizeAngle(float angle)
    {
        if (angle > 180f) angle -= 360f;
        return angle;
    }
}
