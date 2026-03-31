using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class WebGLBuildCommand
{
    public static void BuildPagesWebGL()
    {
        var scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            throw new System.Exception("No enabled scenes found in EditorBuildSettings.");
        }

        var buildOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = "Build/docs",
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(buildOptions);
        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new System.Exception($"WebGL build failed: {report.summary.result}");
        }
    }
}
