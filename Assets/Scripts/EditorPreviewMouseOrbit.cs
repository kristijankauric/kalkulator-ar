using UnityEngine;

public class EditorPreviewMouseOrbit : MonoBehaviour
{
    [SerializeField] private float m_RotationSpeed = 0.35f;
    [SerializeField] private float m_PitchMin = -35f;
    [SerializeField] private float m_PitchMax = 35f;

    private float m_Yaw;
    private float m_Pitch;
    private Vector3 m_LastMousePosition;

    private void Awake()
    {
        var euler = transform.localEulerAngles;
        m_Yaw = NormalizeAngle(euler.y);
        m_Pitch = NormalizeAngle(euler.x);
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
        {
            m_LastMousePosition = Input.mousePosition;
        }

        if (!Input.GetMouseButton(0) && !Input.GetMouseButton(1))
        {
            return;
        }

        var mousePosition = Input.mousePosition;
        var delta = mousePosition - m_LastMousePosition;
        m_LastMousePosition = mousePosition;

        m_Yaw += delta.x * m_RotationSpeed;
        m_Pitch -= delta.y * m_RotationSpeed;
        m_Pitch = Mathf.Clamp(m_Pitch, m_PitchMin, m_PitchMax);

        transform.localRotation = Quaternion.Euler(m_Pitch, m_Yaw, 0f);
#endif
    }

    private static float NormalizeAngle(float angle)
    {
        if (angle > 180f)
        {
            angle -= 360f;
        }

        return angle;
    }
}
