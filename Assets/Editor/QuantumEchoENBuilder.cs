using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Dedicated builder for the QUANTUM ECHO English Edition.
/// Invoked via Unity batchmode: -executeMethod QuantumEchoENBuilder.BuildENAndroid
/// Output: Builds/EN/QuantumEcho-EN-PICO.apk
/// </summary>
public static class QuantumEchoENBuilder
{
    const string OutputPath = "Builds/EN/QuantumEcho-EN-PICO.apk";
    const string TitleScene = "Assets/QuantumEcho/Scenes/QuantumEcho_Title.unity";
    const string MainScene  = "Assets/QuantumEcho/Scenes/QuantumEcho_Main.unity";

    /// <summary>Combined entry: take screenshots (Play mode) then build APK.
    /// Use QuantumEchoQALauncher.CaptureAndBuild for the interactive menu flow.
    /// This static entry keeps the -executeMethod batchmode hook functional.</summary>
    public static void CaptureAndBuild()
    {
        QuantumEchoQALauncher.CaptureAndBuild();
    }

    public static void BuildENAndroid()
    {
        Debug.Log("[EN Builder] Starting QUANTUM ECHO English Edition Android build...");
        Directory.CreateDirectory("Builds/EN");

        PlayerSettings.productName = "QUANTUM ECHO";
        PlayerSettings.SetApplicationIdentifier(
            NamedBuildTarget.Android,
            "com.quantumecho.decrypt1.en");

        var icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/AppIcon/AppIcon.png");
        if (icon != null)
            PlayerSettings.SetIcons(NamedBuildTarget.Android, new[] { icon }, IconKind.Application);

        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

        if (File.Exists(OutputPath))
            FileUtil.DeleteFileOrDirectory(OutputPath);

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { TitleScene, MainScene },
            locationPathName = OutputPath,
            target = BuildTarget.Android,
            options = BuildOptions.CleanBuildCache
        });

        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[EN Builder] Build succeeded: {OutputPath} ({report.summary.totalSize / 1024 / 1024} MB)");
        }
        else
        {
            throw new BuildFailedException($"[EN Builder] Android build failed: {report.summary.result}");
        }
    }

    public static void VerifyBasic()
    {
        var issues = new System.Collections.Generic.List<string>();
        if (!File.Exists(TitleScene)) issues.Add("Title scene missing");
        if (!File.Exists(MainScene))  issues.Add("Main scene missing");
        if (!File.Exists("Assets/AppIcon/AppIcon.png")) issues.Add("AppIcon.png missing");
        if (PlayerSettings.productName != "QUANTUM ECHO") issues.Add("productName is not QUANTUM ECHO");
        if (PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android) != "com.quantumecho.decrypt1.en")
            issues.Add("Bundle ID mismatch");
        if (issues.Count > 0)
            throw new BuildFailedException("[EN Builder] Verification failed:\n" + string.Join("\n", issues));
        Debug.Log("[EN Builder] Basic verification passed.");
    }
}
