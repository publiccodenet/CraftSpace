using UnityEditor;
using UnityEngine;
using System.IO;

public class BuildScript
{
    [MenuItem("Build/WebGL")]
    public static void BuildWebGL()
    {
        // Define the build path
        string buildPath = Path.Combine(Application.dataPath, "..", "Build", "WebGL");
        
        // Make sure the directory exists
        if (!Directory.Exists(buildPath))
        {
            Directory.CreateDirectory(buildPath);
        }
        
        Debug.Log("Starting WebGL build to: " + buildPath);
        
        // Configure build settings
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = GetScenesToBuild(),
            locationPathName = buildPath,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };
        
        // Start the build
        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;
        
        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log("Build succeeded: " + summary.totalSize + " bytes");
        }
        else if (summary.result == BuildResult.Failed)
        {
            Debug.LogError("Build failed");
        }
    }
    
    private static string[] GetScenesToBuild()
    {
        // Get all enabled scenes from the build settings
        int sceneCount = EditorBuildSettings.scenes.Length;
        string[] scenes = new string[sceneCount];
        
        for (int i = 0; i < sceneCount; i++)
        {
            scenes[i] = EditorBuildSettings.scenes[i].path;
        }
        
        return scenes;
    }
} 