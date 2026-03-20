using UnityEngine;

public class WebDesktopOrbitZoom : MonoBehaviour
{
    [SerializeField] private float m_RotateSpeed = 0.012f;
    [SerializeField] private float m_ZoomSpeed = 0.25f;
    [SerializeField] private float m_MinScale = 0.65f;
    [SerializeField] private float m_MaxScale = 2.4f;

    private const float KDragThreshold = 8f;  // pixels before drag mode activates

    private Vector3 m_BaseScale;
    private float m_CurrentScaleMultiplier = 1f;
    private Vector2 m_MousePressPos;
    private bool m_IsDragging;

    private void Awake()
    {
        m_BaseScale = transform.localScale;
    }

    private void Update()
    {
        // Desktop drag rotate — wait for a minimum drag distance so
        // clicking on calculator keys doesn't accidentally rotate the model.
        if (Input.GetMouseButtonDown(0))
        {
            m_MousePressPos = Input.mousePosition;
            m_IsDragging = false;
        }
        if (Input.GetMouseButton(0))
        {
            if (!m_IsDragging)
            {
                var moved = ((Vector2)Input.mousePosition - m_MousePressPos).magnitude;
                if (moved > KDragThreshold) m_IsDragging = true;
            }
            if (m_IsDragging)
            {
                var dx = Input.GetAxis("Mouse X");
                var dy = Input.GetAxis("Mouse Y");
                transform.Rotate(Vector3.up, -dx * m_RotateSpeed * 180f, Space.World);
                transform.Rotate(Vector3.right, dy * m_RotateSpeed * 120f, Space.Self);
            }
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
