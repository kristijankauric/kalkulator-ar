using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor window: zamjenjuje mesh+materijale na baterija_0X objektima i
/// daje kontrolu nad rotacijom svakog objekta direktno u editoru.
/// Otvori: Tools > Digitron > Zamijeni Baterije...
/// </summary>
public class BaterijaSwapper : EditorWindow
{
    private const string FbxPath = "Assets/!k/baterija.fbx";
    private const string TexPath = "Assets/!k/baterija.fbm/beterija-tekstura.jpg";
    private const int    BatCount = 4;

    // Rotacija koja se primjenjuje na svaki baterija_0X objekt
    private Vector3[] m_Rotations = new Vector3[BatCount]
    {
        new Vector3(270f, 0f, 0f),
        new Vector3(0f, 90f, 90f),
        new Vector3(0f, 90f, 270f),
        new Vector3(0f, 90f, 90f),
    };

    private bool   m_UseSharedRotation  = false;
    private Vector3 m_SharedRotation    = new Vector3(0f, 0f, 0f);

    [MenuItem("Tools/Digitron/Zamijeni Baterije...")]
    static void OpenWindow() => GetWindow<BaterijaSwapper>("Baterija Swapper");

    void OnGUI()
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Zamjena baterija", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Klikni 'Primijeni u scenu' pa prilagodi rotacije. " +
            "Svaki klik na Primijeni odmah ažurira objekte u sceni.",
            MessageType.Info);

        EditorGUILayout.Space(6);

        // ── Opcija: ista rotacija za sve ────────────────────────────────────
        m_UseSharedRotation = EditorGUILayout.Toggle("Ista rotacija za sve", m_UseSharedRotation);
        if (m_UseSharedRotation)
        {
            EditorGUI.indentLevel++;
            m_SharedRotation = EditorGUILayout.Vector3Field("Rotacija (sve)", m_SharedRotation);
            EditorGUI.indentLevel--;
        }
        else
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Rotacije po bateriji (Local Euler)", EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel++;
            for (int i = 0; i < BatCount; i++)
                m_Rotations[i] = EditorGUILayout.Vector3Field($"baterija_0{i + 1}", m_Rotations[i]);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(8);

        // ── Gumbi ───────────────────────────────────────────────────────────
        if (GUILayout.Button("Primijeni u scenu", GUILayout.Height(32)))
            Apply(applyMesh: true);

        if (GUILayout.Button("Samo rotacije (bez swap mesha)", GUILayout.Height(24)))
            Apply(applyMesh: false);

        EditorGUILayout.Space(4);
        EditorGUILayout.HelpBox(
            "Kad pronađeš prave vrijednosti, spremi ih — bit će upisane u runtime SwapBaterija().",
            MessageType.None);
    }

    void Apply(bool applyMesh)
    {
        Mesh newMesh = null;
        Material[] newMats = null;

        if (applyMesh)
        {
            var allAssets = AssetDatabase.LoadAllAssetsAtPath(FbxPath);
            if (allAssets == null || allAssets.Length == 0)
            {
                Debug.LogError($"[BaterijaSwapper] FBX nije pronađen: {FbxPath}");
                return;
            }

            newMesh = allAssets.OfType<Mesh>().FirstOrDefault();
            newMats = allAssets.OfType<Material>().ToArray();

            if (newMesh == null)
            {
                Debug.LogError("[BaterijaSwapper] Nema mesha u baterija.fbx");
                return;
            }

            // Flip teksture horizontalno
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(TexPath);
            foreach (var mat in newMats)
            {
                if (mat == null) continue;
                Undo.RecordObject(mat, "Baterija: fix texture flip");
                if (tex != null && mat.mainTexture == null) mat.mainTexture = tex;
                mat.mainTextureScale  = new Vector2(-1f, 1f);
                mat.mainTextureOffset = new Vector2(1f, 0f);
                EditorUtility.SetDirty(mat);
            }
        }

        int found = 0;
        for (int i = 0; i < BatCount; i++)
        {
            var goName = $"baterija_0{i + 1}";
            var go = GameObject.Find(goName);
            if (go == null)
            {
                Debug.LogWarning($"[BaterijaSwapper] '{goName}' nije pronađen u sceni");
                continue;
            }

            Undo.RecordObject(go.transform, $"Baterija: rotacija {goName}");
            var rot = m_UseSharedRotation ? m_SharedRotation : m_Rotations[i];
            go.transform.localEulerAngles = rot;

            if (applyMesh)
            {
                var mf = go.GetComponent<MeshFilter>();
                var mr = go.GetComponent<MeshRenderer>();

                if (mf != null)
                {
                    Undo.RecordObject(mf, $"Baterija: mesh {goName}");
                    mf.sharedMesh = newMesh;
                    EditorUtility.SetDirty(mf);
                }
                if (mr != null && newMats != null && newMats.Length > 0)
                {
                    Undo.RecordObject(mr, $"Baterija: materijal {goName}");
                    mr.sharedMaterials = newMats;
                    EditorUtility.SetDirty(mr);
                }
            }

            EditorUtility.SetDirty(go);
            Debug.Log($"[BaterijaSwapper] {goName} → rot={rot}");
            found++;
        }

        // Sync rotations to MainController SerializedFields so runtime build uses them too
        SyncToMainController();

        if (applyMesh) AssetDatabase.SaveAssets();
        Debug.Log($"[BaterijaSwapper] Gotovo: {found}/{BatCount} baterija ažurirano.");
    }

    void SyncToMainController()
    {
        var mc = Object.FindObjectOfType<MainController>();
        if (mc == null)
        {
            Debug.LogWarning("[BaterijaSwapper] MainController nije pronađen — rotacije nisu sinkronizirane.");
            return;
        }

        var so = new SerializedObject(mc);
        var names = new[] { "m_BatRot01", "m_BatRot02", "m_BatRot03", "m_BatRot04" };
        for (int i = 0; i < BatCount; i++)
        {
            var prop = so.FindProperty(names[i]);
            if (prop == null) continue;
            var rot = m_UseSharedRotation ? m_SharedRotation : m_Rotations[i];
            prop.vector3Value = rot;
        }
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(mc);
        Debug.Log("[BaterijaSwapper] Rotacije sinkronizirane u MainController.");
    }
}
