using UnityEngine;

public class WebDesktopOrbitZoom : MonoBehaviour
{
    [SerializeField] private float m_RotateSpeed = 0.04f;
    [SerializeField] private float m_ZoomSpeed = 0.25f;
    [SerializeField] private float m_MinScale = 0.65f;
    [SerializeField] private float m_MaxScale = 2.4f;

    private Vector3 m_BaseScale;
    private float m_CurrentScaleMultiplier = 1f;

    private void Awake()
    {
        m_BaseScale = transform.localScale;
    }

    private void Update()
    {
        // Desktop drag rotate
        if (Input.GetMouseButton(0))
        {
            var dx = Input.GetAxis("Mouse X");
            var dy = Input.GetAxis("Mouse Y");
            transform.Rotate(Vector3.up, -dx * m_RotateSpeed * 180f, Space.World);
            transform.Rotate(Vector3.right, dy * m_RotateSpeed * 120f, Space.Self);
        }

        // Mouse wheel zoom
        var scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.0001f)
        {
            m_CurrentScaleMultiplier = Mathf.Clamp(
                m_CurrentScaleMultiplier + (scroll * m_ZoomSpeed),
                m_MinScale,
                m_MaxScale);
            transform.localScale = m_BaseScale * m_CurrentScaleMultiplier;
        }

        // Touch pinch zoom fallback
        if (Input.touchCount == 2)
        {
            var touch0 = Input.GetTouch(0);
            var touch1 = Input.GetTouch(1);
            var prevPos0 = touch0.position - touch0.deltaPosition;
            var prevPos1 = touch1.position - touch1.deltaPosition;
            var prevDist = (prevPos0 - prevPos1).magnitude;
            var currDist = (touch0.position - touch1.position).magnitude;
            var delta = (currDist - prevDist) * 0.0025f;

            m_CurrentScaleMultiplier = Mathf.Clamp(m_CurrentScaleMultiplier + delta, m_MinScale, m_MaxScale);
            transform.localScale = m_BaseScale * m_CurrentScaleMultiplier;
        }
    }
}
