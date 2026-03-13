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
        EnsureNumberLabel(hotspotNumber);
    }

    private void OnMouseDown()
    {
        if (m_Controller == null || string.IsNullOrEmpty(m_HotspotId))
        {
            return;
        }

        m_Controller.SelectHotspot(m_HotspotId);
    }

    private void EnsureNumberLabel(int hotspotNumber)
    {
        if (m_NumberLabel == null)
        {
            var labelObject = new GameObject("Label");
            labelObject.transform.SetParent(transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 0f, -0.01f);
            labelObject.transform.localRotation = Quaternion.identity;
            labelObject.transform.localScale = Vector3.one * 0.18f;

            m_NumberLabel = labelObject.AddComponent<TextMesh>();
            m_NumberLabel.anchor = TextAnchor.MiddleCenter;
            m_NumberLabel.alignment = TextAlignment.Center;
            m_NumberLabel.fontSize = 64;
            m_NumberLabel.characterSize = 0.12f;
            m_NumberLabel.color = Color.white;

            var renderer = m_NumberLabel.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        m_NumberLabel.text = hotspotNumber.ToString();
    }
}
