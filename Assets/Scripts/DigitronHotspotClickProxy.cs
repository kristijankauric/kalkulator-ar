using UnityEngine;

public class DigitronHotspotClickProxy : MonoBehaviour
{
    private DigitronHotspotMarker m_Marker;

    public void Setup(DigitronHotspotMarker marker)
    {
        m_Marker = marker;
    }

    private void OnMouseDown()
    {
        m_Marker?.OnClick();
    }
}
