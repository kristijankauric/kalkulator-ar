using UnityEngine;
using Imagine.WebAR;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainController : MonoBehaviour
{
    private const string KOpenClipName = "CalculatorOpen";
    private const string KFallbackClipName = "Take 001";
    [SerializeField] private int m_TargetFrameRate = 30;
    private bool m_EnableDigitronMode = true;
    private float m_TargetDigitronSize = 0.3f;
    private bool m_EnableEditorInstantPreview = true;
    private Vector3 m_EditorPreviewLocalPosition = new Vector3(0f, 0.1f, 1.25f);
    private Vector3 m_EditorPreviewLocalEulerAngles = new Vector3(0f, 180f, 0f);
    private Vector3 m_EditorCameraOffset = new Vector3(-0.08f, 0.12f, -0.82f);
    private Vector3 m_EditorCameraLookOffset = new Vector3(0f, 0.12f, 0f);
    private float m_EditorCameraDistancePadding = 1.35f;
    private float m_EditorPreviewScaleMultiplier = 1.3f;

    private const string KDigitronResourcePath = "Digitron/DB_801_03";
    private const string KDigitronEditorAssetPath = "Assets/Models/DIGITRON stara animacija/DB_801_03.fbx";
    private const string KDigitronRootName = "Digitron Calculator Root";
    private const string KDigitronModelName = "Digitron Model";
    private const string KEditorPreviewCameraName = "Editor Preview Camera";
    private const string KLegacyEditorPreviewName = "Digitron Editor Preview";
    private DigitronCalculatorController m_DigitronController;
    private Transform m_DigitronParent;
    private Camera m_RuntimeCamera;

    private void Awake()
    {
        Application.targetFrameRate  = m_TargetFrameRate;
    }

    private void Start()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        LibraryManager.HandleAppInitialized();
#endif

#if UNITY_EDITOR
        if (Application.isPlaying && m_EnableDigitronMode && m_EnableEditorInstantPreview)
        {
            CleanupLegacyEditorPreviewObjects();
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
        LibraryManager.JSPlaceOrigin();
#endif

        if (m_EnableDigitronMode)
        {
            SpawnDigitron();
        }
    }

    public void OnResetOrigin()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        LibraryManager.JSResetOrigin();
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

        var anchor = FindSceneGameObject("Jankec Anchor");
        if (anchor)
        {
            m_DigitronParent = anchor.transform;
            return;
        }

        var content = FindSceneGameObject("Content");
        if (content)
        {
            m_DigitronParent = content.transform;
        }
    }

    private void SpawnDigitron()
    {
        CacheDigitronParent();
        if (!m_DigitronParent)
        {
            Debug.LogWarning("Digitron parent transform was not found.");
            return;
        }

        CacheExistingDigitronController();

        if (m_DigitronController)
        {
            m_DigitronController.transform.SetParent(m_DigitronParent, false);
            m_DigitronController.transform.localPosition = GetDigitronSpawnLocalPosition();
            m_DigitronController.transform.localRotation = GetDigitronSpawnLocalRotation();
            m_DigitronController.ResetDigitronState();
            m_DigitronController.gameObject.SetActive(true);
            m_DigitronController.HandlePlaced(GetActiveRuntimeCamera());
#if UNITY_EDITOR
            if (Application.isPlaying && m_EnableEditorInstantPreview)
            {
                FrameEditorPlayModeCamera();
            }
#endif
            return;
        }

        var digitronPrefab = LoadDigitronPrefab();
        if (!digitronPrefab)
        {
            Debug.LogError($"Digitron prefab not found. Checked editor asset at '{KDigitronEditorAssetPath}' and Resources/{KDigitronResourcePath}");
            return;
        }

        var digitronRoot = new GameObject(KDigitronRootName);
        digitronRoot.transform.SetParent(m_DigitronParent, false);
        digitronRoot.transform.localPosition = GetDigitronSpawnLocalPosition();
        digitronRoot.transform.localRotation = GetDigitronSpawnLocalRotation();

        var modelInstance = Instantiate(digitronPrefab, digitronRoot.transform, false);
        modelInstance.name = KDigitronModelName;

        m_DigitronController = digitronRoot.AddComponent<DigitronCalculatorController>();
        m_DigitronController.Initialize(modelInstance, GetActiveRuntimeCamera(), GetTargetDigitronSize());

#if UNITY_EDITOR
        if (Application.isPlaying && m_EnableEditorInstantPreview)
        {
            digitronRoot.AddComponent<EditorPreviewMouseOrbit>();
            FrameEditorPlayModeCamera();
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
        return Quaternion.identity;
    }

    private float GetTargetDigitronSize()
    {
#if UNITY_EDITOR
        if (Application.isPlaying && m_EnableEditorInstantPreview)
        {
            return m_TargetDigitronSize * m_EditorPreviewScaleMultiplier;
        }
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
#if UNITY_EDITOR
        if (Application.isPlaying && m_EnableEditorInstantPreview)
        {
            var editorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(KDigitronEditorAssetPath);
            if (editorPrefab && PrefabHasOpenAnimation(KDigitronEditorAssetPath))
            {
                return editorPrefab;
            }
        }
#endif

        return Resources.Load<GameObject>(KDigitronResourcePath);
    }

#if UNITY_EDITOR
    private static bool PrefabHasOpenAnimation(string assetPath)
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        foreach (var asset in assets)
        {
            if (asset is AnimationClip clip &&
                (clip.name == KOpenClipName || clip.name == KFallbackClipName))
            {
                return true;
            }
        }

        return false;
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
        var targetTransform = m_DigitronController.transform;
        var focusPoint = targetTransform.position + m_EditorCameraLookOffset;
        cameraTransform.position = targetTransform.position + m_EditorCameraOffset;
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
}
