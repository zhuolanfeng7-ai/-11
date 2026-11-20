using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

[InitializeOnLoad]
public static class WebGLBuildAndZip
{
    private const string RequestFlag = "Temp/RequestWebGLBuildAndZip.flag";
    private const string ReportPath = "Temp/WebGLBuildAndZipReport.txt";

    static WebGLBuildAndZip()
    {
        EditorApplication.delayCall += TryRun;
    }

    private static void TryRun()
    {
        if (!File.Exists(RequestFlag))
            return;

        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += TryRun;
            return;
        }

        File.Delete(RequestFlag);
        BuildAndZip();
    }

    [MenuItem("Tools/WebGL/Build And Create itch ZIP")]
    public static void BuildAndZip()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string buildDirectory = Path.Combine(projectRoot, "Builds", "WebGL");
        string zipPath = Path.Combine(projectRoot, "Builds", "WebGL_itch_20261120.zip");

        try
        {
            if (Directory.Exists(buildDirectory))
                Directory.Delete(buildDirectory, true);
            if (File.Exists(zipPath))
                File.Delete(zipPath);
            Directory.CreateDirectory(buildDirectory);

            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = buildDirectory,
                target = BuildTarget.WebGL,
                options = BuildOptions.CleanBuildCache
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("WebGL build failed: " + report.summary.result);

            ZipFile.CreateFromDirectory(buildDirectory, zipPath, System.IO.Compression.CompressionLevel.Optimal, false);

            string dataFile = Directory.GetFiles(Path.Combine(buildDirectory, "Build"), "*.data*", SearchOption.TopDirectoryOnly)
                .OrderByDescending(path => new FileInfo(path).Length)
                .FirstOrDefault();
            long dataBytes = dataFile == null ? 0 : new FileInfo(dataFile).Length;
            long zipBytes = new FileInfo(zipPath).Length;
            string result =
                "SUCCESS" + Environment.NewLine +
                "ZIP=" + zipPath + Environment.NewLine +
                "ZIP_MB=" + (zipBytes / 1024d / 1024d).ToString("F2") + Environment.NewLine +
                "DATA=" + (dataFile ?? "NOT_FOUND") + Environment.NewLine +
                "DATA_MB=" + (dataBytes / 1024d / 1024d).ToString("F2") + Environment.NewLine +
                "SCENES=" + string.Join(",", scenes);
            File.WriteAllText(ReportPath, result);
            Debug.Log("[WebGL Build And Zip] " + result);
        }
        catch (Exception exception)
        {
            File.WriteAllText(ReportPath, "FAILED" + Environment.NewLine + exception);
            Debug.LogException(exception);
        }
    }
}
