#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class WebGLBuildAutomation
{
    private const string BuildPath = "Build";

    [MenuItem("Tools/Build/Digitron WebGL Build")]
    public static void BuildWebGL()
    {
        var scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            throw new System.Exception("No enabled scenes found in EditorBuildSettings.");
        }

        if (!Directory.Exists(BuildPath))
        {
            Directory.CreateDirectory(BuildPath);
        }

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = BuildPath,
            target = BuildTarget.WebGL,
            options = BuildOptions.None,
        });

        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new System.Exception($"WebGL build failed: {report.summary.result}");
        }

        Debug.Log($"WebGL build succeeded: {report.summary.outputPath}");
    }
}
#endif
