using UnityEngine;

public class DigitronHotspotMarker : MonoBehaviour
{
    private DigitronCalculatorController m_Controller;
    private string m_HotspotId;
    private TextMesh m_NumberLabel;

    public void Initialize(DigitronCalculatorController controller, string hotspotId, int hotspotNumber, Camera targetCamera)
    {
        m_Controller = controller;
        m_HotspotId = hotspotId;
        EnsureNumberLabel(hotspotNumber, hotspotId);
    }

    private void OnMouseDown()
    {
        if (m_Controller == null || string.IsNullOrEmpty(m_HotspotId))
        {
            return;
        }

        m_Controller.SelectHotspot(m_HotspotId);
    }

    private void EnsureNumberLabel(int hotspotNumber, string labelId)
    {
        if (m_NumberLabel == null)
        {
            var labelObject = new GameObject("Label");
            labelObject.transform.SetParent(transform, false);
            // Offset slightly toward camera (marker local -Z faces camera after LookAt+Rotate180)
            labelObject.transform.localPosition = new Vector3(0f, 0f, -0.15f);
            labelObject.transform.localRotation = Quaternion.identity;
            // Keep scale 1 so characterSize maps directly to marker local units
            labelObject.transform.localScale = Vector3.one;

            m_NumberLabel = labelObject.AddComponent<TextMesh>();
            m_NumberLabel.anchor = TextAnchor.MiddleCenter;
            m_NumberLabel.alignment = TextAlignment.Center;
            m_NumberLabel.fontSize = 96;
            // characterSize in marker local units — marker world scale ~0.04 so 0.6 → ~2.4cm visible
            m_NumberLabel.characterSize = 0.6f;
            m_NumberLabel.color = Color.white;

            var renderer = m_NumberLabel.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        m_NumberLabel.text = $"{hotspotNumber} {labelId}";
    }
}
