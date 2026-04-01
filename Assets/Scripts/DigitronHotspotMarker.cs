using UnityEngine;

public class DigitronHotspotMarker : MonoBehaviour
{
    private const float KBackgroundWidthScale = 0.75f; // ~25% narrower labels, keep same height
    private const float KBackgroundScaleMultiplierDesktop = 2.4f;
    private const float KBackgroundScaleMultiplierMobile = 3.1f;
    private const float KBackgroundMinLocalWidthDesktop = 3.8f;
    private const float KBackgroundMinLocalHeightDesktop = 1.4f;
    private const float KBackgroundMinLocalWidthMobile = 5.4f;
    private const float KBackgroundMinLocalHeightMobile = 1.9f;
    private const float KLabelLocalZOffset = 0.12f;
    private const float KLabelCameraNudge = 0.01f;
    private const float KLabelAnchorOutwardOffset = 0.03f;
    private const float KLineAnchorOutwardOffset = 0.008f;
    private const float KTextBoundsExpand = 2.0f;
    private static readonly Vector3 KForcedTextBoundsSize = new Vector3(20f, 20f, 20f);
    private DigitronCalculatorController m_Controller;
    private string m_HotspotId;
    private LineRenderer m_Line;
    private Camera m_TargetCamera;
    private Transform m_AnchorTarget;
    private Transform m_BackgroundTransform;
    private Renderer m_BackgroundRenderer;
    private TextMesh m_LabelTextMesh;
    private Renderer m_LabelRenderer;
    private Texture2D m_BackgroundTexture;
    private Transform m_LabelTransform;
    private Vector3 m_LabelBaseLocalPosition;

    private static readonly Color KLabelBackgroundColor = new Color(1f, 1f, 1f, 0.98f);
    private static Texture2D s_WhiteTexture;
    // Cache fitted background sizes per hotspot ID so repeated open/close cycles
    // don't cause the background to drift in size.
    private static readonly System.Collections.Generic.Dictionary<string, Vector2> s_FittedBgScales =
        new System.Collections.Generic.Dictionary<string, Vector2>();

    public void Initialize(DigitronCalculatorController controller, string hotspotId, string label, Texture2D backgroundTexture = null)
    {
        m_Controller       = controller;
        m_HotspotId        = hotspotId;
        // Force clean white rectangle background for hotspot labels.
        m_BackgroundTexture = null;
        if (!string.IsNullOrEmpty(hotspotId))
        {
            // Recompute fitted size with current transform scale (prevents stale oversized cache reuse).
            s_FittedBgScales.Remove(hotspotId);
        }
        BuildLabel(NormalizeLabel(label));
        SetSelected(false);
    }

    public void SetTargetCamera(Camera targetCamera)
    {
        m_TargetCamera = targetCamera;
    }

    public void InitLine(Transform anchorTarget)
    {
        m_AnchorTarget = anchorTarget;
        m_Line = gameObject.AddComponent<LineRenderer>();
        m_Line.useWorldSpace        = true;
        m_Line.positionCount        = 2;
        m_Line.alignment            = LineAlignment.View;
        m_Line.startWidth           = 0.010f;
        m_Line.endWidth             = 0.004f;
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
        var toMarker = transform.position - m_AnchorTarget.position;
        if (toMarker.sqrMagnitude > 0.000001f)
        {
            m_Line.SetPosition(1, m_AnchorTarget.position + toMarker.normalized * KLineAnchorOutwardOffset);
        }
        else
        {
            m_Line.SetPosition(1, m_AnchorTarget.position);
        }
    }

    public void SetSelected(bool selected)
    {
        // Line turns orange when this hotspot is selected
        if (m_Line != null && m_Line.material != null)
            m_Line.material.color = selected
                ? new Color(1f, 0.50f, 0.05f, 1f)   // orange
                : new Color(1f, 1f, 1f, 0.85f);      // white
        // Match selected state with line color: selected -> orange, idle -> white.
        if (m_BackgroundRenderer != null && m_BackgroundRenderer.material != null)
            m_BackgroundRenderer.material.color = selected
                ? new Color(1f, 0.50f, 0.05f, 1f)   // orange
                : KLabelBackgroundColor;
    }

    private void BuildLabel(string label)
    {
        var go = new GameObject("Label");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, 0f, KLabelLocalZOffset);
        m_LabelBaseLocalPosition = go.transform.localPosition;
        go.transform.localScale    = Vector3.one;
        m_LabelTransform = go.transform;

        // Counter-scale to neutralize the parent's non-uniform (bgW vs bgH) scale so
        // text characters always render with equal X/Y world scale (no stretch/squish).
        var psX = transform.lossyScale.x;
        var psY = transform.lossyScale.y;
        if (psX > 0.0001f && psY > 0.0001f)
            go.transform.localScale = new Vector3(1f, psX / psY, 1f);

        var tm = go.AddComponent<TextMesh>();
        if (tm == null) return;
        m_LabelTextMesh = tm;
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
        tm.characterSize = Mathf.Min(0.30f, 2.0f / longestLine);

        // Start with a rough background size — FitBackgroundToText() will correct it
        // once the TextMesh mesh has been built and bounds are readable.
        var cs = tm.characterSize;
        var bgWidth  = Mathf.Max(longestLine * cs * 4.0f + cs * 8.0f, cs * 16.0f);
        var bgHeight = lineCount > 1
            ? Mathf.Max(lineCount * cs * 4.5f + cs * 4.0f, cs * 10.0f)
            : Mathf.Max(cs * 7.0f, cs * 8.0f);
        var isMobile = Application.isMobilePlatform;
        var initialScaleMultiplier = isMobile ? KBackgroundScaleMultiplierMobile : KBackgroundScaleMultiplierDesktop;
        var initialMinWidth = isMobile ? KBackgroundMinLocalWidthMobile : KBackgroundMinLocalWidthDesktop;
        var initialMinHeight = isMobile ? KBackgroundMinLocalHeightMobile : KBackgroundMinLocalHeightDesktop;
        bgWidth = Mathf.Max(bgWidth * initialScaleMultiplier, initialMinWidth);
        bgHeight = Mathf.Max(bgHeight * initialScaleMultiplier, initialMinHeight);
        bgWidth *= KBackgroundWidthScale;

        var background = GameObject.CreatePrimitive(PrimitiveType.Quad);
        background.name = "LabelBackground";
        background.transform.SetParent(go.transform, false);
        background.transform.localPosition = new Vector3(0f, 0f, 0.01f);
        background.transform.localRotation = Quaternion.identity;
        background.transform.localScale = new Vector3(bgWidth, bgHeight, 1f);
        m_BackgroundTransform = background.transform;

        // Replace primitive MeshCollider with stable full-rect BoxCollider so the whole
        // label strip is reliably clickable as one button.
        var primitiveCollider = background.GetComponent<Collider>();
        if (primitiveCollider != null)
        {
            Destroy(primitiveCollider);
        }
        var boxCollider = background.AddComponent<BoxCollider>();
        boxCollider.center = Vector3.zero;
        boxCollider.size = new Vector3(1f, 1f, 0.02f);

        // Keep click proxy on the background collider to route clicks to this marker.
        var bgClickProxy = background.AddComponent<DigitronHotspotClickProxy>();
        bgClickProxy.Setup(this);
        var bgRenderer = background.GetComponent<Renderer>();
        if (bgRenderer != null)
        {
            bgRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            bgRenderer.receiveShadows = false;
            bgRenderer.allowOcclusionWhenDynamic = false;
            var mat = CreateOverlayColorMaterial(KLabelBackgroundColor);
            if (mat != null) bgRenderer.material = mat;
            bgRenderer.sortingOrder = 4999;
            m_BackgroundRenderer = bgRenderer;
        }

        var r = tm.GetComponent<MeshRenderer>();
        if (r == null) return;
        m_LabelRenderer = r;
        var textMat = CreateOverlayTextMaterial(r.sharedMaterial, Color.black);
        if (textMat != null) r.material = textMat;
        r.sortingOrder = 5000;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows    = false;
        r.allowOcclusionWhenDynamic = false;
        ExpandTextMeshBounds();

        StartCoroutine(FitBackgroundToText());
    }

    private System.Collections.IEnumerator FitBackgroundToText()
    {
        if (m_BackgroundTransform == null) yield break;

        // If this hotspot was already fitted in a previous build, reuse the cached
        // size immediately — prevents size drift across open/close cycles.
        // Recompute each time so size tweaks are visible immediately in Editor Play mode.

        // Use LOCAL mesh bounds — rotation-independent, unaffected by camera angle.
        var meshFilter = m_LabelTextMesh?.GetComponent<MeshFilter>();
        if (meshFilter == null) yield break;

        // Wait up to 10 frames for Unity to build the TextMesh geometry.
        for (var i = 0; i < 10; i++)
        {
            yield return null;
            if (meshFilter.mesh != null && meshFilter.mesh.bounds.size.magnitude > 0.0001f) break;
        }

        if (meshFilter.mesh == null || meshFilter.mesh.bounds.size.magnitude < 0.0001f) yield break;
        ExpandTextMeshBounds();

        // Local bounds × text object's lossy scale = world size along each axis.
        var lb = meshFilter.mesh.bounds;
        var ls = m_LabelTextMesh.transform.lossyScale;
        var worldW = lb.size.x * Mathf.Abs(ls.x);
        var worldH = lb.size.y * Mathf.Abs(ls.y);

        var padX = worldW * 0.30f;
        var padY = worldH * 0.55f;

        var parentScaleX = m_BackgroundTransform.parent != null ? m_BackgroundTransform.parent.lossyScale.x : 1f;
        var parentScaleY = m_BackgroundTransform.parent != null ? m_BackgroundTransform.parent.lossyScale.y : 1f;

        if (Mathf.Abs(parentScaleX) < 0.0001f || Mathf.Abs(parentScaleY) < 0.0001f) yield break;

        var isMobile = Application.isMobilePlatform;
        var scaleMultiplier = isMobile ? KBackgroundScaleMultiplierMobile : KBackgroundScaleMultiplierDesktop;
        var minWidth = (isMobile ? KBackgroundMinLocalWidthMobile : KBackgroundMinLocalWidthDesktop) * KBackgroundWidthScale;
        var minHeight = isMobile ? KBackgroundMinLocalHeightMobile : KBackgroundMinLocalHeightDesktop;
        var scaleX = ((worldW + padX * 2f) / parentScaleX) * scaleMultiplier;
        var scaleY = ((worldH + padY * 2f) / parentScaleY) * scaleMultiplier;
        scaleX *= KBackgroundWidthScale;
        scaleX = Mathf.Max(scaleX, minWidth);
        scaleY = Mathf.Max(scaleY, minHeight);

        m_BackgroundTransform.localScale = new Vector3(scaleX, scaleY, m_BackgroundTransform.localScale.z);

        // Cache so subsequent rebuilds reuse this size without recalculating.
        if (!string.IsNullOrEmpty(m_HotspotId))
            s_FittedBgScales[m_HotspotId] = new Vector2(scaleX, scaleY);
    }

    private void ExpandTextMeshBounds()
    {
        if (m_LabelTextMesh == null) return;
        var mf = m_LabelTextMesh.GetComponent<MeshFilter>();
        if (mf == null || mf.mesh == null) return;

        var bounds = mf.mesh.bounds;
        bounds.Expand(new Vector3(bounds.size.x * KTextBoundsExpand, bounds.size.y * KTextBoundsExpand, 1f));
        bounds.size = Vector3.Max(bounds.size, KForcedTextBoundsSize);
        mf.mesh.bounds = bounds;
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
        var shader = Shader.Find("Sprites/Default")
            ?? Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Unlit/Color");
        if (shader == null) return null;
        var mat = new Material(shader);
        mat.SetColor("_Color", color);
        mat.SetColor("_BaseColor", color);
        mat.SetTexture("_MainTex", GetWhiteTexture());
        mat.renderQueue = 3000;
        if (mat.HasProperty("_ZWrite")) mat.SetInt("_ZWrite", 0);
        if (mat.HasProperty("_ZTest")) mat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
        if (mat.HasProperty("_Cull")) mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        return mat;
    }

    private static Material CreateOverlayTextureMaterial(Texture2D texture)
    {
        var shader = Shader.Find("Sprites/Default")
            ?? Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Unlit/Color");
        if (shader == null) return null;
        var mat = new Material(shader);
        mat.SetColor("_Color", Color.white);
        mat.SetColor("_BaseColor", Color.white);
        mat.SetTexture("_MainTex", texture);
        mat.renderQueue = 3000;
        if (mat.HasProperty("_ZWrite")) mat.SetInt("_ZWrite", 0);
        if (mat.HasProperty("_ZTest")) mat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
        if (mat.HasProperty("_Cull")) mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        return mat;
    }

    private static Material CreateOverlayTextMaterial(Material source, Color color)
    {
        var shader = Shader.Find("Sprites/Default")
            ?? Shader.Find("GUI/Text Shader");
        if (shader == null) return source;
        var material = new Material(shader);
        material.SetColor("_Color", color);
        var mainTex = source != null ? source.mainTexture : null;
        material.SetTexture("_MainTex", mainTex != null ? mainTex : GetWhiteTexture());
        material.renderQueue = 3001;
        if (material.HasProperty("_ZWrite")) material.SetInt("_ZWrite", 0);
        if (material.HasProperty("_ZTest")) material.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
        if (material.HasProperty("_Cull")) material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        return material;
    }

    private static string NormalizeLabel(string label)
    {
        if (string.IsNullOrEmpty(label))
        {
            return string.Empty;
        }

        var flattened = label.Replace('\n', ' ').Replace('\r', ' ');
        while (flattened.Contains("  "))
        {
            flattened = flattened.Replace("  ", " ");
        }

        return flattened.Trim();
    }

    private void LateUpdate()
    {
        if (m_BackgroundRenderer != null) m_BackgroundRenderer.enabled = true;
        if (m_LabelRenderer != null) m_LabelRenderer.enabled = true;
        if (m_Line != null) m_Line.enabled = true;

        // Billboard: always face the camera regardless of parent rotation.
        var targetCamera = ResolveTargetCamera();
        if (m_LabelTransform != null && targetCamera != null)
        {
            m_LabelTransform.localPosition = m_LabelBaseLocalPosition;
            var worldPos = m_LabelTransform.position;
            var toCamera = targetCamera.transform.position - worldPos;
            if (toCamera.sqrMagnitude > 0.000001f)
            {
                // TextMesh front side points opposite of quad's forward in this setup.
                m_LabelTransform.rotation = Quaternion.LookRotation(-toCamera.normalized, targetCamera.transform.up);
            }
            if (m_AnchorTarget != null)
            {
                var awayFromAnchor = worldPos - m_AnchorTarget.position;
                if (awayFromAnchor.sqrMagnitude > 0.000001f)
                {
                    worldPos += awayFromAnchor.normalized * KLabelAnchorOutwardOffset;
                }
            }
            // Nudge label slightly toward camera to avoid partial clipping by nearby housing edges.
            worldPos += toCamera.sqrMagnitude > 0.000001f
                ? toCamera.normalized * KLabelCameraNudge
                : -targetCamera.transform.forward * KLabelCameraNudge;
            m_LabelTransform.position = worldPos;
            ExpandTextMeshBounds();
        }

        UpdateLine();
    }

    public void OnClick()
    {
        if (m_Controller != null && !string.IsNullOrEmpty(m_HotspotId))
            m_Controller.SelectHotspot(m_HotspotId);
    }

    private void OnMouseDown() => OnClick();

    private Camera ResolveTargetCamera()
    {
        if (m_TargetCamera != null) return m_TargetCamera;
        m_TargetCamera = Camera.main;
        return m_TargetCamera;
    }
}
