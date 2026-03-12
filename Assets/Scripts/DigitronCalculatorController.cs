using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DigitronCalculatorController : MonoBehaviour
{
    private enum DigitronState
    {
        Unplaced,
        PlacedClosed,
        Opening,
        Opened,
        Closing,
    }

    private struct HotspotData
    {
        public string Id;
        public int Number;
        public string Title;
        public string Description;
        public Vector3 NormalizedViewportAnchor;
    }

    private const string KOpenClipName = "CalculatorOpen";
    private const string KFallbackClipName = "Take 001";
    private const float KTakeFrameRate = 25f;
    private const float KClosedPoseFrame = 1f;
    private const float KOpenStartFrame = 100f;
    private const float KOpenEndFrame = 150f;
    private const float KFallbackOpenDuration = 1.1f;
    private const float KOpenButtonWidth = 140f;
    private const float KOpenButtonHeight = 44f;
    private const float KHotspotScale = 0.04f;
    private const float KHotspotOffsetFactor = 0.14f;
    private const float KInfoBoxWidth = 560f;
    private const float KInfoBoxHeight = 170f;
    private const float KBottomUiMargin = 28f;

    private static readonly HotspotData[] Hotspots =
    {
        new HotspotData
        {
            Id = "housing",
            Number = 1,
            Title = "Kućište",
            Description = "Kućište štiti osjetljivu elektroniku i drži sve dijelove na mjestu. Istovremeno daje uređaju prepoznatljiv oblik i dizajn.",
            NormalizedViewportAnchor = new Vector3(0.5f, 0.55f, 0.1f),
        },
        new HotspotData
        {
            Id = "keyboard",
            Number = 2,
            Title = "Tipkovnica",
            Description = "Tipkovnica služi za unos brojeva i matematičkih operacija. Svaki pritisak tipke šalje električni signal koji kalkulator pretvara u izračun.",
            NormalizedViewportAnchor = new Vector3(0.5f, 0.3f, 0.23f),
        },
        new HotspotData
        {
            Id = "board",
            Number = 3,
            Title = "Elektronička ploča",
            Description = "Tiskana elektronička ploča povezuje sve dijelove kalkulatora. Kroz njezine vodove putuju električni signali između tipkovnice, procesora i zaslona.",
            NormalizedViewportAnchor = new Vector3(0.52f, 0.46f, -0.08f),
        },
        new HotspotData
        {
            Id = "chips",
            Number = 4,
            Title = "Integrirani krugovi",
            Description = "Mali crni čipovi predstavljaju mozak kalkulatora. U njima se odvijaju sve matematičke operacije koje omogućuju brzo računanje.",
            NormalizedViewportAnchor = new Vector3(0.67f, 0.46f, -0.02f),
        },
        new HotspotData
        {
            Id = "batteries",
            Number = 5,
            Title = "Baterije",
            Description = "Baterije napajaju kalkulator električnom energijom. Zahvaljujući njima uređaj je mogao biti mali i prenosiv, pravi džepni kalkulator.",
            NormalizedViewportAnchor = new Vector3(0.78f, 0.3f, -0.15f),
        },
        new HotspotData
        {
            Id = "display",
            Number = 6,
            Title = "Zaslon",
            Description = "Crveni LED zaslon prikazuje rezultate računanja. Svaka znamenka sastavljena je od sedam svjetlećih segmenata koji zajedno tvore brojke.",
            NormalizedViewportAnchor = new Vector3(0.5f, 0.77f, 0.02f),
        },
    };

    private GameObject m_ModelInstance;
    private Camera m_TargetCamera;
    private Bounds m_ModelBounds;
    private Bounds m_ModelLocalBounds;
    private DigitronState m_State = DigitronState.Unplaced;
    private Animation m_LegacyAnimation;
    private AnimationState m_OpenAnimationState;
    private AnimationClip m_OpenAnimationClip;
    private AnimationClip m_ClosedPoseClip;
    private float m_ClosedPoseTime;
    private float m_OpenStartTime;
    private float m_OpenEndTime;
    private string m_SelectedHotspotId;
    private GUIStyle m_OpenButtonStyle;
    private GUIStyle m_InfoBoxStyle;
    private GUIStyle m_InfoTitleStyle;
    private Coroutine m_FallbackRoutine;
    private readonly Dictionary<string, DigitronHotspotMarker> m_HotspotMarkers = new Dictionary<string, DigitronHotspotMarker>();

    public void Initialize(GameObject modelInstance, Camera targetCamera, float targetSize)
    {
        m_ModelInstance = modelInstance;
        m_TargetCamera = targetCamera ? targetCamera : Camera.main;

        NormalizeModelScale(targetSize);
        RecalculateBounds();
        m_LegacyAnimation = m_ModelInstance.GetComponentInChildren<Animation>(true);
        TryPrepareAnimation();
        ApplyClosedPose();
        RebuildHotspotMarkers();
        SetHotspotsVisible(false);

        m_State = DigitronState.PlacedClosed;
        m_ModelInstance.SetActive(true);
    }

    public void ResetDigitronState()
    {
        if (!m_ModelInstance)
        {
            return;
        }

        if (m_FallbackRoutine != null)
        {
            StopCoroutine(m_FallbackRoutine);
            m_FallbackRoutine = null;
        }

        ApplyClosedPose();
        SetHotspotsVisible(false);
        m_SelectedHotspotId = null;
        m_State = DigitronState.PlacedClosed;
        m_ModelInstance.SetActive(true);
    }

    public void HandlePlaced(Camera targetCamera)
    {
        m_TargetCamera = targetCamera ? targetCamera : Camera.main;
        RecalculateBounds();
        RebuildHotspotMarkers();
        SetHotspotsVisible(m_State == DigitronState.Opened);
        m_SelectedHotspotId = null;
    }

    public void SelectHotspot(string hotspotId)
    {
        if (m_State != DigitronState.Opened)
        {
            return;
        }

        m_SelectedHotspotId = hotspotId;
    }

    private void Update()
    {
        if (!m_TargetCamera)
        {
            m_TargetCamera = Camera.main;
        }

    }

    private void OnGUI()
    {
        if (!m_ModelInstance || !m_TargetCamera || m_State == DigitronState.Unplaced)
        {
            return;
        }

        EnsureGuiStyles();

        if (m_State == DigitronState.PlacedClosed || m_State == DigitronState.Opened)
        {
            DrawToggleButton();
        }

        DrawInfoBox();
    }

    private void DrawToggleButton()
    {
        var rect = new Rect(
            (Screen.width - KOpenButtonWidth) * 0.5f,
            Screen.height - KOpenButtonHeight - KBottomUiMargin,
            KOpenButtonWidth,
            KOpenButtonHeight);

        var buttonLabel = m_State == DigitronState.Opened ? "Zatvori" : "Otvori";
        if (GUI.Button(rect, buttonLabel, m_OpenButtonStyle))
        {
            ToggleOpenState();
        }
    }

    private void DrawInfoBox()
    {
        if (m_State != DigitronState.Opened || string.IsNullOrEmpty(m_SelectedHotspotId))
        {
            return;
        }

        var hotspot = Hotspots.FirstOrDefault(entry => entry.Id == m_SelectedHotspotId);
        if (string.IsNullOrEmpty(hotspot.Id))
        {
            return;
        }

        var width = Mathf.Min(KInfoBoxWidth, Screen.width - 32f);
        var rect = new Rect(
            (Screen.width - width) * 0.5f,
            Screen.height - KInfoBoxHeight - KOpenButtonHeight - (KBottomUiMargin * 2f),
            width,
            KInfoBoxHeight);

        GUILayout.BeginArea(rect, m_InfoBoxStyle);
        GUILayout.Label(hotspot.Title, m_InfoTitleStyle);
        GUILayout.Space(8f);
        GUILayout.Label(hotspot.Description, GUI.skin.label);
        GUILayout.Space(10f);
        if (GUILayout.Button("Zatvori opis"))
        {
            m_SelectedHotspotId = null;
        }
        GUILayout.EndArea();
    }

    private void ToggleOpenState()
    {
        if (m_State == DigitronState.PlacedClosed)
        {
            StartOpenSequence();
            return;
        }

        if (m_State == DigitronState.Opened)
        {
            StartCloseSequence();
        }
    }

    private void StartOpenSequence()
    {
        if (m_State != DigitronState.PlacedClosed)
        {
            return;
        }

        if (m_FallbackRoutine != null)
        {
            StopCoroutine(m_FallbackRoutine);
            m_FallbackRoutine = null;
        }

        m_SelectedHotspotId = null;
        m_State = DigitronState.Opening;

        if (m_OpenAnimationClip != null)
        {
            m_FallbackRoutine = StartCoroutine(PlayOpenClipRoutine(0f, 1f, DigitronState.Opened));
            return;
        }

        m_FallbackRoutine = StartCoroutine(FallbackOpenRoutine());
    }

    private void StartCloseSequence()
    {
        if (m_State != DigitronState.Opened)
        {
            return;
        }

        if (m_FallbackRoutine != null)
        {
            StopCoroutine(m_FallbackRoutine);
            m_FallbackRoutine = null;
        }

        m_SelectedHotspotId = null;
        m_State = DigitronState.Closing;

        if (m_OpenAnimationClip != null)
        {
            m_FallbackRoutine = StartCoroutine(PlayOpenClipRoutine(1f, 0f, DigitronState.PlacedClosed));
            return;
        }

        m_FallbackRoutine = StartCoroutine(FallbackCloseRoutine());
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
        SetHotspotsVisible(false);
        m_SelectedHotspotId = null;
        m_State = DigitronState.PlacedClosed;
        m_FallbackRoutine = null;
    }

    private IEnumerator PlayOpenClipRoutine(float startNormalizedTime, float endNormalizedTime, DigitronState completedState)
    {
        var animationDuration = Mathf.Max(Mathf.Abs(m_OpenEndTime - m_OpenStartTime), 0.01f);
        var elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            var progress = Mathf.Clamp01(elapsed / animationDuration);
            var normalizedTime = Mathf.Lerp(startNormalizedTime, endNormalizedTime, progress);
            SampleOpenClip(normalizedTime);
            yield return null;
        }

        SampleOpenClip(endNormalizedTime);

        RecalculateBounds();
        RebuildHotspotMarkers();
        var isOpened = completedState == DigitronState.Opened;
        SetHotspotsVisible(isOpened);
        if (!isOpened)
        {
            m_SelectedHotspotId = null;
        }

        m_State = completedState;
        m_FallbackRoutine = null;
    }

    private void TryPrepareAnimation()
    {
        if (m_LegacyAnimation == null)
        {
            Debug.LogWarning("Digitron open animation was not found on the instantiated model. Falling back to state-only open/close.");
            return;
        }

        m_LegacyAnimation.playAutomatically = false;

        if (m_LegacyAnimation.GetClip(KFallbackClipName) != null)
        {
            m_ClosedPoseClip = m_LegacyAnimation.GetClip(KFallbackClipName);
            m_ClosedPoseTime = Mathf.Min(KClosedPoseFrame / KTakeFrameRate, m_ClosedPoseClip.length);
        }

        if (m_LegacyAnimation.GetClip(KOpenClipName) != null)
        {
            m_OpenAnimationState = m_LegacyAnimation[KOpenClipName];
            m_OpenAnimationClip = m_LegacyAnimation.GetClip(KOpenClipName);
            m_OpenStartTime = 0f;
            m_OpenEndTime = m_OpenAnimationClip.length;
            return;
        }

        if (m_LegacyAnimation.GetClip(KFallbackClipName) != null)
        {
            m_OpenAnimationState = m_LegacyAnimation[KFallbackClipName];
            m_OpenAnimationClip = m_LegacyAnimation.GetClip(KFallbackClipName);
            m_OpenStartTime = KOpenStartFrame / KTakeFrameRate;
            m_OpenEndTime = Mathf.Min(KOpenEndFrame / KTakeFrameRate, m_OpenAnimationClip.length);
            return;
        }

        Debug.LogWarning("Digitron model has a legacy Animation component, but no open clip named 'CalculatorOpen' or 'Take 001'.");
    }

    private void ApplyClosedPose()
    {
        if (m_ClosedPoseClip == null && m_OpenAnimationClip == null)
        {
            return;
        }

        var closedPoseClip = m_ClosedPoseClip != null ? m_ClosedPoseClip : m_OpenAnimationClip;
        SampleClipAtTime(closedPoseClip, m_ClosedPoseTime);
        RecalculateBounds();
    }

    private void SampleOpenClip(float normalizedTime)
    {
        if (m_OpenAnimationClip == null)
        {
            return;
        }

        var sampleTime = Mathf.Lerp(m_OpenStartTime, m_OpenEndTime, Mathf.Clamp01(normalizedTime));
        SampleClipAtTime(m_OpenAnimationClip, sampleTime);
    }

    private void SampleClipAtTime(AnimationClip clip, float sampleTime)
    {
        if (clip == null || m_LegacyAnimation == null)
        {
            return;
        }

        clip.SampleAnimation(m_LegacyAnimation.gameObject, Mathf.Clamp(sampleTime, 0f, clip.length));
    }

    private void NormalizeModelScale(float targetSize)
    {
        var bounds = CalculateBounds();
        var maxDimension = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        if (maxDimension <= Mathf.Epsilon)
        {
            return;
        }

        var scaleFactor = targetSize / maxDimension;
        m_ModelInstance.transform.localScale = Vector3.one * scaleFactor;
    }

    private Bounds CalculateBounds()
    {
        var renderers = m_ModelInstance.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return new Bounds(m_ModelInstance.transform.position, Vector3.one * 0.25f);
        }

        var bounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }

    private void RecalculateBounds()
    {
        m_ModelBounds = CalculateBounds();
        m_ModelLocalBounds = CalculateLocalBounds();
    }

    private Bounds CalculateLocalBounds()
    {
        var renderers = m_ModelInstance.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return new Bounds(Vector3.zero, Vector3.one * 0.25f);
        }

        var initialized = false;
        var localBounds = new Bounds();
        foreach (var renderer in renderers)
        {
            var worldBounds = renderer.bounds;
            var min = worldBounds.min;
            var max = worldBounds.max;
            var corners = new[]
            {
                new Vector3(min.x, min.y, min.z),
                new Vector3(min.x, min.y, max.z),
                new Vector3(min.x, max.y, min.z),
                new Vector3(min.x, max.y, max.z),
                new Vector3(max.x, min.y, min.z),
                new Vector3(max.x, min.y, max.z),
                new Vector3(max.x, max.y, min.z),
                new Vector3(max.x, max.y, max.z),
            };

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
        }

        return localBounds;
    }

    private Vector3 GetWorldPointForNormalizedAnchor(Vector3 anchor)
    {
        var min = m_ModelLocalBounds.min;
        var size = m_ModelLocalBounds.size;
        var localPoint = new Vector3(
            min.x + (size.x * anchor.x),
            min.y + (size.y * anchor.y),
            min.z + (size.z * anchor.z));

        return m_ModelInstance.transform.TransformPoint(localPoint);
    }

    private void RebuildHotspotMarkers()
    {
        foreach (var marker in m_HotspotMarkers.Values)
        {
            if (marker)
            {
                Destroy(marker.gameObject);
            }
        }

        m_HotspotMarkers.Clear();

        foreach (var hotspot in Hotspots)
        {
            var markerObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            markerObject.name = hotspot.Id;
            markerObject.transform.SetParent(transform, true);
            markerObject.transform.localScale = Vector3.one * KHotspotScale;

            var anchorPoint = GetWorldPointForNormalizedAnchor(hotspot.NormalizedViewportAnchor);
            var localCenterWorld = m_ModelInstance.transform.TransformPoint(m_ModelLocalBounds.center);
            var directionFromCenter = anchorPoint - localCenterWorld;
            if (directionFromCenter.sqrMagnitude < 0.0001f)
            {
                directionFromCenter = m_ModelInstance.transform.up;
            }

            var markerPosition = anchorPoint + directionFromCenter.normalized * (m_ModelBounds.extents.magnitude * KHotspotOffsetFactor);
            markerObject.transform.position = markerPosition;

            var renderer = markerObject.GetComponent<Renderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.material = CreateHotspotMaterial();

            var collider = markerObject.GetComponent<Collider>();
            collider.isTrigger = false;

            var marker = markerObject.AddComponent<DigitronHotspotMarker>();
            marker.Initialize(this, hotspot.Id, hotspot.Number, m_TargetCamera);
            m_HotspotMarkers.Add(hotspot.Id, marker);
        }
    }

    private void SetHotspotsVisible(bool isVisible)
    {
        foreach (var marker in m_HotspotMarkers.Values)
        {
            if (marker)
            {
                marker.gameObject.SetActive(isVisible);
            }
        }
    }

    private static Material CreateHotspotMaterial()
    {
        var material = new Material(Shader.Find("Unlit/Color"));
        material.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
        return material;
    }

    private void EnsureGuiStyles()
    {
        if (m_OpenButtonStyle != null)
        {
            return;
        }

        m_OpenButtonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
        };

        m_InfoBoxStyle = new GUIStyle(GUI.skin.box)
        {
            padding = new RectOffset(16, 16, 16, 16),
            fontSize = 16,
            alignment = TextAnchor.UpperLeft,
        };

        m_InfoTitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            wordWrap = true,
        };
    }
}
