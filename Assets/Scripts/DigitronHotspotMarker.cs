using UnityEngine;

public class DigitronHotspotMarker : MonoBehaviour
{
    private DigitronCalculatorController m_Controller;
    private string m_HotspotId;
    private Renderer m_Renderer;
    private LineRenderer m_Line;
    private Transform m_AnchorTarget;

    private static readonly Color KNormalColor   = Color.white;
    private static readonly Color KSelectedColor = new Color(1f, 0.80f, 0.00f);
    private static readonly Color KLabelBackgroundColor = new Color(1f, 1f, 1f, 1f);
    private static Texture2D s_WhiteTexture;

    public void Initialize(DigitronCalculatorController controller, string hotspotId, string label)
    {
        m_Controller = controller;
        m_HotspotId  = hotspotId;
        m_Renderer   = GetComponent<Renderer>();
        BuildLabel(label);
        SetSelected(false);
    }

    public void InitLine(Transform anchorTarget)
    {
        m_AnchorTarget = anchorTarget;
        m_Line = gameObject.AddComponent<LineRenderer>();
        m_Line.useWorldSpace        = true;
        m_Line.positionCount        = 2;
        m_Line.startWidth           = 0.005f;
        m_Line.endWidth             = 0.0015f;
        m_Line.shadowCastingMode    = UnityEngine.Rendering.ShadowCastingMode.Off;
        m_Line.receiveShadows       = false;
        m_Line.generateLightingData = false;
        var mat = CreateOverlayColorMaterial(new Color(1f, 1f, 1f, 0.85f));
        m_Line.material = mat;
        UpdateLine();
    }

    public void UpdateLine()
    {
        if (m_Line == null || m_AnchorTarget == null) return;
        m_Line.SetPosition(0, transform.position);
        m_Line.SetPosition(1, m_AnchorTarget.position);
    }

    public void SetSelected(bool selected)
    {
        if (!m_Renderer) return;
        var block = new MaterialPropertyBlock();
        block.SetColor("_Color", selected ? KSelectedColor : KNormalColor);
        m_Renderer.SetPropertyBlock(block);
        // Line turns orange when this hotspot is selected
        if (m_Line != null && m_Line.material != null)
            m_Line.material.color = selected
                ? new Color(1f, 0.50f, 0.05f, 1f)   // orange
                : new Color(1f, 1f, 1f, 0.85f);      // white
    }

    private void BuildLabel(string label)
    {
        var go = new GameObject("Label");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, 0f, -0.15f);
        go.transform.localScale    = Vector3.one;

        var tm = go.AddComponent<TextMesh>();
        if (tm == null) return;
        tm.text      = label;
        tm.anchor    = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.fontSize  = 120;
        tm.fontStyle = FontStyle.Bold;
        tm.color     = Color.black;

        var longestLine = 1;
        var lineCount = 0;
        foreach (var line in label.Split('\n'))
        {
            if (line.Length > longestLine) longestLine = line.Length;
            lineCount++;
        }
        lineCount = Mathf.Max(1, lineCount);
        tm.characterSize = Mathf.Min(0.55f, 3.5f / longestLine);

        // Background sized from characterSize (reliable — mesh bounds may be zero at creation time).
        // +Z offset puts background FURTHER from camera so it renders before text in the
        // transparent back-to-front queue, appearing correctly behind the text.
        var cs = tm.characterSize;
        var bgWidth  = Mathf.Max(longestLine * cs * 1.25f + cs * 2.0f, cs * 5.0f);
        var bgHeight = lineCount > 1
            ? Mathf.Max(lineCount * cs * 1.7f + cs * 1.0f, cs * 4.0f)
            : Mathf.Max(cs * 2.6f, cs * 3.0f);

        var background = GameObject.CreatePrimitive(PrimitiveType.Quad);
        background.name = "LabelBackground";
        background.transform.SetParent(go.transform, false);
        background.transform.localPosition = new Vector3(0f, 0f, 0.01f);
        background.transform.localRotation = Quaternion.identity;
        background.transform.localScale = new Vector3(bgWidth, bgHeight, 1f);
        var bgCollider = background.GetComponent<Collider>();
        if (bgCollider != null) Destroy(bgCollider);
        var bgRenderer = background.GetComponent<Renderer>();
        if (bgRenderer != null)
        {
            bgRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            bgRenderer.receiveShadows = false;
            var mat = CreateOverlayColorMaterial(KLabelBackgroundColor);
            if (mat != null) bgRenderer.material = mat;
            bgRenderer.sortingOrder = 4999;
        }

        var r = tm.GetComponent<MeshRenderer>();
        if (r == null) return;
        var textMat = CreateOverlayTextMaterial(r.sharedMaterial, Color.black);
        if (textMat != null) r.material = textMat;
        r.sortingOrder = 5000;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows    = false;
    }

    private static Texture2D GetWhiteTexture()
    {
        if (s_WhiteTexture != null) return s_WhiteTexture;
        s_WhiteTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        s_WhiteTexture.SetPixel(0, 0, Color.white);
        s_WhiteTexture.Apply();
        return s_WhiteTexture;
    }

    private static Material CreateOverlayColorMaterial(Color color)
    {
        var shader = Shader.Find("GUI/Text Shader")
            ?? Shader.Find("Sprites/Default")
            ?? Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Unlit/Color");
        if (shader == null) return null;
        var mat = new Material(shader);
        mat.SetColor("_Color", color);
        mat.SetColor("_BaseColor", color);
        mat.SetTexture("_MainTex", GetWhiteTexture());
        mat.renderQueue = 5000;
        return mat;
    }

    private static Material CreateOverlayTextMaterial(Material source, Color color)
    {
        var shader = Shader.Find("GUI/Text Shader");
        if (shader == null) return source;
        var material = new Material(shader);
        material.SetColor("_Color", color);
        var mainTex = source != null ? source.mainTexture : null;
        material.SetTexture("_MainTex", mainTex != null ? mainTex : GetWhiteTexture());
        material.renderQueue = 5000;
        return material;
    }

    private void OnMouseDown()
    {
        if (m_Controller != null && !string.IsNullOrEmpty(m_HotspotId))
            m_Controller.SelectHotspot(m_HotspotId);
    }
}
