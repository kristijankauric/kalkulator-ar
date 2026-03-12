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
    private const string KStandaloneOpenClipAssetPath = "Assets/Models/DIGITRON stara animacija/NOVI-OBJEKT/CalculatorOpen.anim";
    private const float KTakeFrameRate = 25f;
    private const float KClosedPoseFrame = 1f;
    private const float KOpenStartFrame = 100f;
    private const float KOpenEndFrame = 150f;
    private const float KFallbackOpenDuration = 1.1f;
    private const float KManualOpenAngle = -110f;
    private const float KPhysicalKeyPressDepth = 0.004f;
    private const float KPhysicalKeyPressDuration = 0.08f;
    private const float KOpenButtonWidth = 140f;
    private const float KOpenButtonHeight = 44f;
    private const float KHotspotScale = 0.04f;
    private const float KHotspotOffsetFactor = 0.14f;
    private const float KInfoBoxWidth = 560f;
    private const float KInfoBoxHeight = 170f;
    private const float KBottomUiMargin = 28f;
    private const float KFrontSurfaceDepth = 0.235f;
    private const float KFrontSurfaceOffset = 0.02f;
    private const float KLampScale = 0.028f;
    private const float KMinimumProjectedKeySize = 10f;
    private const string KDisplayFontAssetPath = "Assets/Models/digital-7 (mono).ttf";
    private const float KKeypadMinX = 0.205f;
    private const float KKeypadMaxX = 0.892f;
    private const float KKeypadMinY = 0.156f;
    private const float KKeypadMaxY = 0.526f;

    private static readonly HotspotData[] Hotspots =
    {
        new HotspotData { Id = "housing", Number = 1, Title = "Kuciste", Description = "Kuciste stiti osjetljivu elektroniku i drzi sve dijelove na mjestu.", NormalizedViewportAnchor = new Vector3(0.5f, 0.55f, 0.1f) },
        new HotspotData { Id = "keyboard", Number = 2, Title = "Tipkovnica", Description = "Tipkovnica sluzi za unos brojeva i matematickih operacija.", NormalizedViewportAnchor = new Vector3(0.5f, 0.3f, 0.23f) },
        new HotspotData { Id = "board", Number = 3, Title = "Elektronicka ploca", Description = "Elektronicka ploca povezuje sve dijelove kalkulatora.", NormalizedViewportAnchor = new Vector3(0.52f, 0.46f, -0.08f) },
        new HotspotData { Id = "chips", Number = 4, Title = "Integrirani krugovi", Description = "Cipovi predstavljaju mozak kalkulatora.", NormalizedViewportAnchor = new Vector3(0.67f, 0.46f, -0.02f) },
        new HotspotData { Id = "batteries", Number = 5, Title = "Baterije", Description = "Baterije napajaju kalkulator elektricnom energijom.", NormalizedViewportAnchor = new Vector3(0.78f, 0.3f, -0.15f) },
        new HotspotData { Id = "display", Number = 6, Title = "Zaslon", Description = "Crveni LED zaslon prikazuje rezultate racunanja.", NormalizedViewportAnchor = new Vector3(0.5f, 0.77f, 0.02f) },
    };

    private static readonly DisplayLayoutData DisplayLayout = new DisplayLayoutData
    {
        NormalizedAnchor = new Vector3(0.635f, 0.783f, KFrontSurfaceDepth),
        NormalizedSize = new Vector2(0.26f, 0.065f),
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
        new KeyLayoutData { KeyId = DigitronKeyId.K, Name = "K", NormalizedAnchor = KeypadAnchor(0.08f, 0.92f), NormalizedSize = KeypadSize(0.11f, 0.10f) },
        new KeyLayoutData { KeyId = DigitronKeyId.F, Name = "F", NormalizedAnchor = KeypadAnchor(0.34f, 0.92f), NormalizedSize = KeypadSize(0.11f, 0.10f) },
        new KeyLayoutData { KeyId = DigitronKeyId.ClearEntry, Name = "CE", NormalizedAnchor = KeypadAnchor(0.60f, 0.92f), NormalizedSize = KeypadSize(0.11f, 0.10f) },
        new KeyLayoutData { KeyId = DigitronKeyId.ClearAll, Name = "C", NormalizedAnchor = KeypadAnchor(0.79f, 0.92f), NormalizedSize = KeypadSize(0.12f, 0.10f) },
        new KeyLayoutData { KeyId = DigitronKeyId.Seven, Name = "7", NormalizedAnchor = KeypadAnchor(0.08f, 0.68f), NormalizedSize = KeypadSize(0.11f, 0.13f) },
        new KeyLayoutData { KeyId = DigitronKeyId.Eight, Name = "8", NormalizedAnchor = KeypadAnchor(0.34f, 0.68f), NormalizedSize = KeypadSize(0.11f, 0.13f) },
        new KeyLayoutData { KeyId = DigitronKeyId.Nine, Name = "9", NormalizedAnchor = KeypadAnchor(0.60f, 0.68f), NormalizedSize = KeypadSize(0.11f, 0.13f) },
        new KeyLayoutData { KeyId = DigitronKeyId.MinusOrEquals, Name = "-=", NormalizedAnchor = KeypadAnchor(0.96f, 0.68f), NormalizedSize = KeypadSize(0.09f, 0.13f) },
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

    private readonly Dictionary<string, DigitronHotspotMarker> m_HotspotMarkers = new Dictionary<string, DigitronHotspotMarker>();
    private readonly List<DigitronKeyHitTarget> m_KeyTargets = new List<DigitronKeyHitTarget>();
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
    private GUIStyle m_OpenButtonStyle;
    private GUIStyle m_InfoBoxStyle;
    private GUIStyle m_InfoTitleStyle;
    private GUIStyle m_KeyButtonStyle;
    private GUIStyle m_DisplayStyle;
    private Renderer m_LampRenderer;
    private Material m_LampMaterial;
    private Coroutine m_FallbackRoutine;
    private Transform m_FrontCoverTransform;
    private Quaternion m_FrontCoverClosedRotation;
    private Quaternion m_FrontCoverOpenRotation;

    public void Initialize(GameObject modelInstance, Camera targetCamera, float targetSize)
    {
        m_ModelInstance = modelInstance;
        m_TargetCamera = targetCamera ? targetCamera : Camera.main;
        m_RelevantRenderers.Clear();
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
        EnsureLampVisual();
        RebuildKeyTargets();
        RebuildHotspotMarkers();
        ResetCalculatorRuntime();
        SetHotspotsVisible(false);
        SetCalculatorInteractionVisible(true);
        m_State = DigitronState.PlacedClosed;
        m_ModelInstance.SetActive(true);
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
        EnsureLampVisual();
        RebuildKeyTargets();
        RebuildHotspotMarkers();
        RefreshCalculatorPresentation();
        SetHotspotsVisible(m_State == DigitronState.Opened);
        SetCalculatorInteractionVisible(m_State == DigitronState.PlacedClosed);
        m_SelectedHotspotId = null;
    }

    public void SelectHotspot(string hotspotId)
    {
        if (m_State == DigitronState.Opened) m_SelectedHotspotId = hotspotId;
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

    private void Update()
    {
        if (!m_TargetCamera) m_TargetCamera = Camera.main;
        UpdateLampFacingCamera();
        HandleCalculatorPointerInput();
    }

    private void OnGUI()
    {
        if (!m_ModelInstance || !m_TargetCamera || m_State == DigitronState.Unplaced) return;
        EnsureGuiStyles();
        if (m_State == DigitronState.PlacedClosed || m_State == DigitronState.Opened) DrawToggleButton();
        if (m_State == DigitronState.PlacedClosed)
        {
            DrawProjectedDisplay();
            if (!m_UsePhysicalKeyTargets) DrawProjectedCalculatorPad();
        }
        DrawInfoBox();
    }

    private void DrawToggleButton()
    {
        var rect = new Rect((Screen.width - KOpenButtonWidth) * 0.5f, Screen.height - KOpenButtonHeight - KBottomUiMargin, KOpenButtonWidth, KOpenButtonHeight);
        if (GUI.Button(rect, m_State == DigitronState.Opened ? "Zatvori" : "Otvori", m_OpenButtonStyle)) ToggleOpenState();
    }

    private void DrawInfoBox()
    {
        if (m_State != DigitronState.Opened || string.IsNullOrEmpty(m_SelectedHotspotId)) return;
        var hotspot = Hotspots.FirstOrDefault(entry => entry.Id == m_SelectedHotspotId);
        if (string.IsNullOrEmpty(hotspot.Id)) return;
        var width = Mathf.Min(KInfoBoxWidth, Screen.width - 32f);
        var rect = new Rect((Screen.width - width) * 0.5f, Screen.height - KInfoBoxHeight - KOpenButtonHeight - (KBottomUiMargin * 2f), width, KInfoBoxHeight);
        GUILayout.BeginArea(rect, m_InfoBoxStyle);
        GUILayout.Label(hotspot.Title, m_InfoTitleStyle);
        GUILayout.Space(8f);
        GUILayout.Label(hotspot.Description, GUI.skin.label);
        GUILayout.Space(10f);
        if (GUILayout.Button("Zatvori opis")) m_SelectedHotspotId = null;
        GUILayout.EndArea();
    }

    private void DrawProjectedDisplay()
    {
        if (!m_Runtime.IsPoweredOn || string.IsNullOrEmpty(m_Runtime.DisplayText)) return;
        if (!TryGetProjectedRect(DisplayLayout.NormalizedAnchor, DisplayLayout.NormalizedSize, out var rect)) return;
        GUI.Label(rect, m_Runtime.DisplayText, m_DisplayStyle);
    }

    private void DrawProjectedCalculatorPad()
    {
        var previousColor = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, 0.42f);
        foreach (var keyLayout in KeyLayouts)
        {
            if (!TryGetProjectedRect(keyLayout.NormalizedAnchor, keyLayout.NormalizedSize, out var rect)) continue;
            if (GUI.Button(rect, keyLayout.Name, m_KeyButtonStyle)) HandleKeyPress(keyLayout.KeyId);
        }
        GUI.color = previousColor;
    }

    private bool TryGetProjectedRect(Vector3 normalizedAnchor, Vector2 normalizedSize, out Rect rect)
    {
        var anchorLocal = GetLocalPointForNormalizedAnchor(normalizedAnchor);
        var halfSizeLocal = new Vector3(m_ModelLocalBounds.size.x * normalizedSize.x * 0.5f, m_ModelLocalBounds.size.y * normalizedSize.y * 0.5f, 0f);
        var topLeftWorld = m_ModelInstance.transform.TransformPoint(anchorLocal + new Vector3(-halfSizeLocal.x, halfSizeLocal.y, 0f));
        var bottomRightWorld = m_ModelInstance.transform.TransformPoint(anchorLocal + new Vector3(halfSizeLocal.x, -halfSizeLocal.y, 0f));
        var topLeftScreen = m_TargetCamera.WorldToScreenPoint(topLeftWorld);
        var bottomRightScreen = m_TargetCamera.WorldToScreenPoint(bottomRightWorld);
        if (topLeftScreen.z <= 0f || bottomRightScreen.z <= 0f)
        {
            rect = default;
            return false;
        }

        rect = Rect.MinMaxRect(
            Mathf.Min(topLeftScreen.x, bottomRightScreen.x),
            Screen.height - Mathf.Max(topLeftScreen.y, bottomRightScreen.y),
            Mathf.Max(topLeftScreen.x, bottomRightScreen.x),
            Screen.height - Mathf.Min(topLeftScreen.y, bottomRightScreen.y));
        return rect.width > KMinimumProjectedKeySize && rect.height > KMinimumProjectedKeySize;
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
        if (m_OpenAnimationClip != null) m_FallbackRoutine = StartCoroutine(PlayOpenClipRoutine(0f, 1f, DigitronState.Opened));
        else if (m_FrontCoverTransform != null) m_FallbackRoutine = StartCoroutine(PlayManualCoverRoutine(0f, 1f, DigitronState.Opened));
        else m_FallbackRoutine = StartCoroutine(FallbackOpenRoutine());
    }

    private void StartCloseSequence()
    {
        if (m_State != DigitronState.Opened) return;
        StopCurrentRoutine();
        m_SelectedHotspotId = null;
        SetHotspotsVisible(false);
        m_State = DigitronState.Closing;
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
    }

    private void TryPrepareAnimation()
    {
        m_OpenAnimationClip = null;
        m_ClosedPoseClip = null;
        m_ClosedPoseTime = 0f;
        m_OpenStartTime = 0f;
        m_OpenEndTime = 0f;

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
                m_OpenStartTime = 0f;
                m_OpenEndTime = m_OpenAnimationClip.length;
                return;
            }

            if (m_LegacyAnimation.GetClip(KFallbackClipName) != null)
            {
                m_OpenAnimationClip = m_LegacyAnimation.GetClip(KFallbackClipName);
                m_OpenStartTime = KOpenStartFrame / KTakeFrameRate;
                m_OpenEndTime = Mathf.Min(KOpenEndFrame / KTakeFrameRate, m_OpenAnimationClip.length);
                return;
            }
        }

        if (m_Animator != null)
        {
            m_Animator.runtimeAnimatorController = null;
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
        }
#endif
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
        if (m_ClosedPoseClip != null || m_OpenAnimationClip != null)
        {
            SampleClipAtTime(m_ClosedPoseClip != null ? m_ClosedPoseClip : m_OpenAnimationClip, m_ClosedPoseTime);
        }
        if (m_FrontCoverTransform != null) m_FrontCoverTransform.localRotation = m_FrontCoverClosedRotation;
        RecalculateBounds();
    }

    private void SampleOpenClip(float normalizedTime)
    {
        if (m_OpenAnimationClip == null) return;
        var sampleTime = Mathf.Lerp(m_OpenStartTime, m_OpenEndTime, Mathf.Clamp01(normalizedTime));
        m_OpenAnimationClip.SampleAnimation(GetAnimationSampleTarget(), Mathf.Clamp(sampleTime, 0f, m_OpenAnimationClip.length));
    }

    private void SampleClipAtTime(AnimationClip clip, float sampleTime)
    {
        if (clip == null) return;
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
            m_LampMaterial = new Material(Shader.Find("Unlit/Color"));
            m_LampRenderer.material = m_LampMaterial;
            var collider = lampObject.GetComponent<Collider>();
            if (collider) Destroy(collider);
        }

        m_LampRenderer.transform.position = GetWorldPointForNormalizedAnchor(new Vector3(0.815f, 0.63f, KFrontSurfaceDepth)) + (GetFrontOffsetDirection() * 0.007f);
    }

    private void UpdateLampFacingCamera()
    {
        if (!m_LampRenderer || !m_TargetCamera) return;
        m_LampRenderer.transform.LookAt(m_TargetCamera.transform.position, Vector3.up);
        m_LampRenderer.transform.Rotate(0f, 180f, 0f);
    }

    private void ResetCalculatorRuntime()
    {
        m_Runtime.ResetPoweredOff();
        RefreshCalculatorPresentation();
    }

    private void RefreshCalculatorPresentation()
    {
        if (m_LampMaterial != null)
        {
            m_LampMaterial.color = m_Runtime.IsPoweredOn ? new Color(1f, 0.14f, 0.14f, 1f) : new Color(0.2f, 0.05f, 0.05f, 1f);
        }
    }

    private void RebuildKeyTargets()
    {
        foreach (var keyTarget in m_KeyTargets) if (keyTarget) Destroy(keyTarget.gameObject);
        m_KeyTargets.Clear();
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
        if (candidates.Count < 10) return false;
        var used = new HashSet<Renderer>();
        foreach (var keyLayout in KeyLayouts)
        {
            var expectedLocal = GetLocalPointForNormalizedAnchor(keyLayout.NormalizedAnchor);
            Renderer best = null;
            var bestScore = float.MaxValue;
            foreach (var candidate in candidates)
            {
                if (used.Contains(candidate)) continue;
                var center = CalculateRendererLocalBounds(candidate).center;
                var dx = Mathf.Abs(center.x - expectedLocal.x) / Mathf.Max(m_ModelLocalBounds.size.x, 0.001f);
                var dy = Mathf.Abs(center.y - expectedLocal.y) / Mathf.Max(m_ModelLocalBounds.size.y, 0.001f);
                var score = dx + (dy * 1.35f);
                if (score < bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            if (best == null || bestScore > 0.18f) continue;
            used.Add(best);
            CreatePhysicalKeyTarget(keyLayout, best);
        }

        m_UsePhysicalKeyTargets = m_KeyTargets.Count >= 10;
        return m_UsePhysicalKeyTargets;
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
        keyObject.transform.rotation = m_ModelInstance.transform.rotation;
        var collider = keyObject.AddComponent<BoxCollider>();
        collider.size = new Vector3(localBounds.size.x * 1.08f, localBounds.size.y * 1.08f, Mathf.Max(localBounds.size.z, m_ModelBounds.size.z * 0.03f));
        var pressOffset = renderer.transform.InverseTransformDirection(-m_ModelInstance.transform.forward) * KPhysicalKeyPressDepth;
        var keyTarget = keyObject.AddComponent<DigitronKeyHitTarget>();
        keyTarget.Initialize(this, keyLayout.KeyId, renderer.transform, pressOffset);
        m_KeyTargets.Add(keyTarget);
    }

    private void SetCalculatorInteractionVisible(bool isVisible)
    {
        foreach (var keyTarget in m_KeyTargets) if (keyTarget) keyTarget.gameObject.SetActive(isVisible);
        if (m_LampRenderer) m_LampRenderer.gameObject.SetActive(isVisible);
    }

    private void HandleCalculatorPointerInput()
    {
        if (!m_UsePhysicalKeyTargets || m_State != DigitronState.PlacedClosed || !m_TargetCamera) return;
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
            HandleKeyPress(keyTarget.KeyId);
            if (keyTarget.PressTarget != null) StartCoroutine(AnimatePhysicalKeyPress(keyTarget));
            return;
        }
    }

    private IEnumerator AnimatePhysicalKeyPress(DigitronKeyHitTarget keyTarget)
    {
        var target = keyTarget.PressTarget;
        if (target == null) yield break;
        var rest = target.localPosition;
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
    }

    private void RebuildHotspotMarkers()
    {
        foreach (var marker in m_HotspotMarkers.Values) if (marker) Destroy(marker.gameObject);
        m_HotspotMarkers.Clear();
        foreach (var hotspot in Hotspots)
        {
            var markerObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            markerObject.name = hotspot.Id;
            markerObject.transform.SetParent(transform, true);
            markerObject.transform.localScale = Vector3.one * KHotspotScale;
            var anchorPoint = GetWorldPointForNormalizedAnchor(hotspot.NormalizedViewportAnchor);
            var localCenterWorld = m_ModelInstance.transform.TransformPoint(m_ModelLocalBounds.center);
            var direction = anchorPoint - localCenterWorld;
            if (direction.sqrMagnitude < 0.0001f) direction = m_ModelInstance.transform.up;
            markerObject.transform.position = anchorPoint + direction.normalized * (m_ModelBounds.extents.magnitude * KHotspotOffsetFactor);
            var renderer = markerObject.GetComponent<Renderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.material = CreateHotspotMaterial();
            var marker = markerObject.AddComponent<DigitronHotspotMarker>();
            marker.Initialize(this, hotspot.Id, hotspot.Number, m_TargetCamera);
            m_HotspotMarkers.Add(hotspot.Id, marker);
        }
    }

    private void SetHotspotsVisible(bool isVisible)
    {
        foreach (var marker in m_HotspotMarkers.Values) if (marker) marker.gameObject.SetActive(isVisible);
    }

    private static Material CreateHotspotMaterial()
    {
        var material = new Material(Shader.Find("Unlit/Color"));
        material.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
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

    private void EnsureGuiStyles()
    {
        if (m_OpenButtonStyle != null) return;
        m_OpenButtonStyle = new GUIStyle(GUI.skin.button) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        m_InfoBoxStyle = new GUIStyle(GUI.skin.box) { padding = new RectOffset(16, 16, 16, 16), fontSize = 16, alignment = TextAnchor.UpperLeft };
        m_InfoTitleStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, wordWrap = true };
        m_KeyButtonStyle = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleCenter, fontSize = 12, fontStyle = FontStyle.Bold };
        m_DisplayStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleRight, fontSize = 24, fontStyle = FontStyle.Bold };
        m_DisplayStyle.normal.textColor = new Color(1f, 0.88f, 0.80f, 1f);
#if UNITY_EDITOR
        var font = AssetDatabase.LoadAssetAtPath<Font>(KDisplayFontAssetPath);
        if (font != null) m_DisplayStyle.font = font;
#endif
    }
}
