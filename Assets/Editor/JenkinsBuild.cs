using System.Linq
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class JenkinsBuild
{
    public static void BuildWindows()
    {
        string[] scenes = GetEnabledScenes();

        if (scenes.Length == 0)
        {
            Debug.LogError("Build Settings に有効なSceneがありません。");
            EditorApplication.Exit(1);
            return;
        }

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = "Build/Windows/CreatorKousien.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);

        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"Build succeeded: {report.summary.totalSize} bytes");
            EditorApplication.Exit(0);
        }
        else
        {
            Debug.LogError($"Build failed: {report.summary.result}");
            EditorApplication.Exit(1);
        }
    }

    private static string[] GetEnabledScenes()
    {
        return System.Array.FindAll(
            EditorBuildSettings.scenes,
            scene => scene.enabled
        ).Select(scene => scene.path).ToArray();
    }
}
