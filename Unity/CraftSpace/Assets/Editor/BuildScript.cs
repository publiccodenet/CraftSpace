using UnityEditor;
using UnityEngine;
using System.IO;

public class BuildScript
{
    [MenuItem("Build/Build WebGL")]
    public static void BuildWebGL()
    {
        // Set up build settings
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        
        buildPlayerOptions.scenes = GetEnabledScenes();
        buildPlayerOptions.targetGroup = BuildTargetGroup.WebGL;
        buildPlayerOptions.target = BuildTarget.WebGL;
        buildPlayerOptions.options = BuildOptions.None;
        
        // Set the output path
        buildPlayerOptions.locationPathName = "Build/WebGL";
        
        // Configure WebGL specific settings
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.memorySize = 512; // Adjust based on your app's needs
        
        // Build the player
        Debug.Log("Starting WebGL build...");
        BuildPipeline.BuildPlayer(buildPlayerOptions);
        Debug.Log("WebGL build completed!");
    }
    
    private static string[] GetEnabledScenes()
    {
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        string[] enabledScenes = new string[scenes.Length];
        
        for (int i = 0; i < scenes.Length; i++)
        {
            if (scenes[i].enabled)
            {
                enabledScenes[i] = scenes[i].path;
            }
        }
        
        return enabledScenes;
    }
} 