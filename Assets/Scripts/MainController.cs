using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Imagine.WebAR;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainController : MonoBehaviour
{
    [SerializeField] private int m_TargetFrameRate = 30;
    [SerializeField] private GameObject m_DigitronPrefab;
    private bool m_EnableDigitronMode = true;
    private float m_TargetDigitronSize = 0.3f;
    private float m_WebTargetScaleMultiplier = 4.0f;
    private bool m_EnableEditorInstantPreview = true;
    private Vector3 m_EditorPreviewLocalPosition = new Vector3(0f, 0.1f, 1.25f);
    private Vector3 m_EditorPreviewLocalEulerAngles = new Vector3(0f, 180f, 0f);
    private Vector3 m_WebSpawnLocalEulerAngles = new Vector3(180f, 0f, 0f);
    private Vector3 m_EditorCameraOffset = new Vector3(-0.08f, 0.12f, -0.82f);
    private Vector3 m_EditorCameraLookOffset = new Vector3(0f, 0.12f, 0f);
    private float m_EditorCameraDistancePadding = 1.35f;
    private float m_EditorPreviewScaleMultiplier = 1.3f;
    private bool m_WebDesktopPreviewInitialized;

    private const string KDigitronResourcePath = "Digitron/DB_801_03";
#if UNITY_EDITOR
    private const string KDigitronEditorAssetPath = "Assets/Models/DIGITRON stara animacija/DB_801_03.fbx";
#endif
    internal static readonly Dictionary<string, Vector3> HotspotNormalizedAnchors = new Dictionary<string, Vector3>();
    private const string KDigitronRootName = "Digitron Calculator Root";
    private const string KDigitronModelName = "Digitron Model";
    private const string KEditorPreviewCameraName = "Editor Preview Camera";
    private const string KLegacyEditorPreviewName = "Digitron Editor Preview";
    private DigitronCalculatorController m_DigitronController;
    private Transform m_DigitronParent;
    private Camera m_RuntimeCamera;
    private bool m_WorldTrackerHooksBound;

    private void Awake()
    {
        Application.targetFrameRate  = m_TargetFrameRate;
    }

    private void Start()
    {
        EnsureWorldTrackerHooks();

#if UNITY_WEBGL && !UNITY_EDITOR
        CleanupLegacyRuntimeSceneChildren();
        ConfigureWebRuntimeCamera();
        ConfigureWebRuntimeLighting();
        if (IsWebDesktopPreview())
        {
            PrepareWebDesktopPreview();
        }
        else
        {
            SetWorldTrackerInteractionComponents(true);
            LogWebRuntime("Start -> JSShowUI");
            LibraryManager.JSShowUI();
            StartCoroutine(ForceInitialMobileResetRoutine());
        }
#endif

#if UNITY_EDITOR
        if (Application.isPlaying && m_EnableDigitronMode && m_EnableEditorInstantPreview)
        {
            TryBuildHotspotNormalizedAnchors();
            CleanupExtraDigitronSceneObjects();
            PrepareEditorPreviewScene();
            CacheDigitronParent();
            SetupEditorPreviewCamera();
            SpawnDigitron();
            FrameEditorPlayModeCamera();
            HidePlacementUiForEditorPreview();
        }
        else
#endif
        if (m_EnableDigitronMode)
        {
            CacheDigitronParent();
        }
    }
    public void OnPlacedOrigin()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        LogWebRuntime("OnPlacedOrigin received");
#endif

        if (m_EnableDigitronMode)
        {
            SpawnDigitron();
        }
    }

    public void OnResetOrigin()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        LogWebRuntime("OnResetOrigin received");
#endif

        if (m_EnableDigitronMode)
        {
            ResetDigitron();
        }
    }

    private void CacheDigitronParent()
    {
        if (m_DigitronParent)
        {
            return;
        }

#if UNITY_EDITOR
        if (Application.isPlaying && m_EnableEditorInstantPreview)
        {
            m_DigitronParent = transform;
            return;
        }
#endif

        if (TrySetDigitronParentFromSceneObject("MainObject")) return;
        if (TrySetDigitronParentFromSceneObject("Jankec Anchor")) return;
        if (TrySetDigitronParentFromSceneObject("Content")) return;

        var tracker = FindObjectOfType<WorldTracker>();
        if (tracker)
        {
            m_DigitronParent = tracker.transform;
            LogWebRuntime($"Digitron parent resolved: '{m_DigitronParent.name}' (WorldTracker fallback)");
            return;
        }

        m_DigitronParent = transform;
        LogWebRuntime($"Digitron parent resolved: '{m_DigitronParent.name}' (MainController fallback)");
    }

    private void SpawnDigitron()
    {
        CacheDigitronParent();
        if (!m_DigitronParent)
        {
            Debug.LogWarning("Digitron parent transform was not found.");
            return;
        }
        LogWebRuntime($"SpawnDigitron using parent '{m_DigitronParent.name}'");

        CacheExistingDigitronController();

        if (m_DigitronController)
        {
            m_DigitronController.transform.SetParent(m_DigitronParent, false);
            m_DigitronController.transform.localPosition = GetDigitronSpawnLocalPosition();
            m_DigitronController.transform.localRotation = GetDigitronSpawnLocalRotation();
            m_DigitronController.ResetDigitronState();
            m_DigitronController.gameObject.SetActive(true);
            m_DigitronController.HandlePlaced(GetActiveRuntimeCamera());
            LogWebRuntime("SpawnDigitron reused existing controller instance");
#if UNITY_EDITOR
            if (Application.isPlaying && m_EnableEditorInstantPreview)
            {
                FrameEditorPlayModeCamera();
            }
#endif
            return;
        }

        var digitronPrefab = LoadDigitronPrefab();
        GameObject modelInstance = null;
#if UNITY_EDITOR
        modelInstance = TryUseExistingEditorPreview(digitronPrefab);
#endif
        if (!modelInstance && !digitronPrefab)
        {
            Debug.LogError(GetDigitronPrefabMissingMessage());
            return;
        }

        var digitronRoot = new GameObject(KDigitronRootName);
        digitronRoot.transform.SetParent(m_DigitronParent, false);
        digitronRoot.transform.localPosition = GetDigitronSpawnLocalPosition();
        digitronRoot.transform.localRotation = GetDigitronSpawnLocalRotation();

        if (!modelInstance)
        {
            modelInstance = Instantiate(digitronPrefab, digitronRoot.transform, false);
            modelInstance.name = KDigitronModelName;
        }
        else
        {
            modelInstance.transform.SetParent(digitronRoot.transform, false);
            modelInstance.name = KDigitronModelName;
        }

        m_DigitronController = digitronRoot.AddComponent<DigitronCalculatorController>();
        try
        {
            m_DigitronController.Initialize(modelInstance, GetActiveRuntimeCamera(), GetTargetDigitronSize());
            LogWebRuntime($"SpawnDigitron initialized new instance '{digitronRoot.name}'");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[MainController] DigitronCalculatorController.Initialize threw: {e}");
        }

#if UNITY_EDITOR
        if (Application.isPlaying && m_EnableEditorInstantPreview)
        {
            digitronRoot.AddComponent<EditorPreviewMouseOrbit>();
            FrameEditorPlayModeCamera();
        }
#endif
#if UNITY_WEBGL && !UNITY_EDITOR
        if (IsWebDesktopPreview())
        {
            AttachDesktopPreviewControls(digitronRoot.transform);
        }
#endif
    }

    private void ResetDigitron()
    {
        if (m_DigitronController)
        {
            m_DigitronController.ResetDigitronState();
        }
    }

    private Vector3 GetDigitronSpawnLocalPosition()
    {
#if UNITY_EDITOR
        if (Application.isPlaying && m_EnableEditorInstantPreview)
        {
            return m_EditorPreviewLocalPosition;
        }
#endif
        return Vector3.zero;
    }

    private Quaternion GetDigitronSpawnLocalRotation()
    {
#if UNITY_EDITOR
        if (Application.isPlaying && m_EnableEditorInstantPreview)
        {
            return Quaternion.Euler(m_EditorPreviewLocalEulerAngles);
        }
#endif
#if UNITY_WEBGL && !UNITY_EDITOR
        return Quaternion.Euler(m_WebSpawnLocalEulerAngles);
#else
        return Quaternion.identity;
#endif
    }

    private float GetTargetDigitronSize()
    {
#if UNITY_EDITOR
        if (Application.isPlaying && m_EnableEditorInstantPreview)
        {
            return m_TargetDigitronSize * m_EditorPreviewScaleMultiplier;
        }
#endif
#if UNITY_WEBGL && !UNITY_EDITOR
        return m_TargetDigitronSize * m_WebTargetScaleMultiplier;
#endif
        return m_TargetDigitronSize;
    }

    private Camera GetActiveRuntimeCamera()
    {
        if (m_RuntimeCamera)
        {
            return m_RuntimeCamera;
        }

        return Camera.main;
    }

    private void CacheExistingDigitronController()
    {
        if (m_DigitronController)
        {
            return;
        }

        var existingRoot = GameObject.Find(KDigitronRootName);
        if (!existingRoot)
        {
            return;
        }

        m_DigitronController = existingRoot.GetComponent<DigitronCalculatorController>();
    }

    private GameObject LoadDigitronPrefab()
    {
        if (m_DigitronPrefab)
        {
            return m_DigitronPrefab;
        }

#if UNITY_EDITOR
        var editorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(KDigitronEditorAssetPath);
        if (editorPrefab)
        {
            return editorPrefab;
        }
#endif

        var resourcePrefab = Resources.Load<GameObject>(KDigitronResourcePath);
        if (resourcePrefab)
        {
            LogWebRuntime($"Loaded runtime prefab from Resources/{KDigitronResourcePath}");
        }
        return resourcePrefab;
    }

    private string GetDigitronPrefabMissingMessage()
    {
#if UNITY_EDITOR
        return $"Digitron prefab not found. Checked editor asset at '{KDigitronEditorAssetPath}' and Resources/{KDigitronResourcePath}";
#else
        return $"Digitron prefab not found. Checked Resources/{KDigitronResourcePath}";
#endif
    }

    private bool TrySetDigitronParentFromSceneObject(string objectName)
    {
        var sceneObject = FindSceneGameObject(objectName);
        if (!sceneObject)
        {
            return false;
        }

        m_DigitronParent = sceneObject.transform;
        LogWebRuntime($"Digitron parent resolved: '{objectName}'");
        return true;
    }

    private static void LogWebRuntime(string message)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        Debug.Log($"[WebAR][MainController] {message}");
#endif
    }

    private void ConfigureWebRuntimeCamera()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        var cam = GetActiveRuntimeCamera();
        if (!cam)
        {
            return;
        }

        cam.allowHDR = false;
        cam.allowMSAA = false;
        LogWebRuntime("Configured runtime camera (HDR/MSAA disabled)");
#endif
    }

    private void ConfigureWebRuntimeLighting()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        RenderSettings.ambientIntensity = 0.62f;
        var sun = RenderSettings.sun;
        if (!sun)
        {
            var directional = GameObject.Find("Directional Light");
            if (directional)
            {
                sun = directional.GetComponent<Light>();
            }
        }

        if (!sun)
        {
            return;
        }

        sun.intensity = 0.75f;
        sun.color = new Color(1f, 0.98f, 0.94f, 1f);
        LogWebRuntime("Configured runtime lighting for non-burned AR preview");
#endif
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    private bool IsWebDesktopPreview()
    {
        return !Application.isMobilePlatform;
    }

    private void PrepareWebDesktopPreview()
    {
        if (m_WebDesktopPreviewInitialized)
        {
            return;
        }

        var tracker = FindObjectOfType<WorldTracker>();
        if (tracker)
        {
            tracker.enabled = false;
        }
        SetWorldTrackerInteractionComponents(false);

        var placementCanvas = FindSceneGameObject("Placement Canvas");
        if (placementCanvas)
        {
            placementCanvas.SetActive(false);
        }

        var placementIndicator = FindSceneGameObject("Placement Indicator");
        if (placementIndicator)
        {
            placementIndicator.SetActive(false);
        }

        var mainObject = FindSceneGameObject("MainObject");
        if (mainObject)
        {
            mainObject.SetActive(true);
        }

        CacheDigitronParent();
        SpawnDigitron();
        FrameWebDesktopCamera();
        m_WebDesktopPreviewInitialized = true;
        LogWebRuntime("Web desktop preview initialized");
    }

    private static void SetWorldTrackerInteractionComponents(bool isEnabled)
    {
        var swipe = FindObjectOfType<SwipeToRotateY>();
        if (swipe) swipe.enabled = isEnabled;

        var pinch = FindObjectOfType<PinchToScale>();
        if (pinch) pinch.enabled = isEnabled;

        var pan = FindObjectOfType<TwoFingerPan>();
        if (pan) pan.enabled = isEnabled;
    }

    private IEnumerator ForceInitialMobileResetRoutine()
    {
        yield return null;
        yield return new WaitForSeconds(0.2f);

        var tracker = FindObjectOfType<WorldTracker>();
        if (!tracker)
        {
            yield break;
        }

        tracker.ResetOrigin();
        LogWebRuntime("Forced initial ResetOrigin to show placement indicator on first run");
    }

    private void FrameWebDesktopCamera()
    {
        var cameraObject = GetActiveRuntimeCamera();
        if (!cameraObject || m_DigitronController == null)
        {
            return;
        }

        var modelBounds = m_DigitronController.GetWorldBounds();
        var focusPoint = modelBounds.center;
        var distance = Mathf.Max(modelBounds.extents.magnitude * 2.2f, 0.8f);
        var camTransform = cameraObject.transform;
        camTransform.position = focusPoint + new Vector3(0f, modelBounds.extents.y * 0.65f, -distance);
        camTransform.LookAt(focusPoint);
    }

    private static void AttachDesktopPreviewControls(Transform target)
    {
        var controller = target.GetComponent<WebDesktopOrbitZoom>();
        if (!controller)
        {
            controller = target.gameObject.AddComponent<WebDesktopOrbitZoom>();
        }

        controller.enabled = true;
    }
#endif

    private void EnsureWorldTrackerHooks()
    {
        if (m_WorldTrackerHooksBound)
        {
            return;
        }

        var tracker = FindObjectOfType<WorldTracker>();
        if (tracker == null || tracker.eventSettings == null)
        {
            return;
        }

        tracker.eventSettings.OnPlacedOrigin.RemoveListener(OnPlacedOrigin);
        tracker.eventSettings.OnResetOrigin.RemoveListener(OnResetOrigin);
        tracker.eventSettings.OnPlacedOrigin.AddListener(OnPlacedOrigin);
        tracker.eventSettings.OnResetOrigin.AddListener(OnResetOrigin);
        m_WorldTrackerHooksBound = true;

        LogWebRuntime("WorldTracker hooks bound at runtime");
    }

    private void CleanupLegacyRuntimeSceneChildren()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        var toRemove = new List<GameObject>();
        foreach (Transform child in transform)
        {
            if (child == null) continue;
            if (child.name == "Hotspots") continue;
            toRemove.Add(child.gameObject);
        }

        foreach (var go in toRemove)
        {
            LogWebRuntime($"Removing legacy runtime child '{go.name}' from MainController");
            Destroy(go);
        }
#endif
    }

#if UNITY_EDITOR
    private GameObject TryUseExistingEditorPreview(GameObject preferredPrefab)
    {
        if (!Application.isPlaying || !m_EnableEditorInstantPreview)
        {
            return null;
        }

        var existingPreview = FindSceneGameObject(KLegacyEditorPreviewName);
        if (!existingPreview)
        {
            return null;
        }

        existingPreview.SetActive(true);
        var renderers = existingPreview.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            return null;
        }

        if (preferredPrefab)
        {
            var previewSource = PrefabUtility.GetCorrespondingObjectFromSource(existingPreview);
            if (previewSource == preferredPrefab)
            {
                return existingPreview;
            }
        }

        return existingPreview;
    }

    private void HidePlacementUiForEditorPreview()
    {
        var placementCanvas = GameObject.Find("Placement Canvas");
        if (placementCanvas)
        {
            placementCanvas.SetActive(false);
        }

        var placementIndicator = GameObject.Find("Placement Indicator");
        if (placementIndicator)
        {
            placementIndicator.SetActive(false);
        }
    }

    private void PrepareEditorPreviewScene()
    {
        var tracker = FindObjectOfType<WorldTracker>();
        if (tracker)
        {
            tracker.enabled = false;
        }

        DisableEditorPreviewComponent<SwipeToRotateY>();
        DisableEditorPreviewComponent<PinchToScale>();
        DisableEditorPreviewComponent<TwoFingerPan>();

        var mainObject = FindSceneGameObject("MainObject");
        if (mainObject)
        {
            mainObject.SetActive(true);
        }

        var content = FindSceneGameObject("Content");
        if (content)
        {
            content.SetActive(true);
        }

        var jankecAnchor = FindSceneGameObject("Jankec Anchor");
        if (jankecAnchor)
        {
            jankecAnchor.SetActive(true);
        }
    }

    private void FrameEditorPlayModeCamera()
    {
        var cameraObject = GetActiveRuntimeCamera();
        if (!cameraObject || m_DigitronController == null)
        {
            return;
        }

        var cameraTransform = cameraObject.transform;
        var modelBounds = m_DigitronController.GetWorldBounds();
        var focusPoint = modelBounds.center + m_EditorCameraLookOffset;
        var viewDirection = m_EditorCameraOffset.sqrMagnitude > 0.0001f
            ? m_EditorCameraOffset.normalized
            : new Vector3(0f, 0f, -1f);
        var verticalHalfFov = cameraObject.fieldOfView * 0.5f * Mathf.Deg2Rad;
        var horizontalHalfFov = Mathf.Atan(Mathf.Tan(verticalHalfFov) * cameraObject.aspect);
        var distanceForHeight = modelBounds.extents.y / Mathf.Max(Mathf.Tan(verticalHalfFov), 0.01f);
        var distanceForWidth = modelBounds.extents.x / Mathf.Max(Mathf.Tan(horizontalHalfFov), 0.01f);
        var distance = Mathf.Max(distanceForHeight, distanceForWidth, modelBounds.extents.z) * m_EditorCameraDistancePadding;
        distance = Mathf.Max(distance, 0.35f);
        cameraTransform.position = focusPoint + (viewDirection * distance);
        cameraTransform.LookAt(focusPoint);
    }

    private void SetupEditorPreviewCamera()
    {
        if (m_RuntimeCamera)
        {
            return;
        }

        var existingPreviewCamera = GameObject.Find(KEditorPreviewCameraName);
        if (existingPreviewCamera)
        {
            m_RuntimeCamera = existingPreviewCamera.GetComponent<Camera>();
            if (m_RuntimeCamera)
            {
                return;
            }
        }

        var sourceCamera = Camera.main;
        if (!sourceCamera)
        {
            return;
        }

        var sourceGameObject = sourceCamera.gameObject;
        sourceGameObject.tag = "Untagged";

        var sourceAudioListener = sourceGameObject.GetComponent<AudioListener>();
        if (sourceAudioListener)
        {
            sourceAudioListener.enabled = false;
        }

        sourceCamera.enabled = false;

        var behaviours = sourceGameObject.GetComponents<MonoBehaviour>();
        foreach (var behaviour in behaviours)
        {
            if (behaviour && behaviour.GetType().Name == "ARCamera")
            {
                behaviour.enabled = false;
            }
        }

        var previewCameraObject = new GameObject(KEditorPreviewCameraName);
        previewCameraObject.tag = "MainCamera";
        m_RuntimeCamera = previewCameraObject.AddComponent<Camera>();
        m_RuntimeCamera.clearFlags = CameraClearFlags.Skybox;
        m_RuntimeCamera.backgroundColor = new Color(0.58f, 0.67f, 0.74f, 1f);
        m_RuntimeCamera.cullingMask = ~0;
        m_RuntimeCamera.fieldOfView = sourceCamera.fieldOfView;
        m_RuntimeCamera.nearClipPlane = sourceCamera.nearClipPlane;
        m_RuntimeCamera.farClipPlane = sourceCamera.farClipPlane;
        m_RuntimeCamera.allowHDR = false;
        previewCameraObject.AddComponent<AudioListener>();

        var sceneView = SceneView.lastActiveSceneView;
        if (sceneView != null && sceneView.camera != null)
        {
            var sceneCamera = sceneView.camera;
            previewCameraObject.transform.position = sceneCamera.transform.position;
            previewCameraObject.transform.rotation = sceneCamera.transform.rotation;
            m_RuntimeCamera.fieldOfView = sceneCamera.fieldOfView;
        }
    }

    private static void CleanupLegacyEditorPreviewObjects()
    {
        var transforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (var candidate in transforms)
        {
            if (candidate.hideFlags != HideFlags.None)
            {
                continue;
            }

            if (!candidate.gameObject.scene.IsValid())
            {
                continue;
            }

            if (candidate.name == KLegacyEditorPreviewName)
            {
                Object.DestroyImmediate(candidate.gameObject);
            }
        }
    }

    private static void TryBuildHotspotNormalizedAnchors()
    {
        HotspotNormalizedAnchors.Clear();

        // Find "Hotspots" parent anywhere in scene (may be child of MainController, scene model, or standalone)
        var hotspotsObj = FindSceneGameObject("Hotspots");
        if (!hotspotsObj || hotspotsObj.transform.childCount == 0) return;
        var hotspotsParent = hotspotsObj.transform;

        // Use the scene model's renderer bounds for normalization if available
        var sceneModel = FindSceneGameObject("db801-novo-odvojene-tipke");
        Bounds bounds;
        if (sceneModel)
        {
            var renderers = sceneModel.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;
            bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        }
        else
        {
            // Fallback: derive bounds from hotspot child positions
            bounds = new Bounds(hotspotsParent.GetChild(0).position, Vector3.zero);
            foreach (Transform child in hotspotsParent) bounds.Encapsulate(child.position);
            bounds.Expand(0.05f);
        }

        foreach (Transform child in hotspotsParent)
        {
            var id = child.name.ToLowerInvariant();
            foreach (var knownId in new[] { "housing", "keyboard", "board", "chips", "batteries", "display" })
            {
                if (!id.Contains(knownId)) continue;
                var p = child.position;
                var normalized = new Vector3(
                    bounds.size.x > 0f ? (p.x - bounds.min.x) / bounds.size.x : 0.5f,
                    bounds.size.y > 0f ? (p.y - bounds.min.y) / bounds.size.y : 0.5f,
                    bounds.size.z > 0f ? (p.z - bounds.min.z) / bounds.size.z : 0.5f);
                HotspotNormalizedAnchors[knownId] = normalized;
                Debug.Log($"[Hotspot] {knownId} -> normalized {normalized:F3} (world {p:F3})");
                break;
            }
        }
        Debug.Log($"[Hotspot] Built {HotspotNormalizedAnchors.Count} anchors from '{hotspotsObj.name}' (parent: {(hotspotsObj.transform.parent ? hotspotsObj.transform.parent.name : "none")})");
    }

    private static void CleanupExtraDigitronSceneObjects()
    {
        var transforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (var candidate in transforms)
        {
            if (candidate.hideFlags != HideFlags.None)
            {
                continue;
            }

            if (!candidate.gameObject.scene.IsValid())
            {
                continue;
            }

            if (candidate.parent != null)
            {
                continue;
            }

            if (candidate.name != "db801-novo-odvojene-tipke")
            {
                continue;
            }

            Object.DestroyImmediate(candidate.gameObject);
        }
    }

    private static void DisableEditorPreviewComponent<T>() where T : Behaviour
    {
        var component = FindObjectOfType<T>();
        if (component)
        {
            component.enabled = false;
        }
    }

    private static GameObject FindSceneGameObject(string objectName)
    {
        var activeObject = GameObject.Find(objectName);
        if (activeObject)
        {
            return activeObject;
        }

        var transforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (var candidate in transforms)
        {
            if (candidate.hideFlags != HideFlags.None)
            {
                continue;
            }

            if (!candidate.gameObject.scene.IsValid())
            {
                continue;
            }

            if (candidate.name == objectName)
            {
                return candidate.gameObject;
            }
        }

        return null;
    }
#endif

#if !UNITY_EDITOR
    private static GameObject FindSceneGameObject(string objectName)
    {
        return GameObject.Find(objectName);
    }
#endif
}
