using UnityEngine;

public class MainController : MonoBehaviour
{
    public void OnPlacedOrigin()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        LibraryManager.JSPlaceOrigin();
#endif
    }

    public void OnResetOrigin()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        LibraryManager.JSResetOrigin();
#endif
    }
}
