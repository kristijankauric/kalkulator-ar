using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class DigitronCalculatorController : MonoBehaviour
{
    private enum DigitronState { Unplaced, PlacedClosed, Opening, Opened, Closing }

    private struct HotspotData
    {
        public string Id;
        public int Number;
        public string Title;
        public string Description;
        public Vector3 NormalizedViewportAnchor;
        public Vector3 MarkerLocalDir; // Marker placement direction in model local space (ensures spread around perimeter)
    }

    private struct KeyLayoutData
    {
        public DigitronKeyId KeyId;
        public string Name;
        public Vector3 NormalizedAnchor;
        public Vector2 NormalizedSize;
    }

    private struct DisplayLayoutData
    {
        public Vector3 NormalizedAnchor;
        public Vector2 NormalizedSize;
    }

    private const string KOpenClipName = "CalculatorOpen";
    private const string KFallbackClipName = "Take 001";
    private const string KRuntimeClipResourcesPath = "Digitron/db801-novo-odvojene-tipke";
    private const string KRuntimeControllerResourcesPath = "Digitron/DigitronAnimator";
    private const string KRuntimeStandaloneClipResourcesPath = "Digitron/CalculatorOpen";
#if UNITY_EDITOR
    private const string KRuntimeClipEditorModelPath = "Assets/Resources/Digitron/db801-novo-odvojene-tipke.fbx";
#endif
    private const string KStandaloneOpenClipAssetPath = "Assets/Resources/Digitron/CalculatorOpen.anim";
    private const float KTakeFrameRate = 25f;
    private const float KClosedPoseFrame = 100f;
    private const float KOpenStartFrame = 100f;
    private const float KOpenEndFrame = 150f;
    private const float KFallbackOpenDuration = 1.1f;
    private const float KManualOpenAngle = -110f;
    private const float KPhysicalKeyPressDepth = 0.010f;
    private const float KPhysicalKeyPressDuration = 0.08f;
    private const float KOpenButtonWidth = 160f;
    private const float KOpenButtonHeight = 50f;
    private const float KHotspotScale = 0.045f;
    private const float KHotspotOffsetFactor = 0.28f;
    private const float KInfoBoxWidth = 660f;
    private const float KInfoBoxHeight = 220f;
    private const float KBottomUiMargin = 28f;
    private const float KFrontSurfaceDepth = 0.235f;
    private const float KFrontSurfaceOffset = 0.02f;
    private const float KLampScale = 0.028f;
    private const float KDisplayMaskDepthOffset = 0.006f;
    private const float KMinimumProjectedKeySize = 10f;
    private const string KDisplayFontAssetPath = "Assets/Models/digital-7 (mono).ttf";
    private const float KKeypadMinX = 0.205f;
    private const float KKeypadMaxX = 0.892f;
    private const float KKeypadMinY = 0.156f;
    private const float KKeypadMaxY = 0.526f;

    private static readonly HotspotData[] Hotspots =
    {
        // MarkerLocalDir: offset direction in model-local space from anchor mesh centre → label quad position
        new HotspotData { Id = "housing",   Number = 1, Title = "Ku\u0107i\u0161te",              Description = "Ku\u0107i\u0161te je \u201eoklop\u201c kalkulatora: dr\u017ei sve dijelove na mjestu i \u0161titi osjetljivu elektroniku od udaraca, pra\u0161ine i slu\u010dajnih dodira. Kod db801 ku\u0107i\u0161te ima jo\u0161 jednu zanimljivu ulogu \u2013 slu\u017eilo je i kao prostor za kratke upute korisniku. Na pole\u0111ini se, primjerice, upozorava da kalkulator najbolje radi u temperaturnom rasponu 0\u201340\u00a0\u00b0C te da ga se na hladno\u0107i mo\u017ee zagrijati tjelesnom toplinom. To je mali podsjetnik koliko je ova tehnologija tada bila nova i \u201efina\u201c, i koliko se pazilo da radi pouzdano.",  NormalizedViewportAnchor = new Vector3(0.5f,  0.55f,  0.1f),  MarkerLocalDir = new Vector3( 2.6f,  0.35f,  0.10f) },
        new HotspotData { Id = "keyboard",  Number = 2, Title = "Tipkovnica",                Description = "Tipkovnica je mjesto gdje kalkulator \u201e\u010duje\u201c korisnika. Svaka tipka \u0161alje jasnu naredbu: broj, ra\u010dunsku operaciju ili brisanje unosa. Kod starijih ure\u0111aja poput db801 tipke su mehani\u010dke, pa je bilo va\u017eno da elektronika pouzdano prepoznaje svaki pritisak \u2013 bez slu\u010dajnog duplog klika. Specijalizirani \u010dip unutar kalkulatora ima ugra\u0111ene funkcije koje poma\u017eu ba\u0161 u tome: stabilno o\u010ditavanje tipki i pretvaranje pritiska u naredbu. Rezultat je jednostavno iskustvo kori\u0161tenja, iako je unutra tehnologija za svoje vrijeme bila vrlo napredna.",                     NormalizedViewportAnchor = new Vector3(0.5f,  0.3f,  0.23f), MarkerLocalDir = new Vector3(-2.2f, -0.20f,  0.30f) },
        new HotspotData { Id = "board",     Number = 3, Title = "Elektroni\u010dka\nplo\u010da", Description = "Elektroni\u010dka plo\u010da je \u201eprometna mre\u017ea\u201c kalkulatora. Po njoj idu tanke vodljive staze koje spajaju tipkovnicu, \u010dipove, zaslon i napajanje u jednu cjelinu. Na plo\u010di se nalaze i sitni dijelovi koji poma\u017eu da sve radi stabilno: povezivanje, filtriranje i raspodjela signala. U praksi, plo\u010da je ono \u0161to omogu\u0107uje da pritisak tipke postane broj na zaslonu \u2013 brzo, tiho i bez mehani\u010dkih pokretnih dijelova osim samih tipki. Kad gledate plo\u010du izbliza, vidite otisak in\u017eenjerskog rada: sve je moralo biti to\u010dno poslo\u017eeno da bi kalkulator bio pouzdan i prenosiv.",                     NormalizedViewportAnchor = new Vector3(0.52f, 0.46f, -0.08f), MarkerLocalDir = new Vector3( 2.2f,  0.35f, -0.20f) },
        new HotspotData { Id = "chips",     Number = 4, Title = "Integrirani\nkrugovi",       Description = "Integrirani krugovi (\u010dipovi) su mozak i logistika kalkulatora u jednom. Umjesto puno zasebnih dijelova, klju\u010dne funkcije ra\u010dunanja spakirane su u mali crni paki\u0107 s puno no\u017eica. U db801 koristi se specijalizirani kalkulatorski \u010dip koji zna osnovne operacije (+, \u2212, \u00d7, \u00f7), ali i prakti\u010dne opcije poput rada s konstantom, izbora decimalnih mjesta te funkcija poput postotka i korijena. Taj \u010dip ne samo da ra\u010duna, nego i poma\u017ee upravljati tipkama i segmentima zaslona. Za rane 1970-e to je zna\u010dilo isto \u0161to danas zna\u010di pametni ure\u0111aj: puno mogu\u0107nosti u malom d\u017eepnom formatu.",                                           NormalizedViewportAnchor = new Vector3(0.67f, 0.46f, -0.02f), MarkerLocalDir = new Vector3(-2.4f,  0.15f, -0.20f) },
        new HotspotData { Id = "batteries", Number = 5, Title = "Baterije",                   Description = "Ovdje je izvor energije \u2013 akumulatori (punjive baterije). Bez njih db801 ne bi mogao biti prijenosiv, a upravo je prenosivost bila dio \u010darolije prvih d\u017eepnih kalkulatora. Na samom ure\u0111aju stoji i prakti\u010dna poruka: nakon pra\u017enjenja akumulatore ne treba bacati, jer se mogu ponovno napuniti. To je detalj koji lijepo pokazuje duh vremena: tehnologija je bila skupa i vrijedna, pa se pazilo na sve \u2013 od elektronike do napajanja. Danas nam je punjenje normalno, ali tada je to bila velika prednost u odnosu na ure\u0111aje koji su brzo tro\u0161ili baterije.",              NormalizedViewportAnchor = new Vector3(0.78f, 0.3f, -0.15f), MarkerLocalDir = new Vector3(-2.3f, -0.65f,  0.20f) },
        new HotspotData { Id = "display",   Number = 6, Title = "Zaslon",                     Description = "Zaslon je pozornica kalkulatora: tu se vidi svaki unos i svaki rezultat. db801 koristi LED segmentni prikaz \u2013 znamenke se crtaju svjetle\u0107im segmentima, a preko svega ide crveni prozor koji pobolj\u0161ava kontrast i \u010dini brojke \u010ditljivijima. Danas je to retro-\u0161arm, ali u ranim 1970-ima bio je to znak moderne elektronike: brojke su svijetlile jasno, bez papira i bez pisa\u010da. Upravo zato je zaslon jedan od najprepoznatljivijih dijelova Digitronovih ranih modela \u2013 \u010dim se upali, odmah je jasno da gledate koma\u010di\u0107 povijesti d\u017eepnog ra\u010dunanja.",                                 NormalizedViewportAnchor = new Vector3(0.5f,  0.77f,  0.02f), MarkerLocalDir = new Vector3( 0.3f,  1.2f,  0.3f) },
    };

    private static readonly DisplayLayoutData DisplayLayout = new DisplayLayoutData
    {
        NormalizedAnchor = new Vector3(0.665f, 0.775f, KFrontSurfaceDepth),
        NormalizedSize = new Vector2(0.34f, 0.085f),
    };

    private static Vector3 KeypadAnchor(float x, float y)
    {
        return new Vector3(
            Mathf.Lerp(KKeypadMaxX, KKeypadMinX, x),
            Mathf.Lerp(KKeypadMinY, KKeypadMaxY, y),
            KFrontSurfaceDepth);
    }

    private static Vector2 KeypadSize(float width, float height)
    {
        return new Vector2(
            (KKeypadMaxX - KKeypadMinX) * width,
            (KKeypadMaxY - KKeypadMinY) * height);
    }

    private static readonly KeyLayoutData[] KeyLayouts =
    {
        new KeyLayoutData { KeyId = DigitronKeyId.F, Name = "F", NormalizedAnchor = KeypadAnchor(0.08f, 0.92f), NormalizedSize = KeypadSize(0.11f, 0.10f) },
        new KeyLayoutData { KeyId = DigitronKeyId.ClearEntry, Name = "CE", NormalizedAnchor = KeypadAnchor(0.34f, 0.92f), NormalizedSize = KeypadSize(0.11f, 0.10f) },
        new KeyLayoutData { KeyId = DigitronKeyId.ClearAll, Name = "C", NormalizedAnchor = KeypadAnchor(0.60f, 0.92f), NormalizedSize = KeypadSize(0.12f, 0.10f) },
        new KeyLayoutData { KeyId = DigitronKeyId.Equals, Name = "=", NormalizedAnchor = KeypadAnchor(0.96f, 0.92f), NormalizedSize = KeypadSize(0.09f, 0.10f) },
        new KeyLayoutData { KeyId = DigitronKeyId.Seven, Name = "7", NormalizedAnchor = KeypadAnchor(0.08f, 0.68f), NormalizedSize = KeypadSize(0.11f, 0.13f) },
        new KeyLayoutData { KeyId = DigitronKeyId.Eight, Name = "8", NormalizedAnchor = KeypadAnchor(0.34f, 0.68f), NormalizedSize = KeypadSize(0.11f, 0.13f) },
        new KeyLayoutData { KeyId = DigitronKeyId.Nine, Name = "9", NormalizedAnchor = KeypadAnchor(0.60f, 0.68f), NormalizedSize = KeypadSize(0.11f, 0.13f) },
        new KeyLayoutData { KeyId = DigitronKeyId.Subtract, Name = "-", NormalizedAnchor = KeypadAnchor(0.96f, 0.68f), NormalizedSize = KeypadSize(0.09f, 0.13f) },
        new KeyLayoutData { KeyId = DigitronKeyId.Four, Name = "4", NormalizedAnchor = KeypadAnchor(0.08f, 0.45f), NormalizedSize = KeypadSize(0.11f, 0.13f) },
        new KeyLayoutData { KeyId = DigitronKeyId.Five, Name = "5", NormalizedAnchor = KeypadAnchor(0.34f, 0.45f), NormalizedSize = KeypadSize(0.11f, 0.13f) },
        new KeyLayoutData { KeyId = DigitronKeyId.Six, Name = "6", NormalizedAnchor = KeypadAnchor(0.60f, 0.45f), NormalizedSize = KeypadSize(0.11f, 0.13f) },
        new KeyLayoutData { KeyId = DigitronKeyId.Divide, Name = "/", NormalizedAnchor = KeypadAnchor(0.96f, 0.45f), NormalizedSize = KeypadSize(0.09f, 0.13f) },
        new KeyLayoutData { KeyId = DigitronKeyId.One, Name = "1", NormalizedAnchor = KeypadAnchor(0.08f, 0.22f), NormalizedSize = KeypadSize(0.11f, 0.13f) },
        new KeyLayoutData { KeyId = DigitronKeyId.Two, Name = "2", NormalizedAnchor = KeypadAnchor(0.34f, 0.22f), NormalizedSize = KeypadSize(0.11f, 0.13f) },
        new KeyLayoutData { KeyId = DigitronKeyId.Three, Name = "3", NormalizedAnchor = KeypadAnchor(0.60f, 0.22f), NormalizedSize = KeypadSize(0.11f, 0.13f) },
        new KeyLayoutData { KeyId = DigitronKeyId.Multiply, Name = "x", NormalizedAnchor = KeypadAnchor(0.96f, 0.22f), NormalizedSize = KeypadSize(0.09f, 0.13f) },
        new KeyLayoutData { KeyId = DigitronKeyId.Zero, Name = "0", NormalizedAnchor = KeypadAnchor(0.17f, 0.01f), NormalizedSize = KeypadSize(0.25f, 0.10f) },
        new KeyLayoutData { KeyId = DigitronKeyId.Decimal, Name = ",", NormalizedAnchor = KeypadAnchor(0.55f, 0.01f), NormalizedSize = KeypadSize(0.15f, 0.10f) },
        new KeyLayoutData { KeyId = DigitronKeyId.Add, Name = "+", NormalizedAnchor = KeypadAnchor(0.96f, 0.01f), NormalizedSize = KeypadSize(0.09f, 0.10f) },
        new KeyLayoutData { KeyId = DigitronKeyId.Power, Name = "ON", NormalizedAnchor = new Vector3(0.790f, 0.595f, KFrontSurfaceDepth), NormalizedSize = new Vector2(0.105f, 0.055f) },
    };

    // Used for highlight/dim — all mesh objects belonging to each system
    private static readonly Dictionary<string, string[]> KHotspotMeshKeywords = new Dictionary<string, string[]>
    {
        { "housing",   new[] { "db801_prednja", "db801_straznja", "poklopac_straznji",
                               "lampica", "logo_", "sklopka", "chamfercyl", "cylinder0",
                               "kontakti", "t01", "t02", "t03" } },
        { "keyboard",  new[] { "tipka", "tipke" } },
        { "board",     new[] { "plocica_02", "tube0", "zice_", "vijak_" } },
        { "chips",     new[] { "box0", "object001" } },
        { "batteries", new[] { "baterija" } },
        { "display",   new[] { "plocica_01b", "textplus" } },
    };

    // Used for anchor/line-endpoint positioning only — the single most visible representative mesh
    private static readonly Dictionary<string, string[]> KHotspotAnchorKeywords = new Dictionary<string, string[]>
    {
        { "housing",  new[] { "db801_prednja" } },  // front outer plastic shell
        { "board",    new[] { "plocica_02" } },      // main PCB board surface
    };

    private MaterialPropertyBlock m_DimBlock;

    private readonly Dictionary<string, DigitronHotspotMarker> m_HotspotMarkers = new Dictionary<string, DigitronHotspotMarker>();
    private readonly Dictionary<string, Transform> m_HotspotAnchors = new Dictionary<string, Transform>();
    private readonly List<DigitronKeyHitTarget> m_KeyTargets = new List<DigitronKeyHitTarget>();
    private readonly Dictionary<Transform, Coroutine> m_KeyPressRoutines = new Dictionary<Transform, Coroutine>();
    private readonly DigitronRuntime m_Runtime = new DigitronRuntime();

    private GameObject m_ModelInstance;
    private Camera m_TargetCamera;
    private Bounds m_ModelBounds;
    private Bounds m_ModelLocalBounds;
    private DigitronState m_State = DigitronState.Unplaced;
    private Animator m_Animator;
    private Animation m_LegacyAnimation;
    private AnimationClip m_OpenAnimationClip;
    private AnimationClip m_ClosedPoseClip;
    private float m_ClosedPoseTime;
    private float m_OpenStartTime;
    private float m_OpenEndTime;
    private List<Renderer> m_RelevantRenderers = new List<Renderer>();
    private bool m_UsePhysicalKeyTargets;
    private string m_SelectedHotspotId;
    [SerializeField] private Texture2D m_TexDigitronNaslov;
    [SerializeField] private Texture2D m_TexSoloTrakica;
    [SerializeField] private Texture2D m_TexPapirPodloga;
    private GUIStyle m_OpenButtonStyle;
    private GUIStyle m_InfoBoxStyle;
    private GUIStyle m_InfoTitleStyle;
    private GUIStyle m_InfoDescStyle;
    private GUIStyle m_CloseButtonStyle;
    private GUIStyle m_KeyButtonStyle;
    private GUIStyle m_DisplayStyle;
    private Renderer m_LampRenderer;
    private Material m_LampMaterial;
    private Transform m_LampAnchor;
    private Renderer m_DisplayMaskRenderer;
    private Material m_DisplayMaskMaterial;
    private Transform m_DisplayAnchor;
    private Transform m_DisplaySurface;
    private TextMesh m_DisplayText;
    private Font m_DisplayFont;
    private Renderer m_ModelDisplayTextRenderer;
    private Transform m_ModelDisplayTextTransform;
    private TextMesh m_ModelDisplayTextMesh;
    private Vector2 m_DisplayWorldSize = new Vector2(0.165f, 0.048f);
    private readonly List<Renderer> m_HiddenDisplayRenderers = new List<Renderer>();
    private Coroutine m_FallbackRoutine;
    private Transform m_FrontCoverTransform;
    private Quaternion m_FrontCoverClosedRotation;
    private Quaternion m_FrontCoverOpenRotation;
    private Transform m_PowerSwitchTransform;
    private Vector3 m_PowerSwitchOnLocalPosition;
    private Vector3 m_PowerSwitchOffLocalPosition;
    private AnimationClip m_ExplicitOpenAnimationClip;

    public void Initialize(GameObject modelInstance, Camera targetCamera, float targetSize, AnimationClip explicitOpenAnimationClip = null)
    {
        m_ModelInstance = modelInstance;
        m_TargetCamera = targetCamera ? targetCamera : Camera.main;
        m_ExplicitOpenAnimationClip = explicitOpenAnimationClip;
        m_RelevantRenderers.Clear();

        try
        {
            NormalizeModelScale(targetSize);
            RecalculateBounds();
            m_Animator = m_ModelInstance.GetComponentInChildren<Animator>(true);
            if (m_Animator != null)
            {
                m_Animator.enabled = false;
                m_Animator.keepAnimatorStateOnDisable = true;
            }
            m_LegacyAnimation = m_ModelInstance.GetComponentInChildren<Animation>(true);
            TryPrepareAnimation();
            TryPrepareManualOpenTransform();
            ApplyClosedPose();
            EnsureDisplayAnchor();
            EnsureDisplayText();
            EnsureLampVisual();
            EnsureDisplayMask();
            EnsureHotspotAnchors();
            RebuildKeyTargets();
            RebuildHotspotMarkers();
            ResetCalculatorRuntime();
            SetHotspotsVisible(false);
            SetCalculatorInteractionVisible(true);
            m_State = DigitronState.PlacedClosed;
            m_ModelInstance.SetActive(true);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Digitron] Initialize failed, entering safe closed state: {e}");
            m_State = DigitronState.PlacedClosed;
            m_ModelInstance.SetActive(true);
            SetHotspotsVisible(false);
            SetCalculatorInteractionVisible(false);
        }
    }

    public void ResetDigitronState()
    {
        if (!m_ModelInstance) return;
        StopCurrentRoutine();
        m_RelevantRenderers.Clear();
        if (m_Animator != null)
        {
            m_Animator.enabled = false;
        }
        ApplyClosedPose();
        EnsureDisplayAnchor();
        EnsureDisplayText();
        ResetCalculatorRuntime();
        RebuildKeyTargets();
        SetHotspotsVisible(false);
        SetCalculatorInteractionVisible(true);
        m_SelectedHotspotId = null;
        m_State = DigitronState.PlacedClosed;
        m_ModelInstance.SetActive(true);
    }

    public void HandlePlaced(Camera targetCamera)
    {
        m_TargetCamera = targetCamera ? targetCamera : Camera.main;
        m_RelevantRenderers.Clear();
        if (m_Animator != null)
        {
            m_Animator.enabled = false;
        }
        RecalculateBounds();
        EnsureDisplayAnchor();
        EnsureDisplayText();
        EnsureLampVisual();
        EnsureDisplayMask();
        EnsureHotspotAnchors();
        RebuildKeyTargets();
        RebuildHotspotMarkers();
        RefreshCalculatorPresentation();
        if (m_State == DigitronState.Unplaced || m_State == DigitronState.Opening || m_State == DigitronState.Closing)
        {
            m_State = DigitronState.PlacedClosed;
        }
        SetHotspotsVisible(m_State == DigitronState.Opened);
        SetCalculatorInteractionVisible(m_State == DigitronState.PlacedClosed);
        m_SelectedHotspotId = null;
        Debug.Log($"[Digitron] HandlePlaced -> state {m_State}");
    }

    public void SelectHotspot(string hotspotId)
    {
        if (m_State != DigitronState.Opened) return;
        m_SelectedHotspotId = hotspotId;
        ApplyHotspotHighlight(hotspotId);
        UpdateHotspotMarkerStates();
#if UNITY_EDITOR
        if (!string.IsNullOrEmpty(hotspotId) &&
            m_HotspotAnchors.TryGetValue(hotspotId, out var anchor) && anchor != null)
        {
            var orbit = Object.FindObjectOfType<EditorPreviewMouseOrbit>();
            if (orbit != null) orbit.FocusOnPoint(anchor.position);
        }
#endif
    }

    public Bounds GetWorldBounds()
    {
        return m_ModelBounds;
    }

    public void HandleKeyPress(DigitronKeyId keyId)
    {
        if (m_State != DigitronState.PlacedClosed) return;
        m_Runtime.PressKey(keyId);
        RefreshCalculatorPresentation();
    }

    public void HandlePhysicalKeyTargetPressed(DigitronKeyHitTarget keyTarget)
    {
        if (keyTarget == null || m_State != DigitronState.PlacedClosed)
        {
            return;
        }

        HandleKeyPress(keyTarget.KeyId);
        if (keyTarget.PressTarget != null)
        {
            if (m_KeyPressRoutines.TryGetValue(keyTarget.PressTarget, out var runningRoutine) && runningRoutine != null)
            {
                StopCoroutine(runningRoutine);
            }

            keyTarget.PressTarget.localPosition = keyTarget.RestLocalPosition;
            var routine = StartCoroutine(AnimatePhysicalKeyPress(keyTarget));
            m_KeyPressRoutines[keyTarget.PressTarget] = routine;
        }
    }

    private void Update()
    {
        if (!m_TargetCamera) m_TargetCamera = Camera.main;
        UpdateLampFacingCamera();
        UpdateDisplayTextVisibility();
        UpdateHotspotMarkersFacingCamera();
        HandleCalculatorPointerInput();
    }

    private void OnGUI()
    {
        DrawTitle();
        if (!m_ModelInstance || m_State == DigitronState.Unplaced) return;
        if (!m_TargetCamera) m_TargetCamera = Camera.main;
        EnsureGuiStyles();
        // Responsive font sizes — recalculated every frame based on screen height
        var fs = Screen.height;
        m_OpenButtonStyle.fontSize  = Mathf.Clamp(Mathf.RoundToInt(fs * 0.028f), 12, 24);
        m_InfoTitleStyle.fontSize   = Mathf.Clamp(Mathf.RoundToInt(fs * 0.028f), 13, 28);
        m_InfoDescStyle.fontSize    = Mathf.Clamp(Mathf.RoundToInt(fs * 0.020f), 11, 20);
        m_CloseButtonStyle.fontSize = Mathf.Clamp(Mathf.RoundToInt(fs * 0.028f), 13, 24);
        if (m_State != DigitronState.Unplaced) DrawToggleButton();
        DrawInfoBox();
    }

    private void DrawTitle()
    {
        m_TexDigitronNaslov ??= Resources.Load<Texture2D>("digitron naslov");
        if (m_TexDigitronNaslov == null) return;
        var titleH = Screen.height * 0.082f;
        var aspect = (float)m_TexDigitronNaslov.width / Mathf.Max(1, m_TexDigitronNaslov.height);
        var titleW = titleH * aspect;
        var rect = new Rect((Screen.width - titleW) * 0.5f, Screen.height * 0.012f, titleW, titleH);
        GUI.DrawTexture(rect, m_TexDigitronNaslov, ScaleMode.ScaleToFit, true);
    }

    private void DrawToggleButton()
    {
        // Responsive: larger touch target for mobile runtime
        var btnW = Screen.width  * 0.3f;
        var btnH = Screen.height * 0.082f;
        var margin = Screen.height * 0.022f;
        var rect = new Rect((Screen.width - btnW) * 0.5f, Screen.height - btnH - margin, btnW, btnH);
        if (GUI.Button(rect, m_State == DigitronState.Opened ? "Zatvori" : "Otvori", m_OpenButtonStyle))
            ToggleOpenState();
    }

    private void DrawInfoBox()
    {
        if (m_State != DigitronState.Opened || string.IsNullOrEmpty(m_SelectedHotspotId)) return;
        var hotspot = Hotspots.FirstOrDefault(entry => entry.Id == m_SelectedHotspotId);
        if (string.IsNullOrEmpty(hotspot.Id)) return;

        // All dimensions as screen-percentage so portrait/landscape/tablet all work
        var pad       = Screen.height * 0.018f;
        var closeSize = Screen.height * 0.05f;
        var lineH     = Screen.height * 0.058f;
        var titleLineCount = hotspot.Title.Split('\n').Length;
        var titleH    = lineH * titleLineCount;
        var panelW    = Screen.width  * 0.88f;
        var descW     = panelW - pad * 2f;

        // Auto-height description so text is never clipped
        var descContent = new GUIContent(hotspot.Description);
        var descH = m_InfoDescStyle.CalcHeight(descContent, descW);
        descH = Mathf.Max(descH, Screen.height * 0.04f);

        var panelH  = pad + titleH + pad * 0.5f + descH + pad;
        var panelX  = (Screen.width - panelW) * 0.5f;
        var btnH    = Screen.height * 0.065f;
        var margin  = Screen.height * 0.025f;
        var panelY  = Screen.height - panelH - btnH - margin * 2f;
        panelY = Mathf.Max(panelY, margin);   // never go above top edge

        GUI.Box(new Rect(panelX, panelY, panelW, panelH), GUIContent.none, m_InfoBoxStyle);

        // Title
        GUI.Label(new Rect(panelX + pad, panelY + pad, panelW - pad * 2f - closeSize - 4f, titleH),
                  hotspot.Title, m_InfoTitleStyle);

        // Close ×
        if (GUI.Button(new Rect(panelX + panelW - pad - closeSize, panelY + pad * 0.4f, closeSize, closeSize),
                       "×", m_CloseButtonStyle))
        {
            m_SelectedHotspotId = null;
            ApplyHotspotHighlight(null);
            UpdateHotspotMarkerStates();
        }

        // Description (auto-sized)
        GUI.Label(new Rect(panelX + pad, panelY + pad + titleH + pad * 0.5f, descW, descH),
                  hotspot.Description, m_InfoDescStyle);
    }

    private void ToggleOpenState()
    {
        if (m_State == DigitronState.PlacedClosed) StartOpenSequence();
        else if (m_State == DigitronState.Opened) StartCloseSequence();
    }

    private void StartOpenSequence()
    {
        if (m_State != DigitronState.PlacedClosed) return;
        StopCurrentRoutine();
        m_SelectedHotspotId = null;
        SetCalculatorInteractionVisible(false);
        m_State = DigitronState.Opening;
        Debug.Log("[Digitron] StartOpenSequence -> Opening");
        if (m_OpenAnimationClip != null) m_FallbackRoutine = StartCoroutine(PlayOpenClipRoutine(0f, 1f, DigitronState.Opened));
        else if (m_FrontCoverTransform != null) m_FallbackRoutine = StartCoroutine(PlayManualCoverRoutine(0f, 1f, DigitronState.Opened));
        else m_FallbackRoutine = StartCoroutine(FallbackOpenRoutine());
    }

    private void StartCloseSequence()
    {
        if (m_State != DigitronState.Opened) return;
        StopCurrentRoutine();
        m_SelectedHotspotId = null;
        ApplyHotspotHighlight(null);
        SetHotspotsVisible(false);
        m_State = DigitronState.Closing;
        Debug.Log("[Digitron] StartCloseSequence -> Closing");
        if (m_OpenAnimationClip != null) m_FallbackRoutine = StartCoroutine(PlayOpenClipRoutine(1f, 0f, DigitronState.PlacedClosed));
        else if (m_FrontCoverTransform != null) m_FallbackRoutine = StartCoroutine(PlayManualCoverRoutine(1f, 0f, DigitronState.PlacedClosed));
        else m_FallbackRoutine = StartCoroutine(FallbackCloseRoutine());
    }

    private void StopCurrentRoutine()
    {
        if (m_FallbackRoutine == null) return;
        StopCoroutine(m_FallbackRoutine);
        m_FallbackRoutine = null;
    }

    private IEnumerator FallbackOpenRoutine()
    {
        yield return new WaitForSeconds(KFallbackOpenDuration);
        RecalculateBounds();
        RebuildHotspotMarkers();
        SetHotspotsVisible(true);
        m_State = DigitronState.Opened;
        m_FallbackRoutine = null;
    }

    private IEnumerator FallbackCloseRoutine()
    {
        yield return new WaitForSeconds(KFallbackOpenDuration);
        RecalculateBounds();
        RebuildKeyTargets();
        RefreshCalculatorPresentation();
        SetCalculatorInteractionVisible(true);
        m_SelectedHotspotId = null;
        m_State = DigitronState.PlacedClosed;
        m_FallbackRoutine = null;
    }

    private IEnumerator PlayOpenClipRoutine(float startNormalizedTime, float endNormalizedTime, DigitronState completedState)
    {
        var finalized = false;
        try
        {
            var duration = Mathf.Max(Mathf.Abs(m_OpenEndTime - m_OpenStartTime), 0.01f);
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                SampleOpenClip(Mathf.Lerp(startNormalizedTime, endNormalizedTime, Mathf.Clamp01(elapsed / duration)));
                yield return null;
            }

            SampleOpenClip(endNormalizedTime);
            FinalizeStateAfterTransition(completedState);
            finalized = true;
        }
        finally
        {
            if (!finalized)
            {
                Debug.LogWarning("[Digitron] Open clip routine fail-safe finalize triggered");
                FinalizeStateAfterTransition(completedState);
            }
        }
    }

    private IEnumerator PlayManualCoverRoutine(float startNormalizedTime, float endNormalizedTime, DigitronState completedState)
    {
        var elapsed = 0f;
        while (elapsed < KFallbackOpenDuration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Lerp(startNormalizedTime, endNormalizedTime, Mathf.Clamp01(elapsed / KFallbackOpenDuration));
            m_FrontCoverTransform.localRotation = Quaternion.Slerp(m_FrontCoverClosedRotation, m_FrontCoverOpenRotation, t);
            yield return null;
        }

        m_FrontCoverTransform.localRotation = Quaternion.Slerp(m_FrontCoverClosedRotation, m_FrontCoverOpenRotation, endNormalizedTime);
        FinalizeStateAfterTransition(completedState);
    }

    private void FinalizeStateAfterTransition(DigitronState completedState)
    {
        RecalculateBounds();
        EnsureLampVisual();
        RebuildKeyTargets();
        RebuildHotspotMarkers();
        var isOpened = completedState == DigitronState.Opened;
        SetHotspotsVisible(isOpened);
        SetCalculatorInteractionVisible(!isOpened);
        if (!isOpened)
        {
            m_SelectedHotspotId = null;
            RefreshCalculatorPresentation();
        }

        m_State = completedState;
        m_FallbackRoutine = null;
        Debug.Log($"[Digitron] Transition finalized -> {m_State}, hotspotsVisible={isOpened}");
    }

    private void TryPrepareAnimation()
    {
        m_OpenAnimationClip = null;
        m_ClosedPoseClip = null;
        m_ClosedPoseTime = 0f;
        m_OpenStartTime = 0f;
        m_OpenEndTime = 0f;

        // Ensure Animator controller is assigned before any clip path (Generic model in WebGL needs this).
        if (m_Animator != null && m_Animator.runtimeAnimatorController == null)
        {
            var loadedController = Resources.Load<RuntimeAnimatorController>(KRuntimeControllerResourcesPath);
            if (loadedController != null)
            {
                m_Animator.runtimeAnimatorController = loadedController;
                Debug.Log($"[Digitron] Loaded Animator controller from Resources/{KRuntimeControllerResourcesPath}");
            }
        }

        if (m_ExplicitOpenAnimationClip != null)
        {
            m_OpenAnimationClip = m_ExplicitOpenAnimationClip;
            m_ClosedPoseClip = m_ExplicitOpenAnimationClip;
            m_ClosedPoseTime = 0f;
            m_OpenStartTime = 0f;
            m_OpenEndTime = m_ExplicitOpenAnimationClip.length;
            Debug.Log($"[Digitron] Open animation source: Explicit clip '{m_ExplicitOpenAnimationClip.name}'");
            EnsureLegacyAnimationComponent();
            return;
        }

        if (m_LegacyAnimation != null)
        {
            m_LegacyAnimation.playAutomatically = false;
            if (m_LegacyAnimation.GetClip(KFallbackClipName) != null)
            {
                m_ClosedPoseClip = m_LegacyAnimation.GetClip(KFallbackClipName);
                m_ClosedPoseTime = Mathf.Min(KClosedPoseFrame / KTakeFrameRate, m_ClosedPoseClip.length);
            }

            if (m_LegacyAnimation.GetClip(KOpenClipName) != null)
            {
                m_OpenAnimationClip = m_LegacyAnimation.GetClip(KOpenClipName);
                m_ClosedPoseClip = m_OpenAnimationClip;
                m_ClosedPoseTime = 0f;
                m_OpenStartTime = 0f;
                m_OpenEndTime = m_OpenAnimationClip.length;
                Debug.Log($"[Digitron] Open animation source: Legacy Animation clip '{m_OpenAnimationClip.name}'");
                return;
            }

            if (m_LegacyAnimation.GetClip(KFallbackClipName) != null)
            {
                m_OpenAnimationClip = m_LegacyAnimation.GetClip(KFallbackClipName);
                m_OpenStartTime = KOpenStartFrame / KTakeFrameRate;
                m_OpenEndTime = Mathf.Min(KOpenEndFrame / KTakeFrameRate, m_OpenAnimationClip.length);
                Debug.Log($"[Digitron] Open animation source: Legacy fallback clip '{m_OpenAnimationClip.name}' [{m_OpenStartTime:F2}-{m_OpenEndTime:F2}]");
                return;
            }
        }

        if (m_Animator != null)
        {
            var controller = m_Animator.runtimeAnimatorController;
            if (controller != null)
            {
                var clips = controller.animationClips;
                if (clips != null && clips.Length > 0)
                {
                    var openClip = clips.FirstOrDefault(c => c != null && c.name == KOpenClipName);
                    if (openClip != null)
                    {
                        m_OpenAnimationClip = openClip;
                        m_ClosedPoseClip = openClip;
                        m_ClosedPoseTime = 0f;
                        m_OpenStartTime = 0f;
                        m_OpenEndTime = openClip.length;
                        Debug.Log($"[Digitron] Open animation source: Animator clip '{openClip.name}'");
                        EnsureLegacyAnimationComponent();
                        return;
                    }

                    var fallbackClip = clips.FirstOrDefault(c => c != null && c.name == KFallbackClipName);
                    if (fallbackClip != null)
                    {
                        m_OpenAnimationClip = fallbackClip;
                        m_ClosedPoseClip = fallbackClip;
                        m_ClosedPoseTime = Mathf.Min(KClosedPoseFrame / KTakeFrameRate, fallbackClip.length);
                        m_OpenStartTime = KOpenStartFrame / KTakeFrameRate;
                        m_OpenEndTime = Mathf.Min(KOpenEndFrame / KTakeFrameRate, fallbackClip.length);
                        Debug.Log($"[Digitron] Open animation source: Animator fallback '{fallbackClip.name}' [{m_OpenStartTime:F2}-{m_OpenEndTime:F2}]");
                        EnsureLegacyAnimationComponent();
                        return;
                    }
                }
            }
        }

        if (TryPrepareAnimationFromResourceSubAssets())
        {
            EnsureLegacyAnimationComponent();
            return;
        }

#if UNITY_EDITOR
        var standaloneClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(KStandaloneOpenClipAssetPath);
        if (standaloneClip != null)
        {
            m_OpenAnimationClip = standaloneClip;
            m_ClosedPoseClip = standaloneClip;
            m_ClosedPoseTime = 0f;
            m_OpenStartTime = 0f;
            m_OpenEndTime = standaloneClip.length;
            Debug.Log($"[Digitron] Open animation source: Editor standalone '{standaloneClip.name}'");
            EnsureLegacyAnimationComponent();
            return;
        }
#endif

        Debug.LogWarning("[Digitron] Open animation clip not found, using manual/fallback open.");
    }

    private void EnsureLegacyAnimationComponent()
    {
        // For Generic models in WebGL, Animator.Play()+Update(0) doesn't reliably sample poses.
        // Adding a runtime Legacy Animation component is the only approach confirmed to work in WebGL.
        if (m_OpenAnimationClip == null || m_LegacyAnimation != null) return;
        if (!m_OpenAnimationClip.legacy) m_OpenAnimationClip.legacy = true;
        m_LegacyAnimation = m_ModelInstance.AddComponent<Animation>();
        m_LegacyAnimation.playAutomatically = false;
        m_LegacyAnimation.AddClip(m_OpenAnimationClip, m_OpenAnimationClip.name);
        Debug.Log($"[Digitron] Added runtime Legacy Animation component for '{m_OpenAnimationClip.name}'");
    }

    private bool TryPrepareAnimationFromResourceSubAssets()
    {
        var clips = Resources.LoadAll(KRuntimeClipResourcesPath)
            .OfType<AnimationClip>()
            .ToArray();
        if (clips == null || clips.Length == 0)
        {
#if UNITY_EDITOR
            clips = AssetDatabase.LoadAllAssetsAtPath(KRuntimeClipEditorModelPath)
                .OfType<AnimationClip>()
                .ToArray();
#endif
        }

        // For Generic FBX models, sub-asset clips are not returned by Resources.LoadAll.
        // Try loading the standalone CalculatorOpen.anim directly from Resources.
        if (clips == null || clips.Length == 0)
        {
            var directClip = Resources.Load<AnimationClip>(KRuntimeStandaloneClipResourcesPath);
            if (directClip != null)
            {
                m_OpenAnimationClip = directClip;
                m_ClosedPoseClip = directClip;
                m_ClosedPoseTime = 0f;
                m_OpenStartTime = 0f;
                m_OpenEndTime = directClip.length;
                Debug.Log($"[Digitron] Open animation source: Resources direct '{directClip.name}'");
                return true;
            }
            return false;
        }

        var openClip = clips.FirstOrDefault(c => c != null && c.name == KOpenClipName);
        if (openClip != null)
        {
            m_OpenAnimationClip = openClip;
            m_ClosedPoseClip = openClip;
            m_ClosedPoseTime = 0f;
            m_OpenStartTime = 0f;
            m_OpenEndTime = openClip.length;
            Debug.Log($"[Digitron] Open animation source: Resource sub-asset '{openClip.name}'");
            return true;
        }

        var fallbackClip = clips.FirstOrDefault(c => c != null && c.name == KFallbackClipName);
        if (fallbackClip != null)
        {
            m_OpenAnimationClip = fallbackClip;
            m_ClosedPoseClip = fallbackClip;
            m_ClosedPoseTime = Mathf.Min(KClosedPoseFrame / KTakeFrameRate, fallbackClip.length);
            m_OpenStartTime = KOpenStartFrame / KTakeFrameRate;
            m_OpenEndTime = Mathf.Min(KOpenEndFrame / KTakeFrameRate, fallbackClip.length);
            Debug.Log($"[Digitron] Open animation source: Resource fallback '{fallbackClip.name}' [{m_OpenStartTime:F2}-{m_OpenEndTime:F2}]");
            return true;
        }

        return false;
    }

    private void TryPrepareManualOpenTransform()
    {
        m_FrontCoverTransform = FindBestFrontCoverTransform();
        if (m_FrontCoverTransform == null) return;
        m_FrontCoverClosedRotation = m_FrontCoverTransform.localRotation;
        m_FrontCoverOpenRotation = m_FrontCoverClosedRotation * Quaternion.Euler(0f, KManualOpenAngle, 0f);
    }

    private void ApplyClosedPose()
    {
        Debug.Log($"[Digitron] ApplyClosedPose: legacy={m_LegacyAnimation != null}, clip={m_ClosedPoseClip?.name ?? "null"}, time={m_ClosedPoseTime:F2}");
        // Only sample an animation clip for closed pose when we explicitly have one.
        // If only the open segment clip exists, keep importer bind pose as "closed".
        if (m_ClosedPoseClip != null)
        {
            SampleClipAtTime(m_ClosedPoseClip, m_ClosedPoseTime);
        }
        else if (m_FrontCoverTransform != null) m_FrontCoverTransform.localRotation = m_FrontCoverClosedRotation;
        RecalculateBounds();
    }

    private void SampleOpenClip(float normalizedTime)
    {
        if (m_OpenAnimationClip == null) return;
        var sampleTime = Mathf.Lerp(m_OpenStartTime, m_OpenEndTime, Mathf.Clamp01(normalizedTime));
        ApplyClipAtTime(m_OpenAnimationClip, sampleTime, disableAfter: false);
    }

    private void SampleClipAtTime(AnimationClip clip, float sampleTime)
    {
        if (clip == null) return;
        ApplyClipAtTime(clip, sampleTime, disableAfter: true);
    }

    private void ApplyClipAtTime(AnimationClip clip, float sampleTime, bool disableAfter)
    {
        if (clip == null) return;
        // Primary: use Animation component (Legacy)
        if (m_LegacyAnimation != null)
        {
            if (m_LegacyAnimation.GetClip(clip.name) == null)
                m_LegacyAnimation.AddClip(clip, clip.name);
            var state = m_LegacyAnimation[clip.name];
            if (state != null)
            {
                state.speed   = 0f;
                state.enabled = true;
                state.weight  = 1f;
                state.time    = Mathf.Clamp(sampleTime, 0f, clip.length);
                m_LegacyAnimation.Sample();
                if (disableAfter) state.enabled = false;
                return;
            }
        }
        // Secondary: use Animator (Generic)
        if (m_Animator != null && m_Animator.runtimeAnimatorController != null && clip.length > 0f)
        {
            m_Animator.enabled = true;
            m_Animator.Play(clip.name, 0, Mathf.Clamp01(sampleTime / clip.length));
            m_Animator.Update(0f);
            if (disableAfter) m_Animator.enabled = false;
            return;
        }
        // Fallback: direct clip sampling
        if (!clip.legacy) clip.legacy = true;
        clip.SampleAnimation(GetAnimationSampleTarget(), Mathf.Clamp(sampleTime, 0f, clip.length));
    }

    private GameObject GetAnimationSampleTarget()
    {
        return m_ModelInstance;
    }

    private void NormalizeModelScale(float targetSize)
    {
        var bounds = CalculateBounds();
        var maxDimension = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        if (maxDimension <= Mathf.Epsilon) return;
        m_ModelInstance.transform.localScale = Vector3.one * (targetSize / maxDimension);
    }

    private Bounds CalculateBounds()
    {
        var renderers = GetRelevantRenderers();
        if (renderers.Length == 0) return new Bounds(m_ModelInstance.transform.position, Vector3.one * 0.25f);
        var bounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    private void RecalculateBounds()
    {
        m_RelevantRenderers.Clear();
        m_ModelBounds = CalculateBounds();
        m_ModelLocalBounds = CalculateLocalBounds();
    }

    private Bounds CalculateLocalBounds()
    {
        var renderers = GetRelevantRenderers();
        if (renderers.Length == 0) return new Bounds(Vector3.zero, Vector3.one * 0.25f);
        var initialized = false;
        var localBounds = new Bounds();
        foreach (var renderer in renderers)
        {
            var rendererBounds = CalculateRendererLocalBounds(renderer);
            if (!initialized)
            {
                localBounds = rendererBounds;
                initialized = true;
            }
            else
            {
                localBounds.Encapsulate(rendererBounds.min);
                localBounds.Encapsulate(rendererBounds.max);
            }
        }

        return localBounds;
    }

    private Bounds CalculateRendererLocalBounds(Renderer renderer)
    {
        var min = renderer.bounds.min;
        var max = renderer.bounds.max;
        var corners = new[]
        {
            new Vector3(min.x, min.y, min.z), new Vector3(min.x, min.y, max.z), new Vector3(min.x, max.y, min.z), new Vector3(min.x, max.y, max.z),
            new Vector3(max.x, min.y, min.z), new Vector3(max.x, min.y, max.z), new Vector3(max.x, max.y, min.z), new Vector3(max.x, max.y, max.z),
        };

        var initialized = false;
        var localBounds = new Bounds();
        foreach (var corner in corners)
        {
            var localCorner = m_ModelInstance.transform.InverseTransformPoint(corner);
            if (!initialized)
            {
                localBounds = new Bounds(localCorner, Vector3.zero);
                initialized = true;
            }
            else
            {
                localBounds.Encapsulate(localCorner);
            }
        }

        return localBounds;
    }

    private Renderer[] GetRelevantRenderers()
    {
        if (m_ModelInstance == null) return System.Array.Empty<Renderer>();
        if (m_RelevantRenderers != null && m_RelevantRenderers.Count > 0)
        {
            m_RelevantRenderers.RemoveAll(renderer => renderer == null);
            if (m_RelevantRenderers.Count > 0) return m_RelevantRenderers.ToArray();
        }

        var allRenderers = m_ModelInstance.GetComponentsInChildren<Renderer>(true);
        if (allRenderers == null || allRenderers.Length == 0) return System.Array.Empty<Renderer>();
        if (allRenderers.Length == 1)
        {
            m_RelevantRenderers = new List<Renderer>(allRenderers);
            return allRenderers;
        }

        Renderer anchorRenderer = null;
        var anchorScore = float.MinValue;
        foreach (var renderer in allRenderers)
        {
            var size = renderer.bounds.size;
            var score = size.x * size.y * size.z;
            if (score <= anchorScore) continue;
            anchorScore = score;
            anchorRenderer = renderer;
        }

        if (anchorRenderer == null)
        {
            m_RelevantRenderers = new List<Renderer>(allRenderers);
            return allRenderers;
        }

        var cluster = new List<Renderer> { anchorRenderer };
        var clusterBounds = anchorRenderer.bounds;
        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var renderer in allRenderers)
            {
                if (cluster.Contains(renderer)) continue;
                var centerDistance = Vector3.Distance(clusterBounds.center, renderer.bounds.center);
                var threshold = Mathf.Max(clusterBounds.extents.magnitude * 2.4f, anchorRenderer.bounds.extents.magnitude * 3f) + renderer.bounds.extents.magnitude;
                if (centerDistance > threshold) continue;
                cluster.Add(renderer);
                clusterBounds.Encapsulate(renderer.bounds);
                changed = true;
            }
        }

        m_RelevantRenderers = cluster;
        return m_RelevantRenderers.ToArray();
    }

    private Vector3 GetWorldPointForNormalizedAnchor(Vector3 anchor)
    {
        return m_ModelInstance.transform.TransformPoint(GetLocalPointForNormalizedAnchor(anchor));
    }

    private Vector3 GetLocalPointForNormalizedAnchor(Vector3 anchor)
    {
        var min = m_ModelLocalBounds.min;
        var size = m_ModelLocalBounds.size;
        return new Vector3(min.x + (size.x * anchor.x), min.y + (size.y * anchor.y), min.z + (size.z * anchor.z));
    }

    private Vector3 GetFrontOffsetDirection()
    {
        return m_ModelInstance.transform.TransformDirection(Vector3.forward);
    }

    private void EnsureDisplayAnchor()
    {
        if (m_ModelDisplayTextRenderer == null)
        {
            foreach (var child in m_ModelInstance.GetComponentsInChildren<Transform>(true))
            {
                if (!child.name.ToLowerInvariant().Contains("textplus"))
                {
                    continue;
                }

                m_ModelDisplayTextTransform = child;
                m_ModelDisplayTextRenderer = child.GetComponent<Renderer>();
                if (m_ModelDisplayTextRenderer != null)
                {
                    break;
                }
            }
        }

        m_ModelDisplayTextMesh = null;

        if (m_DisplayAnchor == null)
        {
            var anchor = new GameObject("Digitron Display Anchor");
            anchor.transform.SetParent(m_ModelInstance.transform, false);
            m_DisplayAnchor = anchor.transform;
        }

        if (m_HiddenDisplayRenderers.Count == 0)
        {
            foreach (var child in m_ModelInstance.GetComponentsInChildren<Transform>(true))
            {
                if (!child.name.ToLowerInvariant().Contains("textplus"))
                {
                    continue;
                }

                if (m_ModelDisplayTextTransform == null)
                {
                    m_ModelDisplayTextTransform = child;
                    m_ModelDisplayTextRenderer = child.GetComponent<Renderer>();
                }

                var textMesh = child.GetComponent<TextMesh>();
                if (textMesh != null && m_ModelDisplayTextMesh == null)
                {
                    m_ModelDisplayTextMesh = textMesh;
                }

                foreach (var renderer in child.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer == m_ModelDisplayTextRenderer)
                    {
                        continue;
                    }

                    if (!m_HiddenDisplayRenderers.Contains(renderer))
                    {
                        m_HiddenDisplayRenderers.Add(renderer);
                    }
                }

                if (textMesh != null)
                {
                    textMesh.text = string.Empty;
                }
                else if (m_ModelDisplayTextRenderer != null && !m_HiddenDisplayRenderers.Contains(m_ModelDisplayTextRenderer))
                {
                    m_HiddenDisplayRenderers.Add(m_ModelDisplayTextRenderer);
                }
            }
        }

        foreach (var renderer in m_HiddenDisplayRenderers)
        {
            if (renderer != null)
            {
                renderer.enabled = false;
            }
        }

        if (m_ModelDisplayTextRenderer != null)
        {
            var localBounds = CalculateRendererLocalBounds(m_ModelDisplayTextRenderer);
            m_DisplayWorldSize = new Vector2(
                Mathf.Max(localBounds.size.x * 1.35f, 0.16f),
                Mathf.Max(localBounds.size.y * 1.45f, 0.05f));
        }

        // Always find the display surface — must happen before any early return
        if (m_DisplaySurface == null)
        {
            foreach (var child in m_ModelInstance.GetComponentsInChildren<Transform>(true))
            {
                if (child.name.ToLowerInvariant() == "plocica_01b")
                {
                    m_DisplaySurface = child;
                    break;
                }
            }
        }

        if (m_ModelDisplayTextTransform != null)
        {
            var anchorParent = m_ModelDisplayTextTransform.parent != null
                ? m_ModelDisplayTextTransform.parent
                : m_ModelInstance.transform;

            m_DisplayAnchor.SetParent(anchorParent, false);
            m_DisplayAnchor.localPosition = m_ModelDisplayTextTransform.localPosition;
            m_DisplayAnchor.localRotation = m_ModelDisplayTextTransform.localRotation;
            m_DisplayAnchor.localScale = m_ModelDisplayTextTransform.localScale;
            return;
        }

        m_DisplayAnchor.SetParent(m_ModelInstance.transform, false);
        m_DisplayAnchor.position = GetWorldPointForNormalizedAnchor(DisplayLayout.NormalizedAnchor) + (GetFrontOffsetDirection() * 0.004f);
        m_DisplayAnchor.rotation = m_ModelInstance.transform.rotation;
        m_DisplayAnchor.localScale = Vector3.one;
    }

    private void EnsureDisplayText()
    {
        if (m_ModelDisplayTextMesh != null)
        {
            m_DisplayText = m_ModelDisplayTextMesh;
            // Reposition TextPlus001 to the correct display window location within plocica_01B
            if (m_DisplaySurface != null)
            {
                m_DisplayText.transform.SetParent(m_DisplaySurface, false);
                m_DisplayText.transform.localPosition = new Vector3(-0.251f, 0.041f, 0.031f);
                m_DisplayText.transform.localRotation = Quaternion.Euler(0f, -180f, 0f);
                m_DisplayText.transform.localScale = Vector3.one;
            }
        }
        else
        {
            if (m_DisplayAnchor == null)
            {
                return;
            }

            if (m_DisplayText == null)
            {
                var textObject = new GameObject("Digitron Display Text");
                textObject.transform.SetParent(transform, false);
                m_DisplayText = textObject.AddComponent<TextMesh>();
                var renderer = m_DisplayText.GetComponent<Renderer>();
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.sortingOrder = 50;
            }

            if (m_DisplaySurface != null)
            {
                // Parent to display board so text tracks it during animation.
                // Use world-space position/rotation so the text faces camera
                // regardless of plocica_01B's own tilt (X:105 in FBX).
                // Local position within plocica_01B that lands on the display window
                // (read from inspector after manual placement)
                m_DisplayText.transform.SetParent(m_DisplaySurface, false);
                m_DisplayText.transform.localPosition = new Vector3(-0.251f, 0.041f, 0.031f);
                m_DisplayText.transform.localRotation = Quaternion.Euler(0f, -180f, 0f);
                m_DisplayText.transform.localScale = Vector3.one;
                m_DisplayText.characterSize = 0.0052f;
            }
            else
            {
                m_DisplayText.transform.SetParent(transform, false);
                m_DisplayText.transform.localPosition = new Vector3(-0.176f, 0.398f, -0.046f);
                m_DisplayText.transform.localRotation = Quaternion.Euler(0f, -180f, 0f);
                m_DisplayText.transform.localScale = Vector3.one;
            }
        }

        if (m_DisplayFont == null)
        {
            m_DisplayFont = Resources.Load<Font>("digital-7 (mono)");
#if UNITY_EDITOR
            if (m_DisplayFont == null)
                m_DisplayFont = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>(KDisplayFontAssetPath);
#endif
        }

        m_DisplayText.anchor = TextAnchor.MiddleRight;
        m_DisplayText.alignment = TextAlignment.Right;
        m_DisplayText.fontSize = 128;
        if (m_DisplaySurface == null)
        {
            m_DisplayText.characterSize = 0.0052f;
        }
        m_DisplayText.color = new Color(1f, 0.08f, 0.02f, 1f);

        if (m_DisplayFont != null)
        {
            m_DisplayText.font = m_DisplayFont;
            m_DisplayText.GetComponent<MeshRenderer>().material = m_DisplayFont.material;
        }

        UpdateDisplayText();
        ApplyDepthTestingMaterialToDisplay();
    }

    private void ApplyDepthTestingMaterialToDisplay()
    {
        if (m_DisplayText == null) return;
        var r = m_DisplayText.GetComponent<MeshRenderer>();
        if (r == null) return;
        // GUI/Text Shader correctly renders TextMesh vertex colors (white glyph on transparent).
        var shader = Shader.Find("GUI/Text Shader");
        if (shader == null) return;
        var tex = r.sharedMaterial != null ? r.sharedMaterial.mainTexture : null;
        var mat = new Material(shader);
        if (tex != null) mat.mainTexture = tex;
        mat.color = m_DisplayText.color;
        mat.renderQueue = 3000;
        r.material = mat;
    }

    private void UpdateDisplayText()
    {
        if (m_DisplayText == null)
        {
            return;
        }

        m_DisplayText.text = m_Runtime.IsPoweredOn ? m_Runtime.DisplayText : string.Empty;
    }

    private void EnsureHotspotAnchors()
    {
        foreach (var hotspot in Hotspots)
        {
            if (m_HotspotAnchors.ContainsKey(hotspot.Id) && m_HotspotAnchors[hotspot.Id] != null)
                continue;

            // Special case: display hotspot anchors directly to the known display surface
            if (hotspot.Id == "display" && m_ModelDisplayTextTransform != null)
            {
                var dispAnchor = new GameObject($"Hotspot Anchor {hotspot.Id}");
                dispAnchor.transform.SetParent(m_ModelDisplayTextTransform, false);
                dispAnchor.transform.localPosition = Vector3.zero;
                m_HotspotAnchors[hotspot.Id] = dispAnchor.transform;
                continue;
            }

            Transform anchorParent = m_ModelInstance.transform;
            Vector3 anchorWorldPos = GetWorldPointForNormalizedAnchor(hotspot.NormalizedViewportAnchor);

            if (KHotspotMeshKeywords.TryGetValue(hotspot.Id, out var keywords))
            {
                // Use specific anchor keywords if defined (most representative visible part),
                // otherwise fall back to the full mesh keyword list
                var anchorKeywords = KHotspotAnchorKeywords.TryGetValue(hotspot.Id, out var ak) ? ak : keywords;

                var anchorMatching = new List<Renderer>();
                foreach (var r in m_ModelInstance.GetComponentsInChildren<Renderer>(true))
                    if (IsRendererForHotspot(r, anchorKeywords)) anchorMatching.Add(r);

                // Fall back to full keywords if anchor-specific search found nothing
                if (anchorMatching.Count == 0)
                    foreach (var r in m_ModelInstance.GetComponentsInChildren<Renderer>(true))
                        if (IsRendererForHotspot(r, keywords)) anchorMatching.Add(r);

                if (anchorMatching.Count > 0)
                {
                    var b = anchorMatching[0].bounds;
                    for (var i = 1; i < anchorMatching.Count; i++) b.Encapsulate(anchorMatching[i].bounds);
                    anchorWorldPos = b.center;
                    var bestAnchorRenderer = anchorMatching[0];
                    var bestDist = float.MaxValue;
                    foreach (var r in anchorMatching)
                    {
                        var d = Vector3.Distance(r.bounds.center, anchorWorldPos);
                        if (d < bestDist) { bestDist = d; bestAnchorRenderer = r; }
                    }
                    anchorParent = bestAnchorRenderer.transform;
                }
                else
                {
                    Debug.Log($"[Hotspot] No mesh found for '{hotspot.Id}', using normalized fallback.");
                }
            }

            var anchorObject = new GameObject($"Hotspot Anchor {hotspot.Id}");
            anchorObject.transform.SetParent(anchorParent, true);
            anchorObject.transform.position = anchorWorldPos;
            anchorObject.transform.rotation = m_ModelInstance.transform.rotation;
            m_HotspotAnchors[hotspot.Id] = anchorObject.transform;
        }
    }

    private void EnsureLampVisual()
    {
        if (m_LampRenderer == null)
        {
            var lampObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            lampObject.name = "Digitron Power Lamp";
            lampObject.transform.SetParent(transform, false);
            lampObject.transform.localScale = Vector3.one * KLampScale;
            m_LampRenderer = lampObject.GetComponent<Renderer>();
            m_LampRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            m_LampRenderer.receiveShadows = false;
            m_LampMaterial = new Material(Shader.Find("Unlit/Color") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Standard"));
            m_LampMaterial.color = new Color(1f, 0.14f, 0.14f, 1f);   // always bright red
            m_LampRenderer.material = m_LampMaterial;
            var collider = lampObject.GetComponent<Collider>();
            if (collider) Destroy(collider);
        }

        // Find the lampica mesh anchor once
        if (m_LampAnchor == null && m_ModelInstance != null)
        {
            foreach (var r in m_ModelInstance.GetComponentsInChildren<Renderer>(true))
            {
                var n = r.gameObject.name.ToLowerInvariant();
                var pn = r.transform.parent != null ? r.transform.parent.name.ToLowerInvariant() : "";
                if (n.Contains("lampica") || pn.Contains("lampica"))
                {
                    m_LampAnchor = r.transform;
                    break;
                }
            }
        }

        // Position on lampica mesh; fall back to hardcoded if not found
        if (m_LampAnchor != null)
            m_LampRenderer.transform.position = m_LampAnchor.position + GetFrontOffsetDirection() * 0.005f;
        else
            m_LampRenderer.transform.position = GetWorldPointForNormalizedAnchor(new Vector3(0.815f, 0.63f, KFrontSurfaceDepth)) + (GetFrontOffsetDirection() * 0.007f);
    }

    private void EnsureDisplayMask()
    {
        // Display mask disabled — no-op
    }

    private void UpdateLampFacingCamera()
    {
        if (!m_LampRenderer || !m_TargetCamera) return;
        // Track lampica mesh position every frame (handles animation/explosion)
        if (m_LampAnchor != null)
            m_LampRenderer.transform.position = m_LampAnchor.position + GetFrontOffsetDirection() * 0.005f;
        m_LampRenderer.transform.LookAt(m_TargetCamera.transform.position, Vector3.up);
        m_LampRenderer.transform.Rotate(0f, 180f, 0f);
    }

    private void UpdateHotspotMarkersFacingCamera()
    {
        if (!m_TargetCamera) return;
        foreach (var marker in m_HotspotMarkers.Values)
        {
            if (!marker || !marker.gameObject.activeSelf) continue;
            marker.transform.LookAt(m_TargetCamera.transform.position, Vector3.up);
            marker.transform.Rotate(0f, 180f, 0f);
            marker.UpdateLine();
        }
    }

    private void UpdateDisplayTextVisibility()
    {
        if (m_DisplayText == null || !m_TargetCamera) return;
        var renderer = m_DisplayText.GetComponent<MeshRenderer>();
        if (renderer == null) return;
        // Display only shown when calculator is closed — hide during opening/opened states
        if (m_State != DigitronState.PlacedClosed)
        {
            renderer.enabled = false;
            return;
        }
        if (m_DisplayAnchor == null)
        {
            renderer.enabled = m_Runtime.IsPoweredOn;
            return;
        }
        var toCamera = (m_TargetCamera.transform.position - m_DisplayAnchor.position).normalized;
        var isFacing = Vector3.Dot(toCamera, m_DisplayAnchor.forward) > 0f;
        renderer.enabled = isFacing && m_Runtime.IsPoweredOn;
    }

    private void ResetCalculatorRuntime()
    {
        m_Runtime.PowerOnDefault();
        RefreshCalculatorPresentation();
    }

    private void RefreshCalculatorPresentation()
    {
        if (m_LampRenderer != null)
            m_LampRenderer.enabled = m_Runtime.IsPoweredOn;
        if (m_PowerSwitchTransform != null)
        {
            m_PowerSwitchTransform.localPosition = m_Runtime.IsPoweredOn ? m_PowerSwitchOnLocalPosition : m_PowerSwitchOffLocalPosition;
        }
        UpdateDisplayText();
    }

    private void RebuildKeyTargets()
    {
        foreach (var keyTarget in m_KeyTargets) if (keyTarget) Destroy(keyTarget.gameObject);
        m_KeyTargets.Clear();
        foreach (var routine in m_KeyPressRoutines.Values)
        {
            if (routine != null)
            {
                StopCoroutine(routine);
            }
        }
        m_KeyPressRoutines.Clear();
        if (TryBuildPhysicalKeyTargets()) return;
        m_UsePhysicalKeyTargets = false;
        foreach (var keyLayout in KeyLayouts)
        {
            var keyObject = new GameObject($"Key {keyLayout.Name}");
            keyObject.transform.SetParent(transform, true);
            keyObject.transform.position = GetWorldPointForNormalizedAnchor(keyLayout.NormalizedAnchor) + (GetFrontOffsetDirection() * KFrontSurfaceOffset);
            keyObject.transform.rotation = m_ModelInstance.transform.rotation;
            var collider = keyObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(m_ModelLocalBounds.size.x * keyLayout.NormalizedSize.x, m_ModelLocalBounds.size.y * keyLayout.NormalizedSize.y, m_ModelBounds.size.z * 0.04f);
            var keyTarget = keyObject.AddComponent<DigitronKeyHitTarget>();
            keyTarget.Initialize(this, keyLayout.KeyId);
            m_KeyTargets.Add(keyTarget);
        }
    }

    private bool TryBuildPhysicalKeyTargets()
    {
        var candidates = CollectPhysicalKeyCandidates();
        if (candidates.Count < 15) return false;
        var hasNamedTargets = TryCreateNamedPhysicalKeyTargets();
        if (!hasNamedTargets)
        {
            TryCreateSortedPhysicalKeyTargets(candidates);
        }
        EnsureProjectedTargetsForLowerRows();
        CreateFallbackProjectedKeyTarget(KeyLayouts.First(layout => layout.KeyId == DigitronKeyId.Power));
        TryPreparePowerSwitchVisual(candidates);
        m_UsePhysicalKeyTargets = m_KeyTargets.Count >= 10;
        return m_UsePhysicalKeyTargets;
    }

    private bool TryCreateNamedPhysicalKeyTargets()
    {
        var nameToKeyId = new Dictionary<string, DigitronKeyId>
        {
            ["tipka_f"] = DigitronKeyId.F,
            ["tipka_ce"] = DigitronKeyId.ClearEntry,
            ["tipka_c"] = DigitronKeyId.ClearAll,
            ["tipka_jednako"] = DigitronKeyId.Equals,
            ["tipka_7"] = DigitronKeyId.Seven,
            ["tipka_8"] = DigitronKeyId.Eight,
            ["tipka_9"] = DigitronKeyId.Nine,
            ["tipka_minus"] = DigitronKeyId.Subtract,
            ["tipka_4"] = DigitronKeyId.Four,
            ["tipka_5"] = DigitronKeyId.Five,
            ["tipka_6"] = DigitronKeyId.Six,
            ["tipka_djeljeno"] = DigitronKeyId.Divide,
            ["tipka_1"] = DigitronKeyId.One,
            ["tipka_2"] = DigitronKeyId.Two,
            ["tipka_3"] = DigitronKeyId.Three,
            ["tipka_puta"] = DigitronKeyId.Multiply,
            ["tipka_nula"] = DigitronKeyId.Zero,
            ["tipka_desimala"] = DigitronKeyId.Decimal,
            ["tipka_plus"] = DigitronKeyId.Add,
        };

        var namedCount = 0;
        foreach (var renderer in m_ModelInstance.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null)
            {
                continue;
            }

            var rendererName = renderer.transform.name.ToLowerInvariant();
            if (!nameToKeyId.TryGetValue(rendererName, out var keyId))
            {
                continue;
            }

            if (m_KeyTargets.Any(target => target != null && target.KeyId == keyId))
            {
                continue;
            }

            var keyLayout = KeyLayouts.First(layout => layout.KeyId == keyId);
            CreatePhysicalKeyTarget(keyLayout, renderer);
            namedCount++;
        }

        return namedCount >= 18;
    }

    private void TryCreateSortedPhysicalKeyTargets(List<Renderer> candidates)
    {
        var keypadCandidates = new List<Renderer>(candidates);
        keypadCandidates.Sort((left, right) => CalculateRendererLocalBounds(right).center.y.CompareTo(CalculateRendererLocalBounds(left).center.y));
        var rows = new List<List<Renderer>>();
        var rowThreshold = Mathf.Max(m_ModelLocalBounds.size.y * 0.035f, 0.01f);
        foreach (var candidate in keypadCandidates)
        {
            var centerY = CalculateRendererLocalBounds(candidate).center.y;
            List<Renderer> targetRow = null;
            foreach (var row in rows)
            {
                var rowCenterY = CalculateRendererLocalBounds(row[0]).center.y;
                if (Mathf.Abs(rowCenterY - centerY) <= rowThreshold)
                {
                    targetRow = row;
                    break;
                }
            }

            if (targetRow == null)
            {
                targetRow = new List<Renderer>();
                rows.Add(targetRow);
            }
            targetRow.Add(candidate);
        }

        rows.RemoveAll(row => row.Count < 2);
        rows.Sort((left, right) => CalculateRendererLocalBounds(right[0]).center.y.CompareTo(CalculateRendererLocalBounds(left[0]).center.y));
        if (rows.Count < 5) return;

        var expectedRows = new[]
        {
            new[] { DigitronKeyId.F, DigitronKeyId.ClearEntry, DigitronKeyId.ClearAll, DigitronKeyId.Equals },
            new[] { DigitronKeyId.Seven, DigitronKeyId.Eight, DigitronKeyId.Nine, DigitronKeyId.Subtract },
            new[] { DigitronKeyId.Four, DigitronKeyId.Five, DigitronKeyId.Six, DigitronKeyId.Divide },
            new[] { DigitronKeyId.One, DigitronKeyId.Two, DigitronKeyId.Three, DigitronKeyId.Multiply },
            new[] { DigitronKeyId.Zero, DigitronKeyId.Decimal, DigitronKeyId.Add },
        };

        for (var rowIndex = 0; rowIndex < expectedRows.Length; rowIndex++)
        {
            var row = rows[rowIndex];
            row.Sort((left, right) => CalculateRendererLocalBounds(left).center.x.CompareTo(CalculateRendererLocalBounds(right).center.x));
            if (row.Count < expectedRows[rowIndex].Length) continue;
            if (row.Count > expectedRows[rowIndex].Length)
            {
                row = row.Take(expectedRows[rowIndex].Length).ToList();
            }

            for (var columnIndex = 0; columnIndex < expectedRows[rowIndex].Length; columnIndex++)
            {
                var keyId = expectedRows[rowIndex][columnIndex];
                var keyLayout = KeyLayouts.First(layout => layout.KeyId == keyId);
                CreatePhysicalKeyTarget(keyLayout, row[columnIndex]);
            }
        }
    }

    private void EnsureProjectedTargetsForLowerRows()
    {
        var requiredKeyIds = new[]
        {
            DigitronKeyId.F,
            DigitronKeyId.One,
            DigitronKeyId.Two,
            DigitronKeyId.Three,
            DigitronKeyId.Zero,
            DigitronKeyId.Decimal,
            DigitronKeyId.Add,
            DigitronKeyId.Multiply,
            DigitronKeyId.Four,
            DigitronKeyId.Five,
            DigitronKeyId.Six,
        };

        foreach (var keyId in requiredKeyIds)
        {
            var alreadyExists = m_KeyTargets.Any(target => target != null && target.KeyId == keyId);
            if (alreadyExists)
            {
                continue;
            }

            var layout = KeyLayouts.First(entry => entry.KeyId == keyId);
            CreateFallbackProjectedKeyTarget(layout);
        }
    }

    private void CreateFallbackProjectedKeyTarget(KeyLayoutData keyLayout)
    {
        var keyObject = new GameObject($"Key {keyLayout.Name}");
        keyObject.transform.SetParent(transform, true);
        keyObject.transform.position = GetWorldPointForNormalizedAnchor(keyLayout.NormalizedAnchor) + (GetFrontOffsetDirection() * KFrontSurfaceOffset);
        keyObject.transform.rotation = m_ModelInstance.transform.rotation;
        var collider = keyObject.AddComponent<BoxCollider>();
        collider.size = new Vector3(
            m_ModelLocalBounds.size.x * keyLayout.NormalizedSize.x,
            m_ModelLocalBounds.size.y * keyLayout.NormalizedSize.y,
            Mathf.Max(m_ModelBounds.size.z * 0.04f, 0.01f));
        var pressTarget = FindNearestRendererTransform(keyLayout.NormalizedAnchor);
        var pressOffset = pressTarget != null
            ? pressTarget.localRotation * (Vector3.back * KPhysicalKeyPressDepth)
            : Vector3.zero;
        var keyTarget = keyObject.AddComponent<DigitronKeyHitTarget>();
        keyTarget.Initialize(this, keyLayout.KeyId, pressTarget, pressOffset);
        m_KeyTargets.Add(keyTarget);
    }

    private Transform FindNearestRendererTransform(Vector3 normalizedAnchor)
    {
        var expectedLocal = GetLocalPointForNormalizedAnchor(normalizedAnchor);
        Renderer best = null;
        var bestScore = float.MaxValue;
        foreach (var renderer in CollectPhysicalKeyCandidates())
        {
            var localBounds = CalculateRendererLocalBounds(renderer);
            var center = localBounds.center;
            var score = Vector2.Distance(new Vector2(center.x, center.y), new Vector2(expectedLocal.x, expectedLocal.y));
            if (score >= bestScore) continue;
            bestScore = score;
            best = renderer;
        }

        return best != null ? best.transform : null;
    }

    private List<Renderer> CollectPhysicalKeyCandidates()
    {
        var results = new List<Renderer>();
        foreach (var renderer in GetRelevantRenderers())
        {
            var localBounds = CalculateRendererLocalBounds(renderer);
            var center = localBounds.center;
            var size = localBounds.size;
            if (center.y < (m_ModelLocalBounds.min.y + (m_ModelLocalBounds.size.y * 0.10f)) ||
                center.y > (m_ModelLocalBounds.min.y + (m_ModelLocalBounds.size.y * 0.62f))) continue;
            if (center.x < (m_ModelLocalBounds.min.x + (m_ModelLocalBounds.size.x * 0.08f)) ||
                center.x > (m_ModelLocalBounds.min.x + (m_ModelLocalBounds.size.x * 0.96f))) continue;
            if (size.x < (m_ModelLocalBounds.size.x * 0.02f) || size.x > (m_ModelLocalBounds.size.x * 0.22f)) continue;
            if (size.y < (m_ModelLocalBounds.size.y * 0.02f) || size.y > (m_ModelLocalBounds.size.y * 0.16f)) continue;
            results.Add(renderer);
        }

        return results;
    }

    private void CreatePhysicalKeyTarget(KeyLayoutData keyLayout, Renderer renderer)
    {
        var localBounds = CalculateRendererLocalBounds(renderer);
        var keyObject = new GameObject($"Key {keyLayout.Name}");
        keyObject.transform.SetParent(transform, true);
        keyObject.transform.position = m_ModelInstance.transform.TransformPoint(localBounds.center) + (GetFrontOffsetDirection() * 0.003f);
        keyObject.transform.rotation = renderer.transform.rotation;
        var collider = keyObject.AddComponent<BoxCollider>();
        collider.size = new Vector3(localBounds.size.x * 1.08f, localBounds.size.y * 1.08f, Mathf.Max(localBounds.size.z, m_ModelBounds.size.z * 0.03f));
        var pressOffset = renderer.transform.localRotation * (Vector3.back * KPhysicalKeyPressDepth);
        var keyTarget = keyObject.AddComponent<DigitronKeyHitTarget>();
        keyTarget.Initialize(this, keyLayout.KeyId, renderer.transform, pressOffset);
        m_KeyTargets.Add(keyTarget);
    }

    private void TryPreparePowerSwitchVisual(List<Renderer> candidates)
    {
        var expectedLocal = GetLocalPointForNormalizedAnchor(KeyLayouts.First(layout => layout.KeyId == DigitronKeyId.Power).NormalizedAnchor);
        Renderer best = null;
        var bestScore = float.MaxValue;
        foreach (var candidate in candidates)
        {
            var localBounds = CalculateRendererLocalBounds(candidate);
            var center = localBounds.center;
            var dx = Mathf.Abs(center.x - expectedLocal.x);
            var dy = Mathf.Abs(center.y - expectedLocal.y);
            var score = dx + (dy * 1.2f);
            if (score >= bestScore) continue;
            bestScore = score;
            best = candidate;
        }

        if (best == null) return;
        m_PowerSwitchTransform = best.transform;
        m_PowerSwitchOnLocalPosition = m_PowerSwitchTransform.localPosition;
        m_PowerSwitchOffLocalPosition = m_PowerSwitchOnLocalPosition + (m_PowerSwitchTransform.InverseTransformDirection(m_ModelInstance.transform.right) * 0.01f);
    }

    private void SetCalculatorInteractionVisible(bool isVisible)
    {
        foreach (var keyTarget in m_KeyTargets) if (keyTarget) keyTarget.gameObject.SetActive(isVisible);
        if (m_LampRenderer) m_LampRenderer.gameObject.SetActive(isVisible);
    }

    private void HandleCalculatorPointerInput()
    {
        if (m_State != DigitronState.PlacedClosed || !m_TargetCamera) return;
        Vector3 pointerPosition;
        if (Input.touchCount > 0)
        {
            var touch = Input.GetTouch(0);
            if (touch.phase != TouchPhase.Began) return;
            pointerPosition = touch.position;
        }
        else if (Input.GetMouseButtonDown(0))
        {
            pointerPosition = Input.mousePosition;
        }
        else
        {
            return;
        }

        var hits = Physics.RaycastAll(m_TargetCamera.ScreenPointToRay(pointerPosition), 100f);
        if (hits == null || hits.Length == 0) return;
        System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
        foreach (var hit in hits)
        {
            var keyTarget = hit.collider.GetComponent<DigitronKeyHitTarget>();
            if (keyTarget == null) continue;
            HandlePhysicalKeyTargetPressed(keyTarget);
            return;
        }
    }

    private IEnumerator AnimatePhysicalKeyPress(DigitronKeyHitTarget keyTarget)
    {
        var target = keyTarget.PressTarget;
        if (target == null) yield break;
        var rest = keyTarget.RestLocalPosition;
        var pressed = rest + keyTarget.PressLocalOffset;
        var elapsed = 0f;
        while (elapsed < KPhysicalKeyPressDuration)
        {
            elapsed += Time.deltaTime;
            target.localPosition = Vector3.Lerp(rest, pressed, Mathf.Clamp01(elapsed / KPhysicalKeyPressDuration));
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < KPhysicalKeyPressDuration)
        {
            elapsed += Time.deltaTime;
            target.localPosition = Vector3.Lerp(pressed, rest, Mathf.Clamp01(elapsed / KPhysicalKeyPressDuration));
            yield return null;
        }

        target.localPosition = rest;
        m_KeyPressRoutines.Remove(target);
    }

    private void RebuildHotspotMarkers()
    {
        // Destroy old anchors so they are recomputed from the current (open) model state
        foreach (var anchor in m_HotspotAnchors.Values) if (anchor) Destroy(anchor.gameObject);
        m_HotspotAnchors.Clear();
        EnsureHotspotAnchors();

        foreach (var marker in m_HotspotMarkers.Values) if (marker) Destroy(marker.gameObject);
        m_HotspotMarkers.Clear();
        foreach (var hotspot in Hotspots)
        {
            var markerObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            markerObject.name = hotspot.Id;
            markerObject.transform.SetParent(transform, true);
            // Stretch background to fit the label text
            var titleLines   = hotspot.Title.Split('\n');
            var longestLine  = 0;
            foreach (var ln in titleLines) if (ln.Length > longestLine) longestLine = ln.Length;
            var bgW = Mathf.Max(1f, longestLine * 0.11f) * KHotspotScale;
            var bgH = (titleLines.Length > 1 ? 1.7f : 1.0f) * KHotspotScale;
            markerObject.transform.localScale = new Vector3(bgW, bgH, KHotspotScale);
            var anchor = m_HotspotAnchors.TryGetValue(hotspot.Id, out var hotspotAnchor) ? hotspotAnchor : null;
            // Place marker near its anchor mesh, offset in MarkerLocalDir direction
            var anchorPos = anchor != null
                ? anchor.position
                : GetWorldPointForNormalizedAnchor(hotspot.NormalizedViewportAnchor);
            var spreadDist = m_ModelLocalBounds.size.magnitude * 0.14f;
            var perHotspotDistance = spreadDist * Mathf.Max(1f, hotspot.MarkerLocalDir.magnitude);
            var worldOffset = m_ModelInstance.transform.TransformDirection(hotspot.MarkerLocalDir.normalized) * perHotspotDistance;
            var markerPos = anchorPos + worldOffset;
            markerObject.transform.position = markerPos;
            markerObject.transform.rotation = anchor ? anchor.rotation : m_ModelInstance.transform.rotation;
            var renderer = markerObject.GetComponent<Renderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.material = CreateHotspotMaterial();
            var marker = markerObject.AddComponent<DigitronHotspotMarker>();
            try { marker.Initialize(this, hotspot.Id, hotspot.Title, m_TexSoloTrakica); }
            catch (System.Exception e) { Debug.LogWarning($"[Hotspot] marker init failed for {hotspot.Id}: {e.Message}"); }
            if (anchor != null) marker.InitLine(anchor);
            m_HotspotMarkers.Add(hotspot.Id, marker);
        }
    }

    private void SetHotspotsVisible(bool isVisible)
    {
        foreach (var marker in m_HotspotMarkers.Values) if (marker) marker.gameObject.SetActive(isVisible);
    }

    private void UpdateHotspotMarkerStates()
    {
        foreach (var kv in m_HotspotMarkers)
            kv.Value?.SetSelected(kv.Key == m_SelectedHotspotId);
    }

    private void ApplyHotspotHighlight(string selectedId)
    {
        if (m_ModelInstance == null) return;
        var allRenderers = m_ModelInstance.GetComponentsInChildren<Renderer>(true);
        if (allRenderers == null) return;
        var hasSelection = !string.IsNullOrEmpty(selectedId);

        string[] selectedKeywords = null;
        if (hasSelection) KHotspotMeshKeywords.TryGetValue(selectedId, out selectedKeywords);

        if (m_DimBlock == null) m_DimBlock = new MaterialPropertyBlock();
        m_DimBlock.Clear();
        m_DimBlock.SetColor("_Color", new Color(0.22f, 0.22f, 0.22f, 1f));

        foreach (var r in allRenderers)
        {
            if (r == null) continue;
            if (!hasSelection || IsRendererForHotspot(r, selectedKeywords))
                r.SetPropertyBlock(null);
            else
                r.SetPropertyBlock(m_DimBlock);
        }
    }

    private static bool IsRendererForHotspot(Renderer r, string[] keywords)
    {
        if (keywords == null) return false;
        var name = r.gameObject.name.ToLowerInvariant();
        foreach (var kw in keywords) if (name.Contains(kw)) return true;
        // FBX imports often put MeshRenderer on a child of the named node — check parent name too
        var parent = r.transform.parent;
        if (parent != null)
        {
            var parentName = parent.name.ToLowerInvariant();
            foreach (var kw in keywords) if (parentName.Contains(kw)) return true;
        }
        return false;
    }

    private static Material CreateHotspotMaterial()
    {
        var material = new Material(Shader.Find("Unlit/Color") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Standard"));
        material.color = new Color(0.96f, 0.92f, 0.80f, 1f); // cream
        return material;
    }

    private Transform FindChildContaining(string token)
    {
        if (m_ModelInstance == null) return null;
        var needle = token.ToLowerInvariant();
        foreach (var child in m_ModelInstance.GetComponentsInChildren<Transform>(true))
        {
            if (child == m_ModelInstance.transform) continue;
            if (child.name.ToLowerInvariant().Contains(needle)) return child;
        }

        return null;
    }

    private Transform FindBestFrontCoverTransform()
    {
        var directMatch = FindChildContaining("prednja") ??
                          FindChildContaining("poklopac") ??
                          FindChildContaining("front") ??
                          FindChildContaining("cover");
        if (directMatch != null) return directMatch;
        if (m_ModelInstance == null) return null;

        Renderer bestRenderer = null;
        var bestScore = float.MinValue;
        foreach (var renderer in GetRelevantRenderers())
        {
            var localBounds = CalculateRendererLocalBounds(renderer);
            var center = localBounds.center;
            var size = localBounds.size;
            var normalizedY = Mathf.InverseLerp(m_ModelLocalBounds.min.y, m_ModelLocalBounds.max.y, center.y);
            var normalizedZ = Mathf.InverseLerp(m_ModelLocalBounds.min.z, m_ModelLocalBounds.max.z, center.z);
            var score = (size.x * size.y) + (normalizedY * 0.3f) + (normalizedZ * 0.2f);
            if (normalizedY < 0.45f) continue;
            if (score <= bestScore) continue;
            bestScore = score;
            bestRenderer = renderer;
        }

        return bestRenderer ? bestRenderer.transform : null;
    }

    private static Texture2D MakeTex(Color c)
    {
        var t = new Texture2D(1, 1);
        t.SetPixel(0, 0, c);
        t.Apply();
        return t;
    }

    private void EnsureGuiStyles()
    {
        if (m_OpenButtonStyle != null) return;

        // Try loading from Resources if not assigned via Inspector
        m_TexSoloTrakica   ??= Resources.Load<Texture2D>("solo trakica");
        m_TexPapirPodloga  ??= Resources.Load<Texture2D>("papir podloga kvadrati");
        m_TexDigitronNaslov ??= Resources.Load<Texture2D>("digitron naslov");

        // Textures: use assigned assets, fall back to solid colours
        var btnBg    = m_TexSoloTrakica  ?? MakeTex(new Color(0.14f, 0.14f, 0.20f, 0.96f));
        var panelBg  = m_TexPapirPodloga ?? MakeTex(new Color(0.06f, 0.06f, 0.10f, 0.93f));
        var lightBg  = m_TexSoloTrakica  != null;  // true when using light paper texture
        var lightPanel = m_TexPapirPodloga != null;

        var btnTextColor  = lightBg    ? new Color(0.12f, 0.06f, 0.02f) : Color.white;
        var titleColor    = lightPanel ? new Color(0.12f, 0.06f, 0.02f) : Color.white;
        var descColor     = lightPanel ? new Color(0.22f, 0.10f, 0.04f) : new Color(0.80f, 0.80f, 0.80f);
        var closeColor    = lightPanel ? new Color(0.30f, 0.15f, 0.05f) : new Color(0.65f, 0.65f, 0.65f);

        m_OpenButtonStyle = new GUIStyle(GUI.skin.button);
        m_OpenButtonStyle.fontSize    = 16;
        m_OpenButtonStyle.fontStyle   = FontStyle.Bold;
        m_OpenButtonStyle.alignment   = TextAnchor.MiddleCenter;
        m_OpenButtonStyle.normal.background  = btnBg;
        m_OpenButtonStyle.normal.textColor   = btnTextColor;
        m_OpenButtonStyle.hover.background   = btnBg;
        m_OpenButtonStyle.hover.textColor    = btnTextColor;
        m_OpenButtonStyle.active.background  = btnBg;
        m_OpenButtonStyle.active.textColor   = btnTextColor;
        m_OpenButtonStyle.border = new RectOffset(4, 4, 4, 4);

        m_InfoBoxStyle = new GUIStyle(GUI.skin.box);
        m_InfoBoxStyle.padding = new RectOffset(0, 0, 0, 0);
        m_InfoBoxStyle.border  = new RectOffset(4, 4, 4, 4);
        m_InfoBoxStyle.normal.background = panelBg;

        m_InfoTitleStyle = new GUIStyle(GUI.skin.label);
        m_InfoTitleStyle.fontSize   = 15;
        m_InfoTitleStyle.fontStyle  = FontStyle.Bold;
        m_InfoTitleStyle.wordWrap   = false;
        m_InfoTitleStyle.normal.textColor = titleColor;

        m_InfoDescStyle = new GUIStyle(GUI.skin.label);
        m_InfoDescStyle.fontSize  = 12;
        m_InfoDescStyle.wordWrap  = true;
        m_InfoDescStyle.normal.textColor = descColor;

        m_CloseButtonStyle = new GUIStyle(GUI.skin.button);
        m_CloseButtonStyle.fontSize   = 20;
        m_CloseButtonStyle.fontStyle  = FontStyle.Bold;
        m_CloseButtonStyle.alignment  = TextAnchor.MiddleCenter;
        m_CloseButtonStyle.normal.textColor  = closeColor;
        m_CloseButtonStyle.hover.textColor   = lightPanel ? new Color(0.7f, 0.1f, 0f) : Color.white;
        m_CloseButtonStyle.active.textColor  = lightPanel ? new Color(0.7f, 0.1f, 0f) : Color.white;
        m_CloseButtonStyle.normal.background  = MakeTex(new Color(0f, 0f, 0f, 0f));
        m_CloseButtonStyle.hover.background   = MakeTex(new Color(0f, 0f, 0f, 0.06f));
        m_CloseButtonStyle.active.background  = MakeTex(new Color(0f, 0f, 0f, 0.06f));
        m_CloseButtonStyle.border = new RectOffset(0, 0, 0, 0);

        m_KeyButtonStyle = new GUIStyle(GUI.skin.button);
        m_KeyButtonStyle.alignment = TextAnchor.MiddleCenter;
        m_KeyButtonStyle.fontSize  = 12;
        m_KeyButtonStyle.fontStyle = FontStyle.Bold;

        m_DisplayStyle = new GUIStyle(GUI.skin.label);
        m_DisplayStyle.alignment = TextAnchor.MiddleRight;
        m_DisplayStyle.fontSize  = 24;
        m_DisplayStyle.fontStyle = FontStyle.Bold;
        m_DisplayStyle.normal.textColor = new Color(1f, 0.08f, 0.02f, 1f);
        var displayFont = Resources.Load<Font>("digital-7 (mono)");
#if UNITY_EDITOR
        if (displayFont == null)
            displayFont = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>(KDisplayFontAssetPath);
#endif
        if (displayFont != null) m_DisplayStyle.font = displayFont;
    }
}
