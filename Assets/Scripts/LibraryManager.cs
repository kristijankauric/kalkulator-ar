using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class LibraryManager : MonoBehaviour
{
    [DllImport("__Internal")]
    public static extern void JSShowUI();
    [DllImport("__Internal")]
    public static extern void JSPlaceOrigin();
    [DllImport("__Internal")]
    public static extern void JSResetOrigin();
}
